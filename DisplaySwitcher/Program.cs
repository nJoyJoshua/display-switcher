using System;
using System.Windows.Forms;

namespace DisplaySwitcher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            // Single instance check
            bool createdNew;
            using var mutex = new System.Threading.Mutex(true, "DisplaySwitcherTrayApp", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("DisplaySwitcher läuft bereits im System Tray.", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.Run(new TrayApplicationContext());
        }
    }
}
