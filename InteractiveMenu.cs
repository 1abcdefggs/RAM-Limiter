using System;
using System.Threading;

namespace RAMLIMITER
{
    static class InteractiveMenu
    {
        const int CustomIntervalMs = 3000;

        // 0-9 from the main row or the numpad, -1 for any other key
        static int ToDigit(ConsoleKey key)
        {
            if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9) return key - ConsoleKey.D0;
            if (key >= ConsoleKey.NumPad0 && key <= ConsoleKey.NumPad9) return key - ConsoleKey.NumPad0;
            return -1;
        }

        internal static void Run(MonitorRegistry monitors)
        {
            bool running = true;
            while (running)
            {
                // existing interactive menu (keeps behavior)
                Console.Clear();
                Console.WriteLine("Limit Discord: 1");
                Console.WriteLine("Limit Chrome: 2");
                Console.WriteLine("Limit OBS: 3");
                Console.WriteLine("Limit Discord & Chrome: 4");
                Console.WriteLine("Limit Custom: 5");
                Console.WriteLine("Stop monitoring a process: 6");
                Console.WriteLine("List active monitors: 7");
                Console.WriteLine("Exit: 0");

                switch (ToDigit(Console.ReadKey(true).Key))
                {
                    case 1:
                        monitors.Start("discord", Config.DefaultIntervalMs);
                        break;
                    case 2:
                        monitors.Start("chrome", Config.DefaultIntervalMs);
                        break;
                    case 3:
                        monitors.Start("obs64", Config.DefaultIntervalMs);
                        break;
                    case 4:
                        monitors.Start("discord", Config.DefaultIntervalMs);
                        monitors.Start("chrome", Config.DefaultIntervalMs);
                        break;
                    case 5:
                        Console.WriteLine("Enter process names separated by commas (e.g., chrome, obs64, discord):");
                        foreach (var n in Config.SplitNames(Console.ReadLine() ?? string.Empty))
                            monitors.Start(n, CustomIntervalMs);
                        break;
                    case 6:
                        Console.WriteLine("Enter process name to stop monitoring:");
                        monitors.Stop(Console.ReadLine() ?? string.Empty);
                        break;
                    case 7:
                        Console.WriteLine("Active monitors:");
                        foreach (var k in monitors.Names)
                            Console.WriteLine(" - " + k);
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey(true);
                        break;
                    case 0:
                        running = false;
                        break;
                    default:
                        Log.Write(ConsoleColor.Red, "Invalid input. Try again.");
                        Thread.Sleep(1000);
                        break;
                }
            }
        }
    }
}
