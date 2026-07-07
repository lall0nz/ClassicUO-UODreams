using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    // Palette modeled on the official ClassicUOLauncher (dark navy + blue/violet gradients).
    internal static class Theme
    {
        public static readonly Color WindowTop = Color.FromArgb(23, 23, 38);
        public static readonly Color WindowBottom = Color.FromArgb(14, 14, 24);
        public static readonly Color Card = Color.FromArgb(28, 28, 46);
        public static readonly Color CardBorder = Color.FromArgb(46, 46, 74);
        public static readonly Color Input = Color.FromArgb(38, 38, 58);
        public static readonly Color InputBorder = Color.FromArgb(58, 58, 88);
        public static readonly Color Text = Color.FromArgb(235, 235, 245);
        public static readonly Color TextMuted = Color.FromArgb(150, 150, 175);
        public static readonly Color SectionGreen = Color.FromArgb(38, 208, 124);
        public static readonly Color GradientStart = Color.FromArgb(78, 107, 245);
        public static readonly Color GradientEnd = Color.FromArgb(138, 92, 246);
        public static readonly Color ButtonNeutral = Color.FromArgb(38, 38, 64);
        public static readonly Color ButtonNeutralHover = Color.FromArgb(50, 50, 82);
        public static readonly Color Error = Color.FromArgb(244, 105, 105);
        public static readonly Color DiscordAccent = Color.FromArgb(88, 101, 242);
        public static readonly Color LinkHoverBorder = Color.FromArgb(78, 107, 245);

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Rounded card panel with border, like the settings cards of the official launcher.
    internal sealed class CardPanel : Panel
    {
        public int CornerRadius { get; set; } = 14;

        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundedRect(rect, CornerRadius);
            using var fill = new SolidBrush(Theme.Card);
            using var pen = new Pen(Theme.CardBorder);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        }
    }

    // Rounded input container that hosts a borderless TextBox / control.
    internal sealed class InputPanel : Panel
    {
        public InputPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
            Padding = new Padding(10, 6, 10, 6);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundedRect(rect, 8);
            using var fill = new SolidBrush(Theme.Input);
            using var pen = new Pen(Theme.InputBorder);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        }
    }

    // Button with rounded corners, optional blue->violet gradient and hover glow.
    internal sealed class ThemedButton : Button
    {
        private bool _hover;
        private bool _down;

        public bool UseGradient { get; set; }
        public int CornerRadius { get; set; } = 10;

        public ThemedButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Theme.Text;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // repaint parent background behind our rounded corners
            if (Parent != null)
            {
                using var bg = new SolidBrush(Parent is CardPanel ? Theme.Card : Theme.WindowBottom);
                e.Graphics.FillRectangle(bg, ClientRectangle);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundedRect(rect, CornerRadius);

            if (UseGradient)
            {
                Color start = Theme.GradientStart, end = Theme.GradientEnd;

                if (_hover)
                {
                    start = ControlPaint.Light(start, 0.2f);
                    end = ControlPaint.Light(end, 0.2f);
                }

                if (_down)
                {
                    start = ControlPaint.Dark(Theme.GradientStart, 0.05f);
                    end = ControlPaint.Dark(Theme.GradientEnd, 0.05f);
                }

                using var brush = new LinearGradientBrush(rect, start, end, LinearGradientMode.Horizontal);
                e.Graphics.FillPath(brush, path);
            }
            else
            {
                Color fillColor = _hover ? Theme.ButtonNeutralHover : Theme.ButtonNeutral;
                if (_down) fillColor = Theme.ButtonNeutral;
                using var brush = new SolidBrush(fillColor);
                using var pen = new Pen(Theme.InputBorder);
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                e.Graphics, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            );
        }
    }

    // Compact footer link button with subtle border and hover glow.
    internal sealed class FooterLinkButton : Button
    {
        private bool _hover;
        private bool _down;

        public Color? AccentColor { get; set; }
        public int CornerRadius { get; set; } = 8;

        public FooterLinkButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Theme.TextMuted;
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundedRect(rect, CornerRadius);

            Color fillColor = _down ? Theme.ButtonNeutral : Theme.ButtonNeutral;
            if (_hover)
                fillColor = Theme.ButtonNeutralHover;

            Color borderColor = Theme.InputBorder;
            if (_hover)
                borderColor = AccentColor ?? Theme.LinkHoverBorder;

            using (var brush = new SolidBrush(fillColor))
                e.Graphics.FillPath(brush, path);
            using (var pen = new Pen(borderColor))
                e.Graphics.DrawPath(pen, path);

            Color textColor = AccentColor ?? (_hover ? Theme.Text : Theme.TextMuted);
            if (_down && AccentColor == null)
                textColor = Theme.TextMuted;

            TextRenderer.DrawText(
                e.Graphics, Text, Font, rect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            );
        }
    }
}
