using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Styleko.Launcher
{
    internal sealed class MainForm : Form
    {
        private readonly string _root;
        private readonly WebView2 _web = new WebView2();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();
        private LauncherSnapshot _snapshot = new LauncherSnapshot();
        private bool _webReady;
        private bool _gameStartRequested;

        internal MainForm(string root)
        {
            _root = root;
            Text = "STYLEKO Launcher";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1120, 585);
            MinimumSize = MaximumSize = Size;
            MaximizeBox = false;
            MinimizeBox = false;

            _web.Dock = DockStyle.Fill;
            Controls.Add(_web);

            Shown += async (s, e) => await InitializeAsync();
            FormClosed += (s, e) => _cts.Cancel();
        }

        private async Task InitializeAsync()
        {
            string html = Path.Combine(_root, "StyleKO", "LauncherWeb", "index.html");
            if (!File.Exists(html))
            {
                MessageBox.Show("StyleKO\\LauncherWeb\\index.html bulunamadi.", "STYLEKO Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            try
            {
                string userData = Path.Combine(_root, ".StyleKO.Launcher.WebView2");
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userData);
                await _web.EnsureCoreWebView2Async(env);
                ConfigureWebView();
                _web.CoreWebView2.WebMessageReceived += WebMessageReceived;
                _web.Source = new Uri(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Microsoft Edge WebView2 Runtime baslatilamadi.\r\n\r\n" + ex.Message, "STYLEKO Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            var progress = new Progress<LauncherSnapshot>(UpdateSnapshot);
            var engine = new PatchEngine(_root, progress);
            _ = Task.Run(() => engine.RunAsync(_cts.Token));
        }

        private void ConfigureWebView()
        {
            CoreWebView2Settings settings = _web.CoreWebView2.Settings;
            settings.AreDefaultContextMenusEnabled = false;
            settings.AreDevToolsEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.IsZoomControlEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = false;
        }

        private void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string command;
            try { command = e.TryGetWebMessageAsString(); }
            catch { return; }

            switch ((command ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "launcher-ready":
                    _webReady = true;
                    PushSnapshot();
                    break;
                case "drag":
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, (IntPtr)NativeMethods.HTCAPTION, IntPtr.Zero);
                    break;
                case "home":
                    OpenHome();
                    break;
                case "close":
                    Close();
                    break;
                case "option":
                    StartOption();
                    break;
                case "start":
                    if (_snapshot.Ready && !_gameStartRequested)
                        BeginGameStart();
                    break;
            }
        }

        private void UpdateSnapshot(LauncherSnapshot snapshot)
        {
            _snapshot = snapshot;
            PushSnapshot();
        }

        private async void PushSnapshot()
        {
            if (!_webReady || _web.CoreWebView2 == null || IsDisposed)
                return;
            try
            {
                string json = _json.Serialize(new
                {
                    percent = _snapshot.Percent,
                    ready = _snapshot.Ready,
                    status = _snapshot.Status,
                    notices = _snapshot.Notices
                });
                await _web.ExecuteScriptAsync("window.akLauncherUpdate&&window.akLauncherUpdate(" + json + ");");
            }
            catch { }
        }

        private void OpenHome()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://www.stylekopvp.com/") { UseShellExecute = true });
            }
            catch { }
        }

        private void StartOption()
        {
            string exe = Path.Combine(_root, "Option.exe");
            if (!File.Exists(exe))
            {
                MessageBox.Show("Option.exe bulunamadi.", "STYLEKO Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                NativeMethods.ShellExecute(IntPtr.Zero, null, exe, null, _root, NativeMethods.SW_RESTORE);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "STYLEKO Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BeginGameStart()
        {
            _gameStartRequested = true;
            Hide();

            using (var guard = new GuardForm(_root))
            {
                bool completed = await guard.RunAsync(this);
                if (!completed)
                {
                    _gameStartRequested = false;
                    Show();
                    Activate();
                    return;
                }
            }

            string exe = Path.Combine(_root, "KnightOnLine.exe");
            if (!File.Exists(exe))
            {
                MessageBox.Show("KnightOnLine.exe bulunamadi.", "STYLEKO Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _gameStartRequested = false;
                Show();
                return;
            }

            string args = Process.GetCurrentProcess().Id.ToString();
            IntPtr result = NativeMethods.ShellExecute(IntPtr.Zero, null, exe, args, _root, NativeMethods.SW_RESTORE);
            long code = result.ToInt64();
            if (code <= 32)
            {
                MessageBox.Show("KnightOnLine.exe baslatilamadi. ShellExecute kodu: " + code, "STYLEKO Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _gameStartRequested = false;
                Show();
                return;
            }

            await Task.Delay(5000);
            Close();
        }
    }
}
