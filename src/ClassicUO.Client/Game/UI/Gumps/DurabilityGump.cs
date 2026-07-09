#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class DurabilityGumpMinimized : Control
    {
        private readonly World _world;

        public uint Graphic { get; set; } = 5587;

        public DurabilityGumpMinimized(World world)
        {
            _world = world;
            SetTooltip("Open Equipment Durability Tracker");
            WantUpdateSize = true;
            AcceptMouseInput = true;
            Width = 30;
            Height = 30;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ref readonly var texture = ref Client.Game.UO.Gumps.GetGump(Graphic);

            if (texture.Texture != null)
            {
                batcher.Draw(
                    texture.Texture,
                    new Rectangle(x, y, Width, Height),
                    texture.UV,
                    ShaderHueTranslator.GetHueVector(0)
                );
            }

            return base.Draw(batcher, x, y);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                UIManager.GetGump<DurabilitysGump>()?.Dispose();
                UIManager.Add(new DurabilitysGump(_world));
            }
        }
    }

    internal class DurabilitysGump : Gump
    {
        private const int WIDTH = 300;
        private const int HEIGHT = 400;

        private static int _lastX;
        private static int _lastY;

        private enum DurabilityColors
        {
            RED = 0x0805,
            BLUE = 0x0806,
            GREEN = 0x0808,
            YELLOW = 0x0809
        }

        private readonly DataBox _dataBox;

        public DurabilitysGump(World world) : base(world, 0, 0)
        {
            CanCloseWithRightClick = true;
            CanMove = true;
            Width = WIDTH;
            Height = HEIGHT;
            X = _lastX;
            Y = _lastY;

            if (_lastX == 0 && _lastY == 0)
            {
                X = _lastX = (Client.Game.Scene.Camera.Bounds.Width - Width) / 2;
                Y = _lastY = Client.Game.Scene.Camera.Bounds.Y + 20;
            }

            Add(new BorderControl(0, 0, Width, Height, 4));
            Add(new AlphaBlendControl(0.9f) { Width = Width, Height = Height });

            var header = new DataBox(0, 0, Width, 1) { WantUpdateSize = true, CanMove = true };
            Label title = new Label("Equipment Durability", true, 0xFFFF);
            title.X = (Width >> 1) - (title.Width >> 1);
            title.Y = (title.Height >> 1) >> 1;
            header.Height = title.Y + title.Height;
            header.Add(title);
            Add(header);

            ScrollArea area = new ScrollArea(10, 30, Width - 20, Height - 50, true)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            Add(area);
            _dataBox = new DataBox(0, 0, Width - 40, Height - 20);
            area.Add(_dataBox);
            UpdateContents();
        }

        public override GumpType GumpType => GumpType.DurabilityGump;

        public override void Dispose()
        {
            _lastX = X;
            _lastY = Y;
            base.Dispose();
        }

        protected override void UpdateContents()
        {
            _dataBox.Clear();
            _dataBox.WantUpdateSize = true;

            Rectangle barBounds = Client.Game.UO.Gumps.GetGump((uint)DurabilityColors.RED).UV;
            int startY = 0;

            IReadOnlyList<DurabiltyProp> items = World.DurabilityManager?.Durabilities ?? Array.Empty<DurabiltyProp>();

            foreach (DurabiltyProp durability in items.OrderBy(d => d.Percentage))
            {
                if (durability.MaxDurabilty <= 0)
                {
                    continue;
                }

                if (!World.Items.TryGetValue((uint)durability.Serial, out Item item))
                {
                    continue;
                }

                var row = new DataBox(0, startY, Width - 40, 44)
                {
                    AcceptMouseInput = true,
                    CanMove = true
                };

                string itemName = string.IsNullOrWhiteSpace(item.Name) ? item.Layer.ToString() : item.Name;
                Label name = new Label(itemName, true, 0xFFFF);
                row.Add(name);

                GumpPic red = new GumpPic(0, name.Y + name.Height + 5, (ushort)DurabilityColors.RED, 0);
                row.Add(red);

                DurabilityColors statusGump = DurabilityColors.GREEN;

                if (durability.Percentage < 0.7f)
                {
                    statusGump = DurabilityColors.YELLOW;
                }
                else if (durability.Percentage < 0.95f)
                {
                    statusGump = DurabilityColors.BLUE;
                }

                if (durability.Percentage > 0)
                {
                    row.Add(new GumpPicTiled(
                        0,
                        red.Y,
                        (int)Math.Floor(barBounds.Width * durability.Percentage),
                        barBounds.Height,
                        (ushort)statusGump));
                }

                string durText = $"{durability.Durabilty} / {durability.MaxDurabilty}";
                int durWidth = FontsLoader.Instance.GetWidthUnicode(0, durText);
                row.Add(new Label(durText, true, 0xFFFF) { Y = red.Y - 2, X = Width - 38 - durWidth });
                _dataBox.Add(row);
                startY += row.Height + 4;
            }
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("lastX", X.ToString());
            writer.WriteAttributeString("lastY", Y.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            if (int.TryParse(xml.GetAttribute("lastX"), out int lastX))
            {
                X = lastX;
            }

            if (int.TryParse(xml.GetAttribute("lastY"), out int lastY))
            {
                Y = lastY;
            }
        }
    }
}
