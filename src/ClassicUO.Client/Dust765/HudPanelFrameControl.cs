// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765
{
    internal sealed class HudPanelFrameControl : Control
    {
        private static readonly Texture2D _texture = SolidColorTextureCache.GetTexture(Color.White);

        public ushort OuterHue { get; set; } = 34;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            Vector3 outer = ShaderHueTranslator.GetHueVector(OuterHue, false, Alpha * 0.55f);
            Vector3 inner = ShaderHueTranslator.GetHueVector(0, false, Alpha * 0.35f);

            batcher.DrawRectangle(_texture, x, y, Width, Height, outer);
            batcher.DrawRectangle(_texture, x + 1, y + 1, Width - 2, Height - 2, inner);

            return true;
        }
    }
}
