using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Styleko.Launcher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            bool created;
            using (var mutex = new Mutex(true, @"Local\STYLEKO_LAUNCHER_SINGLE_INSTANCE", out created))
            {
                if (!created)
                {
                    MessageBox.Show("STYLEKO Launcher zaten calisiyor.", "STYLEKO Launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.Run(new MainForm(root));
            }
        }
    }
}
