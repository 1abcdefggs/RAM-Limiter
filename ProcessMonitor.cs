using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RAMLIMITER
{
    static class ProcessMonitor
    {
        // Choose the process instance with the largest working set (safely)
        static int GetLargestProcessIdByName(string name)
        {
            try
            {
                var procs = Process.GetProcessesByName(name);
                int bestId = -1;
                long bestWs = -1;
                foreach (var p in procs)
                {
                    try
                    {
                        if (p.WorkingSet64 > bestWs)
                        {
                            bestWs = p.WorkingSet64;
                            bestId = p.Id;
                        }
                    }
                    catch
                    {
                        // accessing WorkingSet64 can throw for protected processes; ignore
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }
                return bestId;
            }
            catch
            {
                return -1;
            }
        }

        // Async monitor that trims working set periodically using EmptyWorkingSet
        internal static async Task MonitorProcessAsync(string name, int intervalMs, CancellationToken ct)
        {
            name = name?.Trim().ToLower();
            if (string.IsNullOrEmpty(name))
                return;

            double lastReportedLoad = -1;
            var reportThrottle = TimeSpan.FromSeconds(5);
            var lastReport = DateTime.MinValue;

            while (!ct.IsCancellationRequested)
            {
                int pid = GetLargestProcessIdByName(name);
                if (pid == -1)
                {
                    if (DateTime.UtcNow - lastReport > reportThrottle)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"{name}: not running");
                        Console.ResetColor();
                        lastReport = DateTime.UtcNow;
                    }

                    try { await Task.Delay(intervalMs, ct); } catch (TaskCanceledException) { break; }
                    continue;
                }

                try
                {
                    using (var proc = Process.GetProcessById(pid))
                    {
                        // Attempt to trim the working set
                        try
                        {
                            if (!NativeMethods.EmptyWorkingSet(proc.Handle))
                            {
                                // fail silently but log occasionally
                                if (DateTime.UtcNow - lastReport > reportThrottle)
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    Console.WriteLine($"{name}: EmptyWorkingSet failed (pid {pid})");
                                    Console.ResetColor();
                                    lastReport = DateTime.UtcNow;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (DateTime.UtcNow - lastReport > reportThrottle)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"{name}: error trimming process (pid {pid}): {ex.Message}");
                                Console.ResetColor();
                                lastReport = DateTime.UtcNow;
                            }
                        }
                    }

                    // Get system memory usage using GlobalMemoryStatusEx (cheap)
                    var mem = new NativeMethods.MEMORYSTATUSEX();
                    mem.dwLength = (uint)Marshal.SizeOf(typeof(NativeMethods.MEMORYSTATUSEX));
                    if (NativeMethods.GlobalMemoryStatusEx(ref mem))
                    {
                        double percent = mem.dwMemoryLoad; // already percentage used (0-100)

                        // Only print when it changes noticeably or throttle frequency
                        if (Math.Abs(percent - lastReportedLoad) > 1.0 || DateTime.UtcNow - lastReport > reportThrottle)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"{name.ToUpper()}: Total RAM usage: {percent:F2}% (pid {pid})");
                            Console.ResetColor();
                            lastReportedLoad = percent;
                            lastReport = DateTime.UtcNow;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error handling {name}: {ex.Message}");
                    Console.ResetColor();
                }

                try { await Task.Delay(intervalMs, ct); } catch (TaskCanceledException) { break; }
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Stopped monitoring {name}");
            Console.ResetColor();
        }
    }
}
