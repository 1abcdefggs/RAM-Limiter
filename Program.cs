using System;

namespace RAMLIMITER
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            ArgParser.ParseAndApplyArgs(args, out var cfg, out var showHelp);
            if (showHelp) { ArgParser.PrintHelp(); return 0; }
            Elevation.ElevatePrivileges(args);
            var monitors = new MonitorRegistry(cfg);
            // Autostart (and tray) run the configured monitors until Enter or the tray's Exit
            if (cfg.AutoStart && cfg.Processes.Length > 0)
            {
                foreach (var p in cfg.Processes) monitors.Start(p, cfg.IntervalMs);
                if (cfg.Tray) TrayHost.Run("RAM Limiter: " + string.Join(", ", cfg.Processes));
                else
                {
                    Console.WriteLine("Autostart monitors running. Press Enter to exit.");
                    Console.ReadLine();
                }
            }
            else InteractiveMenu.Run(monitors);
            monitors.CancelAll();
            return 0;
        }
    }
}
