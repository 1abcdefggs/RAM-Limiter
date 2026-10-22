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

        public MonitorRegistry(Config cfg)
        {
            mode = cfg.Mode ?? TrimMode.Largest;
            idleIntervals = cfg.IdleIntervals ?? Config.DefaultIdleIntervals;
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
            monitors.Remove(name);
            Notify($"Stopped monitoring {name}");
        }

        public void CancelAll()
        {
            scheduler.Stop();
            foreach (var name in monitors.Keys) Log.Write(ConsoleColor.Gray, $"Stopped monitoring {name}");
        }
    }
}
