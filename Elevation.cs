using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Threading;

namespace RAMLIMITER
{
    static class Elevation
    {
        static bool IsAdmin()
        {
            using (var id = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static void ElevatePrivileges(string[] args)
        {
            if (IsAdmin()) return;
            Console.Clear();
            Console.WriteLine("RAM Limiter does not currently have admin. privileges.\nWould you like to run as admin? (y/n)");
            if (Console.ReadKey(true).Key != ConsoleKey.Y) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Assembly.GetEntryAssembly().Location,
                    Arguments = string.Join(" ", args),
                    Verb = "runas"
                });
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not elevate program.\n" + ex.Message);
                Thread.Sleep(2000);
            }
        }
    }
}
