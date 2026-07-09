#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Land
    {
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            //Engine.DebugInfo.LandsRendered++;

            ushort hue = Hue;

            if (ProfileManager.CurrentProfile.HighlightGameObjects && SelectedObject.Object == this)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
            }
            else if (
                ProfileManager.CurrentProfile.NoColorObjectsOutOfRange
                && Distance > World.ClientViewRange
            )
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }
            else if (TryGetRangeHighlightHue(out ushort rangeHue))
            {
                // Dust765 VISUAL HELPERS: highlight the ring of ground tiles at a fixed
                // range around the player (optionally only while casting a spell).
                hue = rangeHue;
            }

            Vector3 hueVec;
            if (hue != 0)
            {
                hueVec.X = hue - 1;
                hueVec.Y = IsStretched
                    ? ShaderHueTranslator.SHADER_LAND_HUED
                    : ShaderHueTranslator.SHADER_HUED;
            }
            else
            {
                hueVec.X = 0;
                hueVec.Y = IsStretched
                    ? ShaderHueTranslator.SHADER_LAND
                    : ShaderHueTranslator.SHADER_NONE;
            }
            hueVec.Z = 1f;

            if (IsStretched)
            {
                posY += Z << 2;

                ref readonly var texmapInfo = ref Client.Game.UO.Texmaps.GetTexmap(
                    TileDataLoader.Instance.LandData[Graphic].TexID
                );

                if (texmapInfo.Texture != null)
                {
                    batcher.DrawStretchedLand(
                        texmapInfo.Texture,
                        new Vector2(posX, posY),
                        texmapInfo.UV,
                        ref YOffsets,
                        ref NormalTop,
                        ref NormalRight,
                        ref NormalLeft,
                        ref NormalBottom,
                        hueVec,
                        depth + 0.5f
                    );
                }
                else
                {
                    DrawStatic(
                        batcher,
                        Graphic,
                        posX,
                        posY,
                        hueVec,
                        depth,
                        ProfileManager.CurrentProfile.AnimatedWaterEffect && TileData.IsWet
                    );
                }
            }
            else
            {
                ref readonly var artInfo = ref Client.Game.UO.Arts.GetLand(Graphic);

                if (artInfo.Texture != null)
                {
                    var pos = new Vector2(posX, posY);
                    var scale = Vector2.One;

                    if (ProfileManager.CurrentProfile.AnimatedWaterEffect && TileData.IsWet)
                    {
                        batcher.Draw(
                            artInfo.Texture,
                            pos,
                            artInfo.UV,
                            hueVec,
                            0f,
                            Vector2.Zero,
                            scale,
                            SpriteEffects.None,
                            depth + 0.5f
                        );

                        var sin = (float)Math.Sin(Time.Ticks / 1000f);
                        var cos = (float)Math.Cos(Time.Ticks / 1000f);
                        scale = new Vector2(1.1f + sin * 0.1f, 1.1f + cos * 0.5f * 0.1f);
                    }

                    batcher.Draw(
                        artInfo.Texture,
                        pos,
                        artInfo.UV,
                        hueVec,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        depth + 0.5f
                    );
                }
            }

            return true;
        }

        // Dust765 VISUAL HELPERS - "Highlight tiles on range". Returns the configured
        // hue when this land tile sits exactly on the highlighted range ring around the
        // player. Two independent toggles: always-on, and the spell/target ring which is
        // shown during a cast wind-up AND whenever a target cursor is active on the mouse
        // (spells like Remove Curse, item/potion targets like an Explosion potion, etc.).
        private bool TryGetRangeHighlightHue(out ushort hue)
        {
            hue = 0;

            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || World.Player == null)
            {
                return false;
            }

            bool spellRingActive =
                profile.LTHighlightRangeOnCast
                && (GameActions.IsCasting() || World.TargetManager.IsTargeting);

            if (!profile.LTHighlightRangeOnActivated && !spellRingActive)
            {
                return false;
            }

            int distance = Math.Max(
                Math.Abs(X - World.Player.X),
                Math.Abs(Y - World.Player.Y)
            );

            if (spellRingActive && IsOnRangeRing(distance, profile.LTHighlightRangeOnCastRange, profile.LTHighlightRangeLineThickness))
            {
                hue = profile.LTHighlightRangeOnCastHue;

                return true;
            }

            if (profile.LTHighlightRangeOnActivated &&
                IsOnRangeRing(distance, profile.LTHighlightRangeOnActivatedRange, profile.LTHighlightRangeLineThickness))
            {
                hue = profile.LTHighlightRangeOnActivatedHue;

                return true;
            }

            return false;
        }

        private static bool IsOnRangeRing(int distance, int targetRange, float thickness)
        {
            float band = Math.Max(0.2f, Math.Min(1f, thickness));

            return Math.Abs(distance - targetRange) < band;
        }

        public override bool CheckMouseSelection()
        {
            if (IsStretched)
            {
                return SelectedObject.IsPointInStretchedLand(
                    ref YOffsets,
                    RealScreenPosition.X,
                    RealScreenPosition.Y + (Z << 2)
                );
            }

            return SelectedObject.IsPointInLand(RealScreenPosition.X, RealScreenPosition.Y);
        }
    }
}
