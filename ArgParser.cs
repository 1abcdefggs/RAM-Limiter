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

        const int MinIntervalMs = 100;

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
                bool hasValue = i + 1 < args.Length;
                if (Is(a, "--help") || Is(a, "-h")) { showHelp = true; return; }
                else if (Is(a, "--config") && hasValue) configPath = args[++i];
                else if (Is(a, "--processes") && hasValue) cfg.Processes = Config.SplitNames(args[++i]);
                else if (Is(a, "--interval") && hasValue)
                {
                    if (int.TryParse(args[++i], out var ms)) { cfg.IntervalMs = Math.Max(MinIntervalMs, ms); intervalSet = true; }
                }
                else if (Is(a, "--autostart")) cfg.AutoStart = true;
                else if (Is(a, "--tray")) cfg.Tray = cfg.AutoStart = true;
            }
            // file values apply only where the CLI gave none
            var fileCfg = ConfigLoader.LoadConfig(configPath);
            if (fileCfg != null)
            {
                if ((cfg.Processes == null || cfg.Processes.Length == 0) && fileCfg.Processes != null) cfg.Processes = fileCfg.Processes;
                if (!intervalSet && fileCfg.IntervalMs > 0) cfg.IntervalMs = Math.Max(MinIntervalMs, fileCfg.IntervalMs);
                cfg.AutoStart |= fileCfg.AutoStart;
            }
            cfg.Processes = cfg.Processes ?? new string[0];
        }
    }
}
