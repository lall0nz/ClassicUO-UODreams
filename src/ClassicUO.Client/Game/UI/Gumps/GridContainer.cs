// SPDX-License-Identifier: BSD-2-Clause
// Grid container feature ported from Dust765 into vanilla ClassicUO.

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridContainer : ResizableGump
    {
        #region CONSTANTS
        private const int X_SPACING = 1, Y_SPACING = 1;
        private const int TOP_BAR_HEIGHT = 20;
        private const int TOP_BAR_ICON_SIZE = 20;
        private const int GRID_CONTENT_TOP_PADDING = 2;
        private const int GRID_CONTENT_BOTTOM_PADDING = 8;
        #endregion

        #region private static vars
        private static int lastX = 100, lastY = 100, lastCorpseX = 100, lastCorpseY = 100;
        private static int gridItemSize { get { return (int)Math.Round(50 * ((ProfileManager.CurrentProfile?.GridContainersScale ?? 100) / 100f)); } }
        private static int borderWidth = 4;
        #endregion

        #region private readonly vars
        private readonly AlphaBlendControl background;
        private readonly Label containerNameLabel;
        private readonly StbTextBox searchBox;
        private readonly GumpPic openRegularGump, sortContents;
        private readonly GridTopBarIcon quickDropBackpack;
        private readonly GumpPicTiled backgroundTexture;
        private readonly NiceButton setLootBag;
        private readonly bool isCorpse = false;
        #endregion

        #region private vars
        private Item container { get { return World.Items.Get(LocalSerial); } }
        private int _lastGridCellSize = -1;
        private int lastWidth = GetWidth(), lastHeight = GetHeight();
        private bool quickLootThisContainer = false;
        private bool? UseOldContainerStyle = null;
        private bool autoSortContainer = false;
        private bool skipSave = false;
        private readonly ushort originalContainerItemGraphic;
        private ScrollArea scrollArea;
        private GridSlotManager gridSlotManager;
        #endregion

        #region private tooltip vars
        private string quickLootStatus { get { return ProfileManager.CurrentProfile.CorpseSingleClickLoot ? "<basefont color=\"green\">Enabled" : "<basefont color=\"red\">Disabled"; } }
        private string quickLootTooltip
        {
            get
            {
                if (isCorpse)
                    return $"Drop an item here to send it to your backpack.<br><br>Click to enable/disable single-click looting for corpses.<br>   Currently {quickLootStatus}";
                else
                    return $"Drop an item here to send it to your backpack.<br><br>Click to enable/disable single-click loot for this container.<br>   Currently " + (quickLootThisContainer ? "<basefont color=\"green\">Enabled" : "<basefont color=\"red\">Disabled");
            }
        }
        private string sortButtonTooltip
        {
            get
            {
                string status = autoSortContainer ? "<basefont color=\"green\">Enabled" : "<basefont color=\"red\">Disabled";
                return $"Sort this container.<br>Alt + Click to enable auto sort<br>Auto sort currently {status}";
            }
        }
        #endregion

        #region internal vars
        internal readonly bool IsPlayerBackpack = false;
        internal GridSlotManager GetGridSlotManager { get { return gridSlotManager; } }
        internal List<Item> GetContents { get { return gridSlotManager.ContainerContents; } }
        internal bool SkipSave { get { return skipSave; } set { skipSave = value; } }
        #endregion

        internal GridContainer(World world, uint local, ushort originalContainerGraphic, bool? useGridStyle = null)
            : base(world, GetWidth(), GetHeight(), GetWidth(2), GetHeight(1), local, 0)
        {
            if (container == null)
            {
                Dispose();
                return;
            }

            #region SET VARS
            isCorpse = container.IsCorpse || container.Graphic == 0x0009;
            if (useGridStyle != null)
                UseOldContainerStyle = !useGridStyle;

            IsPlayerBackpack = World.Player.FindItemByLayer(Layer.Backpack)?.Serial == LocalSerial;

            autoSortContainer = GridSaveSystem.Instance.AutoSortContainer(LocalSerial);

            Point lastPos = GridSaveSystem.Instance.GetLastPosition(LocalSerial);
            Point savedSize = GridSaveSystem.Instance.GetLastSize(LocalSerial);

            if (IsPlayerBackpack && !GridSaveSystem.Instance.HasContainer(LocalSerial))
            {
                lastPos = ProfileManager.CurrentProfile.BackpackGridPosition;
                savedSize = ProfileManager.CurrentProfile.BackpackGridSize;
            }

            lastWidth = Width = Math.Max(savedSize.X, GetWidth(2));
            lastHeight = Height = Math.Max(savedSize.Y, GetHeight(1));

            X = isCorpse ? lastCorpseX : lastPos.X;
            Y = isCorpse ? lastCorpseY : lastPos.Y;

            if (isCorpse)
            {
                World.Player.ManualOpenedCorpses.Remove(LocalSerial);

                if (World.Player.AutoOpenedCorpses.Contains(LocalSerial)
                    && ProfileManager.CurrentProfile != null
                    && ProfileManager.CurrentProfile.SkipEmptyCorpse
                    && container.IsEmpty)
                {
                    IsVisible = false;
                    Dispose();
                    return;
                }
            }

            originalContainerItemGraphic = originalContainerGraphic;

            CanMove = true;
            AcceptMouseInput = true;
            CanBeLocked = true;
            #endregion

            #region background
            background = new AlphaBlendControl()
            {
                Width = Width - (borderWidth * 2),
                Height = Height - (borderWidth * 2),
                X = borderWidth,
                Y = borderWidth,
                Alpha = (float)ProfileManager.CurrentProfile.ContainerOpacity / 100,
                Hue = ProfileManager.CurrentProfile.Grid_UseContainerHue ? container.Hue : ProfileManager.CurrentProfile.AltGridContainerBackgroundHue
            };
            backgroundTexture = new GumpPicTiled(0);
            #endregion

            #region TOP BAR
            containerNameLabel = new Label(GetContainerName(), true, 0x0481, ishtml: true)
            {
                X = borderWidth,
                Y = -20
            };

            searchBox = new StbTextBox(0xFF, 20, 150, true, FontStyle.None, 0x0481)
            {
                X = borderWidth,
                Y = borderWidth,
                Multiline = false,
                Width = 150,
                Height = 20
            };
            searchBox.TextChanged += (sender, e) => { UpdateItems(); };

            var regularGumpIcon = Client.Game.UO.Gumps.GetGump(5839).Texture;
            openRegularGump = new GumpPic(background.Width - 25 - borderWidth, borderWidth, regularGumpIcon == null ? (ushort)1209 : (ushort)5839, 0);
            openRegularGump.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    UseOldContainerStyle = true;
                    GridSaveSystem.Instance.SetUseOriginalContainer(LocalSerial, true);
                    OpenOldContainer(LocalSerial);
                }
            };
            openRegularGump.MouseEnter += (sender, e) => { openRegularGump.Graphic = regularGumpIcon == null ? (ushort)1210 : (ushort)5840; };
            openRegularGump.MouseExit += (sender, e) => { openRegularGump.Graphic = regularGumpIcon == null ? (ushort)1209 : (ushort)5839; };
            openRegularGump.SetTooltip("Open the original style container.\n\nCtrl+Click to lock item | Alt+Click quick move");

            Item backpack = World.Player.FindItemByLayer(Layer.Backpack);
            quickDropBackpack = new GridTopBarIcon(backpack?.DisplayedGraphic ?? 0x0E75, TOP_BAR_ICON_SIZE)
            {
                X = borderWidth,
                Y = borderWidth
            };
            quickDropBackpack.Hit.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                    {
                        Item bp = World.Player.FindItemByLayer(Layer.Backpack);
                        if (bp != null)
                            GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, bp.Serial);
                    }
                    else if (isCorpse)
                    {
                        ProfileManager.CurrentProfile.CorpseSingleClickLoot ^= true;
                        quickDropBackpack.SetTooltip(quickLootTooltip);
                    }
                    else
                    {
                        quickLootThisContainer ^= true;
                        quickDropBackpack.SetTooltip(quickLootTooltip);
                    }
                }
            };
            quickDropBackpack.Hit.MouseEnter += (sender, e) => { quickDropBackpack.Hue = 0x34; };
            quickDropBackpack.Hit.MouseExit += (sender, e) => { quickDropBackpack.Hue = 0; };
            quickDropBackpack.SetTooltip(quickLootTooltip);

            sortContents = new GumpPic(borderWidth, borderWidth, 1210, 0);
            sortContents.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && Keyboard.Alt)
                {
                    autoSortContainer ^= true;
                    sortContents.SetTooltip(sortButtonTooltip);
                }
                UpdateItems(true);
            };
            sortContents.MouseEnter += (sender, e) => { sortContents.Graphic = 1209; };
            sortContents.MouseExit += (sender, e) => { sortContents.Graphic = 1210; };
            sortContents.SetTooltip(sortButtonTooltip);
            #endregion

            #region Scroll Area
            scrollArea = new ScrollArea(background.X, 0, background.Width, 1, true);
            scrollArea.MouseUp += ScrollArea_MouseUp;
            UpdateScrollAreaLayout();
            #endregion

            #region Set loot bag
            setLootBag = new NiceButton(0, Height - 20, 100, 20, ButtonAction.Default, "Set loot bag") { IsSelectable = false };
            setLootBag.IsVisible = isCorpse;
            setLootBag.SetTooltip("For double click looting only");
            setLootBag.MouseUp += (s, e) =>
            {
                GameActions.Print(World, ResGumps.TargetContainerToGrabItemsInto);
                World.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            };
            #endregion

            #region Add controls
            Add(background);
            Add(backgroundTexture);
            Add(containerNameLabel);
            searchBox.Add(new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                Width = searchBox.Width,
                Height = searchBox.Height
            });
            Add(searchBox);
            Add(openRegularGump);
            Add(quickDropBackpack);
            Add(sortContents);
            Add(scrollArea);
            Add(setLootBag);
            #endregion

            AlwaysShowResizeHandle = true;

            gridSlotManager = new GridSlotManager(World, LocalSerial, this, scrollArea);

            if (GridSaveSystem.Instance.UseOriginalContainerGump(LocalSerial) && (UseOldContainerStyle == null || UseOldContainerStyle == true))
            {
                skipSave = true;
                OpenOldContainer(local);
                return;
            }

            BuildBorder();
            UpdateTopBarIconPositions();
            ResizeWindow(savedSize);
            OnResize();
            BringResizeButtonToFront();
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            PersistLayout();
        }

        private void PersistLayout()
        {
            if (skipSave || isCorpse || gridSlotManager == null)
            {
                return;
            }

            GridSaveSystem.Instance.SaveContainer(
                LocalSerial,
                gridSlotManager.GridSlots,
                Width,
                Height,
                X,
                Y,
                UseOldContainerStyle,
                autoSortContainer
            );

            if (IsPlayerBackpack && ProfileManager.CurrentProfile != null)
            {
                ProfileManager.CurrentProfile.BackpackGridPosition = new Point(X, Y);
                ProfileManager.CurrentProfile.BackpackGridSize = new Point(Width, Height);
            }
        }

        public override GumpType GumpType => GumpType.GridContainer;

        /// <summary>
        /// Padlock sits to the right of the container title ("Backpack") above the top bar.
        /// </summary>
        protected override Point GetLockIconPosition(int iconWidth, int iconHeight)
        {
            if (containerNameLabel == null)
            {
                return base.GetLockIconPosition(iconWidth, iconHeight);
            }

            int x = containerNameLabel.X + Math.Max(containerNameLabel.Width, 8) + 4;
            int y = containerNameLabel.Y + Math.Max(0, (containerNameLabel.Height - iconHeight) / 2);

            return new Point(x, y);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (TryToggleLock(x, y, button))
            {
                return;
            }

            base.OnMouseUp(x, y, button);
        }

        public override void OnResize()
        {
            base.OnResize();

            if (IsDisposed || gridSlotManager == null || background == null || scrollArea == null)
            {
                return;
            }

            lastWidth = Width;
            lastHeight = Height;
            background.Width = Width - (borderWidth * 2);
            background.Height = Height - (borderWidth * 2);
            UpdateScrollAreaLayout();
            UpdateTopBarIconPositions();
            setLootBag.Y = Height - 20;
            BuildBorder();
            UpdateItems();
            PersistLayout();
        }

        private static int GetWidth(int columns = -1)
        {
            if (columns < 0)
                columns = ProfileManager.CurrentProfile.Grid_DefaultColumns;
            return (borderWidth * 2) + 15 + (gridItemSize * columns) + (X_SPACING * columns);
        }

        private static int GetHeight(int rows = -1)
        {
            if (rows < 0)
                rows = ProfileManager.CurrentProfile.Grid_DefaultRows;
            return TOP_BAR_HEIGHT + (borderWidth * 2) + ((gridItemSize + Y_SPACING) * rows);
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            PersistLayout();
            writer.WriteAttributeString("ogContainer", originalContainerItemGraphic.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            if (
                int.TryParse(xml.GetAttribute("x"), out int savedX)
                && int.TryParse(xml.GetAttribute("y"), out int savedY)
            )
            {
                GridSaveSystem.Instance.SaveContainerLayout(
                    LocalSerial,
                    Width > 0 ? Width : GetWidth(),
                    Height > 0 ? Height : GetHeight(),
                    savedX,
                    savedY,
                    UseOldContainerStyle,
                    autoSortContainer
                );

                Item backpack = World.Player?.FindItemByLayer(Layer.Backpack);

                if (backpack != null && backpack.Serial == LocalSerial)
                {
                    ProfileManager.CurrentProfile.BackpackGridPosition = new Point(savedX, savedY);
                }
            }

            base.Restore(xml);
            GameActions.DoubleClickQueued(LocalSerial);
            Dispose();
        }

        private void ScrollArea_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && scrollArea.MouseIsOver)
            {
                if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                    GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, LocalSerial);
                else if (World.TargetManager.IsTargeting)
                    World.TargetManager.Target(LocalSerial);
            }
            else if (e.Button == MouseButtonType.Right)
            {
                InvokeMouseCloseGumpWithRClick();
            }
        }

        private void OpenOldContainer(uint serial)
        {
            UIManager.GetGump<ContainerGump>(serial)?.Dispose();

            ushort graphic = originalContainerItemGraphic;
            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_706000 && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.UseLargeContainerGumps)
            {
                switch (graphic)
                {
                    case 0x0048: if (Client.Game.UO.Gumps.GetGump(0x06E8).Texture != null) graphic = 0x06E8; break;
                    case 0x0049: if (Client.Game.UO.Gumps.GetGump(0x9CDF).Texture != null) graphic = 0x9CDF; break;
                    case 0x0051: if (Client.Game.UO.Gumps.GetGump(0x06E7).Texture != null) graphic = 0x06E7; break;
                    case 0x003E: if (Client.Game.UO.Gumps.GetGump(0x06E9).Texture != null) graphic = 0x06E9; break;
                    case 0x004D: if (Client.Game.UO.Gumps.GetGump(0x06EA).Texture != null) graphic = 0x06EA; break;
                    case 0x004E: if (Client.Game.UO.Gumps.GetGump(0x06E6).Texture != null) graphic = 0x06E6; break;
                    case 0x004F: if (Client.Game.UO.Gumps.GetGump(0x06E5).Texture != null) graphic = 0x06E5; break;
                    case 0x004A: if (Client.Game.UO.Gumps.GetGump(0x9CDD).Texture != null) graphic = 0x9CDD; break;
                    case 0x0044: if (Client.Game.UO.Gumps.GetGump(0x9CE3).Texture != null) graphic = 0x9CE3; break;
                }
            }

            World.ContainerManager.CalculateContainerPosition(serial, graphic);

            UIManager.Add(new ContainerGump(World, container.Serial, graphic, true)
            {
                X = World.ContainerManager.X,
                Y = World.ContainerManager.Y,
                InvalidateContents = true
            });
            Dispose();
        }

        private void UpdateItems(bool overrideSort = false)
        {
            if (container == null)
            {
                Dispose();
                return;
            }
            UpdateContainerName();

            if (autoSortContainer) overrideSort = true;

            string searchText = searchBox.Text?.Trim() ?? "";
            List<Item> sortedContents = (ProfileManager.CurrentProfile is null || ProfileManager.CurrentProfile.GridContainerSearchMode == 0)
                ? gridSlotManager.SearchResults(searchText)
                : GridSlotManager.GetItemsInContainer(container);
            gridSlotManager.RebuildContainer(sortedContents, searchText, overrideSort);

            InvalidateContents = false;
        }

        internal static bool FindContainer(uint serial, out GridContainer gridContainer) => (gridContainer = UIManager.GetGump<GridContainer>(serial)) != null;

        internal static void SetContainerGridStyle(uint serial, bool useOriginalContainer)
        {
            GridSaveSystem.Instance.SetUseOriginalContainer(serial, useOriginalContainer);
        }

        internal static void OpenGridView(World world, uint serial, ushort originalContainerGraphic)
        {
            SetContainerGridStyle(serial, false);
            UIManager.GetGump<ContainerGump>(serial)?.Dispose();
            UIManager.Add(new GridContainer(world, serial, originalContainerGraphic, useGridStyle: true));
        }

        protected override void UpdateContents()
        {
            if (InvalidateContents && !IsDisposed && IsVisible)
                UpdateItems();
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (isCorpse && container != null && container == SelectedObject.CorpseObject)
                SelectedObject.CorpseObject = null;
        }

        public override void Dispose()
        {
            if (isCorpse) { lastCorpseX = X; lastCorpseY = Y; }
            else { lastX = X; lastY = Y; }

            Item _c = container;
            if (_c != null)
            {
                if (_c == SelectedObject.CorpseObject)
                    SelectedObject.CorpseObject = null;

                Item bank = World.Player.FindItemByLayer(Layer.Bank);
                if (bank != null && (_c.Serial == bank.Serial || _c.Container == bank.Serial))
                {
                    for (LinkedObject i = _c.Items; i != null; i = i.Next)
                    {
                        Item child = (Item)i;
                        if (child.Container == _c)
                        {
                            UIManager.GetGump<GridContainer>(child)?.Dispose();
                            UIManager.GetGump<ContainerGump>(child)?.Dispose();
                        }
                    }
                }
            }

            if (gridSlotManager != null && !skipSave && !isCorpse)
            {
                PersistLayout();
            }

            base.Dispose();
        }

        public override void Update()
        {
            base.Update();
            if (IsDisposed) return;

            if (container == null || container.IsDestroyed)
            {
                Dispose();
                return;
            }

            if (isCorpse && container.Distance > 3)
            {
                Dispose();
                return;
            }

            int cellNow = gridItemSize;
            if (_lastGridCellSize != cellNow)
            {
                _lastGridCellSize = cellNow;
                UpdateItems();
            }

            if (gridSlotManager?.ClearStaleSlots() == true)
            {
                UpdateItems();
            }
        }

        private string GetContainerName()
        {
            if (container == null) return string.Empty;
            return World.OPL.TryGetNameAndData(container.Serial, out string name, out _) ? name : (container.Name ?? "Container");
        }

        private void UpdateContainerName()
        {
            if (containerNameLabel != null)
                containerNameLabel.Text = GetContainerName();
        }

        private void UpdateTopBarIconPositions()
        {
            openRegularGump.X = background.Width - openRegularGump.Width - borderWidth;
            quickDropBackpack.X = openRegularGump.X - TOP_BAR_ICON_SIZE;
            sortContents.X = quickDropBackpack.X - TOP_BAR_ICON_SIZE;
        }

        private void UpdateScrollAreaLayout()
        {
            if (scrollArea == null || background == null || searchBox == null)
            {
                return;
            }

            scrollArea.X = background.X;
            scrollArea.Y = searchBox.Y + searchBox.Height + GRID_CONTENT_TOP_PADDING;
            scrollArea.Width = background.Width;
            scrollArea.Height = background.Y + background.Height - scrollArea.Y - GRID_CONTENT_BOTTOM_PADDING;
        }

        internal void RefreshBorderOptions()
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile != null && background != null)
            {
                background.Alpha = (float)profile.ContainerOpacity / 100;
                background.Hue = profile.Grid_UseContainerHue && container != null
                    ? container.Hue
                    : profile.AltGridContainerBackgroundHue;
            }

            BuildBorder();
        }

        private void BuildBorder()
        {
            Profile profile = ProfileManager.CurrentProfile;
            ShowBorder = profile == null || !profile.Grid_HideBorder;
            BorderHue = profile?.GridContainerBorderHue ?? 946;
        }

        internal class GridSlotManager
        {
            private Dictionary<int, GridItem> gridSlots = new Dictionary<int, GridItem>();
            private Item container;
            internal List<Item> containerContents;
            private int amount = 125;
            private Control area;
            private Dictionary<int, uint> itemPositions = new Dictionary<int, uint>();
            private List<uint> itemLocks = new List<uint>();
            private World world;

            internal Dictionary<int, GridItem> GridSlots { get { return gridSlots; } }
            internal List<Item> ContainerContents { get { return containerContents; } }
            internal Dictionary<int, uint> ItemPositions { get { return itemPositions; } }

            internal GridSlotManager(World _world, uint thisContainer, GridContainer gridContainer, Control controlArea)
            {
                world = _world;
                area = controlArea;
                foreach (GridSaveSystem.GridItemSlotSaveData item in GridSaveSystem.Instance.GetItemSlots(thisContainer))
                {
                    ItemPositions.Add(item.Slot, item.Serial);
                    if (item.IsLocked)
                        itemLocks.Add(item.Serial);
                }
                container = world.Items.Get(thisContainer);
                UpdateItems();
                if (containerContents.Count > 125)
                    amount = containerContents.Count;

                for (int i = 0; i < amount; i++)
                {
                    GridItem GI = new GridItem(0, gridItemSize, container, gridContainer, i);
                    gridSlots.Add(i, GI);
                    area.Add(GI);
                }
            }

            internal void AddLockedItemSlot(uint serial, int specificSlot)
            {
                int removeSlot = -1;
                foreach (KeyValuePair<int, uint> kv in ItemPositions)
                {
                    if (kv.Value == serial) { removeSlot = kv.Key; break; }
                }
                if (removeSlot >= 0) ItemPositions.Remove(removeSlot);
                if (ItemPositions.ContainsKey(specificSlot)) ItemPositions.Remove(specificSlot);
                ItemPositions.Add(specificSlot, serial);
            }

            internal GridItem FindItem(uint serial)
            {
                foreach (var slot in gridSlots)
                    if (slot.Value.LocalSerial == serial)
                        return slot.Value;
                return null;
            }

            internal void RebuildContainer(List<Item> filteredItems, string searchText = "", bool overrideSort = false)
            {
                foreach (var slot in gridSlots)
                    slot.Value.SetGridItem(null);

                // Clean up stale itemPositions entries for items no longer in this container
                List<int> staleSlots = null;
                foreach (var spot in itemPositions)
                {
                    Item i = world.Items.Get(spot.Value);
                    if (i == null || i.Container != container.Serial)
                    {
                        staleSlots ??= new List<int>();
                        staleSlots.Add(spot.Key);
                    }
                }
                if (staleSlots != null)
                {
                    foreach (int s in staleSlots)
                    {
                        if (itemPositions.TryGetValue(s, out uint staleSerial))
                        {
                            itemLocks.Remove(staleSerial);
                            itemPositions.Remove(s);
                        }
                    }
                }

                CompactUnlockedItemPositions();

                foreach (var spot in itemPositions)
                {
                    Item i = world.Items.Get(spot.Value);
                    if (i != null && filteredItems.Contains(i) && (!overrideSort || itemLocks.Contains(spot.Value)))
                    {
                        if (spot.Key < gridSlots.Count)
                        {
                            gridSlots[spot.Key].SetGridItem(i);
                            if (itemLocks.Contains(spot.Value))
                                gridSlots[spot.Key].ItemGridLocked = true;
                            filteredItems.Remove(i);
                        }
                    }
                }

                foreach (Item i in filteredItems)
                {
                    foreach (var slot in gridSlots)
                    {
                        if (slot.Value.SlotItem != null) continue;
                        slot.Value.SetGridItem(i);
                        AddLockedItemSlot(i.Serial, slot.Key);
                        break;
                    }
                }

                bool hideMode = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GridContainerSearchMode == 0;
                bool highlightMode = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GridContainerSearchMode == 1;
                string searchTermLower = (searchText ?? "").Trim().ToLowerInvariant();
                bool hasSearch = !string.IsNullOrEmpty(searchTermLower);
                foreach (var slot in gridSlots)
                {
                    if (slot.Value.SlotItem != null)
                    {
                        slot.Value.IsVisible = !hasSearch || !hideMode;
                        if (hasSearch)
                        {
                            if (SearchItemNameAndProps(searchTermLower, slot.Value.SlotItem))
                            {
                                slot.Value.Hightlight = highlightMode;
                                slot.Value.IsVisible = true;
                            }
                        }
                    }
                    else
                    {
                        slot.Value.Hightlight = false;
                        // Empty cells only participate in search layouts; compact reflow hides holes.
                        slot.Value.IsVisible = hasSearch && !hideMode;
                    }
                }

                SetGridPositions();
            }

            private void CompactUnlockedItemPositions()
            {
                List<uint> unlockedSerials = new List<uint>();
                List<int> unlockedSlots = new List<int>();

                foreach (KeyValuePair<int, uint> spot in itemPositions)
                {
                    if (itemLocks.Contains(spot.Value))
                    {
                        continue;
                    }

                    unlockedSerials.Add(spot.Value);
                    unlockedSlots.Add(spot.Key);
                }

                if (unlockedSerials.Count == 0)
                {
                    return;
                }

                foreach (int slot in unlockedSlots)
                {
                    itemPositions.Remove(slot);
                }

                int nextSlot = 0;

                foreach (KeyValuePair<int, uint> spot in itemPositions)
                {
                    if (spot.Key >= nextSlot)
                    {
                        nextSlot = spot.Key + 1;
                    }
                }

                foreach (uint serial in unlockedSerials)
                {
                    while (itemPositions.ContainsKey(nextSlot))
                    {
                        nextSlot++;
                    }

                    itemPositions[nextSlot] = serial;
                    nextSlot++;
                }
            }

            internal void SetLockedSlot(int slot, bool locked)
            {
                if (gridSlots[slot].SlotItem == null) return;
                gridSlots[slot].ItemGridLocked = locked;
                if (!locked) itemLocks.Remove(gridSlots[slot].SlotItem.Serial);
                else itemLocks.Add(gridSlots[slot].SlotItem.Serial);
            }

            internal void SetGridPositions()
            {
                int x = X_SPACING, y = 0;

                foreach (KeyValuePair<int, GridItem> slot in gridSlots)
                {
                    if (!slot.Value.IsVisible)
                    {
                        continue;
                    }

                    if (slot.Value.SlotItem == null)
                    {
                        // Empty slots must not keep stale coordinates between compacted items.
                        slot.Value.X = -gridItemSize;
                        slot.Value.Y = -gridItemSize;
                        continue;
                    }

                    if (x + gridItemSize >= area.Width - 14)
                    {
                        x = X_SPACING;
                        y += gridItemSize + Y_SPACING;
                    }

                    slot.Value.X = x;
                    slot.Value.Y = y;
                    slot.Value.Resize();
                    x += gridItemSize + X_SPACING;
                }
            }

            internal List<Item> SearchResults(string search)
            {
                UpdateItems();
                if (string.IsNullOrWhiteSpace(search)) return containerContents;
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GridContainerSearchMode != 0)
                    return containerContents;
                List<Item> filteredContents = new List<Item>();
                string term = search.Trim().ToLowerInvariant();
                foreach (Item i in containerContents)
                {
                    if (SearchItemNameAndProps(term, i))
                        filteredContents.Add(i);
                }
                return filteredContents;
            }

            private bool SearchItemNameAndProps(string searchTermLower, Item item)
            {
                if (item == null || string.IsNullOrEmpty(searchTermLower)) return false;
                if (world.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
                {
                    if (name != null && name.ToLowerInvariant().Contains(searchTermLower)) return true;
                    if (data != null && data.ToLowerInvariant().Contains(searchTermLower)) return true;
                }
                else
                {
                    if (item.Name != null && item.Name.ToLowerInvariant().Contains(searchTermLower)) return true;
                    if (item.ItemData.Name != null && item.ItemData.Name.ToLowerInvariant().Contains(searchTermLower)) return true;
                }
                return false;
            }

            internal void UpdateItems()
            {
                containerContents = GetItemsInContainer(container);
            }

            internal bool ClearStaleSlots()
            {
                bool changed = false;

                foreach (var slot in gridSlots)
                {
                    if (slot.Value.ClearIfStale())
                    {
                        changed = true;
                    }
                }

                if (changed)
                {
                    SetGridPositions();
                }

                return changed;
            }

            internal void RemoveItemPosition(uint serial)
            {
                int removeSlot = -1;

                foreach (KeyValuePair<int, uint> kv in itemPositions)
                {
                    if (kv.Value == serial)
                    {
                        removeSlot = kv.Key;
                        break;
                    }
                }

                if (removeSlot >= 0)
                {
                    itemPositions.Remove(removeSlot);
                    itemLocks.Remove(serial);
                }
            }

            internal static List<Item> GetItemsInContainer(Item _container)
            {
                List<Item> contents = new List<Item>();
                for (LinkedObject i = _container.Items; i != null; i = i.Next)
                {
                    Item item = (Item)i;
                    var layer = (Layer)item.ItemData.Layer;
                    if (_container.IsCorpse && item.Layer > 0 && !Constants.BAD_CONTAINER_LAYERS[(int)layer]) continue;
                    if (item.ItemData.IsWearable && (layer == Layer.Face || layer == Layer.Beard || layer == Layer.Hair)) continue;
                    contents.Add(item);
                }
                contents.Sort(static (a, b) => { int g = a.Graphic.CompareTo(b.Graphic); return g != 0 ? g : a.Hue.CompareTo(b.Hue); });
                return contents;
            }
        }

        internal class GridItem : Control
        {
            private Item _item;
            private readonly HitBox hit;
            private Label count;
            private GridContainer gridContainer;
            private Item container;
            private int slot;
            private bool mousePressedWhenEntered = false;
            private GridContainerPreview preview;

            internal bool Hightlight = false;
            internal bool ItemGridLocked = false;

            internal Item SlotItem { get { return _item; } }

            internal GridItem(uint serial, int size, Item _container, GridContainer _gridContainer, int _slot)
            {
                container = _container;
                gridContainer = _gridContainer;
                slot = _slot;
                LocalSerial = serial;

                Width = size;
                Height = size;
                WantUpdateSize = false;
                AcceptMouseInput = true;
                CanMove = false;

                hit = new HitBox(0, 0, size, size, null, 0f);
                Add(hit);

                hit.MouseUp += _hit_MouseUp;
                hit.MouseDoubleClick += _hit_MouseDoubleClick;
                hit.MouseEnter += _hit_MouseEnter;
                hit.MouseExit += _hit_MouseExit;
            }

            internal void SetGridItem(Item item)
            {
                if (item == null)
                {
                    _item = null;
                    LocalSerial = 0;
                    hit.ClearTooltip();
                    Hightlight = false;
                    count?.Dispose();
                    count = null;
                    ItemGridLocked = false;
                }
                else
                {
                    _item = item;
                    LocalSerial = item.Serial;
                    int itemAmt = (_item.ItemData.IsStackable ? _item.Amount : 1);
                    if (itemAmt > 1)
                    {
                        count?.Dispose();
                        count = new Label(itemAmt.ToString(), true, 0x0481, align: TEXT_ALIGN_TYPE.TS_LEFT);
                        count.X = 1;
                        count.Y = Height - count.Height;
                    }
                    else
                    {
                        count?.Dispose();
                        count = null;
                    }
                    hit.SetTooltip(_item);
                }
            }

            internal bool ClearIfStale()
            {
                if (_item == null)
                {
                    return false;
                }

                if (_item.IsDestroyed || container == null || container.IsDestroyed)
                {
                    gridContainer.gridSlotManager.RemoveItemPosition(LocalSerial);
                    SetGridItem(null);
                    return true;
                }

                Item current = gridContainer.World.Items.Get(LocalSerial);

                if (current == null || current.IsDestroyed || current.Container != container.Serial)
                {
                    gridContainer.gridSlotManager.RemoveItemPosition(LocalSerial);
                    SetGridItem(null);
                    return true;
                }

                return false;
            }

            private bool TryGetValidItem(out Item item)
            {
                item = null;

                if (_item == null || _item.IsDestroyed || container == null || container.IsDestroyed)
                {
                    return false;
                }

                item = gridContainer.World.Items.Get(LocalSerial);

                if (item == null || item.IsDestroyed || item.Container != container.Serial)
                {
                    return false;
                }

                return true;
            }

            internal void Resize()
            {
                int size = gridItemSize;
                Width = size; Height = size;
                hit.Width = size; hit.Height = size;
                if (count != null) count.Y = Height - count.Height;
            }

            private void _hit_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
            {
                ClearIfStale();

                if (e.Button != MouseButtonType.Left || gridContainer.World.TargetManager.IsTargeting || _item == null)
                    return;

                if (!Keyboard.Ctrl && ProfileManager.CurrentProfile.DoubleClickToLootInsideContainers && gridContainer.isCorpse
                    && !_item.IsDestroyed && !_item.ItemData.IsContainer
                    && container != gridContainer.World.Player.FindItemByLayer(Layer.Backpack)
                    && !_item.IsLocked && _item.IsLootable)
                {
                    GameActions.GrabItem(gridContainer.World, _item.Serial, _item.Amount);
                }
                else
                {
                    GameActions.DoubleClick(gridContainer.World, LocalSerial);
                }
                e.Result = true;
            }

            private void _hit_MouseUp(object sender, MouseEventArgs e)
            {
                ClearIfStale();

                if (e.Button == MouseButtonType.Left)
                {
                    if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                    {
                        if (_item != null && _item.ItemData.IsContainer)
                        {
                            GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _item.Serial);
                            Mouse.CancelDoubleClick = true;
                        }
                        else if (_item != null && _item.ItemData.IsStackable && _item.Graphic == Client.Game.UO.GameCursor.ItemHold.Graphic)
                        {
                            GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, _item.X, _item.Y, 0, _item.Serial);
                            Mouse.CancelDoubleClick = true;
                        }
                        else
                        {
                            Rectangle containerBounds = gridContainer.World.ContainerManager.Get(container.Graphic).Bounds;
                            gridContainer.gridSlotManager.AddLockedItemSlot(Client.Game.UO.GameCursor.ItemHold.Serial, slot);
                            GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, containerBounds.Width / 2, containerBounds.Height / 2, 0, container.Serial);
                            Mouse.CancelDoubleClick = true;
                        }
                    }
                    else if (gridContainer.World.TargetManager.IsTargeting)
                    {
                        if (_item != null)
                        {
                            gridContainer.World.TargetManager.Target(_item.Serial);
                            if (gridContainer.World.TargetManager.TargetingState == CursorTarget.SetTargetClientSide)
                                UIManager.Add(new InspectorGump(gridContainer.World, _item));
                        }
                        else
                            gridContainer.World.TargetManager.Target(container.Serial);
                        Mouse.CancelDoubleClick = true;
                    }
                    else if (Keyboard.Ctrl)
                    {
                        gridContainer.gridSlotManager.SetLockedSlot(slot, !ItemGridLocked);
                        Mouse.CancelDoubleClick = true;
                    }
                    else if (_item != null)
                    {
                        Point offset = Mouse.LDragOffset;
                        if (Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS && Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                        {
                            if ((gridContainer.isCorpse && ProfileManager.CurrentProfile.CorpseSingleClickLoot) || gridContainer.quickLootThisContainer)
                            {
                                GameActions.GrabItem(gridContainer.World, _item.Serial, _item.Amount);
                                Mouse.CancelDoubleClick = true;
                            }
                            else
                            {
                                if (gridContainer.World.ClientFeatures.TooltipsEnabled)
                                    gridContainer.World.DelayedObjectClickManager.Set(_item.Serial, gridContainer.X, gridContainer.Y - 80, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                                else
                                    GameActions.SingleClick(gridContainer.World, _item.Serial);
                            }
                        }
                    }
                }
            }

            private void _hit_MouseExit(object sender, MouseEventArgs e)
            {
                if (Mouse.LButtonPressed && !mousePressedWhenEntered)
                {
                    Point offset = Mouse.LDragOffset;
                    if (Math.Abs(offset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                    {
                        if (_item != null)
                        {
                            if (!Keyboard.Alt)
                                GameActions.PickUp(gridContainer.World, _item.Serial, e.X, e.Y);
                        }
                        else
                        {
                            if (ProfileManager.CurrentProfile.HoldAltToMoveGumps)
                            {
                                if (Keyboard.Alt)
                                    UIManager.AttemptDragControl(gridContainer);
                            }
                            else
                                UIManager.AttemptDragControl(gridContainer);
                        }
                    }
                }

                GridContainerPreview g;
                while ((g = UIManager.GetGump<GridContainerPreview>()) != null)
                    g.Dispose();
            }

            private void _hit_MouseEnter(object sender, MouseEventArgs e)
            {
                ClearIfStale();

                SelectedObject.Object = gridContainer.World.Get(LocalSerial);
                mousePressedWhenEntered = Mouse.LButtonPressed;

                if (_item != null)
                {
                    if (_item.ItemData.IsContainer && _item.Items != null && ProfileManager.CurrentProfile.GridEnableContPreview)
                    {
                        preview = new GridContainerPreview(gridContainer.World, _item.Serial, Mouse.Position.X, Mouse.Position.Y);
                        UIManager.Add(preview);
                    }

                    if (!hit.HasTooltip)
                        hit.SetTooltip(_item);
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (!TryGetValidItem(out Item item))
                {
                    item = null;
                }

                base.Draw(batcher, x, y);

                if (item != null)
                {
                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(item.DisplayedGraphic);
                    var texture = artInfo.Texture;

                    if (texture != null)
                    {
                        int artW = artInfo.UV.Width;
                        int artH = artInfo.UV.Height;

                        Point originalSize = new Point(hit.Width, hit.Height);
                        Point point = new Point();

                        if (artW < hit.Width)
                        {
                            originalSize.X = artW;
                            point.X = (hit.Width >> 1) - (originalSize.X >> 1);
                        }
                        else if (artW > hit.Width)
                        {
                            originalSize.X = hit.Width;
                            point.X = 0;
                        }

                        if (artH < hit.Height)
                        {
                            originalSize.Y = artH;
                            point.Y = (hit.Height >> 1) - (originalSize.Y >> 1);
                        }
                        else if (artH > hit.Height)
                        {
                            originalSize.Y = hit.Height;
                            point.Y = 0;
                        }

                        Vector3 hueVector = ShaderHueTranslator.GetHueVector(item.Hue, item.ItemData.IsPartialHue, 1f);

                        batcher.Draw(
                            texture,
                            new Rectangle(x + point.X, y + point.Y + hit.Y, originalSize.X, originalSize.Y),
                            artInfo.UV,
                            hueVector
                        );

                        if (count != null && !count.IsDisposed)
                        {
                            count.Draw(batcher, x + count.X, y + count.Y);
                        }
                    }
                }

                Profile borderProfile = ProfileManager.CurrentProfile;
                ushort borderHue = borderProfile?.GridBorderHue ?? 946;
                float borderAlpha = (borderProfile?.GridBorderAlpha ?? 35) / 100f;
                if (ItemGridLocked)
                {
                    borderHue = 0x2;
                }

                if (Hightlight)
                {
                    borderHue = 0x34;
                    borderAlpha = 1f;
                }

                if (item != null)
                {
                    Vector3 borderHueVector = ShaderHueTranslator.GetHueVector(borderHue, false, borderAlpha);

                    batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.White), x, y, Width, Height, borderHueVector);

                    if (hit.MouseIsOver)
                    {
                        Vector3 hov = ShaderHueTranslator.GetHueVector(borderHue, false, 0.3f);
                        batcher.Draw(SolidColorTextureCache.GetTexture(Color.White), new Rectangle(x + 1, y, Width - 1, Height), hov);
                    }
                }

                return true;
            }
        }

        private class GridContainerPreview : Gump
        {
            private readonly AlphaBlendControl _background;
            private readonly Item _container;

            private const int WIDTH = 170;
            private const int HEIGHT = 150;
            private const int GRIDSIZE = 50;

            internal GridContainerPreview(World world, uint serial, int x, int y) : base(world, serial, 0)
            {
                _container = world.Items.Get(serial);
                if (_container == null) { Dispose(); return; }

                X = x - WIDTH - 20;
                Y = y - HEIGHT - 20;
                _background = new AlphaBlendControl() { Width = WIDTH, Height = HEIGHT };

                CanCloseWithRightClick = true;
                Add(_background);
                InvalidateContents = true;
            }

            protected override void UpdateContents()
            {
                base.UpdateContents();
                if (InvalidateContents && !IsDisposed && IsVisible && _container?.Items != null)
                {
                    int currentCount = 0, lastX = 0, lastY = 0;
                    for (LinkedObject i = _container.Items; i != null; i = i.Next)
                    {
                        Item item = (Item)i;
                        if (item == null || currentCount > 8) break;

                        StaticPic gridItem = new StaticPic(item.DisplayedGraphic, item.Hue);
                        gridItem.X = lastX;
                        if (gridItem.X + GRIDSIZE > WIDTH) { gridItem.X = 0; lastX = 0; lastY += GRIDSIZE; }
                        lastX += GRIDSIZE;
                        gridItem.Y = lastY;
                        Add(gridItem);
                        currentCount++;
                    }
                }
            }

            public override void Update()
            {
                if (IsDisposed) return;
                if (_container == null || _container.IsDestroyed || (_container.OnGround && _container.Distance > 3)) { Dispose(); return; }
                base.Update();
            }
        }

        private sealed class GridTopBarIcon : Control
        {
            private readonly HitBox _hit;
            private readonly ushort _graphic;

            internal GridTopBarIcon(ushort graphic, int size)
            {
                _graphic = graphic;
                Width = Height = size;
                WantUpdateSize = false;
                AcceptMouseInput = true;
                CanMove = false;

                _hit = new HitBox(0, 0, size, size, null, 0f);
                Add(_hit);
            }

            internal HitBox Hit => _hit;

            public ushort Hue { get; set; }

            internal void SetTooltip(string text) => _hit.SetTooltip(text);

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);

                if (artInfo.Texture == null)
                {
                    return true;
                }

                int artW = artInfo.UV.Width;
                int artH = artInfo.UV.Height;
                Point originalSize = new Point(Width, Height);
                Point point = new Point();

                if (artW < Width)
                {
                    originalSize.X = artW;
                    point.X = (Width >> 1) - (originalSize.X >> 1);
                }
                else if (artW > Width)
                {
                    originalSize.X = Width;
                    point.X = 0;
                }

                if (artH < Height)
                {
                    originalSize.Y = artH;
                    point.Y = (Height >> 1) - (originalSize.Y >> 1);
                }
                else if (artH > Height)
                {
                    originalSize.Y = Height;
                    point.Y = 0;
                }

                Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, 1f);

                batcher.Draw(
                    artInfo.Texture,
                    new Rectangle(x + point.X, y + point.Y, originalSize.X, originalSize.Y),
                    artInfo.UV,
                    hueVector
                );

                return true;
            }
        }

        private class GridSaveSystem
        {
            private const long TIME_CUTOFF = ((60 * 60) * 24) * 60;
            private string gridSavePath = Path.Combine(ProfileManager.ProfilePath, "GridContainers.xml");
            private XDocument saveDocument;
            private XElement rootElement;
            private bool enabled = false;

            private static GridSaveSystem instance;
            internal static GridSaveSystem Instance
            {
                get
                {
                    if (instance == null) instance = new GridSaveSystem();
                    return instance;
                }
            }

            private GridSaveSystem()
            {
                if (!SaveFileCheck()) { enabled = false; return; }
                try { saveDocument = XDocument.Load(gridSavePath); }
                catch { saveDocument = new XDocument(); }

                rootElement = saveDocument.Element("grid_gumps");
                if (rootElement == null)
                {
                    saveDocument.Add(new XElement("grid_gumps"));
                    rootElement = saveDocument.Root;
                }
                enabled = true;
            }

            internal bool HasContainer(uint container)
            {
                if (!enabled)
                {
                    return false;
                }

                return rootElement.Element("container_" + container.ToString()) != null;
            }

            internal void SaveContainerLayout(
                uint serial,
                int width,
                int height,
                int lx,
                int ly,
                bool? useOriginalContainer,
                bool autoSort
            )
            {
                if (!enabled)
                {
                    return;
                }

                if (useOriginalContainer == null)
                {
                    useOriginalContainer = false;
                }

                XElement thisContainer = rootElement.Element("container_" + serial.ToString());

                if (thisContainer == null)
                {
                    thisContainer = new XElement("container_" + serial.ToString());
                    rootElement.Add(thisContainer);
                }

                thisContainer.SetAttributeValue("last_opened", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                thisContainer.SetAttributeValue("width", width.ToString());
                thisContainer.SetAttributeValue("height", height.ToString());
                thisContainer.SetAttributeValue("lastX", lx.ToString());
                thisContainer.SetAttributeValue("lastY", ly.ToString());
                thisContainer.SetAttributeValue("useOriginalContainer", useOriginalContainer.ToString());
                thisContainer.SetAttributeValue("autoSort", autoSort.ToString());
                saveDocument.Save(gridSavePath);
            }

            internal bool SaveContainer(uint serial, Dictionary<int, GridItem> gridSlots, int width, int height, int lx = 100, int ly = 100, bool? useOriginalContainer = false, bool autoSort = false)
            {
                if (!enabled) return false;
                if (useOriginalContainer == null) useOriginalContainer = false;

                XElement thisContainer = rootElement.Element("container_" + serial.ToString());
                if (thisContainer == null) { thisContainer = new XElement("container_" + serial.ToString()); rootElement.Add(thisContainer); }
                else thisContainer.RemoveNodes();

                thisContainer.SetAttributeValue("last_opened", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                thisContainer.SetAttributeValue("width", width.ToString());
                thisContainer.SetAttributeValue("height", height.ToString());
                thisContainer.SetAttributeValue("lastX", lx.ToString());
                thisContainer.SetAttributeValue("lastY", ly.ToString());
                thisContainer.SetAttributeValue("useOriginalContainer", useOriginalContainer.ToString());
                thisContainer.SetAttributeValue("autoSort", autoSort.ToString());

                foreach (var slot in gridSlots)
                {
                    if (slot.Value.SlotItem == null) continue;
                    XElement item_slot = new XElement("item");
                    item_slot.SetAttributeValue("serial", slot.Value.SlotItem.Serial.ToString());
                    item_slot.SetAttributeValue("locked", slot.Value.ItemGridLocked.ToString());
                    item_slot.SetAttributeValue("slot", slot.Key.ToString());
                    thisContainer.Add(item_slot);
                }
                RemoveOldContainers();
                saveDocument.Save(gridSavePath);
                return true;
            }

            internal List<GridItemSlotSaveData> GetItemSlots(uint container)
            {
                List<GridItemSlotSaveData> items = new List<GridItemSlotSaveData>();
                if (!enabled) return items;

                XElement thisContainer = rootElement.Element("container_" + container.ToString());
                if (thisContainer != null)
                {
                    foreach (XElement itemSlot in thisContainer.Elements("item"))
                    {
                        XAttribute slot = itemSlot.Attribute("slot");
                        XAttribute serial = itemSlot.Attribute("serial");
                        XAttribute isLockedAttribute = itemSlot.Attribute("locked");
                        if (slot != null && serial != null)
                        {
                            if (int.TryParse(slot.Value, out int slotV) && uint.TryParse(serial.Value, out uint serialV))
                            {
                                bool isLocked = isLockedAttribute != null && bool.TryParse(isLockedAttribute.Value, out bool il) && il;
                                items.Add(new GridItemSlotSaveData(slotV, serialV, isLocked));
                            }
                        }
                    }
                }
                return items;
            }

            internal class GridItemSlotSaveData
            {
                internal readonly int Slot;
                internal readonly uint Serial;
                internal readonly bool IsLocked;
                internal GridItemSlotSaveData(int slot, uint serial, bool isLocked) { Slot = slot; Serial = serial; IsLocked = isLocked; }
            }

            internal Point GetLastSize(uint container)
            {
                Point lastSize = new Point(GetWidth(), GetHeight());
                if (!enabled) return lastSize;
                XElement thisContainer = rootElement.Element("container_" + container.ToString());
                if (thisContainer != null)
                {
                    XAttribute width = thisContainer.Attribute("width");
                    XAttribute height = thisContainer.Attribute("height");
                    if (width != null && height != null)
                    {
                        int.TryParse(width.Value, out lastSize.X);
                        int.TryParse(height.Value, out lastSize.Y);
                    }
                }
                return lastSize;
            }

            internal Point GetLastPosition(uint container)
            {
                Point LastPos = new Point(GridContainer.lastX, GridContainer.lastY);
                if (!enabled) return LastPos;
                XElement thisContainer = rootElement.Element("container_" + container.ToString());
                if (thisContainer != null)
                {
                    XAttribute lx = thisContainer.Attribute("lastX");
                    XAttribute ly = thisContainer.Attribute("lastY");
                    if (lx != null && ly != null)
                    {
                        int.TryParse(lx.Value, out LastPos.X);
                        int.TryParse(ly.Value, out LastPos.Y);
                    }
                }
                return LastPos;
            }

            internal bool UseOriginalContainerGump(uint container)
            {
                bool useOriginalContainer = false;
                if (!enabled) return useOriginalContainer;
                XElement thisContainer = rootElement.Element("container_" + container.ToString());
                if (thisContainer?.Attribute("useOriginalContainer") is XAttribute attr)
                    bool.TryParse(attr.Value, out useOriginalContainer);
                return useOriginalContainer;
            }

            internal void SetUseOriginalContainer(uint container, bool useOriginalContainer)
            {
                if (!enabled) return;

                XElement thisContainer = rootElement.Element("container_" + container.ToString());
                if (thisContainer == null)
                {
                    thisContainer = new XElement("container_" + container.ToString());
                    rootElement.Add(thisContainer);
                }

                thisContainer.SetAttributeValue("useOriginalContainer", useOriginalContainer.ToString());
                thisContainer.SetAttributeValue("last_opened", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                saveDocument.Save(gridSavePath);
            }

            internal bool AutoSortContainer(uint container)
            {
                bool autoSort = false;
                if (!enabled) return autoSort;
                XElement thisContainer = rootElement.Element("container_" + container.ToString());
                if (thisContainer?.Attribute("autoSort") is XAttribute attr)
                    bool.TryParse(attr.Value, out autoSort);
                return autoSort;
            }

            private void RemoveOldContainers()
            {
                long cutOffTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - TIME_CUTOFF;
                List<XElement> removeMe = new List<XElement>();
                foreach (XElement container in rootElement.Elements())
                {
                    if (container.Attribute("last_opened") is XAttribute lo)
                    {
                        long.TryParse(lo.Value, out long loVal);
                        if (loVal < cutOffTime) removeMe.Add(container);
                    }
                }
                foreach (XElement container in removeMe) container.Remove();
            }

            private bool SaveFileCheck()
            {
                try
                {
                    if (!File.Exists(gridSavePath))
                        File.Create(gridSavePath).Dispose();
                }
                catch { return false; }
                return true;
            }

            internal void Clear() { instance = null; }
        }
    }
}
