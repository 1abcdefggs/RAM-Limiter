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

        public IEnumerable<string> Names => monitors.Keys;

        // false for a blank name; otherwise the trimmed, lower-cased key
        static bool TryNormalize(string name, out string key)
        {
            key = string.IsNullOrWhiteSpace(name) ? null : name.Trim().ToLower();
            return key != null;
        }

        // print a status line and pause so the menu does not clear it at once
        static void Notify(string message, int pauseMs)
        {
            Console.WriteLine(message);
            Thread.Sleep(pauseMs);
        }

        public void Start(string name, int intervalMs)
        {
            if (!TryNormalize(name, out name))
                return;

            if (monitors.ContainsKey(name))
            {
                Notify($"Already monitoring {name}", 800);
                return;
            }

            var cts = new CancellationTokenSource();
            monitors[name] = cts;
            Task.Run(() => ProcessMonitor.MonitorProcessAsync(name, intervalMs, cts.Token));
            Notify($"Started monitoring {name}", 500);
        }

        public void Stop(string name)
        {
            if (!TryNormalize(name, out name))
                return;

            if (!monitors.TryGetValue(name, out var cts))
            {
                Notify($"Not monitoring {name}", 800);
                return;
            }

            try
            {
                cts.Cancel();
            }
            catch { }
            monitors.Remove(name);
            Notify($"Stopped monitoring {name}", 500);
        }

        public void CancelAll()
        {
            foreach (var cts in monitors.Values)
            {
                try { cts.Cancel(); } catch { }
            }
        }
    }
}
