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

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaMathHelper = Microsoft.Xna.Framework.MathHelper;

namespace ClassicUO.Game.Managers
{
    internal sealed class BandageRingTimer
    {
        private enum CooldownKind : byte
        {
            Bandage = 0,
            Apple = 1,
            Conflagration = 2,
            Heal = 3
        }

        private const ushort BANDAGE_GRAPHIC = 0x0E21;
        private const ushort APPLE_GRAPHIC = 0x2FD8;
        private const ushort HEAL_POTION_GRAPHIC = 0x0F0C;
        private const ushort CONFLA_POTION_GRAPHIC = 0x0F06;

        private const ushort HUE_BANDAGE = 0x0044; // green (countdown ring)
        private const ushort HUE_APPLE = 0x0013;   // purple (countdown ring)
        private const ushort HUE_CONFLA = 0x0021;  // red (countdown ring)
        private const ushort HUE_HEAL = 0x0035;    // yellow (countdown ring)

        // Real item hues (UODreams) so icons match in-game art.
        private const ushort ICON_HUE_APPLE = 0x0488;
        private const ushort ICON_HUE_CONFLA = 0x0489;

        private const float RING_RADIUS = 23.5f;
        private const float RING_THICKNESS = 3.5f;
        private const float BG_RADIUS = 22f;
        private const float SLOT_SPACING = 58f;
        private const int ARC_SEGMENTS = 48;
        private const int OLD_BAR_HEIGHT = 4;
        private const int BAR_HEIGHT_HALF = 4;
        /// <summary>Extra gap below status bars for default auto anchor (~1cm / 76px).</summary>
        private const float DEFAULT_ANCHOR_EXTRA_Y = 76f;
        private const long PENDING_TIMEOUT_MS = 8000;
        private const float HIT_RADIUS = 28f;

        private static Texture2D _circleTexture;

        private readonly World _world;
        private readonly Slot[] _slots = new Slot[4];
        private bool _pendingBandageUse;
        private long _pendingUntil;
        private bool _hadHealingBuff;
        private bool _dragging;
        private Point _dragGrabOffset;

        public BandageRingTimer(World world)
        {
            _world = world;

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new Slot();
            }
        }

        public bool IsActive
        {
            get
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].Active)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void ObserveOutgoingPacket(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty || ProfileManager.CurrentProfile is not { ShowBandageRingTimer: true })
            {
                return;
            }

            byte id = data[0];

            if (id == 0x06 && data.Length >= 5)
            {
                HandleItemUse(ReadUInt32BE(data, 1), requiresTargetForBandage: true);

                return;
            }

            if (id == 0xBF && data.Length >= 11)
            {
                ushort sub = ReadUInt16BE(data, 3);

                if (sub == 0x002C)
                {
                    HandleItemUse(ReadUInt32BE(data, 5), requiresTargetForBandage: false);
                }

                return;
            }

            if (id == 0x6C && _pendingBandageUse && Time.Ticks <= _pendingUntil)
            {
                _pendingBandageUse = false;
                NotifyBandageStarted();
            }
        }

        public void NotifyHealingBuffChanged(bool added)
        {
            if (ProfileManager.CurrentProfile is not { ShowBandageRingTimer: true })
            {
                return;
            }

            if (added)
            {
                SyncBandageFromBuffOrEstimate(forceRestart: !_slots[(int)CooldownKind.Bandage].Active);
            }
            else if (_slots[(int)CooldownKind.Bandage].Active && !HasHealingBuff())
            {
                StopSlot(CooldownKind.Bandage);
            }
        }

        public void NotifyBandageUsed()
        {
            ArmPendingBandageUse();
        }

        public void NotifyBandageStarted()
        {
            if (ProfileManager.CurrentProfile is not { ShowBandageRingTimer: true } || _world.Player == null)
            {
                return;
            }

            if (_slots[(int)CooldownKind.Bandage].Active)
            {
                return;
            }

            _pendingBandageUse = false;
            SyncBandageFromBuffOrEstimate(forceRestart: true);
        }

        public void Update()
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || !profile.ShowBandageRingTimer || _world.Player == null || !_world.InGame)
            {
                StopAll();
                _pendingBandageUse = false;
                return;
            }

            if (_pendingBandageUse && Time.Ticks > _pendingUntil)
            {
                _pendingBandageUse = false;
            }

            UpdateBandageBuffSync();

            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];

                if (slot.Active && Time.Ticks >= slot.EndTicks)
                {
                    StopSlot((CooldownKind)i);
                }
            }
        }

        public void Draw(UltimaBatcher2D batcher, Camera camera)
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || !profile.ShowBandageRingTimer || _world.Player == null)
            {
                return;
            }

            if (!TryGetLayout(camera, out Vector2 anchor, out int activeCount, out float startX))
            {
                return;
            }

            EnsureCircleTexture(batcher.GraphicsDevice);

            for (int i = 0; i < activeCount; i++)
            {
                int idx = GetActiveSlotIndex(i);
                Slot slot = _slots[idx];
                float remainingMs = slot.EndTicks - Time.Ticks;

                if (remainingMs <= 0f)
                {
                    continue;
                }

                float progress = Math.Clamp(remainingMs / Math.Max(1f, slot.DurationMs), 0f, 1f);
                int secondsLeft = Math.Max(1, (int)Math.Ceiling(remainingMs / 1000f));
                Vector2 center = new Vector2(startX + i * SLOT_SPACING, anchor.Y);

                DrawBackground(batcher, center);
                DrawIcon(batcher, center, slot.IconGraphic, slot.IconHue, slot.IconPartialHue);
                DrawRing(batcher, center, progress, slot.RingHue);
                DrawSeconds(batcher, center, secondsLeft);
            }
        }

        /// <summary>
        /// Invisible hit area for drag repositioning (no visible frame). Returns true when consumed.
        /// </summary>
        public bool OnMouseDown(MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return false;
            }

            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || !profile.ShowBandageRingTimer || profile.BandageRingTimerLocked || _world.Player == null)
            {
                return false;
            }

            if (!TryGetHitBounds(Client.Game.Scene.Camera, out Rectangle bounds))
            {
                return false;
            }

            if (!bounds.Contains(Mouse.Position))
            {
                return false;
            }

            Vector2 anchor = GetTimerAnchor(Client.Game.Scene.Camera);
            EnsureManualAnchor(profile, anchor);

            _dragging = true;
            _dragGrabOffset = new Point(
                Mouse.Position.X - profile.BandageRingTimerX,
                Mouse.Position.Y - profile.BandageRingTimerY
            );

            return true;
        }

        public bool OnMouseDragging()
        {
            if (!_dragging)
            {
                return false;
            }

            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || profile.BandageRingTimerLocked)
            {
                _dragging = false;

                return false;
            }

            profile.BandageRingTimerX = Mouse.Position.X - _dragGrabOffset.X;
            profile.BandageRingTimerY = Mouse.Position.Y - _dragGrabOffset.Y;

            return true;
        }

        public bool OnMouseUp(MouseButtonType button)
        {
            if (button != MouseButtonType.Left || !_dragging)
            {
                return false;
            }

            _dragging = false;

            return true;
        }

        private bool TryGetLayout(Camera camera, out Vector2 anchor, out int activeCount, out float startX)
        {
            anchor = default;
            activeCount = 0;
            startX = 0f;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Active)
                {
                    activeCount++;
                }
            }

            if (activeCount == 0)
            {
                return false;
            }

            anchor = GetTimerAnchor(camera);
            float totalWidth = (activeCount - 1) * SLOT_SPACING;
            startX = anchor.X - totalWidth * 0.5f;

            return true;
        }

        private int GetActiveSlotIndex(int visibleIndex)
        {
            int seen = 0;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].Active)
                {
                    continue;
                }

                if (seen == visibleIndex)
                {
                    return i;
                }

                seen++;
            }

            return 0;
        }

        private bool TryGetHitBounds(Camera camera, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;

            if (!TryGetLayout(camera, out Vector2 anchor, out int activeCount, out float startX))
            {
                return false;
            }

            float minX = startX - HIT_RADIUS;
            float maxX = startX + (activeCount - 1) * SLOT_SPACING + HIT_RADIUS;
            float minY = anchor.Y - HIT_RADIUS;
            float maxY = anchor.Y + HIT_RADIUS + 18f;

            bounds = new Rectangle(
                (int)MathF.Floor(minX),
                (int)MathF.Floor(minY),
                (int)MathF.Ceiling(maxX - minX),
                (int)MathF.Ceiling(maxY - minY)
            );

            return true;
        }

        private static void EnsureManualAnchor(Profile profile, Vector2 anchor)
        {
            if (profile.BandageRingTimerX < 0 || profile.BandageRingTimerY < 0)
            {
                profile.BandageRingTimerX = (int)MathF.Round(anchor.X);
                profile.BandageRingTimerY = (int)MathF.Round(anchor.Y);
            }
        }

        private void HandleItemUse(uint serial, bool requiresTargetForBandage)
        {
            Item item = _world.Items.Get(serial);

            if (item == null || item.IsDestroyed)
            {
                return;
            }

            CooldownKind? kind = ClassifyItem(item);

            if (kind == null)
            {
                return;
            }

            // Already cooling down: keep the existing countdown, do not restart.
            if (_slots[(int)kind.Value].Active)
            {
                return;
            }

            if (kind == CooldownKind.Bandage)
            {
                if (requiresTargetForBandage)
                {
                    ArmPendingBandageUse();
                }
                else
                {
                    NotifyBandageStarted();
                }

                return;
            }

            ushort icon = kind == CooldownKind.Apple
                ? APPLE_GRAPHIC
                : (item.Graphic != 0 ? item.Graphic : GetFallbackGraphic(kind.Value));

            StartFixed(kind.Value, GetFixedDuration(kind.Value), icon, GetIconHue(kind.Value, item), item.ItemData.IsPartialHue);
        }

        private void ArmPendingBandageUse()
        {
            if (_slots[(int)CooldownKind.Bandage].Active)
            {
                return;
            }

            _pendingBandageUse = true;
            _pendingUntil = Time.Ticks + PENDING_TIMEOUT_MS;

            if (HasHealingBuff())
            {
                NotifyBandageStarted();
            }
        }

        private void UpdateBandageBuffSync()
        {
            bool hasBuff = HasHealingBuff();

            if (hasBuff)
            {
                if (!_hadHealingBuff || !_slots[(int)CooldownKind.Bandage].Active)
                {
                    SyncBandageFromBuffOrEstimate(forceRestart: true);
                }
                else
                {
                    long endTicks = GetBuffEndTicks();

                    if (IsValidBuffEnd(endTicks))
                    {
                        Slot slot = _slots[(int)CooldownKind.Bandage];
                        slot.EndTicks = endTicks;
                        slot.DurationMs = Math.Max(1f, slot.EndTicks - slot.StartTicks);
                    }
                }

                _hadHealingBuff = true;
            }
            else
            {
                if (_hadHealingBuff && _slots[(int)CooldownKind.Bandage].Active)
                {
                    StopSlot(CooldownKind.Bandage);
                }

                _hadHealingBuff = false;
            }
        }

        private void SyncBandageFromBuffOrEstimate(bool forceRestart)
        {
            long endTicks = GetBuffEndTicks();
            Slot slot = _slots[(int)CooldownKind.Bandage];

            if (IsValidBuffEnd(endTicks))
            {
                float durationSec = Math.Max(0.5f, (endTicks - Time.Ticks) / 1000f);

                if (forceRestart || !slot.Active)
                {
                    StartSlot(CooldownKind.Bandage, durationSec, endTicks, BANDAGE_GRAPHIC, 0, false, HUE_BANDAGE);
                }
                else
                {
                    slot.EndTicks = endTicks;
                    slot.DurationMs = Math.Max(1f, slot.EndTicks - slot.StartTicks);
                }
            }
            else if (forceRestart || !slot.Active)
            {
                StartSlot(CooldownKind.Bandage, EstimateBandageDurationSeconds(), null, BANDAGE_GRAPHIC, 0, false, HUE_BANDAGE);
            }
        }

        private void StartFixed(
            CooldownKind kind,
            float durationSeconds,
            ushort iconGraphic,
            ushort iconHue,
            bool iconPartialHue
        )
        {
            if (_slots[(int)kind].Active)
            {
                return;
            }

            StartSlot(kind, durationSeconds, null, iconGraphic, iconHue, iconPartialHue, GetHue(kind));
        }

        private void StartSlot(
            CooldownKind kind,
            float durationSeconds,
            long? endTicks,
            ushort iconGraphic,
            ushort iconHue,
            bool iconPartialHue,
            ushort ringHue
        )
        {
            Slot slot = _slots[(int)kind];

            if (slot.Active)
            {
                return;
            }

            slot.DurationMs = Math.Max(500f, durationSeconds * 1000f);
            slot.StartTicks = Time.Ticks;
            slot.EndTicks = endTicks ?? (slot.StartTicks + (long)slot.DurationMs);
            slot.IconGraphic = iconGraphic;
            slot.IconHue = iconHue;
            slot.IconPartialHue = iconPartialHue;
            slot.RingHue = ringHue;
            slot.Active = true;
        }

        private void StopSlot(CooldownKind kind)
        {
            Slot slot = _slots[(int)kind];
            slot.Active = false;
            slot.StartTicks = 0;
            slot.EndTicks = 0;
            slot.DurationMs = 0;
        }

        private void StopAll()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                StopSlot((CooldownKind)i);
            }

            _hadHealingBuff = false;
        }

        private Vector2 GetTimerAnchor(Camera camera)
        {
            Profile profile = ProfileManager.CurrentProfile;

            // Manual coordinates from Options (both >= 0) are absolute positions within the
            // full client window/monitor, so the timer can be placed anywhere (over the
            // backpack, paperdoll, etc.), not just inside the game world viewport.
            // -1 / unset = under player.
            if (profile != null && profile.BandageRingTimerX >= 0 && profile.BandageRingTimerY >= 0)
            {
                return new Vector2(profile.BandageRingTimerX, profile.BandageRingTimerY);
            }

            return GetAnchorBelowStatusBars(camera);
        }

        private Vector2 GetAnchorBelowStatusBars(Camera camera)
        {
            Mobile player = _world.Player;
            Point p = player.RealScreenPosition;
            p.X += (int)player.Offset.X + 22;
            p.Y += (int)(player.Offset.Y - player.Offset.Z) + 22 + 5;
            p = camera.WorldToScreen(p);

            // WorldToScreen() returns coordinates local to the game world viewport (i.e.
            // relative to Camera.Bounds.X/Y, not the full client window). This overlay is now
            // drawn in full-window screen space (on top of gumps), so re-apply the world
            // viewport's offset within the window to keep "under player" accurate.
            p.X += camera.Bounds.X;
            p.Y += camera.Bounds.Y;

            int barsBottom = p.Y - BAR_HEIGHT_HALF + (OLD_BAR_HEIGHT + 1) * 3;
            float y = barsBottom + RING_RADIUS + 10f + DEFAULT_ANCHOR_EXTRA_Y;

            return new Vector2(p.X, y);
        }

        private bool HasHealingBuff()
        {
            PlayerMobile player = _world.Player;

            return player != null
                   && (player.IsBuffIconExists(BuffIconType.Healing)
                       || player.IsBuffIconExists(BuffIconType.Veterinary));
        }

        private long GetBuffEndTicks()
        {
            PlayerMobile player = _world.Player;

            if (player == null)
            {
                return 0;
            }

            if (player.BuffIcons.TryGetValue(BuffIconType.Healing, out BuffIcon healing))
            {
                return healing.Timer;
            }

            if (player.BuffIcons.TryGetValue(BuffIconType.Veterinary, out BuffIcon veterinary))
            {
                return veterinary.Timer;
            }

            return 0;
        }

        private static bool IsValidBuffEnd(long endTicks)
        {
            return endTicks > Time.Ticks && endTicks != unchecked((long)0xFFFF_FFFF);
        }

        private float EstimateBandageDurationSeconds()
        {
            float dex = _world.Player?.Dexterity ?? 100;
            float seconds = 11f - (dex / 20f);

            return Math.Clamp(seconds, 4f, 18f);
        }

        private static float GetFixedDuration(CooldownKind kind)
        {
            return kind switch
            {
                CooldownKind.Apple => 30f,
                CooldownKind.Conflagration => 30f,
                CooldownKind.Heal => 10f,
                _ => 10f
            };
        }

        private static ushort GetHue(CooldownKind kind)
        {
            return kind switch
            {
                CooldownKind.Bandage => HUE_BANDAGE,
                CooldownKind.Apple => HUE_APPLE,
                CooldownKind.Conflagration => HUE_CONFLA,
                CooldownKind.Heal => HUE_HEAL,
                _ => HUE_BANDAGE
            };
        }

        private static ushort GetIconHue(CooldownKind kind, Item item)
        {
            return kind switch
            {
                CooldownKind.Apple => item.Hue != 0 ? item.Hue : ICON_HUE_APPLE,
                CooldownKind.Conflagration => item.Hue != 0 ? item.Hue : ICON_HUE_CONFLA,
                CooldownKind.Heal => item.Hue,
                _ => 0
            };
        }

        private static ushort GetFallbackGraphic(CooldownKind kind)
        {
            return kind switch
            {
                CooldownKind.Bandage => BANDAGE_GRAPHIC,
                CooldownKind.Apple => APPLE_GRAPHIC,
                CooldownKind.Conflagration => CONFLA_POTION_GRAPHIC,
                CooldownKind.Heal => HEAL_POTION_GRAPHIC,
                _ => BANDAGE_GRAPHIC
            };
        }

        private static CooldownKind? ClassifyItem(Item item)
        {
            if (IsBandageItem(item))
            {
                return CooldownKind.Bandage;
            }

            string name = item.Name;

            if (string.IsNullOrEmpty(name))
            {
                name = item.ItemData.Name;
            }

            if (!string.IsNullOrEmpty(name))
            {
                if (name.IndexOf("enchanted apple", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return CooldownKind.Apple;
                }

                if (name.IndexOf("conflagration", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return CooldownKind.Conflagration;
                }

                if (name.IndexOf("heal potion", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return CooldownKind.Heal;
                }
            }

            // Fallback graphics used on many shards.
            if (item.Graphic == APPLE_GRAPHIC)
            {
                return CooldownKind.Apple;
            }

            if (item.Graphic == CONFLA_POTION_GRAPHIC)
            {
                return CooldownKind.Conflagration;
            }

            if (item.Graphic == HEAL_POTION_GRAPHIC)
            {
                return CooldownKind.Heal;
            }

            return null;
        }

        private static void EnsureCircleTexture(GraphicsDevice device)
        {
            if (_circleTexture != null && !_circleTexture.IsDisposed)
            {
                return;
            }

            const int size = 64;
            const int radius = 30;
            Color[] data = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    data[x + y * size] = d <= radius ? Color.White : Color.Transparent;
                }
            }

            _circleTexture = new Texture2D(device, size, size, false, SurfaceFormat.Color);
            _circleTexture.SetData(data);
        }

        private static void DrawBackground(UltimaBatcher2D batcher, Vector2 center)
        {
            float diameter = BG_RADIUS * 2f;
            // Flat black fill like the first bandage timer tests.
            Vector3 hue = new Vector3(0f, 1f, 0.72f);
            batcher.Draw(
                _circleTexture,
                new Rectangle(
                    (int)(center.X - BG_RADIUS),
                    (int)(center.Y - BG_RADIUS),
                    (int)diameter,
                    (int)diameter
                ),
                hue
            );
        }

        private static void DrawIcon(
            UltimaBatcher2D batcher,
            Vector2 center,
            ushort graphic,
            ushort iconHue,
            bool partialHue
        )
        {
            if (graphic == 0)
            {
                return;
            }

            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);
            var rect = Client.Game.UO.Arts.GetRealArtBounds(graphic);

            if (artInfo.Texture == null || rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            int w = rect.Width;
            int h = rect.Height;
            int x = (int)(center.X - w * 0.5f);
            int y = (int)(center.Y - h * 0.5f - 1f);

            Vector3 hue = ShaderHueTranslator.GetHueVector(iconHue, partialHue, 1f);
            batcher.Draw(
                artInfo.Texture,
                new Rectangle(x, y, w, h),
                new Rectangle(
                    artInfo.UV.X + rect.X,
                    artInfo.UV.Y + rect.Y,
                    rect.Width,
                    rect.Height
                ),
                hue
            );
        }

        private static void DrawRing(UltimaBatcher2D batcher, Vector2 center, float remaining, ushort ringHue)
        {
            Texture2D white = SolidColorTextureCache.GetTexture(Color.White);
            float startAngle = -XnaMathHelper.PiOver2;

            // Dim track for the already-consumed portion.
            DrawArcSegment(
                batcher,
                white,
                center,
                RING_RADIUS,
                startAngle + XnaMathHelper.TwoPi * remaining,
                XnaMathHelper.TwoPi * (1f - remaining),
                ShaderHueTranslator.GetHueVector(ringHue, false, 0.28f),
                RING_THICKNESS
            );

            if (remaining <= 0.001f)
            {
                return;
            }

            float sweep = XnaMathHelper.TwoPi * remaining;
            float edgeOffset = RING_THICKNESS * 0.42f;
            float edgeThickness = Math.Max(1.2f, RING_THICKNESS * 0.38f);

            // Soft center of the active ring, then crisper outer/inner edges.
            DrawArcSegment(
                batcher,
                white,
                center,
                RING_RADIUS,
                startAngle,
                sweep,
                ShaderHueTranslator.GetHueVector(ringHue, false, 0.55f),
                RING_THICKNESS
            );
            DrawArcSegment(
                batcher,
                white,
                center,
                RING_RADIUS - edgeOffset,
                startAngle,
                sweep,
                ShaderHueTranslator.GetHueVector(ringHue, false, 1f),
                edgeThickness
            );
            DrawArcSegment(
                batcher,
                white,
                center,
                RING_RADIUS + edgeOffset,
                startAngle,
                sweep,
                ShaderHueTranslator.GetHueVector(ringHue, false, 1f),
                edgeThickness
            );
        }

        private static void DrawArcSegment(
            UltimaBatcher2D batcher,
            Texture2D white,
            Vector2 center,
            float radius,
            float startAngle,
            float sweep,
            Vector3 hueVec,
            float thickness
        )
        {
            if (sweep <= 0.001f)
            {
                return;
            }

            int segments = Math.Max(2, (int)(ARC_SEGMENTS * (sweep / XnaMathHelper.TwoPi)));
            Vector2 prev = center + new Vector2(
                (float)Math.Cos(startAngle) * radius,
                (float)Math.Sin(startAngle) * radius);

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = startAngle + sweep * t;
                Vector2 next = center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius);
                batcher.DrawLine(white, prev, next, hueVec, thickness);
                prev = next;
            }
        }

        private static void DrawSeconds(UltimaBatcher2D batcher, Vector2 center, int seconds)
        {
            string text = $"{seconds}s";
            Vector2 size = Fonts.Bold.MeasureString(text);
            int x = (int)(center.X - size.X * 0.5f);
            int y = (int)(center.Y + RING_RADIUS - size.Y * 0.35f);

            Vector3 shadow = new Vector3(0f, 1f, 1f);
            batcher.DrawString(Fonts.Bold, text, x + 1, y + 1, shadow);
            batcher.DrawString(Fonts.Bold, text, x - 1, y, shadow);
            batcher.DrawString(Fonts.Bold, text, x, y - 1, shadow);

            // Always white so the countdown stays readable on any ring color.
            Vector3 hue = ShaderHueTranslator.GetHueVector(0, false, 1f);
            batcher.DrawString(Fonts.Bold, text, x, y, hue);
        }

        private static uint ReadUInt32BE(ReadOnlySpan<byte> data, int offset)
        {
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
        }

        private static ushort ReadUInt16BE(ReadOnlySpan<byte> data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        public static bool IsBandageItem(Item item)
        {
            if (item == null || item.IsDestroyed)
            {
                return false;
            }

            if (item.Graphic == BANDAGE_GRAPHIC || item.Graphic == 0x0EE9 || item.Graphic == 0x0E20)
            {
                return true;
            }

            string name = item.Name;

            if (string.IsNullOrEmpty(name))
            {
                name = item.ItemData.Name;
            }

            return !string.IsNullOrEmpty(name)
                   && name.IndexOf("bandage", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class Slot
        {
            public bool Active;
            public long StartTicks;
            public long EndTicks;
            public float DurationMs;
            public ushort IconGraphic;
            public ushort IconHue;
            public bool IconPartialHue;
            public ushort RingHue;
        }
    }
}
