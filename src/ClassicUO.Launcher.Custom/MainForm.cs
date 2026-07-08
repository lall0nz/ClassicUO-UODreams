using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    public sealed class MainForm : Form
    {
        private readonly LauncherSettings _settings = LauncherSettings.Load();

        private ThemedComboBox _assistantCombo = null!;
        private TextBox _assistantPathBox = null!;
        private InputPanel _assistantPathPanel = null!;
        private ThemedButton _assistantBrowseButton = null!;
        private Label _assistantPathLabel = null!;
        private TextBox _clientPathBox = null!;
        private TextBox _uoPathBox = null!;
        private TextBox _ipBox = null!;
        private NumericUpDown _portBox = null!;
        private ThemedComboBox _serverCombo = null!;
        private bool _suppressServerComboEvents;
        private bool _suppressAssistantComboEvents;
        private CheckBox _encryptionCheck = null!;
        private Label _statusLabel = null!;
        private ThemedButton _downloadUoButton = null!;
        private Label _uoHintLabel = null!;
        private ThemedButton _downloadAssistantButton = null!;
        private Label _assistantHintLabel = null!;

        // References kept for language switching.
        private ThemedButton _langButton = null!;
        private ThemedButton _updateButton = null!;
        private ThemedButton _clearPathsButton = null!;
        private ThemedButton _launchButton = null!;
        private ThemedButton _uoBrowseButton = null!;
        private Label _assistantSectionLabel = null!;
        private Label _uoSectionLabel = null!;
        private Label _shardSectionLabel = null!;
        private Label _serverLabel = null!;
        private Label _ipLabel = null!;
        private Label _portLabel = null!;
        private FooterLinkButton _registerBtn = null!;
        private readonly System.Windows.Forms.Timer _updatePulseTimer = new() { Interval = 700 };
        private float _updatePulsePhase;

        public MainForm()
        {
            Loc.Lang = _settings.Language == "en" ? "en" : "it";
            LoadWindowIcon();
            BuildUi();
            LoadFromSettings();
            RefreshWindowTitle();

            _updatePulseTimer.Tick += (_, _) =>
            {
                _updatePulsePhase += 0.22f;
                float pulse = (float)((Math.Sin(_updatePulsePhase) + 1.0) * 0.5);
                _updateButton.HighlightPulse = pulse;
                _updateButton.Invalidate();
            };

            Shown += (_, _) => _ = CheckForUpdatesOnStartupAsync();
        }

        private void LoadWindowIcon()
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly()
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

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new LinearGradientBrush(
                ClientRectangle, Theme.WindowTop, Theme.WindowBottom, LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        private static Image? LoadLogo()
        {
            try
            {
                using Stream? s = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("ClassicUO.Launcher.Custom.Resources.uodreams_logo.png");
                return s != null ? Image.FromStream(s) : null;
            }
            catch
            {
                return null;
            }
        }

        private void BuildUi()
        {
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            const int formWidth = 620;
            const int bottomMargin = 12;
            int margin = 24;
            int width = formWidth - margin * 2;
            int y = 16;

            // ----- UODreams banner -----
            Image? logo = LoadLogo();

            if (logo != null)
            {
                int bannerH = 118;
                var logoBox = new PictureBox
                {
                    Image = logo,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Bounds = new Rectangle(margin, y, width, bannerH)
                };
                Controls.Add(logoBox);
                y += bannerH + 10;
            }

            // ----- Card 1: assistant -----
            var assistantCard = new CardPanel { Bounds = new Rectangle(margin, y, width, 190) };
            Controls.Add(assistantCard);

            int cx = 18, cy = 14, cw = width - 36;

            _assistantSectionLabel = SectionLabel(Loc.S("Seleziona l'assistant", "Select assistant"), cx, cy, cw);
            assistantCard.Controls.Add(_assistantSectionLabel);
            cy += 28;

            _assistantCombo = new ThemedComboBox
            {
                PlaceholderFirstItem = true,
                Bounds = new Rectangle(cx, cy, cw, 30)
            };
            _assistantCombo.Items.Add(Loc.S("Seleziona un assistant", "Select an assistant"));
            _assistantCombo.Items.Add("ClassicAssist");
            _assistantCombo.Items.Add("Razor Enhanced");
            _assistantCombo.Items.Add("Orion");
            _assistantCombo.Items.Add("UOSteam");
            _assistantCombo.SelectedIndexChanged += (_, _) =>
            {
                if (_suppressAssistantComboEvents)
                {
                    return;
                }

                if (SelectedAssistant == "UOSteam")
                {
                    ShowUOSteamNotice();
                }

                UpdateAssistantUi();
                UpdateAssistantDownloadUi();
            };
            assistantCard.Controls.Add(_assistantCombo);
            cy += 38;

            _assistantPathLabel = new Label
            {
                Text = Loc.S("Percorso assistente", "Assistant path"),
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Bounds = new Rectangle(cx, cy, cw, 18)
            };
            assistantCard.Controls.Add(_assistantPathLabel);
            cy += 20;

            (_assistantPathPanel, _assistantPathBox, _assistantBrowseButton) =
                PathRow(assistantCard, cx, cy, cw - 168, BrowseAssistant);
            _assistantPathBox.TextChanged += (_, _) => UpdateAssistantDownloadUi();

            _downloadAssistantButton = new ThemedButton
            {
                Text = Loc.S("⬇ Scarica", "⬇ Download"),
                UseGradient = true,
                CornerRadius = 8,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                Bounds = new Rectangle(cx + cw - 158, cy, 158, 32)
            };
            _downloadAssistantButton.Click += (_, _) => DownloadAssistant();
            assistantCard.Controls.Add(_downloadAssistantButton);
            cy += 38;

            _assistantHintLabel = new Label
            {
                Text = "",
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                MaximumSize = new Size(cw, 0),
                Location = new Point(cx, cy)
            };
            assistantCard.Controls.Add(_assistantHintLabel);
            y += assistantCard.Height + 14;

            // ----- Card 2: Ultima Online -----
            var pathsCard = new CardPanel { Bounds = new Rectangle(margin, y, width, 150) };
            Controls.Add(pathsCard);
            cy = 14;

            _uoSectionLabel = SectionLabel(Loc.S("Client Ultima Online", "Ultima Online client"), cx, cy, cw);
            pathsCard.Controls.Add(_uoSectionLabel);
            cy += 26;
            (_, _uoPathBox, _uoBrowseButton) = PathRow(pathsCard, cx, cy, cw - 168, BrowseUoFolder);
            _uoPathBox.TextChanged += (_, _) => UpdateUoDownloadUi();

            _downloadUoButton = new ThemedButton
            {
                Text = Loc.S("⬇ Scarica UODreams", "⬇ Download UODreams"),
                UseGradient = true,
                CornerRadius = 8,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                Bounds = new Rectangle(cx + cw - 158, cy, 158, 32)
            };
            _downloadUoButton.Click += (_, _) => DownloadUoClient();
            pathsCard.Controls.Add(_downloadUoButton);
            cy += 38;

            _uoHintLabel = new Label
            {
                Text = "",
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                MaximumSize = new Size(cw, 0),
                Location = new Point(cx, cy)
            };
            pathsCard.Controls.Add(_uoHintLabel);

            _clientPathBox = new TextBox { Visible = false };
            Controls.Add(_clientPathBox);
            y += pathsCard.Height + 14;

            // ----- Card 3: shard -----
            var shardCard = new CardPanel { Bounds = new Rectangle(margin, y, width, 174) };
            Controls.Add(shardCard);
            cy = 14;

            _shardSectionLabel = SectionLabel("SHARD - SERVER", cx, cy, cw);
            shardCard.Controls.Add(_shardSectionLabel);
            cy += 28;

            _serverLabel = new Label
            {
                Text = "Server",
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Bounds = new Rectangle(cx, cy, 200, 16)
            };
            shardCard.Controls.Add(_serverLabel);
            cy += 18;

            _serverCombo = new ThemedComboBox
            {
                Bounds = new Rectangle(cx, cy, cw - 42, 30)
            };
            _serverCombo.SelectedIndexChanged += (_, _) => OnServerComboChanged();
            shardCard.Controls.Add(_serverCombo);

            var addServerButton = new ThemedButton
            {
                Text = "+",
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                Bounds = new Rectangle(cx + cw - 34, cy, 34, 30)
            };
            addServerButton.Click += (_, _) => AddServer();
            shardCard.Controls.Add(addServerButton);
            cy += 40;

            _ipLabel = new Label
            {
                Text = Loc.S("Indirizzo", "Address"),
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Bounds = new Rectangle(cx, cy, 200, 16)
            };
            _portLabel = new Label
            {
                Text = Loc.S("Porta", "Port"),
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Bounds = new Rectangle(cx + 320, cy, 80, 16)
            };
            shardCard.Controls.Add(_ipLabel);
            shardCard.Controls.Add(_portLabel);
            cy += 18;

            var ipPanel = new InputPanel { Bounds = new Rectangle(cx, cy, 300, 32) };
            _ipBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                Dock = DockStyle.Fill
            };
            ipPanel.Controls.Add(_ipBox);
            shardCard.Controls.Add(ipPanel);

            var portPanel = new InputPanel { Bounds = new Rectangle(cx + 320, cy, 90, 32) };
            _portBox = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 65535,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                Dock = DockStyle.Fill
            };
            portPanel.Controls.Add(_portBox);
            shardCard.Controls.Add(portPanel);

            _encryptionCheck = new CheckBox
            {
                Text = Loc.S("Crittografia", "Encryption"),
                ForeColor = Theme.Text,
                BackColor = Color.Transparent,
                Bounds = new Rectangle(cx + 430, cy + 4, cw - 430, 24)
            };
            shardCard.Controls.Add(_encryptionCheck);
            y += shardCard.Height + 18;

            // ----- Launch button -----
            _launchButton = new ThemedButton
            {
                Text = Loc.S("AVVIA", "START"),
                UseGradient = true,
                CornerRadius = 12,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                Bounds = new Rectangle(margin, y, width, 48)
            };
            _launchButton.Click += (_, _) => Launch();
            Controls.Add(_launchButton);
            y += 56;

            _statusLabel = new Label
            {
                Text = "",
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Bounds = new Rectangle(margin, y, width, 26),
                TextAlign = ContentAlignment.TopCenter
            };
            Controls.Add(_statusLabel);
            y += 30;

            // ----- Footer links -----
            const int footerGap = 6;
            const int footerH = 28;
            int footerBtnW = (width - footerGap * 2) / 3;

            var websiteBtn = new FooterLinkButton
            {
                Text = "UODreams",
                Bounds = new Rectangle(margin, y, footerBtnW, footerH)
            };
            websiteBtn.Click += (_, _) => OpenUrl("https://www.uodreams.it");
            Controls.Add(websiteBtn);

            var discordBtn = new FooterLinkButton
            {
                Text = "💬 Discord",
                AccentColor = Theme.DiscordAccent,
                Bounds = new Rectangle(margin + footerBtnW + footerGap, y, footerBtnW, footerH)
            };
            discordBtn.Click += (_, _) => OpenUrl("https://discord.com/invite/FWVjsRv");
            Controls.Add(discordBtn);

            _registerBtn = new FooterLinkButton
            {
                Text = Loc.S("Registrati gratis", "Register for free"),
                Bounds = new Rectangle(margin + (footerBtnW + footerGap) * 2, y, footerBtnW, footerH)
            };
            _registerBtn.Click += (_, _) => OpenRegisterWindow();
            Controls.Add(_registerBtn);

            // ----- Toolbar (top) -----
            _clearPathsButton = new ThemedButton
            {
                Text = Loc.S("🗑 Pulisci Campi", "🗑 Clear Fields"),
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                ForeColor = Theme.Text,
                Bounds = new Rectangle(margin, 12, 112, 26)
            };
            _clearPathsButton.Click += (_, _) => ClearAllPaths();
            Controls.Add(_clearPathsButton);

            _updateButton = new ThemedButton
            {
                Text = Loc.S("⬇ Aggiorna", "⬇ Update"),
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Theme.Text,
                Bounds = new Rectangle(formWidth - margin - 148, 12, 76, 26)
            };
            _updateButton.Click += (_, _) => CheckForUpdates();
            Controls.Add(_updateButton);

            // ----- Language toggle (top-right) -----
            _langButton = new ThemedButton
            {
                Text = LangButtonText(),
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Theme.Text,
                Bounds = new Rectangle(formWidth - margin - 66, 12, 66, 26)
            };
            _langButton.Click += (_, _) => ToggleLanguage();
            Controls.Add(_langButton);
            _clearPathsButton.BringToFront();
            _updateButton.BringToFront();
            _langButton.BringToFront();

            ClientSize = new Size(formWidth, y + footerH + bottomMargin);
        }

        private void RefreshWindowTitle()
        {
            string launcherVer = LauncherManifest.LauncherVersion;
            string clientVer = _settings.EffectiveClientVersion;
            Text = string.Equals(launcherVer, clientVer, StringComparison.OrdinalIgnoreCase)
                ? $"UODreams Launcher v{launcherVer}"
                : $"UODreams Launcher v{launcherVer} · client v{clientVer}";
        }

        private void SetUpdateAvailable(bool available)
        {
            _updateButton.PulseHighlight = available;
            if (available)
            {
                _updatePulseTimer.Start();
            }
            else
            {
                _updatePulseTimer.Stop();
                _updateButton.HighlightPulse = 0;
                _updateButton.Invalidate();
            }
        }

        private async Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                UpdateCheckResult? info = await LauncherUpdater.CheckForUpdatesAsync(_settings.EffectiveClientVersion);
                if (info?.HasAnyUpdate == true && !IsDisposed)
                {
                    BeginInvoke(() => SetUpdateAvailable(true));
                }
            }
            catch
            {
                // silent background check
            }
        }

        private static string BuildUpdateDetails(UpdateCheckResult info)
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(Loc.S(
                $"Disponibile la versione v{info.LatestVersion}.",
                $"Version v{info.LatestVersion} is available."));
            lines.AppendLine();

            if (info.NeedsLauncherUpdate)
            {
                lines.AppendLine(Loc.S("• Aggiornamento launcher", "• Launcher update"));
            }

            if (info.NeedsClientUpdate)
            {
                lines.AppendLine(Loc.S("• Aggiornamento client ClassicUO", "• ClassicUO client update"));
            }

            if (!string.IsNullOrWhiteSpace(info.ReleaseNotes))
            {
                lines.AppendLine();
                lines.AppendLine(Loc.S("Novità:", "What's new:"));
                lines.AppendLine(info.ReleaseNotes);
            }

            lines.AppendLine();
            lines.AppendLine(Loc.S(
                "Procedere con il download e l'installazione?",
                "Proceed with download and installation?"));

            return lines.ToString().TrimEnd();
        }

        private void MarkClientUpdated(string version)
        {
            _settings.InstalledClientVersion = version;
            _settings.Save();
            RefreshWindowTitle();
        }

        private void ClearAllPaths()
        {
            _settings.ResetUserPaths();
            _settings.Save();

            _assistantCombo.SelectedIndex = 0;
            _uoPathBox.Text = "";
            _assistantPathBox.Text = "";
            _clientPathBox.Text = "";

            UpdateAssistantUi();
            UpdateAssistantDownloadUi();
            UpdateUoDownloadUi();

            _statusLabel.ForeColor = Theme.SectionGreen;
            _statusLabel.Text = Loc.S("Path resettati.", "Paths cleared.");
        }

        private static string LangButtonText() => Loc.IsEn ? "🌐 EN" : "🌐 IT";

        private void ToggleLanguage()
        {
            Loc.Lang = Loc.IsEn ? "it" : "en";
            _settings.Language = Loc.Lang;
            _settings.Save();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            _langButton.Text = LangButtonText();

            _assistantSectionLabel.Text = Loc.S("Seleziona l'assistant", "Select assistant").ToUpperInvariant();
            _uoSectionLabel.Text = Loc.S("Client Ultima Online", "Ultima Online client").ToUpperInvariant();
            _shardSectionLabel.Text = "SHARD - SERVER";

            if (_assistantCombo.Items.Count > 0)
            {
                _assistantCombo.Items[0] = Loc.S("Seleziona un assistant", "Select an assistant");
            }

            _assistantBrowseButton.Text = Loc.S("Sfoglia…", "Browse…");
            _uoBrowseButton.Text = Loc.S("Sfoglia…", "Browse…");
            _downloadAssistantButton.Text = Loc.S("⬇ Scarica", "⬇ Download");
            _downloadUoButton.Text = Loc.S("⬇ Scarica UODreams", "⬇ Download UODreams");
            _clearPathsButton.Text = Loc.S("🗑 Pulisci Campi", "🗑 Clear Fields");
            _updateButton.Text = Loc.S("⬇ Aggiorna", "⬇ Update");
            _launchButton.Text = Loc.S("AVVIA", "START");
            _registerBtn.Text = Loc.S("Registrati gratis", "Register for free");

            _serverLabel.Text = "Server";
            _ipLabel.Text = Loc.S("Indirizzo", "Address");
            _portLabel.Text = Loc.S("Porta", "Port");
            _encryptionCheck.Text = Loc.S("Crittografia", "Encryption");

            // Owner-drawn combo: repaint after language switch.
            _assistantCombo.Invalidate();

            // Refresh dynamic/contextual texts.
            UpdateAssistantUi();
            UpdateUoDownloadUi();

            _statusLabel.Text = "";
        }

        private void OpenRegisterWindow()
        {
            try
            {
                using var form = new RegisterForm();
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                LauncherLog.Error("Failed to open registration window", ex);
                RegisterForm.OpenInExternalBrowser("https://www.gamesnet.it/register");
            }
        }

        private static void OpenUrl(string url)
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
                    Loc.S("Impossibile aprire il link:\n", "Unable to open the link:\n") + ex.Message,
                    "UODreams Launcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private static Label SectionLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text.ToUpperInvariant(),
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = Theme.SectionGreen,
                BackColor = Color.Transparent,
                AutoSize = false,
                Bounds = new Rectangle(x, y, width, 20)
            };
        }

        private static (InputPanel, TextBox, ThemedButton) PathRow(Control parent, int x, int y, int width, Action browse)
        {
            var panel = new InputPanel { Bounds = new Rectangle(x, y, width - 104, 32) };
            var box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                Dock = DockStyle.Fill
            };
            panel.Controls.Add(box);

            var button = new ThemedButton
            {
                Text = Loc.S("Sfoglia…", "Browse…"),
                Bounds = new Rectangle(x + width - 94, y, 94, 32)
            };
            button.Click += (_, _) => browse();

            parent.Controls.Add(panel);
            parent.Controls.Add(button);
            return (panel, box, button);
        }

        private void LoadFromSettings()
        {
            _suppressAssistantComboEvents = true;
            try
            {
                string savedAssistant = _settings.Assistant;
                if (savedAssistant == "Nessuno" || string.IsNullOrEmpty(savedAssistant))
                {
                    _assistantCombo.SelectedIndex = 0;
                }
                else if (savedAssistant == "Razor")
                {
                    _assistantCombo.SelectedItem = "Razor Enhanced";
                }
                else if (_assistantCombo.Items.Contains(savedAssistant))
                {
                    _assistantCombo.SelectedItem = savedAssistant;
                }
                else
                {
                    _assistantCombo.SelectedIndex = 0;
                }
            }
            finally
            {
                _suppressAssistantComboEvents = false;
            }

            _uoPathBox.Text = _settings.UoDirectory;
            _ipBox.Text = _settings.ShardIp;
            _portBox.Value = Math.Min(Math.Max(_settings.ShardPort, 1), 65535);
            _encryptionCheck.Checked = _settings.Encryption != 0;
            RefreshServerCombo();

            _clientPathBox.Text = !string.IsNullOrEmpty(_settings.ClientPath) && File.Exists(_settings.ClientPath)
                ? _settings.ClientPath
                : DetectDefaultClient();

            UpdateAssistantUi();
            UpdateAssistantDownloadUi();
            UpdateUoDownloadUi();
        }

        private void RefreshServerCombo()
        {
            _suppressServerComboEvents = true;
            _serverCombo.Items.Clear();

            foreach (ShardServer server in _settings.Servers)
            {
                _serverCombo.Items.Add(server.Name);
            }

            int idx = _serverCombo.FindStringExact(_settings.SelectedServer);

            if (idx < 0)
            {
                string ip = (_ipBox.Text ?? "").Trim();
                ShardServer? match = _settings.Servers.Find(s =>
                    string.Equals(s.Ip, ip, StringComparison.OrdinalIgnoreCase) &&
                    s.Port == (int)_portBox.Value);

                if (match != null)
                {
                    idx = _serverCombo.Items.IndexOf(match.Name);
                }
            }

            if (idx < 0 && _serverCombo.Items.Count > 0)
            {
                idx = 0;
            }

            if (idx >= 0)
            {
                _serverCombo.SelectedIndex = idx;
            }

            _suppressServerComboEvents = false;
        }

        private void OnServerComboChanged()
        {
            if (_suppressServerComboEvents)
            {
                return;
            }

            if (_serverCombo.SelectedItem is not string name)
            {
                return;
            }

            ShardServer? server = _settings.Servers.Find(s => s.Name == name);
            if (server == null)
            {
                return;
            }

            _settings.SelectedServer = name;
            _ipBox.Text = server.Ip;
            _portBox.Value = Math.Min(Math.Max(server.Port, 1), 65535);
        }

        private void AddServer()
        {
            ShardServer? added = ShardServerDialog.Prompt(this);
            if (added == null)
            {
                return;
            }

            ShardServer? existing = _settings.Servers.Find(s =>
                string.Equals(s.Name, added.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Ip = added.Ip;
                existing.Port = added.Port;
            }
            else
            {
                _settings.Servers.Add(added);
            }

            _settings.SelectedServer = added.Name;
            _settings.Save();

            RefreshServerCombo();
            OnServerComboChanged();
        }

        private void UpdateUoDownloadUi()
        {
            string uoPath = (_uoPathBox.Text ?? "").Trim().Trim('"');
            bool valid = !string.IsNullOrEmpty(uoPath) &&
                Directory.Exists(uoPath) &&
                File.Exists(Path.Combine(uoPath, "tiledata.mul"));

            _downloadUoButton.Visible = !valid;
            _uoHintLabel.Text = valid
                ? Loc.S("Client Ultima Online pronto.", "Ultima Online client ready.")
                : Loc.S(
                    "Client non trovato — scarica il pacchetto UODreams o seleziona la cartella del gioco con il client.",
                    "Client not found — download the UODreams package or select the game folder containing the client.");
            _uoHintLabel.ForeColor = valid ? Theme.SectionGreen : Theme.TextMuted;
        }

        private static string GetClientDir() => ClientRuntimeDownloader.ClientDir;

        private static string GetBootstrapDir() => ClientRuntimeDownloader.BootstrapDir;

        private static string DetectDefaultClient()
        {
            string? unified = ClientRuntimeDownloader.TryGetUnifiedNativeClientExe();
            if (unified != null)
            {
                return unified;
            }

            string clientDir = GetClientDir();
            string modded = Path.Combine(clientDir, "cuo-modded.exe");

            if (File.Exists(modded))
                return modded;

            string legacy = Path.Combine(clientDir, "cuo.exe");
            return File.Exists(legacy) ? legacy : "";
        }

        private static bool IsNativeCuoDll(string cuoDllPath) =>
            ClientRuntimeDownloader.IsNativeCuoDll(cuoDllPath);

        private static string ResolveClientExecutable(string clientPath, string assistant)
        {
            string clientDir = GetClientDir();

            // Dust765-style: modded native cuo.dll + ClassicUO.exe (mods + Razor together).
            string? unified = ClientRuntimeDownloader.TryGetUnifiedNativeClientExe();
            if (unified != null)
            {
                if (assistant is "Razor Enhanced" or "ClassicAssist" or "Nessuno")
                {
                    return unified;
                }
            }

            if (assistant == "Razor Enhanced")
            {
                string? legacyBootstrap = ClientRuntimeDownloader.TryGetLegacyBootstrapClientExe();
                if (legacyBootstrap != null)
                {
                    return legacyBootstrap;
                }

                string bootstrapDir = GetBootstrapDir();
                string bootstrap = Path.Combine(bootstrapDir, "ClassicUO.exe");
                string nativeCuo = Path.Combine(bootstrapDir, "cuo.dll");

                if (File.Exists(bootstrap) && IsNativeCuoDll(nativeCuo))
                    return bootstrap;
            }
            else
            {
                string modded = Path.Combine(clientDir, "cuo-modded.exe");
                if (File.Exists(modded))
                    return modded;

                string legacy = Path.Combine(clientDir, "cuo.exe");
                if (File.Exists(legacy))
                    return legacy;
            }

            return clientPath;
        }

        private string SelectedAssistant
        {
            get
            {
                if (_assistantCombo.SelectedIndex <= 0)
                {
                    return "Nessuno";
                }

                return _assistantCombo.SelectedItem as string ?? "Nessuno";
            }
        }

        private void UpdateAssistantUi()
        {
            bool hasAssistant = SelectedAssistant != "Nessuno";
            _assistantPathLabel.Visible = hasAssistant;
            _assistantPathPanel.Visible = hasAssistant;
            _assistantBrowseButton.Visible = hasAssistant;

            bool allowAutoDetect = _settings.FirstRunCompleted;

            switch (SelectedAssistant)
            {
                case "ClassicAssist":
                    _assistantPathLabel.Text = Loc.S(
                        "Percorso di ClassicAssist.dll (o della sua cartella)",
                        "Path to ClassicAssist.dll (or its folder)");
                    _assistantPathBox.Text = _settings.ClassicAssistPath;
                    break;
                case "Razor Enhanced":
                    _assistantPathLabel.Text = Loc.S(
                        "Percorso di RazorEnhanced (cartella o RazorEnhanced.exe)",
                        "Path to RazorEnhanced (folder or RazorEnhanced.exe)");
                    _assistantPathBox.Text = string.IsNullOrWhiteSpace(_settings.RazorPath) && allowAutoDetect
                        ? ClientRuntimeDownloader.DetectRazorEnhancedPath() ?? ""
                        : _settings.RazorPath;
                    break;
                case "Orion":
                    _assistantPathLabel.Text = Loc.S(
                        "Percorso di Orion Launcher (cartella o OrionLauncher64.exe)",
                        "Path to Orion Launcher (folder or OrionLauncher64.exe)");
                    _assistantPathBox.Text = string.IsNullOrWhiteSpace(_settings.OrionPath) && allowAutoDetect
                        ? ClientRuntimeDownloader.DetectOrionInstallRoot() ?? ""
                        : _settings.OrionPath;
                    break;
                case "UOSteam":
                    _assistantPathLabel.Text = Loc.S(
                        "Percorso di UOSteam (cartella o UOS.exe)",
                        "Path to UOSteam (folder or UOS.exe)");
                    _assistantPathBox.Text = string.IsNullOrWhiteSpace(_settings.UOSteamPath) && allowAutoDetect
                        ? ClientRuntimeDownloader.DetectUOSteamExe() ?? ""
                        : _settings.UOSteamPath;
                    break;
            }

            UpdateAssistantDownloadUi();
        }

        private void UpdateAssistantDownloadUi()
        {
            bool hasAssistant = SelectedAssistant != "Nessuno";
            bool canDownload = hasAssistant && AssistantDownloader.SupportsDownload(SelectedAssistant);
            bool installed = hasAssistant &&
                AssistantDownloader.IsPluginValidAtPath(SelectedAssistant, _assistantPathBox.Text);

            _downloadAssistantButton.Visible = canDownload && !installed;
            _assistantHintLabel.Visible = hasAssistant;

            if (!hasAssistant)
            {
                return;
            }

            if (installed)
            {
                _assistantHintLabel.Text = Loc.S($"{SelectedAssistant} pronto.", $"{SelectedAssistant} ready.");
                _assistantHintLabel.ForeColor = Theme.SectionGreen;
            }
            else if (canDownload)
            {
                _assistantHintLabel.Text = SelectedAssistant switch
                {
                    "UOSteam" => Loc.S(
                        "UOSteam non trovato — scaricalo o seleziona la cartella con UOS.exe.",
                        "UOSteam not found — download it or select the folder containing UOS.exe."),
                    "Orion" => Loc.S(
                        "Orion non trovato — scaricalo o seleziona la cartella con OrionLauncher64.exe.",
                        "Orion not found — download it or select the folder containing OrionLauncher64.exe."),
                    _ => Loc.S(
                        $"{SelectedAssistant} non trovato — scaricalo o seleziona il percorso del plugin.",
                        $"{SelectedAssistant} not found — download it or select the plugin path.")
                };
                _assistantHintLabel.ForeColor = Theme.TextMuted;
            }
            else
            {
                _assistantHintLabel.Text = Loc.S(
                    "Seleziona il percorso dell'assistente.",
                    "Select the assistant path.");
                _assistantHintLabel.ForeColor = Theme.TextMuted;
            }
        }

        private void BrowseAssistant()
        {
            using var dialog = new OpenFileDialog
            {
                Title = SelectedAssistant switch
                {
                    "ClassicAssist" => Loc.S("Seleziona ClassicAssist.dll", "Select ClassicAssist.dll"),
                    "Orion" => Loc.S(
                        "Seleziona OrionLauncher64.exe o la cartella di Orion Launcher",
                        "Select OrionLauncher64.exe or the Orion Launcher folder"),
                    "UOSteam" => Loc.S(
                        "Seleziona UOS.exe o la cartella di UOSteam",
                        "Select UOS.exe or the UOSteam folder"),
                    _ => Loc.S(
                        "Seleziona RazorEnhanced.exe o la sua cartella",
                        "Select RazorEnhanced.exe or its folder")
                },
                Filter = Loc.S(
                    "Plugin (*.dll;*.exe)|*.dll;*.exe|Tutti i file (*.*)|*.*",
                    "Plugin (*.dll;*.exe)|*.dll;*.exe|All files (*.*)|*.*")
            };

            if (!string.IsNullOrEmpty(_assistantPathBox.Text))
            {
                try
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(_assistantPathBox.Text);
                }
                catch { }
            }

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _assistantPathBox.Text = dialog.FileName;
                UpdateAssistantDownloadUi();
            }
        }

        private void BrowseUoFolder()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = Loc.S(
                    "Seleziona la cartella di Ultima Online (deve contenere tiledata.mul)",
                    "Select the Ultima Online folder (must contain tiledata.mul)"),
                UseDescriptionForTitle = true
            };

            if (Directory.Exists(_uoPathBox.Text))
                dialog.SelectedPath = _uoPathBox.Text;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _uoPathBox.Text = dialog.SelectedPath;
                UpdateUoDownloadUi();
            }
        }

        private void DownloadAssistant()
        {
            if (!AssistantDownloader.SupportsDownload(SelectedAssistant))
            {
                return;
            }

            string orionInfoMessage = Loc.S(
                "Orion Assistant: una volta terminato il download, si aprirà in automatico e bisognerà aggiornarlo e configurarlo.",
                "Orion Assistant: once the download is finished, it will open automatically and you will need to update and configure it.");

            using var folderDialog = new FolderBrowserDialog
            {
                Description = Loc.S(
                    $"Scegli dove installare {SelectedAssistant}",
                    $"Choose where to install {SelectedAssistant}"),
                UseDescriptionForTitle = true
            };

            string defaultDir = AssistantDownloader.GetDefaultInstallDirectory(SelectedAssistant);
            if (Directory.Exists(_assistantPathBox.Text))
            {
                folderDialog.SelectedPath = _assistantPathBox.Text;
            }
            else if (Directory.Exists(defaultDir))
            {
                folderDialog.SelectedPath = defaultDir;
            }
            else if (Directory.Exists(Path.GetDirectoryName(defaultDir) ?? ""))
            {
                folderDialog.SelectedPath = Path.GetDirectoryName(defaultDir)!;
            }

            if (folderDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string installDir = Path.Combine(folderDialog.SelectedPath, SelectedAssistant switch
            {
                "ClassicAssist" => "ClassicAssist",
                "Razor Enhanced" => "RazorEnhanced",
                "Orion" => "Orion",
                "UOSteam" => "UOSteam",
                _ => SelectedAssistant
            });

            _statusLabel.ForeColor = Theme.TextMuted;
            _statusLabel.Text = Loc.S($"Download {SelectedAssistant} in corso…", $"Downloading {SelectedAssistant}…");
            _downloadAssistantButton.Enabled = false;

            using var progressForm = DownloadProgressForm.ForAssistant(
                SelectedAssistant,
                installDir,
                SelectedAssistant == "Orion" ? orionInfoMessage : null);
            var result = progressForm.ShowDialog(this);

            _downloadAssistantButton.Enabled = true;

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(progressForm.ResultPath))
            {
                _assistantPathBox.Text = progressForm.ResultPath;
                SaveSettings();
                UpdateAssistantDownloadUi();
                _statusLabel.ForeColor = Theme.SectionGreen;
                _statusLabel.Text = Loc.S($"{SelectedAssistant} installato correttamente.", $"{SelectedAssistant} installed successfully.");
            }
            else if (result == DialogResult.Abort)
            {
                _statusLabel.ForeColor = Theme.Error;
                _statusLabel.Text = Loc.S(
                    $"Download di {SelectedAssistant} non riuscito. Riprova o seleziona manualmente il percorso.",
                    $"{SelectedAssistant} download failed. Retry or select the path manually.");
            }
            else
            {
                _statusLabel.Text = "";
            }
        }

        private void DownloadUoClient()
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = Loc.S(
                    "Scegli dove salvare il client Ultima Online UODreams",
                    "Choose where to save the Ultima Online UODreams client"),
                UseDescriptionForTitle = true
            };

            if (Directory.Exists(_uoPathBox.Text))
                folderDialog.SelectedPath = _uoPathBox.Text;
            else if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
                folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (folderDialog.ShowDialog(this) != DialogResult.OK)
                return;

            string extractDir = Path.Combine(folderDialog.SelectedPath, "Ultima Online UODreams");

            _statusLabel.ForeColor = Theme.TextMuted;
            _statusLabel.Text = Loc.S("Download client UODreams in corso…", "Downloading UODreams client…");
            _downloadUoButton.Enabled = false;

            using var progressForm = DownloadProgressForm.ForUoClient(extractDir);
            var result = progressForm.ShowDialog(this);

            _downloadUoButton.Enabled = true;

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(progressForm.ResultPath))
            {
                _uoPathBox.Text = progressForm.ResultPath;
                SaveSettings();
                UpdateUoDownloadUi();
                _statusLabel.ForeColor = Theme.SectionGreen;
                _statusLabel.Text = Loc.S("Client UODreams installato correttamente.", "UODreams client installed successfully.");
            }
            else if (result == DialogResult.Abort)
            {
                _statusLabel.ForeColor = Theme.Error;
                _statusLabel.Text = Loc.S(
                    "Download non riuscito. Riprova o seleziona manualmente la cartella.",
                    "Download failed. Retry or select the folder manually.");
            }
            else
            {
                _statusLabel.Text = "";
            }
        }

        private string? ResolveAssistantDll()
        {
            string path = _assistantPathBox.Text.Trim().Trim('"');

            if (string.IsNullOrEmpty(path))
                return null;

            if (SelectedAssistant == "Orion")
                return null;

            if (Directory.Exists(path))
            {
                string[] candidates = SelectedAssistant switch
                {
                    "ClassicAssist" => new[] { "ClassicAssist.dll" },
                    _ => new[] { "RazorEnhanced.exe", "RazorEnhanced.dll", "Razor.dll", "RazorCE.dll", "Razor.exe" }
                };

                foreach (string name in candidates)
                {
                    string candidate = Path.Combine(path, name);
                    if (File.Exists(candidate))
                        return candidate;
                }

                return null;
            }

            return File.Exists(path) ? path : null;
        }

        private void LaunchOrion()
        {
            string? orionExe = AssistantPaths.ResolveOrionLauncherExe(_assistantPathBox.Text);
            if (orionExe == null)
            {
                ShowError(Loc.S(
                    "Orion Launcher non trovato. Seleziona la cartella con OrionLauncher64.exe\n(es. C:\\Orion Launcher).",
                    "Orion Launcher not found. Select the folder containing OrionLauncher64.exe\n(e.g. C:\\Orion Launcher)."
                ));
                return;
            }

            SaveSettings();

            string orionDir = Path.GetDirectoryName(orionExe)!;
            var psi = new ProcessStartInfo
            {
                FileName = orionExe,
                WorkingDirectory = orionDir,
                UseShellExecute = true
            };

            LauncherLog.Info($"Launch Orion Launcher external exe={orionExe} (no CLI args)");

            try
            {
                Process? process = Process.Start(psi);
                if (process == null)
                {
                    ShowError(Loc.S("Impossibile avviare Orion Launcher.", "Unable to start Orion Launcher."));
                    return;
                }

                _statusLabel.ForeColor = Theme.TextMuted;
                _statusLabel.Text = Loc.S(
                    "Orion Launcher avviato. Seleziona il profilo e premi Launch.",
                    "Orion Launcher started. Select the profile and press Launch.");
                LauncherLog.Info($"Orion Launcher avviato PID={process.Id}");
                Close();
            }
            catch (Exception ex)
            {
                ShowError(Loc.S("Errore durante l'avvio di Orion Launcher: ", "Error while starting Orion Launcher: ") + ex.Message, ex);
            }
        }

        private void ShowUOSteamNotice()
        {
            ThemedMessageDialog.ShowInfo(
                this,
                Loc.S("UOSteam", "UOSteam"),
                Loc.S(
                    "UOSteam è un assistant che non supporta ClassicUO. Se vuoi provare un nuovo assistant simile, " +
                    "prova ClassicAssist, così potrai usufruire di tutte le feature del Classic Client di UO.",
                    "UOSteam is an assistant that does not support ClassicUO. If you want to try a similar new " +
                    "assistant, try ClassicAssist, so you can enjoy all the features of the UO Classic Client."));
        }

        private void LaunchUOSteam()
        {
            string? uosExe = AssistantPaths.ResolveUOSteamExe(_assistantPathBox.Text);
            if (uosExe == null)
            {
                ShowError(Loc.S(
                    "UOSteam non trovato. Seleziona la cartella con UOS.exe\n(es. C:\\Program Files (x86)\\UOS).",
                    "UOSteam not found. Select the folder containing UOS.exe\n(e.g. C:\\Program Files (x86)\\UOS)."));
                return;
            }

            SaveSettings();

            string uosDir = Path.GetDirectoryName(uosExe)!;
            var psi = new ProcessStartInfo
            {
                FileName = uosExe,
                WorkingDirectory = uosDir,
                UseShellExecute = true
            };

            LauncherLog.Info(
                $"Launch UOSteam external exe={uosExe} shard={_ipBox.Text.Trim()}:{_portBox.Value} " +
                "(shard/port configured inside UOS UI — no CLI args)"
            );

            try
            {
                Process? process = Process.Start(psi);
                if (process == null)
                {
                    ShowError(Loc.S("Impossibile avviare UOS.exe.", "Unable to start UOS.exe."));
                    return;
                }

                _statusLabel.ForeColor = Theme.TextMuted;
                _statusLabel.Text = Loc.S(
                    "UOSteam avviato. Configura shard/porta nell'interfaccia UOS.",
                    "UOSteam started. Configure shard/port in the UOS interface.");
                LauncherLog.Info($"UOSteam avviato PID={process.Id}");
                Close();
            }
            catch (Exception ex)
            {
                ShowError(Loc.S("Errore durante l'avvio di UOSteam: ", "Error while starting UOSteam: ") + ex.Message, ex);
            }
        }

        private async void CheckForUpdates()
        {
            _updateButton.Enabled = false;
            _statusLabel.ForeColor = Theme.TextMuted;
            _statusLabel.Text = Loc.S("Controllo aggiornamenti…", "Checking for updates…");

            try
            {
                UpdateCheckResult? info = await LauncherUpdater.CheckForUpdatesAsync(_settings.EffectiveClientVersion);
                if (info == null)
                {
                    ShowError(Loc.S(
                        "Impossibile controllare gli aggiornamenti su GitHub.",
                        "Unable to check for updates on GitHub."));
                    return;
                }

                if (!info.HasAnyUpdate)
                {
                    SetUpdateAvailable(false);
                    _statusLabel.ForeColor = Theme.SectionGreen;
                    _statusLabel.Text = Loc.S(
                        $"Sei già aggiornato (v{LauncherManifest.LauncherVersion}).",
                        $"You are up to date (v{LauncherManifest.LauncherVersion}).");
                    ThemedMessageDialog.ShowInfo(
                        this,
                        Loc.S("Aggiornamento", "Update"),
                        Loc.S(
                            $"Launcher e client sono aggiornati (v{LauncherManifest.LauncherVersion}).",
                            $"Launcher and client are up to date (v{LauncherManifest.LauncherVersion})."));
                    return;
                }

                SetUpdateAvailable(true);

                if (!ThemedMessageDialog.ShowConfirm(
                        this,
                        Loc.S("Aggiornamento disponibile", "Update available"),
                        BuildUpdateDetails(info),
                        Loc.S("Aggiorna", "Update"),
                        Loc.S("Annulla", "Cancel")))
                {
                    _statusLabel.Text = "";
                    return;
                }

                if (info.NeedsClientUpdate &&
                    !string.IsNullOrEmpty(info.ClientDownloadUrl) &&
                    !string.IsNullOrEmpty(info.ClientPackageFileName))
                {
                    using var clientForm = DownloadProgressForm.ForClientRuntimeUpdate(
                        info.ClientDownloadUrl,
                        info.ClientPackageFileName);
                    if (clientForm.ShowDialog(this) != DialogResult.OK)
                    {
                        ShowError(Loc.S("Aggiornamento client non riuscito.", "Client update failed."));
                        return;
                    }

                    string? clientVersion = LauncherUpdater.ParseVersionFromPackageName(info.ClientPackageFileName)
                        ?? info.LatestVersion;
                    MarkClientUpdated(clientVersion);
                    _clientPathBox.Text = DetectDefaultClient();
                    UpdateAssistantUi();
                }

                if (info.NeedsLauncherUpdate &&
                    !string.IsNullOrEmpty(info.LauncherDownloadUrl) &&
                    !string.IsNullOrEmpty(info.LauncherPackageFileName))
                {
                    using var launcherForm = DownloadProgressForm.ForLauncherUpdate(
                        info.LauncherDownloadUrl,
                        info.LauncherPackageFileName);
                    launcherForm.ShowDialog(this);
                    Environment.Exit(0);
                    return;
                }

                SetUpdateAvailable(false);
                _statusLabel.ForeColor = Theme.SectionGreen;
                _statusLabel.Text = Loc.S("Aggiornamento completato.", "Update completed.");
                RefreshWindowTitle();
            }
            catch (Exception ex)
            {
                ShowError(Loc.S("Errore durante l'aggiornamento: ", "Error during update: ") + ex.Message, ex);
            }
            finally
            {
                _updateButton.Enabled = true;
            }
        }

        private void SaveSettings()
        {
            _settings.Assistant = SelectedAssistant;
            _settings.UoDirectory = _uoPathBox.Text.Trim();
            _settings.ClientPath = DetectDefaultClient();
            _settings.ShardIp = _ipBox.Text.Trim();
            _settings.ShardPort = (int)_portBox.Value;
            _settings.Encryption = _encryptionCheck.Checked ? 1 : 0;
            _settings.SelectedServer = _serverCombo.SelectedItem as string ?? _settings.SelectedServer;

            switch (SelectedAssistant)
            {
                case "ClassicAssist":
                    _settings.ClassicAssistPath = _assistantPathBox.Text.Trim();
                    break;
                case "Razor Enhanced":
                    _settings.RazorPath = _assistantPathBox.Text.Trim();
                    break;
                case "Orion":
                    _settings.OrionPath = _assistantPathBox.Text.Trim();
                    break;
                case "UOSteam":
                    _settings.UOSteamPath = _assistantPathBox.Text.Trim();
                    break;
            }

            _settings.Save();
        }

        private void Launch()
        {
            if (SelectedAssistant == "UOSteam")
            {
                LaunchUOSteam();
                return;
            }

            if (SelectedAssistant == "Orion")
            {
                LaunchOrion();
                return;
            }

            if (!ClientRuntimeDownloader.IsInstalled())
            {
                using var bootstrapForm = DownloadProgressForm.ForClientRuntime();
                if (bootstrapForm.ShowDialog(this) != DialogResult.OK)
                {
                    ShowError(Loc.S("Componenti di gioco non installati.", "Game components not installed."));
                    return;
                }

                _clientPathBox.Text = DetectDefaultClient();
                UpdateAssistantUi();
            }

            string clientPath = DetectDefaultClient();
            if (string.IsNullOrWhiteSpace(clientPath) && !string.IsNullOrWhiteSpace(_clientPathBox.Text))
                clientPath = _clientPathBox.Text.Trim().Trim('"');
            string uoPath = _uoPathBox.Text.Trim().Trim('"');
            string effectiveClient = ResolveClientExecutable(clientPath, SelectedAssistant);

            if (SelectedAssistant == "Razor Enhanced")
            {
                string clientDir = Path.GetDirectoryName(effectiveClient)!;
                string nativeCuo = Path.Combine(clientDir, "cuo.dll");

                if (!File.Exists(effectiveClient) || !IsNativeCuoDll(nativeCuo))
                {
                    ShowError(Loc.S(
                        "Razor Enhanced richiede ClassicUO.exe con cuo.dll nativo (come Dust765).\n" +
                        "Il client gestito cuo-modded.exe non può caricare Razor.\n\n" +
                        "Serve il pacchetto unificato: ClassicUO.exe + cuo.dll moddato nativo nella cartella Client.",
                        "Razor Enhanced requires ClassicUO.exe with native cuo.dll (Dust765-style).\n" +
                        "The managed cuo-modded.exe client cannot load Razor.\n\n" +
                        "You need the unified package: ClassicUO.exe + native modded cuo.dll in the Client folder."
                    ));
                    return;
                }
            }
            else if (!File.Exists(effectiveClient))
            {
                ShowError(Loc.S(
                    "Client non trovato. Seleziona ClassicUO.exe o cuo-modded.exe nella cartella Client.",
                    "Client not found. Select ClassicUO.exe or cuo-modded.exe in the Client folder."));
                return;
            }

            if (!Directory.Exists(uoPath) || !File.Exists(Path.Combine(uoPath, "tiledata.mul")))
            {
                ShowError(Loc.S(
                    "Cartella di Ultima Online non valida (manca tiledata.mul).",
                    "Invalid Ultima Online folder (tiledata.mul is missing)."));
                return;
            }

            string? assistantDll = null;

            if (SelectedAssistant != "Nessuno")
            {
                assistantDll = ResolveAssistantDll();

                if (assistantDll == null)
                {
                    ShowError(Loc.S($"Percorso di {SelectedAssistant} non valido.", $"Invalid {SelectedAssistant} path."));
                    return;
                }

                if (SelectedAssistant != "Razor Enhanced" && !Is64BitCompatible(assistantDll))
                {
                    ShowError(
                        Path.GetFileName(assistantDll) +
                        Loc.S(
                            " è a 32 bit (x86) e non può essere caricato dal client a 64 bit.\n" +
                            "Usa Razor Enhanced (bootstrap + cuo.dll nativo) o ClassicAssist.",
                            " is 32-bit (x86) and cannot be loaded by the 64-bit client.\n" +
                            "Use Razor Enhanced (bootstrap + native cuo.dll) or ClassicAssist.")
                    );
                    return;
                }
            }

            SaveSettings();

            var psi = new ProcessStartInfo
            {
                FileName = effectiveClient,
                WorkingDirectory = Path.GetDirectoryName(effectiveClient)!,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            psi.ArgumentList.Add("-ip");
            psi.ArgumentList.Add(_ipBox.Text.Trim());
            psi.ArgumentList.Add("-port");
            psi.ArgumentList.Add(((int)_portBox.Value).ToString());
            psi.ArgumentList.Add("-uopath");
            psi.ArgumentList.Add(uoPath);
            psi.ArgumentList.Add("-encryption");
            psi.ArgumentList.Add(_encryptionCheck.Checked ? "1" : "0");

            // always pass -plugins so a stale value in the client settings.json never wins
            psi.ArgumentList.Add("-plugins");
            psi.ArgumentList.Add(assistantDll ?? "");

            string args = string.Join(" ", psi.ArgumentList);
            LauncherLog.Info(
                $"Launch assistant={SelectedAssistant} exe={effectiveClient} plugin={assistantDll ?? "(none)"} args={args}"
            );

            try
            {
                Process? process = Process.Start(psi);
                if (process == null)
                {
                    ShowError(Loc.S(
                        "Impossibile avviare il client (Process.Start ha restituito null).",
                        "Unable to start the client (Process.Start returned null)."));
                    return;
                }

                _statusLabel.ForeColor = Theme.TextMuted;
                _statusLabel.Text = SelectedAssistant == "Nessuno"
                    ? Loc.S("Client avviato.", "Client started.")
                    : Loc.S($"Client avviato con {SelectedAssistant}.", $"Client started with {SelectedAssistant}.");

                LauncherLog.Info($"Client avviato PID={process.Id}");
                Close();
            }
            catch (Exception ex)
            {
                ShowError(Loc.S("Errore durante l'avvio: ", "Error while starting: ") + ex.Message, ex);
            }
        }

        // Returns false only for images the x64 client can't load: native x86 binaries
        // and managed assemblies marked 32-bit required (e.g. RazorEnhanced.exe).
        // AnyCPU managed dlls also report Machine=I386 but are fine.
        private static bool Is64BitCompatible(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                using var pe = new System.Reflection.PortableExecutable.PEReader(fs);

                var machine = pe.PEHeaders.CoffHeader.Machine;

                if (machine != System.Reflection.PortableExecutable.Machine.I386)
                    return true;

                var cor = pe.PEHeaders.CorHeader;

                if (cor == null)
                    return false; // native 32-bit binary

                return (cor.Flags & System.Reflection.PortableExecutable.CorFlags.Requires32Bit) == 0;
            }
            catch
            {
                return true; // unreadable header: let the client try
            }
        }

        private void ShowError(string message, Exception? ex = null)
        {
            LauncherLog.Error(message, ex);
            _statusLabel.ForeColor = Theme.Error;
            _statusLabel.Text = message;

            string dialogText = message;
            if (!string.IsNullOrEmpty(LauncherLog.LastError) && LauncherLog.LastError != message)
            {
                dialogText += "\n\n" + LauncherLog.LastError;
            }

            dialogText += $"\n\nLog: {LauncherLog.LogPath}";

            MessageBox.Show(
                this,
                dialogText,
                "UODreams Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
