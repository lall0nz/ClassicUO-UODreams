using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ClassicUO.Launcher.Custom
{
    // In-launcher registration window. Embeds the gamesnet.it (vBulletin 5) registration
    // page in a WebView2 control so the user can register without leaving the launcher.
    //
    // NOTE: the registration page enforces Google reCAPTCHA v2 and CSRF/AJAX flow, so a
    // fully silent HttpClient POST is not possible. The realistic "register from launcher"
    // solution is this embedded browser: the user completes the same fields (and the
    // reCAPTCHA) inside the launcher window itself.
    public sealed class RegisterForm : Form
    {
        private const string RegisterUrl = "https://www.gamesnet.it/register";

        private readonly WebView2 _webView;
        private readonly Label _statusLabel;
        private bool _webViewReady;

        public RegisterForm()
        {
            Text = "UODreams Launcher — Registrazione";
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 720);
            MinimumSize = new Size(420, 560);
            BackColor = Theme.WindowBottom;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);

            LoadWindowIcon();

            var banner = new Label
            {
                Dock = DockStyle.Top,
                Height = 54,
                Text = "Compila i campi e completa la verifica \"Non sono un robot\" per registrarti.\n"
                     + "La registrazione avviene direttamente qui, senza aprire il browser.",
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Card,
                Padding = new Padding(14, 8, 14, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Text = "Caricamento pagina di registrazione…",
                ForeColor = Theme.TextMuted,
                BackColor = Theme.WindowBottom,
                Padding = new Padding(14, 4, 14, 4),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Theme.WindowBottom
            };

            Controls.Add(_webView);
            Controls.Add(_statusLabel);
            Controls.Add(banner);

            Shown += OnShown;
        }

        private void LoadWindowIcon()
        {
            try
            {
                using Stream? stream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("ClassicUO.Launcher.Custom.Resources.uodreams.ico");
                if (stream != null)
                {
                    Icon = new Icon(stream);
                }
            }
            catch
            {
                // keep default icon if resource is missing
            }
        }

        private async void OnShown(object? sender, EventArgs e)
        {
            Shown -= OnShown;
            await InitializeWebViewAsync();
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "UODreamsLauncher",
                    "WebView2"
                );
                Directory.CreateDirectory(userDataFolder);

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder)
                    .ConfigureAwait(true);
                await _webView.EnsureCoreWebView2Async(env).ConfigureAwait(true);

                CoreWebView2 core = _webView.CoreWebView2;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.AreDefaultContextMenusEnabled = false;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.IsSwipeNavigationEnabled = false;

                // Open target="_blank" links (e.g. terms) inside the same view instead of a new window.
                core.NewWindowRequested += (_, args) =>
                {
                    args.Handled = true;
                    if (!string.IsNullOrEmpty(args.Uri))
                    {
                        core.Navigate(args.Uri);
                    }
                };

                core.NavigationStarting += (_, _) =>
                {
                    _statusLabel.Text = "Caricamento…";
                };

                core.NavigationCompleted += (_, args) =>
                {
                    _statusLabel.Text = args.IsSuccess
                        ? "Pagina pronta. Compila i campi e completa la verifica."
                        : "Impossibile caricare la pagina. Controlla la connessione.";
                };

                _webViewReady = true;
                core.Navigate(RegisterUrl);
            }
            catch (Exception ex)
            {
                _webViewReady = false;
                LauncherLog.Error("WebView2 init failed", ex);
                ShowRuntimeMissingFallback();
            }
        }

        private void ShowRuntimeMissingFallback()
        {
            _statusLabel.Text = "Componente browser non disponibile.";

            var result = MessageBox.Show(
                "Per la registrazione in-launcher è necessario il runtime \"Microsoft Edge WebView2\", "
                    + "che non risulta installato.\n\n"
                    + "Vuoi aprire la pagina di registrazione nel browser predefinito?",
                "UODreams Launcher",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information
            );

            if (result == DialogResult.Yes)
            {
                OpenInExternalBrowser(RegisterUrl);
            }

            Close();
        }

        internal static void OpenInExternalBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Impossibile aprire il link:\n" + ex.Message,
                    "UODreams Launcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (_webViewReady)
            {
                _webView.Dispose();
            }
        }
    }
}
