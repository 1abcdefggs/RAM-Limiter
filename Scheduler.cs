using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace RAMLIMITER
{
    // One background thread serves every monitor. Each wake-up takes a single process snapshot
    // and runs all monitors that are due, instead of one full process scan per monitor per tick.
    sealed class Scheduler
    {
        readonly object gate = new object();
        readonly List<AppMonitor> monitors = new List<AppMonitor>();
        readonly AutoResetEvent changed = new AutoResetEvent(false);
        Thread thread;
        volatile bool stopping;

        public void Add(AppMonitor monitor)
        {
            lock (gate)
            {
                monitors.Add(monitor);
                if (thread == null)
                {
                    thread = new Thread(Loop) { IsBackground = true, Name = "RAM Limiter scheduler" };
                    thread.Start();
                }
            }
            changed.Set();
        }

        public void Remove(AppMonitor monitor)
        {
            lock (gate) monitors.Remove(monitor);
            changed.Set();
        }

        public void Stop()
        {
            stopping = true;
            changed.Set();
        }

        void Loop()
        {
            while (!stopping)
            {
                AppMonitor[] due;
                lock (gate) due = monitors.Where(m => m.NextDue <= DateTime.UtcNow).ToArray();
                if (due.Length > 0) RunTick(due);
                int waitMs;
                lock (gate)
                    waitMs = monitors.Count == 0 ? Timeout.Infinite
                        : (int)Math.Max(0, Math.Ceiling((monitors.Min(m => m.NextDue) - DateTime.UtcNow).TotalMilliseconds));
                changed.WaitOne(waitMs);
            }
        }

        // One process scan shared by every due monitor; all snapshot processes are disposed afterwards.
        static void RunTick(AppMonitor[] due)
        {
            Process[] all;
            try { all = Process.GetProcesses(); }
            catch { all = new Process[0]; }
            try
            {
                var byName = new Dictionary<string, List<Process>>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in all)
                {
                    if (!byName.TryGetValue(p.ProcessName, out var list)) byName[p.ProcessName] = list = new List<Process>();
                    list.Add(p);
                }
                var load = NativeMethods.MemoryLoad();
                foreach (var m in due) m.Tick(byName.TryGetValue(m.Name, out var instances) ? instances : null, load);
            }
            finally
            {
                foreach (var p in all) p.Dispose();
            }
        }
    }
}
