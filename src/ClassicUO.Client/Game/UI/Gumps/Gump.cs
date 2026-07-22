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
using System.IO;
using System.Xml;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class Gump : Control
    {
        private const ushort LOCK_GRAPHIC = 0x082C;
        /// <summary>Inset of the padlock from the gump frame edge.</summary>
        protected const int LOCK_INSET = 3;

        private bool _isLocked;
        private bool _canMoveWhenUnlocked = true;
        private bool _canCloseWithRightClickWhenUnlocked = true;
        private bool _lockIconClickLatched;

        public Gump(World world, uint local, uint server)
        {
            World = world;
            LocalSerial = local;
            ServerSerial = server;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
        }

        public World World { get; }

        public bool CanBeSaved => GumpType != Gumps.GumpType.None;

        public virtual GumpType GumpType { get; }

        public bool InvalidateContents { get; set; }

        public uint MasterGumpSerial { get; set; }

        /// <summary>
        /// When true, Alt+Shift + left-click on the padlock toggles <see cref="IsLocked"/>.
        /// </summary>
        public bool CanBeLocked { get; set; }

        /// <summary>
        /// Locked gumps cannot be moved or closed with right-click.
        /// Padlock icon is shown only while Alt+Shift are held (see <see cref="ShowLockIcon"/>).
        /// </summary>
        public virtual bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked == value)
                {
                    return;
                }

                if (value)
                {
                    _canMoveWhenUnlocked = CanMove;
                    _canCloseWithRightClickWhenUnlocked = CanCloseWithRightClick;
                    CanMove = false;
                    CanCloseWithRightClick = false;
                }
                else
                {
                    CanMove = _canMoveWhenUnlocked;
                    CanCloseWithRightClick = _canCloseWithRightClickWhenUnlocked;
                }

                _isLocked = value;
            }
        }

        /// <summary>
        /// Padlock is visible only while Alt+Shift are held (locked or unlocked).
        /// </summary>
        protected bool ShowLockIcon => CanBeLocked && Keyboard.Alt && Keyboard.Shift;

        /// <summary>
        /// Top-left of the padlock in gump-local coordinates. Default: top-right inside the frame.
        /// </summary>
        protected virtual Point GetLockIconPosition(int iconWidth, int iconHeight)
        {
            int w = Width > 0 ? Width : iconWidth + LOCK_INSET * 2;

            return new Point(Math.Max(LOCK_INSET, w - iconWidth - LOCK_INSET), LOCK_INSET);
        }

        /// <summary>
        /// Padlock icon bounds in gump-local coordinates (draw + hitbox).
        /// </summary>
        protected Rectangle GetLockIconBounds()
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(LOCK_GRAPHIC);

            if (gumpInfo.Texture == null)
            {
                return Rectangle.Empty;
            }

            Point pos = GetLockIconPosition(gumpInfo.UV.Width, gumpInfo.UV.Height);

            return new Rectangle(pos.X, pos.Y, gumpInfo.UV.Width, gumpInfo.UV.Height);
        }

        protected bool IsMouseOverLockIcon()
        {
            if (!CanBeLocked || !ShowLockIcon)
            {
                return false;
            }

            Rectangle lockBounds = GetLockIconBounds();

            if (lockBounds.IsEmpty)
            {
                return false;
            }

            int localX = Mouse.Position.X - X - ParentX;
            int localY = Mouse.Position.Y - Y - ParentY;

            return lockBounds.Contains(localX, localY);
        }

        public override void Update()
        {
            // Re-assert lock after BuildGump / other code resets CanMove / CanCloseWithRightClick.
            if (_isLocked)
            {
                CanMove = false;
                CanCloseWithRightClick = false;
            }

            ProcessLockIconInput();

            if (InvalidateContents)
            {
                UpdateContents();
                InvalidateContents = false;
            }

            if (ActivePage == 0)
            {
                ActivePage = 1;
            }

            base.Update();
        }

        /// <summary>
        /// Toggle padlock on Alt+Shift+press while the cursor is over the icon.
        /// Uses press (not MouseUp) so it works when a child control owns the hit-test
        /// and when the icon is drawn outside gump Bounds (title-adjacent GridContainer).
        /// </summary>
        private void ProcessLockIconInput()
        {
            if (!ShowLockIcon || Keyboard.Ctrl)
            {
                _lockIconClickLatched = false;

                return;
            }

            if (!Mouse.LButtonPressed)
            {
                _lockIconClickLatched = false;

                return;
            }

            if (_lockIconClickLatched || !IsMouseOverLockIcon())
            {
                return;
            }

            _lockIconClickLatched = true;
            IsLocked = !IsLocked;
        }

        public override void Dispose()
        {
            Item it = World.Items.Get(LocalSerial);

            if (it != null && it.Opened)
            {
                it.Opened = false;
            }

            base.Dispose();
        }


        public virtual void Save(XmlTextWriter writer)
        {
            writer.WriteAttributeString("type", ((int) GumpType).ToString());
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteAttributeString("serial", LocalSerial.ToString());

            if (CanBeLocked)
            {
                writer.WriteAttributeString("isLocked", IsLocked.ToString());
            }
        }

        public void SetInScreen()
        {
            Rectangle windowBounds = Client.Game.Window.ClientBounds;
            Rectangle bounds = Bounds;
            bounds.X += windowBounds.X;
            bounds.Y += windowBounds.Y;

            if (windowBounds.Intersects(bounds))
            {
                return;
            }

            X = 0;
            Y = 0;
        }

        public virtual void Restore(XmlElement xml)
        {
            if (CanBeLocked && bool.TryParse(xml.GetAttribute("isLocked"), out bool locked))
            {
                IsLocked = locked;
            }
        }

        public void RequestUpdateContents()
        {
            InvalidateContents = true;
        }

        protected virtual void UpdateContents()
        {
        }

        protected override void OnDragEnd(int x, int y)
        {
            Point position = Location;
            int halfWidth = Width - (Width >> 2);
            int halfHeight = Height - (Height >> 2);

            if (X < -halfWidth)
            {
                position.X = -halfWidth;
            }

            if (Y < -halfHeight)
            {
                position.Y = -halfHeight;
            }

            if (X > Client.Game.Window.ClientBounds.Width - (Width - halfWidth))
            {
                position.X = Client.Game.Window.ClientBounds.Width - (Width - halfWidth);
            }

            if (Y > Client.Game.Window.ClientBounds.Height - (Height - halfHeight))
            {
                position.Y = Client.Game.Window.ClientBounds.Height - (Height - halfHeight);
            }

            Location = position;

            // Persist movable server gump positions (incl. Razor Running Scripts / Agent Status).
            // Control.OnDragEnd does this, but this override previously skipped it — so logout/restart lost the spot.
            if (ServerSerial != 0 && CanMove && !IsDisposed)
            {
                UIManager.SavePosition(ServerSerial, Location);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!IsVisible)
            {
                return false;
            }

            base.Draw(batcher, x, y);

            if (ShowLockIcon)
            {
                DrawLockIcon(batcher, x, y);
            }

            return true;
        }

        protected virtual void DrawLockIcon(UltimaBatcher2D batcher, int x, int y)
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(LOCK_GRAPHIC);

            if (gumpInfo.Texture == null)
            {
                return;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            // Highlight only when the cursor is over the padlock itself (matches click target).
            if (Keyboard.Alt && Keyboard.Shift && IsMouseOverLockIcon())
            {
                hueVector.X = 34;
                hueVector.Y = 1;
            }

            Rectangle lockBounds = GetLockIconBounds();

            batcher.Draw(
                gumpInfo.Texture,
                new Vector2(x + lockBounds.X, y + lockBounds.Y),
                gumpInfo.UV,
                hueVector
            );
        }

        /// <summary>
        /// Returns true when an Alt+Shift padlock click should be consumed (suppress other actions).
        /// The actual toggle is applied in <see cref="ProcessLockIconInput"/> on mouse press.
        /// </summary>
        protected bool TryToggleLock(int x, int y, MouseButtonType button)
        {
            if (
                !CanBeLocked
                || button != MouseButtonType.Left
                || !Keyboard.Alt
                || !Keyboard.Shift
                || Keyboard.Ctrl
            )
            {
                return false;
            }

            Rectangle lockBounds = GetLockIconBounds();

            return IsMouseOverLockIcon()
                || (!lockBounds.IsEmpty && lockBounds.Contains(x, y));
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            TryToggleLock(x, y, button);
            base.OnMouseUp(x, y, button);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (!IsDisposed && LocalSerial != 0)
            {
                List<uint> switches = new List<uint>();
                List<Tuple<ushort, string>> entries = new List<Tuple<ushort, string>>();

                foreach (Control control in Children)
                {
                    switch (control)
                    {
                        case Checkbox checkbox when checkbox.IsChecked:
                            switches.Add(control.LocalSerial);

                            break;

                        case StbTextBox textBox:
                            entries.Add(new Tuple<ushort, string>((ushort) textBox.LocalSerial, textBox.Text));

                            break;
                    }
                }

                GameActions.ReplyGump
                (
                    LocalSerial,
                    // Seems like MasterGump serial does not work as expected.
                    /*MasterGumpSerial != 0 ? MasterGumpSerial :*/ ServerSerial,
                    buttonID,
                    switches.ToArray(),
                    entries.ToArray()
                );

                if (CanMove)
                {
                    UIManager.SavePosition(ServerSerial, Location);
                }
                else
                {
                    UIManager.RemovePosition(ServerSerial);
                }

                Dispose();
            }
        }

        protected override void CloseWithRightClick()
        {
            if (!CanCloseWithRightClick)
            {
                return;
            }

            if (ServerSerial != 0)
            {
                OnButtonClick(0);
            }

            base.CloseWithRightClick();
        }

        public override void ChangePage(int pageIndex)
        {
            // For a gump, Page is the page that is drawing.
            ActivePage = pageIndex;
        }
    }
}