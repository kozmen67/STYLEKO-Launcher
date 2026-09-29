using System.Text;

namespace Styleko.Launcher
{
    internal sealed class IniFile
    {
        private readonly string _path;

        internal IniFile(string path)
        {
            _path = path;
        }

        internal int ReadInt(string section, string key, int defaultValue)
        {
            return NativeMethods.GetPrivateProfileInt(section, key, defaultValue, _path);
        }

        internal string ReadString(string section, string key, string defaultValue)
        {
            var sb = new StringBuilder(4096);
            NativeMethods.GetPrivateProfileString(section, key, defaultValue ?? string.Empty, sb, sb.Capacity, _path);
            return sb.ToString();
        }

        internal void WriteString(string section, string key, string value)
        {
            NativeMethods.WritePrivateProfileString(section, key, value ?? string.Empty, _path);
        }
    }
}
