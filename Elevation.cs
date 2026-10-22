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
            var id = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static void ElevatePrivileges(string[] args)
        {
            if (IsAdmin())
                return;

            var arguments = args == null || args.Length == 0 ? string.Empty : string.Join(" ", args);

            var psi = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Assembly.GetEntryAssembly().Location,
                Arguments = arguments,
                Verb = "runas"
            };

            Console.Clear();
            Console.WriteLine("RAM Limiter does not currently have admin. privileges.\nWould you like to run as admin? (y/n)");
            if (Console.ReadKey(true).Key == ConsoleKey.Y)
            {
                try
                {
                    Process.Start(psi);
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
}
