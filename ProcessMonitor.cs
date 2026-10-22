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

        // false when the wait was cancelled
        static async Task<bool> DelayAsync(int ms, CancellationToken ct)
        {
            try { await Task.Delay(ms, ct); return true; }
            catch (TaskCanceledException) { return false; }
        }

        // Async monitor that trims working set periodically using EmptyWorkingSet.
        // The name is already normalized by MonitorRegistry.
        internal static async Task MonitorProcessAsync(string name, int intervalMs, CancellationToken ct)
        {
            double lastReportedLoad = -1;
            var reportThrottle = TimeSpan.FromSeconds(5);
            var lastReport = DateTime.MinValue;

            bool ReportDue() => DateTime.UtcNow - lastReport > reportThrottle;

            void Report(ConsoleColor color, string message)
            {
                Log.Write(color, message);
                lastReport = DateTime.UtcNow;
            }

            void ReportThrottled(ConsoleColor color, string message)
            {
                if (ReportDue()) Report(color, message);
            }

            while (!ct.IsCancellationRequested)
            {
                int pid = GetLargestProcessIdByName(name);
                if (pid == -1)
                {
                    ReportThrottled(ConsoleColor.Yellow, $"{name}: not running");

                    if (!await DelayAsync(intervalMs, ct)) break;
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
                                ReportThrottled(ConsoleColor.DarkYellow, $"{name}: EmptyWorkingSet failed (pid {pid})");
                        }
                        catch (Exception ex)
                        {
                            ReportThrottled(ConsoleColor.Red, $"{name}: error trimming process (pid {pid}): {ex.Message}");
                        }
                    }

                    // Get system memory usage using GlobalMemoryStatusEx (cheap)
                    var mem = new NativeMethods.MEMORYSTATUSEX();
                    mem.dwLength = (uint)Marshal.SizeOf(typeof(NativeMethods.MEMORYSTATUSEX));
                    if (NativeMethods.GlobalMemoryStatusEx(ref mem))
                    {
                        double percent = mem.dwMemoryLoad; // already percentage used (0-100)

                        // Only print when it changes noticeably or throttle frequency
                        if (Math.Abs(percent - lastReportedLoad) > 1.0 || ReportDue())
                        {
                            Report(ConsoleColor.Cyan, $"{name.ToUpper()}: Total RAM usage: {percent:F2}% (pid {pid})");
                            lastReportedLoad = percent;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ConsoleColor.Red, $"Error handling {name}: {ex.Message}");
                }

                if (!await DelayAsync(intervalMs, ct)) break;
            }

            Log.Write(ConsoleColor.Gray, $"Stopped monitoring {name}");
        }
    }
}
