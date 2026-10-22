using System;
using System.Runtime.InteropServices;

namespace RAMLIMITER
{
    static class NativeMethods
    {
        // Use EmptyWorkingSet which is the documented way to trim another processes working set
        [DllImport("psapi.dll")]
        internal static extern bool EmptyWorkingSet(IntPtr hProcess);

        // GlobalMemoryStatusEx to get system memory stats (lightweight)
        [DllImport("kernel32.dll")]
        internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        // Id of the process that owns the foreground window, or 0
        internal static int ForegroundProcessId()
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out var pid);
            return (int)pid;
        }

        // Memory priority (Windows 8+): pages of a low-priority process are reused first
        internal const uint MemoryPriorityLow = 2;
        const int ProcessMemoryPriority = 0;

        [DllImport("kernel32.dll")]
        static extern bool SetProcessInformation(IntPtr hProcess, int infoClass, ref uint info, uint size);

        [DllImport("kernel32.dll")]
        static extern bool GetProcessInformation(IntPtr hProcess, int infoClass, ref uint info, uint size);

        internal static bool SetMemoryPriority(IntPtr hProcess, uint priority) =>
            SetProcessInformation(hProcess, ProcessMemoryPriority, ref priority, sizeof(uint));

        internal static uint? GetMemoryPriority(IntPtr hProcess)
        {
            uint priority = 0;
            return GetProcessInformation(hProcess, ProcessMemoryPriority, ref priority, sizeof(uint)) ? priority : (uint?)null;
        }

        public delegate bool ConsoleCtrlHandler(int ctrlType);

        // Runs the handler on Ctrl+C, Ctrl+Break, console close, logoff and shutdown
        [DllImport("kernel32.dll")]
        internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);

        static readonly uint MemoryStatusSize = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

        // System-wide memory load in percent (0-100), or null when the call fails
        internal static uint? MemoryLoad()
        {
            var mem = new MEMORYSTATUSEX { dwLength = MemoryStatusSize };
            return GlobalMemoryStatusEx(ref mem) ? mem.dwMemoryLoad : (uint?)null;
        }
    }
}
