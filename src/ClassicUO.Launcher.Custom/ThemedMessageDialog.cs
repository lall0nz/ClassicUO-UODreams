using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    // Dark-themed info dialog matching the launcher palette (Theme + ThemedButton).
    internal static class ThemedMessageDialog
    {
        private const int Padding = 24;
        private const int ButtonWidth = 120;
        private const int ButtonHeight = 38;
        private const int DialogWidth = 440;
        private const int MinDialogHeight = 170;

        public static void ShowInfo(IWin32Window? owner, string title, string message)
        {
            using var dialog = CreateDialog(owner, title, message);
            if (owner is Control { Visible: true } parent)
            {
                dialog.ShowDialog(parent);
            }
            else
            {
                dialog.ShowDialog();
            }
        }

        private static Form CreateDialog(IWin32Window? owner, string title, string message)
        {
            int messageWidth = DialogWidth - Padding * 2;
            var messageFont = Theme.ComboFont;
            Size textSize = TextRenderer.MeasureText(
                message,
                messageFont,
                new Size(messageWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.Left);

            int clientHeight = Math.Max(
                MinDialogHeight,
                Padding + textSize.Height + Padding + ButtonHeight + Padding);

            var dialog = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(DialogWidth, clientHeight),
                BackColor = Theme.WindowBottom,
                ForeColor = Theme.Text,
                Font = Theme.ComboFont
            };

            if (owner is Form ownerForm)
            {
                try
                {
                    dialog.Icon = ownerForm.Icon;
                }
                catch
                {
                    // ignore icon issues
                }
            }

            var messageLabel = new Label
            {
                Text = message,
                ForeColor = Theme.Text,
                BackColor = Color.Transparent,
                AutoSize = false,
                Bounds = new Rectangle(Padding, Padding, messageWidth, textSize.Height),
                TextAlign = ContentAlignment.TopLeft,
                Font = messageFont
            };

            var okButton = new ThemedButton
            {
                Text = Loc.S("OK", "OK"),
                UseGradient = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Bounds = new Rectangle(
                    DialogWidth - Padding - ButtonWidth,
                    clientHeight - Padding - ButtonHeight,
                    ButtonWidth,
                    ButtonHeight)
            };
            okButton.Click += (_, _) =>
            {
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };

            dialog.Controls.Add(messageLabel);
            dialog.Controls.Add(okButton);
            dialog.AcceptButton = okButton;

            return dialog;
        }
    }
}
