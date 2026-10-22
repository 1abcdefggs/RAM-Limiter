using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RAMLIMITER
{
    // Hosts the notification-area icon and keeps the process alive until Exit is chosen.
    static class TrayHost
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static void SetConsoleVisible(bool visible)
        {
            var hwnd = GetConsoleWindow();
            if (hwnd != IntPtr.Zero)
                ShowWindow(hwnd, visible ? SW_SHOW : SW_HIDE);
        }

        // Blocks until the user selects Exit from the tray menu.
        public static void Run(string tooltip)
        {
            Application.EnableVisualStyles();
            using (var icon = Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location))
            using (var tray = new NotifyIcon())
            using (var menu = new ContextMenuStrip())
            {
                bool consoleVisible = false;
                SetConsoleVisible(false);
                var toggle = new ToolStripMenuItem("Show console");
                toggle.Click += (s, e) =>
                {
                    consoleVisible = !consoleVisible;
                    SetConsoleVisible(consoleVisible);
                    toggle.Text = consoleVisible ? "Hide console" : "Show console";
                };
                var exit = new ToolStripMenuItem("Exit");
                exit.Click += (s, e) => Application.ExitThread();
                menu.Items.Add(toggle);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(exit);
                tray.Icon = icon;
                tray.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
                tray.ContextMenuStrip = menu;
                tray.Visible = true;
                Application.Run();
                tray.Visible = false;
                SetConsoleVisible(true);
            }
        }
    }
}
