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
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.Managers
{
    internal sealed class HealthLinesManager
    {
        private const int BAR_WIDTH = 50;
        private const int BAR_HEIGHT = 8;
        private const int BAR_WIDTH_HALF = BAR_WIDTH >> 1;
        private const int BAR_HEIGHT_HALF = BAR_HEIGHT >> 1;
        private const int OLD_BAR_HEIGHT = 4;

        const ushort BACKGROUND_GRAPHIC = 0x1068;
        const ushort HP_GRAPHIC = 0x1069;

        private readonly World _world;

        public HealthLinesManager(World world) { _world = world; }

        // Show overhead bars for all real mobiles (players, enemies, allies, pets,
        // creatures). Only skip purely decorative / invulnerable NPCs such as town
        // vendors, guards and statues. Real players/enemies are never Invulnerable,
        // so they always keep their bars.
        private bool ShouldShowOverheadHealthBar(Mobile mobile)
        {
            if (mobile == null || mobile.IsDestroyed)
            {
                return false;
            }

            if (mobile == _world.Player)
            {
                return true;
            }

            if (mobile.NotorietyFlag == NotorietyFlag.Invulnerable)
            {
                return false;
            }

            return true;
        }

        public bool IsEnabled =>
            ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ShowMobilesHP;

        public void Draw(UltimaBatcher2D batcher)
        {
            var camera = Client.Game.Scene.Camera;

            if (SerialHelper.IsMobile(_world.TargetManager.LastTargetInfo.Serial))
            {
                DrawHealthLineWithMath(
                    batcher,
                    _world.TargetManager.LastTargetInfo.Serial,
                    camera.Bounds.Width,
                    camera.Bounds.Height
                );
            }

            if (SerialHelper.IsMobile(_world.TargetManager.SelectedTarget))
            {
                DrawHealthLineWithMath(
                    batcher,
                    _world.TargetManager.SelectedTarget,
                    camera.Bounds.Width,
                    camera.Bounds.Height
                );
            }

            if (SerialHelper.IsMobile(_world.TargetManager.LastAttack))
            {
                DrawHealthLineWithMath(
                    batcher,
                    _world.TargetManager.LastAttack,
                    camera.Bounds.Width,
                    camera.Bounds.Height
                );
            }

            if (!IsEnabled)
            {
                return;
            }

            int mode = ProfileManager.CurrentProfile.MobileHPType;

            if (mode < 0)
            {
                return;
            }

            int showWhen = ProfileManager.CurrentProfile.MobileHPShowWhen;

            foreach (Mobile mobile in _world.Mobiles.Values)
            {
                if (!ShouldShowOverheadHealthBar(mobile))
                {
                    continue;
                }

                int current = mobile.Hits;
                int max = mobile.HitsMax;

                if (max == 0)
                {
                    continue;
                }

                if (showWhen == 1 && current == max)
                {
                    continue;
                }

                Point p = mobile.RealScreenPosition;
                p.X += (int)mobile.Offset.X + 22 + 5;
                p.Y += (int)(mobile.Offset.Y - mobile.Offset.Z) + 22 + 5;

                if (mode != 1 && !mobile.IsDead)
                {
                    if (showWhen == 2 && current != max || showWhen <= 1)
                    {
                        if (mobile.HitsPercentage != 0)
                        {
                            Client.Game.UO.Animations.GetAnimationDimensions(
                                mobile.AnimIndex,
                                mobile.GetGraphicForAnimation(),
                                /*(byte) m.GetDirectionForAnimation()*/
                                0,
                                /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                                0,
                                mobile.IsMounted,
                                /*(byte) m.AnimIndex*/
                                0,
                                out int centerX,
                                out int centerY,
                                out int width,
                                out int height
                            );

                            Point p1 = p;
                            p1.Y -= height + centerY + 8 + 22;

                            if (mobile.IsGargoyle && mobile.IsFlying)
                            {
                                p1.Y -= 22;
                            }
                            else if (!mobile.IsMounted)
                            {
                                p1.Y += 22;
                            }

                            p1 = Client.Game.Scene.Camera.WorldToScreen(p1);
                            p1.X -= (mobile.HitsTexture.Width >> 1) + 5;
                            p1.Y -= mobile.HitsTexture.Height;

                            if (mobile.ObjectHandlesStatus == ObjectHandlesStatus.DISPLAYING)
                            {
                                p1.Y -= Constants.OBJECT_HANDLES_GUMP_HEIGHT + 5;
                            }

                            if (
                                !(
                                    p1.X < 0
                                    || p1.X > camera.Bounds.Width - mobile.HitsTexture.Width
                                    || p1.Y < 0
                                    || p1.Y > camera.Bounds.Height
                                )
                            )
                            {
                                mobile.HitsTexture.Draw(batcher, p1.X, p1.Y);
                            }
                        }
                    }
                }

                if (
                    mobile.Serial == _world.TargetManager.LastTargetInfo.Serial
                    || mobile.Serial == _world.TargetManager.SelectedTarget
                    || mobile.Serial == _world.TargetManager.LastAttack
                )
                {
                    continue;
                }

                p.X -= 5;
                p = Client.Game.Scene.Camera.WorldToScreen(p);
                p.X -= BAR_WIDTH_HALF;
                p.Y -= BAR_HEIGHT_HALF;

                if (p.X < 0 || p.X > camera.Bounds.Width - BAR_WIDTH)
                {
                    continue;
                }

                if (p.Y < 0 || p.Y > camera.Bounds.Height - BAR_HEIGHT)
                {
                    continue;
                }

                if (mode >= 1)
                {
                    if (ProfileManager.CurrentProfile.UseOldHealthBars)
                    {
                        DrawOldHealthLine(batcher, mobile, p.X, p.Y, mobile.Serial != _world.Player.Serial);
                    }
                    else
                    {
                        DrawHealthLine(batcher, mobile, p.X, p.Y, mobile.Serial != _world.Player.Serial);
                    }
                }
            }
        }

        private void DrawHealthLineWithMath(
            UltimaBatcher2D batcher,
            uint serial,
            int screenW,
            int screenH
        )
        {
            Entity entity = _world.Get(serial);

            if (entity is Mobile mobile && !ShouldShowOverheadHealthBar(mobile))
            {
                return;
            }

            if (entity == null)
            {
                return;
            }

            Point p = entity.RealScreenPosition;
            p.X += (int)entity.Offset.X + 22;
            p.Y += (int)(entity.Offset.Y - entity.Offset.Z) + 22 + 5;

            p = Client.Game.Scene.Camera.WorldToScreen(p);
            p.X -= BAR_WIDTH_HALF;
            p.Y -= BAR_HEIGHT_HALF;

            if (p.X < 0 || p.X > screenW - BAR_WIDTH)
            {
                return;
            }

            if (p.Y < 0 || p.Y > screenH - BAR_HEIGHT)
            {
                return;
            }

            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.UseOldHealthBars)
            {
                DrawOldHealthLine(batcher, entity, p.X, p.Y, false);
            }
            else
            {
                DrawHealthLine(batcher, entity, p.X, p.Y, false);
            }
        }

        // Classic-style overhead bars: solid HP bar for everyone, plus Mana and
        // Stamina bars for the player and party members (old UO look).
        private void DrawOldHealthLine(
            UltimaBatcher2D batcher,
            Entity entity,
            int x,
            int y,
            bool passive
        )
        {
            if (entity == null)
            {
                return;
            }

            Mobile mobile = entity as Mobile;

            float alpha = passive ? 0.5f : 1.0f;
            Vector3 hueVec = ShaderHueTranslator.GetHueVector(0, false, alpha);

            // Only the local player keeps the celeste (light blue) bar. Every other
            // mobile/player (enemies, innocents, allies, party) renders a WHITE base
            // bar so it stands out against grass instead of the purple/violet tint.
            bool isLocalPlayer = mobile != null && mobile == _world.Player;

            Color hpColor;

            if (mobile != null && mobile.IsParalyzed)
            {
                hpColor = Color.AliceBlue;
            }
            else if (mobile != null && mobile.IsYellowHits)
            {
                hpColor = Color.Orange;
            }
            else if (mobile != null && mobile.IsPoisoned)
            {
                hpColor = Color.LimeGreen;
            }
            else if (isLocalPlayer)
            {
                hpColor = Color.CornflowerBlue;
            }
            else
            {
                hpColor = Color.White;
            }

            int hpWidth = CalcBarWidth(entity.Hits, entity.HitsMax, BAR_WIDTH);
            DrawOldBar(batcher, x, y, BAR_WIDTH, OLD_BAR_HEIGHT, hpWidth, hpColor, hueVec);

            bool showAll = mobile != null && (mobile == _world.Player || _world.Party.Contains(mobile.Serial));

            if (showAll)
            {
                int manaWidth = CalcBarWidth(mobile.Mana, mobile.ManaMax, BAR_WIDTH);
                int stamWidth = CalcBarWidth(mobile.Stamina, mobile.StaminaMax, BAR_WIDTH);

                DrawOldBar(batcher, x, y + OLD_BAR_HEIGHT + 1, BAR_WIDTH, OLD_BAR_HEIGHT, manaWidth, Color.CornflowerBlue, hueVec);
                DrawOldBar(batcher, x, y + (OLD_BAR_HEIGHT + 1) * 2, BAR_WIDTH, OLD_BAR_HEIGHT, stamWidth, Color.CornflowerBlue, hueVec);
            }
        }

        private static int CalcBarWidth(int current, int max, int barWidth)
        {
            if (max <= 0)
            {
                return 0;
            }

            int percent = current * 100 / max;

            if (percent > 100)
            {
                percent = 100;
            }
            else if (percent < 0)
            {
                percent = 0;
            }

            return barWidth * percent / 100;
        }

        private static void DrawOldBar(
            UltimaBatcher2D batcher,
            int x,
            int y,
            int width,
            int height,
            int filled,
            Color fill,
            Vector3 hueVec
        )
        {
            batcher.Draw(
                SolidColorTextureCache.GetTexture(Color.Black),
                new Rectangle(x - 1, y - 1, width + 2, height + 2),
                hueVec
            );

            batcher.Draw(
                SolidColorTextureCache.GetTexture(Color.Red),
                new Rectangle(x, y, width, height),
                hueVec
            );

            if (filled > 0)
            {
                batcher.Draw(
                    SolidColorTextureCache.GetTexture(fill),
                    new Rectangle(x, y, filled, height),
                    hueVec
                );
            }
        }

        private void DrawHealthLine(
            UltimaBatcher2D batcher,
            Entity entity,
            int x,
            int y,
            bool passive
        )
        {
            if (entity == null)
            {
                return;
            }

            int per = BAR_WIDTH * entity.HitsPercentage / 100;

            Mobile mobile = entity as Mobile;

            float alpha = passive ? 0.5f : 1.0f;
            ushort hue =
                mobile != null
                    ? Notoriety.GetHue(mobile.NotorietyFlag)
                    : Notoriety.GetHue(NotorietyFlag.Gray);

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, false, alpha);

            if (mobile == null)
            {
                y += 22;
            }

            const int MULTIPLER = 1;

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(BACKGROUND_GRAPHIC);

            batcher.Draw(
                gumpInfo.Texture,
                new Rectangle(x, y, gumpInfo.UV.Width * MULTIPLER, gumpInfo.UV.Height * MULTIPLER),
                gumpInfo.UV,
                hueVec
            );

            hueVec.X = 0x21;

            if (entity.Hits != entity.HitsMax || entity.HitsMax == 0)
            {
                int offset = 2;

                if (per >> 2 == 0)
                {
                    offset = per;
                }

                gumpInfo = ref Client.Game.UO.Gumps.GetGump(HP_GRAPHIC);

                batcher.DrawTiled(
                    gumpInfo.Texture,
                    new Rectangle(
                        x + per * MULTIPLER - offset,
                        y,
                        (BAR_WIDTH - per) * MULTIPLER - offset / 2,
                        gumpInfo.UV.Height * MULTIPLER
                    ),
                    gumpInfo.UV,
                    hueVec
                );
            }

            hue = 90;

            if (per > 0)
            {
                if (mobile != null)
                {
                    if (mobile.IsPoisoned)
                    {
                        hue = 63;
                    }
                    else if (mobile.IsYellowHits)
                    {
                        hue = 53;
                    }
                }

                hueVec.X = hue;

                gumpInfo = ref Client.Game.UO.Gumps.GetGump(HP_GRAPHIC);
                batcher.DrawTiled(
                    gumpInfo.Texture,
                    new Rectangle(x, y, per * MULTIPLER, gumpInfo.UV.Height * MULTIPLER),
                    gumpInfo.UV,
                    hueVec
                );
            }
        }
    }
}
