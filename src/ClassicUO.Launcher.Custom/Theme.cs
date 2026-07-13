using System;

using System.Drawing;

using System.Drawing.Drawing2D;

using System.Drawing.Imaging;

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

        public static readonly Color NeonNeutral = Color.FromArgb(175, 178, 195);



        // Assistant pill accent colors

        public static readonly Color PillRazor = Color.FromArgb(220, 68, 68);

        public static readonly Color PillClassicAssist = Color.FromArgb(68, 132, 245);

        public static readonly Color PillUOSteam = Color.FromArgb(245, 148, 58);

        public static readonly Color PillOrion = Color.FromArgb(168, 168, 178);

        public static readonly Color PillCoffee = Color.FromArgb(255, 193, 58);

        // Shared button metrics (DPI-safe layout constants)
        public const int PrimaryButtonHeight = 32;
        public const int PrimaryButtonRadius = 8;
        public const int CompactPrimaryHeight = 30;
        public const int CompactPrimaryRadius = 6;
        public const int ToolbarButtonHeight = 26;
        public const int ToolbarButtonRadius = 8;
        public const int OutlineAssistantHeight = 42;
        public const int OutlineAssistantRadius = 8;
        public const int OutlineAssistantIconSize = 20;
        public const int OutlineAssistantHorizontalPadding = 4;
        public const int DownloadButtonHorizontalPadding = 22;
        public const int AssistantCardBottomPadding = 14;
        public const int SectionRowGap = 8;

        private static Font? _comboFont;
        private static Font? _outlineAssistantFont;
        private static Font? _compactPrimaryFont;

        public static Font OutlineAssistantFont =>
            _outlineAssistantFont ??= new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);

        public static Font CompactPrimaryFont =>
            _compactPrimaryFont ??= new Font("Segoe UI Semibold", 11f, FontStyle.Bold);

        /// <summary>Download-style filled gradient button.</summary>
        public static void ApplyPrimaryStyle(ThemedButton button)
        {
            button.UseGradient = true;
            button.CornerRadius = PrimaryButtonRadius;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            button.Height = PrimaryButtonHeight;
            button.Padding = new Padding(DownloadButtonHorizontalPadding, 0, DownloadButtonHorizontalPadding, 0);
        }

        /// <summary>Compact filled gradient button (e.g. Buy me a coffee).</summary>
        public static void ApplyCompactPrimaryStyle(ThemedButton button)
        {
            button.UseGradient = true;
            button.CornerRadius = CompactPrimaryRadius;
            button.ForeColor = Color.White;
            button.Font = CompactPrimaryFont;
            button.Height = CompactPrimaryHeight;
            button.Padding = new Padding(16, 0, 16, 0);
        }

        /// <summary>Compact toolbar gradient button (matches Update button height).</summary>
        public static void ApplyToolbarPrimaryStyle(ThemedButton button)
        {
            button.UseGradient = true;
            button.CornerRadius = ToolbarButtonRadius;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            button.Height = ToolbarButtonHeight;
            button.Padding = new Padding(8, 0, 10, 0);
        }

        internal static void DrawRecoloredImage(Graphics g, Image image, Rectangle dest, Color color)
        {
            var matrix = new ColorMatrix(new float[][]
            {
                new float[] {0, 0, 0, 0, 0},
                new float[] {0, 0, 0, 0, 0},
                new float[] {0, 0, 0, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {color.R / 255f, color.G / 255f, color.B / 255f, 0, 1}
            });

            using var attrs = new ImageAttributes();
            attrs.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            g.DrawImage(
                image,
                dest,
                0,
                0,
                image.Width,
                image.Height,
                GraphicsUnit.Pixel,
                attrs);
        }



        public static Font ComboFont => _comboFont ??= new Font("Segoe UI", 9.5f, FontStyle.Regular);

        internal static Color GetSurfaceBackground(Control? control)
        {
            for (Control? parent = control; parent != null; parent = parent.Parent)
            {
                if (parent is CardPanel)
                {
                    return Theme.Card;
                }
            }

            return Theme.WindowBottom;
        }

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

        internal static bool HasPaintableSize(int width, int height) => width > 0 && height > 0;

        internal static bool HasPaintableSize(Rectangle rect) => HasPaintableSize(rect.Width, rect.Height);

        internal static void FillLinearGradient(Graphics g, Rectangle rect, Color start, Color end, LinearGradientMode mode)
        {
            if (!HasPaintableSize(rect))
            {
                return;
            }

            using var brush = new LinearGradientBrush(rect, start, end, mode);
            g.FillRectangle(brush, rect);
        }

        internal static void FillLinearGradientPath(Graphics g, GraphicsPath path, Rectangle rect, Color start, Color end, LinearGradientMode mode)
        {
            if (!HasPaintableSize(rect))
            {
                return;
            }

            using var brush = new LinearGradientBrush(rect, start, end, mode);
            g.FillPath(brush, path);
        }

        // Layered path strokes to mimic the soft neon halo from the official Tauri/WebView launcher.

        internal static void DrawNeonGlow(Graphics g, GraphicsPath path, Color accent, bool hovered)

        {

            DrawNeonGlow(g, path, accent, hovered ? 1f : 0f);

        }



        internal static void DrawNeonGlow(Graphics g, GraphicsPath path, Color accent, float intensity)

        {

            intensity = Math.Clamp(intensity, 0f, 1f);

            bool hovered = intensity > 0.35f;

            var layers = hovered

                ? new (float width, int alpha)[] { (14f, 18), (10f, 32), (6f, 52), (3f, 90) }

                : new (float width, int alpha)[] { (10f, 8), (7f, 14), (4f, 22), (2f, 34) };



            Color glowColor = Color.FromArgb(

                (int)(NeonNeutral.R + (accent.R - NeonNeutral.R) * intensity),

                (int)(NeonNeutral.G + (accent.G - NeonNeutral.G) * intensity),

                (int)(NeonNeutral.B + (accent.B - NeonNeutral.B) * intensity));



            g.CompositingQuality = CompositingQuality.HighQuality;

            foreach ((float width, int alpha) in layers)

            {

                int layerAlpha = (int)(alpha * (hovered ? 0.65f + intensity * 0.55f : 0.55f + intensity * 0.65f));

                using var pen = new Pen(Color.FromArgb(layerAlpha, glowColor), width)

                {

                    LineJoin = LineJoin.Round,

                    StartCap = LineCap.Round,

                    EndCap = LineCap.Round

                };

                g.DrawPath(pen, path);

            }

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

            if (!Theme.HasPaintableSize(Width, Height))
            {
                return;
            }

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

            if (!Theme.HasPaintableSize(Width, Height))
            {
                return;
            }

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

        public bool HighlightAsUpdate { get; set; }

        public int CornerRadius { get; set; } = 10;

        public Image? IconImage { get; set; }

        public int IconSize { get; set; } = 14;

        public Color? IconTintColor { get; set; }



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

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size textSize = TextRenderer.MeasureText(Text, Font, proposedSize, TextFormatFlags.NoPadding);
            const int iconGap = 5;
            int iconExtra = IconImage != null ? IconSize + iconGap : 0;
            int width = textSize.Width + Padding.Horizontal + iconExtra + 4;
            int height = Math.Max(Height > 0 ? Height : Theme.PrimaryButtonHeight, textSize.Height + Padding.Vertical + 4);
            if (MinimumSize.Width > 0)
            {
                width = Math.Max(width, MinimumSize.Width);
            }

            if (MinimumSize.Height > 0)
            {
                height = Math.Max(height, MinimumSize.Height);
            }

            return new Size(width, height);
        }

        protected override void OnPaint(PaintEventArgs e)

        {

            if (!Theme.HasPaintableSize(Width, Height))
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;



            // repaint parent background behind our rounded corners

            using var bg = new SolidBrush(Theme.GetSurfaceBackground(Parent));
            e.Graphics.FillRectangle(bg, ClientRectangle);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = Theme.RoundedRect(rect, CornerRadius);



            Color textColor = ForeColor;



            if (HighlightAsUpdate)

            {

                Color fillColor = Theme.SectionGreen;

                if (_hover) fillColor = ControlPaint.Light(fillColor, 0.15f);

                if (_down) fillColor = ControlPaint.Dark(fillColor, 0.08f);

                using var brush = new SolidBrush(fillColor);

                e.Graphics.FillPath(brush, path);

                textColor = Color.White;

            }

            else if (UseGradient)

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



                Theme.FillLinearGradientPath(e.Graphics, path, rect, start, end, LinearGradientMode.Horizontal);

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



            const int iconGap = 5;
            int iconExtra = IconImage != null ? IconSize + iconGap : 0;
            Size textSize = TextRenderer.MeasureText(e.Graphics, Text, Font, Size.Empty, TextFormatFlags.NoPadding);
            int contentWidth = iconExtra + textSize.Width;
            int startX = rect.Left + Math.Max(Padding.Left, (rect.Width - contentWidth - Padding.Horizontal) / 2 + Padding.Left / 2);
            int centerY = rect.Top + rect.Height / 2;

            if (IconImage != null)
            {
                var iconRect = new Rectangle(startX, centerY - IconSize / 2, IconSize, IconSize);
                if (IconTintColor is Color tint)
                {
                    Theme.DrawRecoloredImage(e.Graphics, IconImage, iconRect, tint);
                }
                else
                {
                    e.Graphics.DrawImage(IconImage, iconRect);
                }

                startX = iconRect.Right + iconGap;
            }

            var textRect = new Rectangle(
                startX,
                rect.Top,
                Math.Max(0, rect.Right - Padding.Right - startX),
                rect.Height);

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                textRect,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

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

            if (!Theme.HasPaintableSize(Width, Height))
            {
                return;
            }

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



    // Read-only path field with end-ellipsis for long paths.
    internal sealed class PathEllipsisTextBox : Control
    {
        private string _text = "";

        public override string Text
        {
            get => _text;
            set => ApplyText(value);
        }

        private void ApplyText(string? value)
        {
            string next = value ?? "";
            if (_text == next)
            {
                return;
            }

            _text = next;
            Invalidate();
            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        public new event EventHandler? TextChanged;

        public PathEllipsisTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Theme.Input;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            TextRenderer.DrawText(
                e.Graphics,
                _text,
                Font,
                ClientRectangle,
                ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }

    // Outline assistant selector: transparent fill, accent border, centered icon + label.
    internal sealed class OutlineAssistantButton : Button
    {
        private bool _hover;
        private bool _down;
        private bool _isSelected;
        private Image? _iconImage;

        public Color AccentColor { get; set; } = Theme.SectionGreen;

        public Image? IconImage
        {
            get => _iconImage;
            set
            {
                _iconImage?.Dispose();
                _iconImage = value;
                Invalidate();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                Invalidate();
            }
        }

        public OutlineAssistantButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Theme.Text;
            Font = Theme.OutlineAssistantFont;
            Cursor = Cursors.Hand;
            Height = Theme.OutlineAssistantHeight;
            TabStop = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _iconImage?.Dispose();
                _iconImage = null;
            }

            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        private static Size MeasureMultilineText(Graphics g, Font font, string[] lines)
        {
            int maxWidth = 0;
            int totalHeight = 0;
            int lineHeight = TextRenderer.MeasureText(g, "Ag", font, Size.Empty, TextFormatFlags.NoPadding).Height;

            for (int i = 0; i < lines.Length; i++)
            {
                Size lineSize = TextRenderer.MeasureText(g, lines[i], font, Size.Empty, TextFormatFlags.NoPadding);
                maxWidth = Math.Max(maxWidth, lineSize.Width);
            }

            totalHeight = Math.Max(lineHeight, lines.Length * lineHeight);
            return new Size(maxWidth, totalHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!Theme.HasPaintableSize(Width, Height))
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var bg = new SolidBrush(Theme.GetSurfaceBackground(Parent));
            e.Graphics.FillRectangle(bg, ClientRectangle);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundedRect(rect, Theme.OutlineAssistantRadius);

            int fillAlpha = _down ? 38 : _isSelected ? 56 : _hover ? 32 : 0;
            if (fillAlpha > 0)
            {
                using var fillBrush = new SolidBrush(Color.FromArgb(fillAlpha, AccentColor));
                e.Graphics.FillPath(fillBrush, path);
            }

            float borderWidth = _isSelected || _hover ? 1.5f : 1f;
            int borderAlpha = _down ? 180 : _isSelected ? 255 : _hover ? 230 : 160;
            using (var pen = new Pen(Color.FromArgb(borderAlpha, AccentColor), borderWidth))
            {
                e.Graphics.DrawPath(pen, path);
            }

            if (Focused)
            {
                var focusRect = Rectangle.Inflate(rect, -2, -2);
                using var focusPath = Theme.RoundedRect(focusRect, Theme.OutlineAssistantRadius - 1);
                using var focusPen = new Pen(Color.FromArgb(120, AccentColor), 1f);
                e.Graphics.DrawPath(focusPen, focusPath);
            }

            const int iconGap = 6;
            int iconSize = Theme.OutlineAssistantIconSize;
            string[] textLines = Text.Split('\n');
            Size textSize = MeasureMultilineText(e.Graphics, Font, textLines);
            int contentWidth = (_iconImage != null ? iconSize + iconGap : 0) + textSize.Width;
            int startX = rect.Left + Math.Max(Theme.OutlineAssistantHorizontalPadding, (rect.Width - contentWidth) / 2);
            int centerY = rect.Top + rect.Height / 2;

            Color contentColor = Color.FromArgb(_down ? 220 : 255, AccentColor);

            if (_iconImage != null)
            {
                var iconRect = new Rectangle(startX, centerY - iconSize / 2, iconSize, iconSize);
                e.Graphics.DrawImage(_iconImage, iconRect);
                startX = iconRect.Right + iconGap;
            }

            int lineHeight = TextRenderer.MeasureText(e.Graphics, "Ag", Font, Size.Empty, TextFormatFlags.NoPadding).Height;
            int textTop = centerY - textSize.Height / 2;
            int textWidth = Math.Max(0, rect.Right - startX - 4);
            for (int i = 0; i < textLines.Length; i++)
            {
                var lineRect = new Rectangle(startX, textTop + i * lineHeight, textWidth, lineHeight);
                TextRenderer.DrawText(
                    e.Graphics,
                    textLines[i],
                    Font,
                    lineRect,
                    contentColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }
    }

    // Static rounded pill button with colored border, optional icon image.

    internal sealed class PillButton : Button

    {

        private static readonly Color PillFill = Color.FromArgb(35, 35, 46);



        private bool _hover;

        private bool _down;

        private bool _isSelected;

        private Image? _iconImage;



        public Color AccentColor { get; set; } = Theme.SectionGreen;

        public string IconText { get; set; } = "";



        public Image? IconImage

        {

            get => _iconImage;

            set

            {

                _iconImage?.Dispose();

                _iconImage = value;

                Invalidate();

            }

        }



        public bool IsSelected

        {

            get => _isSelected;

            set

            {

                if (_isSelected == value)

                {

                    return;

                }



                _isSelected = value;

                Invalidate();

            }

        }



        public PillButton()

        {

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            FlatStyle = FlatStyle.Flat;

            FlatAppearance.BorderSize = 0;

            BackColor = Color.Transparent;

            ForeColor = Theme.Text;

            Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold);

            Cursor = Cursors.Hand;

            CornerRadius = 16;

        }



        public int CornerRadius { get; set; } = 16;



        protected override void Dispose(bool disposing)

        {

            if (disposing)

            {

                _iconImage?.Dispose();

                _iconImage = null;

            }



            base.Dispose(disposing);

        }



        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }

        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }

        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }



        protected override void OnPaint(PaintEventArgs e)

        {

            if (!Theme.HasPaintableSize(Width, Height))
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;



            if (Parent != null)

            {

                using var bg = new SolidBrush(Parent is CardPanel ? Theme.Card : Theme.WindowBottom);

                e.Graphics.FillRectangle(bg, ClientRectangle);

            }



            var pillRect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = Theme.RoundedRect(pillRect, CornerRadius);



            using (var brush = new SolidBrush(PillFill))

            {

                e.Graphics.FillPath(brush, path);

            }



            int borderAlpha = _down ? 120 : _hover ? 255 : _isSelected ? 210 : 140;

            float borderWidth = _hover || _isSelected ? 1.5f : 1f;

            using (var pen = new Pen(Color.FromArgb(borderAlpha, AccentColor), borderWidth))

            {

                e.Graphics.DrawPath(pen, path);

            }



            const int iconSize = 16;

            const int iconGap = 5;

            int contentLeft = pillRect.Left + 8;

            int contentRight = pillRect.Right - 8;

            int contentTop = pillRect.Top;

            int contentHeight = pillRect.Height;



            if (_iconImage != null)

            {

                var iconRect = new Rectangle(contentLeft, contentTop + (contentHeight - iconSize) / 2, iconSize, iconSize);

                e.Graphics.DrawImage(_iconImage, iconRect);

                contentLeft = iconRect.Right + iconGap;

            }

            else if (!string.IsNullOrEmpty(IconText))

            {

                var iconRect = new Rectangle(contentLeft, contentTop, iconSize + 2, contentHeight);

                TextRenderer.DrawText(

                    e.Graphics, IconText, Font, iconRect, ForeColor,

                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                contentLeft = iconRect.Right + 2;

            }



            var textRect = new Rectangle(contentLeft, contentTop, Math.Max(0, contentRight - contentLeft), contentHeight);

            TextRenderer.DrawText(

                e.Graphics, Text, Font, textRect, ForeColor,

                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        }

    }



    // Compact "..." button for inline folder/file browse on path rows.

    internal sealed class BrowseDotsButton : Button

    {

        private bool _hover;



        public BrowseDotsButton()

        {

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            FlatStyle = FlatStyle.Flat;

            FlatAppearance.BorderSize = 0;

            BackColor = Color.Transparent;

            ForeColor = Theme.TextMuted;

            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);

            Text = "...";

            Cursor = Cursors.Hand;

            Size = new Size(24, 28);

        }



        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }

        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }



        protected override void OnPaint(PaintEventArgs e)

        {

            if (!Theme.HasPaintableSize(Width, Height))
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;



            using var bg = new SolidBrush(Theme.GetSurfaceBackground(Parent));
            e.Graphics.FillRectangle(bg, ClientRectangle);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = Theme.RoundedRect(rect, 6);



            Color fill = _hover ? Theme.ButtonNeutralHover : Theme.ButtonNeutral;

            using (var brush = new SolidBrush(fill))

                e.Graphics.FillPath(brush, path);

            using (var pen = new Pen(_hover ? Theme.SectionGreen : Theme.InputBorder))

                e.Graphics.DrawPath(pen, path);



            Color textColor = _hover ? Theme.SectionGreen : Theme.TextMuted;

            TextRenderer.DrawText(

                e.Graphics, Text, Font, rect, textColor,

                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        }

    }



    // Compact trash icon button for clearing individual path fields.

    internal sealed class TrashIconButton : Button

    {

        private bool _hover;



        public TrashIconButton()

        {

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            FlatStyle = FlatStyle.Flat;

            FlatAppearance.BorderSize = 0;

            BackColor = Color.Transparent;

            ForeColor = Theme.TextMuted;

            Font = new Font("Segoe UI", 8.5f);

            Text = "🗑";

            Cursor = Cursors.Hand;

            Size = new Size(24, 28);

        }



        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }

        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }



        protected override void OnPaint(PaintEventArgs e)

        {

            if (!Theme.HasPaintableSize(Width, Height))
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;



            using var bg = new SolidBrush(Theme.GetSurfaceBackground(Parent));
            e.Graphics.FillRectangle(bg, ClientRectangle);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = Theme.RoundedRect(rect, 6);



            Color fill = _hover ? Theme.ButtonNeutralHover : Theme.ButtonNeutral;

            using (var brush = new SolidBrush(fill))

                e.Graphics.FillPath(brush, path);

            using (var pen = new Pen(_hover ? Theme.Error : Theme.InputBorder))

                e.Graphics.DrawPath(pen, path);



            Color textColor = _hover ? Theme.Error : Theme.TextMuted;

            TextRenderer.DrawText(

                e.Graphics, Text, Font, rect, textColor,

                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

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


