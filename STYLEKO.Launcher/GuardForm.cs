using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Styleko.Launcher
{
    internal sealed class GuardForm : Form
    {
        private const int GuardWidth = 512;
        private const int GuardHeight = 300;
        private const int ScreenMargin = 18;

        private readonly string _root;
        private readonly WebView2 _web = new WebView2();
        private readonly TaskCompletionSource<bool> _completion = new TaskCompletionSource<bool>();

        internal GuardForm(string root)
        {
            _root = root;
            Text = "STYLEKO Guard";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(GuardWidth, GuardHeight);
            MinimumSize = MaximumSize = Size;
            Padding = Padding.Empty;
            ShowInTaskbar = false;
            TopMost = true;

            _web.Dock = DockStyle.Fill;
            _web.Margin = Padding.Empty;
            Controls.Add(_web);

            FormClosed += (s, e) => _completion.TrySetResult(false);
        }

        internal async Task<bool> RunAsync(IWin32Window owner)
        {
            string html = Path.Combine(_root, "StyleKO", "Guard", "preview.html");
            if (!File.Exists(html))
            {
                MessageBox.Show("Guard onizleme dosyasi bulunamadi: " + html, "STYLEKO Guard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                string userData = Path.Combine(_root, ".StyleKO.Guard.WebView2");
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userData);
                await _web.EnsureCoreWebView2Async(env);

                _web.ZoomFactor = 1.0;
                _web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _web.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _web.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _web.CoreWebView2.Settings.IsZoomControlEnabled = false;

                _web.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    string msg;
                    try { msg = e.TryGetWebMessageAsString(); } catch { return; }

                    if (string.Equals(msg, "guard-complete", StringComparison.OrdinalIgnoreCase))
                    {
                        _completion.TrySetResult(true);
                        Hide();
                    }
                };

                _web.Source = new Uri(html);

                PositionAtBottomRight(owner);
                Show(owner);
                BringToFront();

                return await _completion.Task;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "STYLEKO Guard browser surface olusturulamadi." + Environment.NewLine + Environment.NewLine + ex.Message,
                    "STYLEKO Guard",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private void PositionAtBottomRight(IWin32Window owner)
        {
            Screen screen;

            if (owner != null && owner.Handle != IntPtr.Zero)
                screen = Screen.FromHandle(owner.Handle);
            else
                screen = Screen.PrimaryScreen;

            Rectangle area = screen.WorkingArea;
            int x = area.Right - Width - ScreenMargin;
            int y = area.Bottom - Height - ScreenMargin;

            Location = new Point(Math.Max(area.Left, x), Math.Max(area.Top, y));
        }
    }
}
