#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// Portions adapted from PlayTazUO/TazUO ModernColorPicker (BSD-2-Clause).

#endregion

using System;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// TazUO-style multi-page dye-tub hue grid (black/white/specials included).
    /// </summary>
    internal class ModernColorPicker : Gump
    {
        private const int WIDTH = 200;
        private const int HEIGHT = 400;
        private const int BORDER = 10;
        private const int ROWS = 20;
        private const int COLUMNS = 10;

        private readonly Action<ushort> _hueChanged;
        private readonly DataBox _grid;
        private readonly Label _pageLabel;
        private int _pages;
        private int _cPage;

        public ModernColorPicker(World world, Action<ushort> hueChanged) : base(world, 0, 0)
        {
            _hueChanged = hueChanged;
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;
            Width = WIDTH;
            Height = HEIGHT;

            int huesCount = Math.Max(1, HuesLoader.Instance.HuesCount);
            _pages = Math.Max(0, (int)Math.Ceiling(huesCount / (double)(ROWS * COLUMNS)) - 1);

            Add(
                new AlphaBlendControl(0.92f)
                {
                    X = 0,
                    Y = 0,
                    Width = WIDTH,
                    Height = HEIGHT
                }
            );

            Add(
                _grid = new DataBox(BORDER, BORDER, WIDTH - BORDER * 2, HEIGHT - BORDER * 2 - 24)
                {
                    WantUpdateSize = false,
                    AcceptMouseInput = true
                }
            );

            _pageLabel = new Label("1", true, 0xFFFF, 40, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = (WIDTH / 2) - 20,
                Y = HEIGHT - BORDER - 20
            };
            Add(_pageLabel);

            NiceButton prev = new NiceButton(BORDER, HEIGHT - BORDER - 20, 20, 20, ButtonAction.Activate, "<")
            {
                IsSelectable = false
            };
            prev.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    _cPage--;
                    if (_cPage < 0)
                    {
                        _cPage = _pages;
                    }

                    FillHueDisplays();
                    _pageLabel.Text = (_cPage + 1).ToString();
                }
            };
            Add(prev);

            NiceButton next = new NiceButton(WIDTH - BORDER - 20, HEIGHT - BORDER - 20, 20, 20, ButtonAction.Activate, ">")
            {
                IsSelectable = false
            };
            next.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    _cPage++;
                    if (_cPage > _pages)
                    {
                        _cPage = 0;
                    }

                    FillHueDisplays();
                    _pageLabel.Text = (_cPage + 1).ToString();
                }
            };
            Add(next);

            FillHueDisplays();

            X = (Client.Game.Window.ClientBounds.Width - Width) >> 1;
            Y = (Client.Game.Window.ClientBounds.Height - Height) >> 1;
        }

        private void FillHueDisplays()
        {
            for (int i = _grid.Children.Count - 1; i >= 0; i--)
            {
                _grid.Children[i].Dispose();
            }

            int huesCount = Math.Max(1, HuesLoader.Instance.HuesCount);

            for (int col = 1; col < COLUMNS + 1; col++)
            {
                for (int row = 1; row < ROWS + 1; row++)
                {
                    int index = row + (col - 1) * ROWS + _cPage * ROWS * COLUMNS - 1;

                    if (index < 0 || index >= huesCount)
                    {
                        continue;
                    }

                    _grid.Add(
                        new HueDisplay(World, (ushort)index, OnHuePicked)
                        {
                            X = (col - 1) * 18,
                            Y = (row - 1) * 18
                        }
                    );
                }
            }
        }

        private void OnHuePicked(ushort hue)
        {
            _hueChanged?.Invoke(hue);
            Dispose();
        }

        internal class HueDisplay : Control
        {
            private readonly Action<ushort> _hueChanged;
            private readonly Rectangle _uv;
            private readonly Rectangle _realBounds;
            private readonly Texture2D _texture;
            private Vector3 _hueVector;
            private bool _flash;
            private float _flashAlpha = 1f;
            private bool _rev;

            public HueDisplay(World world, ushort hue, Action<ushort> hueChanged)
            {
                Hue = hue;
                _hueChanged = hueChanged;
                _hueVector = ShaderHueTranslator.GetHueVector(hue, true, 1f);

                ref readonly SpriteInfo staticArt = ref Client.Game.UO.Arts.GetArt(0x0FAB);
                _texture = staticArt.Texture;
                _uv = staticArt.UV;
                _realBounds = Client.Game.UO.Arts.GetRealArtBounds(0x0FAB);

                Width = 18;
                Height = 18;
                CanMove = true;
                AcceptMouseInput = true;
                SetTooltip(hue.ToString());
            }

            public ushort Hue { get; }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);

                if (button == MouseButtonType.Left)
                {
                    _hueChanged?.Invoke(Hue);
                    _flash = true;
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                if (_texture == null)
                {
                    return true;
                }

                Vector3 hueVector = _hueVector;

                if (_flash)
                {
                    hueVector.Z = _flashAlpha;

                    if (!_rev)
                    {
                        _flashAlpha -= 0.1f;
                    }
                    else
                    {
                        _flashAlpha += 0.1f;
                    }

                    if (_flashAlpha <= 0f)
                    {
                        _rev = true;
                    }
                    else if (_flashAlpha >= 1f)
                    {
                        _rev = false;
                        _flash = false;
                    }
                }

                batcher.Draw(
                    _texture,
                    new Rectangle(x, y, Width, Height),
                    new Rectangle(
                        _uv.X + _realBounds.X,
                        _uv.Y + _realBounds.Y,
                        _realBounds.Width,
                        _realBounds.Height
                    ),
                    hueVector
                );

                return true;
            }
        }
    }
}
