using System;
using System.Collections.Generic;
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

        private string _selectedAssistant = "Nessuno";
        private readonly Dictionary<string, OutlineAssistantButton> _assistantPills = new(StringComparer.OrdinalIgnoreCase);
        private Panel _assistantPathPanel = null!;
        private Panel _assistantActionPanel = null!;
        private PathEllipsisTextBox _assistantPathBox = null!;
        private BrowseDotsButton _assistantBrowseButton = null!;
        private TextBox _clientPathBox = null!;
        private PathEllipsisTextBox _uoPathBox = null!;
        private TextBox _ipBox = null!;
        private NumericUpDown _portBox = null!;
        private ThemedComboBox _serverCombo = null!;
        private bool _suppressServerComboEvents;
        private CheckBox _encryptionCheck = null!;
        private Label _statusLabel = null!;
        private ThemedButton _downloadUoButton = null!;
        private Label _uoHintLabel = null!;
        private ThemedButton _downloadAssistantButton = null!;
        private Label _assistantRazorInfoLabel = null!;
        private Label _assistantHintLabel = null!;
        private TrashIconButton _uoTrashButton = null!;
        private TrashIconButton _assistantTrashButton = null!;

        // References kept for language switching.
        private ThemedButton _langButton = null!;
        private ThemedButton _updateButton = null!;
        private ThemedButton _buyCoffeeButton = null!;
        private ThemedButton _launchButton = null!;
        private BrowseDotsButton _uoBrowseButton = null!;
        private ThemedButton _editServerButton = null!;
        private Label _assistantSectionLabel = null!;
        private Label _uoSectionLabel = null!;
        private Label _shardSectionLabel = null!;
        private FooterLinkButton _registerBtn = null!;
        private Label _enhancedMapLink = null!;
        private CheckBox _enhancedMapAutoOpenCheck = null!;
        private ThemedContextMenu? _enhancedMapMenu;
        private int _enhancedMapRowY;
        private bool _updateAvailable;
        private bool _suppressAssistantPathEvents;
        private CardPanel _assistantCard = null!;
        private const int UoCardPadH = 18;
        private const int UoPathRowY = 40;

        public MainForm()
        {
            Loc.Lang = _settings.Language == "en" ? "en" : "it";
            LoadWindowIcon();
            BuildUi();
            LoadFromSettings();
            _settings.SyncInstalledClientVersionFromRuntime();
            RefreshWindowTitle();

            Shown += OnMainFormShown;
        }

        private void OnMainFormShown(object? sender, EventArgs e)
        {
            RefreshAssistantSectionLayout();
            BeginInvoke(RefreshAssistantSectionLayout);
            _ = CheckForUpdatesOnStartupAsync();
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

        private static Image? LoadPillIcon(string fileName)
        {
            try
            {
                using Stream? s = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream($"ClassicUO.Launcher.Custom.Resources.{fileName}");
                return s != null ? Image.FromStream(s) : null;
            }
            catch
            {
                return null;
            }
        }

        private static Image? LoadCoffeeIcon()
        {
            Image? src = LoadPillIcon("coffee.png");
            if (src == null)
            {
                return null;
            }

            var bmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                Theme.DrawRecoloredImage(g, src, new Rectangle(0, 0, src.Width, src.Height), Color.FromArgb(196, 118, 58));
            }

            src.Dispose();
            return bmp;
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
            const int bottomMargin = 10;
            int margin = 22;
            int width = formWidth - margin * 2;
            int y = 16;

            // ----- UODreams banner -----
            Image? logo = LoadLogo();

            if (logo != null)
            {
                const int bannerH = 148;
                var logoBox = new PictureBox
                {
                    Image = logo,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Bounds = new Rectangle(margin, y, width, bannerH)
                };
                Controls.Add(logoBox);
                y += bannerH + 4;
            }

            // ----- Enhanced Map (right-aligned below banner) -----
            _enhancedMapRowY = y;
            _enhancedMapLink = new Label
            {
                Text = Loc.S("🌍 ENHANCED MAP", "🌍 ENHANCED MAP"),
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Theme.SectionGreen,
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            _enhancedMapLink.Click += (_, _) => ShowEnhancedMapMenu();
            _enhancedMapLink.MouseEnter += (_, _) => _enhancedMapLink.ForeColor = Color.FromArgb(72, 228, 154);
            _enhancedMapLink.MouseLeave += (_, _) => _enhancedMapLink.ForeColor = Theme.SectionGreen;
            Controls.Add(_enhancedMapLink);

            _enhancedMapAutoOpenCheck = new CheckBox
            {
                Text = Loc.S("apri mappa automaticamente", "autoopen map"),
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            _enhancedMapAutoOpenCheck.CheckedChanged += (_, _) =>
            {
                _settings.EnhancedMapAutoOpen = _enhancedMapAutoOpenCheck.Checked;
                _settings.Save();
            };
            Controls.Add(_enhancedMapAutoOpenCheck);
            LayoutEnhancedMapControls(margin, width, _enhancedMapRowY);
            y += 50;

            // ----- Card 1: assistant -----
            const int pathRowH = 30;
            const int actionRowH = Theme.PrimaryButtonHeight;
            const int actionGap = Theme.SectionRowGap;
            const int cardPadH = 18;
            const int cardPadTop = 14;
            const int assistantCardHeight = 204;
            _assistantCard = new CardPanel { Bounds = new Rectangle(margin, y, width, assistantCardHeight) };
            Controls.Add(_assistantCard);
            var assistantCard = _assistantCard;
            assistantCard.Resize += (_, _) => LayoutAssistantHintLabels();

            int cw = width - cardPadH * 2;
            int ay = cardPadTop;

            _assistantSectionLabel = SectionLabel(Loc.S("Seleziona l'assistant", "Select assistant"), cardPadH, ay, cw);
            assistantCard.Controls.Add(_assistantSectionLabel);
            ay += 20 + actionGap;

            const int pillGap = 6;
            int pillW = (cw - pillGap * 3) / 4;

            var pillRow = new Panel
            {
                Bounds = new Rectangle(cardPadH, ay, cw, Theme.OutlineAssistantHeight),
                BackColor = Color.Transparent
            };
            assistantCard.Controls.Add(pillRow);

            var pillDefs = new (string Key, string Label, string IconFile, Color Color)[]
            {
                ("Razor Enhanced", GetRazorPillLabel(), "razor.png", Theme.PillRazor),
                ("ClassicAssist", "Classic Assist", "classicassist.png", Theme.PillClassicAssist),
                ("UOSteam", "UOSteam", "uosteam.png", Theme.PillUOSteam),
                ("Orion", "Orion", "orion.png", Theme.PillOrion)
            };

            for (int i = 0; i < pillDefs.Length; i++)
            {
                var def = pillDefs[i];
                var pill = new OutlineAssistantButton
                {
                    Text = def.Label,
                    IconImage = LoadPillIcon(def.IconFile),
                    AccentColor = def.Color,
                    Bounds = new Rectangle(i * (pillW + pillGap), 0, pillW, Theme.OutlineAssistantHeight),
                    Tag = def.Key
                };
                pill.Click += (_, _) => SelectAssistant((string)pill.Tag!);
                _assistantPills[def.Key] = pill;
                pillRow.Controls.Add(pill);
            }
            ay += Theme.OutlineAssistantHeight + actionGap;

            (_assistantPathPanel, _assistantPathBox, _assistantBrowseButton, _assistantTrashButton) =
                CreatePathRow(BrowseAssistant, ClearAssistantPath);
            _assistantPathPanel.Visible = false;
            _assistantPathPanel.Bounds = new Rectangle(cardPadH, ay, cw, pathRowH);
            _assistantPathPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            assistantCard.Controls.Add(_assistantPathPanel);
            ay += pathRowH + 6;

            _assistantRazorInfoLabel = new Label
            {
                Text = "",
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                MaximumSize = new Size(cw, 0),
                Location = new Point(cardPadH, ay),
                Visible = false
            };
            assistantCard.Controls.Add(_assistantRazorInfoLabel);

            _assistantHintLabel = new Label
            {
                Text = "",
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                MaximumSize = new Size(cw, 0),
                Location = new Point(cardPadH, ay),
                Visible = false
            };
            assistantCard.Controls.Add(_assistantHintLabel);

            _assistantActionPanel = new Panel
            {
                Bounds = new Rectangle(cardPadH, ay, cw, actionRowH),
                BackColor = Color.Transparent,
                Visible = false
            };
            _assistantActionPanel.Resize += (_, _) => LayoutAssistantDownloadButton();
            assistantCard.Controls.Add(_assistantActionPanel);

            _downloadAssistantButton = new ThemedButton
            {
                Text = GetAssistantDownloadButtonText("Razor Enhanced"),
                AutoSize = true
            };
            Theme.ApplyPrimaryStyle(_downloadAssistantButton);
            _downloadAssistantButton.Click += (_, _) => DownloadAssistant();
            _assistantActionPanel.Controls.Add(_downloadAssistantButton);

            _assistantPathBox.TextChanged += (_, _) =>
            {
                if (!_suppressAssistantPathEvents)
                {
                    UpdateAssistantDownloadUi();
                }
            };
            y += assistantCard.Height + 10;

            // ----- Card 2: Ultima Online -----
            int cx = 18;
            var pathsCard = new CardPanel { Bounds = new Rectangle(margin, y, width, 132) };
            Controls.Add(pathsCard);
            int cy = 14;

            _uoSectionLabel = SectionLabel(Loc.S("Client Ultima Online", "Ultima Online client"), cx, cy, cw);
            pathsCard.Controls.Add(_uoSectionLabel);
            cy += 26;

            (_, _uoPathBox, _uoBrowseButton, _uoTrashButton) = PathRow(pathsCard, cx, cy, cw, BrowseUoFolder, ClearUoPath);
            _uoPathBox.TextChanged += (_, _) => UpdateUoDownloadUi();
            cy += pathRowH + actionGap;

            _downloadUoButton = new ThemedButton
            {
                Text = Loc.S("⬇ Scarica UODreams", "⬇ Download UODreams"),
                AutoSize = true,
                Visible = false
            };
            Theme.ApplyPrimaryStyle(_downloadUoButton);
            _downloadUoButton.Click += (_, _) => DownloadUoClient();
            pathsCard.Controls.Add(_downloadUoButton);
            cy += actionRowH + 6;

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
            y += pathsCard.Height + 10;

            // ----- Card 3: shard -----
            var shardCard = new CardPanel { Bounds = new Rectangle(margin, y, width, 82) };
            Controls.Add(shardCard);
            cy = 14;

            _shardSectionLabel = SectionLabel("SHARD - SERVER", cx, cy, cw);
            shardCard.Controls.Add(_shardSectionLabel);
            cy += 28;

            _serverCombo = new ThemedComboBox
            {
                Bounds = new Rectangle(cx, cy, cw - 84, 30)
            };
            _serverCombo.SelectedIndexChanged += (_, _) => OnServerComboChanged();
            shardCard.Controls.Add(_serverCombo);

            _editServerButton = new ThemedButton
            {
                Text = "✎",
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                Bounds = new Rectangle(cx + cw - 76, cy, 38, 30)
            };
            _editServerButton.Click += (_, _) => EditServer();
            shardCard.Controls.Add(_editServerButton);

            var addServerButton = new ThemedButton
            {
                Text = "+",
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                Bounds = new Rectangle(cx + cw - 34, cy, 34, 30)
            };
            addServerButton.Click += (_, _) => AddServer();
            shardCard.Controls.Add(addServerButton);

            _ipBox = new TextBox { Visible = false };
            _portBox = new NumericUpDown { Minimum = 1, Maximum = 65535, Visible = false };
            _encryptionCheck = new CheckBox { Visible = false };
            Controls.Add(_ipBox);
            Controls.Add(_portBox);
            Controls.Add(_encryptionCheck);
            y += shardCard.Height + 14;

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
            y += 52;

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
            y += 26;

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
            const int toolbarBtnH = Theme.ToolbarButtonHeight;
            _buyCoffeeButton = new ThemedButton
            {
                Text = Loc.S("Buy me a coffee", "Buy me a coffee"),
                IconImage = LoadCoffeeIcon(),
                IconSize = 14,
                AutoSize = true,
                MinimumSize = new Size(0, toolbarBtnH)
            };
            Theme.ApplyToolbarPrimaryStyle(_buyCoffeeButton);
            int coffeeBtnW = _buyCoffeeButton.PreferredSize.Width;
            _buyCoffeeButton.Bounds = new Rectangle(margin, 12, coffeeBtnW, toolbarBtnH);
            _buyCoffeeButton.Click += (_, _) => OpenUrl("https://www.paypal.com/donate/?hosted_button_id=ZUGYHHC2L7ZXC");
            Controls.Add(_buyCoffeeButton);

            _updateButton = new ThemedButton
            {
                Text = Loc.S("⬇ Aggiorna", "⬇ Update"),
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Theme.Text,
                Bounds = new Rectangle(formWidth - margin - 148, 12, 76, toolbarBtnH)
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
                Bounds = new Rectangle(formWidth - margin - 66, 12, 66, toolbarBtnH)
            };
            _langButton.Click += (_, _) => ToggleLanguage();
            Controls.Add(_langButton);
            _buyCoffeeButton.BringToFront();
            _updateButton.BringToFront();
            _langButton.BringToFront();

            ClientSize = new Size(formWidth, y + footerH + bottomMargin);
        }

        private void RefreshWindowTitle()
        {
            string launcherVer = LauncherManifest.RuntimeLauncherVersion;
            string clientVer = _settings.EffectiveClientVersion;
            string title = LauncherManifest.ProductTitle;
            Text = string.Equals(launcherVer, clientVer, StringComparison.OrdinalIgnoreCase)
                ? $"{title} v{launcherVer}"
                : $"{title} v{launcherVer} · client v{clientVer}";
        }

        private void SetUpdateAvailable(bool available)
        {
            _updateAvailable = available;
            _updateButton.HighlightAsUpdate = available;
            _updateButton.Text = available
                ? Loc.S("★ Aggiorna", "★ Update")
                : Loc.S("⬇ Aggiorna", "⬇ Update");
            _updateButton.Invalidate();
        }

        private async Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                _settings.SyncInstalledClientVersionFromRuntime();
                UpdateCheckResult? info = await LauncherUpdater.CheckForUpdatesAsync(_settings.EffectiveClientVersion);
                if (!IsDisposed)
                {
                    BeginInvoke(() => SetUpdateAvailable(info?.HasAnyUpdate == true));
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

        private void ClearAssistantPath()
        {
            string current = SelectedAssistant;
            if (string.Equals(current, "Nessuno", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _settings.ClearAssistantPath(current);
            _settings.Save();
            _assistantPathBox.Text = "";
            UpdateAssistantDownloadUi();
        }

        private void ClearAssistantSelection()
        {
            string previous = SelectedAssistant;
            if (!string.Equals(previous, "Nessuno", StringComparison.OrdinalIgnoreCase))
            {
                _settings.ClearAssistantPath(previous);
            }

            _settings.Assistant = "Nessuno";
            _settings.Save();
            SetSelectedAssistant("Nessuno");
            _assistantPathBox.Text = "";
            UpdateAssistantUi();
            UpdateAssistantDownloadUi();
        }

        private void ClearUoPath()
        {
            _uoPathBox.Text = "";
            _settings.UoDirectory = "";
            _settings.Save();
            UpdateUoDownloadUi();
        }

        private static string LangButtonText() => Loc.IsEn ? "🌐 EN" : "🌐 IT";

        private static string GetRazorPillLabel() => Loc.S("Razor\nEnhanced", "Razor\nEnhanced");

        private static string GetAssistantDownloadButtonText(string assistant) => assistant switch
        {
            "Razor Enhanced" => Loc.S("Download Razor Enhanced", "Download Razor Enhanced"),
            "ClassicAssist" => Loc.S("Download Classic Assist", "Download Classic Assist"),
            "UOSteam" => Loc.S("Download UOSteam", "Download UOSteam"),
            "Orion" => Loc.S("Download Orion", "Download Orion"),
            _ => Loc.S("Download", "Download")
        };

        private static string GetRazorBundledInfoText() => Loc.S(
            "La versione di Razor Enhanced modded è inclusa nel launcher.",
            "The modded Razor Enhanced version is included in the launcher.");

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

            if (_assistantPills.TryGetValue("Razor Enhanced", out OutlineAssistantButton? razorPill))
            {
                razorPill.Text = GetRazorPillLabel();
            }

            _assistantSectionLabel.Text = Loc.S("Seleziona l'assistant", "Select assistant").ToUpperInvariant();
            _uoSectionLabel.Text = Loc.S("Client Ultima Online", "Ultima Online client").ToUpperInvariant();
            _shardSectionLabel.Text = "SHARD - SERVER";

            _downloadAssistantButton.Text = GetAssistantDownloadButtonText(SelectedAssistant);
            _downloadUoButton.Text = Loc.S("⬇ Scarica UODreams", "⬇ Download UODreams");
            _editServerButton.Text = "✎";
            _buyCoffeeButton.Text = Loc.S("Buy me a coffee", "Buy me a coffee");
            _updateButton.Text = _updateAvailable
                ? Loc.S("★ Aggiorna", "★ Update")
                : Loc.S("⬇ Aggiorna", "⬇ Update");
            _launchButton.Text = Loc.S("AVVIA", "START");
            _registerBtn.Text = Loc.S("Registrati gratis", "Register for free");
            _enhancedMapLink.Text = Loc.S("🌍 ENHANCED MAP", "🌍 ENHANCED MAP");
            _enhancedMapAutoOpenCheck.Text = Loc.S("apri mappa automaticamente", "autoopen map");
            LayoutEnhancedMapControls(24, 620 - 48, _enhancedMapRowY);

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

        private static (Panel RowPanel, PathEllipsisTextBox Box, BrowseDotsButton Browse, TrashIconButton Trash) CreatePathRow(
            Action browse, Action clear)
        {
            const int iconW = 24;
            const int gap = 4;
            const int rowH = 30;

            var row = new Panel
            {
                Height = rowH,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };

            var panel = new InputPanel { BackColor = Color.Transparent };
            var box = new PathEllipsisTextBox { Dock = DockStyle.Fill };

            var browseButton = new BrowseDotsButton();
            browseButton.Click += (_, _) => browse();

            var trashButton = new TrashIconButton();
            trashButton.Click += (_, _) => clear();

            panel.Controls.Add(box);
            row.Controls.Add(panel);
            row.Controls.Add(browseButton);
            row.Controls.Add(trashButton);

            void layoutRow()
            {
                int h = row.ClientSize.Height;
                int w = row.ClientSize.Width;
                trashButton.SetBounds(w - iconW, 0, iconW, h);
                browseButton.SetBounds(w - iconW * 2 - gap, 0, iconW, h);
                panel.SetBounds(0, 0, Math.Max(0, w - iconW * 2 - gap * 2), h);
            }

            row.Resize += (_, _) => layoutRow();
            row.HandleCreated += (_, _) => layoutRow();

            return (row, box, browseButton, trashButton);
        }

        private static (Panel RowPanel, PathEllipsisTextBox Box, BrowseDotsButton Browse, TrashIconButton Trash) PathRow(
            Control parent, int x, int y, int width, Action browse, Action clear)
        {
            var created = CreatePathRow(browse, clear);
            created.RowPanel.Bounds = new Rectangle(x, y, width, 30);
            created.RowPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            parent.Controls.Add(created.RowPanel);
            return created;
        }

        private void SelectAssistant(string assistantKey)
        {
            if (string.Equals(_selectedAssistant, assistantKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (assistantKey == "UOSteam")
            {
                ShowUOSteamNotice();
            }

            SetSelectedAssistant(assistantKey);
            UpdateAssistantUi();
            UpdateAssistantDownloadUi();
        }

        private void SetSelectedAssistant(string assistantKey)
        {
            _selectedAssistant = assistantKey;
            foreach (var pair in _assistantPills)
            {
                pair.Value.IsSelected = string.Equals(pair.Key, assistantKey, StringComparison.OrdinalIgnoreCase);
                pair.Value.Invalidate();
            }

            bool hasAssistant = assistantKey != "Nessuno";
            _assistantPathPanel.Visible = hasAssistant;
        }

        private static string GetBundledRazorPath()
        {
            string? bundled = AssistantPaths.DetectBundledRazorExe();
            if (!string.IsNullOrEmpty(bundled))
            {
                return bundled;
            }

            return "";
        }

        private string ResolveRazorPathForUi(bool canAutoDetect)
        {
            string saved = _settings.RazorPath.Trim();
            if (!string.IsNullOrEmpty(saved) && !AssistantPaths.IsLegacyClientPluginsRazorPath(saved))
            {
                return saved;
            }

            if (!LauncherManifest.IsPvpEdition)
            {
                return "";
            }

            string bundledRazor = GetBundledRazorPath();
            if (!string.IsNullOrEmpty(bundledRazor))
            {
                return bundledRazor;
            }

            if (canAutoDetect ||
                string.IsNullOrEmpty(saved) ||
                AssistantPaths.IsLegacyClientPluginsRazorPath(saved))
            {
                string defaultPath = AssistantPaths.GetDefaultRazorExePath();
                if (File.Exists(defaultPath))
                {
                    return defaultPath;
                }

                string? legacyRoot = ClientRuntimeDownloader.DetectRazorEnhancedPathInPlugins();
                if (legacyRoot != null)
                {
                    string legacyExe = Path.Combine(legacyRoot, "RazorEnhanced.exe");
                    if (File.Exists(legacyExe))
                    {
                        return legacyExe;
                    }
                }

                return defaultPath;
            }

            return "";
        }

        private static bool IsBundledRazorAvailable() =>
            LauncherManifest.IsPvpEdition &&
            !string.IsNullOrEmpty(GetBundledRazorPath()) &&
            File.Exists(GetBundledRazorPath());

        private void LoadFromSettings()
        {
            string savedAssistant = _settings.Assistant;
            if (savedAssistant == "Nessuno" || string.IsNullOrEmpty(savedAssistant))
            {
                SetSelectedAssistant("Nessuno");
            }
            else if (savedAssistant == "Razor")
            {
                SetSelectedAssistant("Razor Enhanced");
            }
            else if (_assistantPills.ContainsKey(savedAssistant))
            {
                SetSelectedAssistant(savedAssistant);
            }
            else
            {
                SetSelectedAssistant("Nessuno");
            }

            _uoPathBox.Text = _settings.UoDirectory;
            _ipBox.Text = _settings.ShardIp;
            _portBox.Value = Math.Min(Math.Max(_settings.ShardPort, 1), 65535);
            _encryptionCheck.Checked = _settings.Encryption != 0;
            _enhancedMapAutoOpenCheck.Checked = _settings.EnhancedMapAutoOpen;
            RefreshServerCombo();

            _clientPathBox.Text = !string.IsNullOrEmpty(_settings.ClientPath) && File.Exists(_settings.ClientPath)
                ? _settings.ClientPath
                : DetectDefaultClient();

            UpdateAssistantUi();
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

        private void EditServer()
        {
            if (_serverCombo.SelectedItem is not string name)
            {
                return;
            }

            ShardServer? server = _settings.Servers.Find(s => s.Name == name);
            if (server == null)
            {
                return;
            }

            var edited = ShardServerDialog.Edit(this, server, _settings.Encryption);
            if (edited == null)
            {
                return;
            }

            ShardServer updated = edited.Value.Server!;
            server.Name = updated.Name;
            server.Ip = updated.Ip;
            server.Port = updated.Port;
            _settings.Encryption = edited.Value.Encryption;
            _settings.SelectedServer = server.Name;
            _settings.Save();

            _ipBox.Text = server.Ip;
            _portBox.Value = Math.Min(Math.Max(server.Port, 1), 65535);
            _encryptionCheck.Checked = _settings.Encryption != 0;
            RefreshServerCombo();
        }

        private void AddServer()
        {
            var added = ShardServerDialog.PromptAdd(this);
            if (added == null)
            {
                return;
            }

            ShardServer server = added.Value.Server!;
            ShardServer? existing = _settings.Servers.Find(s =>
                string.Equals(s.Name, server.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Ip = server.Ip;
                existing.Port = server.Port;
            }
            else
            {
                _settings.Servers.Add(server);
            }

            _settings.SelectedServer = server.Name;
            _settings.Encryption = added.Value.Encryption;
            _encryptionCheck.Checked = _settings.Encryption != 0;
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

            _uoBrowseButton.Visible = true;
            LayoutUoClientSection(valid);
        }

        private void LayoutUoClientSection(bool clientReady)
        {
            Control? card = _uoSectionLabel.Parent;
            int contentW = card?.ClientSize.Width > 0
                ? card.ClientSize.Width - UoCardPadH * 2
                : 576;

            if (clientReady)
            {
                _downloadUoButton.Visible = false;
                _uoHintLabel.Visible = true;
                _uoHintLabel.Text = Loc.S("✓ Client Ultima Online pronto.", "✓ Ultima Online client ready.");
                _uoHintLabel.ForeColor = Theme.SectionGreen;
                _uoHintLabel.Location = new Point(UoCardPadH, UoPathRowY + 30 + 4);
            }
            else
            {
                _uoHintLabel.Visible = false;
                _uoHintLabel.Text = "";
                _downloadUoButton.Visible = true;
                _downloadUoButton.Text = Loc.S("⬇ Scarica UODreams", "⬇ Download UODreams");
                int buttonW = _downloadUoButton.PreferredSize.Width;
                int buttonH = Theme.PrimaryButtonHeight;
                int buttonX = UoCardPadH + Math.Max(0, (contentW - buttonW) / 2);
                int buttonY = UoPathRowY + 30 + Theme.SectionRowGap;
                _downloadUoButton.Bounds = new Rectangle(buttonX, buttonY, buttonW, buttonH);
            }
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

        private string SelectedAssistant => _selectedAssistant;

        private void UpdateAssistantUi(bool allowAutoDetect = true)
        {
            bool hasAssistant = SelectedAssistant != "Nessuno";
            _assistantPathPanel.Visible = hasAssistant;
            _assistantActionPanel.Visible = hasAssistant;

            if (!hasAssistant)
            {
                UpdateAssistantDownloadUi();
                return;
            }

            bool canAutoDetect = allowAutoDetect &&
                !_settings.IsAssistantPathClearedByUser(SelectedAssistant) &&
                _settings.FirstRunCompleted;

            _suppressAssistantPathEvents = true;
            try
            {
                switch (SelectedAssistant)
                {
                    case "ClassicAssist":
                        _assistantPathBox.Text = _settings.ClassicAssistPath;
                        break;
                    case "Razor Enhanced":
                        _assistantPathBox.Text = ResolveRazorPathForUi(canAutoDetect);
                        if (!string.IsNullOrWhiteSpace(_assistantPathBox.Text) &&
                            File.Exists(_assistantPathBox.Text))
                        {
                            _settings.ClearAssistantPathClearedFlag("Razor Enhanced");
                        }
                        break;
                    case "Orion":
                        _assistantPathBox.Text = !string.IsNullOrWhiteSpace(_settings.OrionPath)
                            ? _settings.OrionPath
                            : canAutoDetect
                                ? ClientRuntimeDownloader.DetectOrionInstallRoot() ?? ""
                                : "";
                        break;
                    case "UOSteam":
                        _assistantPathBox.Text = !string.IsNullOrWhiteSpace(_settings.UOSteamPath)
                            ? _settings.UOSteamPath
                            : canAutoDetect
                                ? ClientRuntimeDownloader.DetectUOSteamExe() ?? ""
                                : "";
                        break;
                }
            }
            finally
            {
                _suppressAssistantPathEvents = false;
            }

            UpdateAssistantDownloadUi();
        }

        private void RefreshAssistantSectionLayout()
        {
            UpdateAssistantDownloadUi();
        }

        private void UpdateAssistantDownloadUi()
        {
            bool hasAssistant = SelectedAssistant != "Nessuno";

            if (SelectedAssistant == "Razor Enhanced" &&
                string.IsNullOrWhiteSpace(_assistantPathBox.Text) &&
                IsBundledRazorAvailable())
            {
                _assistantPathBox.Text = GetBundledRazorPath();
                _settings.ClearAssistantPathClearedFlag("Razor Enhanced");
            }

            bool isBundledRazor = SelectedAssistant == "Razor Enhanced" && IsBundledRazorAvailable();
            bool canDownload = hasAssistant &&
                AssistantDownloader.SupportsDownload(SelectedAssistant) &&
                !isBundledRazor;
            bool installed = hasAssistant &&
                AssistantDownloader.IsPluginValidAtPath(SelectedAssistant, _assistantPathBox.Text);

            bool showDownload = canDownload && !installed;

            _assistantPathPanel.Visible = hasAssistant;
            _downloadAssistantButton.Visible = showDownload;
            _assistantActionPanel.Visible = hasAssistant && showDownload;
            _assistantBrowseButton.Visible = hasAssistant && !isBundledRazor;
            _assistantHintLabel.Visible = hasAssistant;
            _assistantTrashButton.Visible = hasAssistant &&
                !string.IsNullOrWhiteSpace(_assistantPathBox.Text) &&
                !isBundledRazor;

            if (!hasAssistant)
            {
                _assistantRazorInfoLabel.Visible = false;
                _assistantRazorInfoLabel.Text = "";
                return;
            }

            if (showDownload)
            {
                _downloadAssistantButton.Text = GetAssistantDownloadButtonText(SelectedAssistant);
            }

            bool showRazorBundledInfo = LauncherManifest.IsPvpEdition &&
                SelectedAssistant == "Razor Enhanced" &&
                installed;
            _assistantRazorInfoLabel.Visible = showRazorBundledInfo;
            _assistantRazorInfoLabel.Text = showRazorBundledInfo ? GetRazorBundledInfoText() : "";
            _assistantRazorInfoLabel.ForeColor = Theme.TextMuted;

            if (installed)
            {
                _assistantHintLabel.Text = Loc.S(
                    "✓ Assistant caricato correttamente.",
                    "✓ Assistant loaded successfully.");
                _assistantHintLabel.ForeColor = Theme.SectionGreen;
            }
            else if (isBundledRazor)
            {
                _assistantHintLabel.Text = Loc.S(
                    "Razor Enhanced incluso nel Launcher PVP.",
                    "Razor Enhanced bundled with the PVP Launcher.");
                _assistantHintLabel.ForeColor = Theme.TextMuted;
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

            LayoutAssistantHintLabels();
            LayoutAssistantDownloadButton();
        }

        private void LayoutAssistantHintLabels()
        {
            if (_assistantPathPanel.Parent is not Control card)
            {
                return;
            }

            const int cardPadH = 18;
            int cw = Math.Max(0, card.ClientSize.Width - cardPadH * 2);
            if (cw <= 0)
            {
                return;
            }

            int y = _assistantPathPanel.Bottom + 6;

            if (_assistantRazorInfoLabel.Visible)
            {
                y = PlaceAutoSizeLabel(_assistantRazorInfoLabel, cardPadH, y, cw) + 4;
            }

            if (_assistantHintLabel.Visible)
            {
                y = PlaceAutoSizeLabel(_assistantHintLabel, cardPadH, y, cw);
            }

            if (_assistantActionPanel.Visible)
            {
                int buttonH = Theme.PrimaryButtonHeight;
                int innerBottom = card.ClientSize.Height - Theme.AssistantCardBottomPadding;
                int available = innerBottom - y;
                int actionY = y + Math.Max(Theme.SectionRowGap, (available - buttonH) / 2);
                _assistantActionPanel.Location = new Point(cardPadH, actionY);
                _assistantActionPanel.Height = buttonH;
            }
        }

        private static int PlaceAutoSizeLabel(Label label, int x, int y, int maxWidth)
        {
            label.MaximumSize = new Size(maxWidth, 0);
            label.Location = new Point(x, y);
            int height = label.GetPreferredSize(new Size(maxWidth, 0)).Height;
            return y + Math.Max(height, label.Height);
        }

        private void LayoutAssistantDownloadButton()
        {
            if (!_downloadAssistantButton.Visible || !_assistantActionPanel.Visible)
            {
                return;
            }

            int panelW = _assistantActionPanel.ClientSize.Width;
            if (panelW <= 0)
            {
                return;
            }

            int buttonW = _downloadAssistantButton.PreferredSize.Width;
            int buttonH = Theme.PrimaryButtonHeight;
            int buttonX = Math.Max(0, (panelW - buttonW) / 2);
            _downloadAssistantButton.Bounds = new Rectangle(buttonX, 0, buttonW, buttonH);
        }

        private void BrowseAssistant()
        {
            string assistant = SelectedAssistant;
            if (assistant == "Nessuno")
            {
                return;
            }

            using var dialog = new OpenFileDialog
            {
                Title = assistant switch
                {
                    "ClassicAssist" => Loc.S("Seleziona ClassicAssist.dll", "Select ClassicAssist.dll"),
                    "Orion" => Loc.S("Seleziona OrionLauncher64.exe", "Select OrionLauncher64.exe"),
                    "UOSteam" => Loc.S("Seleziona UOS.exe", "Select UOS.exe"),
                    _ => Loc.S("Seleziona RazorEnhanced.exe", "Select RazorEnhanced.exe")
                },
                Filter = GetAssistantBrowseFilter(assistant),
                CheckFileExists = true,
                ValidateNames = true,
                Multiselect = false
            };

            if (!string.IsNullOrEmpty(_assistantPathBox.Text))
            {
                try
                {
                    string? initial = _assistantPathBox.Text.Trim().Trim('"');
                    if (File.Exists(initial))
                    {
                        dialog.InitialDirectory = Path.GetDirectoryName(initial);
                        dialog.FileName = Path.GetFileName(initial);
                    }
                    else if (Directory.Exists(initial))
                    {
                        dialog.InitialDirectory = initial;
                    }
                }
                catch { }
            }

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (!IsValidAssistantBrowseSelection(assistant, dialog.FileName))
            {
                ThemedMessageDialog.ShowInfo(
                    this,
                    Loc.S("Percorso non valido", "Invalid path"),
                    assistant switch
                    {
                        "ClassicAssist" => Loc.S(
                            "Seleziona il file ClassicAssist.dll.",
                            "Select the ClassicAssist.dll file."),
                        "Orion" => Loc.S(
                            "Seleziona OrionLauncher64.exe.",
                            "Select OrionLauncher64.exe."),
                        "UOSteam" => Loc.S(
                            "Seleziona UOS.exe.",
                            "Select UOS.exe."),
                        _ => Loc.S(
                            "Seleziona RazorEnhanced.exe.",
                            "Select RazorEnhanced.exe.")
                    });
                return;
            }

            _assistantPathBox.Text = dialog.FileName;
            _settings.ClearAssistantPathClearedFlag(assistant);
            SaveSettings();
            UpdateAssistantDownloadUi();
        }

        private static string GetAssistantBrowseFilter(string assistant) => assistant switch
        {
            "ClassicAssist" => Loc.S("ClassicAssist (*.dll)|ClassicAssist.dll", "ClassicAssist (*.dll)|ClassicAssist.dll"),
            "Razor Enhanced" => Loc.S(
                "Razor Enhanced (*.exe)|RazorEnhanced.exe",
                "Razor Enhanced (*.exe)|RazorEnhanced.exe"),
            "Orion" => Loc.S(
                "Orion Launcher (*.exe)|OrionLauncher64.exe;Orion Launcher64.exe",
                "Orion Launcher (*.exe)|OrionLauncher64.exe;Orion Launcher64.exe"),
            "UOSteam" => Loc.S("UOSteam (UOS.exe)|UOS.exe", "UOSteam (UOS.exe)|UOS.exe"),
            _ => Loc.S("File (*.exe)|*.exe", "File (*.exe)|*.exe")
        };

        private static bool IsValidAssistantBrowseSelection(string assistant, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            string fileName = Path.GetFileName(filePath);
            return assistant switch
            {
                "ClassicAssist" => fileName.Equals("ClassicAssist.dll", StringComparison.OrdinalIgnoreCase),
                "Razor Enhanced" => fileName.Equals("RazorEnhanced.exe", StringComparison.OrdinalIgnoreCase),
                "Orion" => fileName.Equals("OrionLauncher64.exe", StringComparison.OrdinalIgnoreCase) ||
                           fileName.Equals("Orion Launcher64.exe", StringComparison.OrdinalIgnoreCase),
                "UOSteam" => fileName.Equals("UOS.exe", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private void LayoutEnhancedMapControls(int margin, int contentWidth, int rowY)
        {
            int rightEdge = margin + contentWidth;
            _enhancedMapLink.Location = new Point(rightEdge - _enhancedMapLink.PreferredSize.Width, rowY);
            _enhancedMapAutoOpenCheck.Location = new Point(
                rightEdge - _enhancedMapAutoOpenCheck.PreferredSize.Width,
                rowY + 22);
        }

        private void ShowEnhancedMapMenu()
        {
            if (_enhancedMapMenu == null || _enhancedMapMenu.IsDisposed)
            {
                _enhancedMapMenu = new ThemedContextMenu();
                _enhancedMapMenu.AddAction(Loc.S("Avvia", "Launch"), () => LaunchEnhancedMap());
                _enhancedMapMenu.AddAction(Loc.S("Sfoglia…", "Browse…"), BrowseEnhancedMap);
                _enhancedMapMenu.AddAction(Loc.S("Scarica", "Download"), DownloadEnhancedMap);
            }

            _enhancedMapMenu.ShowBelow(_enhancedMapLink);
        }

        private void BrowseEnhancedMap()
        {
            using var dialog = new OpenFileDialog
            {
                Title = Loc.S("Seleziona EnhancedMap.exe", "Select EnhancedMap.exe"),
                Filter = Loc.S(
                    "Enhanced Map (*.exe)|EnhancedMap.exe",
                    "Enhanced Map (*.exe)|EnhancedMap.exe"),
                CheckFileExists = true,
                ValidateNames = true,
                Multiselect = false
            };

            string? saved = _settings.EnhancedMapPath;
            if (!string.IsNullOrWhiteSpace(saved))
            {
                try
                {
                    string? resolved = EnhancedMapDownloader.ResolveExePath(saved);
                    if (resolved != null)
                    {
                        dialog.InitialDirectory = Path.GetDirectoryName(resolved);
                        dialog.FileName = EnhancedMapDownloader.ExeFileName;
                    }
                    else if (Directory.Exists(saved))
                    {
                        dialog.InitialDirectory = saved;
                    }
                }
                catch { }
            }

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (!Path.GetFileName(dialog.FileName)
                    .Equals(EnhancedMapDownloader.ExeFileName, StringComparison.OrdinalIgnoreCase))
            {
                ThemedMessageDialog.ShowInfo(
                    this,
                    Loc.S("Percorso non valido", "Invalid path"),
                    Loc.S("Seleziona EnhancedMap.exe.", "Select EnhancedMap.exe."));
                return;
            }

            _settings.EnhancedMapPath = dialog.FileName;
            _settings.Save();
            _statusLabel.ForeColor = Theme.SectionGreen;
            _statusLabel.Text = Loc.S("Enhanced Map configurato.", "Enhanced Map configured.");
        }

        private void DownloadEnhancedMap()
        {
            string installDir = EnhancedMapDownloader.DefaultInstallDirectory;

            _statusLabel.ForeColor = Theme.TextMuted;
            _statusLabel.Text = Loc.S("Download Enhanced Map in corso…", "Downloading Enhanced Map…");

            using var progressForm = DownloadProgressForm.ForEnhancedMap(installDir);
            var result = progressForm.ShowDialog(this);

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(progressForm.ResultPath))
            {
                _settings.EnhancedMapPath = progressForm.ResultPath;
                _settings.Save();
                _statusLabel.ForeColor = Theme.SectionGreen;
                _statusLabel.Text = Loc.S("Enhanced Map installato correttamente.", "Enhanced Map installed successfully.");
            }
            else if (result == DialogResult.Abort)
            {
                _statusLabel.ForeColor = Theme.Error;
                _statusLabel.Text = Loc.S(
                    "Download Enhanced Map non riuscito. Riprova o seleziona manualmente il percorso.",
                    "Enhanced Map download failed. Retry or select the path manually.");
            }
            else
            {
                _statusLabel.Text = "";
            }
        }

        private void LaunchEnhancedMapIfNeeded()
        {
            if (!_enhancedMapAutoOpenCheck.Checked)
            {
                return;
            }

            LaunchEnhancedMap(silent: true);
        }

        private void LaunchEnhancedMap(bool silent = false)
        {
            string? exe = EnhancedMapDownloader.ResolveExePath(_settings.EnhancedMapPath);
            if (exe == null)
            {
                if (!silent)
                {
                    ShowError(Loc.S(
                        "Enhanced Map non trovato. Scaricalo o seleziona il percorso di EnhancedMap.exe.",
                        "Enhanced Map not found. Download it or select the path to EnhancedMap.exe."));
                }

                return;
            }

            try
            {
                if (EnhancedMapDownloader.LaunchOrFocus(exe))
                {
                    LauncherLog.Info($"Enhanced Map avviato exe={exe}");
                }
                else
                {
                    LauncherLog.Info($"Enhanced Map già in esecuzione, portato in primo piano exe={exe}");
                }
            }
            catch (Exception ex)
            {
                LauncherLog.Error("Impossibile avviare Enhanced Map", ex);
                if (!silent)
                {
                    ShowError(Loc.S("Errore durante l'avvio di Enhanced Map: ", "Error while starting Enhanced Map: ") + ex.Message, ex);
                }
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

            AssistantPaths.EnsureLauncherAssistantRoot();
            string installDir = AssistantDownloader.GetDefaultInstallDirectory(SelectedAssistant);

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
                _settings.ClearAssistantPathClearedFlag(SelectedAssistant);
                SaveSettings();
                UpdateAssistantDownloadUi();
                _statusLabel.ForeColor = Theme.SectionGreen;
                _statusLabel.Text = Loc.S(
                    "✓ Assistant caricato correttamente.",
                    "✓ Assistant loaded successfully.");
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
                _clientPathBox.Text = DetectDefaultClient();
                SaveSettings();
                UpdateUoDownloadUi();
                UpdateAssistantUi();
                _statusLabel.ForeColor = Theme.SectionGreen;
                _statusLabel.Text = Loc.S(
                    "✓ Client UODreams installato correttamente.",
                    "✓ UODreams client installed successfully.");
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
                LaunchEnhancedMapIfNeeded();
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
                    "UOSteam non supporta ClassicUO. Prova invece ClassicAssist, Razor Enhanced o Orion " +
                    "per usare tutte le feature del Classic Client.",
                    "UOSteam does not support ClassicUO. Instead, try ClassicAssist, Razor Enhanced, or Orion " +
                    "to use all the features of the Classic Client."));
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
                LaunchEnhancedMapIfNeeded();
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
                _settings.SyncInstalledClientVersionFromRuntime();
                RefreshWindowTitle();
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
                    string launcherVer = LauncherManifest.RuntimeLauncherVersion;
                    string clientVer = _settings.EffectiveClientVersion;
                    _statusLabel.ForeColor = Theme.SectionGreen;
                    _statusLabel.Text = Loc.S(
                        $"Sei già aggiornato (launcher v{launcherVer}, client v{clientVer}).",
                        $"You are up to date (launcher v{launcherVer}, client v{clientVer}).");
                    ThemedMessageDialog.ShowInfo(
                        this,
                        Loc.S("Aggiornamento", "Update"),
                        Loc.S(
                            $"Launcher e client sono aggiornati (launcher v{launcherVer}, client v{clientVer}).",
                            $"Launcher and client are up to date (launcher v{launcherVer}, client v{clientVer})."));
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
            _settings.EnhancedMapAutoOpen = _enhancedMapAutoOpenCheck.Checked;

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

            if (SelectedAssistant == "Razor Enhanced" && LauncherManifest.IsPvpEdition)
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

            string clientWorkingDir = Path.GetDirectoryName(effectiveClient)!;
            string? runtimeError = ClientNativeRuntime.Validate(clientWorkingDir);
            if (runtimeError != null)
            {
                ShowError(runtimeError);
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = effectiveClient,
                WorkingDirectory = clientWorkingDir,
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
                LaunchEnhancedMapIfNeeded();
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
