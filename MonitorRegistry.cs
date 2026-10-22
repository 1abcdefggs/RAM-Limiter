using System;
using System.Collections.Generic;

namespace RAMLIMITER
{
    // Owns the running monitors; the menu and the autostart/tray paths all go through it.
    class MonitorRegistry
    {
        readonly Dictionary<string, AppMonitor> monitors = new Dictionary<string, AppMonitor>(StringComparer.OrdinalIgnoreCase);
        readonly Scheduler scheduler = new Scheduler();
        // Status lines since the menu last drew; the menu shows them under itself.
        readonly List<string> notices = new List<string>();

        readonly TrimMode mode;
        readonly int idleIntervals;
        readonly HashSet<string> favored = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool cancelled;

        public MonitorRegistry(Config cfg)
        {
            mode = cfg.Mode ?? TrimMode.Largest;
            idleIntervals = cfg.IdleIntervals ?? Config.DefaultIdleIntervals;
            foreach (var name in cfg.Favor ?? new string[0]) if (TryNormalize(name, out var key)) favored.Add(key);
        }

        public IEnumerable<string> Names => monitors.Keys;

        // false for a blank name; otherwise the trimmed, lower-cased key
        static bool TryNormalize(string name, out string key)
        {
            key = string.IsNullOrWhiteSpace(name) ? null : name.Trim().ToLower();
            return key != null;
        }

        void Notify(string message)
        {
            Console.WriteLine(message);
            notices.Add(message);
        }

        // Returns and clears the pending status lines.
        public string[] TakeNotices()
        {
            var taken = notices.ToArray();
            notices.Clear();
            return taken;
        }

        public void Start(string name, int intervalMs)
        {
            if (!TryNormalize(name, out name)) return;
            if (monitors.ContainsKey(name)) { Notify($"Already monitoring {name}"); return; }
            if (favored.Contains(name)) { Notify($"{name} is favored and is not trimmed"); return; }
            var monitor = new AppMonitor(name, intervalMs, mode, idleIntervals);
            monitors[name] = monitor;
            scheduler.Add(monitor);
            Notify($"Started monitoring {name}");
        }

        public void Stop(string name)
        {
            if (!TryNormalize(name, out name)) return;
            if (!monitors.TryGetValue(name, out var monitor)) { Notify($"Not monitoring {name}"); return; }
            scheduler.Remove(monitor);
            monitor.Restore();
            monitors.Remove(name);
            Notify($"Stopped monitoring {name}");
        }

        // Stops every monitor and restores memory priorities. Safe to call twice: the normal exit
        // path and the console control handler (Ctrl+C, window closed) may both call it.
        public void CancelAll()
        {
            lock (monitors)
            {
                if (cancelled) return;
                cancelled = true;
                scheduler.Stop();
                foreach (var monitor in monitors.Values)
                {
                    monitor.Restore();
                    Log.Write(ConsoleColor.Gray, $"Stopped monitoring {monitor.Name}");
                }
            }
        }
    }
}
