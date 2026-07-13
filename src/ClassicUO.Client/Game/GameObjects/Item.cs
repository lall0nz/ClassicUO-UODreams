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
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item : Entity
    {
        //private static readonly QueuedPool<Item> _pool = new QueuedPool<Item>(
        //    Constants.PREDICTABLE_CHUNKS * 3,
        //    i =>
        //    {
        //        i.IsDestroyed = false;
        //        i.Graphic = 0;
        //        i.Amount = 0;
        //        i.Container = 0xFFFF_FFFF;
        //        i._isMulti = false;
        //        i.Layer = 0;
        //        i.Price = 0;
        //        i.UsedLayer = false;
        //        i._displayedGraphic = null;
        //        i.X = 0;
        //        i.Y = 0;
        //        i.Z = 0;

        //        i.LightID = 0;
        //        i.MultiDistanceBonus = 0;
        //        i.Flags = 0;
        //        i.WantUpdateMulti = true;
        //        i.MultiInfo = null;
        //        i.MultiGraphic = 0;

        //        i.AlphaHue = 0;
        //        i.Name = null;
        //        i.Direction = 0;
        //        i.AnimIndex = 0;
        //        i.Hits = 0;
        //        i.HitsMax = 0;
        //        i.LastStepTime = 0;
        //        i.LastAnimationChangeTime = 0;

        //        i.Clear();

        //        i.IsClicked = false;
        //        i.IsDamageable = false;
        //        i.Offset = Vector3.Zero;
        //        i.HitsPercentage = 0;
        //        i.Opened = false;
        //        i.TextContainer?.Clear();
        //        i.IsFlipped = false;
        //        i.FrameInfo = Rectangle.Empty;
        //        i.ObjectHandlesStatus = ObjectHandlesStatus.NONE;
        //        i.AlphaHue = 0;
        //        i.AllowedToDraw = true;
        //        i.ExecuteAnimation = true;
        //        i.HitsRequest = HitsRequestStatus.None;
        //    }
        //);

        private ushort? _displayedGraphic;
        private bool _isMulti;

        public Item(World world) : base(world, 0) { }

        public bool IsCoin => Graphic == 0x0EEA || Graphic == 0x0EED || Graphic == 0x0EF0;

        public ushort DisplayedGraphic
        {
            get
            {
                if (_displayedGraphic.HasValue)
                {
                    return _displayedGraphic.Value;
                }

                if (IsCoin)
                {
                    if (Amount > 5)
                    {
                        return (ushort)(Graphic + 2);
                    }

                    if (Amount > 1)
                    {
                        return (ushort)(Graphic + 1);
                    }
                }
                else if (IsMulti)
                {
                    return MultiGraphic;
                }

                return Graphic;
            }
            set => _displayedGraphic = value;
        }

        public bool IsLocked => (Flags & Flags.Movable) == 0 && ItemData.Weight > 90;

        public ushort MultiGraphic { get; private set; }

        public bool IsMulti
        {
            get => _isMulti;
            set
            {
                _isMulti = value;

                if (!value)
                {
                    MultiDistanceBonus = 0;
                    MultiInfo = null;
                }
            }
        }

        public int MultiDistanceBonus { get; private set; }

        public bool IsCorpse => /*MathHelper.InRange(Graphic, 0x0ECA, 0x0ED2) ||*/
            Graphic == 0x2006;

        public bool OnGround => !SerialHelper.IsValid(Container);

        public uint RootContainer
        {
            get
            {
                Item item = this;

                while (SerialHelper.IsItem(item.Container))
                {
                    item = World.Items.Get(item.Container);

                    if (item == null)
                    {
                        return 0;
                    }
                }

                return SerialHelper.IsMobile(item.Container) ? item.Container : item;
            }
        }

        public ref StaticTiles ItemData =>
            ref TileDataLoader.Instance.StaticData[IsMulti ? MultiGraphic : Graphic];

        public bool IsLootable =>
            ItemData.Layer != (int)Layer.Hair
            && ItemData.Layer != (int)Layer.Beard
            && ItemData.Layer != (int)Layer.Face
            && Graphic != 0;

        public ushort Amount;
        public uint Container = 0xFFFF_FFFF;

        public bool IsDamageable;
        public Layer Layer;
        public byte LightID;

        public Rectangle? MultiInfo;
        public bool Opened;

        public uint Price;
        public bool UsedLayer;
        public bool WantUpdateMulti = true;

        public static Item Create(World world, uint serial)
        {
            Item i = new Item(world); // _pool.GetOne();
            i.Serial = serial;

            return i;
        }

        public override void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            if (Opened)
            {
                UIManager.GetGump<ContainerGump>(Serial)?.Dispose();
                UIManager.GetGump<SpellbookGump>(Serial)?.Dispose();
                UIManager.GetGump<MapGump>(Serial)?.Dispose();

                if (IsCorpse)
                {
                    UIManager.GetGump<GridLootGump>(Serial)?.Dispose();
                }

                UIManager.GetGump<BulletinBoardGump>(Serial)?.Dispose();
                UIManager.GetGump<SplitMenuGump>(Serial)?.Dispose();

                Opened = false;
            }

            base.Destroy();

            //_pool.ReturnOne(this);
        }

        private unsafe void LoadMulti()
        {
            WantUpdateMulti = false;

            short minX = 0;
            short minY = 0;
            short maxX = 0;
            short maxY = 0;

            if (!World.HouseManager.TryGetHouse(Serial, out House house))
            {
                house = new House(World, Serial, 0, false);
                World.HouseManager.Add(Serial, house);
            }
            else
            {
                house.ClearComponents();
            }

            ref UOFileIndex entry = ref MultiLoader.Instance.GetValidRefEntry(Graphic);
            MultiLoader.Instance.File.SetData(entry.Address, entry.FileSize);
            bool movable = false;

            if (MultiLoader.Instance.IsUOP)
            {
                if (entry.Length > 0 && entry.DecompressedLength > 0)
                {
                    MultiLoader.Instance.File.Seek(entry.Offset);

                    byte[] buffer = null;
                    Span<byte> span =
                        entry.DecompressedLength <= 1024
                            ? stackalloc byte[entry.DecompressedLength]
                            : (
                                buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(
                                    entry.DecompressedLength
                                )
                            );

                    try
                    {
                        fixed (byte* dataPtr = span)
                        {
                            ZLib.Decompress(
                                MultiLoader.Instance.File.PositionAddress,
                                entry.Length,
                                0,
                                (IntPtr)dataPtr,
                                entry.DecompressedLength
                            );

                            StackDataReader reader = new StackDataReader(
                                span.Slice(0, entry.DecompressedLength)
                            );
                            reader.Skip(4);

                            int count = reader.ReadInt32LE();

                            int sizeOf = sizeof(MultiBlockNew);

                            for (int i = 0; i < count; i++)
                            {
                                MultiBlockNew* block = (MultiBlockNew*)(
                                    reader.PositionAddress + i * sizeOf
                                );

                                if (block->Unknown != 0)
                                {
                                    reader.Skip((int)(block->Unknown * 4));
                                }

                                if (block->X < minX)
                                {
                                    minX = block->X;
                                }

                                if (block->X > maxX)
                                {
                                    maxX = block->X;
                                }

                                if (block->Y < minY)
                                {
                                    minY = block->Y;
                                }

                                if (block->Y > maxY)
                                {
                                    maxY = block->Y;
                                }

                                if (block->Flags == 0 || block->Flags == 0x100)
                                {
                                    Multi m = Multi.Create(World, block->ID);
                                    m.MultiOffsetX = block->X;
                                    m.MultiOffsetY = block->Y;
                                    m.MultiOffsetZ = block->Z;
                                    m.Hue = Hue;
                                    m.AlphaHue = 255;
                                    m.IsCustom = false;
                                    m.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE;
                                    m.IsMovable = ItemData.IsMultiMovable;

                                    m.SetInWorldTile(
                                        (ushort)(X + block->X),
                                        (ushort)(Y + block->Y),
                                        (sbyte)(Z + block->Z)
                                    );

                                    house.Components.Add(m);

                                    if (m.ItemData.IsMultiMovable)
                                    {
                                        movable = true;
                                    }
                                }
                                else if (i == 0)
                                {
                                    MultiGraphic = block->ID;
                                }
                            }

                            reader.Release();
                        }
                    }
                    finally
                    {
                        if (buffer != null)
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
                else
                {
                    Log.Warn($"[MultiCollection.uop] invalid entry (0x{Graphic:X4})");
                }
            }
            else
            {
                int count = entry.Length / MultiLoader.Instance.Offset;
                MultiLoader.Instance.File.Seek(entry.Offset);

                for (int i = 0; i < count; i++)
                {
                    MultiBlock* block = (MultiBlock*)(
                        MultiLoader.Instance.File.PositionAddress + i * MultiLoader.Instance.Offset
                    );

                    if (block->X < minX)
                    {
                        minX = block->X;
                    }

                    if (block->X > maxX)
                    {
                        maxX = block->X;
                    }

                    if (block->Y < minY)
                    {
                        minY = block->Y;
                    }

                    if (block->Y > maxY)
                    {
                        maxY = block->Y;
                    }

                    if (block->Flags != 0)
                    {
                        Multi m = Multi.Create(World, block->ID);
                        m.MultiOffsetX = block->X;
                        m.MultiOffsetY = block->Y;
                        m.MultiOffsetZ = block->Z;
                        m.Hue = Hue;
                        m.AlphaHue = 255;
                        m.IsCustom = false;
                        m.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE;
                        m.IsMovable = ItemData.IsMultiMovable;

                        m.SetInWorldTile(
                            (ushort)(X + block->X),
                            (ushort)(Y + block->Y),
                            (sbyte)(Z + block->Z)
                        );

                        house.Components.Add(m);

                        if (m.ItemData.IsMultiMovable)
                        {
                            movable = true;
                        }
                    }
                    else if (i == 0)
                    {
                        MultiGraphic = block->ID;
                    }
                }
            }

            MultiInfo = new Rectangle
            {
                X = minX,
                Y = minY,
                Width = maxX,
                Height = maxY
            };

            // hack to make baots movable.
            // Mast is not the main center in bigger boats, so if we got a movable multi --> makes all multi movable
            if (movable)
            {
                foreach (Multi m in house.Components)
                {
                    m.IsMovable = movable;
                }
            }

            MultiDistanceBonus = Math.Max(
                Math.Max(Math.Abs(minX), maxX),
                Math.Max(Math.Abs(minY), maxY)
            );

            house.Bounds = MultiInfo.Value;

            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (World.HouseManager.EntityIntoHouse(Serial, World.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            World.BoatMovingManager.ClearSteps(Serial);
        }

        public override void CheckGraphicChange(byte animIndex = 0)
        {
            if (!IsMulti)
            {
                if (!IsCorpse)
                {
                    AllowedToDraw = CanBeDrawn(World, Graphic);
                }
                else
                {
                    AnimIndex = 99;

                    if ((Direction & Direction.Running) != 0)
                    {
                        UsedLayer = true;
                        Direction &= (Direction)0x7F;
                    }
                    else
                    {
                        UsedLayer = false;
                    }

                    Layer = (Layer)Direction;
                    AllowedToDraw = true;
                }
            }
            else if (WantUpdateMulti)
            {
                World.UoAssist.SignalAddMulti((ushort)(Graphic | 0x4000), X, Y);

                if (
                    MultiDistanceBonus == 0
                    || World.HouseManager.IsHouseInRange(Serial, World.ClientViewRange)
                )
                {
                    LoadMulti();
                    AllowedToDraw = MultiGraphic > 2;
                }
            }
        }

        public override void Update()
        {
            if (IsDestroyed)
            {
                return;
            }

            base.Update();

            ProcessAnimation();
        }

        public override ushort GetGraphicForAnimation()
        {
            var graphic = Graphic;

            if (Layer == Layer.Mount)
            {
                // ethereal unicorn
                if (graphic == 0x3E9B || graphic == 0x3E9D)
                {
                    return 0x00C0;
                }

                // ethereal kirin
                if (graphic == 0x3E9C)
                {
                    return 0x00BF;
                }

                if (Mounts.TryGet(graphic, out var mountInfo))
                {
                    graphic = mountInfo.Graphic;
                }
                else if (ItemData.AnimID != 0)
                {
                    graphic = ItemData.AnimID;
                }
            }
            else if (IsCorpse)
            {
                return Amount;
            }

            return graphic;
        }

        public override void UpdateTextCoordsV()
        {
            if (TextContainer == null)
            {
                return;
            }

            TextObject last = (TextObject)TextContainer.Items;

            while (last?.Next != null)
            {
                last = (TextObject)last.Next;
            }

            if (last == null)
            {
                return;
            }

            int offY = 0;

            if (OnGround)
            {
                Point p = RealScreenPosition;

                var bounds = Client.Game.UO.Arts.GetRealArtBounds(Graphic);
                p.Y -= bounds.Height >> 1;

                p.X += (int)Offset.X + 22;
                p.Y += (int)(Offset.Y - Offset.Z) + 22;

                p = Client.Game.Scene.Camera.WorldToScreen(p);

                for (; last != null; last = (TextObject)last.Previous)
                {
                    if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                    {
                        if (offY == 0 && last.Time < Time.Ticks)
                        {
                            continue;
                        }

                        last.OffsetY = offY;
                        offY += last.RenderedText.Height;

                        last.RealScreenPosition.X = p.X - (last.RenderedText.Width >> 1);
                        last.RealScreenPosition.Y = p.Y - offY;
                    }
                }

                FixTextCoordinatesInScreen();
            }
            else
            {
                for (; last != null; last = (TextObject)last.Previous)
                {
                    if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                    {
                        if (offY == 0 && last.Time < Time.Ticks)
                        {
                            continue;
                        }

                        last.OffsetY = offY;
                        offY += last.RenderedText.Height;

                        last.RealScreenPosition.X = last.X - (last.RenderedText.Width >> 1);
                        last.RealScreenPosition.Y = last.Y - offY;
                    }
                }
            }
        }

        public override void ProcessAnimation(bool evalutate = false)
        {
            if (IsCorpse)
            {
                var dir = (byte)Layer;

                if (LastAnimationChangeTime < Time.Ticks)
                {
                    byte frameIndex = (byte)(AnimIndex + (ExecuteAnimation ? 1 : 0));
                    ushort id = GetGraphicForAnimation();

                    bool mirror = false;
                    AnimationsLoader.Instance.GetAnimDirection(ref dir, ref mirror);

                    if (id < Client.Game.UO.Animations.MaxAnimationCount && dir < 5)
                    {
                        Client.Game.UO.Animations.ConvertBodyIfNeeded(ref id);
                        var animGroup = Client.Game.UO.Animations.GetAnimType(id);
                        var animFlags = Client.Game.UO.Animations.GetAnimFlags(id);
                        byte action = AnimationsLoader.Instance.GetDeathAction(
                            id,
                            animFlags,
                            animGroup,
                            UsedLayer
                        );
                        var frames = Client.Game.UO.Animations.GetAnimationFrames(
                            id,
                            action,
                            dir,
                            out _,
                            out _,
                            isCorpse: true
                        );

                        if (frames.Length > 0)
                        {
                            // when the animation is done, stop to animate the corpse
                            if (frameIndex >= frames.Length)
                            {
                                frameIndex = (byte)(frames.Length - 1);
                            }

                            AnimIndex = (byte)(frameIndex % frames.Length);
                        }
                    }

                    LastAnimationChangeTime = Time.Ticks + Constants.CHARACTER_ANIMATION_DELAY;
                }
            }
        }
    }
}
