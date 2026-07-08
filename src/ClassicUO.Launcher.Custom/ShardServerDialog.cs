using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    // Small themed dialog to add a custom shard server (name / ip / port).
    internal sealed class ShardServerDialog : Form
    {
        private readonly TextBox _nameBox;
        private readonly TextBox _ipBox;
        private readonly NumericUpDown _portBox;

        public ShardServer? Result { get; private set; }

        private ShardServerDialog()
        {
            Text = Loc.S("Aggiungi server", "Add server");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 286);
            BackColor = Theme.WindowBottom;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);

            int x = 20;
            int w = ClientSize.Width - x * 2;
            int y = 18;

            Controls.Add(FieldLabel(Loc.S("Nome", "Name"), x, y, w));
            y += 20;
            (Panel namePanel, _nameBox) = TextField(x, y, w);
            Controls.Add(namePanel);
            y += 42;

            Controls.Add(FieldLabel(Loc.S("Indirizzo (IP o host)", "Address (IP or host)"), x, y, w));
            y += 20;
            (Panel ipPanel, _ipBox) = TextField(x, y, w);
            Controls.Add(ipPanel);
            y += 42;

            Controls.Add(FieldLabel(Loc.S("Porta", "Port"), x, y, w));
            y += 20;
            var portPanel = new InputPanel { Bounds = new Rectangle(x, y, 110, 32) };
            _portBox = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 65535,
                Value = 2593,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                Dock = DockStyle.Fill
            };
            portPanel.Controls.Add(_portBox);
            Controls.Add(portPanel);

            const int buttonHeight = 34;
            const int bottomPadding = 24;
            int buttonY = ClientSize.Height - bottomPadding - buttonHeight;

            var cancelButton = new ThemedButton
            {
                Text = Loc.S("Annulla", "Cancel"),
                Bounds = new Rectangle(x + w - 200, buttonY, 96, buttonHeight)
            };
            cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancelButton);

            var okButton = new ThemedButton
            {
                Text = Loc.S("Aggiungi", "Add"),
                UseGradient = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Bounds = new Rectangle(x + w - 96, buttonY, 96, buttonHeight)
            };
            okButton.Click += OnConfirm;
            Controls.Add(okButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private void OnConfirm(object? sender, EventArgs e)
        {
            string name = _nameBox.Text.Trim();
            string ip = _ipBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(ip))
            {
                MessageBox.Show(
                    this,
                    Loc.S("Inserisci un nome e un indirizzo validi.", "Enter a valid name and address."),
                    Loc.S("Aggiungi server", "Add server"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            Result = new ShardServer
            {
                Name = name,
                Ip = ip,
                Port = (int)_portBox.Value
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label FieldLabel(string text, int x, int y, int w) => new()
        {
            Text = text,
            ForeColor = Theme.TextMuted,
            BackColor = Color.Transparent,
            AutoSize = false,
            Bounds = new Rectangle(x, y, w, 16)
        };

        private static (Panel, TextBox) TextField(int x, int y, int w)
        {
            var panel = new InputPanel { Bounds = new Rectangle(x, y, w, 32) };
            var box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                Dock = DockStyle.Fill
            };
            panel.Controls.Add(box);
            return (panel, box);
        }

        public static ShardServer? Prompt(IWin32Window owner)
        {
            using var dialog = new ShardServerDialog();
            return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.Result : null;
        }
    }
}
