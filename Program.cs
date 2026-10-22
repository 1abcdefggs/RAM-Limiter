using System;
using System.Threading;

namespace RAMLIMITER
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            ArgParser.ParseAndApplyArgs(args, out var cfg, out var showHelp);
            if (showHelp)
            {
                ArgParser.PrintHelp();
                return 0;
            }

            Elevation.ElevatePrivileges(args);

            var monitors = new MonitorRegistry();

            // If autostart is requested, start monitors from config and run until keypress
            if (cfg.AutoStart && cfg.Processes.Length > 0)
            {
                foreach (var p in cfg.Processes)
                    monitors.Start(p, cfg.IntervalMs > 0 ? cfg.IntervalMs : Config.DefaultIntervalMs);

                if (cfg.Tray)
                {
                    TrayHost.Run("RAM Limiter: " + string.Join(", ", cfg.Processes));
                }
                else
                {
                    Console.WriteLine("Autostart monitors running. Press Enter to exit.");
                    Console.ReadLine();
                }

                monitors.CancelAll();
                return 0;
            }

            InteractiveMenu.Run(monitors);

            // Shutdown monitors
            monitors.CancelAll();

            // allow tasks to finish briefly
            Thread.Sleep(500);

            return 0;
        }
    }
}
