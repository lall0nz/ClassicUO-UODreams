using System.Drawing;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    internal sealed class ThemedContextMenu : ContextMenuStrip
    {
        public ThemedContextMenu()
        {
            Renderer = new ThemedMenuRenderer();
            BackColor = Theme.Card;
            ForeColor = Theme.Text;
            ShowImageMargin = false;
            ShowCheckMargin = false;
            Padding = new Padding(4, 4, 4, 4);
            Font = new Font("Segoe UI", 9.5f);
        }

        public void AddAction(string text, System.Action action)
        {
            var item = new ToolStripMenuItem(text)
            {
                BackColor = Theme.Card,
                ForeColor = Theme.Text,
                Font = Font
            };

            item.Click += (_, _) =>
            {
                // Defer until after the menu finishes its close/hide sequence.
                BeginInvoke(action);
            };
            Items.Add(item);
        }

        public void ShowBelow(Control anchor)
        {
            Point screen = anchor.PointToScreen(new Point(0, anchor.Height + 2));
            Show(screen);
        }

        private sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
        {
            public ThemedMenuRenderer() : base(new ThemedMenuColors()) { }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                var rect = new Rectangle(Point.Empty, e.Item.Size);
                Color fill = e.Item.Selected ? Theme.ButtonNeutralHover : Theme.Card;
                using var brush = new SolidBrush(fill);
                e.Graphics.FillRectangle(brush, rect);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using var pen = new Pen(Theme.CardBorder);
                var rect = new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                int y = e.Item.Height / 2;
                using var pen = new Pen(Theme.InputBorder);
                e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
            }
        }

        private sealed class ThemedMenuColors : ProfessionalColorTable
        {
            public override Color MenuBorder => Theme.CardBorder;
            public override Color ToolStripDropDownBackground => Theme.Card;
            public override Color ImageMarginGradientBegin => Theme.Card;
            public override Color ImageMarginGradientMiddle => Theme.Card;
            public override Color ImageMarginGradientEnd => Theme.Card;
        }
    }
}
