using System;

namespace RAMLIMITER
{
    static class Program
    {
        // Kept in a field so the delegate is not collected while Windows still holds it
        static NativeMethods.ConsoleCtrlHandler ctrlHandler;

        [STAThread]
        static int Main(string[] args)
        {
            ArgParser.ParseAndApplyArgs(args, out var cfg, out var showHelp);
            if (showHelp) { ArgParser.PrintHelp(); return 0; }
            Elevation.ElevatePrivileges(args);
            var monitors = new MonitorRegistry(cfg);
            // Restore memory priorities when the console is closed or Ctrl+C is pressed;
            // returning false lets Windows end the process as usual afterwards.
            ctrlHandler = _ => { monitors.CancelAll(); return false; };
            NativeMethods.SetConsoleCtrlHandler(ctrlHandler, true);
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
