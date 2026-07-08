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
        public static readonly Color LinkText = Color.FromArgb(205, 205, 228);
        public static readonly Color ComboPlaceholderText = Color.FromArgb(100, 120, 120, 145);
        public static readonly Color SectionGreen = Color.FromArgb(38, 208, 124);
        public static readonly Color GradientStart = Color.FromArgb(78, 107, 245);
        public static readonly Color GradientEnd = Color.FromArgb(138, 92, 246);
        public static readonly Color ButtonNeutral = Color.FromArgb(38, 38, 64);
        public static readonly Color ButtonNeutralHover = Color.FromArgb(50, 50, 82);
        public static readonly Color Error = Color.FromArgb(244, 105, 105);
        public static readonly Color DiscordAccent = Color.FromArgb(88, 101, 242);
        public static readonly Color LinkHoverBorder = Color.FromArgb(78, 107, 245);

        private static Font? _comboFont;

        public static Font ComboFont => _comboFont ??= new Font("Segoe UI", 9.5f, FontStyle.Regular);

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
        public bool PulseHighlight { get; set; }
        public float HighlightPulse { get; set; }
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

            if (PulseHighlight && HighlightPulse > 0.01f)
            {
                int alpha = (int)(60 + 140 * HighlightPulse);
                using var glowPen = new Pen(Color.FromArgb(alpha, Theme.GradientStart), 2f);
                e.Graphics.DrawPath(glowPen, path);

                int fillAlpha = (int)(25 + 45 * HighlightPulse);
                using var glowFill = new SolidBrush(Color.FromArgb(fillAlpha, Theme.GradientEnd));
                e.Graphics.FillPath(glowFill, path);
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
            ForeColor = Theme.LinkText;
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

            Color textColor = AccentColor ?? (_hover ? Theme.Text : Theme.LinkText);
            if (_down && AccentColor == null)
                textColor = Theme.LinkText;

            TextRenderer.DrawText(
                e.Graphics, Text, Font, rect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            );
        }
    }

    // Combo with dark themed field, custom border and dropdown list (no default white chrome).
    internal sealed class ThemedComboBox : ComboBox
    {
        private const int WM_PAINT = 0x000F;
        private const int ArrowWidth = 28;
        private const int CornerRadius = 8;

        // When true, index 0 uses a faded placeholder label; other items stay bright.
        public bool PlaceholderFirstItem { get; set; }

        public ThemedComboBox()
        {
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 26;
            BackColor = Theme.Input;
            ForeColor = Theme.Text;
            Font = Theme.ComboFont;
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_PAINT && IsHandleCreated && Width > 0 && Height > 0)
            {
                using var g = Graphics.FromHwnd(Handle);
                PaintSurface(g);
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0)
            {
                return;
            }

            bool isPlaceholder = PlaceholderFirstItem && e.Index == 0;
            bool selected = (e.State & DrawItemState.Selected) != 0;

            Color bg = selected ? Theme.ButtonNeutralHover : Theme.Input;
            Color textColor = isPlaceholder ? Theme.ComboPlaceholderText : Theme.Text;

            using var brush = new SolidBrush(bg);
            e.Graphics.FillRectangle(brush, e.Bounds);

            string itemText = Items[e.Index]?.ToString() ?? "";
            TextRenderer.DrawText(
                e.Graphics,
                itemText,
                Font,
                new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height),
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private void PaintSurface(Graphics g)
        {
            bool placeholderState = PlaceholderFirstItem && SelectedIndex <= 0;
            Color textColor = placeholderState ? Theme.ComboPlaceholderText : Theme.Text;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundedRect(rect, CornerRadius);
            using (var fill = new SolidBrush(Theme.Input))
            {
                g.FillPath(fill, path);
            }

            using (var pen = new Pen(Theme.InputBorder))
            {
                g.DrawPath(pen, path);
            }

            var arrowRect = new Rectangle(Width - ArrowWidth, 2, ArrowWidth - 2, Height - 4);
            using (var arrowPath = Theme.RoundedRect(arrowRect, 6))
            using (var arrowFill = new SolidBrush(Theme.ButtonNeutral))
            {
                g.FillPath(arrowFill, arrowPath);
            }

            int cx = Width - ArrowWidth / 2;
            int cy = Height / 2;
            using var arrowPen = new Pen(Theme.Text, 2f);
            g.DrawLine(arrowPen, cx - 4, cy - 1, cx, cy + 3);
            g.DrawLine(arrowPen, cx, cy + 3, cx + 4, cy - 1);

            string text = SelectedIndex >= 0 ? Items[SelectedIndex]?.ToString() ?? "" : "";
            var textRect = new Rectangle(10, 0, Width - ArrowWidth - 14, Height);
            TextRenderer.DrawText(
                g,
                text,
                Font,
                textRect,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
