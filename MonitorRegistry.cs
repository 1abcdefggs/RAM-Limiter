using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RAMLIMITER
{
    // Owns the running monitors; the menu and the autostart/tray paths all go through it.
    class MonitorRegistry
    {
        readonly Dictionary<string, CancellationTokenSource> monitors =
            new Dictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);
        // Status lines since the menu last drew; the menu shows them under itself.
        readonly List<string> notices = new List<string>();

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
            var cts = new CancellationTokenSource();
            monitors[name] = cts;
            Task.Run(() => ProcessMonitor.MonitorProcessAsync(name, intervalMs, cts.Token));
            Notify($"Started monitoring {name}");
        }

        public void Stop(string name)
        {
            if (!TryNormalize(name, out name)) return;
            if (!monitors.TryGetValue(name, out var cts)) { Notify($"Not monitoring {name}"); return; }
            cts.Cancel();
            monitors.Remove(name);
            Notify($"Stopped monitoring {name}");
        }

        public void CancelAll()
        {
            foreach (var cts in monitors.Values) cts.Cancel();
        }
    }
}
