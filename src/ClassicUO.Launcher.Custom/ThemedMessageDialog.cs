using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    // Dark-themed dialogs matching the launcher palette (Theme + ThemedButton).
    internal static class ThemedMessageDialog
    {
        private const int Padding = 24;
        private const int ButtonWidth = 120;
        private const int ButtonHeight = 38;
        private const int ButtonGap = 10;
        private const int DialogWidth = 460;
        private const int MinDialogHeight = 170;

        public static void ShowInfo(IWin32Window? owner, string title, string message) =>
            ShowDialog(owner, title, message, confirmOnly: true);

        public static bool ShowConfirm(
            IWin32Window? owner,
            string title,
            string message,
            string confirmText = "OK",
            string cancelText = "Cancel")
        {
            using var dialog = CreateDialog(owner, title, message, confirmOnly: false, confirmText, cancelText);
            DialogResult result = ShowModal(dialog, owner);
            return result == DialogResult.Yes;
        }

        private static void ShowDialog(
            IWin32Window? owner,
            string title,
            string message,
            bool confirmOnly,
            string confirmText = "OK",
            string cancelText = "Cancel")
        {
            using var dialog = CreateDialog(owner, title, message, confirmOnly, confirmText, cancelText);
            ShowModal(dialog, owner);
        }

        private static DialogResult ShowModal(Form dialog, IWin32Window? owner)
        {
            if (owner is Control { Visible: true })
            {
                return dialog.ShowDialog(owner);
            }

            dialog.ShowDialog();
            return dialog.DialogResult;
        }

        private static Form CreateDialog(
            IWin32Window? owner,
            string title,
            string message,
            bool confirmOnly,
            string confirmText = "OK",
            string cancelText = "Cancel")
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

            int buttonY = clientHeight - Padding - ButtonHeight;
            ThemedButton primaryButton;
            if (confirmOnly)
            {
                primaryButton = new ThemedButton
                {
                    Text = confirmText,
                    UseGradient = true,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                    Bounds = new Rectangle(DialogWidth - Padding - ButtonWidth, buttonY, ButtonWidth, ButtonHeight)
                };
                primaryButton.Click += (_, _) =>
                {
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };
                dialog.Controls.Add(primaryButton);
                dialog.AcceptButton = primaryButton;
            }
            else
            {
                var cancelButton = new ThemedButton
                {
                    Text = cancelText,
                    Bounds = new Rectangle(DialogWidth - Padding - ButtonWidth * 2 - ButtonGap, buttonY, ButtonWidth, ButtonHeight)
                };
                cancelButton.Click += (_, _) =>
                {
                    dialog.DialogResult = DialogResult.No;
                    dialog.Close();
                };

                primaryButton = new ThemedButton
                {
                    Text = confirmText,
                    UseGradient = true,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                    Bounds = new Rectangle(DialogWidth - Padding - ButtonWidth, buttonY, ButtonWidth, ButtonHeight)
                };
                primaryButton.Click += (_, _) =>
                {
                    dialog.DialogResult = DialogResult.Yes;
                    dialog.Close();
                };

                dialog.Controls.Add(cancelButton);
                dialog.Controls.Add(primaryButton);
                dialog.AcceptButton = primaryButton;
                dialog.CancelButton = cancelButton;
            }

            dialog.Controls.Add(messageLabel);

            return dialog;
        }
    }
}
