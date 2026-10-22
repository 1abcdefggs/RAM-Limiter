using System;
using System.Linq;
using System.Threading;

namespace RAMLIMITER
{
    static class InteractiveMenu
    {
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

                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        monitors.Start("discord", 5000);
                        break;
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        monitors.Start("chrome", 5000);
                        break;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        monitors.Start("obs64", 5000);
                        break;
                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        monitors.Start("discord", 5000);
                        monitors.Start("chrome", 5000);
                        break;
                    case ConsoleKey.D5:
                    case ConsoleKey.NumPad5:
                        Console.WriteLine("Enter process names separated by commas (e.g., chrome, obs64, discord):");
                        var line = Console.ReadLine() ?? string.Empty;
                        var names = line.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                        foreach (var n in names)
                            monitors.Start(n, 3000);
                        break;
                    case ConsoleKey.D6:
                    case ConsoleKey.NumPad6:
                        Console.WriteLine("Enter process name to stop monitoring:");
                        var stopName = Console.ReadLine() ?? string.Empty;
                        monitors.Stop(stopName.Trim());
                        break;
                    case ConsoleKey.D7:
                    case ConsoleKey.NumPad7:
                        Console.WriteLine("Active monitors:");
                        foreach (var k in monitors.Names)
                            Console.WriteLine(" - " + k);
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey(true);
                        break;
                    case ConsoleKey.D0:
                    case ConsoleKey.NumPad0:
                        running = false;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid input. Try again.");
                        Thread.Sleep(1000);
                        Console.ResetColor();
                        break;
                }
            }
        }
    }
}
