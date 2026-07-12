using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    // Themed dialog to add or edit a custom shard server (name / ip / port / encryption).
    internal sealed class ShardServerDialog : Form
    {
        private readonly TextBox _nameBox;
        private readonly TextBox _ipBox;
        private readonly NumericUpDown _portBox;
        private readonly CheckBox _encryptionCheck;
        private readonly ThemedButton _okButton;
        private readonly bool _editMode;

        public ShardServer? Result { get; private set; }
        public int Encryption { get; private set; }

        private ShardServerDialog(bool editMode)
        {
            _editMode = editMode;
            Text = editMode
                ? Loc.S("Modifica server", "Edit server")
                : Loc.S("Aggiungi server", "Add server");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 318);
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
            y += 42;

            _encryptionCheck = new CheckBox
            {
                Text = Loc.S("Crittografia", "Encryption"),
                ForeColor = Theme.Text,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(x, y)
            };
            Controls.Add(_encryptionCheck);

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

            _okButton = new ThemedButton
            {
                Text = editMode ? Loc.S("Salva", "Save") : Loc.S("Aggiungi", "Add"),
                UseGradient = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Bounds = new Rectangle(x + w - 96, buttonY, 96, buttonHeight)
            };
            _okButton.Click += OnConfirm;
            Controls.Add(_okButton);

            AcceptButton = _okButton;
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
                    Text,
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
            Encryption = _encryptionCheck.Checked ? 1 : 0;

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

        public static (ShardServer Server, int Encryption)? PromptAdd(IWin32Window owner)
        {
            using var dialog = new ShardServerDialog(editMode: false);
            if (dialog.ShowDialog(owner) != DialogResult.OK || dialog.Result == null)
            {
                return null;
            }

            return (dialog.Result, dialog.Encryption);
        }

        public static ShardServer? Prompt(IWin32Window owner)
        {
            return PromptAdd(owner)?.Server;
        }

        public static (ShardServer? Server, int Encryption)? Edit(IWin32Window owner, ShardServer existing, int encryption)
        {
            using var dialog = new ShardServerDialog(editMode: true);
            dialog._nameBox.Text = existing.Name;
            dialog._ipBox.Text = existing.Ip;
            dialog._portBox.Value = Math.Min(Math.Max(existing.Port, 1), 65535);
            dialog._encryptionCheck.Checked = encryption != 0;

            if (dialog.ShowDialog(owner) != DialogResult.OK)
            {
                return null;
            }

            return (dialog.Result, dialog.Encryption);
        }
    }
}
