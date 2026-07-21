using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    public sealed class MainForm : Form, IGradientBackgroundHost
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
#if LAUNCHER_EDITION_ONEUO
        private ThemedButton _themeButton = null!;
        private ThemedContextMenu? _themeMenu;
#endif
        private ThemedButton _updateButton = null!;
        private ThemedButton _paypalPill = null!;
        private ThemedButton _coffeePill = null!;
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
        private string? _detectedUoVersion;
        private CardPanel _assistantCard = null!;
        private const int UoCardPadH = 18;
        private const int UoPathRowY = 40;

        public MainForm()
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
            Loc.Lang = _settings.Language == "en" ? "en" : "it";
#if LAUNCHER_EDITION_ONEUO
            Theme.ApplyLauncherTheme(_settings.UiTheme);
#endif
            LoadWindowIcon();
            BuildUi();
            LoadFromSettings();
            _settings.SanitizeInstalledClientVersion();
            _settings.SyncInstalledClientVersionFromRuntime();
            RefreshWindowTitle();

            Shown += OnMainFormShown;
            ResizeEnd += OnMainFormResizeEnd;
        }

        private FormWindowState _lastWindowState = FormWindowState.Normal;

        private void OnMainFormResizeEnd(object? sender, EventArgs e)
        {
            if (_lastWindowState == FormWindowState.Minimized && WindowState != FormWindowState.Minimized)
            {
                Invalidate(true);
                RefreshAssistantSectionLayout();
            }

            _lastWindowState = WindowState;
        }

        private void OnMainFormShown(object? sender, EventArgs e)
        {
            try
            {
                if (LauncherUpdater.EnsureHealthyDefaultRazorProfile())
                {
                    LauncherLog.Info("Repaired corrupted Razor Default PVP profile from bundled stock.");
                }
            }
            catch (Exception ex)
            {
                LauncherLog.Error($"Razor Default PVP profile health check failed: {ex.Message}", ex);
            }

            RefreshAssistantSectionLayout();
            BeginInvoke(RefreshAssistantSectionLayout);
            _ = CheckForUpdatesOnStartupAsync();
        }

        private void LoadWindowIcon()
        {
            try
            {
#if LAUNCHER_EDITION_ONEUO
                const string iconResource = "ClassicUO.Launcher.Custom.Resources.oneuo.ico";
#else
                const string iconResource = "ClassicUO.Launcher.Custom.Resources.uodreams.ico";
#endif
                using Stream? stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(iconResource);

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
            Theme.FillLinearGradient(
                e.Graphics,
                ClientRectangle,
                Theme.WindowTop,
                Theme.WindowBottom,
                LinearGradientMode.Vertical);
        }

        /// <summary>Lets child controls (see <see cref="Theme.GetSurfaceBrush"/>) sample the exact
        /// gradient color behind their bounds instead of approximating with a flat fill.</summary>
        Color IGradientBackgroundHost.GetBackgroundColorAt(int clientY, int clientHeight) =>
            Theme.SampleWindowGradient(clientY, clientHeight);

        private static Image? LoadLogo()
        {
            try
            {
#if LAUNCHER_EDITION_ONEUO
                const string logoResource = "ClassicUO.Launcher.Custom.Resources.oneuo_logo.png";
#else
                const string logoResource = "ClassicUO.Launcher.Custom.Resources.uodreams_logo.png";
#endif
                using Stream? s = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(logoResource);
                if (s == null)
                {
                    return null;
                }

                using Image raw = Image.FromStream(s);
#if LAUNCHER_EDITION_ONEUO
                // Make the solid-black backdrop (and near-black shading) transparent, then drop the
                // now-empty padding so Zoom fills the banner with artwork instead of a black box.
                using Bitmap keyed = ApplyBlackChromaKey(raw);
                return TrimSolidBlackBorders(keyed);
#else
                return new Bitmap(raw);
#endif
            }
            catch
            {
                return null;
            }
        }

#if LAUNCHER_EDITION_ONEUO
        // Pixels darker than this are pure background/padding -> fully transparent.
        private const int ChromaKeyLowThreshold = 6;
        // Pixels between the low and high threshold fade in proportionally, so anti-aliased edges
        // (and the wing artwork's own dark shading/texture holes) blend smoothly into whatever
        // theme background sits behind the logo instead of leaving a hard black cutout.
        private const int ChromaKeyHighThreshold = 50;

        /// <summary>
        /// Converts the logo's solid-black background (and near-black shading) into real alpha
        /// transparency, so no opaque black box remains behind the skull/wings artwork.
        /// </summary>
        private static Bitmap ApplyBlackChromaKey(Image source)
        {
            var bmp = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height));
            }

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int stride = data.Stride;
                int byteCount = stride * bmp.Height;
                byte[] buffer = new byte[byteCount];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, byteCount);

                for (int y = 0; y < bmp.Height; y++)
                {
                    int rowStart = y * stride;
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int i = rowStart + x * 4; // Format32bppArgb byte order: B, G, R, A
                        byte a = buffer[i + 3];
                        if (a == 0)
                        {
                            continue;
                        }

                        int brightness = Math.Max(buffer[i], Math.Max(buffer[i + 1], buffer[i + 2]));
                        if (brightness <= ChromaKeyLowThreshold)
                        {
                            buffer[i + 3] = 0;
                        }
                        else if (brightness < ChromaKeyHighThreshold)
                        {
                            float ratio = (brightness - ChromaKeyLowThreshold) / (float)(ChromaKeyHighThreshold - ChromaKeyLowThreshold);
                            buffer[i + 3] = (byte)Math.Round(a * ratio);
                        }
                    }
                }

                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, byteCount);
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            return bmp;
        }

        /// <summary>
        /// Crops near-black borders so the logo art fills its PictureBox without letterboxing.
        /// </summary>
        private static Bitmap TrimSolidBlackBorders(Image source)
        {
            using var src = new Bitmap(source);
            int w = src.Width;
            int h = src.Height;
            const int threshold = 18; // treat near-black as padding

            int left = 0, top = 0, right = w - 1, bottom = h - 1;

            bool RowIsPadding(int y)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = src.GetPixel(x, y);
                    if (c.A > 8 && (c.R > threshold || c.G > threshold || c.B > threshold))
                    {
                        return false;
                    }
                }

                return true;
            }

            bool ColIsPadding(int x)
            {
                for (int y = 0; y < h; y++)
                {
                    Color c = src.GetPixel(x, y);
                    if (c.A > 8 && (c.R > threshold || c.G > threshold || c.B > threshold))
                    {
                        return false;
                    }
                }

                return true;
            }

            while (top <= bottom && RowIsPadding(top)) top++;
            while (bottom >= top && RowIsPadding(bottom)) bottom--;
            while (left <= right && ColIsPadding(left)) left++;
            while (right >= left && ColIsPadding(right)) right--;

            if (right < left || bottom < top)
            {
                return new Bitmap(src);
            }

            // Small breathing room so spikes/wings aren't flush against the edge.
            const int pad = 4;
            left = Math.Max(0, left - pad);
            top = Math.Max(0, top - pad);
            right = Math.Min(w - 1, right + pad);
            bottom = Math.Min(h - 1, bottom + pad);

            var rect = new Rectangle(left, top, right - left + 1, bottom - top + 1);
            var trimmed = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(trimmed))
            {
                g.DrawImage(src, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
            }

            return trimmed;
        }
#endif

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

        private static Image? LoadDonationCoffeeIcon()
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
                Theme.DrawRecoloredImage(g, src, new Rectangle(0, 0, src.Width, src.Height), Theme.PillCoffeeCupBrown);
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
#if LAUNCHER_EDITION_ONEUO
            BackColor = Color.Black;
#endif

            const int formWidth = 620;
            const int bottomMargin = 10;
            int margin = 22;
            int width = formWidth - margin * 2;
            // Same vertical rhythm as classic launcher — do NOT grow the window for ONEUO.
            int y = 16;

            // ----- Banner logo -----
            Image? logo = LoadLogo();
            int logoBottomY = y;

            if (logo != null)
            {
#if LAUNCHER_EDITION_ONEUO
                // Slightly taller banner; Enhanced Map overlays the black corner (form height unchanged).
                const int bannerH = 178;
#else
                const int bannerH = 148;
#endif
                var logoBox = new PictureBox
                {
                    Image = logo,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Bounds = new Rectangle(margin, y, width, bannerH)
                };
                Controls.Add(logoBox);
                logoBottomY = y + bannerH;
                y += bannerH + 4;
            }

            // ----- Enhanced Map (right-aligned) -----
#if LAUNCHER_EDITION_ONEUO
            // Sit in the black corner over the logo — does not push the layout down.
            _enhancedMapRowY = Math.Max(16, logoBottomY - 44);
#else
            _enhancedMapRowY = y;
#endif
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
            _enhancedMapLink.BringToFront();
            _enhancedMapAutoOpenCheck.BringToFront();
#if LAUNCHER_EDITION_ONEUO
            // Map controls overlay the logo; only a tiny gap before the first card.
            y = logoBottomY + 8;
#else
            y += 50;
#endif

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
            y += footerH + 12;

            // ----- Donation footer -----
            const int donatePillH = Theme.CompactPrimaryHeight;
            const int donatePillGap = 8;

            var donationCredit = new Label
            {
#if LAUNCHER_EDITION_ONEUO
                Text = "0nE UO Launcher project by lall0ne",
#else
                Text = "UODreams Launcher project by lall0ne",
#endif
                Font = Theme.DonationCreditFont,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            Controls.Add(donationCredit);
            donationCredit.Location = new Point(
                margin + Math.Max(0, (width - donationCredit.PreferredSize.Width) / 2),
                y);
            y += donationCredit.Height + 8;

            _paypalPill = new ThemedButton
            {
                Text = "PayPal",
                IconText = "♥",
                IconTextColor = Theme.PillPayPalHeart
            };
            Theme.ApplyCompactPrimaryStyle(_paypalPill);
            _paypalPill.Click += (_, _) => OpenUrl("https://www.paypal.com/donate/?hosted_button_id=ZUGYHHC2L7ZXC");

            _coffeePill = new ThemedButton
            {
                Text = Loc.S("Buy me a coffee", "Buy me a coffee"),
                IconImage = LoadDonationCoffeeIcon()
            };
            Theme.ApplyCompactPrimaryStyle(_coffeePill);
            _coffeePill.Click += (_, _) => OpenUrl("https://buymeacoffee.com/lall0ne");

            int paypalW = _paypalPill.PreferredSize.Width;
            int coffeeW = _coffeePill.PreferredSize.Width;

            int donateRowW = paypalW + donatePillGap + coffeeW;
            int donateStartX = margin + Math.Max(0, (width - donateRowW) / 2);
            _paypalPill.Bounds = new Rectangle(donateStartX, y, paypalW, donatePillH);
            _coffeePill.Bounds = new Rectangle(donateStartX + paypalW + donatePillGap, y, coffeeW, donatePillH);
            Controls.Add(_paypalPill);
            Controls.Add(_coffeePill);
            y += donatePillH;

            // ----- Toolbar (top) -----
            const int toolbarBtnHTop = Theme.ToolbarButtonHeight;
            _updateButton = new ThemedButton
            {
                Text = UpToDateButtonText(),
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Theme.Text,
                Padding = new Padding(Theme.CompactPrimaryHorizontalPadding, 0, Theme.CompactPrimaryHorizontalPadding, 0),
#if LAUNCHER_EDITION_ONEUO
                Bounds = new Rectangle(margin, 12, 76, toolbarBtnHTop)
#else
                Bounds = new Rectangle(formWidth - margin - 148, 12, 76, toolbarBtnHTop)
#endif
            };
            _updateButton.Click += (_, _) => CheckForUpdates();
            Controls.Add(_updateButton);
            ResizeUpdateButtonToFitText();

            // ----- Language toggle (top-right) -----
            _langButton = new ThemedButton
            {
                Text = LangButtonText(),
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Theme.Text,
                Bounds = new Rectangle(formWidth - margin - 66, 12, 66, toolbarBtnHTop)
            };
            _langButton.Click += (_, _) => ToggleLanguage();
            Controls.Add(_langButton);

#if LAUNCHER_EDITION_ONEUO
            // ----- Theme picker (top-right, left of language toggle) -----
            const int themeBtnW = 76;
            const int themeBtnGap = 8;
            _themeButton = new ThemedButton
            {
                Text = Loc.S("🎨 Tema", "🎨 Theme"),
                CornerRadius = 8,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Theme.Text,
                Bounds = new Rectangle(formWidth - margin - 66 - themeBtnGap - themeBtnW, 12, themeBtnW, toolbarBtnHTop)
            };
            _themeButton.Click += (_, _) => ShowThemeMenu();
            Controls.Add(_themeButton);
            _themeButton.BringToFront();
#endif

            _updateButton.BringToFront();
            _langButton.BringToFront();

            ClientSize = new Size(formWidth, y + bottomMargin);
        }

        private void RefreshWindowTitle()
        {
            string launcherVer = LauncherManifest.RuntimeLauncherVersion;
            string title = LauncherManifest.ProductTitle;
#if LAUNCHER_EDITION_ONEUO
            // Always: "0nE UO Launcher vX.Y.Z by lall0ne" — never append client version.
            Text = $"{title} v{launcherVer} by lall0ne";
#else
            string clientVer = _settings.EffectiveClientVersion;
            Text = string.Equals(launcherVer, clientVer, StringComparison.OrdinalIgnoreCase)
                ? $"{title} v{launcherVer}"
                : $"{title} v{launcherVer} · client v{clientVer}";
#endif
        }

        private bool ShouldHighlightUpdate(UpdateCheckResult? info)
        {
            if (info?.HasAnyUpdate == true)
            {
                return true;
            }

            // Launcher ahead of installed client (e.g. launcher-only OTA) must still offer client update.
            return LauncherUpdater.CompareVersions(
                LauncherManifest.RuntimeLauncherVersion,
                _settings.EffectiveClientVersion) > 0;
        }

        private static string UpToDateButtonText() => Loc.S("Aggiornato", "Up to date");

        private static string UpdateAvailableButtonText() => Loc.S("Aggiornamento disponibile", "Update Available");

        /// <summary>
        /// Grows (or shrinks back to a sane minimum) the update button so its current text never
        /// gets clipped. Anchored to the left edge on ONEUO (top-left placement) and to the right
        /// edge otherwise, so it never overlaps the Theme/language toggles.
        /// </summary>
        private void ResizeUpdateButtonToFitText()
        {
            int preferredWidth = _updateButton.GetPreferredSize(Size.Empty).Width;
            int newWidth = Math.Max(76, preferredWidth);
#if LAUNCHER_EDITION_ONEUO
            _updateButton.Width = newWidth;
#else
            int right = _updateButton.Right;
            _updateButton.Width = newWidth;
            _updateButton.Left = right - newWidth;
#endif
        }

        private void SetUpdateAvailable(bool available)
        {
            _updateAvailable = available;
            // Static (non-animated) highlight only when an update is available; "up to date"
            // always renders with the normal, non-highlighted button chrome.
            _updateButton.HighlightAsUpdate = available;
            _updateButton.Text = available
                ? UpdateAvailableButtonText()
                : UpToDateButtonText();
            ResizeUpdateButtonToFitText();
            _updateButton.Invalidate();
        }

        private async Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                _settings.SanitizeInstalledClientVersion();
                _settings.SyncInstalledClientVersionFromRuntime();
                UpdateCheckResult? info = await LauncherUpdater.CheckForUpdatesAsync(_settings.EffectiveClientVersion);
                if (!IsDisposed)
                {
                    BeginInvoke(() => SetUpdateAvailable(ShouldHighlightUpdate(info)));
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
                lines.AppendLine(FormatComponentUpdateLine(
                    Loc.S("Launcher", "Launcher"),
                    info.LocalLauncherVersion,
                    info.LauncherRemoteVersion ?? info.LatestVersion,
                    info.LauncherSizeBytes));
            }

            if (info.NeedsClientUpdate)
            {
                lines.AppendLine(FormatComponentUpdateLine(
                    Loc.S("Client ClassicUO", "ClassicUO client"),
                    info.LocalClientVersion,
                    info.ClientRemoteVersion ?? info.LatestVersion,
                    info.ClientSizeBytes));
            }

            if (info.NeedsRazorUpdate)
            {
                lines.AppendLine(FormatComponentUpdateLine(
                    Loc.S("Razor Enhanced", "Razor Enhanced"),
                    info.LocalRazorVersion,
                    info.RazorRemoteVersion ?? info.LatestVersion,
                    info.RazorSizeBytes));
            }

            if (info.UsesManifest)
            {
                lines.AppendLine();
                lines.AppendLine(Loc.S(
                    "Il profilo \"Default PVP\" non verra' sovrascritto se gia' presente.",
                    "The \"Default PVP\" profile will not be overwritten if already present."));
            }

            if (!string.IsNullOrWhiteSpace(info.ReleaseNotes))
            {
                lines.AppendLine();
                lines.AppendLine(Loc.S("Novita':", "What's new:"));
                lines.AppendLine(info.ReleaseNotes);
            }

            if (info.TotalDownloadBytes > 0)
            {
                lines.AppendLine();
                lines.AppendLine(Loc.S(
                    $"Download totale stimato: {UoClientDownloader.FormatBytes(info.TotalDownloadBytes)}.",
                    $"Estimated total download: {UoClientDownloader.FormatBytes(info.TotalDownloadBytes)}."));
            }

            lines.AppendLine();
            lines.AppendLine(Loc.S(
                "Procedere con il download e l'installazione?",
                "Proceed with download and installation?"));

            return lines.ToString().TrimEnd();
        }

        private static string FormatComponentUpdateLine(
            string label,
            string localVersion,
            string remoteVersion,
            long sizeBytes)
        {
            string local = string.IsNullOrWhiteSpace(localVersion) ? "0.0.0" : localVersion;
            string remote = string.IsNullOrWhiteSpace(remoteVersion) ? "?" : remoteVersion;
            string sizeSuffix = sizeBytes > 0
                ? $"  (~{UoClientDownloader.FormatBytes(sizeBytes)})"
                : "";
            return Loc.S(
                $"• {label}  v{local} -> v{remote}{sizeSuffix}",
                $"• {label}  v{local} -> v{remote}{sizeSuffix}");
        }

        private void MarkRazorUpdated(string version)
        {
            LauncherUpdater.WriteRazorVersionMarker(version);
        }

        private void MarkClientUpdated(string version)
        {
            string normalized = LauncherUpdater.NormalizeVersion(version);
            _settings.InstalledClientVersion = normalized;
            _settings.Save();
            ClientRuntimeDownloader.WriteClientVersionMarker(normalized);
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

#if LAUNCHER_EDITION_ONEUO
        private void ShowThemeMenu()
        {
            _themeMenu?.Dispose();
            _themeMenu = new ThemedContextMenu();
            foreach (Theme.LauncherThemePreset preset in Theme.LauncherThemes)
            {
                string label = Theme.ThemeLabel(preset);
                bool isCurrent = string.Equals(preset.Id, Theme.CurrentUiThemeId, StringComparison.OrdinalIgnoreCase);
                _themeMenu.AddAction(isCurrent ? $"✓ {label}" : $"    {label}", () => ApplyUiTheme(preset.Id));
            }

            _themeMenu.ShowBelow(_themeButton);
        }

        private void ApplyUiTheme(string themeId)
        {
            Theme.ApplyLauncherTheme(themeId);
            _settings.UiTheme = Theme.CurrentUiThemeId;
            _settings.Save();
            RefreshThemeUi();
        }

        /// <summary>
        /// Re-applies theme-derived colors that were snapshotted at control construction time,
        /// then invalidates the form (and every child, including CardPanels) to repaint live-read colors.
        /// </summary>
        private void RefreshThemeUi()
        {
            _assistantPathBox.BackColor = Theme.Input;
            _uoPathBox.BackColor = Theme.Input;
            _serverCombo.BackColor = Theme.Input;
            _serverCombo.ForeColor = Theme.Text;

            Invalidate(true);
        }
#endif

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
            _updateButton.Text = _updateAvailable
                ? UpdateAvailableButtonText()
                : UpToDateButtonText();
            ResizeUpdateButtonToFitText();
            _launchButton.Text = Loc.S("AVVIA", "START");
            _coffeePill.Text = Loc.S("Buy me a coffee", "Buy me a coffee");
            _registerBtn.Text = Loc.S("Registrati gratis", "Register for free");
            _enhancedMapLink.Text = Loc.S("🌍 ENHANCED MAP", "🌍 ENHANCED MAP");
            _enhancedMapAutoOpenCheck.Text = Loc.S("apri mappa automaticamente", "autoopen map");
            RebuildEnhancedMapMenu();
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

        private void OpenRoomRequestWindow()
        {
            try
            {
                using var form = new RoomRequestForm();
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                LauncherLog.Error("Failed to open room request window", ex);
                RegisterForm.OpenInExternalBrowser("http://www.uodreams.it/?f=page");
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
                string? resolved = ResolveExistingRazorExe(saved);
                if (!string.IsNullOrEmpty(resolved))
                {
                    return resolved;
                }

                // Stale path (Razor moved/uninstalled): clear so UI falls back to bundled/default.
                _settings.RazorPath = "";
                _settings.Save();
                saved = "";
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

        /// <summary>
        /// Accepts RazorEnhanced.exe or its containing folder; returns null if missing.
        /// </summary>
        private static string? ResolveExistingRazorExe(string path)
        {
            string trimmed = path.Trim().Trim('"');
            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            if (File.Exists(trimmed) &&
                Path.GetFileName(trimmed).Equals("RazorEnhanced.exe", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            if (Directory.Exists(trimmed))
            {
                string candidate = Path.Combine(trimmed, "RazorEnhanced.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private bool IsShowingBundledRazorPath()
        {
            if (!IsBundledRazorAvailable())
            {
                return false;
            }

            string current = (_assistantPathBox.Text ?? "").Trim();
            string bundled = GetBundledRazorPath();
            return !string.IsNullOrEmpty(current) &&
                   string.Equals(current, bundled, StringComparison.OrdinalIgnoreCase);
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

            _detectedUoVersion = valid ? UoClientVersionDetector.Detect(uoPath) : null;

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
                _uoHintLabel.Text = string.IsNullOrWhiteSpace(_detectedUoVersion)
                    ? Loc.S("✓ Client Ultima Online pronto.", "✓ Ultima Online client ready.")
                    : Loc.S(
                        $"✓ Client Ultima Online pronto (v{_detectedUoVersion}).",
                        $"✓ Ultima Online client ready (v{_detectedUoVersion}).");
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

            bool razorSelected = SelectedAssistant == "Razor Enhanced";
            bool bundledRazorAvailable = razorSelected && IsBundledRazorAvailable();
            bool showingBundledRazor = razorSelected && IsShowingBundledRazorPath();
            bool canDownload = hasAssistant &&
                AssistantDownloader.SupportsDownload(SelectedAssistant) &&
                !bundledRazorAvailable;
            bool installed = hasAssistant &&
                AssistantDownloader.IsPluginValidAtPath(SelectedAssistant, _assistantPathBox.Text);

            bool showDownload = canDownload && !installed;

            _assistantPathPanel.Visible = hasAssistant;
            _downloadAssistantButton.Visible = showDownload;
            _assistantActionPanel.Visible = hasAssistant && showDownload;
            // Always allow browse (including Razor) so users can retarget after moving installs.
            _assistantBrowseButton.Visible = hasAssistant;
            _assistantHintLabel.Visible = hasAssistant;
            _assistantTrashButton.Visible = hasAssistant &&
                !string.IsNullOrWhiteSpace(_assistantPathBox.Text) &&
                !showingBundledRazor;

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
                razorSelected &&
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
            else if (!string.IsNullOrWhiteSpace(_assistantPathBox.Text) && !installed)
            {
                _assistantHintLabel.Text = Loc.S(
                    "Percorso non valido — usa … per selezionare il file corretto.",
                    "Invalid path — use … to select the correct file.");
                _assistantHintLabel.ForeColor = Theme.TextMuted;
            }
            else if (bundledRazorAvailable)
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
                    "Razor Enhanced" => Loc.S(
                        "Razor Enhanced non trovato — scaricalo o usa … per selezionare RazorEnhanced.exe.",
                        "Razor Enhanced not found — download it or use … to select RazorEnhanced.exe."),
                    _ => Loc.S(
                        $"{SelectedAssistant} non trovato — scaricalo o seleziona il percorso del plugin.",
                        $"{SelectedAssistant} not found — download it or select the plugin path.")
                };
                _assistantHintLabel.ForeColor = Theme.TextMuted;
            }
            else
            {
                _assistantHintLabel.Text = Loc.S(
                    "Seleziona il percorso dell'assistente (pulsante …).",
                    "Select the assistant path (… button).");
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
            RebuildEnhancedMapMenu();
            _enhancedMapMenu!.ShowBelow(_enhancedMapLink);
        }

        private void RebuildEnhancedMapMenu()
        {
            if (_enhancedMapMenu != null)
            {
                _enhancedMapMenu.Dispose();
                _enhancedMapMenu = null;
            }

            _enhancedMapMenu = new ThemedContextMenu();
            _enhancedMapMenu.AddAction(Loc.S("Avvia", "Launch"), () => LaunchEnhancedMap());
            _enhancedMapMenu.AddAction(Loc.S("Sfoglia…", "Browse…"), BrowseEnhancedMap);
            _enhancedMapMenu.AddAction(Loc.S("Scarica", "Download"), DownloadEnhancedMap);
            if (!LauncherManifest.IsPvpEdition)
            {
                _enhancedMapMenu.AddAction(
                    Loc.S("Richiedi Stanza", "Request Room"),
                    OpenRoomRequestWindow);
            }
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
                SaveSettings();
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
                _settings.SanitizeInstalledClientVersion();
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
                    string razorVer = LauncherUpdater.ResolveEffectiveRazorVersion();
                    _statusLabel.ForeColor = Theme.SectionGreen;
                    _statusLabel.Text = Loc.S(
                        $"Sei gia' aggiornato (launcher v{launcherVer}, client v{clientVer}, Razor v{razorVer}).",
                        $"You are up to date (launcher v{launcherVer}, client v{clientVer}, Razor v{razorVer}).");
                    ThemedMessageDialog.ShowInfo(
                        this,
                        Loc.S("Aggiornamento", "Update"),
                        Loc.S(
                            $"Launcher, client e Razor sono aggiornati (launcher v{launcherVer}, client v{clientVer}, Razor v{razorVer}).",
                            $"Launcher, client, and Razor are up to date (launcher v{launcherVer}, client v{clientVer}, Razor v{razorVer})."));
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

                // Client first: launcher update restarts the process and would skip later steps otherwise.
                if (info.NeedsClientUpdate &&
                    !string.IsNullOrEmpty(info.ClientDownloadUrl) &&
                    !string.IsNullOrEmpty(info.ClientPackageFileName))
                {
                    using var clientForm = DownloadProgressForm.ForClientRuntimeUpdate(
                        info.ClientDownloadUrl,
                        info.ClientPackageFileName,
                        info.ClientSha256);
                    if (clientForm.ShowDialog(this) != DialogResult.OK)
                    {
                        ShowError(Loc.S("Aggiornamento client non riuscito.", "Client update failed."));
                        return;
                    }

                    string? clientVersion = info.ClientRemoteVersion
                        ?? LauncherUpdater.ParseVersionFromPackageName(info.ClientPackageFileName)
                        ?? info.LatestVersion;
                    MarkClientUpdated(clientVersion);
                    _clientPathBox.Text = DetectDefaultClient();
                    UpdateAssistantUi();
                }

                if (info.NeedsRazorUpdate &&
                    info.UsesManifest &&
                    !string.IsNullOrEmpty(info.RazorDownloadUrl) &&
                    !string.IsNullOrEmpty(info.RazorPackageFileName))
                {
                    using var razorForm = DownloadProgressForm.ForRazorUpdate(
                        info.RazorDownloadUrl,
                        info.RazorPackageFileName,
                        info.RazorSha256);
                    if (razorForm.ShowDialog(this) != DialogResult.OK)
                    {
                        ShowError(Loc.S("Aggiornamento Razor non riuscito.", "Razor update failed."));
                        return;
                    }

                    string? razorVersion = info.RazorRemoteVersion
                        ?? LauncherUpdater.ParseVersionFromPackageName(info.RazorPackageFileName)
                        ?? info.LatestVersion;
                    MarkRazorUpdated(razorVersion);
                    UpdateAssistantUi();
                }

                if (info.NeedsLauncherUpdate &&
                    !string.IsNullOrEmpty(info.LauncherDownloadUrl) &&
                    !string.IsNullOrEmpty(info.LauncherPackageFileName))
                {
                    using var launcherForm = DownloadProgressForm.ForLauncherUpdate(
                        info.LauncherDownloadUrl,
                        info.LauncherPackageFileName,
                        info.LauncherSha256);
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

            // Mark first-run complete as soon as the user configures paths/assistant,
            // so a later Client repair download cannot treat this as a virgin install.
            _settings.MigrateFirstRunFlag();
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

            ClientNativeRuntime.ApplyPvpGraphicsDriver(psi);

            psi.ArgumentList.Add("-ip");
            psi.ArgumentList.Add(_ipBox.Text.Trim());
            psi.ArgumentList.Add("-port");
            psi.ArgumentList.Add(((int)_portBox.Value).ToString());
            psi.ArgumentList.Add("-uopath");
            psi.ArgumentList.Add(uoPath);
            psi.ArgumentList.Add("-encryption");
            psi.ArgumentList.Add(_encryptionCheck.Checked ? "1" : "0");

            string? uoClientVersion = UoClientVersionDetector.Detect(uoPath);
            if (!string.IsNullOrWhiteSpace(uoClientVersion))
            {
                psi.ArgumentList.Add("-clientversion");
                psi.ArgumentList.Add(uoClientVersion);
                ClassicUOSettingsSync.SyncForLaunch(clientWorkingDir, uoPath, uoClientVersion);
                LauncherLog.Info($"UO client version detected: {uoClientVersion} (folder={uoPath})");
            }
            else
            {
                LauncherLog.Info($"Could not detect UO client version for folder={uoPath}");
            }

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
