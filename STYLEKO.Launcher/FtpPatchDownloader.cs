using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Styleko.Launcher
{
    internal static class FtpPatchDownloader
    {
        internal static async Task DownloadAsync(string server, string remotePath, string fileName, string destination, Action<long, long> progress, CancellationToken token)
        {
            Uri uri = BuildUri(server, remotePath, fileName);
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.Credentials = new NetworkCredential("anonymous", "anonymous@stylekopvp.com");
            request.Timeout = 15000;
            request.ReadWriteTimeout = 30000;

            using (var response = (FtpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
            using (var input = response.GetResponseStream())
            using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, true))
            {
                long total = response.ContentLength;
                long done = 0;
                byte[] buffer = new byte[64 * 1024];
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    int read = await input.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    if (read <= 0)
                        break;

                    await output.WriteAsync(buffer, 0, read, token).ConfigureAwait(false);
                    done += read;
                    progress?.Invoke(done, total);
                }
            }
        }

        private static Uri BuildUri(string server, string remotePath, string fileName)
        {
            string host = (server ?? string.Empty).Trim();
            if (!host.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                host = "ftp://" + host;

            host = host.TrimEnd('/');
            string path = (remotePath ?? string.Empty).Replace((char)92, '/').Trim('/');
            string file = (fileName ?? string.Empty).Replace((char)92, '/').TrimStart('/');
            string url = host + "/" + (path.Length > 0 ? path + "/" : string.Empty) + file;
            return new Uri(url);
        }
    }
}
