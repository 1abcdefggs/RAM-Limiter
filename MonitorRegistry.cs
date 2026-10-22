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

        public void Start(string name, int intervalMs)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            name = name.Trim().ToLower();
            if (monitors.ContainsKey(name))
            {
                Console.WriteLine($"Already monitoring {name}");
                Thread.Sleep(800);
                return;
            }

            var cts = new CancellationTokenSource();
            monitors[name] = cts;
            Task.Run(() => ProcessMonitor.MonitorProcessAsync(name, intervalMs, cts.Token));
            Console.WriteLine($"Started monitoring {name}");
            Thread.Sleep(500);
        }

        public void Stop(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            name = name.Trim().ToLower();
            if (!monitors.TryGetValue(name, out var cts))
            {
                Console.WriteLine($"Not monitoring {name}");
                Thread.Sleep(800);
                return;
            }

            try
            {
                cts.Cancel();
            }
            catch { }
            monitors.Remove(name);
            Console.WriteLine($"Stopped monitoring {name}");
            Thread.Sleep(500);
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
