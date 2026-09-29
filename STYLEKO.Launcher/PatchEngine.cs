using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Styleko.Launcher
{
    internal sealed class PatchEngine
    {
        private readonly string _root;
        private readonly IProgress<LauncherSnapshot> _progress;
        private readonly LauncherSnapshot _state = new LauncherSnapshot();
        private readonly string _serverIni;

        internal PatchEngine(string root, IProgress<LauncherSnapshot> progress)
        {
            _root = root;
            _progress = progress;
            _serverIni = Path.Combine(root, "Server.ini");
            _state.Notices = NoticeLoader.Load(root);
        }

        internal async Task RunAsync(CancellationToken token)
        {
            Report(0, false, "Surum kontrol ediliyor...");

            if (!File.Exists(_serverIni))
            {
                Report(0, false, "Server.ini bulunamadi.");
                return;
            }

            var ini = new IniFile(_serverIni);
            string host = ini.ReadString("Server", "IP0", string.Empty).Trim();
            short localVersion = checked((short)ini.ReadInt("Version", "Files", 1));
            if (host.Length == 0)
            {
                Report(0, false, "Launcher sunucu adresi bulunamadi.");
                return;
            }

            try
            {
                using (var protocol = new KoProtocol())
                {
                    Report(1, false, "Sunucu baglantisi kuruluyor...");
                    await protocol.ConnectAsync(host, 15100, token).ConfigureAwait(false);

                    await protocol.RequestVersionAsync(token).ConfigureAwait(false);
                    byte[] versionPacket = await ReceiveOpcodeAsync(protocol, 0x01, token).ConfigureAwait(false);
                    short remoteVersion = KoProtocol.ParseVersion(versionPacket);

                    if (localVersion > remoteVersion)
                    {
                        Report(0, false, "Version Gecersiz.");
                        return;
                    }

                    await protocol.RequestNoticesAsync(token).ConfigureAwait(false);
                    await protocol.RequestPatchesAsync(localVersion, token).ConfigureAwait(false);

                    PatchListResponse patchList = null;
                    DateTime deadline = DateTime.UtcNow.AddSeconds(12);
                    while (patchList == null && DateTime.UtcNow < deadline)
                    {
                        byte[] packet = await ReceiveWithTimeout(protocol, 5000, token).ConfigureAwait(false);
                        if (packet == null || packet.Length == 0)
                            continue;
                        if (packet[0] == 0x03)
                        {
                            List<string> serverNotices = KoProtocol.ParseNotices(packet);
                            if (serverNotices.Count > 0)
                            {
                                _state.Notices = serverNotices.Take(5).ToList();
                                Report(_state.Percent, false, _state.Status);
                            }
                        }
                        else if (packet[0] == 0x02)
                        {
                            patchList = KoProtocol.ParsePatchList(packet);
                        }
                    }

                    if (patchList == null)
                        throw new TimeoutException("Patch listesi alinamadi.");

                    int fileCount = patchList.Files.Count;
                    for (int i = 0; i < fileCount; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        if (Process.GetProcessesByName("KnightOnLine").Length > 0)
                        {
                            Report(_state.Percent, false, "KnightOnLine.exe kapatip tekrar deneyiniz.");
                            return;
                        }

                        string file = patchList.Files[i];
                        string localFile = Path.Combine(_root, Path.GetFileName(file));
                        int fileIndex = i;
                        Report(BasePercent(fileIndex, fileCount), false, "Indiriliyor: " + file);

                        await FtpPatchDownloader.DownloadAsync(
                            patchList.Server,
                            patchList.RemotePath,
                            file,
                            localFile,
                            (done, total) =>
                            {
                                int percent;
                                if (total > 0)
                                {
                                    double inFile = Math.Max(0.0, Math.Min(1.0, (double)done / total));
                                    percent = (int)Math.Round(((fileIndex + inFile) / Math.Max(1, fileCount)) * 90.0);
                                }
                                else
                                {
                                    percent = BasePercent(fileIndex, fileCount);
                                }
                                double dmb = done / 1024d / 1024d;
                                double tmb = total > 0 ? total / 1024d / 1024d : 0d;
                                string status = total > 0
                                    ? string.Format("Indiriliyor {0}: {1:0.00}/{2:0.00} MB.", file, dmb, tmb)
                                    : string.Format("Indiriliyor {0}: {1:0.00} MB.", file, dmb);
                                Report(percent, false, status);
                            }, token).ConfigureAwait(false);

                        Report(BasePercent(fileIndex, fileCount), false, "Cikartiliyor: " + file);
                        ZipExtractor.ExtractOverwrite(localFile, _root, entry => Report(_state.Percent, false, "Cikartiliyor: " + entry));
                        File.Delete(localFile);

                        int parsedVersion;
                        if (TryParsePatchVersion(file, out parsedVersion))
                        {
                            localVersion = checked((short)parsedVersion);
                            ini.WriteString("Version", "Files", parsedVersion.ToString());
                        }
                    }
                }

                Report(92, false, "STYLEKO dosyalari paketleniyor...");
                var packer = new HdrPacker(_root);
                packer.PackAll(s => Report(95, false, s));
                Report(100, true, "STYLEKO guncelleme islemi tamamlandi.");
            }
            catch (OperationCanceledException)
            {
                Report(_state.Percent, false, "Islem iptal edildi.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Report(_state.Percent, false, "Baglanti / guncelleme hatasi. Lutfen tekrar deneyiniz.");
            }
        }

        private static int BasePercent(int index, int count)
        {
            if (count <= 0) return 90;
            return Math.Max(1, Math.Min(89, (int)Math.Floor((double)index / count * 90.0)));
        }

        private static bool TryParsePatchVersion(string file, out int version)
        {
            string stem = Path.GetFileNameWithoutExtension(file) ?? string.Empty;
            int length = 0;
            while (length < stem.Length && char.IsDigit(stem[length])) length++;
            return int.TryParse(stem.Substring(0, length), out version);
        }

        private async Task<byte[]> ReceiveOpcodeAsync(KoProtocol protocol, byte opcode, CancellationToken token)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(12);
            while (DateTime.UtcNow < deadline)
            {
                byte[] packet = await ReceiveWithTimeout(protocol, 5000, token).ConfigureAwait(false);
                if (packet != null && packet.Length > 0 && packet[0] == opcode)
                    return packet;
            }
            throw new TimeoutException("Beklenen launcher paketi gelmedi.");
        }

        private static async Task<byte[]> ReceiveWithTimeout(KoProtocol protocol, int milliseconds, CancellationToken token)
        {
            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                Task<byte[]> receive = protocol.ReceiveFrameAsync(timeoutCts.Token);
                Task delay = Task.Delay(milliseconds, token);
                Task winner = await Task.WhenAny(receive, delay).ConfigureAwait(false);
                if (winner == receive)
                {
                    timeoutCts.Cancel();
                    return await receive.ConfigureAwait(false);
                }
                timeoutCts.Cancel();
                return null;
            }
        }

        private void Report(int percent, bool ready, string status)
        {
            _state.Percent = Math.Max(0, Math.Min(100, percent));
            _state.Ready = ready;
            _state.Status = status ?? string.Empty;
            _progress.Report(_state.Clone());
        }
    }
}
