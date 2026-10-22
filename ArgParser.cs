using System;
using System.IO;

namespace RAMLIMITER
{
    static class ArgParser
    {
        internal static void PrintHelp()
        {
            Console.WriteLine("Usage: RAMLimiter.exe [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --processes p1,p2    Comma-separated process names to monitor");
            Console.WriteLine("  --interval <ms>      Interval in milliseconds between trims (default 5000)");
            Console.WriteLine("  --autostart           Start monitors immediately and run headless");
            Console.WriteLine("  --tray                Start monitors and live in the system tray (implies --autostart)");
            Console.WriteLine("  --config <path>     Path to JSON config file (defaults to ./config.json)");
            Console.WriteLine("  --help                Show this help");
        }

        static bool Is(string arg, string flag) => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase);

        internal static void ParseAndApplyArgs(string[] args, out Config cfg, out bool showHelp)
        {
            cfg = new Config();
            showHelp = false;

            string configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            bool intervalSet = false;

            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (Is(a, "--help") || Is(a, "-h"))
                {
                    showHelp = true; return;
                }
                if (Is(a, "--config") && i + 1 < args.Length)
                {
                    configPath = args[++i];
                    continue;
                }
                if (Is(a, "--processes") && i + 1 < args.Length)
                {
                    cfg.Processes = Config.SplitNames(args[++i]);
                    continue;
                }
                if (Is(a, "--interval") && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out var ms))
                    {
                        cfg.IntervalMs = Math.Max(100, ms);
                        intervalSet = true;
                    }
                    continue;
                }
                if (Is(a, "--autostart"))
                {
                    cfg.AutoStart = true; continue;
                }
                if (Is(a, "--tray"))
                {
                    cfg.Tray = true; cfg.AutoStart = true; continue;
                }
            }

            var fileCfg = ConfigLoader.LoadConfig(configPath);
            if (fileCfg != null)
            {
                // apply file values only when not provided by CLI (CLI already wrote into cfg.Processes/IntervalMs/AutoStart)
                if ((cfg.Processes == null || cfg.Processes.Length == 0) && fileCfg.Processes != null)
                    cfg.Processes = fileCfg.Processes;
                if (!intervalSet && fileCfg.IntervalMs > 0)
                    cfg.IntervalMs = Math.Max(100, fileCfg.IntervalMs);
                if (!cfg.AutoStart && fileCfg.AutoStart)
                    cfg.AutoStart = fileCfg.AutoStart;
            }

            // default to empty processes array
            if (cfg.Processes == null) cfg.Processes = new string[0];
        }
    }
}
