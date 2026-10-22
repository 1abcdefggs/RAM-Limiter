using System;

namespace RAMLIMITER
{
    static class InteractiveMenu
    {
        const int CustomIntervalMs = 3000;

        static readonly string MenuText = string.Join(Environment.NewLine,
            "Limit Discord: 1",
            "Limit Chrome: 2",
            "Limit OBS: 3",
            "Limit Discord & Chrome: 4",
            "Limit Custom: 5",
            "Stop monitoring a process: 6",
            "List active monitors: 7",
            "Exit: 0");
        // Process names for menu keys 1-4
        static readonly string[][] Presets =
        {
            null,
            new[] { "discord" },
            new[] { "chrome" },
            new[] { "obs64" },
            new[] { "discord", "chrome" },
        };

        // 0-9 from the main row or the numpad, -1 for any other key
        static int ToDigit(ConsoleKey key)
        {
            if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9) return key - ConsoleKey.D0;
            if (key >= ConsoleKey.NumPad0 && key <= ConsoleKey.NumPad9) return key - ConsoleKey.NumPad0;
            return -1;
        }

        internal static void Run(MonitorRegistry monitors)
        {
            string error = null;
            while (true)
            {
                Console.Clear();
                Console.WriteLine(MenuText);
                foreach (var notice in monitors.TakeNotices()) Console.WriteLine(notice);
                if (error != null) { Log.Write(ConsoleColor.Red, error); error = null; }
                switch (ToDigit(Console.ReadKey(true).Key))
                {
                    case int d when d >= 1 && d <= 4:
                        foreach (var n in Presets[d]) monitors.Start(n, Config.DefaultIntervalMs);
                        break;
                    case 5:
                        Console.WriteLine("Enter process names separated by commas (e.g., chrome, obs64, discord):");
                        foreach (var n in Config.SplitNames(Console.ReadLine() ?? string.Empty)) monitors.Start(n, CustomIntervalMs);
                        break;
                    case 6:
                        Console.WriteLine("Enter process name to stop monitoring:");
                        monitors.Stop(Console.ReadLine());
                        break;
                    case 7:
                        Console.WriteLine("Active monitors:");
                        foreach (var k in monitors.Names) Console.WriteLine(" - " + k);
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey(true);
                        break;
                    case 0:
                        return;
                    default:
                        error = "Invalid input. Try again.";
                        break;
                }
            }
        }
    }
}
