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

        static readonly uint MemoryStatusSize = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

        // System-wide memory load in percent (0-100), or null when the call fails
        internal static uint? MemoryLoad()
        {
            var mem = new MEMORYSTATUSEX { dwLength = MemoryStatusSize };
            return GlobalMemoryStatusEx(ref mem) ? mem.dwMemoryLoad : (uint?)null;
        }
    }
}
