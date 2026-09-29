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
        private readonly string _root;
        private readonly WebView2 _web = new WebView2();
        private readonly TaskCompletionSource<bool> _completion = new TaskCompletionSource<bool>();

        internal GuardForm(string root)
        {
            _root = root;
            Text = "STYLEKO Guard";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(512, 300);
            MinimumSize = MaximumSize = Size;
            ShowInTaskbar = false;
            TopMost = true;
            _web.Dock = DockStyle.Fill;
            Controls.Add(_web);
            FormClosed += (s, e) => _completion.TrySetResult(false);
        }

        internal async Task<bool> RunAsync(IWin32Window owner)
        {
            string html = Path.Combine(_root, "StyleKO", "Guard", "preview.html");
            if (!File.Exists(html))
            {
                MessageBox.Show("StyleKO\Guard\preview.html bulunamadi.", "STYLEKO Guard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                string userData = Path.Combine(_root, ".StyleKO.Guard.WebView2");
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userData);
                await _web.EnsureCoreWebView2Async(env);
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
                Show(owner);
                BringToFront();
                return await _completion.Task;
            }
            catch (Exception ex)
            {
                MessageBox.Show("STYLEKO Guard browser surface olusturulamadi.

" + ex.Message, "STYLEKO Guard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
