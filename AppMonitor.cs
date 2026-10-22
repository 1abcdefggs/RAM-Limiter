using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RAMLIMITER
{
    // State for one monitored app name: its interval, when it is next due and its report throttle.
    // Scheduler calls Tick with this app's processes from a snapshot shared by all monitors.
    sealed class AppMonitor
    {
        static readonly TimeSpan ReportThrottle = TimeSpan.FromSeconds(5);

        public readonly string Name;
        public readonly int IntervalMs;
        public DateTime NextDue = DateTime.UtcNow;
        double lastReportedLoad = -1;
        DateTime lastReport = DateTime.MinValue;

        public AppMonitor(string name, int intervalMs) { Name = name; IntervalMs = intervalMs; }

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

        static Process Largest(List<Process> instances)
        {
            Process best = null;
            if (instances != null) foreach (var p in instances) if (best == null || p.WorkingSet64 > best.WorkingSet64) best = p;
            return best;
        }

        // Trim the largest instance. The snapshot owns the processes, so nothing is disposed here.
        public void Tick(List<Process> instances, uint? load)
        {
            var proc = Largest(instances);
            if (proc == null) ReportThrottled(ConsoleColor.Yellow, $"{Name}: not running");
            else
            {
                int pid = proc.Id;
                try
                {
                    try
                    {
                        if (!NativeMethods.EmptyWorkingSet(proc.Handle))
                            ReportThrottled(ConsoleColor.DarkYellow, $"{Name}: EmptyWorkingSet failed (pid {pid})");
                    }
                    catch (Exception ex)
                    {
                        ReportThrottled(ConsoleColor.Red, $"{Name}: error trimming process (pid {pid}): {ex.Message}");
                    }
                    // Only print when the load changes noticeably or the throttle allows it
                    if (load.HasValue && (Math.Abs(load.Value - lastReportedLoad) > 1.0 || ReportDue()))
                    {
                        Report(ConsoleColor.Cyan, $"{Name.ToUpper()}: Total RAM usage: {(double)load.Value:F2}% (pid {pid})");
                        lastReportedLoad = load.Value;
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ConsoleColor.Red, $"Error handling {Name}: {ex.Message}");
                }
            }
            NextDue = DateTime.UtcNow.AddMilliseconds(IntervalMs);
        }
    }
}
