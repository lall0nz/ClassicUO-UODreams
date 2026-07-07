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

        // Where to send the embedded browser once registration succeeds.
        private const string SuccessRedirectUrl = "https://www.uodreams.it";

        private readonly WebView2 _webView;
        private readonly Label _statusLabel;
        private readonly Label _openInBrowserBar;
        private bool _webViewReady;

        // Guards against multiple redirects / re-entrant success detection.
        private bool _registrationSucceeded;

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

            // Thin clickable bar that lets the user open the UODreams site in their real
            // default browser instead of the embedded WebView2. It stays hidden during
            // registration and is only revealed once the view lands on www.uodreams.it.
            _openInBrowserBar = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Visible = false,
                Text = Loc.S(
                    "Preferisci il browser? Clicca qui per aprire www.uodreams.it esternamente.",
                    "Prefer your browser? Click here to open www.uodreams.it externally."),
                ForeColor = Theme.Text,
                BackColor = Theme.ButtonNeutral,
                Cursor = Cursors.Hand,
                Padding = new Padding(14, 5, 14, 5),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9f, FontStyle.Underline)
            };
            _openInBrowserBar.Click += (_, _) => OpenInExternalBrowser(SuccessRedirectUrl);
            _openInBrowserBar.MouseEnter += (_, _) => _openInBrowserBar.BackColor = Theme.ButtonNeutralHover;
            _openInBrowserBar.MouseLeave += (_, _) => _openInBrowserBar.BackColor = Theme.ButtonNeutral;

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

            // Docking is resolved from the last-added control inward, so add the
            // fill/bottom first, then the instruction banner, then the open-in-browser
            // bar last so it sits at the very top edge.
            Controls.Add(_webView);
            Controls.Add(_statusLabel);
            Controls.Add(banner);
            Controls.Add(_openInBrowserBar);

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

                core.NavigationCompleted += async (_, args) =>
                {
                    // The "open in browser" bar is only relevant once we have landed on
                    // uodreams.it (i.e. after a successful registration + redirect).
                    string currentUrl = core.Source ?? string.Empty;
                    bool onUodreams = currentUrl.IndexOf("uodreams.it", StringComparison.OrdinalIgnoreCase) >= 0;
                    _openInBrowserBar.Visible = onUodreams;

                    if (_registrationSucceeded)
                    {
                        if (onUodreams)
                        {
                            _statusLabel.Text = Loc.S(
                                "Benvenuto su UODreams! Ricordati di attivare l'account dall'email.",
                                "Welcome to UODreams! Remember to activate your account from the email.");
                        }
                        return;
                    }

                    _statusLabel.Text = args.IsSuccess
                        ? "Pagina pronta. Compila i campi e completa la verifica."
                        : "Impossibile caricare la pagina. Controlla la connessione.";

                    // Fallback content check (in case the MutationObserver missed a
                    // synchronously-rendered confirmation page).
                    if (args.IsSuccess)
                    {
                        await CheckForRegistrationSuccessAsync().ConfigureAwait(true);
                    }
                };

                // The confirmation ("Grazie, una mail è stata inviata…") is rendered by
                // vBulletin via AJAX without a full navigation, so a MutationObserver is
                // injected into every document to watch for the success text and notify
                // the host through postMessage.
                core.WebMessageReceived += (_, args) =>
                {
                    string message;
                    try
                    {
                        message = args.TryGetWebMessageAsString();
                    }
                    catch
                    {
                        message = string.Empty;
                    }

                    if (message == "uod-register-success")
                    {
                        HandleRegistrationSuccess();
                    }
                };

                await core.AddScriptToExecuteOnDocumentCreatedAsync(SuccessWatcherScript)
                    .ConfigureAwait(true);

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

        // Injected into every document. Watches the page (initial render + later AJAX
        // DOM mutations) for the vBulletin post-registration confirmation text and, when
        // found, notifies the host via postMessage("uod-register-success").
        private const string SuccessWatcherScript = @"
(function () {
    if (window.__uodRegWatch) { return; }
    window.__uodRegWatch = true;

    var keywords = [
        'una mail \u00e8 stata inviata',
        'una email \u00e8 stata inviata',
        'mail \u00e8 stata inviata',
        '\u00e8 stata inviata',
        'grazie per esserti registrato',
        'grazie per la registrazione',
        'grazie per aver effettuato la registrazione',
        'controlla la tua casella',
        'controlla la tua email',
        'controlla la tua posta',
        'email di attivazione',
        'mail di attivazione',
        'email di conferma',
        'verifica il tuo indirizzo',
        'account attivazione',
        'attivare il tuo account'
    ];

    function pageMatches() {
        try {
            var text = (document.body ? document.body.innerText : '') || '';
            text = text.toLowerCase();
            for (var i = 0; i < keywords.length; i++) {
                if (text.indexOf(keywords[i]) !== -1) {
                    return true;
                }
            }
        } catch (e) { }
        return false;
    }

    function notifySuccess() {
        try {
            window.chrome.webview.postMessage('uod-register-success');
        } catch (e) { }
    }

    function start() {
        if (pageMatches()) {
            notifySuccess();
            return;
        }
        try {
            var target = document.body || document.documentElement;
            if (!target) { return; }
            var observer = new MutationObserver(function () {
                if (pageMatches()) {
                    observer.disconnect();
                    notifySuccess();
                }
            });
            observer.observe(target, { childList: true, subtree: true, characterData: true });
        } catch (e) { }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
";

        private async Task CheckForRegistrationSuccessAsync()
        {
            if (_registrationSucceeded || !_webViewReady)
            {
                return;
            }

            try
            {
                string script =
                    "(function(){var t=(document.body?document.body.innerText:'')||'';t=t.toLowerCase();"
                    + "var k=['\\u00e8 stata inviata','grazie per esserti registrato','grazie per la registrazione',"
                    + "'controlla la tua casella','controlla la tua email','email di attivazione','mail di attivazione',"
                    + "'email di conferma','verifica il tuo indirizzo'];"
                    + "for(var i=0;i<k.length;i++){if(t.indexOf(k[i])!==-1){return true;}}return false;})();";

                string result = await _webView.CoreWebView2.ExecuteScriptAsync(script)
                    .ConfigureAwait(true);

                if (result == "true")
                {
                    HandleRegistrationSuccess();
                }
            }
            catch (Exception ex)
            {
                LauncherLog.Error("Registration success check failed", ex);
            }
        }

        private void HandleRegistrationSuccess()
        {
            if (_registrationSucceeded)
            {
                return;
            }

            _registrationSucceeded = true;
            _statusLabel.Text = Loc.S(
                "Registrazione completata! Controlla l'email per attivare l'account.",
                "Registration complete! Check your email to activate the account.");

            // Defer the modal dialog out of the WebView2 event callback to avoid
            // re-entrancy, then redirect only after the user acknowledges.
            BeginInvoke(new Action(ShowSuccessPopupAndRedirect));
        }

        private void ShowSuccessPopupAndRedirect()
        {
            MessageBox.Show(
                this,
                Loc.S(
                    "Controlla l'email per attivare l'account.",
                    "Check your email to activate the account."),
                Loc.S("Registrazione completata", "Registration complete"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            try
            {
                if (_webViewReady && _webView.CoreWebView2 != null)
                {
                    _webView.CoreWebView2.Navigate(SuccessRedirectUrl);
                }
            }
            catch (Exception ex)
            {
                LauncherLog.Error("Redirect to uodreams.it failed", ex);
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
