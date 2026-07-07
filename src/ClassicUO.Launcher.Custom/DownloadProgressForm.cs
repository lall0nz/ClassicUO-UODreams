using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    public sealed class DownloadProgressForm : Form
    {
        private readonly Func<IProgress<DownloadProgressReport>, CancellationToken, Task<string?>> _downloadWork;
        private readonly CancellationTokenSource _cts = new();
        private readonly Label _titleLabel;
        private readonly Label _detailLabel;
        private readonly Label _speedLabel;
        private readonly GreenProgressBar _progressBar;
        private readonly ThemedButton _cancelButton;
        private bool _completed;
        private bool _success;
        private bool _failed;

        public string? ResultPath { get; private set; }

        public static DownloadProgressForm ForUoClient(string extractDirectory) =>
            new(
                async (progress, ct) =>
                    await UoClientDownloader.DownloadAndExtractAsync(extractDirectory, progress, ct)
                        .ConfigureAwait(true),
                "UODreams Launcher — Download client UO",
                "Scaricamento client Ultima Online UODreams…"
            );

        public static DownloadProgressForm ForClientRuntime() =>
            new(
                async (progress, ct) =>
                {
                    await ClientRuntimeDownloader.DownloadAndInstallAsync(progress, ct).ConfigureAwait(true);
                    return AppContext.BaseDirectory;
                },
                "UODreams Launcher — Prima installazione",
                "Download componenti ClassicUO UODreams…"
            );

        public static DownloadProgressForm ForAssistant(string assistant, string installDirectory, string? infoMessage = null) =>
            new(
                async (progress, ct) =>
                    await AssistantDownloader.DownloadAndInstallAsync(assistant, installDirectory, progress, ct)
                        .ConfigureAwait(true),
                Loc.S($"UODreams Launcher — Download {assistant}", $"UODreams Launcher — Download {assistant}"),
                Loc.S($"Scaricamento {assistant}…", $"Downloading {assistant}…"),
                infoMessage
            );

        private DownloadProgressForm(
            Func<IProgress<DownloadProgressReport>, CancellationToken, Task<string?>> downloadWork,
            string windowTitle,
            string title,
            string? infoMessage = null)
        {
            _downloadWork = downloadWork;

            bool hasInfo = !string.IsNullOrWhiteSpace(infoMessage);
            int infoHeight = hasInfo ? 62 : 0;

            Text = windowTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 210 + infoHeight);
            BackColor = Theme.WindowBottom;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            DoubleBuffered = true;

            _titleLabel = new Label
            {
                Text = title,
                ForeColor = Theme.Text,
                BackColor = Color.Transparent,
                AutoSize = false,
                Bounds = new Rectangle(24, 20, 472, 24),
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold)
            };

            _detailLabel = new Label
            {
                Text = "Preparazione…",
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Bounds = new Rectangle(24, 52, 472, 20)
            };

            _speedLabel = new Label
            {
                Text = "",
                ForeColor = Theme.SectionGreen,
                BackColor = Color.Transparent,
                AutoSize = false,
                Bounds = new Rectangle(24, 76, 472, 20)
            };

            _progressBar = new GreenProgressBar
            {
                Bounds = new Rectangle(24, 108, 472, 22)
            };

            _cancelButton = new ThemedButton
            {
                Text = Loc.S("Annulla", "Cancel"),
                Bounds = new Rectangle(402, 148 + infoHeight, 94, 34)
            };
            _cancelButton.Click += OnCancelClick;

            Controls.Add(_titleLabel);
            Controls.Add(_detailLabel);
            Controls.Add(_speedLabel);
            Controls.Add(_progressBar);

            if (hasInfo)
            {
                var infoPanel = new InputPanel
                {
                    Bounds = new Rectangle(24, 138, 472, infoHeight - 6)
                };
                var infoLabel = new Label
                {
                    Text = infoMessage,
                    ForeColor = Theme.SectionGreen,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10, 6, 10, 6),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold)
                };
                infoPanel.Controls.Add(infoLabel);
                Controls.Add(infoPanel);
            }

            Controls.Add(_cancelButton);

            Shown += OnFormShown;
            FormClosing += (_, _) =>
            {
                if (!_completed && !_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
            };
        }

        private async void OnFormShown(object? sender, EventArgs e)
        {
            Shown -= OnFormShown;
            await RunDownloadAsync();
        }

        private void OnCancelClick(object? sender, EventArgs e)
        {
            if (!_completed)
            {
                _cts.Cancel();
                return;
            }

            DialogResult = _success
                ? DialogResult.OK
                : _failed
                    ? DialogResult.Abort
                    : DialogResult.Cancel;
            Close();
        }

        private async Task RunDownloadAsync()
        {
            var progress = new Progress<DownloadProgressReport>(report =>
            {
                try
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    _detailLabel.Text = report.Status;

                    if (report.TotalBytes is > 0)
                    {
                        double ratio = Math.Clamp(
                            (double)report.BytesReceived / report.TotalBytes.Value,
                            0,
                            1
                        );
                        _progressBar.Value = (int)(ratio * 1000);

                        string received = UoClientDownloader.FormatBytes(report.BytesReceived);
                        string total = UoClientDownloader.FormatBytes(report.TotalBytes.Value);
                        int percent = (int)Math.Round(ratio * 100);

                        if (report.BytesPerSecond > 0)
                        {
                            string speed = UoClientDownloader.FormatBytes((long)report.BytesPerSecond);
                            _speedLabel.Text = $"{received} / {total}  ({percent}%)  •  {speed}/s";
                        }
                        else
                        {
                            _speedLabel.Text = $"{received} / {total}  ({percent}%)";
                        }
                    }
                    else if (report.BytesReceived > 0)
                    {
                        _speedLabel.Text = $"{UoClientDownloader.FormatBytes(report.BytesReceived)} scaricati";
                        _progressBar.Value = Math.Min(_progressBar.Value + 1, 999);
                    }
                }
                catch
                {
                    // ignore UI update errors during progress
                }
            });

            try
            {
                ResultPath = await _downloadWork(progress, _cts.Token).ConfigureAwait(true);

                _success = true;
                _completed = true;
                _progressBar.Value = 1000;
                _titleLabel.Text = "Download completato";
                _detailLabel.Text = ResultPath ?? "Pronto.";
                _speedLabel.Text = "Pronto per giocare.";
                _cancelButton.Text = "Chiudi";
            }
            catch (OperationCanceledException)
            {
                _completed = true;
                _titleLabel.Text = "Download annullato";
                _detailLabel.Text = "Operazione interrotta.";
                _speedLabel.Text = "";
                _cancelButton.Text = "Chiudi";
            }
            catch (Exception ex)
            {
                _failed = true;
                _completed = true;
                _titleLabel.ForeColor = Theme.Error;
                _titleLabel.Text = "Errore download";
                _detailLabel.Text = ex.Message;
                _speedLabel.Text = "Controlla la connessione e riprova.";
                _cancelButton.Text = "Chiudi";
            }
        }
    }

    internal sealed class GreenProgressBar : Control
    {
        private int _value;

        public int Value
        {
            get => _value;
            set
            {
                int clamped = Math.Clamp(value, 0, 1000);
                if (_value == clamped)
                {
                    return;
                }

                _value = clamped;
                Invalidate();
            }
        }

        public GreenProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Height = 22;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var trackPath = Theme.RoundedRect(rect, 10);
            using var trackBrush = new SolidBrush(Theme.Input);
            using var trackPen = new Pen(Theme.InputBorder);
            e.Graphics.FillPath(trackBrush, trackPath);
            e.Graphics.DrawPath(trackPen, trackPath);

            if (_value <= 0)
            {
                return;
            }

            int fillWidth = Math.Max(8, (int)((Width - 2) * (_value / 1000.0)));
            var fillRect = new Rectangle(1, 1, fillWidth, Height - 3);
            using var fillPath = Theme.RoundedRect(fillRect, 9);
            using var fillBrush = new LinearGradientBrush(
                fillRect,
                Color.FromArgb(34, 197, 94),
                Color.FromArgb(22, 163, 74),
                LinearGradientMode.Horizontal
            );
            e.Graphics.FillPath(fillBrush, fillPath);
        }
    }
}
