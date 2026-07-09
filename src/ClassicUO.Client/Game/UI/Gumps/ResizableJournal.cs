#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ResizableJournal : ResizableGump
    {
        public static bool ReloadTabs { get; set; }

        private const int BORDER_WIDTH = 4;
        private const int TAB_WIDTH = 80;
        private const int TAB_HEIGHT = 30;
        private const int SCROLL_BAR_WIDTH = 14;
        private const int MIN_HEIGHT = 100;

        private static int _lastX = 100;
        private static int _lastY = 100;
        private static int _lastWidth;
        private static int _lastHeight = 300;

        private readonly List<NiceButton> _tabs = new List<NiceButton>();
        private readonly List<MessageType[]> _tabTypes = new List<MessageType[]>();
        private MessageType[] _currentFilter;

        private AlphaBlendControl _background;
        private JournalEntriesContainer _journalArea;
        private ScrollBar _scrollBar;
        private NiceButton _newTabButton;

        private static int MinWidth => (BORDER_WIDTH * 2) + (TAB_WIDTH * 4) + 20;

        public ResizableJournal(World world) : base(world, GetInitialWidth(), _lastHeight, MinWidth, MIN_HEIGHT, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;

            if (ProfileManager.CurrentProfile != null)
            {
                _lastX = ProfileManager.CurrentProfile.JournalPosition.X;
                _lastY = ProfileManager.CurrentProfile.JournalPosition.Y;
            }

            X = _lastX;
            Y = _lastY;

            _background = new AlphaBlendControl(ProfileManager.CurrentProfile?.JournalOpacity / 100f ?? 1f)
            {
                Width = Width - (BORDER_WIDTH * 2),
                Height = Height - (BORDER_WIDTH * 2),
                X = BORDER_WIDTH,
                Y = BORDER_WIDTH,
                CanCloseWithRightClick = true
            };

            _background.DragBegin += (_, e) => InvokeDragBegin(e.Location);

            _scrollBar = new ScrollBar(
                Width - SCROLL_BAR_WIDTH - BORDER_WIDTH,
                BORDER_WIDTH + TAB_HEIGHT,
                Height - TAB_HEIGHT - (BORDER_WIDTH * 2));

            _journalArea = new JournalEntriesContainer(
                BORDER_WIDTH,
                BORDER_WIDTH + TAB_HEIGHT,
                Width - SCROLL_BAR_WIDTH - (BORDER_WIDTH * 2),
                Height - (BORDER_WIDTH * 2) - TAB_HEIGHT,
                _scrollBar,
                this);

            _journalArea.DragBegin += (_, e) => InvokeDragBegin(e.Location);

            Add(_background);
            Add(_scrollBar);
            Add(_journalArea);

            _newTabButton = new NiceButton(0, 0, 20, TAB_HEIGHT, ButtonAction.Activate, "+") { IsSelectable = false };
            _newTabButton.SetTooltip("Add a new tab");
            _newTabButton.MouseUp += OnNewTabClicked;
            Add(_newTabButton);

            EnsureDefaultTabs();
            BuildTabs();
            InitJournalEntries();

            Point savedSize = GetSavedSize();
            Width = savedSize.X;
            Height = savedSize.Y;
            OnResize();
            ScrollToBottom();

            world.Journal.EntryAdded += OnJournalEntryAdded;
        }

        public override GumpType GumpType => GumpType.Journal;

        private static int GetInitialWidth()
        {
            if (_lastWidth > 0)
            {
                return _lastWidth;
            }

            Point saved = ProfileManager.CurrentProfile?.ResizeJournalSize ?? Point.Zero;
            return saved.X > 0 ? saved.X : MinWidth;
        }

        private Point GetSavedSize()
        {
            Point saved = ProfileManager.CurrentProfile?.ResizeJournalSize ?? Point.Zero;

            if (saved.X > 0 && saved.Y > 0)
            {
                return saved;
            }

            return new Point(Width > 0 ? Width : MinWidth, _lastHeight);
        }

        private static void EnsureDefaultTabs()
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null)
            {
                return;
            }

            if (profile.JournalTabs == null || profile.JournalTabs.Count == 0)
            {
                profile.JournalTabs = Profile.CreateDefaultJournalTabs();
            }
        }

        private void ScrollToBottom()
        {
            if (_journalArea == null || _scrollBar == null)
            {
                return;
            }

            _journalArea.RecalculateScrollBar();
            _scrollBar.Value = _scrollBar.MaxValue;
        }

        private void OnJournalEntryAdded(object sender, JournalEntry entry)
        {
            AddJournalEntry(entry);
        }

        private void OnNewTabClicked(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left)
            {
                return;
            }

            UIManager.Add(new JournalTabNameGump(World, X, Y, name =>
            {
                if (!string.IsNullOrWhiteSpace(name) && !ProfileManager.CurrentProfile.JournalTabs.ContainsKey(name))
                {
                    ProfileManager.CurrentProfile.JournalTabs[name] = new[] { MessageType.Regular };
                    ReloadTabs = true;
                }
            }));
        }

        private void BuildTabs()
        {
            foreach (NiceButton tab in _tabs)
            {
                tab.Dispose();
            }

            _tabs.Clear();
            _tabTypes.Clear();

            Profile profile = ProfileManager.CurrentProfile;

            if (profile?.JournalTabs == null)
            {
                return;
            }

            foreach (KeyValuePair<string, MessageType[]> tab in profile.JournalTabs)
            {
                AddTab(tab.Key, tab.Value);
            }

            int selected = profile.LastJournalTab;

            if (selected >= 0 && selected < _tabs.Count)
            {
                _tabs[selected].IsSelected = true;
                OnButtonClick(selected);
            }
            else if (_tabs.Count > 0)
            {
                _tabs[0].IsSelected = true;
                OnButtonClick(0);
            }

            foreach (NiceButton tab in _tabs)
            {
                Add(tab);
            }

            _newTabButton.X = (_tabs.Count * TAB_WIDTH) + 4;
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID < 0 || buttonID >= _tabs.Count)
            {
                return;
            }

            foreach (NiceButton tab in _tabs)
            {
                tab.IsSelected = false;
            }

            _tabs[buttonID].IsSelected = true;
            _currentFilter = _tabTypes[buttonID];

            if (_journalArea != null && _scrollBar != null)
            {
                _journalArea.RecalculateScrollBar();
                _scrollBar.Value = _scrollBar.MaxValue;
            }

            Profile profile = ProfileManager.CurrentProfile;

            if (profile != null)
            {
                profile.LastJournalTab = buttonID;
            }
        }

        private void AddTab(string name, MessageType[] filters)
        {
            int index = _tabs.Count;
            NiceButton tab = new NiceButton((index * TAB_WIDTH) + 4, 0, TAB_WIDTH, TAB_HEIGHT, ButtonAction.Activate, name, 1)
            {
                ButtonParameter = index,
                IsSelectable = true,
                CanCloseWithRightClick = false,
                ContextMenu = new JournalTabContextMenu(this, name)
            };

            tab.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtonType.Right)
                {
                    tab.ContextMenu.Show();
                }
                else if (e.Button == MouseButtonType.Left)
                {
                    OnButtonClick(tab.ButtonParameter);
                }
            };

            _tabs.Add(tab);
            _tabTypes.Add(filters);
        }

        private void AddJournalEntry(JournalEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(entry.Name) && World.IgnoreManager.IgnoredCharsList.Contains(entry.Name))
            {
                return;
            }

            string text = string.IsNullOrEmpty(entry.Name) ? entry.Text : $"{entry.Name}: {entry.Text}";
            _journalArea.AddEntry(text, entry.Hue, entry.Time, entry.TextType, entry.MessageType, entry.Font, entry.IsUnicode);
        }

        private void InitJournalEntries()
        {
            foreach (JournalEntry entry in JournalManager.Entries)
            {
                AddJournalEntry(entry);
            }
        }

        public void RebuildEntries()
        {
            _journalArea?.ClearEntries();
            InitJournalEntries();
            ScrollToBottom();
        }

        public override void OnResize()
        {
            base.OnResize();

            if (_background == null || _journalArea == null || _scrollBar == null || _newTabButton == null)
            {
                return;
            }

            _background.Width = Math.Max(0, Width - (BORDER_WIDTH * 2));
            _background.Height = Math.Max(0, Height - (BORDER_WIDTH * 2));
            _journalArea.Width = Math.Max(0, Width - SCROLL_BAR_WIDTH - (BORDER_WIDTH * 2));
            _journalArea.Height = Math.Max(0, Height - (BORDER_WIDTH * 2) - TAB_HEIGHT);
            _journalArea.Y = TAB_HEIGHT;
            _scrollBar.X = Width - SCROLL_BAR_WIDTH - BORDER_WIDTH;
            _scrollBar.Y = _journalArea.Y;
            _scrollBar.Height = Math.Max(0, Height - BORDER_WIDTH - TAB_HEIGHT);
            _lastWidth = Width;
            _lastHeight = Height;

            Profile profile = ProfileManager.CurrentProfile;

            if (profile != null)
            {
                profile.ResizeJournalSize = new Point(Width, Height);
            }

            ScrollToBottom();
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            base.OnMouseWheel(delta);
            _scrollBar?.InvokeMouseWheel(delta);
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (X != _lastX || Y != _lastY)
            {
                _lastX = X;
                _lastY = Y;

                if (ProfileManager.CurrentProfile != null)
                {
                    ProfileManager.CurrentProfile.JournalPosition = new Point(X, Y);
                }
            }

            if (ReloadTabs)
            {
                ReloadTabs = false;
                BuildTabs();
            }
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("rw", Width.ToString());
            writer.WriteAttributeString("rh", Height.ToString());

            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i].IsSelected)
                {
                    writer.WriteAttributeString("tab", i.ToString());
                    break;
                }
            }
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            Point savedSize = new Point(Width, Height);

            if (int.TryParse(xml.GetAttribute("rw"), out int width) && width > 0)
            {
                savedSize.X = width;
            }

            if (int.TryParse(xml.GetAttribute("rh"), out int height) && height > 0)
            {
                savedSize.Y = height;
            }

            if (int.TryParse(xml.GetAttribute("tab"), out int tab))
            {
                OnButtonClick(tab);
            }

            ResizeWindow(savedSize);
        }

        public override void Dispose()
        {
            World.Journal.EntryAdded -= OnJournalEntryAdded;
            base.Dispose();
        }

        private sealed class JournalEntriesContainer : Control
        {
            private readonly Deque<JournalData> _entries = new Deque<JournalData>();
            private readonly ScrollBarBase _scrollBar;
            private readonly ResizableJournal _journal;
            private int _lastWidth;
            private int _lastHeight;

            public JournalEntriesContainer(int x, int y, int width, int height, ScrollBarBase scrollBar, ResizableJournal journal)
            {
                _scrollBar = scrollBar;
                _journal = journal;
                AcceptMouseInput = true;
                CanMove = true;
                X = x;
                Y = y;
                Width = _lastWidth = width;
                Height = _lastHeight = height;
                WantUpdateSize = false;
            }

            public void ClearEntries()
            {
                foreach (JournalData entry in _entries)
                {
                    entry.Destroy();
                }

                _entries.Clear();
                _scrollBar.MaxValue = 0;
                _scrollBar.Value = 0;
            }

            public void AddEntry(string text, ushort hue, DateTime time, TextType textType, MessageType messageType, byte font, bool isUnicode)
            {
                bool maxScroll = _scrollBar.Value == _scrollBar.MaxValue;
                Profile profile = ProfileManager.CurrentProfile;
                int maxEntries = profile?.MaxJournalEntries ?? 200;

                while (_entries.Count > maxEntries)
                {
                    _entries.RemoveFromFront().Destroy();
                }

                JournalManager.ResolveJournalFont(ref font, ref isUnicode);

                if (hue == 0)
                {
                    hue = 0x0481;
                }

                RenderedText timestamp = null;
                int timestampWidth = 0;

                if (profile == null || !profile.HideJournalTimestamp)
                {
                    timestamp = RenderedText.Create($"{time:t} ", 1150, 1, true, FontStyle.BlackBorder);
                    timestampWidth = timestamp.Width;
                }

                RenderedText entryText = RenderedText.Create(
                    text,
                    hue,
                    font,
                    isUnicode,
                    FontStyle.Indention | FontStyle.BlackBorder,
                    maxWidth: Width - timestampWidth - 8);

                _entries.AddToBack(new JournalData(entryText, timestamp, textType, messageType));

                if (maxScroll)
                {
                    _scrollBar.Value = _scrollBar.MaxValue;
                }

                RecalculateScrollBar();
            }

            public void RecalculateScrollBar()
            {
                bool atMax = _scrollBar.Value == _scrollBar.MaxValue;
                int height = 0;

                foreach (JournalData entry in _entries)
                {
                    if (entry.EntryText == null)
                    {
                        continue;
                    }

                    if (CanBeDrawn(entry.TextType, entry.MessageType))
                    {
                        height += entry.EntryText.Height;
                    }
                }

                height -= _scrollBar.Height;

                if (height > 0)
                {
                    _scrollBar.MaxValue = height;

                    if (atMax)
                    {
                        _scrollBar.Value = _scrollBar.MaxValue;
                    }
                }
                else
                {
                    _scrollBar.MaxValue = 0;
                    _scrollBar.Value = 0;
                }

                _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
            }

            private bool CanBeDrawn(TextType textType, MessageType messageType)
            {
                MessageType[] filter = _journal._currentFilter;

                if (filter == null || filter.Length == 0)
                {
                    return true;
                }

                if (textType == TextType.SYSTEM && filter.Contains(MessageType.System))
                {
                    return true;
                }

                if (textType == TextType.SYSTEM)
                {
                    return false;
                }

                return filter.Contains(messageType);
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    int my = y;

                    foreach (JournalData entry in _entries)
                    {
                        if (entry.EntryText == null || string.IsNullOrEmpty(entry.EntryText.Text))
                        {
                            continue;
                        }

                        if (!CanBeDrawn(entry.TextType, entry.MessageType))
                        {
                            continue;
                        }

                        if (
                            my + entry.EntryText.Height - y >= _scrollBar.Value
                            && my - y <= _scrollBar.Value + _scrollBar.Height
                        )
                        {
                            int yy = my - _scrollBar.Value;
                            int textX = x;

                            if (entry.Timestamp != null)
                            {
                                entry.Timestamp.Draw(batcher, x, yy);
                                textX += entry.Timestamp.Width;
                            }

                            entry.EntryText.Draw(batcher, textX, yy);
                        }

                        my += entry.EntryText.Height;
                    }

                    batcher.ClipEnd();
                }

                return true;
            }

            public override void Update()
            {
                base.Update();

                if (!IsVisible)
                {
                    return;
                }

                if (Width != _lastWidth || Height != _lastHeight)
                {
                    _lastWidth = Width;
                    _lastHeight = Height;
                    RecalculateScrollBar();
                }
            }

            public override void Dispose()
            {
                foreach (JournalData entry in _entries)
                {
                    entry.Destroy();
                }

                _entries.Clear();
                base.Dispose();
            }

            private sealed class JournalData
            {
                public JournalData(RenderedText entryText, RenderedText timestamp, TextType textType, MessageType messageType)
                {
                    EntryText = entryText;
                    Timestamp = timestamp;
                    TextType = textType;
                    MessageType = messageType;
                }

                public RenderedText EntryText { get; }
                public RenderedText Timestamp { get; }
                public TextType TextType { get; }
                public MessageType MessageType { get; }

                public void Destroy()
                {
                    EntryText?.Destroy();
                    Timestamp?.Destroy();
                }
            }
        }

        private sealed class JournalTabContextMenu : ContextMenuControl
        {
            public JournalTabContextMenu(ResizableJournal journal, string name) : base(journal)
            {
                if (!ProfileManager.CurrentProfile.JournalTabs.TryGetValue(name, out MessageType[] selectedTypes))
                {
                    return;
                }

                foreach (MessageType item in Enum.GetValues(typeof(MessageType)))
                {
                    string entryName = item.ToString();
                    MessageType captured = item;

                    Add(entryName, () =>
                    {
                        if (!ProfileManager.CurrentProfile.JournalTabs.TryGetValue(name, out MessageType[] types))
                        {
                            return;
                        }

                        List<MessageType> list = types.ToList();

                        if (list.Contains(captured))
                        {
                            list.Remove(captured);
                        }
                        else
                        {
                            list.Add(captured);
                        }

                        ProfileManager.CurrentProfile.JournalTabs[name] = list.ToArray();
                        ReloadTabs = true;
                    }, true, selectedTypes.Contains(item));
                }

                Add("Delete tab", () =>
                {
                    UIManager.Add(new QuestionGump(journal.World, $"Delete [{name}] tab?", yes =>
                    {
                        if (yes)
                        {
                            ProfileManager.CurrentProfile.JournalTabs.Remove(name);
                            ReloadTabs = true;
                        }
                    }));
                });
            }
        }
    }

    internal sealed class JournalTabNameGump : Gump
    {
        private readonly Action<string> _callback;
        private readonly StbTextBox _textBox;

        public JournalTabNameGump(World world, int x, int y, Action<string> callback) : base(world, 0, 0)
        {
            _callback = callback;
            X = x;
            Y = y;
            Width = 260;
            Height = 120;
            CanMove = true;
            CanCloseWithRightClick = true;

            Add(new AlphaBlendControl(0.95f) { Width = Width, Height = Height });
            Add(new BorderControl(0, 0, Width, Height, 4));
            Add(new Label("Enter tab name", false, 0x0386, font: 1) { X = 12, Y = 12 });

            _textBox = new StbTextBox(1, maxWidth: 220) { X = 12, Y = 40, Width = 220, Height = 22 };
            Add(_textBox);

            NiceButton save = new NiceButton(12, 78, 80, 25, ButtonAction.Activate, "Save");
            save.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    _callback?.Invoke(_textBox.Text?.Trim());
                    Dispose();
                }
            };

            NiceButton cancel = new NiceButton(100, 78, 80, 25, ButtonAction.Activate, "Cancel");
            cancel.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    Dispose();
                }
            };

            Add(save);
            Add(cancel);
            _textBox.SetKeyboardFocus();
        }
    }
}
