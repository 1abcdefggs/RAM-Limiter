using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RAMLIMITER
{
    // State for one monitored app name: its interval, mode, when it is next due and its report throttle.
    // Scheduler calls Tick with this app's processes from a snapshot shared by all monitors.
    sealed class AppMonitor
    {
        static readonly TimeSpan ReportThrottle = TimeSpan.FromSeconds(5);
        // An instance is idle when it used less than this share of one core since the last tick
        const double IdleCpuShare = 0.01;

        // Per-instance history for idle mode; StartTime tells a reused pid from the original process
        sealed class Sample
        {
            public DateTime StartTime, Taken;
            public TimeSpan Cpu;
            public int IdleTicks;
            public bool Trimmed;
            // The priority found before this tool lowered it; null when it was not lowered
            public uint? Original;
        }

        public readonly string Name;
        public readonly int IntervalMs;
        readonly TrimMode mode;
        readonly int idleIntervals;
        readonly Dictionary<int, Sample> samples = new Dictionary<int, Sample>();
        public DateTime NextDue = DateTime.UtcNow;
        double lastReportedLoad = -1;
        DateTime lastReport = DateTime.MinValue;
        // Tick runs on the scheduler thread, Restore on the caller's; stopped blocks late ticks
        readonly object gate = new object();
        bool stopped;

        public AppMonitor(string name, int intervalMs, TrimMode mode, int idleIntervals)
        {
            Name = name;
            IntervalMs = intervalMs;
            this.mode = mode;
            this.idleIntervals = idleIntervals;
        }

        bool ReportDue() => DateTime.UtcNow - lastReport > ReportThrottle;

        void Report(ConsoleColor color, string message)
        {
            Log.Write(color, message);
            lastReport = DateTime.UtcNow;
        }

        void ReportThrottled(ConsoleColor color, string message)
        {
            if (ReportDue()) Report(color, message);
        }

        // Print the system load when it changed noticeably or the throttle allows it
        void ReportLoad(uint? load, string detail)
        {
            if (!load.HasValue || (Math.Abs(load.Value - lastReportedLoad) <= 1.0 && !ReportDue())) return;
            Report(ConsoleColor.Cyan, $"{Name.ToUpper()}: Total RAM usage: {(double)load.Value:F2}% ({detail})");
            lastReportedLoad = load.Value;
        }

        // false (and a throttled report) when the trim failed
        bool Trim(Process proc)
        {
            try
            {
                if (NativeMethods.EmptyWorkingSet(proc.Handle)) return true;
                ReportThrottled(ConsoleColor.DarkYellow, $"{Name}: EmptyWorkingSet failed (pid {proc.Id})");
            }
            catch (Exception ex)
            {
                ReportThrottled(ConsoleColor.Red, $"{Name}: error trimming process (pid {proc.Id}): {ex.Message}");
            }
            return false;
        }

        static bool SetPriority(Process proc, uint priority)
        {
            try { return NativeMethods.SetMemoryPriority(proc.Handle, priority); }
            catch { return false; }
        }

        // Lowers the memory priority and returns the value it had, or null when nothing was changed
        // (Windows may already have lowered it, or the process cannot be queried).
        static uint? Lower(Process proc)
        {
            try
            {
                var current = NativeMethods.GetMemoryPriority(proc.Handle);
                if (current > NativeMethods.MemoryPriorityLow && NativeMethods.SetMemoryPriority(proc.Handle, NativeMethods.MemoryPriorityLow)) return current;
            }
            catch { }
            return null;
        }

        // The snapshot owns the processes, so nothing is disposed here.
        public void Tick(List<Process> instances, uint? load, int foregroundPid)
        {
            lock (gate) if (!stopped) TickCore(instances, load, foregroundPid);
            NextDue = DateTime.UtcNow.AddMilliseconds(IntervalMs);
        }

        void TickCore(List<Process> instances, uint? load, int foregroundPid)
        {
            try
            {
                if (instances == null || instances.Count == 0)
                {
                    samples.Clear();
                    ReportThrottled(ConsoleColor.Yellow, $"{Name}: not running");
                }
                else if (mode == TrimMode.Largest) TickLargest(instances, load);
                else if (mode == TrimMode.All) TickAll(instances, load);
                else TickIdle(instances, load, foregroundPid);
            }
            catch (Exception ex)
            {
                Log.Write(ConsoleColor.Red, $"Error handling {Name}: {ex.Message}");
            }
        }

        // Stop for good and put back the original memory priority on every instance this monitor lowered.
        // A pid is only touched when its start time still matches, so a reused pid is left alone.
        public void Restore()
        {
            lock (gate)
            {
                stopped = true;
                foreach (var entry in samples)
                {
                    var original = entry.Value.Original;
                    if (!original.HasValue) continue;
                    try
                    {
                        using (var p = Process.GetProcessById(entry.Key))
                            if (p.StartTime == entry.Value.StartTime) NativeMethods.SetMemoryPriority(p.Handle, original.Value);
                    }
                    catch { } // the process has exited; its priority went with it
                }
                samples.Clear();
            }
        }

        void TickLargest(List<Process> instances, uint? load)
        {
            var best = instances[0];
            foreach (var p in instances) if (p.WorkingSet64 > best.WorkingSet64) best = p;
            Trim(best);
            ReportLoad(load, $"pid {best.Id}");
        }

        void TickAll(List<Process> instances, uint? load)
        {
            int trimmed = 0;
            foreach (var p in instances) if (Trim(p)) trimmed++;
            ReportLoad(load, $"trimmed {trimmed} of {instances.Count}");
        }

        // Trim each instance once, after it has been idle for idleIntervals ticks in a row.
        // It becomes eligible again only after it has been active.
        void TickIdle(List<Process> instances, uint? load, int foregroundPid)
        {
            var now = DateTime.UtcNow;
            var seen = new HashSet<int>();
            int idle = 0, trimmedNow = 0;
            foreach (var p in instances)
            {
                DateTime start;
                TimeSpan cpu;
                try { start = p.StartTime; cpu = p.TotalProcessorTime; }
                catch { continue; } // no query access: leave this instance alone
                seen.Add(p.Id);
                if (!samples.TryGetValue(p.Id, out var s) || s.StartTime != start)
                {
                    samples[p.Id] = new Sample { StartTime = start, Taken = now, Cpu = cpu };
                    continue; // first sample is the baseline
                }
                bool busy = p.Id == foregroundPid || (cpu - s.Cpu).TotalMilliseconds >= (now - s.Taken).TotalMilliseconds * IdleCpuShare;
                s.Cpu = cpu;
                s.Taken = now;
                if (busy)
                {
                    s.IdleTicks = 0;
                    s.Trimmed = false;
                    if (s.Original.HasValue && SetPriority(p, s.Original.Value)) s.Original = null;
                    continue;
                }
                idle++;
                if (++s.IdleTicks < idleIntervals) continue;
                if (!s.Original.HasValue) s.Original = Lower(p);
                if (!s.Trimmed && Trim(p)) { s.Trimmed = true; trimmedNow++; }
            }
            foreach (var pid in new List<int>(samples.Keys)) if (!seen.Contains(pid)) samples.Remove(pid);
            if (trimmedNow > 0) lastReportedLoad = -1; // always report the tick that trimmed something
            ReportLoad(load, $"{idle} idle of {instances.Count}, trimmed {trimmedNow} now");
        }
    }
}
