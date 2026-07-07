using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    public sealed class MainForm : Form
    {
        private readonly LauncherSettings _settings = LauncherSettings.Load();

        private ComboBox _assistantCombo = null!;
        private TextBox _assistantPathBox = null!;
        private InputPanel _assistantPathPanel = null!;
        private ThemedButton _assistantBrowseButton = null!;
        private Label _assistantPathLabel = null!;
        private TextBox _clientPathBox = null!;
        private TextBox _uoPathBox = null!;
        private TextBox _ipBox = null!;
        private NumericUpDown _portBox = null!;
        private CheckBox _encryptionCheck = null!;
        private Label _statusLabel = null!;
        private ThemedButton _downloadUoButton = null!;
        private Label _uoHintLabel = null!;
        private ThemedButton _downloadAssistantButton = null!;
        private Label _assistantHintLabel = null!;

        public MainForm()
        {
            LoadWindowIcon();
            BuildUi();
            LoadFromSettings();
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
            Text = $"UODreams Launcher v{LauncherManifest.LauncherVersion}";
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(620, 700);
            DoubleBuffered = true;

            int margin = 24;
            int width = ClientSize.Width - margin * 2;
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
                y += bannerH + 4;
            }

            var subtitle = new Label
            {
                Text = "CLIENT PERSONALIZZATO  •  GRID CONTAINER  •  BARRE VITA CLASSICHE  •  AUTO-EVITA OSTACOLI",
                Font = new Font("Segoe UI Semibold", 7.8f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Bounds = new Rectangle(margin, y, width, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(subtitle);
            y += 30;

            // ----- Card 1: assistant -----
            var assistantCard = new CardPanel { Bounds = new Rectangle(margin, y, width, 190) };
            Controls.Add(assistantCard);

            int cx = 18, cy = 14, cw = width - 36;

            assistantCard.Controls.Add(SectionLabel("Assistente", cx, cy, cw));
            cy += 28;

            _assistantCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                Font = new Font("Segoe UI", 10f),
                Bounds = new Rectangle(cx, cy, cw, 30),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 24
            };
            _assistantCombo.Items.AddRange(new object[] { "Nessuno", "ClassicAssist", "Razor Enhanced", "Orion", "UOSteam" });
            _assistantCombo.DrawItem += (_, e) =>
            {
                if (e.Index < 0) return;
                bool selected = (e.State & DrawItemState.Selected) != 0;
                using var bg = new SolidBrush(selected ? Theme.ButtonNeutralHover : Theme.Input);
                e.Graphics.FillRectangle(bg, e.Bounds);
                TextRenderer.DrawText(
                    e.Graphics,
                    _assistantCombo.Items[e.Index]!.ToString(),
                    _assistantCombo.Font,
                    new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height),
                    Theme.Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                );
            };
            _assistantCombo.SelectedIndexChanged += (_, _) =>
            {
                UpdateAssistantUi();
                UpdateAssistantDownloadUi();
            };
            assistantCard.Controls.Add(_assistantCombo);
            cy += 38;

            _assistantPathLabel = new Label
            {
                Text = "Percorso assistente",
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
                Text = "⬇ Scarica",
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
                AutoSize = false,
                Bounds = new Rectangle(cx, cy, cw, 18)
            };
            assistantCard.Controls.Add(_assistantHintLabel);
            y += assistantCard.Height + 14;

            // ----- Card 2: Ultima Online -----
            var pathsCard = new CardPanel { Bounds = new Rectangle(margin, y, width, 132) };
            Controls.Add(pathsCard);
            cy = 14;

            pathsCard.Controls.Add(SectionLabel("Client Ultima Online", cx, cy, cw));
            cy += 26;
            (_, _uoPathBox, _) = PathRow(pathsCard, cx, cy, cw - 168, BrowseUoFolder);

            _downloadUoButton = new ThemedButton
            {
                Text = "⬇ Scarica UODreams",
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
                AutoSize = false,
                Bounds = new Rectangle(cx, cy, cw, 18)
            };
            pathsCard.Controls.Add(_uoHintLabel);

            _clientPathBox = new TextBox { Visible = false };
            Controls.Add(_clientPathBox);
            y += pathsCard.Height + 14;

            // ----- Card 3: shard -----
            var shardCard = new CardPanel { Bounds = new Rectangle(margin, y, width, 108) };
            Controls.Add(shardCard);
            cy = 14;

            shardCard.Controls.Add(SectionLabel("Shard  —  UODreams", cx, cy, cw));
            cy += 26;

            var ipLabel = new Label
            {
                Text = "Server",
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Bounds = new Rectangle(cx, cy, 200, 16)
            };
            var portLabel = new Label
            {
                Text = "Porta",
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Bounds = new Rectangle(cx + 320, cy, 80, 16)
            };
            shardCard.Controls.Add(ipLabel);
            shardCard.Controls.Add(portLabel);
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
                Text = "Crittografia",
                ForeColor = Theme.Text,
                BackColor = Color.Transparent,
                Bounds = new Rectangle(cx + 430, cy + 4, cw - 430, 24)
            };
            shardCard.Controls.Add(_encryptionCheck);
            y += shardCard.Height + 18;

            // ----- Launch button -----
            var launchButton = new ThemedButton
            {
                Text = "AVVIA",
                UseGradient = true,
                CornerRadius = 12,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                Bounds = new Rectangle(margin, y, width, 48)
            };
            launchButton.Click += (_, _) => Launch();
            Controls.Add(launchButton);
            y += 56;

            _statusLabel = new Label
            {
                Text = "",
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Bounds = new Rectangle(margin, y, width, 34),
                TextAlign = ContentAlignment.TopCenter
            };
            Controls.Add(_statusLabel);
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
                Text = "Sfoglia…",
                Bounds = new Rectangle(x + width - 94, y, 94, 32)
            };
            button.Click += (_, _) => browse();

            parent.Controls.Add(panel);
            parent.Controls.Add(button);
            return (panel, box, button);
        }

        private void LoadFromSettings()
        {
            _assistantCombo.SelectedItem =
                _assistantCombo.Items.Contains(_settings.Assistant)
                    ? _settings.Assistant
                    : _settings.Assistant == "Razor"
                        ? "Razor Enhanced"
                        : "Nessuno";
            _uoPathBox.Text = _settings.UoDirectory;
            _ipBox.Text = _settings.ShardIp;
            _portBox.Value = Math.Min(Math.Max(_settings.ShardPort, 1), 65535);
            _encryptionCheck.Checked = _settings.Encryption != 0;

            _clientPathBox.Text = !string.IsNullOrEmpty(_settings.ClientPath) && File.Exists(_settings.ClientPath)
                ? _settings.ClientPath
                : DetectDefaultClient();

            UpdateAssistantUi();
            UpdateAssistantDownloadUi();
            UpdateUoDownloadUi();
        }

        private void UpdateUoDownloadUi()
        {
            string uoPath = _uoPathBox.Text.Trim().Trim('"');
            bool valid = Directory.Exists(uoPath) && File.Exists(Path.Combine(uoPath, "tiledata.mul"));

            _downloadUoButton.Visible = !valid;
            _uoHintLabel.Text = valid
                ? "Client Ultima Online pronto."
                : "Client non trovato — scarica il pacchetto UODreams o seleziona la cartella con tiledata.mul.";
            _uoHintLabel.ForeColor = valid ? Theme.SectionGreen : Theme.TextMuted;
        }

        private static string GetClientDir() => ClientRuntimeDownloader.ClientDir;

        private static string GetBootstrapDir() => ClientRuntimeDownloader.BootstrapDir;

        private static string DetectDefaultClient()
        {
            string clientDir = GetClientDir();
            string modded = Path.Combine(clientDir, "cuo-modded.exe");

            if (File.Exists(modded))
                return modded;

            string legacy = Path.Combine(clientDir, "cuo.exe");
            return File.Exists(legacy) ? legacy : "";
        }

        private static bool IsNativeCuoDll(string cuoDllPath)
        {
            if (!File.Exists(cuoDllPath))
                return false;

            try
            {
                AssemblyName.GetAssemblyName(cuoDllPath);
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static string ResolveClientExecutable(string clientPath, string assistant)
        {
            string clientDir = GetClientDir();

            if (assistant == "Razor Enhanced")
            {
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

        private string SelectedAssistant => _assistantCombo.SelectedItem as string ?? "Nessuno";

        private void UpdateAssistantUi()
        {
            bool hasAssistant = SelectedAssistant != "Nessuno";
            _assistantPathLabel.Visible = hasAssistant;
            _assistantPathPanel.Visible = hasAssistant;
            _assistantBrowseButton.Visible = hasAssistant;

            switch (SelectedAssistant)
            {
                case "ClassicAssist":
                    _assistantPathLabel.Text = "Percorso di ClassicAssist.dll (o della sua cartella)";
                    _assistantPathBox.Text = _settings.ClassicAssistPath;
                    break;
                case "Razor Enhanced":
                    _assistantPathLabel.Text = "Percorso di RazorEnhanced (cartella o RazorEnhanced.exe)";
                    _assistantPathBox.Text = string.IsNullOrWhiteSpace(_settings.RazorPath)
                        ? ClientRuntimeDownloader.DetectRazorEnhancedPath() ?? ""
                        : _settings.RazorPath;
                    break;
                case "Orion":
                    _assistantPathLabel.Text = "Percorso di Orion (cartella o OrionAssistant64.dll)";
                    _assistantPathBox.Text = string.IsNullOrWhiteSpace(_settings.OrionPath)
                        ? ClientRuntimeDownloader.DetectOrionAssistantDll() ?? ""
                        : _settings.OrionPath;
                    break;
                case "UOSteam":
                    _assistantPathLabel.Text = "Percorso di UOSteam (cartella o UOS.dll)";
                    _assistantPathBox.Text = string.IsNullOrWhiteSpace(_settings.UOSteamPath)
                        ? ClientRuntimeDownloader.DetectUOSteamDll() ?? ""
                        : _settings.UOSteamPath;
                    break;
            }

            UpdateAssistantDownloadUi();
        }

        private void UpdateAssistantDownloadUi()
        {
            bool hasAssistant = SelectedAssistant != "Nessuno";
            bool canDownload = hasAssistant && AssistantDownloader.SupportsDownload(SelectedAssistant);
            bool installed = hasAssistant && AssistantDownloader.IsInstalled(SelectedAssistant, _assistantPathBox.Text);

            _downloadAssistantButton.Visible = canDownload && !installed;
            _assistantHintLabel.Visible = hasAssistant;

            if (!hasAssistant)
            {
                return;
            }

            if (installed)
            {
                _assistantHintLabel.Text = $"{SelectedAssistant} pronto.";
                _assistantHintLabel.ForeColor = Theme.SectionGreen;
            }
            else if (canDownload)
            {
                _assistantHintLabel.Text = $"{SelectedAssistant} non trovato — scaricalo o seleziona il percorso del plugin.";
                _assistantHintLabel.ForeColor = Theme.TextMuted;
            }
            else
            {
                _assistantHintLabel.Text = "Seleziona il percorso dell'assistente.";
                _assistantHintLabel.ForeColor = Theme.TextMuted;
            }
        }

        private void BrowseAssistant()
        {
            using var dialog = new OpenFileDialog
            {
                Title = SelectedAssistant switch
                {
                    "ClassicAssist" => "Seleziona ClassicAssist.dll",
                    "Orion" => "Seleziona OrionAssistant64.dll o la cartella di Orion",
                    "UOSteam" => "Seleziona UOS.dll o la cartella di UOSteam",
                    _ => "Seleziona RazorEnhanced.exe o la sua cartella"
                },
                Filter = "Plugin (*.dll;*.exe)|*.dll;*.exe|Tutti i file (*.*)|*.*"
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
                Description = "Seleziona la cartella di Ultima Online (deve contenere tiledata.mul)",
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

            using var folderDialog = new FolderBrowserDialog
            {
                Description = $"Scegli dove installare {SelectedAssistant}",
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
            _statusLabel.Text = $"Download {SelectedAssistant} in corso…";
            _downloadAssistantButton.Enabled = false;

            using var progressForm = DownloadProgressForm.ForAssistant(SelectedAssistant, installDir);
            var result = progressForm.ShowDialog(this);

            _downloadAssistantButton.Enabled = true;

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(progressForm.ResultPath))
            {
                _assistantPathBox.Text = progressForm.ResultPath;
                SaveSettings();
                UpdateAssistantDownloadUi();
                _statusLabel.ForeColor = Theme.SectionGreen;
                _statusLabel.Text = $"{SelectedAssistant} installato correttamente.";
            }
            else if (result == DialogResult.Abort)
            {
                _statusLabel.ForeColor = Theme.Error;
                _statusLabel.Text = $"Download di {SelectedAssistant} non riuscito. Riprova o seleziona manualmente il percorso.";
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
                Description = "Scegli dove salvare il client Ultima Online UODreams",
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
            _statusLabel.Text = "Download client UODreams in corso…";
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
                _statusLabel.Text = "Client UODreams installato correttamente.";
            }
            else if (result == DialogResult.Abort)
            {
                _statusLabel.ForeColor = Theme.Error;
                _statusLabel.Text = "Download non riuscito. Riprova o seleziona manualmente la cartella.";
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

            if (Directory.Exists(path))
            {
                string[] candidates = SelectedAssistant switch
                {
                    "ClassicAssist" => new[] { "ClassicAssist.dll" },
                    "Orion" => new[] { "OA\\OrionAssistant64.dll", "OrionAssistant64.dll", "OA\\OrionAssistant.dll", "OrionAssistant.dll" },
                    "UOSteam" => new[] { "UOS.dll" },
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

        private void SaveSettings()
        {
            _settings.Assistant = SelectedAssistant;
            _settings.UoDirectory = _uoPathBox.Text.Trim();
            _settings.ClientPath = DetectDefaultClient();
            _settings.ShardIp = _ipBox.Text.Trim();
            _settings.ShardPort = (int)_portBox.Value;
            _settings.Encryption = _encryptionCheck.Checked ? 1 : 0;

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
            if (!ClientRuntimeDownloader.IsInstalled())
            {
                using var bootstrapForm = DownloadProgressForm.ForClientRuntime();
                if (bootstrapForm.ShowDialog(this) != DialogResult.OK)
                {
                    ShowError("Componenti di gioco non installati.");
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
                string bootstrapDir = GetBootstrapDir();
                string nativeCuo = Path.Combine(bootstrapDir, "cuo.dll");

                if (!File.Exists(effectiveClient) || !IsNativeCuoDll(nativeCuo))
                {
                    ShowError(
                        "Razor Enhanced richiede la cartella Client\\Bootstrap con ClassicUO.exe e cuo.dll nativo.\n" +
                        "Il client moddato (cuo-modded.exe) non può caricare Razor in-process."
                    );
                    return;
                }
            }
            else if (!File.Exists(effectiveClient))
            {
                ShowError("Client non trovato. Seleziona ClassicUO.exe o cuo-modded.exe nella cartella Client.");
                return;
            }

            if (!Directory.Exists(uoPath) || !File.Exists(Path.Combine(uoPath, "tiledata.mul")))
            {
                ShowError("Cartella di Ultima Online non valida (manca tiledata.mul).");
                return;
            }

            string? assistantDll = null;

            if (SelectedAssistant != "Nessuno")
            {
                assistantDll = ResolveAssistantDll();

                if (assistantDll == null)
                {
                    ShowError($"Percorso di {SelectedAssistant} non valido.");
                    return;
                }

                if (SelectedAssistant != "Razor Enhanced" && !Is64BitCompatible(assistantDll))
                {
                    string hint = SelectedAssistant == "UOSteam"
                        ? "UOSteam (UOS.dll) è a 32 bit e non può essere caricato dal client ClassicUO a 64 bit.\n" +
                          "Usa ClassicAssist (sintassi simile a UOSteam), Razor Enhanced o Orion."
                        : Path.GetFileName(assistantDll) +
                          " è a 32 bit (x86) e non può essere caricato dal client a 64 bit.\n" +
                          "Usa Razor Enhanced (bootstrap + cuo.dll nativo), ClassicAssist o Orion.";

                    ShowError(hint);
                    return;
                }
            }

            SaveSettings();

            var psi = new ProcessStartInfo
            {
                FileName = effectiveClient,
                WorkingDirectory = Path.GetDirectoryName(effectiveClient)!,
                UseShellExecute = false,
                CreateNoWindow = true
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

            try
            {
                Process.Start(psi);
                _statusLabel.ForeColor = Theme.TextMuted;
                _statusLabel.Text = SelectedAssistant == "Nessuno"
                    ? "Client avviato."
                    : $"Client avviato con {SelectedAssistant}.";

                Close();
            }
            catch (Exception ex)
            {
                ShowError("Errore durante l'avvio: " + ex.Message);
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

        private void ShowError(string message)
        {
            _statusLabel.ForeColor = Theme.Error;
            _statusLabel.Text = message;
        }
    }
}
