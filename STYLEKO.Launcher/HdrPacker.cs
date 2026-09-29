using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Styleko.Launcher
{
    internal sealed class HdrPacker
    {
        private readonly string _root;
        private readonly string _pathIni;
        private readonly List<string> _directories = new List<string>();
        private int _repairCount;
        private bool _changed;

        internal HdrPacker(string root)
        {
            _root = root;
            _pathIni = Path.Combine(root, "Path.ini");
            if (!File.Exists(_pathIni))
                return;

            var ini = new IniFile(_pathIni);
            int count = ini.ReadInt("Dir", "count", 0);
            for (int i = 0; i < count; i++)
            {
                string value = ini.ReadString("Dir", i.ToString(), string.Empty).Trim();
                if (value.Length > 0)
                    _directories.Add(value);
            }
        }

        internal void PackAll(Action<string> status)
        {
            if (!File.Exists(_pathIni) || _directories.Count == 0)
                return;

            var ini = new IniFile(_pathIni);
            _repairCount = ini.ReadInt("Version", "Count", 10);
            if (_repairCount < 0 || _repairCount > 10)
                _repairCount = 0;

            foreach (string dir in _directories)
            {
                status?.Invoke("Paketleniyor: " + dir);
                PackDirectory(dir);
            }

            if (_changed)
            {
                _repairCount--;
                if (_repairCount < 0 || _repairCount > 10)
                    _repairCount = 0;
                ini.WriteString("Version", "Count", _repairCount.ToString());
            }
        }

        private void PackDirectory(string name)
        {
            string dir = Path.Combine(_root, name);
            if (!Directory.Exists(dir))
                return;

            TryDelete(Path.Combine(dir, name + "2.hdr"));
            TryDelete(Path.Combine(dir, name + "2.src"));

            if (_repairCount == 0)
                UnpackExisting(name, dir);

            string hdrPath = Path.Combine(dir, name + ".hdr");
            string srcPath = Path.Combine(dir, name + ".src");
            Directory.CreateDirectory(dir);

            using (var hdr = new FileStream(hdrPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            using (var src = new FileStream(srcPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                var entries = ReadHeader(hdr, out int rowCount);
                int originalCount = rowCount;

                var looseFiles = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                    .Where(p => !p.EndsWith(".hdr", StringComparison.OrdinalIgnoreCase) && !p.EndsWith(".src", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (string file in looseFiles)
                {
                    string relative = MakeRelative(dir, file).Replace('/', '\');
                    long offset = src.Length;
                    long length = new FileInfo(file).Length;
                    if (offset > uint.MaxValue || length > uint.MaxValue)
                        throw new InvalidDataException("HDR/SRC 4GB sinirini asti: " + file);

                    src.Position = src.Length;
                    using (var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                        input.CopyTo(src);

                    HeaderEntry existing;
                    if (entries.TryGetValue(relative, out existing))
                    {
                        hdr.Position = existing.OffsetFieldPosition;
                        WriteUInt32(hdr, (uint)offset);
                        WriteUInt32(hdr, (uint)length);
                    }
                    else
                    {
                        hdr.Position = hdr.Length;
                        byte[] nameBytes = Encoding.Default.GetBytes(relative);
                        WriteUInt32(hdr, (uint)nameBytes.Length);
                        hdr.Write(nameBytes, 0, nameBytes.Length);
                        WriteUInt32(hdr, (uint)offset);
                        WriteUInt32(hdr, (uint)length);
                        rowCount++;
                    }

                    inputDelete(file);
                }

                if (rowCount != originalCount)
                {
                    _changed = true;
                    hdr.Position = 0;
                    WriteUInt32(hdr, (uint)rowCount);
                }
            }
        }

        private void UnpackExisting(string name, string dir)
        {
            _repairCount = 10;
            string hdrPath = Path.Combine(dir, name + ".hdr");
            string srcPath = Path.Combine(dir, name + ".src");
            if (!File.Exists(hdrPath) || !File.Exists(srcPath))
                return;

            using (var hdr = new FileStream(hdrPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var src = new FileStream(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (hdr.Length < 4)
                    return;
                int rowCount = checked((int)ReadUInt32(hdr));
                for (int i = 0; i < rowCount; i++)
                {
                    int nameLen = checked((int)ReadUInt32(hdr));
                    if (nameLen < 0 || nameLen > 1024 * 1024)
                        throw new InvalidDataException("HDR dosya adi uzunlugu gecersiz.");
                    byte[] nameBytes = ReadExact(hdr, nameLen);
                    string relative = Encoding.Default.GetString(nameBytes);
                    uint offset = ReadUInt32(hdr);
                    uint length = ReadUInt32(hdr);

                    string target = Path.GetFullPath(Path.Combine(dir, relative));
                    string safeRoot = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (!target.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("HDR guvenli olmayan yol iceriyor: " + relative);

                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    src.Position = offset;
                    byte[] bytes = ReadExact(src, checked((int)length));
                    File.WriteAllBytes(target, bytes);
                }
            }

            TryDelete(hdrPath);
            TryDelete(srcPath);
        }

        private static Dictionary<string, HeaderEntry> ReadHeader(FileStream hdr, out int rowCount)
        {
            var result = new Dictionary<string, HeaderEntry>(StringComparer.OrdinalIgnoreCase);
            if (hdr.Length < 4)
            {
                hdr.Position = 0;
                WriteUInt32(hdr, 0);
                rowCount = 0;
                return result;
            }

            hdr.Position = 0;
            rowCount = checked((int)ReadUInt32(hdr));
            for (int i = 0; i < rowCount; i++)
            {
                int nameLen = checked((int)ReadUInt32(hdr));
                if (nameLen < 0 || nameLen > 1024 * 1024)
                    throw new InvalidDataException("HDR kaydi gecersiz.");
                byte[] nameBytes = ReadExact(hdr, nameLen);
                string name = Encoding.Default.GetString(nameBytes);
                long offsetPosition = hdr.Position;
                ReadUInt32(hdr);
                ReadUInt32(hdr);
                result[name] = new HeaderEntry { OffsetFieldPosition = offsetPosition };
            }
            return result;
        }

        private sealed class HeaderEntry
        {
            internal long OffsetFieldPosition;
        }

        private static string MakeRelative(string root, string file)
        {
            Uri rootUri = new Uri(AppendSlash(Path.GetFullPath(root)));
            Uri fileUri = new Uri(Path.GetFullPath(file));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AppendSlash(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        private static void inputDelete(string path)
        {
            File.Delete(path);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static uint ReadUInt32(Stream stream)
        {
            byte[] b = ReadExact(stream, 4);
            return (uint)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24));
        }

        private static void WriteUInt32(Stream stream, uint value)
        {
            byte[] b = { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
            stream.Write(b, 0, 4);
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            byte[] b = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(b, offset, count - offset);
                if (read <= 0)
                    throw new EndOfStreamException();
                offset += read;
            }
            return b;
        }
    }
}
