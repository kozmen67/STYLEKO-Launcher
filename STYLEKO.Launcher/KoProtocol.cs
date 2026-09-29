using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Styleko.Launcher
{
    internal sealed class PatchListResponse
    {
        internal string Server { get; set; }
        internal string RemotePath { get; set; }
        internal List<string> Files { get; set; } = new List<string>();
    }

    internal sealed class KoProtocol : IDisposable
    {
        private readonly TcpClient _client = new TcpClient();
        private NetworkStream _stream;
        private static readonly Encoding KoEncoding = Encoding.GetEncoding(1254);

        internal async Task ConnectAsync(string host, int port, CancellationToken token)
        {
            Task connectTask = _client.ConnectAsync(host, port);
            Task winner = await Task.WhenAny(connectTask, Task.Delay(10000, token)).ConfigureAwait(false);
            if (winner != connectTask)
                throw new TimeoutException("Launcher sunucusuna baglanti zaman asimina ugradi.");
            await connectTask.ConfigureAwait(false);
            _client.NoDelay = true;
            _stream = _client.GetStream();
        }

        internal Task RequestVersionAsync(CancellationToken token)
        {
            return SendFrameAsync(new byte[] { 0x01 }, token);
        }

        internal Task RequestNoticesAsync(CancellationToken token)
        {
            return SendFrameAsync(new byte[] { 0x03 }, token);
        }

        internal Task RequestPatchesAsync(short localVersion, CancellationToken token)
        {
            byte[] payload = new byte[3];
            payload[0] = 0x02;
            payload[1] = (byte)(localVersion & 0xFF);
            payload[2] = (byte)((localVersion >> 8) & 0xFF);
            return SendFrameAsync(payload, token);
        }

        internal async Task<byte[]> ReceiveFrameAsync(CancellationToken token)
        {
            byte[] header = await ReadExactAsync(4, token).ConfigureAwait(false);
            if (header[0] != 0xAA || header[1] != 0x55)
                throw new InvalidDataException("Launcher paket basligi gecersiz.");

            int length = header[2] | (header[3] << 8);
            if (length <= 0 || length > 262144)
                throw new InvalidDataException("Launcher paket uzunlugu gecersiz.");

            byte[] payload = await ReadExactAsync(length, token).ConfigureAwait(false);
            byte[] tail = await ReadExactAsync(2, token).ConfigureAwait(false);
            if (tail[0] != 0x55 || tail[1] != 0xAA)
                throw new InvalidDataException("Launcher paket sonu gecersiz.");
            return payload;
        }

        internal static short ParseVersion(byte[] payload)
        {
            using (var reader = NewReader(payload))
            {
                byte opcode = reader.ReadByte();
                if (opcode != 0x01)
                    throw new InvalidDataException("Version paket opcode gecersiz.");
                return reader.ReadInt16();
            }
        }

        internal static PatchListResponse ParsePatchList(byte[] payload)
        {
            using (var reader = NewReader(payload))
            {
                byte opcode = reader.ReadByte();
                if (opcode != 0x02)
                    throw new InvalidDataException("Patch paket opcode gecersiz.");

                var response = new PatchListResponse
                {
                    Server = ReadKoString(reader),
                    RemotePath = ReadKoString(reader)
                };

                ushort count = reader.ReadUInt16();
                for (int i = 0; i < count; i++)
                    response.Files.Add(ReadKoString(reader));
                return response;
            }
        }

        internal static List<string> ParseNotices(byte[] payload)
        {
            using (var reader = NewReader(payload))
            {
                byte opcode = reader.ReadByte();
                if (opcode != 0x03)
                    throw new InvalidDataException("Notice paket opcode gecersiz.");

                ushort count = reader.ReadUInt16();
                var notices = new List<string>();
                for (int i = 0; i < count; i++)
                    notices.Add(ReadKoString(reader));
                notices.Reverse();
                return notices;
            }
        }

        private async Task SendFrameAsync(byte[] payload, CancellationToken token)
        {
            if (_stream == null)
                throw new InvalidOperationException("Socket bagli degil.");

            byte[] frame = new byte[payload.Length + 6];
            frame[0] = 0xAA;
            frame[1] = 0x55;
            frame[2] = (byte)(payload.Length & 0xFF);
            frame[3] = (byte)((payload.Length >> 8) & 0xFF);
            Buffer.BlockCopy(payload, 0, frame, 4, payload.Length);
            frame[frame.Length - 2] = 0x55;
            frame[frame.Length - 1] = 0xAA;
            await _stream.WriteAsync(frame, 0, frame.Length, token).ConfigureAwait(false);
            await _stream.FlushAsync(token).ConfigureAwait(false);
        }

        private async Task<byte[]> ReadExactAsync(int count, CancellationToken token)
        {
            var buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = await _stream.ReadAsync(buffer, offset, count - offset, token).ConfigureAwait(false);
                if (read <= 0)
                    throw new EndOfStreamException("Launcher sunucusu baglantiyi kapatti.");
                offset += read;
            }
            return buffer;
        }

        private static BinaryReader NewReader(byte[] payload)
        {
            return new BinaryReader(new MemoryStream(payload, false), KoEncoding);
        }

        private static string ReadKoString(BinaryReader reader)
        {
            ushort len = reader.ReadUInt16();
            if (len == 0)
                return string.Empty;
            byte[] bytes = reader.ReadBytes(len);
            if (bytes.Length != len)
                throw new EndOfStreamException("KO string eksik geldi.");
            return KoEncoding.GetString(bytes);
        }

        public void Dispose()
        {
            try { _stream?.Dispose(); } catch { }
            try { _client.Close(); } catch { }
        }
    }
}
