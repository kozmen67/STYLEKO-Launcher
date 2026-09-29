using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Styleko.Launcher
{
    internal static class NativeMethods
    {
        internal const int WM_NCLBUTTONDOWN = 0x00A1;
        internal const int HTCAPTION = 2;
        internal const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("shell32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr ShellExecute(IntPtr hwnd, string operation, string file, string parameters, string directory, int showCmd);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        internal static extern int GetPrivateProfileInt(string section, string key, int defaultValue, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        internal static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder buffer, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WritePrivateProfileString(string section, string key, string value, string filePath);
    }
}
