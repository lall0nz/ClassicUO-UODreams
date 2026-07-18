using System;

using System.Drawing;

using System.Drawing.Drawing2D;

using System.Drawing.Imaging;

using System.Runtime.InteropServices;

using System.Windows.Forms;



namespace ClassicUO.Launcher.Custom

{

    /// <summary>
    /// Implemented by forms whose client background paints a vertical gradient (instead of a flat
    /// color), so child controls hosted directly on the form can sample the exact backdrop color
    /// behind them (see <see cref="Theme.GetSurfaceBrush"/>).
    /// </summary>
    internal interface IGradientBackgroundHost
    {
        Color GetBackgroundColorAt(int clientY, int clientHeight);
    }

    // Palette modeled on the official ClassicUOLauncher (dark navy + blue/violet gradients).
    // ONEUO edition can switch Window/Card colors at runtime via ApplyLauncherTheme.

    internal static class Theme

    {

        public static Color WindowTop { get; private set; } = Color.FromArgb(14, 14, 24);

        public static Color WindowBottom { get; private set; } = Color.FromArgb(23, 23, 38);

        /// <summary>
        /// Flat background used by dialogs/panels that do NOT paint the main window's vertical
        /// gradient (RegisterForm, ShardServerDialog, ThemedMessageDialog, DownloadProgressForm),
        /// and as the fallback surface color for controls that aren't hosted on a gradient form.
        /// Kept separate from <see cref="WindowBottom"/> so reversing the window gradient direction
        /// never changes the look of those flat dialogs.
        /// </summary>
        public static Color DialogBackground { get; private set; } = Color.FromArgb(14, 14, 24);

        public static Color Card { get; private set; } = Color.FromArgb(28, 28, 46);

        public static Color CardBorder { get; private set; } = Color.FromArgb(46, 46, 74);

        public static Color Input { get; private set; } = Color.FromArgb(38, 38, 58);

        public static Color InputBorder { get; private set; } = Color.FromArgb(58, 58, 88);

        public static Color ButtonNeutral { get; private set; } = Color.FromArgb(38, 38, 64);

        public static Color ButtonNeutralHover { get; private set; } = Color.FromArgb(50, 50, 82);

        public static readonly Color Text = Color.FromArgb(235, 235, 245);

        public static readonly Color TextMuted = Color.FromArgb(150, 150, 175);

        public static readonly Color LinkText = Color.FromArgb(205, 205, 228);

        public static readonly Color ComboPlaceholderText = Color.FromArgb(100, 120, 120, 145);

        public static readonly Color SectionGreen = Color.FromArgb(38, 208, 124);

        public static Color GradientStart { get; private set; } = Color.FromArgb(78, 107, 245);

        public static Color GradientEnd { get; private set; } = Color.FromArgb(138, 92, 246);

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

        // Donation footer pill colors
        public static readonly Color PillPayPalFill = Color.FromArgb(18, 32, 58);
        public static readonly Color PillPayPalAccent = Color.FromArgb(0, 112, 186);
        public static readonly Color PillPayPalText = Color.White;
        public static readonly Color PillPayPalHeart = Color.FromArgb(220, 53, 69);
        public static readonly Color PillCoffeeFill = Color.FromArgb(52, 44, 24);
        public static readonly Color PillCoffeeAccent = Color.FromArgb(210, 168, 48);
        public static readonly Color PillCoffeeText = Color.White;
        public static readonly Color PillCoffeeCupBrown = Color.FromArgb(139, 90, 43);
        public static readonly Color PillCoffeeCupWhite = Color.White;

#if LAUNCHER_EDITION_ONEUO
        public const string DefaultUiThemeId = "black-crimson";
#else
        public const string DefaultUiThemeId = "classic-navy";
#endif

        public readonly record struct LauncherThemePreset(
            string Id,
            string LabelIt,
            string LabelEn,
            Color Window,
            Color Card,
            Color CardBorder,
            Color Input,
            Color InputBorder,
            Color Button,
            Color ButtonHover,
            Color GradientStart,
            Color GradientEnd);

        public static readonly LauncherThemePreset[] LauncherThemes =
        {
            new("black-navy", "Nero + blu", "Black + navy",
                Color.Black, Color.FromArgb(28, 28, 46), Color.FromArgb(46, 46, 74),
                Color.FromArgb(38, 38, 58), Color.FromArgb(58, 58, 88),
                Color.FromArgb(38, 38, 64), Color.FromArgb(50, 50, 82),
                Color.FromArgb(78, 107, 245), Color.FromArgb(138, 92, 246)),
            new("black-crimson", "Nero + rosso scuro", "Black + dark red",
                Color.Black, Color.FromArgb(48, 18, 22), Color.FromArgb(92, 36, 42),
                Color.FromArgb(58, 24, 28), Color.FromArgb(110, 48, 54),
                Color.FromArgb(52, 22, 26), Color.FromArgb(72, 30, 36),
                Color.FromArgb(150, 32, 40), Color.FromArgb(214, 58, 68)),
            new("black-blood", "Nero + blood", "Black + blood",
                Color.Black, Color.FromArgb(36, 12, 14), Color.FromArgb(120, 28, 34),
                Color.FromArgb(48, 16, 18), Color.FromArgb(140, 40, 48),
                Color.FromArgb(40, 14, 16), Color.FromArgb(64, 22, 26),
                Color.FromArgb(120, 20, 26), Color.FromArgb(198, 40, 46)),
            new("black-bronze", "Nero + bronzo", "Black + bronze",
                Color.Black, Color.FromArgb(42, 32, 18), Color.FromArgb(120, 92, 42),
                Color.FromArgb(52, 40, 22), Color.FromArgb(140, 110, 55),
                Color.FromArgb(46, 36, 20), Color.FromArgb(68, 52, 28),
                Color.FromArgb(150, 110, 40), Color.FromArgb(212, 164, 64)),
            new("charcoal-ember", "Antracite + ember", "Charcoal + ember",
                Color.FromArgb(12, 12, 12), Color.FromArgb(54, 22, 16), Color.FromArgb(160, 72, 36),
                Color.FromArgb(64, 28, 20), Color.FromArgb(180, 90, 48),
                Color.FromArgb(48, 22, 16), Color.FromArgb(72, 34, 24),
                Color.FromArgb(170, 80, 30), Color.FromArgb(230, 120, 40)),
            new("black-emerald", "Nero + smeraldo", "Black + emerald",
                Color.Black, Color.FromArgb(14, 36, 28), Color.FromArgb(32, 96, 68),
                Color.FromArgb(18, 44, 34), Color.FromArgb(40, 120, 82),
                Color.FromArgb(16, 40, 30), Color.FromArgb(28, 64, 48),
                Color.FromArgb(28, 120, 86), Color.FromArgb(56, 176, 120)),
            new("classic-navy", "Blu classic", "Classic navy",
                Color.FromArgb(23, 23, 38), Color.FromArgb(28, 28, 46), Color.FromArgb(46, 46, 74),
                Color.FromArgb(38, 38, 58), Color.FromArgb(58, 58, 88),
                Color.FromArgb(38, 38, 64), Color.FromArgb(50, 50, 82),
                Color.FromArgb(78, 107, 245), Color.FromArgb(138, 92, 246)),
        };

        public static string CurrentUiThemeId { get; private set; } = DefaultUiThemeId;

        static Theme()
        {
            // ONEUO defaults to black-crimson (dark red cards); classic/PVP keep classic-navy.
            ApplyLauncherTheme(DefaultUiThemeId);
        }

        public static string ThemeLabel(LauncherThemePreset preset) =>
            Loc.IsEn ? preset.LabelEn : preset.LabelIt;

        /// <summary>Linear interpolation between two colors (t=0 -> a, t=1 -> b).</summary>
        private static Color Blend(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return Color.FromArgb(
                a.R + (int)((b.R - a.R) * t),
                a.G + (int)((b.G - a.G) * t),
                a.B + (int)((b.B - a.B) * t));
        }

        /// <summary>Blends a color toward white by the given amount (0..1).</summary>
        private static Color Lighten(Color c, float amount) => Blend(c, Color.White, amount);

        public static void ApplyLauncherTheme(string? themeId)
        {
            LauncherThemePreset preset = LauncherThemes[0];
            foreach (LauncherThemePreset candidate in LauncherThemes)
            {
                if (string.Equals(candidate.Id, themeId, StringComparison.OrdinalIgnoreCase))
                {
                    preset = candidate;
                    break;
                }
            }

            CurrentUiThemeId = preset.Id;
            // Window always fades darker at the top -> lighter at the bottom, never a flat single
            // color. Tint the lighter stop toward the preset's card hue so black-based presets
            // (Window = pure black) still show a visible gradient instead of two identical stops.
            DialogBackground = preset.Window;
            WindowTop = preset.Window;
            WindowBottom = Lighten(Blend(preset.Window, preset.Card, 0.6f), 0.10f);
            Card = preset.Card;
            CardBorder = preset.CardBorder;
            Input = preset.Input;
            InputBorder = preset.InputBorder;
            ButtonNeutral = preset.Button;
            ButtonNeutralHover = preset.ButtonHover;
            GradientStart = preset.GradientStart;
            GradientEnd = preset.GradientEnd;
        }

        // Shared button metrics (DPI-safe layout constants)
        public const int PrimaryButtonHeight = 32;
        public const int PrimaryButtonRadius = 8;
        public const int CompactPrimaryHeight = 26;
        public const int CompactPrimaryRadius = 6;
        public const int CompactPrimaryHorizontalPadding = 10;
        public const int CompactPrimaryIconSize = 12;
        public const int ToolbarButtonHeight = 26;
        public const int ToolbarButtonRadius = 8;
        public const int OutlineAssistantHeight = 42;
        public const int OutlineAssistantRadius = 8;
        public const int OutlineAssistantIconSize = 20;
        public const int OutlineAssistantHorizontalPadding = 4;
        public const int DownloadButtonHorizontalPadding = 22;
        public const int DonationPillHeight = 27;
        public const int DonationPillRadius = 6;
        public const int DonationPillHorizontalPadding = 8;
        public const int DonationPillIconSize = 14;
        public const int AssistantCardBottomPadding = 14;
        public const int SectionRowGap = 8;

        private static Font? _comboFont;
        private static Font? _outlineAssistantFont;
        private static Font? _compactPrimaryFont;
        private static Font? _primaryButtonFont;
        private static Font? _donationPillFont;
        private static Font? _donationCreditFont;

        public static Font PrimaryButtonFont =>
            _primaryButtonFont ??= new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);

        public static Font DonationPillFont =>
            _donationPillFont ??= new Font("Segoe UI Semibold", 9f, FontStyle.Bold);

        public static Font DonationCreditFont =>
            _donationCreditFont ??= new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);

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
            button.Font = PrimaryButtonFont;
            button.Height = PrimaryButtonHeight;
            button.Padding = new Padding(DownloadButtonHorizontalPadding, 0, DownloadButtonHorizontalPadding, 0);
        }

        /// <summary>Compact filled gradient button (e.g. Buy me a coffee / donation footer).</summary>
        public static void ApplyCompactPrimaryStyle(ThemedButton button)
        {
            button.UseGradient = true;
            button.CornerRadius = CompactPrimaryRadius;
            button.ForeColor = Color.White;
            button.Font = PrimaryButtonFont;
            button.Height = CompactPrimaryHeight;
            button.Padding = new Padding(CompactPrimaryHorizontalPadding, 0, CompactPrimaryHorizontalPadding, 0);
            button.TightContentFit = true;
            button.IconSize = CompactPrimaryIconSize;
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

        /// <summary>Compact donation footer pills (PayPal / Buy me a coffee).</summary>
        public static void ApplyDonationPillStyle(ThemedButton button)
        {
            button.UseGradient = true;
            button.CornerRadius = DonationPillRadius;
            button.ForeColor = Color.White;
            button.Font = PrimaryButtonFont;
            button.Height = DonationPillHeight;
            button.Padding = new Padding(DonationPillHorizontalPadding, 0, DonationPillHorizontalPadding, 0);
            button.TightContentFit = true;
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

        /// <summary>
        /// Brush that matches whatever visually sits behind <paramref name="control"/>, so painting
        /// it into the rectangle behind a rounded-corner control (to clear the square corners before
        /// the rounded shape is drawn on top) blends seamlessly instead of leaving mismatched flat
        /// "corner triangles". Cards use the flat card color; controls sitting directly on a form
        /// that implements <see cref="IGradientBackgroundHost"/> sample the exact vertical-gradient
        /// slice behind their bounds; everything else falls back to the flat dialog background.
        /// </summary>
        internal static Brush GetSurfaceBrush(Control control, Rectangle localBounds)
        {
            for (Control? parent = control.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is CardPanel)
                {
                    return new SolidBrush(Theme.Card);
                }
            }

            if (control.FindForm() is IGradientBackgroundHost host && HasPaintableSize(localBounds))
            {
                var hostControl = (Control)host;
                int formHeight = hostControl.ClientSize.Height;
                if (formHeight > 0)
                {
                    Point topOnScreen = control.PointToScreen(new Point(localBounds.Left, localBounds.Top));
                    Point formOriginOnScreen = hostControl.PointToScreen(Point.Empty);

                    int yTop = topOnScreen.Y - formOriginOnScreen.Y;
                    int yBottom = yTop + localBounds.Height;

                    Color colorTop = host.GetBackgroundColorAt(yTop, formHeight);
                    Color colorBottom = host.GetBackgroundColorAt(yBottom, formHeight);

                    if (colorTop == colorBottom)
                    {
                        return new SolidBrush(colorTop);
                    }

                    var rect = new Rectangle(0, 0, Math.Max(1, localBounds.Width), Math.Max(1, localBounds.Height));
                    return new LinearGradientBrush(rect, colorTop, colorBottom, LinearGradientMode.Vertical);
                }
            }

            return new SolidBrush(Theme.DialogBackground);
        }

        /// <summary>Vertical position (0..1) of <paramref name="clientY"/> blended between WindowTop/WindowBottom.</summary>
        internal static Color SampleWindowGradient(int clientY, int clientHeight)
        {
            if (clientHeight <= 0)
            {
                return WindowTop;
            }

            float t = Math.Clamp((float)clientY / clientHeight, 0f, 1f);
            return Blend(WindowTop, WindowBottom, t);
        }

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            return RoundedRect(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), radius);
        }

        public static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            float d = radius * 2f;

            var path = new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);

            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);

            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);

            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);

            path.CloseFigure();

            return path;

        }

        /// <summary>
        /// Standard high-quality paint setup for every rounded-corner control below. Anti-aliasing
        /// alone still leaves a visible "staircase"/grainy edge on small-radius rounded rects unless
        /// paired with a high-quality pixel offset (so the rasterizer samples pixel centers instead of
        /// corners) and high-quality compositing (so translucent AA edge pixels blend smoothly instead
        /// of banding). Call this once at the top of OnPaint before building any GraphicsPath.
        /// </summary>
        internal static void PrepareHighQualityPaint(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
        }

        /// <summary>
        /// Border pen aligned fully *inside* the path (<see cref="PenAlignment.Inset"/>) instead of the
        /// GDI+ default (<see cref="PenAlignment.Center"/>), which straddles the path outline half
        /// inside/half outside. A straddling stroke on an anti-aliased rounded rect bleeds a half-pixel
        /// ring past the fill, which is what produces the grainy/jagged double-edge look on button
        /// borders. Inset keeps the stroke crisp and fully contained within the filled shape.
        /// </summary>
        internal static Pen CreateInsetPen(Color color, float width = 1f)
        {
            return new Pen(color, width) { Alignment = PenAlignment.Inset, LineJoin = LineJoin.Round };
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

    /// <summary>
    /// Scales an image to cover the control (fill width+height, crop overflow) — no letterboxing.
    /// </summary>
    internal sealed class CoverPictureBox : Panel
    {
        private Image? _image;

        public Image? Image
        {
            get => _image;
            set
            {
                _image = value;
                Invalidate();
            }
        }

        public CoverPictureBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Black;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            if (_image == null || Width <= 0 || Height <= 0)
            {
                return;
            }

            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float scale = Math.Max((float)Width / _image.Width, (float)Height / _image.Height);
            int drawW = (int)Math.Ceiling(_image.Width * scale);
            int drawH = (int)Math.Ceiling(_image.Height * scale);
            int x = (Width - drawW) / 2;
            int y = (Height - drawH) / 2;
            e.Graphics.DrawImage(_image, new Rectangle(x, y, drawW, drawH));
        }
    }

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

            Theme.PrepareHighQualityPaint(e.Graphics);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = Theme.RoundedRect(rect, CornerRadius);

            using var fill = new SolidBrush(Theme.Card);

            using var pen = Theme.CreateInsetPen(Theme.CardBorder);

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

            Theme.PrepareHighQualityPaint(e.Graphics);

            using var bg = Theme.GetSurfaceBrush(this, ClientRectangle);
            e.Graphics.FillRectangle(bg, ClientRectangle);

            var fillBounds = new RectangleF(0f, 0f, Width, Height);

            var strokeBounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            using var fillPath = Theme.RoundedRect(fillBounds, 8);

            using var strokePath = Theme.RoundedRect(strokeBounds, 8);

            using var fill = new SolidBrush(Theme.Input);

            using var pen = Theme.CreateInsetPen(Theme.InputBorder);

            e.Graphics.FillPath(fill, fillPath);

            e.Graphics.DrawPath(pen, strokePath);

        }

    }



    // Button with rounded corners, optional blue->violet gradient and hover glow.

    internal sealed class ThemedButton : Button

    {

        private bool _hover;

        private bool _down;

        private bool _highlightAsUpdate;



        public bool UseGradient { get; set; }

        /// <summary>Static (non-animated) highlight tint, e.g. to flag "update available".</summary>
        public bool HighlightAsUpdate
        {
            get => _highlightAsUpdate;
            set
            {
                if (_highlightAsUpdate == value)
                {
                    return;
                }

                _highlightAsUpdate = value;
                Invalidate();
            }
        }

        public int CornerRadius { get; set; } = 10;

        public Image? IconImage { get; set; }

        public int IconSize { get; set; } = 14;

        public string IconText { get; set; } = "";

        public Color IconTextColor { get; set; } = Color.Empty;

        public Color? IconTintColor { get; set; }

        public bool TightContentFit { get; set; }



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
            bool hasIcon = IconImage != null || !string.IsNullOrEmpty(IconText);
            int iconExtra = 0;
            if (hasIcon)
            {
                if (TightContentFit && !string.IsNullOrEmpty(IconText))
                {
                    iconExtra = TextRenderer.MeasureText(IconText, Font, proposedSize, TextFormatFlags.NoPadding).Width + iconGap;
                }
                else
                {
                    iconExtra = IconSize + iconGap + (TightContentFit ? 0 : 2);
                }
            }

            int contentPad = TightContentFit ? 2 : 4;
            int width = textSize.Width + Padding.Horizontal + iconExtra + contentPad;
            int height = Math.Max(Height > 0 ? Height : Theme.PrimaryButtonHeight, textSize.Height + Padding.Vertical + contentPad);
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

            Theme.PrepareHighQualityPaint(e.Graphics);



            // Repaint whatever actually sits behind our rounded corners (gradient slice, card color,
            // or flat dialog background) so the square corners never show as a mismatched patch.

            using var bg = Theme.GetSurfaceBrush(this, ClientRectangle);
            e.Graphics.FillRectangle(bg, ClientRectangle);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = Theme.RoundedRect(rect, CornerRadius);



            Color textColor = ForeColor;



            if (HighlightAsUpdate)

            {

                // Static highlight (no animation) to flag "update available".
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

                using var pen = Theme.CreateInsetPen(Theme.InputBorder);

                e.Graphics.FillPath(brush, path);

                e.Graphics.DrawPath(pen, path);

            }



            const int iconGap = 5;
            bool hasIconImage = IconImage != null;
            bool hasIconText = !string.IsNullOrEmpty(IconText);
            Size textSize = TextRenderer.MeasureText(e.Graphics, Text, Font, Size.Empty, TextFormatFlags.NoPadding);
            int iconWidth = 0;
            if (hasIconImage)
            {
                iconWidth = IconSize;
            }
            else if (hasIconText)
            {
                iconWidth = TightContentFit
                    ? TextRenderer.MeasureText(e.Graphics, IconText, Font, Size.Empty, TextFormatFlags.NoPadding).Width
                    : IconSize + 2;
            }

            int iconExtra = iconWidth > 0 ? iconWidth + iconGap : 0;
            int contentWidth = iconExtra + textSize.Width;
            int startX = rect.Left + Math.Max(Padding.Left, (rect.Width - contentWidth - Padding.Horizontal) / 2 + Padding.Left / 2);
            int centerY = rect.Top + rect.Height / 2;

            if (hasIconImage)
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
            else if (hasIconText)
            {
                var iconRect = new Rectangle(startX, rect.Top, iconWidth, rect.Height);
                Color iconColor = IconTextColor.IsEmpty ? textColor : IconTextColor;
                TextRenderer.DrawText(
                    e.Graphics,
                    IconText,
                    Font,
                    iconRect,
                    iconColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
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

            Theme.PrepareHighQualityPaint(e.Graphics);



            // BUG FIX: this control never cleared the square corners behind its rounded path, so
            // whatever the native double-buffer left underneath (typically black) showed through as
            // dark rectangular corner artifacts. Every other rounded control in this file (ThemedButton,
            // PillButton, OutlineAssistantButton, ...) does this same fill first — this one was missing it.
            using var bg = Theme.GetSurfaceBrush(this, ClientRectangle);
            e.Graphics.FillRectangle(bg, ClientRectangle);

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

            using (var pen = Theme.CreateInsetPen(borderColor))

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

            Theme.PrepareHighQualityPaint(e.Graphics);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var bg = Theme.GetSurfaceBrush(this, ClientRectangle);
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
            using (var pen = Theme.CreateInsetPen(Color.FromArgb(borderAlpha, AccentColor), borderWidth))
            {
                e.Graphics.DrawPath(pen, path);
            }

            if (Focused)
            {
                var focusRect = Rectangle.Inflate(rect, -2, -2);
                using var focusPath = Theme.RoundedRect(focusRect, Theme.OutlineAssistantRadius - 1);
                using var focusPen = Theme.CreateInsetPen(Color.FromArgb(120, AccentColor), 1f);
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

        public Color FillColor { get; set; } = PillFill;

        public string IconText { get; set; } = "";

        public Color IconTextColor { get; set; } = Color.Empty;



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

            Font = Theme.DonationPillFont;

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

            Theme.PrepareHighQualityPaint(e.Graphics);

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;



            if (Parent != null)

            {

                using var bg = Theme.GetSurfaceBrush(this, ClientRectangle);
                e.Graphics.FillRectangle(bg, ClientRectangle);

            }



            var pillRect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = Theme.RoundedRect(pillRect, CornerRadius);



            using (var brush = new SolidBrush(FillColor))

            {

                e.Graphics.FillPath(brush, path);

            }



            int borderAlpha = _down ? 120 : _hover ? 255 : _isSelected ? 210 : 140;

            float borderWidth = _hover || _isSelected ? 1.5f : 1f;

            using (var pen = Theme.CreateInsetPen(Color.FromArgb(borderAlpha, AccentColor), borderWidth))

            {

                e.Graphics.DrawPath(pen, path);

            }



            const int iconSize = 18;

            const int iconGap = 6;

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

                Color iconColor = IconTextColor.IsEmpty ? ForeColor : IconTextColor;
                TextRenderer.DrawText(

                    e.Graphics, IconText, Font, iconRect, iconColor,

                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                contentLeft = iconRect.Right + iconGap;

            }



            var textRect = new Rectangle(contentLeft, contentTop, Math.Max(0, contentRight - contentLeft), contentHeight);

            TextRenderer.DrawText(

                e.Graphics, Text, Font, textRect, ForeColor,

                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

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

            Theme.PrepareHighQualityPaint(e.Graphics);



            using var bg = Theme.GetSurfaceBrush(this, ClientRectangle);
            e.Graphics.FillRectangle(bg, ClientRectangle);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = Theme.RoundedRect(rect, 6);



            Color fill = _hover ? Theme.ButtonNeutralHover : Theme.ButtonNeutral;

            using (var brush = new SolidBrush(fill))

                e.Graphics.FillPath(brush, path);

            using (var pen = Theme.CreateInsetPen(_hover ? Theme.SectionGreen : Theme.InputBorder))

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

            Theme.PrepareHighQualityPaint(e.Graphics);



            using var bg = Theme.GetSurfaceBrush(this, ClientRectangle);
            e.Graphics.FillRectangle(bg, ClientRectangle);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = Theme.RoundedRect(rect, 6);



            Color fill = _hover ? Theme.ButtonNeutralHover : Theme.ButtonNeutral;

            using (var brush = new SolidBrush(fill))

                e.Graphics.FillPath(brush, path);

            using (var pen = Theme.CreateInsetPen(_hover ? Theme.Error : Theme.InputBorder))

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

        private const int WM_ERASEBKGND = 0x0014;

        private const int ArrowWidth = 28;

        private const int CornerRadius = 8;



        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]

        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);



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

            SetStyle(ControlStyles.ResizeRedraw, true);

        }



        protected override void OnHandleCreated(EventArgs e)

        {

            base.OnHandleCreated(e);

            if (Handle != IntPtr.Zero)

            {

                SetWindowTheme(Handle, " ", " ");

            }

        }



        protected override void OnSelectedIndexChanged(EventArgs e)

        {

            base.OnSelectedIndexChanged(e);

            Invalidate();

        }



        protected override void WndProc(ref Message m)

        {

            if (m.Msg == WM_ERASEBKGND)

            {

                m.Result = (IntPtr)1;

                return;

            }



            base.WndProc(ref m);

            if (m.Msg == WM_PAINT && IsHandleCreated && Theme.HasPaintableSize(Width, Height))

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



            Theme.PrepareHighQualityPaint(g);

            var clientRect = ClientRectangle;

            using var bg = Theme.GetSurfaceBrush(this, clientRect);

            g.FillRectangle(bg, clientRect);

            var fillBounds = new RectangleF(0f, 0f, Width, Height);

            var strokeBounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            using var fillPath = Theme.RoundedRect(fillBounds, CornerRadius);

            using var strokePath = Theme.RoundedRect(strokeBounds, CornerRadius);

            using (var fill = new SolidBrush(Theme.Input))

            {

                g.FillPath(fill, fillPath);

            }



            using (var pen = Theme.CreateInsetPen(Theme.InputBorder))

            {

                g.DrawPath(pen, strokePath);

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


