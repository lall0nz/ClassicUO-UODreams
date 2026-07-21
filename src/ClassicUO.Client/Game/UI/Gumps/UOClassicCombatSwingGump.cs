// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Dust765;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class UOClassicCombatSwingGump : Gump
    {
        public static readonly List<ushort> WeaponsList = new List<ushort>();

        // Weapon base delay in ticks (tooltip seconds * 4). Bows/archery first.
        private static readonly Dictionary<ushort, ushort> DefaultBaseTicks = new Dictionary<ushort, ushort>
        {
            // Bows / ranged
            { 0x13B1, 16 }, { 0x13B2, 16 }, // Bow 4.00s
            { 0x13FC, 10 }, { 0x13FD, 10 }, // Composite Bow 2.50s
            { 0x0F4F, 18 }, { 0x0F50, 18 }, // Crossbow 4.50s
            { 0x0F62, 20 }, { 0x0F63, 20 }, // Heavy Crossbow 5.00s
            { 0x26C2, 11 }, { 0x26C3, 11 }, // Repeating Crossbow ~2.75s
            { 0x26C5, 10 }, { 0x26C6, 10 }, // Elven Composite Bow 2.50s
            { 0x2D1E, 10 }, { 0x2D2A, 10 }, // Magical Shortbow 2.50s
            { 0x2D1F, 16 }, { 0x2D2B, 16 }, // Yumi 4.00s
            // Melee (common)
            { 0x1400, 14 }, { 0x1401, 14 },
            { 0x13FE, 10 }, { 0x13FF, 10 },
            { 0x1440, 11 }, { 0x1441, 11 },
            { 0x13B5, 11 }, { 0x13B6, 11 },
            { 0x13B3, 12 }, { 0x13B4, 12 },
            { 0x143C, 16 }, { 0x143D, 16 },
            { 0x13AF, 12 }, { 0x13B0, 12 },
            { 0x1404, 13 }, { 0x1405, 13 },
            { 0x13B7, 13 }, { 0x13B8, 13 },
            { 0x0F60, 13 }, { 0x0F61, 13 },
            { 0x0F5E, 14 }, { 0x0F5F, 14 },
            { 0x13B9, 15 }, { 0x13BA, 15 },
            { 0x0F5C, 15 }, { 0x0F5D, 15 },
            { 0x143A, 15 }, { 0x143B, 15 },
            { 0x1406, 11 }, { 0x1407, 11 },
            { 0x1402, 15 }, { 0x1403, 15 },
            { 0x0E87, 12 }, { 0x0E88, 12 },
            { 0x0F49, 15 }, { 0x0F4A, 15 },
            { 0x0F47, 14 }, { 0x0F48, 14 },
            { 0x0F4B, 13 }, { 0x0F4C, 13 },
            { 0x0F45, 14 }, { 0x0F46, 14 },
            { 0x13FA, 13 }, { 0x13FB, 13 },
            { 0x1442, 12 }, { 0x1443, 12 },
            { 0x13F8, 13 }, { 0x13F9, 13 },
            { 0x0DF0, 11 }, { 0x0DF1, 11 },
            { 0x0E89, 12 }, { 0x0E8A, 12 },
            { 0x143E, 17 }, { 0x143F, 17 },
            { 0x0F4D, 17 }, { 0x0F4E, 17 },
            { 0x1438, 14 }, { 0x1439, 14 },
        };

        private const byte FONT = 0xFF;
        private const ushort HUE_YELLOW = 0x35;
        private const ushort HUE_RED = 0x26;
        private const ushort HUE_GREEN = 0x3F;
        private const ushort HUE_WAR_BORDER = 0x0026;

        private const int LINE_HEIGHT = 24;
        private const int LABEL_WIDTH = 44;
        private const int LINE_CONTENT_GAP = 4;
        private const int LINE_BAR_OUTER_W = 52;
        private const int LINE_BAR_OUTER_H = 12;
        private const int LINE_BAR_TRACK_W = 44;
        private const int LINE_BAR_TRACK_H = 8;
        private const int LINE_TIMER_SLOT_W = 22;
        private const int LINE_PADDING_LEFT = 4;
        private const int LINE_PADDING_RIGHT = 4;
        private const float TEXT_SCALE = 1.1f;

        private static readonly uint COLOR_TRACK = Color.FromNonPremultiplied(38, 38, 38, 255).PackedValue;
        private static readonly uint COLOR_SHELL = Color.FromNonPremultiplied(18, 18, 18, 255).PackedValue;
        private static readonly uint COLOR_FILL = Color.FromNonPremultiplied(210, 175, 55, 255).PackedValue;
        private static readonly uint COLOR_FILL_READY = Color.FromNonPremultiplied(80, 185, 75, 255).PackedValue;

        private readonly HudPanelFrameControl _frame;
        private readonly HudFillBar _barShell;
        private readonly HudFillBar _barTrack;
        private readonly HudFillBar _barFill;
        private readonly HudSwingLabel _timerLabel;

        private readonly int _barX;
        private readonly int _barY;
        private readonly int _trackX;
        private readonly int _trackY;
        private readonly int _timerX;

        private bool _triggerSwing;
        private uint _timerSwing;
        private uint _tickSwing;
        private uint _activeSwingCooldownMs;

        public UOClassicCombatSwingGump(World world)
            : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            LayerOrder = UILayer.Over;

            if (ProfileManager.CurrentProfile.SwingTimerBarLocked)
            {
                CanMove = false;
                AcceptMouseInput = false;
            }

            _barY = (LINE_HEIGHT - LINE_BAR_OUTER_H) / 2;
            int labelX = LINE_PADDING_LEFT;
            _barX = labelX + LABEL_WIDTH + LINE_CONTENT_GAP;
            _timerX = _barX + LINE_BAR_OUTER_W + 6;
            int panelWidth = _timerX + LINE_TIMER_SLOT_W + LINE_PADDING_RIGHT;

            _trackX = _barX + (LINE_BAR_OUTER_W - LINE_BAR_TRACK_W) / 2;
            _trackY = _barY + (LINE_BAR_OUTER_H - LINE_BAR_TRACK_H) / 2;

            Width = panelWidth;
            Height = LINE_HEIGHT;

            Add(
                new AlphaBlendControl(0.9f)
                {
                    X = 0,
                    Y = 0,
                    Width = panelWidth,
                    Height = LINE_HEIGHT
                }
            );

            Add(
                _frame = new HudPanelFrameControl
                {
                    X = 0,
                    Y = 0,
                    Width = panelWidth,
                    Height = LINE_HEIGHT,
                    AcceptMouseInput = false
                }
            );

            HudSwingLabel swingLabel = new HudSwingLabel(
                "Swing",
                HUE_YELLOW,
                LABEL_WIDTH,
                FONT,
                TEXT_ALIGN_TYPE.TS_CENTER
            );
            swingLabel.X = labelX;
            swingLabel.Y = _barY + (LINE_BAR_OUTER_H - swingLabel.Height) / 2;
            swingLabel.AcceptMouseInput = false;
            Add(swingLabel);

            _barShell = new HudFillBar(_barX, _barY, LINE_BAR_OUTER_W, LINE_BAR_OUTER_H, COLOR_SHELL);
            _barShell.AcceptMouseInput = false;
            Add(_barShell);

            _barTrack = new HudFillBar(_trackX, _trackY, LINE_BAR_TRACK_W, LINE_BAR_TRACK_H, COLOR_TRACK);
            _barTrack.AcceptMouseInput = false;
            Add(_barTrack);

            _barFill = new HudFillBar(_trackX, _trackY, LINE_BAR_TRACK_W, LINE_BAR_TRACK_H, COLOR_FILL_READY);
            _barFill.AcceptMouseInput = false;
            Add(_barFill);

            _timerLabel = new HudSwingLabel(
                "0",
                HUE_GREEN,
                LINE_TIMER_SLOT_W,
                FONT,
                TEXT_ALIGN_TYPE.TS_CENTER
            );
            _timerLabel.X = _timerX;
            _timerLabel.Y = _barY + (LINE_BAR_OUTER_H - _timerLabel.Height) / 2;
            _timerLabel.AcceptMouseInput = false;
            Add(_timerLabel);

            WantUpdateSize = false;
            LoadSwingTimerFile();
            ResetBarVisuals();
        }

        public override GumpType GumpType => GumpType.None;

        internal static void NotifyPlayerAnimation(World world, ushort action)
        {
            if (world?.Player == null)
            {
                return;
            }

            if (
                action >= 9 && action <= 15
                || action == 18
                || action == 19
                || action >= 26 && action <= 29
                || action == 31
            )
            {
                UIManager.GetGump<UOClassicCombatSwingGump>()?.ClilocTriggerSwing();
            }
        }

        internal static void NotifyPlayerNewAnimation(World world, ushort type, ushort action)
        {
            if (world?.Player == null)
            {
                return;
            }

            if (type == 0 && (action == 18 || action == 19))
            {
                UIManager.GetGump<UOClassicCombatSwingGump>()?.ClilocTriggerSwing();
            }
        }

        internal static void NotifyPlayerSwing(World world)
        {
            if (world?.Player == null)
            {
                return;
            }

            UIManager.GetGump<UOClassicCombatSwingGump>()?.ClilocTriggerSwing(true);
        }

        public void ClilocTriggerSwing(bool authoritative = false)
        {
            if (!ProfileManager.CurrentProfile.ShowSwingTimerBar)
            {
                return;
            }

            if (
                !authoritative
                && _triggerSwing
                && _tickSwing != 0
                && _activeSwingCooldownMs > 0
                && _tickSwing + _activeSwingCooldownMs > Time.Ticks
            )
            {
                return;
            }

            _tickSwing = Time.Ticks;
            _triggerSwing = true;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.CurrentProfile.SwingTimerBarLocation = new Point(ScreenCoordinateX, ScreenCoordinateY);
        }

        public override void Update()
        {
            base.Update();

            if (World.Player == null || World.Player.IsDestroyed)
            {
                SwingTimerService.SetSwingReady(false);
                return;
            }

            UpdateFrameHue();

            if (_timerSwing == 0)
            {
                _timerLabel.Hue = HUE_GREEN;
            }
            else
            {
                _timerLabel.Hue = HUE_RED;
            }

            if (_triggerSwing)
            {
                RunSwingCooldown();
            }

            SwingTimerService.SetSwingReady(_timerSwing == 0 && !_triggerSwing);
        }

        private void UpdateFrameHue()
        {
            if (World.Player.InWarMode)
            {
                _frame.OuterHue = HUE_WAR_BORDER;
                return;
            }

            NotorietyFlag flag = World.Player.NotorietyFlag;

            if (flag == NotorietyFlag.Criminal || flag == NotorietyFlag.Gray)
            {
                _frame.OuterHue = 34;
                return;
            }

            ushort hue = Notoriety.GetHue(flag);
            _frame.OuterHue = hue > 0 ? hue : (ushort)34;
        }

        private void ResetBarVisuals()
        {
            _barShell.FillWidth = LINE_BAR_OUTER_W;
            _barTrack.FillWidth = LINE_BAR_TRACK_W;
            _barFill.FillWidth = LINE_BAR_TRACK_W;
            _barFill.SetColor(COLOR_FILL_READY);
        }

        private void RunSwingCooldown()
        {
            uint swingCooldown = CalculateSwingCooldownMs(World.Player);

            _activeSwingCooldownMs = swingCooldown;

            if (_tickSwing != 0)
            {
                _timerSwing = swingCooldown / 100 - (Time.Ticks - _tickSwing) / 100;
            }

            if (_tickSwing != 0 && _tickSwing + swingCooldown <= Time.Ticks)
            {
                _timerSwing = 0;
                _tickSwing = 0;
                _triggerSwing = false;
                _activeSwingCooldownMs = 0;
                ResetBarVisuals();
            }

            _timerLabel.Text = $"{_timerSwing}";

            if (_tickSwing != 0 && swingCooldown > 0)
            {
                float progress = _timerSwing / (swingCooldown / 100f);
                progress = Math.Clamp(progress, 0f, 1f);
                _barFill.SetColor(progress <= 0.2f ? COLOR_FILL_READY : COLOR_FILL);
                _barFill.FillWidth = (int)(LINE_BAR_TRACK_W * progress);
                _barShell.FillWidth = LINE_BAR_OUTER_W;
                _barTrack.FillWidth = LINE_BAR_TRACK_W;
            }
        }

        private static uint CalculateSwingCooldownMs(PlayerMobile player)
        {
            Item weapon = player.FindItemByLayer(Layer.TwoHanded)
                ?? player.FindItemByLayer(Layer.OneHanded);

            // OSI AOS: weapon base ticks + current stamina (DEX drives max stam) + SSI.
            uint baseTicks = weapon != null ? GetWeaponBaseTicks(weapon.Graphic) : 10;
            int stamina = Math.Min((int)player.Stamina, 240);

            // Prefer current stamina; if stam packet is stale/zero, fall back to DEX.
            if (stamina <= 0 && player.Dexterity > 0)
            {
                stamina = Math.Min((int)player.Dexterity, 240);
            }

            int stamTicks = stamina / 30;
            int preSsiTicks = Math.Max(0, (int)baseTicks - stamTicks);
            int ssi = Math.Max(0, Math.Min((int)player.SwingSpeedIncrease, 60));
            int finalTicks = (int)Math.Floor(preSsiTicks * 100.0 / (100.0 + ssi));

            finalTicks = Math.Max(5, finalTicks);

            return (uint)(finalTicks * 250);
        }

        private static uint GetWeaponBaseTicks(ushort graphic)
        {
            // Prefer known UO tooltip speeds (ticks = seconds * 4).
            if (DefaultBaseTicks.TryGetValue(graphic, out ushort knownTicks))
            {
                return knownTicks;
            }

            int index = WeaponsList.IndexOf(graphic);

            if (index >= 0 && index + 1 < WeaponsList.Count)
            {
                ushort value = WeaponsList[index + 1];

                // New format: base ticks (1..25). Old Dust765 "speed" values are > 25 — ignore them.
                if (value >= 1 && value <= 25)
                {
                    return value;
                }
            }

            return 10;
        }

        private static void LoadSwingTimerFile()
        {
            if (WeaponsList.Count > 0)
            {
                return;
            }

            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string swingPath = Path.Combine(path, "swingtimer.txt");

            if (!File.Exists(swingPath))
            {
                CreateDefaultSwingTimerFile(swingPath);
            }

            TextFileParser parser = new TextFileParser(
                File.ReadAllText(swingPath),
                new[] { ' ', '\t', ',', '=' },
                new[] { '#', ';' },
                new[] { '"', '"' }
            );

            while (!parser.IsEOF())
            {
                List<string> tokens = parser.ReadTokens();

                if (tokens == null || tokens.Count == 0)
                {
                    continue;
                }

                if (tokens.Count > 0 && ushort.TryParse(tokens[0], out ushort graphic))
                {
                    WeaponsList.Add(graphic);
                }

                if (tokens.Count > 1 && ushort.TryParse(tokens[1], out ushort spd))
                {
                    WeaponsList.Add(spd);
                }
            }
        }

        private static void CreateDefaultSwingTimerFile(string swingPath)
        {
            ushort[] weapons =
            {
                0x1400, 0x1401, 0x13FE, 0x13FF, 0x1440, 0x1441, 0x13B5, 0x13B6,
                0x13B3, 0x13B4, 0x143C, 0x143D, 0x13AF, 0x13B0, 0x1404, 0x1405,
                0x13B7, 0x13B8, 0x0F60, 0x0F61, 0x0F5E, 0x0F5F, 0x13B9, 0x13BA,
                0x0F5C, 0x0F5D, 0x143A, 0x143B, 0x1406, 0x1407, 0x13B1, 0x13B2,
                0x1402, 0x1403, 0x0E87, 0x0E88, 0x0F49, 0x0F4A, 0x0F47, 0x0F48,
                0x0F4B, 0x0F4C, 0x0F45, 0x0F46, 0x13FA, 0x13FB, 0x1442, 0x1443,
                0x13F8, 0x13F9, 0x0DF0, 0x0DF1, 0x0E89, 0x0E8A, 0x0F4F, 0x0F50,
                0x0F62, 0x0F63, 0x143E, 0x143F, 0x0F4D, 0x0F4E, 0x1438, 0x1439,
                0x13FC, 0x13FD
            };

            ushort[] speeds =
            {
                14, 14, 10, 10, 11, 11, 11, 11, 12, 12, 16, 16, 12, 12, 13, 13,
                13, 13, 13, 13, 14, 14, 15, 15, 15, 15, 15, 15, 11, 11, 16, 16,
                15, 15, 12, 12, 15, 15, 14, 14, 13, 13, 14, 14, 13, 13, 12, 12,
                13, 13, 11, 11, 12, 12, 18, 18, 20, 20, 17, 17, 17, 17, 14, 14,
                10, 10
            };

            using StreamWriter w = new StreamWriter(swingPath);

            w.WriteLine("# graphic=base_ticks (weapon tooltip seconds * 4)");

            for (int i = 0; i < weapons.Length && i < speeds.Length; i++)
            {
                w.WriteLine($"{weapons[i]}={speeds[i]}");
            }

            // Extra ranged weapons
            w.WriteLine("9922=11"); // 0x26C2 Repeating Crossbow
            w.WriteLine("9923=11"); // 0x26C3
            w.WriteLine("9925=10"); // 0x26C5 Elven Composite
            w.WriteLine("9926=10"); // 0x26C6
        }

        internal static void RefreshOpenGump(World world)
        {
            UOClassicCombatSwingGump existing = UIManager.GetGump<UOClassicCombatSwingGump>();

            if (existing != null)
            {
                existing.Dispose();
            }

            Profile p = ProfileManager.CurrentProfile;

            if (p != null && p.ShowSwingTimerBar)
            {
                UIManager.Add(
                    new UOClassicCombatSwingGump(world)
                    {
                        X = p.SwingTimerBarLocation.X,
                        Y = p.SwingTimerBarLocation.Y
                    }
                );
            }
            else
            {
                SwingTimerService.SetSwingReady(false);
            }
        }

        private sealed class HudSwingLabel : Control
        {
            private readonly RenderedText _text;

            public HudSwingLabel(
                string text,
                ushort hue,
                int slotWidth,
                byte font,
                TEXT_ALIGN_TYPE align
            )
            {
                _text = RenderedText.Create(
                    text,
                    hue,
                    font,
                    true,
                    FontStyle.BlackBorder,
                    align,
                    slotWidth
                );

                Width = slotWidth;
                Height = (int)Math.Ceiling(_text.Height * TEXT_SCALE);
                AcceptMouseInput = false;
            }

            public string Text
            {
                get => _text.Text;
                set
                {
                    _text.Text = value;
                    Height = (int)Math.Ceiling(_text.Height * TEXT_SCALE);
                }
            }

            public ushort Hue
            {
                get => _text.Hue;
                set => _text.Hue = value;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (IsDisposed)
                {
                    return false;
                }

                int drawWidth = (int)Math.Ceiling(_text.Width * TEXT_SCALE);
                int drawHeight = (int)Math.Ceiling(_text.Height * TEXT_SCALE);
                int drawX = x + (Width - drawWidth) / 2;
                int drawY = y + (Height - drawHeight) / 2;

                _text.Draw(
                    batcher,
                    _text.Width,
                    _text.Height,
                    drawX,
                    drawY,
                    drawWidth,
                    drawHeight,
                    0,
                    0
                );

                return base.Draw(batcher, x, y);
            }

            public override void Dispose()
            {
                base.Dispose();
                _text.Destroy();
            }
        }
    }
}
