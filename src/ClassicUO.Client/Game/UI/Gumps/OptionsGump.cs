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
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class OptionsGump : Gump
    {
        private const byte FONT = 0xFF;
        private const ushort HUE_FONT = 0xFFFF;
        private const int WIDTH = 700;
        private const int HEIGHT = 500;
        private const int TEXTBOX_HEIGHT = 25;
        private const int OptionsSearchRowHeight = 26;
        private const int OptionsSearchToContentGap = 20;
        private const int OptionsSectionGap = 40;
        private const int OptionsSectionGapLarge = 60;
        // Extra vertical breathing room above the "EnergyField auto-avoid" row in Mods -> Visual Helpers.
        private const int VisualHelpersEnergyFieldTopGap = 12;
        private const int OptionsScrollContentPadding = 12;
        private const int OptionsTabStartY = 10;
        private const int OptionsScrollY = OptionsTabStartY + OptionsSearchRowHeight + OptionsSearchToContentGap;
        private const int OptionsScrollHeight = 420 - (OptionsScrollY - 20);
        private const int OptionsMacrosToolbarHeight = 61;

        private static Texture2D _logoTexture2D;
        private Combobox _auraType;
        private Combobox _autoOpenCorpseOptions;
        private InputField _autoOpenCorpseRange;

        //experimental
        private Checkbox _autoOpenDoors, _autoOpenCorpse, _skipEmptyCorpse, _disableTabBtn, _disableCtrlQWBtn, _disableDefaultHotkeys, _disableArrowBtn, _disableAutoMove, _overrideContainerLocation, _smoothDoors, _showTargetRangeIndicator, _forceGargoyleWalk, _customBars, _customBarsBBG, _saveHealthbars;
        private Checkbox _avoidObstacles, _avoidObstaclesIgnoreHumanoids;
        private HSliderBar _cellSize;
        private Checkbox _containerScaleItems, _containerDoubleClickToLoot, _relativeDragAnDropItems, _useLargeContianersGumps, _highlightContainersWhenMouseIsOver;


        // containers
        private HSliderBar _containersScale;
        // grid container
        private Checkbox _gridContainerEnabled, _gridHideBorder, _gridContainerPreview;
        private HSliderBar _gridColumns, _gridRows, _gridContainerScale, _gridContainerOpacity, _gridBorderOpacity;
        private ClickableColorBox _gridBorderHue, _gridContainerBorderHue;
        private Combobox _gridSearchMode;
        private Combobox _cotType;
        private DataBox _databox;
        private HSliderBar _delay_before_display_tooltip, _tooltip_zoom, _tooltip_background_opacity;
        private Combobox _dragSelectModifierKey;
        private Combobox _backpackStyle;
        private Checkbox _hueContainerGumps;


        //counters
        private Checkbox _enableCounters, _highlightOnUse, _highlightOnAmount, _enableAbbreviatedAmount;
        private Checkbox _enableDragSelect, _dragSelectHumanoidsOnly;

        // sounds
        private Checkbox _enableSounds, _enableMusic, _footStepsSound, _combatMusic, _musicInBackground, _loginMusic;

        // fonts
        private FontSelector _fontSelectorChat;
        private Checkbox _forceUnicodeJournal;
        private InputField _gameWindowHeight;

        private Checkbox _gameWindowLock, _gameWindowFullsize;
        // GameWindowPosition
        private InputField _gameWindowPositionX;
        private InputField _gameWindowPositionY;

        // GameWindowSize
        private InputField _gameWindowWidth;
        private Combobox _gridLoot;
        private Checkbox _hideScreenshotStoredInMessage;
        private Checkbox _highlightObjects, /*_smoothMovements,*/
                         _enablePathfind,
                         _useShiftPathfind,
                         _alwaysRun,
                         _alwaysRunUnlessHidden,
                         _showHpMobile,
                         _useOldHealthBars,
                         _highlightByPoisoned,
                         _highlightByParalyzed,
                         _highlightByInvul,
                         _drawRoofs,
                         _treeToStumps,
                         _hideVegetation,
                         _hideCarpets,
                         _noColorOutOfRangeObjects,
                         _useCircleOfTransparency,
                         _enableTopbar,
                         _holdDownKeyTab,
                         _holdDownKeyAlt,
                         _closeAllAnchoredGumpsWithRClick,
                         _chatAfterEnter,
                         _chatAdditionalButtonsCheckbox,
                         _chatShiftEnterCheckbox,
                         _enableCaveBorder;
        private Checkbox _holdShiftForContext, _holdShiftToSplitStack, _reduceFPSWhenInactive, _sallosEasyGrab, _partyInviteGump, _objectsFading, _textFading, _holdAltToMoveGumps;
        private Combobox _hpComboBox, _healtbarType, _fieldsType, _hpComboBoxShowWhen;

        // Visual Helpers (Dust765) - Highlight tiles on range
        private Checkbox _ltHighlightRangeOnActivated, _ltHighlightRangeOnCast;
        private HSliderBar _ltHighlightRangeOnActivatedRange, _ltHighlightRangeOnCastRange, _ltHighlightRangeOutlinePixels;
        private ClickableColorBox _ltHighlightRangeOnActivatedHue, _ltHighlightRangeOnCastHue;
        private Checkbox _highlightMirrorClones, _enableUoDreamsNetworkOptimizer, _enableFullSocketDrain, _fastRotation, _showBandageRingTimer;
        private InputField _bandageRingTimerX, _bandageRingTimerY;
        private ClickableColorBox _mirrorCloneHue;
        private static readonly string[] VisualHighlightModes = { "Off", "White", "Pink", "Ice", "Fire", "Custom" };
        private static readonly string[] VisualHighlightStateModes = { "Off", "White", "Pink", "Ice", "Fire", "Special", "Custom" };
        private Combobox _glowingWeaponsType, _highlightLastTargetType, _highlighFriendsGuildType;
        private Combobox _highlightLastTargetTypePoison, _highlightLastTargetTypePara, _highlightLastTargetTypeStunned, _highlightLastTargetTypeMortalled;
        private ClickableColorBox _highlightGlowingWeaponsTypeHue, _highlightLastTargetTypeHue, _highlighFriendsGuildTypeHue;
        private ClickableColorBox _highlightLastTargetTypePoisonHue, _highlightLastTargetTypeParaHue, _highlightLastTargetTypeStunnedHue, _highlightLastTargetTypeMortalledHue;
        private InputField _movementTurnDelay, _movementTurnDelayFast, _movementWalkingDelay, _movementPlayerWalkingDelay;
        private Checkbox _showEquipmentDurabilityButton, _useModernJournal, _hideJournalTimestamp, _invisibleHousesEnabled, _autoOpenUiOnLogin;
        private HSliderBar _invisibleHousesZ;

        private InputField _optionsSearchField;
        private Label _optionsSearchLabel;
        private NiceButton _optionsSearchClearBtn;
        private NiceButton _optionsSearchSubmitBtn;
        private string _optionsSearchAppliedTerm = string.Empty;
        private readonly List<Control> _optionsSearchMatches = new List<Control>();
        private readonly HashSet<int> _optionsSearchPagesWithHits = new HashSet<int>();
        private readonly Dictionary<Control, int> _optionsSearchSavedY = new Dictionary<Control, int>();
        private Checkbox _hidePersistentNPCNames, _nameOverheadAlwaysOn;
        private Checkbox _showAllLayersPaperdoll;
        private Checkbox _energyFieldWallOfStoneAutoAvoid;

        // infobar
        private List<InfoBarBuilderControl> _infoBarBuilderControls;
        private Combobox _infoBarHighlightType;

        // combat & spells
        private ClickableColorBox _innocentColorPickerBox, _friendColorPickerBox, _crimialColorPickerBox, _canAttackColorPickerBox, _enemyColorPickerBox, _murdererColorPickerBox, _neutralColorPickerBox, _beneficColorPickerBox, _harmfulColorPickerBox;
        private HSliderBar _lightBar;
        private Checkbox _buffBarTime, _uiButtonsSingleClick, _queryBeforAttackCheckbox, _queryBeforeBeneficialCheckbox, _spellColoringCheckbox, _spellFormatCheckbox, _enableFastSpellsAssign;

        // macro
        private MacroControl _macroControl;
        private Checkbox _overrideAllFonts;
        private Combobox _overrideAllFontsIsUnicodeCheckbox;
        private Combobox _overrideContainerLocationSetting;
        private ClickableColorBox _poisonColorPickerBox, _paralyzedColorPickerBox, _invulnerableColorPickerBox;
        private NiceButton _randomizeColorsButton;
        private Checkbox _restorezoomCheckbox, _zoomCheckbox;
        private InputField _rows, _columns, _highlightAmount, _abbreviatedAmount;

        // speech
        private Checkbox _scaleSpeechDelay, _saveJournalCheckBox;
        private Checkbox _showHouseContent;
        private Checkbox _showInfoBar;
        private Checkbox _ignoreAllianceMessages;
        private Checkbox _ignoreGuildMessages;

        // general
        private HSliderBar _sliderFPS, _circleOfTranspRadius;
        private HSliderBar _sliderSpeechDelay;
        private HSliderBar _sliderZoom;
        private HSliderBar _soundsVolume, _musicVolume, _loginMusicVolume;
        private ClickableColorBox _speechColorPickerBox, _emoteColorPickerBox, _yellColorPickerBox, _whisperColorPickerBox, _partyMessageColorPickerBox, _guildMessageColorPickerBox, _allyMessageColorPickerBox, _chatMessageColorPickerBox, _partyAuraColorPickerBox;
        private InputField _spellFormatBox;
        private ClickableColorBox _tooltip_font_hue;
        private FontSelector _tooltip_font_selector;
        private HSliderBar _dragSelectStartX, _dragSelectStartY;
        private Checkbox _dragSelectAsAnchor;

        // video
        private Checkbox _use_old_status_gump, _windowBorderless, _enableDeathScreen, _enableBlackWhiteEffect, _altLights, _enableLight, _enableShadows, _enableShadowsStatics, _auraMouse, _runMouseInSeparateThread, _useColoredLights, _darkNights, _partyAura, _hideChatGradient, _animatedWaterEffect;
        private Combobox _lightLevelType;
        private Checkbox _use_smooth_boat_movement;
        private HSliderBar _terrainShadowLevel;

        private Checkbox _use_tooltip;
        private Checkbox _useStandardSkillsGump, _showMobileNameIncoming, _showCorpseNameIncoming;
        private Checkbox _showStatsMessage, _showSkillsMessage;
        private HSliderBar _showSkillsMessageDelta;


        private Profile _currentProfile = ProfileManager.CurrentProfile;

        public OptionsGump(World world) : base(world, 0, 0)
        {
            Add
            (
                new AlphaBlendControl(0.95f)
                {
                    X = 1,
                    Y = 1,
                    Width = WIDTH - 2,
                    Height = HEIGHT - 2,
                    Hue = 999
                }
            );


            int i = 0;

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.General
                ) { IsSelected = true, ButtonParameter = 1 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.Sound
                ) { ButtonParameter = 2 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.Video
                ) { ButtonParameter = 3 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.Macros
                ) { ButtonParameter = 4 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.Tooltip
                ) { ButtonParameter = 5 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.Fonts
                ) { ButtonParameter = 6 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.Speech
                ) { ButtonParameter = 7 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.CombatSpells
                ) { ButtonParameter = 8 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.Counters
                ) { ButtonParameter = 9 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.InfoBar
                ) { ButtonParameter = 10 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.Containers
                ) { ButtonParameter = 11 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.Experimental
                ) { ButtonParameter = 12 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    "Mods"
                ) { ButtonParameter = 13 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.Activate,
                    ResGumps.IgnoreListManager
                )
                {
                    ButtonParameter = (int)Buttons.OpenIgnoreList
                }
            );

            Add
            (
                new Line
                (
                    160,
                    5,
                    1,
                    HEIGHT - 10,
                    Color.Gray.PackedValue
                )
            );

            int offsetX = 60;
            int offsetY = 60;

            Add
            (
                new Line
                (
                    160,
                    405 + 35 + 1,
                    WIDTH - 160,
                    1,
                    Color.Gray.PackedValue
                )
            );

            Add
            (
                new Button((int) Buttons.Cancel, 0x00F3, 0x00F1, 0x00F2)
                {
                    X = 154 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Apply, 0x00EF, 0x00F0, 0x00EE)
                {
                    X = 248 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Default, 0x00F6, 0x00F4, 0x00F5)
                {
                    X = 346 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Ok, 0x00F9, 0x00F8, 0x00F7)
                {
                    X = 443 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
                }
            );

            AcceptMouseInput = true;
            CanMove = true;
            CanCloseWithRightClick = true;

            BuildGeneral();
            BuildSounds();
            BuildVideo();
            BuildCommands();
            BuildFonts();
            BuildSpeech();
            BuildCombat();
            BuildTooltip();
            BuildCounters();
            BuildInfoBar();
            BuildContainers();
            BuildExperimental();
            BuildMods();

            ChangePage(1);

            const int searchFieldMaxW = 380;
            int searchRowY = OptionsTabStartY + 1;
            int contentLeft = 168;
            _optionsSearchLabel = new Label("Search:", true, HUE_FONT, font: FONT)
            {
                X = contentLeft,
                Y = searchRowY + 3
            };
            Add(_optionsSearchLabel, 0);

            const int clearBtnW = 56;
            const int searchBtnW = 78;
            const int btnGap = 6;
            int fieldX = _optionsSearchLabel.X + _optionsSearchLabel.Width + 10;
            int fieldW = Math.Min(
                searchFieldMaxW,
                Math.Max(140, WIDTH - fieldX - clearBtnW - searchBtnW - btnGap * 2 - 12)
            );
            _optionsSearchField = AddInputField(null, fieldX, searchRowY, fieldW, TEXTBOX_HEIGHT);
            Add(_optionsSearchField, 0);

            int clearX = fieldX + fieldW + btnGap;
            _optionsSearchClearBtn = new NiceButton(clearX, searchRowY - 1, clearBtnW, TEXTBOX_HEIGHT + 2, ButtonAction.Activate, "Clear");
            _optionsSearchClearBtn.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    ClearOptionsSearch();
                }
            };
            Add(_optionsSearchClearBtn, 0);

            _optionsSearchSubmitBtn = new NiceButton(clearX + clearBtnW + btnGap, searchRowY - 1, searchBtnW, TEXTBOX_HEIGHT + 2, ButtonAction.Activate, "Search");
            _optionsSearchSubmitBtn.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    RunOptionsSearch();
                }
            };
            Add(_optionsSearchSubmitBtn, 0);
        }

        private void ClearOptionsSearch()
        {
            _optionsSearchField?.SetText(string.Empty);
            _optionsSearchAppliedTerm = string.Empty;
            _optionsSearchMatches.Clear();
            _optionsSearchPagesWithHits.Clear();
            ApplyOptionsSearchFilter();
        }

        private void RunOptionsSearch()
        {
            _optionsSearchAppliedTerm = _optionsSearchField?.Text?.Trim() ?? string.Empty;
            RebuildOptionsSearchMatches();
            ApplyOptionsSearchFilter();
        }

        private bool IsOptionsSearchChrome(Control c)
        {
            for (; c != null; c = c.Parent)
            {
                if (c == _optionsSearchField
                    || c == _optionsSearchClearBtn
                    || c == _optionsSearchSubmitBtn
                    || c == _optionsSearchLabel)
                {
                    return true;
                }
            }

            return false;
        }

        private int OptionsSearchContentPage(Control c)
        {
            for (Control w = c; w != null; w = w.Parent)
            {
                if (w.Parent == this)
                {
                    return w.Page;
                }
            }

            return 0;
        }

        private static string OptionsLocalizedLabel(string italian, string english)
        {
            return string.Equals(Settings.GlobalSettings.Language, "ITA", StringComparison.OrdinalIgnoreCase)
                ? italian
                : english;
        }

        private static void GetControlPositionInOptionsGump(OptionsGump gump, Control c, out int gx, out int gy)
        {
            if (c.Parent == gump)
            {
                gx = c.X;
                gy = c.Y;
                return;
            }

            GetControlPositionInOptionsGump(gump, c.Parent, out gx, out gy);

            if (c.Parent is ScrollArea sa)
            {
                gx += c.X;
                gy += c.Y - sa.ScrollValue + sa.ScissorRectangle.Y;
                return;
            }

            gx += c.X;
            gy += c.Y;
        }

        private void RebuildOptionsSearchMatches()
        {
            _optionsSearchMatches.Clear();
            _optionsSearchPagesWithHits.Clear();

            if (_optionsSearchField == null || string.IsNullOrEmpty(_optionsSearchAppliedTerm) || _optionsSearchAppliedTerm.Length < 2)
            {
                return;
            }

            CollectOptionsSearchMatches(this, _optionsSearchAppliedTerm);
        }

        private void CollectOptionsSearchMatches(Control root, string term)
        {
            for (int i = 0; i < root.Children.Count; i++)
            {
                Control ch = root.Children[i];

                if (ch == null || ch.IsDisposed)
                {
                    continue;
                }

                CollectOptionsSearchMatches(ch, term);
            }

            if (root == this || IsOptionsSearchChrome(root))
            {
                return;
            }

            if (root is Checkbox cb && !string.IsNullOrEmpty(cb.Text) && cb.Text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _optionsSearchMatches.Add(root);
                int pg = OptionsSearchContentPage(root);

                if (pg > 0)
                {
                    _optionsSearchPagesWithHits.Add(pg);
                }

                return;
            }

            if (root is Combobox combo && combo.MatchesSearch(term))
            {
                _optionsSearchMatches.Add(root);
                int pg = OptionsSearchContentPage(root);

                if (pg > 0)
                {
                    _optionsSearchPagesWithHits.Add(pg);
                }

                return;
            }

            if (root is Label lbl && !string.IsNullOrEmpty(lbl.Text) && lbl.Text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _optionsSearchMatches.Add(root);
                int pg = OptionsSearchContentPage(root);

                if (pg > 0)
                {
                    _optionsSearchPagesWithHits.Add(pg);
                }
            }
        }

        private static bool IsOptionsSearchableControl(Control c)
        {
            return c is Checkbox
                or Label
                or Combobox
                or HSliderBar
                or ClickableColorBox
                or FontSelector
                or InputField
                or RadioButton;
        }

        private void ApplyOptionsSearchFilter()
        {
            bool filter = _optionsSearchAppliedTerm.Length >= 2;
            HashSet<Control> matches = filter ? new HashSet<Control>(_optionsSearchMatches) : null;

            if (!filter)
            {
                RestoreOptionsSearchSavedPositions();
            }
            else if (_optionsSearchSavedY.Count == 0)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    Control pageChild = Children[i];

                    if (pageChild.Page < 1 || pageChild.Page > 13)
                    {
                        continue;
                    }

                    SaveOptionsSearchPositions(pageChild);
                }
            }

            for (int i = 0; i < Children.Count; i++)
            {
                Control pageChild = Children[i];

                if (pageChild.Page < 1 || pageChild.Page > 13)
                {
                    continue;
                }

                ApplyOptionsSearchFilterToControl(pageChild, filter, matches);
            }

            if (filter)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    Control pageChild = Children[i];

                    if (pageChild.Page < 1 || pageChild.Page > 13)
                    {
                        continue;
                    }

                    ReflowOptionsSearchLayout(pageChild);
                }

                ResetOptionsSearchScrollForActivePage();
            }
        }

        private void SaveOptionsSearchPositions(Control root)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (root != this && !_optionsSearchSavedY.ContainsKey(root))
            {
                _optionsSearchSavedY[root] = root.Y;
            }

            for (int i = 0; i < root.Children.Count; i++)
            {
                SaveOptionsSearchPositions(root.Children[i]);
            }
        }

        private void RestoreOptionsSearchSavedPositions()
        {
            foreach (KeyValuePair<Control, int> entry in _optionsSearchSavedY)
            {
                if (entry.Key != null && !entry.Key.IsDisposed)
                {
                    entry.Key.Y = entry.Value;
                }
            }

            _optionsSearchSavedY.Clear();
        }

        private static int OptionsSearchOriginalY(Control c, Dictionary<Control, int> savedY)
        {
            return savedY.TryGetValue(c, out int y) ? y : c.Y;
        }

        private static bool DataBoxUsesRowLayout(DataBox box)
        {
            var ys = new HashSet<int>();

            for (int i = 0; i < box.Children.Count; i++)
            {
                Control child = box.Children[i];

                if (child == null || child.IsDisposed)
                {
                    continue;
                }

                if (!ys.Add(child.Y))
                {
                    return true;
                }
            }

            return false;
        }

        private void ReflowOptionsSearchLayout(Control root)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            for (int i = 0; i < root.Children.Count; i++)
            {
                ReflowOptionsSearchLayout(root.Children[i]);
            }

            if (root is SettingsSection section)
            {
                section.SyncContentHeight();
            }
            else if (root is DataBox dataBox)
            {
                if (DataBoxUsesRowLayout(dataBox))
                {
                    ReflowDataBoxRows(dataBox);
                }
                else
                {
                    dataBox.ReArrangeChildren();
                }
            }
            else if (root is ScrollArea scrollArea)
            {
                ReflowScrollAreaContent(scrollArea);
            }
        }

        private void ReflowDataBoxRows(DataBox box)
        {
            var rowOriginalYs = new List<int>();

            for (int i = 0; i < box.Children.Count; i++)
            {
                Control child = box.Children[i];

                if (child == null || child.IsDisposed)
                {
                    continue;
                }

                int origY = OptionsSearchOriginalY(child, _optionsSearchSavedY);

                if (!rowOriginalYs.Contains(origY))
                {
                    rowOriginalYs.Add(origY);
                }
            }

            int nextY = 0;
            const int rowGap = 2;

            for (int r = 0; r < rowOriginalYs.Count; r++)
            {
                int origRowY = rowOriginalYs[r];
                int rowHeight = 0;
                bool anyVisible = false;

                for (int i = 0; i < box.Children.Count; i++)
                {
                    Control child = box.Children[i];

                    if (child == null || child.IsDisposed)
                    {
                        continue;
                    }

                    if (OptionsSearchOriginalY(child, _optionsSearchSavedY) != origRowY)
                    {
                        continue;
                    }

                    if (!child.IsVisible)
                    {
                        continue;
                    }

                    child.Y = nextY;
                    rowHeight = Math.Max(rowHeight, child.Height);
                    anyVisible = true;
                }

                if (anyVisible)
                {
                    nextY += rowHeight + rowGap;
                }
            }

            box.WantUpdateSize = true;
        }

        private void ReflowScrollAreaContent(ScrollArea scrollArea)
        {
            var rowOriginalYs = new List<int>();

            for (int i = 1; i < scrollArea.Children.Count; i++)
            {
                Control child = scrollArea.Children[i];

                if (child == null || child.IsDisposed)
                {
                    continue;
                }

                int origY = OptionsSearchOriginalY(child, _optionsSearchSavedY);

                if (!rowOriginalYs.Contains(origY))
                {
                    rowOriginalYs.Add(origY);
                }
            }

            int nextY = OptionsScrollContentPadding;
            const int rowGap = 2;

            for (int r = 0; r < rowOriginalYs.Count; r++)
            {
                int origRowY = rowOriginalYs[r];
                int rowHeight = 0;
                bool anyVisible = false;

                for (int i = 1; i < scrollArea.Children.Count; i++)
                {
                    Control child = scrollArea.Children[i];

                    if (child == null || child.IsDisposed)
                    {
                        continue;
                    }

                    if (OptionsSearchOriginalY(child, _optionsSearchSavedY) != origRowY)
                    {
                        continue;
                    }

                    if (!child.IsVisible)
                    {
                        continue;
                    }

                    child.Y = nextY;
                    rowHeight = Math.Max(rowHeight, child.Height);
                    anyVisible = true;
                }

                if (anyVisible)
                {
                    nextY += rowHeight + rowGap;
                }
            }

            scrollArea.WantUpdateSize = true;
        }

        private void ResetOptionsSearchScrollForActivePage()
        {
            if (ActivePage < 1 || ActivePage > 13)
            {
                return;
            }

            for (int i = 0; i < Children.Count; i++)
            {
                Control pageChild = Children[i];

                if (pageChild.Page != ActivePage)
                {
                    continue;
                }

                ResetOptionsSearchScroll(pageChild);
            }
        }

        private static void ResetOptionsSearchScroll(Control root)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (root is ScrollArea scrollArea
                && scrollArea.Children.Count > 0
                && scrollArea.Children[0] is ScrollBarBase scrollBar)
            {
                scrollBar.Value = scrollBar.MinValue;
            }

            for (int i = 0; i < root.Children.Count; i++)
            {
                ResetOptionsSearchScroll(root.Children[i]);
            }
        }

        private void ApplyOptionsSearchFilterToControl(Control root, bool filter, HashSet<Control> matches)
        {
            if (root is SettingsSection section)
            {
                section.ApplySearchFilter(filter, _optionsSearchAppliedTerm, matches);
                return;
            }

            for (int i = 0; i < root.Children.Count; i++)
            {
                ApplyOptionsSearchFilterToControl(root.Children[i], filter, matches);
            }

            if (!IsOptionsSearchableControl(root))
            {
                return;
            }

            if (!filter)
            {
                root.IsVisible = true;
                return;
            }

            bool visible = matches.Contains(root);

            if (visible && root.Parent is DataBox dataBox)
            {
                ShowOptionsSearchRowPeers(dataBox, root);
            }

            root.IsVisible = visible;
        }

        private static void ShowOptionsSearchRowPeers(DataBox box, Control matched)
        {
            int rowY = matched.Y;

            for (int i = 0; i < box.Children.Count; i++)
            {
                Control peer = box.Children[i];

                if (peer.Y == rowY)
                {
                    peer.IsVisible = true;
                }
            }
        }

        private static ScrollArea FindOptionsSearchScrollParent(Control c)
        {
            for (Control p = c?.Parent; p != null; p = p.Parent)
            {
                if (p is ScrollArea sa)
                {
                    return sa;
                }
            }

            return null;
        }

        private bool OptionsSearchHighlightVisible(Control c, int gx, int gy, int bw, int bh)
        {
            ScrollArea sa = FindOptionsSearchScrollParent(c);

            if (sa == null)
            {
                return true;
            }

            int top = sa.ScissorRectangle.Y;
            int bottom = top + sa.ScissorRectangle.Height;
            int itemTop = gy;
            int itemBottom = gy + bh;

            return itemBottom >= top && itemTop <= bottom;
        }

        private static void DrawOptionsSearchHighlightBorder(UltimaBatcher2D batcher, Texture2D tex, Vector3 hueVec, int x, int y, int w, int h, int t)
        {
            batcher.Draw(tex, new Rectangle(x, y, w, t), hueVec);
            batcher.Draw(tex, new Rectangle(x, y + h - t, w, t), hueVec);
            batcher.Draw(tex, new Rectangle(x, y, t, h), hueVec);
            batcher.Draw(tex, new Rectangle(x + w - t, y, t, h), hueVec);
        }

        private void BuildMods()
        {
            const int PAGE = 13;

            ScrollArea rightArea = new ScrollArea(190, OptionsScrollY, WIDTH - 210, OptionsScrollHeight, true)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            int startX = 5;
            int startY = OptionsScrollContentPadding;

            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1) { WantUpdateSize = true };
            rightArea.Add(box);

            SettingsSection movement = AddSettingsSection(box, "Movement");
            movement.Add(_avoidObstacles = AddCheckBox(null, "Auto avoid obstacles", _currentProfile.AvoidObstacles, startX, startY));
            movement.AddRight(_avoidObstaclesIgnoreHumanoids = AddCheckBox(null, "Ignore characters when avoiding", _currentProfile.AvoidObstaclesIgnoreHumanoids, startX, startY));
            movement.Add(_forceGargoyleWalk = AddCheckBox(null, ResGumps.ForceGargoyleWalk, _currentProfile.ForceGargoyleWalk, startX, startY));

            SettingsSection combat = AddSettingsSection(box, "Combat & Visual");
            combat.Y = movement.Bounds.Bottom + OptionsSectionGap;
            combat.Add(_useOldHealthBars = AddCheckBox(null, "Old style bars (HP/Mana/Stam)", _currentProfile.UseOldHealthBars, startX, startY));
            combat.Add(_showTargetRangeIndicator = AddCheckBox(null, ResGumps.ShowTarRangeIndic, _currentProfile.ShowTargetRangeIndicator, startX, startY));
            combat.Add(_ltHighlightRangeOnActivated = AddCheckBox(null, "Highlight tiles on range", _currentProfile.LTHighlightRangeOnActivated, startX, startY));
            combat.Add(AddLabel(null, "Range", startX, startY));
            combat.AddRight(_ltHighlightRangeOnActivatedRange = AddHSlider(null, 1, 18, _currentProfile.LTHighlightRangeOnActivatedRange, startX, startY, 150));
            combat.Add(AddLabel(null, "Tile color", startX, startY));
            combat.AddRight(_ltHighlightRangeOnActivatedHue = AddColorBox(null, startX, startY, _currentProfile.LTHighlightRangeOnActivatedHue, string.Empty), 2);
            combat.Add(_ltHighlightRangeOnCast = AddCheckBox(null, "Highlight tiles on range for spells", _currentProfile.LTHighlightRangeOnCast, startX, startY));
            combat.Add(AddLabel(null, "Range", startX, startY));
            combat.AddRight(_ltHighlightRangeOnCastRange = AddHSlider(null, 1, 18, _currentProfile.LTHighlightRangeOnCastRange, startX, startY, 150));
            combat.Add(AddLabel(null, "Tile color", startX, startY));
            combat.AddRight(_ltHighlightRangeOnCastHue = AddColorBox(null, startX, startY, _currentProfile.LTHighlightRangeOnCastHue, string.Empty), 2);
            combat.Add(AddLabel(null, "Outline width (pixels)", startX, startY));
            combat.AddRight(_ltHighlightRangeOutlinePixels = AddHSlider(null, 1, 10, _currentProfile.LTHighlightRangeOutlinePixels, startX, startY, 150));
            combat.Add(_highlightMirrorClones = AddCheckBox(null, "Ghost mirror image clones", _currentProfile.HighlightMirrorImageClones, startX, startY));
            combat.AddRight(_mirrorCloneHue = AddColorBox(null, startX, startY, _currentProfile.MirrorImageCloneHue, string.Empty), 2);
            combat.Add(_showBandageRingTimer = AddCheckBox(null, "Show Timer Countdown", _currentProfile.ShowBandageRingTimer, startX, startY));
            combat.Add(AddLabel(null, "X", startX, startY));
            combat.AddRight(
                _bandageRingTimerX = AddInputField(
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    false
                ),
                4
            );
            _bandageRingTimerX.SetText(_currentProfile.BandageRingTimerX < 0 ? string.Empty : _currentProfile.BandageRingTimerX.ToString());
            combat.AddRight(AddLabel(null, "Y", startX, startY), 2);
            combat.AddRight(
                _bandageRingTimerY = AddInputField(
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    false
                )
            );
            _bandageRingTimerY.SetText(_currentProfile.BandageRingTimerY < 0 ? string.Empty : _currentProfile.BandageRingTimerY.ToString());
            NiceButton resetTimerPosButton = new NiceButton(0, 0, 110, 22, ButtonAction.Activate, "Reset Position")
            {
                IsSelectable = false
            };
            resetTimerPosButton.MouseUp += (_, _) =>
            {
                _bandageRingTimerX?.SetText(string.Empty);
                _bandageRingTimerY?.SetText(string.Empty);
                if (_currentProfile != null)
                {
                    _currentProfile.BandageRingTimerX = -1;
                    _currentProfile.BandageRingTimerY = -1;
                }
            };
            combat.AddRight(resetTimerPosButton, 6);
            combat.AddRight(AddLabel(null, "(under player)", startX, startY), 4);

            SettingsSection visualHelpers = AddSettingsSection(box, "Visual Helpers");
            visualHelpers.Y = combat.Bounds.Bottom + OptionsSectionGap;

            visualHelpers.Add(_nameOverheadAlwaysOn = AddCheckBox(null, "Always show name overheads", _currentProfile.NameOverheadToggled, startX, startY));

            visualHelpers.Add(AddLabel(null, "Glowing Weapons", startX, startY));
            visualHelpers.AddRight(_glowingWeaponsType = AddCombobox(null, VisualHighlightModes, _currentProfile.GlowingWeaponsType, startX, startY, 100), 2);
            visualHelpers.Add(AddLabel(null, "Custom color", startX, startY));
            visualHelpers.AddRight(_highlightGlowingWeaponsTypeHue = AddColorBox(null, startX, startY, _currentProfile.HighlightGlowingWeaponsTypeHue, string.Empty), 2);

            visualHelpers.Add(AddLabel(null, "Highlight lasttarget", startX, startY));
            visualHelpers.AddRight(_highlightLastTargetType = AddCombobox(null, VisualHighlightModes, _currentProfile.HighlightLastTargetType, startX, startY, 100), 2);
            visualHelpers.Add(AddLabel(null, "Custom color", startX, startY));
            visualHelpers.AddRight(_highlightLastTargetTypeHue = AddColorBox(null, startX, startY, _currentProfile.HighlightLastTargetTypeHue, string.Empty), 2);

            visualHelpers.Add(AddLabel(null, "Highlight Friends Guild Mobiles", startX, startY));
            visualHelpers.AddRight(_highlighFriendsGuildType = AddCombobox(null, VisualHighlightModes, _currentProfile.HighlighFriendsGuildType, startX, startY, 100), 2);
            visualHelpers.Add(AddLabel(null, "Custom color", startX, startY));
            visualHelpers.AddRight(_highlighFriendsGuildTypeHue = AddColorBox(null, startX, startY, _currentProfile.HighlighFriendsGuildTypeHue, string.Empty), 2);

            visualHelpers.Add(AddLabel(null, "Highlight lasttarget poisoned", startX, startY));
            visualHelpers.AddRight(_highlightLastTargetTypePoison = AddCombobox(null, VisualHighlightStateModes, _currentProfile.HighlightLastTargetTypePoison, startX, startY, 100), 2);
            visualHelpers.Add(AddLabel(null, "Custom color", startX, startY));
            visualHelpers.AddRight(_highlightLastTargetTypePoisonHue = AddColorBox(null, startX, startY, _currentProfile.HighlightLastTargetTypePoisonHue, string.Empty), 2);

            visualHelpers.Add(AddLabel(null, "Highlight lasttarget paralyzed", startX, startY));
            visualHelpers.AddRight(_highlightLastTargetTypePara = AddCombobox(null, VisualHighlightStateModes, _currentProfile.HighlightLastTargetTypePara, startX, startY, 100), 2);
            visualHelpers.Add(AddLabel(null, "Custom color", startX, startY));
            visualHelpers.AddRight(_highlightLastTargetTypeParaHue = AddColorBox(null, startX, startY, _currentProfile.HighlightLastTargetTypeParaHue, string.Empty), 2);

            visualHelpers.Add(AddLabel(null, "Highlight lasttarget stunned", startX, startY));
            visualHelpers.AddRight(_highlightLastTargetTypeStunned = AddCombobox(null, VisualHighlightStateModes, _currentProfile.HighlightLastTargetTypeStunned, startX, startY, 100), 2);
            visualHelpers.Add(AddLabel(null, "Custom color", startX, startY));
            visualHelpers.AddRight(_highlightLastTargetTypeStunnedHue = AddColorBox(null, startX, startY, _currentProfile.HighlightLastTargetTypeStunnedHue, string.Empty), 2);

            visualHelpers.Add(AddLabel(null, "Highlight lasttarget mortalled (yellow hits)", startX, startY));
            visualHelpers.AddRight(_highlightLastTargetTypeMortalled = AddCombobox(null, VisualHighlightStateModes, _currentProfile.HighlightLastTargetTypeMortalled, startX, startY, 100), 2);
            visualHelpers.Add(AddLabel(null, "Custom color", startX, startY));
            visualHelpers.AddRight(_highlightLastTargetTypeMortalledHue = AddColorBox(null, startX, startY, _currentProfile.HighlightLastTargetTypeMortalledHue, string.Empty), 2);
            visualHelpers.Add(
                _energyFieldWallOfStoneAutoAvoid = AddCheckBox(
                    null,
                    OptionsLocalizedLabel(
                        "EnergyField come WallofStone auto-avoid",
                        "EnergyField WallofStone-style auto-avoid"
                    ),
                    _currentProfile.BlockEnergyFArtForceAoS,
                    startX,
                    startY
                )
            );
            // Extra breathing room above the Energy Field checkbox so it doesn't crowd the
            // highlight rows above it.
            _energyFieldWallOfStoneAutoAvoid.Y += VisualHelpersEnergyFieldTopGap;

            visualHelpers.Add(
                _hideCarpets = AddCheckBox(
                    null,
                    ResGumps.HideCarpets,
                    _currentProfile.HideCarpets,
                    startX,
                    startY
                )
            );
            visualHelpers.SyncContentHeight();

            SettingsSection connections = AddSettingsSection(box, "Connections");
            connections.Y = visualHelpers.Bounds.Bottom + OptionsSectionGapLarge;
            connections.Add(_fastRotation = AddCheckBox(null, "Fast Rotation", _currentProfile.FastRotation, startX, startY));
            connections.Add(AddLabel(null, "Movement turn delay (ms)", startX, startY));
            connections.AddRight
            (
                _movementTurnDelay = AddInputField
                (
                    null,
                    startX,
                    startY,
                    70,
                    TEXTBOX_HEIGHT,
                    numbersOnly: true,
                    maxCharCount: 4
                ),
                2
            );
            _movementTurnDelay.SetText(Math.Clamp(_currentProfile.MovementTurnDelay, 40, 1000).ToString());

            connections.Add(AddLabel(null, "Movement fast turn delay (ms)", startX, startY));
            connections.AddRight
            (
                _movementTurnDelayFast = AddInputField
                (
                    null,
                    startX,
                    startY,
                    70,
                    TEXTBOX_HEIGHT,
                    numbersOnly: true,
                    maxCharCount: 4
                ),
                2
            );
            _movementTurnDelayFast.SetText(Math.Clamp(_currentProfile.MovementTurnDelayFast, 40, 1000).ToString());

            connections.Add(AddLabel(null, "Movement walking delay (ms)", startX, startY));
            connections.AddRight
            (
                _movementWalkingDelay = AddInputField
                (
                    null,
                    startX,
                    startY,
                    70,
                    TEXTBOX_HEIGHT,
                    numbersOnly: true,
                    maxCharCount: 4
                ),
                2
            );
            _movementWalkingDelay.SetText(Math.Clamp(_currentProfile.MovementWalkingDelay, 40, 1000).ToString());

            connections.Add(AddLabel(null, "Movement player walking delay (ms)", startX, startY));
            connections.AddRight
            (
                _movementPlayerWalkingDelay = AddInputField
                (
                    null,
                    startX,
                    startY,
                    70,
                    TEXTBOX_HEIGHT,
                    numbersOnly: true,
                    maxCharCount: 4
                ),
                2
            );
            _movementPlayerWalkingDelay.SetText(Math.Clamp(_currentProfile.MovementPlayerWalkingDelay, 40, 1000).ToString());

            // Dust765-Light has no Connections presets; hardcoded Constants are 80/45/150/150.
            // Balanced turn = 100 (full Dust / user request). Reset = Light Constants (80).
            // Low Ping / High Jitter = UODreams PVP helpers only.
            NiceButton lowPingPresetButton = new NiceButton(startX, startY, 90, 22, ButtonAction.Activate, "Low Ping")
            {
                IsSelectable = false
            };
            lowPingPresetButton.MouseUp += (_, _) => SetMovementDelayInputs(93, 44, 138, 138);
            connections.Add(lowPingPresetButton);

            NiceButton balancedPresetButton = new NiceButton(0, 0, 90, 22, ButtonAction.Activate, "Balanced")
            {
                IsSelectable = false
            };
            balancedPresetButton.MouseUp += (_, _) => SetMovementDelayInputs(
                100,
                Constants.TURN_DELAY_FAST,
                Constants.WALKING_DELAY,
                Constants.PLAYER_WALKING_DELAY);
            connections.Add(balancedPresetButton);

            NiceButton highJitterPresetButton = new NiceButton(0, 0, 100, 22, ButtonAction.Activate, "High Jitter")
            {
                IsSelectable = false
            };
            highJitterPresetButton.MouseUp += (_, _) => SetMovementDelayInputs(140, 70, 220, 220);
            connections.Add(highJitterPresetButton);

            NiceButton resetPresetButton = new NiceButton(0, 0, 70, 22, ButtonAction.Activate, "Reset")
            {
                IsSelectable = false
            };
            resetPresetButton.MouseUp += (_, _) => SetMovementDelayInputs(
                Constants.TURN_DELAY,
                Constants.TURN_DELAY_FAST,
                Constants.WALKING_DELAY,
                Constants.PLAYER_WALKING_DELAY);
            connections.Add(resetPresetButton);

            SettingsSection network = AddSettingsSection(box, "Network");
            network.Y = connections.Bounds.Bottom + 40;
            network.Add(_enableUoDreamsNetworkOptimizer = AddCheckBox(null, "UODreams network optimizer (stone-roof tooltips)", _currentProfile.EnableUoDreamsNetworkOptimizer, startX, startY));
            network.Add(_enableFullSocketDrain = AddCheckBox(null, "Full socket drain each frame", _currentProfile.EnableFullSocketDrain, startX, startY));

            SettingsSection ui = AddSettingsSection(box, "UI");
            ui.Y = network.Bounds.Bottom + 40;
            ui.Add(_showEquipmentDurabilityButton = AddCheckBox(null, "Paperdoll durability button", _currentProfile.ShowEquipmentDurabilityButton, startX, startY));
            ui.Add(_useModernJournal = AddCheckBox(null, "Modern journal (tabs)", _currentProfile.UseModernJournal, startX, startY));
            ui.Add(_hideJournalTimestamp = AddCheckBox(null, "Hide journal timestamps", _currentProfile.HideJournalTimestamp, startX, startY));
            ui.Add(_showAllLayersPaperdoll = AddCheckBox(null, "Show all equipment slots on paperdoll", _currentProfile.ShowAllLayersPaperdoll, startX, startY));
            ui.Add(_autoOpenUiOnLogin = AddCheckBox(null, "Auto-open UI on login (backpack, paperdoll, status)", _currentProfile.AutoOpenUiOnLogin, startX, startY));
            ui.Add(_hidePersistentNPCNames = AddCheckBox(null, "Hide persistent NPC names (vendors, mannequins, parrots, statues)", _currentProfile.HidePersistentNPCNames, startX, startY));

            SettingsSection world = AddSettingsSection(box, "World");
            world.Y = ui.Bounds.Bottom + 40;
            world.Add(_invisibleHousesEnabled = AddCheckBox(null, "Invisible houses (macro toggle)", _currentProfile.InvisibleHousesEnabled, startX, startY));
            world.Add(AddLabel(null, "Hide house tiles above Z offset", startX, startY));
            world.AddRight(_invisibleHousesZ = AddHSlider(null, -10, 20, _currentProfile.InvisibleHousesZ, startX, startY, 150));

            Add(rightArea, PAGE);
        }

        private static Texture2D LogoTexture
        {
            get
            {
                if (_logoTexture2D == null || _logoTexture2D.IsDisposed)
                {
                    using var stream = new MemoryStream(Loader.GetCuoLogo().ToArray());
                    _logoTexture2D = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _logoTexture2D;
            }
        }

        private void BuildGeneral()
        {
            const int PAGE = 1;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;


            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);


            SettingsSection section = AddSettingsSection(box, "General");


            section.Add
            (
                _highlightObjects = AddCheckBox
                (
                    null,
                    ResGumps.HighlightObjects,
                    _currentProfile.HighlightGameObjects,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _enablePathfind = AddCheckBox
                (
                    null,
                    ResGumps.EnablePathfinding,
                    _currentProfile.EnablePathfind,
                    startX,
                    startY
                )
            );

            section.AddRight
            (
                _useShiftPathfind = AddCheckBox
                (
                    null,
                    ResGumps.ShiftPathfinding,
                    _currentProfile.UseShiftToPathfind,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _alwaysRun = AddCheckBox
                (
                    null,
                    ResGumps.AlwaysRun,
                    _currentProfile.AlwaysRun,
                    startX,
                    startY
                )
            );

            section.AddRight
            (
                _alwaysRunUnlessHidden = AddCheckBox
                (
                    null,
                    ResGumps.AlwaysRunHidden,
                    _currentProfile.AlwaysRunUnlessHidden,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _autoOpenDoors = AddCheckBox
                (
                    null,
                    ResGumps.AutoOpenDoors,
                    _currentProfile.AutoOpenDoors,
                    startX,
                    startY
                )
            );

            section.AddRight
            (
                _smoothDoors = AddCheckBox
                (
                    null,
                    ResGumps.SmoothDoors,
                    _currentProfile.SmoothDoors,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _autoOpenCorpse = AddCheckBox
                (
                    null,
                    ResGumps.AutoOpenCorpses,
                    _currentProfile.AutoOpenCorpses,
                    startX,
                    startY
                )
            );

            section.PushIndent();
            section.Add(AddLabel(null, ResGumps.CorpseOpenRange, 0, 0));

            section.AddRight
            (
                _autoOpenCorpseRange = AddInputField
                (
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    ResGumps.CorpseOpenRange,
                    50,
                    false,
                    true,
                    5
                )
            );

            _autoOpenCorpseRange.SetText(_currentProfile.AutoOpenCorpseRange.ToString());

            section.Add
            (
                _skipEmptyCorpse = AddCheckBox
                (
                    null,
                    ResGumps.SkipEmptyCorpses,
                    _currentProfile.SkipEmptyCorpse,
                    startX,
                    startY
                )
            );

            section.Add(AddLabel(null, ResGumps.CorpseOpenOptions, startX, startY));

            section.AddRight
            (
                _autoOpenCorpseOptions = AddCombobox
                (
                    null,
                    new[]
                    {
                        ResGumps.CorpseOpt_None, ResGumps.CorpseOpt_NotTar, ResGumps.CorpseOpt_NotHid,
                        ResGumps.CorpseOpt_Both
                    },
                    _currentProfile.CorpseOpenOptions,
                    startX,
                    startY,
                    150
                ),
                2
            );

            section.PopIndent();

            section.Add
            (
                _noColorOutOfRangeObjects = AddCheckBox
                (
                    rightArea,
                    ResGumps.OutOfRangeColor,
                    _currentProfile.NoColorObjectsOutOfRange,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _sallosEasyGrab = AddCheckBox
                (
                    null,
                    ResGumps.SallosEasyGrab,
                    _currentProfile.SallosEasyGrab,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _showHouseContent = AddCheckBox
                (
                    null,
                    ResGumps.ShowHousesContent,
                    _currentProfile.ShowHouseContent,
                    startX,
                    startY
                )
            );

            _showHouseContent.IsVisible = Client.Game.UO.Version >= ClientVersion.CV_70796;

            section.Add
            (
                _use_smooth_boat_movement = AddCheckBox
                (
                    null,
                    ResGumps.SmoothBoat,
                    _currentProfile.UseSmoothBoatMovement,
                    startX,
                    startY
                )
            );

            _use_smooth_boat_movement.IsVisible = Client.Game.UO.Version >= ClientVersion.CV_7090;


            SettingsSection section2 = AddSettingsSection(box, "Mobiles");
            section2.Y = section.Bounds.Bottom + 40;

            section2.Add
            (
                _showHpMobile = AddCheckBox
                (
                    null,
                    ResGumps.ShowHP,
                    _currentProfile.ShowMobilesHP,
                    startX,
                    startY
                )
            );

            int mode = _currentProfile.MobileHPType;

            if (mode < 0 || mode > 2)
            {
                mode = 0;
            }

            section2.AddRight
            (
                _hpComboBox = AddCombobox
                (
                    null,
                    new[] { ResGumps.HP_Percentage, ResGumps.HP_Line, ResGumps.HP_Both },
                    mode,
                    startX,
                    startY,
                    100
                )
            );

            section2.AddRight(AddLabel(null, ResGumps.HP_Mode, startX, startY));

            mode = _currentProfile.MobileHPShowWhen;

            if (mode != 0 && mode > 2)
            {
                mode = 0;
            }

            section2.AddRight
            (
                _hpComboBoxShowWhen = AddCombobox
                (
                    null,
                    new[] { ResGumps.HPShow_Always, ResGumps.HPShow_Less, ResGumps.HPShow_Smart },
                    mode,
                    startX,
                    startY,
                    100
                ),
                2
            );

            section2.Add
            (
                _highlightByPoisoned = AddCheckBox
                (
                    null,
                    ResGumps.HighlightPoisoned,
                    _currentProfile.HighlightMobilesByPoisoned,
                    startX,
                    startY
                )
            );

            section2.PushIndent();

            section2.Add
            (
                _poisonColorPickerBox = AddColorBox
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.PoisonHue,
                    ResGumps.PoisonedColor
                )
            );

            section2.AddRight(AddLabel(null, ResGumps.PoisonedColor, 0, 0), 2);
            section2.PopIndent();

            section2.Add
            (
                _highlightByParalyzed = AddCheckBox
                (
                    null,
                    ResGumps.HighlightParalyzed,
                    _currentProfile.HighlightMobilesByParalize,
                    startX,
                    startY
                )
            );

            section2.PushIndent();

            section2.Add
            (
                _paralyzedColorPickerBox = AddColorBox
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.ParalyzedHue,
                    ResGumps.ParalyzedColor
                )
            );

            section2.AddRight(AddLabel(null, ResGumps.ParalyzedColor, 0, 0), 2);

            section2.PopIndent();

            section2.Add
            (
                _highlightByInvul = AddCheckBox
                (
                    null,
                    ResGumps.HighlightInvulnerable,
                    _currentProfile.HighlightMobilesByInvul,
                    startX,
                    startY
                )
            );

            section2.PushIndent();

            section2.Add
            (
                _invulnerableColorPickerBox = AddColorBox
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.InvulnerableHue,
                    ResGumps.InvulColor
                )
            );

            section2.AddRight(AddLabel(null, ResGumps.InvulColor, 0, 0), 2);
            section2.PopIndent();

            section2.Add
            (
                _showMobileNameIncoming = AddCheckBox
                (
                    null,
                    ResGumps.ShowIncMobiles,
                    _currentProfile.ShowNewMobileNameIncoming,
                    startX,
                    startY
                )
            );

            section2.Add
            (
                _showCorpseNameIncoming = AddCheckBox
                (
                    null,
                    ResGumps.ShowIncCorpses,
                    _currentProfile.ShowNewCorpseNameIncoming,
                    startX,
                    startY
                )
            );

            section2.Add(AddLabel(null, ResGumps.AuraUnderFeet, startX, startY));

            section2.AddRight
            (
                _auraType = AddCombobox
                (
                    null,
                    new[]
                    {
                        ResGumps.AuraType_None, ResGumps.AuraType_Warmode, ResGumps.AuraType_CtrlShift,
                        ResGumps.AuraType_Always
                    },
                    _currentProfile.AuraUnderFeetType,
                    startX,
                    startY,
                    100
                ),
                2
            );

            section2.PushIndent();

            section2.Add
            (
                _partyAura = AddCheckBox
                (
                    null,
                    ResGumps.CustomColorAuraForPartyMembers,
                    _currentProfile.PartyAura,
                    startX,
                    startY
                )
            );

            section2.PushIndent();

            section2.Add
            (
                _partyAuraColorPickerBox = AddColorBox
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.PartyAuraHue,
                    ResGumps.PartyAuraColor
                )
            );

            section2.AddRight(AddLabel(null, ResGumps.PartyAuraColor, 0, 0));
            section2.PopIndent();
            section2.PopIndent();

            SettingsSection section3 = AddSettingsSection(box, "Gumps & Context");
            section3.Y = section2.Bounds.Bottom + 40;

            section3.Add
            (
                _enableTopbar = AddCheckBox
                (
                    null,
                    ResGumps.DisableMenu,
                    _currentProfile.TopbarGumpIsDisabled,
                    0,
                    0
                )
            );

            section3.Add
            (
                _holdDownKeyAlt = AddCheckBox
                (
                    null,
                    ResGumps.AltCloseGumps,
                    _currentProfile.HoldDownKeyAltToCloseAnchored,
                    0,
                    0
                )
            );

            section3.Add
            (
                _holdAltToMoveGumps = AddCheckBox
                (
                    null,
                    ResGumps.AltMoveGumps,
                    _currentProfile.HoldAltToMoveGumps,
                    0,
                    0
                )
            );

            section3.Add
            (
                _closeAllAnchoredGumpsWithRClick = AddCheckBox
                (
                    null,
                    ResGumps.ClickCloseAllGumps,
                    _currentProfile.CloseAllAnchoredGumpsInGroupWithRightClick,
                    0,
                    0
                )
            );

            section3.Add
            (
                _useStandardSkillsGump = AddCheckBox
                (
                    null,
                    ResGumps.StandardSkillGump,
                    _currentProfile.StandardSkillsGump,
                    0,
                    0
                )
            );

            section3.Add
            (
                _use_old_status_gump = AddCheckBox
                (
                    null,
                    ResGumps.UseOldStatusGump,
                    _currentProfile.UseOldStatusGump,
                    startX,
                    startY
                )
            );

            _use_old_status_gump.IsVisible = !CUOEnviroment.IsOutlands;

            section3.Add
            (
                _partyInviteGump = AddCheckBox
                (
                    null,
                    ResGumps.ShowGumpPartyInv,
                    _currentProfile.PartyInviteGump,
                    0,
                    0
                )
            );

            section3.Add
            (
                _customBars = AddCheckBox
                (
                    null,
                    ResGumps.UseCustomHPBars,
                    _currentProfile.CustomBarsToggled,
                    0,
                    0
                )
            );

            section3.AddRight
            (
                _customBarsBBG = AddCheckBox
                (
                    null,
                    ResGumps.UseBlackBackgr,
                    _currentProfile.CBBlackBGToggled,
                    0,
                    0
                )
            );

            section3.Add
            (
                _saveHealthbars = AddCheckBox
                (
                    null,
                    ResGumps.SaveHPBarsOnLogout,
                    _currentProfile.SaveHealthbars,
                    0,
                    0
                )
            );

            section3.PushIndent();
            section3.Add(AddLabel(null, ResGumps.CloseHPGumpWhen, 0, 0));

            mode = _currentProfile.CloseHealthBarType;

            if (mode < 0 || mode > 2)
            {
                mode = 0;
            }

            _healtbarType = AddCombobox
            (
                null,
                new[] { ResGumps.HPType_None, ResGumps.HPType_MobileOOR, ResGumps.HPType_MobileDead },
                mode,
                0,
                0,
                150
            );

            section3.AddRight(_healtbarType);
            section3.PopIndent();
            section3.Add(AddLabel(null, ResGumps.GridLoot, startX, startY));

            section3.AddRight
            (
                _gridLoot = AddCombobox
                (
                    null,
                    new[] { ResGumps.GridLoot_None, ResGumps.GridLoot_GridOnly, ResGumps.GridLoot_Both },
                    _currentProfile.GridLootType,
                    startX,
                    startY,
                    120
                ),
                2
            );

            section3.Add
            (
                _holdShiftForContext = AddCheckBox
                (
                    null,
                    ResGumps.ShiftContext,
                    _currentProfile.HoldShiftForContext,
                    0,
                    0
                )
            );

            section3.Add
            (
                _holdShiftToSplitStack = AddCheckBox
                (
                    null,
                    ResGumps.ShiftStack,
                    _currentProfile.HoldShiftToSplitStack,
                    0,
                    0
                )
            );


            SettingsSection section4 = AddSettingsSection(box, "Miscellaneous");
            section4.Y = section3.Bounds.Bottom + 40;

            section4.Add
            (
                _useCircleOfTransparency = AddCheckBox
                (
                    null,
                    ResGumps.EnableCircleTrans,
                    _currentProfile.UseCircleOfTransparency,
                    startX,
                    startY
                )
            );

            section4.AddRight
            (
                _circleOfTranspRadius = AddHSlider
                (
                    null,
                    Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    _currentProfile.CircleOfTransparencyRadius,
                    startX,
                    startY,
                    200
                )
            );

            section4.PushIndent();
            section4.Add(AddLabel(null, ResGumps.CircleTransType, startX, startY));
            int cottypeindex = _currentProfile.CircleOfTransparencyType;
            string[] cotTypes = { ResGumps.CircleTransType_Full, ResGumps.CircleTransType_Gradient };

            if (cottypeindex < 0 || cottypeindex > cotTypes.Length)
            {
                cottypeindex = 0;
            }

            section4.AddRight
            (
                _cotType = AddCombobox
                (
                    null,
                    cotTypes,
                    cottypeindex,
                    startX,
                    startY,
                    150
                ),
                2
            );

            section4.PopIndent();

            section4.Add
            (
                _hideScreenshotStoredInMessage = AddCheckBox
                (
                    null,
                    ResGumps.HideScreenshotStoredInMessage,
                    _currentProfile.HideScreenshotStoredInMessage,
                    0,
                    0
                )
            );

            section4.Add
            (
                _objectsFading = AddCheckBox
                (
                    null,
                    ResGumps.ObjAlphaFading,
                    _currentProfile.UseObjectsFading,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _textFading = AddCheckBox
                (
                    null,
                    ResGumps.TextAlphaFading,
                    _currentProfile.TextFading,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _enableDragSelect = AddCheckBox
                (
                    null,
                    ResGumps.EnableDragHPBars,
                    _currentProfile.EnableDragSelect,
                    startX,
                    startY
                )
            );

            section4.PushIndent();
            section4.Add(AddLabel(null, ResGumps.DragKey, startX, startY));

            section4.AddRight
            (
                _dragSelectModifierKey = AddCombobox
                (
                    null,
                    new[] { ResGumps.KeyMod_None, ResGumps.KeyMod_Ctrl, ResGumps.KeyMod_Shift },
                    _currentProfile.DragSelectModifierKey,
                    startX,
                    startY,
                    100
                )
            );

            section4.Add
            (
                _dragSelectHumanoidsOnly = AddCheckBox
                (
                    null,
                    ResGumps.DragHumanoidsOnly,
                    _currentProfile.DragSelectHumanoidsOnly,
                    startX,
                    startY
                )
            );

            section4.Add(new Label(ResGumps.DragSelectStartingPosX, true, HUE_FONT));
            section4.Add(_dragSelectStartX = new HSliderBar(startX, startY, 200, 0, Client.Game.Scene.Camera.Bounds.Width, _currentProfile.DragSelectStartX, HSliderBarStyle.MetalWidgetRecessedBar, true, 0, HUE_FONT));

            section4.Add(new Label(ResGumps.DragSelectStartingPosY, true, HUE_FONT));
            section4.Add(_dragSelectStartY = new HSliderBar(startX, startY, 200, 0, Client.Game.Scene.Camera.Bounds.Height, _currentProfile.DragSelectStartY, HSliderBarStyle.MetalWidgetRecessedBar, true, 0, HUE_FONT));
            section4.Add
            (
                _dragSelectAsAnchor = AddCheckBox
                (
                    null, ResGumps.DragSelectAnchoredHB, _currentProfile.DragSelectAsAnchor, startX,
                    startY
                )
            );

            section4.PopIndent();

            section4.Add
            (
                _showStatsMessage = AddCheckBox
                (
                    null,
                    ResGumps.ShowStatsChangedMessage,
                    _currentProfile.ShowStatsChangedMessage,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _showSkillsMessage = AddCheckBox
                (
                    null,
                    ResGumps.ShowSkillsChangedMessageBy,
                    _currentProfile.ShowStatsChangedMessage,
                    startX,
                    startY
                )
            );

            section4.PushIndent();

            section4.AddRight
            (
                _showSkillsMessageDelta = AddHSlider
                (
                    null,
                    0,
                    100,
                    _currentProfile.ShowSkillsChangedDeltaValue,
                    startX,
                    startY,
                    150
                )
            );

            section4.PopIndent();


            SettingsSection section5 = AddSettingsSection(box, "Terrain & Statics");
            section5.Y = section4.Bounds.Bottom + 40;

            section5.Add
            (
                _drawRoofs = AddCheckBox
                (
                    null,
                    ResGumps.HideRoofTiles,
                    !_currentProfile.DrawRoofs,
                    startX,
                    startY
                )
            );

            section5.Add
            (
                _treeToStumps = AddCheckBox
                (
                    null,
                    ResGumps.TreesStumps,
                    _currentProfile.TreeToStumps,
                    startX,
                    startY
                )
            );

            section5.Add
            (
                _hideVegetation = AddCheckBox
                (
                    null,
                    ResGumps.HideVegetation,
                    _currentProfile.HideVegetation,
                    startX,
                    startY
                )
            );

            section5.Add
            (
                _enableCaveBorder = AddCheckBox
                (
                    null,
                    ResGumps.MarkCaveTiles,
                    _currentProfile.EnableCaveBorder,
                    startX,
                    startY
                )
            );

            section5.Add(AddLabel(null, ResGumps.HPFields, startX, startY));
            mode = _currentProfile.FieldsType;

            if (mode < 0 || mode > 2)
            {
                mode = 0;
            }

            section5.AddRight
            (
                _fieldsType = AddCombobox
                (
                    null,
                    new[] { ResGumps.HPFields_Normal, ResGumps.HPFields_Static, ResGumps.HPFields_Tile },
                    mode,
                    startX,
                    startY,
                    150
                )
            );

            Add(rightArea, PAGE);
        }

        private void BuildSounds()
        {
            const int PAGE = 2;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;

            const int VOLUME_WIDTH = 200;

            _enableSounds = AddCheckBox
            (
                rightArea,
                ResGumps.Sounds,
                _currentProfile.EnableSound,
                startX,
                startY
            );

            _enableMusic = AddCheckBox
            (
                rightArea,
                ResGumps.Music,
                _currentProfile.EnableMusic,
                startX,
                startY + _enableSounds.Height + 2
            );

            _loginMusic = AddCheckBox
            (
                rightArea,
                ResGumps.LoginMusic,
                Settings.GlobalSettings.LoginMusic,
                startX,
                startY + _enableSounds.Height + 2 + _enableMusic.Height + 2
            );

            startX = 120;
            startY += 2;

            _soundsVolume = AddHSlider
            (
                rightArea,
                0,
                100,
                _currentProfile.SoundVolume,
                startX,
                startY,
                VOLUME_WIDTH
            );

            _musicVolume = AddHSlider
            (
                rightArea,
                0,
                100,
                _currentProfile.MusicVolume,
                startX,
                startY + _enableSounds.Height + 2,
                VOLUME_WIDTH
            );

            _loginMusicVolume = AddHSlider
            (
                rightArea,
                0,
                100,
                Settings.GlobalSettings.LoginMusicVolume,
                startX,
                startY + _enableSounds.Height + 2 + _enableMusic.Height + 2,
                VOLUME_WIDTH
            );

            startX = 5;
            startY += _loginMusic.Bounds.Bottom + 2;

            _footStepsSound = AddCheckBox
            (
                rightArea,
                ResGumps.PlayFootsteps,
                _currentProfile.EnableFootstepsSound,
                startX,
                startY
            );

            startY += _footStepsSound.Height + 2;

            _combatMusic = AddCheckBox
            (
                rightArea,
                ResGumps.CombatMusic,
                _currentProfile.EnableCombatMusic,
                startX,
                startY
            );

            startY += _combatMusic.Height + 2;

            _musicInBackground = AddCheckBox
            (
                rightArea,
                ResGumps.ReproduceSoundsAndMusic,
                _currentProfile.ReproduceSoundsInBackground,
                startX,
                startY
            );

            startY += _musicInBackground.Height + 2;

            Add(rightArea, PAGE);
        }

        private void BuildVideo()
        {
            const int PAGE = 3;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;


            Label text = AddLabel(rightArea, ResGumps.FPS, startX, startY);
            startX += text.Bounds.Right + 5;

            _sliderFPS = AddHSlider
            (
                rightArea,
                Constants.MIN_FPS,
                Constants.MAX_FPS,
                Settings.GlobalSettings.FPS,
                startX,
                startY,
                250
            );

            startY += text.Bounds.Bottom + 5;

            _reduceFPSWhenInactive = AddCheckBox
            (
                rightArea,
                ResGumps.FPSInactive,
                _currentProfile.ReduceFPSWhenInactive,
                startX,
                startY
            );

            startY += _reduceFPSWhenInactive.Height + 2;

            startX = 5;
            startY += 20;


            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);

            SettingsSection section = AddSettingsSection(box, "Game window");

            section.Add
            (
                _gameWindowFullsize = AddCheckBox
                (
                    null,
                    ResGumps.AlwaysUseFullsizeGameWindow,
                    _currentProfile.GameWindowFullSize,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _windowBorderless = AddCheckBox
                (
                    null,
                    ResGumps.BorderlessWindow,
                    _currentProfile.WindowBorderless,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _gameWindowLock = AddCheckBox
                (
                    null,
                    ResGumps.LockGameWindowMovingResizing,
                    _currentProfile.GameWindowLock,
                    startX,
                    startY
                )
            );

            section.Add(AddLabel(null, ResGumps.GamePlayWindowPosition, startX, startY));

            section.AddRight
            (
                _gameWindowPositionX = AddInputField
                (
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    true
                ),
                4
            );

            var camera = Client.Game.Scene.Camera;

            _gameWindowPositionX.SetText(camera.Bounds.X.ToString());

            section.AddRight
            (
                _gameWindowPositionY = AddInputField
                (
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    true
                )
            );

            _gameWindowPositionY.SetText(camera.Bounds.Y.ToString());


            section.Add(AddLabel(null, ResGumps.GamePlayWindowSize, startX, startY));

            section.AddRight
            (
                _gameWindowWidth = AddInputField
                (
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    true
                )
            );

            _gameWindowWidth.SetText(camera.Bounds.Width.ToString());

            section.AddRight
            (
                _gameWindowHeight = AddInputField
                (
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    true
                )
            );

            _gameWindowHeight.SetText(camera.Bounds.Height.ToString());


            SettingsSection section2 = AddSettingsSection(box, "Zoom");
            section2.Y = section.Bounds.Bottom + 40;
            section2.Add(AddLabel(null, ResGumps.DefaultZoom, startX, startY));

            var cameraZoomCount = (int)((camera.ZoomMax - camera.ZoomMin) / camera.ZoomStep);
            var cameraZoomIndex = cameraZoomCount - (int)((camera.ZoomMax - camera.Zoom) / camera.ZoomStep);

            section2.AddRight
            (
                _sliderZoom = AddHSlider
                (
                    null,
                    0,
                    cameraZoomCount,
                    cameraZoomIndex,
                    startX,
                    startY,
                    100
                )
            );

            section2.Add
            (
                _zoomCheckbox = AddCheckBox
                (
                    null,
                    ResGumps.EnableMouseWheelForZoom,
                    _currentProfile.EnableMousewheelScaleZoom,
                    startX,
                    startY
                )
            );

            section2.Add
            (
                _restorezoomCheckbox = AddCheckBox
                (
                    null,
                    ResGumps.ReleasingCtrlRestoresScale,
                    _currentProfile.RestoreScaleAfterUnpressCtrl,
                    startX,
                    startY
                )
            );


            SettingsSection section3 = AddSettingsSection(box, "Lights");
            section3.Y = section2.Bounds.Bottom + 40;

            section3.Add
            (
                _altLights = AddCheckBox
                (
                    null,
                    ResGumps.AlternativeLights,
                    _currentProfile.UseAlternativeLights,
                    startX,
                    startY
                )
            );

            section3.Add
            (
                _enableLight = AddCheckBox
                (
                    null,
                    ResGumps.LightLevel,
                    _currentProfile.UseCustomLightLevel,
                    startX,
                    startY
                )
            );

            section3.AddRight
            (
                _lightBar = AddHSlider
                (
                    null,
                    0,
                    0x1E,
                    0x1E - _currentProfile.LightLevel,
                    startX,
                    startY,
                    250
                )
            );

            section3.Add(AddLabel(null, ResGumps.LightLevelType, startX, startY));

            section3.AddRight
            (
                _lightLevelType = AddCombobox
                (
                    null,
                    new[] { ResGumps.LightLevelTypeAbsolute, ResGumps.LightLevelTypeMinimum },
                    _currentProfile.LightLevelType,
                    startX,
                    startY,
                    150
                )
            );

            section3.Add
            (
                _darkNights = AddCheckBox
                (
                    null,
                    ResGumps.DarkNights,
                    _currentProfile.UseDarkNights,
                    startX,
                    startY
                )
            );

            section3.Add
            (
                _useColoredLights = AddCheckBox
                (
                    null,
                    ResGumps.UseColoredLights,
                    _currentProfile.UseColoredLights,
                    startX,
                    startY
                )
            );


            SettingsSection section4 = AddSettingsSection(box, "Misc");
            section4.Y = section3.Bounds.Bottom + 40;

            section4.Add
            (
                _enableDeathScreen = AddCheckBox
                (
                    null,
                    ResGumps.EnableDeathScreen,
                    _currentProfile.EnableDeathScreen,
                    startX,
                    startY
                )
            );

            section4.AddRight
            (
                _enableBlackWhiteEffect = AddCheckBox
                (
                    null,
                    ResGumps.BlackWhiteModeForDeadPlayer,
                    _currentProfile.EnableBlackWhiteEffect,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _runMouseInSeparateThread = AddCheckBox
                (
                    null,
                    ResGumps.RunMouseInASeparateThread,
                    Settings.GlobalSettings.RunMouseInASeparateThread,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _auraMouse = AddCheckBox
                (
                    null,
                    ResGumps.AuraOnMouseTarget,
                    _currentProfile.AuraOnMouse,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _animatedWaterEffect = AddCheckBox
                (
                    null,
                    ResGumps.AnimatedWaterEffect,
                    _currentProfile.AnimatedWaterEffect,
                    startX,
                    startY
                )
            );


            SettingsSection section5 = AddSettingsSection(box, "Shadows");
            section5.Y = section4.Bounds.Bottom + 40;

            section5.Add
            (
                _enableShadows = AddCheckBox
                (
                    null,
                    ResGumps.Shadows,
                    _currentProfile.ShadowsEnabled,
                    startX,
                    startY
                )
            );

            section5.PushIndent();

            section5.Add
            (
                _enableShadowsStatics = AddCheckBox
                (
                    null,
                    ResGumps.ShadowStatics,
                    _currentProfile.ShadowsStatics,
                    startX,
                    startY
                )
            );

            section5.PopIndent();

            section5.Add(AddLabel(null, ResGumps.TerrainShadowsLevel, startX, startY));
            section5.AddRight(_terrainShadowLevel = AddHSlider(null, Constants.MIN_TERRAIN_SHADOWS_LEVEL, Constants.MAX_TERRAIN_SHADOWS_LEVEL, _currentProfile.TerrainShadowsLevel, startX, startY, 200));

            Add(rightArea, PAGE);
        }


        private void BuildCommands()
        {
            const int PAGE = 4;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY + OptionsMacrosToolbarHeight,
                150,
                OptionsScrollHeight - OptionsMacrosToolbarHeight,
                true
            );

            Add
            (
                new Line
                (
                    190,
                    OptionsScrollY + OptionsMacrosToolbarHeight - 2,
                    150,
                    1,
                    Color.Gray.PackedValue
                ),
                PAGE
            );

            Add
            (
                new Line
                (
                    191 + 150,
                    OptionsTabStartY + 11,
                    1,
                    OptionsScrollY + OptionsScrollHeight - (OptionsTabStartY + 11),
                    Color.Gray.PackedValue
                ),
                PAGE
            );

            NiceButton addButton = new NiceButton
            (
                190,
                OptionsScrollY,
                130,
                20,
                ButtonAction.Activate,
                ResGumps.NewMacro
            ) { IsSelectable = false, ButtonParameter = (int) Buttons.NewMacro };

            Add(addButton, PAGE);

            NiceButton delButton = new NiceButton
            (
                190,
                OptionsScrollY + 32,
                130,
                20,
                ButtonAction.Activate,
                ResGumps.DeleteMacro
            ) { IsSelectable = false, ButtonParameter = (int) Buttons.DeleteMacro };

            Add(delButton, PAGE);


            int startX = 5;
            int startY = OptionsScrollContentPadding;

            DataBox databox = new DataBox(startX, startY, 1, 1);
            databox.WantUpdateSize = true;
            rightArea.Add(databox);


            addButton.MouseUp += (sender, e) =>
            {
                EntryDialog dialog = new EntryDialog
                (
                    World,
                    250,
                    150,
                    ResGumps.MacroName,
                    name =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            return;
                        }

                        MacroManager manager = World.Macros;

                        if (manager.FindMacro(name) != null)
                        {
                            return;
                        }

                        NiceButton nb;

                        databox.Add
                        (
                            nb = new NiceButton
                            (
                                0,
                                0,
                                130,
                                25,
                                ButtonAction.Activate,
                                name
                            )
                            {
                                ButtonParameter = (int) Buttons.Last + 1 + rightArea.Children.Count,
                                CanMove = true
                            }
                        );

                        databox.ReArrangeChildren();

                        nb.IsSelected = true;

                        _macroControl?.Dispose();

                        _macroControl = new MacroControl(this, name)
                        {
                            X = 400,
                            Y = OptionsScrollY
                        };

                        manager.PushToBack(_macroControl.Macro);

                        Add(_macroControl, PAGE);

                        nb.DragBegin += (sss, eee) =>
                        {
                            if (UIManager.DraggingControl != this || UIManager.MouseOverControl != sss)
                            {
                                return;
                            }

                            UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s._macro == _macroControl.Macro)?.Dispose();

                            MacroButtonGump macroButtonGump = new MacroButtonGump(World, _macroControl.Macro, Mouse.Position.X, Mouse.Position.Y);

                            macroButtonGump.X = Mouse.Position.X - (macroButtonGump.Width >> 1);
                            macroButtonGump.Y = Mouse.Position.Y - (macroButtonGump.Height >> 1);

                            UIManager.Add(macroButtonGump);

                            UIManager.AttemptDragControl(macroButtonGump, true);
                        };

                        nb.MouseUp += (sss, eee) =>
                        {
                            _macroControl?.Dispose();

                            _macroControl = new MacroControl(this, name)
                            {
                                X = 400,
                                Y = OptionsScrollY
                            };

                            Add(_macroControl, PAGE);
                        };
                    }
                )
                {
                    CanCloseWithRightClick = true
                };

                UIManager.Add(dialog);
            };

            delButton.MouseUp += (ss, ee) =>
            {
                NiceButton nb = databox.FindControls<NiceButton>().SingleOrDefault(a => a.IsSelected);

                if (nb != null)
                {
                    QuestionGump dialog = new QuestionGump
                    (
                        World,
                        ResGumps.MacroDeleteConfirmation,
                        b =>
                        {
                            if (!b)
                            {
                                return;
                            }

                            if (_macroControl != null)
                            {
                                UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s._macro == _macroControl.Macro)?.Dispose();

                                World.Macros.Remove(_macroControl.Macro);

                                _macroControl.Dispose();
                            }

                            nb.Dispose();
                            databox.ReArrangeChildren();
                        }
                    );

                    UIManager.Add(dialog);
                }
            };


            MacroManager macroManager = World.Macros;

            for (Macro macro = (Macro) macroManager.Items; macro != null; macro = (Macro) macro.Next)
            {
                NiceButton nb;

                databox.Add
                (
                    nb = new NiceButton
                    (
                        0,
                        0,
                        130,
                        25,
                        ButtonAction.Activate,
                        macro.Name
                    )
                    {
                        ButtonParameter = (int) Buttons.Last + 1 + rightArea.Children.Count,
                        Tag = macro,
                        CanMove = true
                    }
                );

                nb.IsSelected = true;

                nb.DragBegin += (sss, eee) =>
                {
                    NiceButton mupNiceButton = (NiceButton) sss;

                    Macro m = mupNiceButton.Tag as Macro;

                    if (m == null)
                    {
                        return;
                    }

                    if (UIManager.DraggingControl != this || UIManager.MouseOverControl != sss)
                    {
                        return;
                    }

                    UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s._macro == m)?.Dispose();

                    MacroButtonGump macroButtonGump = new MacroButtonGump(World, m, Mouse.Position.X, Mouse.Position.Y);

                    macroButtonGump.X = Mouse.Position.X - (macroButtonGump.Width >> 1);
                    macroButtonGump.Y = Mouse.Position.Y - (macroButtonGump.Height >> 1);

                    UIManager.Add(macroButtonGump);

                    UIManager.AttemptDragControl(macroButtonGump, true);
                };

                nb.MouseUp += (sss, eee) =>
                {
                    NiceButton mupNiceButton = (NiceButton) sss;

                    Macro m = mupNiceButton.Tag as Macro;

                    if (m == null)
                    {
                        return;
                    }

                    _macroControl?.Dispose();

                    _macroControl = new MacroControl(this, m.Name)
                    {
                        X = 400,
                        Y = OptionsScrollY
                    };

                    Add(_macroControl, PAGE);
                };
            }

            databox.ReArrangeChildren();

            Add(rightArea, PAGE);
        }

        private void BuildTooltip()
        {
            const int PAGE = 5;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;

            _use_tooltip = AddCheckBox
            (
                rightArea,
                ResGumps.UseTooltip,
                _currentProfile.UseTooltip,
                startX,
                startY
            );

            startY += _use_tooltip.Height + 2;

            startX += 40;

            Label text = AddLabel(rightArea, ResGumps.DelayBeforeDisplay, startX, startY);
            startX += text.Width + 5;

            _delay_before_display_tooltip = AddHSlider
            (
                rightArea,
                0,
                1000,
                _currentProfile.TooltipDelayBeforeDisplay,
                startX,
                startY,
                200
            );

            startX = 5 + 40;
            startY += text.Height + 2;

            text = AddLabel(rightArea, ResGumps.TooltipZoom, startX, startY);
            startX += text.Width + 5;

            _tooltip_zoom = AddHSlider
            (
                rightArea,
                100,
                200,
                _currentProfile.TooltipDisplayZoom,
                startX,
                startY,
                200
            );

            startX = 5 + 40;
            startY += text.Height + 2;

            text = AddLabel(rightArea, ResGumps.TooltipBackgroundOpacity, startX, startY);
            startX += text.Width + 5;

            _tooltip_background_opacity = AddHSlider
            (
                rightArea,
                0,
                100,
                _currentProfile.TooltipBackgroundOpacity,
                startX,
                startY,
                200
            );

            startX = 5 + 40;
            startY += text.Height + 2;

            _tooltip_font_hue = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.TooltipTextHue,
                ResGumps.TooltipFontHue
            );

            startY += _tooltip_font_hue.Height + 2;

            startY += 15;

            text = AddLabel(rightArea, ResGumps.TooltipFont, startX, startY);
            startY += text.Height + 2;
            startX += 40;

            _tooltip_font_selector = new FontSelector(7, _currentProfile.TooltipFont, ResGumps.TooltipFontSelect)
            {
                X = startX,
                Y = startY
            };

            rightArea.Add(_tooltip_font_selector);

            Add(rightArea, PAGE);
        }

        private void BuildFonts()
        {
            const int PAGE = 6;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;

            _overrideAllFonts = AddCheckBox
            (
                rightArea,
                ResGumps.OverrideGameFont,
                _currentProfile.OverrideAllFonts,
                startX,
                startY
            );

            startX += _overrideAllFonts.Width + 5;

            _overrideAllFontsIsUnicodeCheckbox = AddCombobox
            (
                rightArea,
                new[]
                {
                    ResGumps.ASCII, ResGumps.Unicode
                },
                _currentProfile.OverrideAllFontsIsUnicode ? 1 : 0,
                startX,
                startY,
                100
            );

            startX = 5;
            startY += _overrideAllFonts.Height + 2;

            _forceUnicodeJournal = AddCheckBox
            (
                rightArea,
                ResGumps.ForceUnicodeInJournal,
                _currentProfile.ForceUnicodeJournal,
                startX,
                startY
            );

            startY += _forceUnicodeJournal.Height + 2;

            Label text = AddLabel(rightArea, ResGumps.SpeechFont, startX, startY);
            startX += 40;
            startY += text.Height + 2;

            _fontSelectorChat = new FontSelector(20, _currentProfile.ChatFont, ResGumps.ThatSClassicUO)
            {
                X = startX,
                Y = startY
            };

            rightArea.Add(_fontSelectorChat);

            Add(rightArea, PAGE);
        }

        private void BuildSpeech()
        {
            const int PAGE = 7;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;

            _scaleSpeechDelay = AddCheckBox
            (
                rightArea,
                ResGumps.ScaleSpeechDelay,
                _currentProfile.ScaleSpeechDelay,
                startX,
                startY
            );

            startX += _scaleSpeechDelay.Width + 5;

            _sliderSpeechDelay = AddHSlider
            (
                rightArea,
                0,
                1000,
                _currentProfile.SpeechDelay,
                startX,
                startY,
                180
            );

            startX = 5;
            startY += _scaleSpeechDelay.Height + 2;

            _saveJournalCheckBox = AddCheckBox
            (
                rightArea,
                ResGumps.SaveJournalToFileInGameFolder,
                _currentProfile.SaveJournalToFile,
                startX,
                startY
            );

            startY += _saveJournalCheckBox.Height + 2;

            if (!_currentProfile.SaveJournalToFile)
            {
                World.Journal.CloseWriter();
            }

            _chatAfterEnter = AddCheckBox
            (
                rightArea,
                ResGumps.ActiveChatWhenPressingEnter,
                _currentProfile.ActivateChatAfterEnter,
                startX,
                startY
            );

            startX += 40;
            startY += _chatAfterEnter.Height + 2;

            _chatAdditionalButtonsCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.UseAdditionalButtonsToActivateChat,
                _currentProfile.ActivateChatAdditionalButtons,
                startX,
                startY
            );

            startY += _chatAdditionalButtonsCheckbox.Height + 2;

            _chatShiftEnterCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.UseShiftEnterToSendMessage,
                _currentProfile.ActivateChatShiftEnterSupport,
                startX,
                startY
            );

            startY += _chatShiftEnterCheckbox.Height + 2;
            startX = 5;

            _hideChatGradient = AddCheckBox
            (
                rightArea,
                ResGumps.HideChatGradient,
                _currentProfile.HideChatGradient,
                startX,
                startY
            );

            startY += _hideChatGradient.Height + 2;

            _ignoreGuildMessages = AddCheckBox
            (
                rightArea,
                ResGumps.IgnoreGuildMessages,
                _currentProfile.IgnoreGuildMessages,
                startX,
                startY
            );

            startY += _ignoreGuildMessages.Height + 2;

            _ignoreAllianceMessages = AddCheckBox
            (
                rightArea,
                ResGumps.IgnoreAllianceMessages,
                _currentProfile.IgnoreAllianceMessages,
                startX,
                startY
            );

            startY += 35;

            _randomizeColorsButton = new NiceButton
            (
                startX,
                startY,
                140,
                25,
                ButtonAction.Activate,
                ResGumps.RandomizeSpeechHues
            ) { ButtonParameter = (int) Buttons.Disabled };

            _randomizeColorsButton.MouseUp += (sender, e) =>
            {
                if (e.Button != MouseButtonType.Left)
                {
                    return;
                }

                ushort speechHue = (ushort) RandomHelper.GetValue(2, 0x03b2); //this seems to be the acceptable hue range for chat messages,

                ushort emoteHue = (ushort) RandomHelper.GetValue(2, 0x03b2); //taken from POL source code.
                ushort yellHue = (ushort) RandomHelper.GetValue(2, 0x03b2);
                ushort whisperHue = (ushort) RandomHelper.GetValue(2, 0x03b2);
                _currentProfile.SpeechHue = speechHue;
                _speechColorPickerBox.Hue = speechHue;
                _currentProfile.EmoteHue = emoteHue;
                _emoteColorPickerBox.Hue = emoteHue;
                _currentProfile.YellHue = yellHue;
                _yellColorPickerBox.Hue = yellHue;
                _currentProfile.WhisperHue = whisperHue;
                _whisperColorPickerBox.Hue = whisperHue;
            };

            rightArea.Add(_randomizeColorsButton);
            startY += _randomizeColorsButton.Height + 2 + 20;


            _speechColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.SpeechHue,
                ResGumps.SpeechColor
            );

            startX += 200;

            _emoteColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.EmoteHue,
                ResGumps.EmoteColor
            );

            startY += _emoteColorPickerBox.Height + 2;
            startX = 5;

            _yellColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.YellHue,
                ResGumps.YellColor
            );

            startX += 200;

            _whisperColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.WhisperHue,
                ResGumps.WhisperColor
            );

            startY += _whisperColorPickerBox.Height + 2;
            startX = 5;

            _partyMessageColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.PartyMessageHue,
                ResGumps.PartyMessageColor
            );

            startX += 200;

            _guildMessageColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.GuildMessageHue,
                ResGumps.GuildMessageColor
            );

            startY += _guildMessageColorPickerBox.Height + 2;
            startX = 5;

            _allyMessageColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.AllyMessageHue,
                ResGumps.AllianceMessageColor
            );

            startX += 200;

            _chatMessageColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.ChatMessageHue,
                ResGumps.ChatMessageColor
            );

            startY += _chatMessageColorPickerBox.Height + 2;
            startX = 5;

            Add(rightArea, PAGE);
        }

        private void BuildCombat()
        {
            const int PAGE = 8;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;

            _holdDownKeyTab = AddCheckBox
            (
                rightArea,
                ResGumps.TabCombat,
                _currentProfile.HoldDownKeyTab,
                startX,
                startY
            );

            startY += _holdDownKeyTab.Height + 2;

            _queryBeforAttackCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.QueryAttack,
                _currentProfile.EnabledCriminalActionQuery,
                startX,
                startY
            );

            startY += _queryBeforAttackCheckbox.Height + 2;

            _queryBeforeBeneficialCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.QueryBeneficialActs,
                _currentProfile.EnabledBeneficialCriminalActionQuery,
                startX,
                startY
            );

            startY += _queryBeforeBeneficialCheckbox.Height + 2;

            _spellFormatCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.EnableOverheadSpellFormat,
                _currentProfile.EnabledSpellFormat,
                startX,
                startY
            );

            startY += _spellFormatCheckbox.Height + 2;

            _spellColoringCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.EnableOverheadSpellHue,
                _currentProfile.EnabledSpellHue,
                startX,
                startY
            );

            startY += _spellColoringCheckbox.Height + 2;

            _uiButtonsSingleClick = AddCheckBox
            (
                rightArea,
                ResGumps.UIButtonsSingleClick,
                _currentProfile.CastSpellsByOneClick,
                startX,
                startY
            );

            startY += _uiButtonsSingleClick.Height + 2;

            _buffBarTime = AddCheckBox
            (
                rightArea,
                ResGumps.ShowBuffDuration,
                _currentProfile.BuffBarTime,
                startX,
                startY
            );

            startY += _buffBarTime.Height + 2;

            _enableFastSpellsAssign = AddCheckBox
            (
                rightArea,
                ResGumps.EnableFastSpellsAssign,
                _currentProfile.FastSpellsAssign,
                startX,
                startY
            );

            startY += 30;

            int initialY = startY;

            _innocentColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.InnocentHue,
                ResGumps.InnocentColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            _friendColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.FriendHue,
                ResGumps.FriendColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            _crimialColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.CriminalHue,
                ResGumps.CriminalColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            _canAttackColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.CanAttackHue,
                ResGumps.CanAttackColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            _murdererColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.MurdererHue,
                ResGumps.MurdererColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            _enemyColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.EnemyHue,
                ResGumps.EnemyColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            startY = initialY;
            startX += 200;

            _beneficColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.BeneficHue,
                ResGumps.BeneficSpellHue
            );

            startY += _beneficColorPickerBox.Height + 2;

            _harmfulColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.HarmfulHue,
                ResGumps.HarmfulSpellHue
            );

            startY += _harmfulColorPickerBox.Height + 2;

            _neutralColorPickerBox = AddColorBox
            (
                rightArea,
                startX,
                startY,
                _currentProfile.NeutralHue,
                ResGumps.NeutralSpellHue
            );

            startY += _neutralColorPickerBox.Height + 2;

            startX = 5;
            startY += (_neutralColorPickerBox.Height + 2) * 4;

            _spellFormatBox = AddInputField
            (
                rightArea,
                startX,
                startY,
                200,
                TEXTBOX_HEIGHT,
                ResGumps.SpellOverheadFormat,
                0,
                true,
                false,
                30
            );

            _spellFormatBox.SetText(_currentProfile.SpellDisplayFormat);

            Add(rightArea, PAGE);
        }

        private void BuildCounters()
        {
            const int PAGE = 9;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;


            _enableCounters = AddCheckBox
            (
                rightArea,
                ResGumps.EnableCounters,
                _currentProfile.CounterBarEnabled,
                startX,
                startY
            );

            startX += 40;
            startY += _enableCounters.Height + 2;

            _highlightOnUse = AddCheckBox
            (
                rightArea,
                ResGumps.HighlightOnUse,
                _currentProfile.CounterBarHighlightOnUse,
                startX,
                startY
            );

            startY += _highlightOnUse.Height + 2;

            _enableAbbreviatedAmount = AddCheckBox
            (
                rightArea,
                ResGumps.EnableAbbreviatedAmountCountrs,
                _currentProfile.CounterBarDisplayAbbreviatedAmount,
                startX,
                startY
            );

            startX += _enableAbbreviatedAmount.Width + 5;

            _abbreviatedAmount = AddInputField
            (
                rightArea,
                startX,
                startY,
                50,
                TEXTBOX_HEIGHT,
                null,
                50,
                false,
                true
            );

            _abbreviatedAmount.SetText(_currentProfile.CounterBarAbbreviatedAmount.ToString());

            startX = 5;
            startX += 40;
            startY += _enableAbbreviatedAmount.Height + 2;

            _highlightOnAmount = AddCheckBox
            (
                rightArea,
                ResGumps.HighlightRedWhenBelow,
                _currentProfile.CounterBarHighlightOnAmount,
                startX,
                startY
            );

            startX += _highlightOnAmount.Width + 5;

            _highlightAmount = AddInputField
            (
                rightArea,
                startX,
                startY,
                50,
                TEXTBOX_HEIGHT,
                null,
                50,
                false,
                true,
                999
            );

            _highlightAmount.SetText(_currentProfile.CounterBarHighlightAmount.ToString());

            startX = 5;
            startX += 40;
            startY += _highlightAmount.Height + 2 + 5;

            startY += 40;

            Label text = AddLabel(rightArea, ResGumps.CounterLayout, startX, startY);

            startX += 40;
            startY += text.Height + 2;
            text = AddLabel(rightArea, ResGumps.CellSize, startX, startY);

            int initialX = startX;
            startX += text.Width + 5;

            _cellSize = AddHSlider
            (
                rightArea,
                30,
                80,
                _currentProfile.CounterBarCellSize,
                startX,
                startY,
                80
            );


            startX = initialX;
            startY += text.Height + 2 + 15;

            _rows = AddInputField
            (
                rightArea,
                startX,
                startY,
                50,
                30,
                ResGumps.Counter_Rows,
                50,
                false,
                true,
                30
            );

            _rows.SetText(_currentProfile.CounterBarRows.ToString());


            startX += _rows.Width + 5 + 100;

            _columns = AddInputField
            (
                rightArea,
                startX,
                startY,
                50,
                30,
                ResGumps.Counter_Columns,
                50,
                false,
                true,
                30
            );

            _columns.SetText(_currentProfile.CounterBarColumns.ToString());


            Add(rightArea, PAGE);
        }

        private void BuildExperimental()
        {
            const int PAGE = 12;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;

            _disableDefaultHotkeys = AddCheckBox
            (
                rightArea,
                ResGumps.DisableDefaultUOHotkeys,
                _currentProfile.DisableDefaultHotkeys,
                startX,
                startY
            );

            startX += 40;
            startY += _disableDefaultHotkeys.Height + 2;

            _disableArrowBtn = AddCheckBox
            (
                rightArea,
                ResGumps.DisableArrowsPlayerMovement,
                _currentProfile.DisableArrowBtn,
                startX,
                startY
            );

            startY += _disableArrowBtn.Height + 2;

            _disableTabBtn = AddCheckBox
            (
                rightArea,
                ResGumps.DisableTab,
                _currentProfile.DisableTabBtn,
                startX,
                startY
            );

            startY += _disableTabBtn.Height + 2;

            _disableCtrlQWBtn = AddCheckBox
            (
                rightArea,
                ResGumps.DisableMessageHistory,
                _currentProfile.DisableCtrlQWBtn,
                startX,
                startY
            );

            startY += _disableCtrlQWBtn.Height + 2;

            _disableAutoMove = AddCheckBox
            (
                rightArea,
                ResGumps.DisableClickAutomove,
                _currentProfile.DisableAutoMove,
                startX,
                startY
            );

            startY += _disableAutoMove.Height + 2;

            Add(rightArea, PAGE);
        }


        private void BuildInfoBar()
        {
            const int PAGE = 10;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;

            _showInfoBar = AddCheckBox
            (
                rightArea,
                ResGumps.ShowInfoBar,
                _currentProfile.ShowInfoBar,
                startX,
                startY
            );

            startX += 40;
            startY += _showInfoBar.Height + 2;

            Label text = AddLabel(rightArea, ResGumps.DataHighlightType, startX, startY);

            startX += text.Width + 5;

            _infoBarHighlightType = AddCombobox
            (
                rightArea,
                new[] { ResGumps.TextColor, ResGumps.ColoredBars },
                _currentProfile.InfoBarHighlightType,
                startX,
                startY,
                150
            );

            startX = 5;
            startY += _infoBarHighlightType.Height + 5;

            NiceButton nb = new NiceButton
            (
                startX,
                startY,
                90,
                20,
                ButtonAction.Activate,
                ResGumps.AddItem,
                0,
                TEXT_ALIGN_TYPE.TS_LEFT
            )
            {
                ButtonParameter = -1,
                IsSelectable = true,
                IsSelected = true
            };

            nb.MouseUp += (sender, e) =>
            {
                InfoBarBuilderControl ibbc = new InfoBarBuilderControl(this, new InfoBarItem("", InfoBarVars.HP, 0x3B9));
                ibbc.X = 5;
                ibbc.Y = _databox.Children.Count * ibbc.Height;
                _infoBarBuilderControls.Add(ibbc);
                _databox.Add(ibbc);
                _databox.WantUpdateSize = true;
            };

            rightArea.Add(nb);


            startY += 40;

            text = AddLabel(rightArea, ResGumps.Label, startX, startY);

            startX += 150;

            text = AddLabel(rightArea, ResGumps.Color, startX, startY);

            startX += 55;
            text = AddLabel(rightArea, ResGumps.Data, startX, startY);

            startX = 5;
            startY += text.Height + 2;

            rightArea.Add
            (
                new Line
                (
                    startX,
                    startY,
                    rightArea.Width,
                    1,
                    Color.Gray.PackedValue
                )
            );

            startY += 20;


            List<InfoBarItem> _infoBarItems = World.InfoBars.GetInfoBars();

            _infoBarBuilderControls = new List<InfoBarBuilderControl>();

            _databox = new DataBox(startX, startY, 10, 10)
            {
                WantUpdateSize = true
            };


            for (int i = 0; i < _infoBarItems.Count; i++)
            {
                InfoBarBuilderControl ibbc = new InfoBarBuilderControl(this, _infoBarItems[i]);
                ibbc.X = 5;
                ibbc.Y = i * ibbc.Height;
                _infoBarBuilderControls.Add(ibbc);
                _databox.Add(ibbc);
            }

            rightArea.Add(_databox);

            Add(rightArea, PAGE);
        }

        private void BuildContainers()
        {
            const int PAGE = 11;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                OptionsScrollY,
                WIDTH - 210,
                OptionsScrollHeight,
                true
            );

            int startX = 5;
            int startY = OptionsScrollContentPadding;
            Label text;

            bool hasBackpacks = Client.Game.UO.Version >= ClientVersion.CV_705301;

            if(hasBackpacks)
            {
                text = AddLabel(rightArea, ResGumps.BackpackStyle, startX, startY);
                startX += text.Width + 5;
            }

            _backpackStyle = AddCombobox
            (
                rightArea,
                new[]
                {
                    ResGumps.BackpackStyle_Default, ResGumps.BackpackStyle_Suede,
                    ResGumps.BackpackStyle_PolarBear, ResGumps.BackpackStyle_GhoulSkin
                },
                _currentProfile.BackpackStyle,
                startX,
                startY,
                200
            );

            _backpackStyle.IsVisible = hasBackpacks;

            if (hasBackpacks)
            {
                startX = 5;
                startY += _backpackStyle.Height + 2 + 10;
            }

            text = AddLabel(rightArea, ResGumps.ContainerScale, startX, startY);
            startX += text.Width + 5;

            _containersScale = AddHSlider
            (
                rightArea,
                Constants.MIN_CONTAINER_SIZE_PERC,
                Constants.MAX_CONTAINER_SIZE_PERC,
                _currentProfile.ContainersScale,
                startX,
                startY,
                200
            );

            startX = 5;
            startY += text.Height + 2;

            _containerScaleItems = AddCheckBox
            (
                rightArea,
                ResGumps.ScaleItemsInsideContainers,
                _currentProfile.ScaleItemsInsideContainers,
                startX,
                startY
            );

            startY += _containerScaleItems.Height + 2;

            _useLargeContianersGumps = AddCheckBox
            (
                rightArea,
                ResGumps.UseLargeContainersGump,
                _currentProfile.UseLargeContainerGumps,
                startX,
                startY
            );

            _useLargeContianersGumps.IsVisible = Client.Game.UO.Version >= ClientVersion.CV_706000;

            if (_useLargeContianersGumps.IsVisible)
            {
                startY += _useLargeContianersGumps.Height + 2;
            }

            startX = 5;
            text = AddLabel(rightArea, "Grid Containers", startX, startY);
            startY += text.Height + 4;

            _gridContainerEnabled = AddCheckBox
            (
                rightArea,
                "Enable grid containers",
                _currentProfile.GridContainerEnabled,
                startX,
                startY
            );

            startY += _gridContainerEnabled.Height + 2;

            _gridHideBorder = AddCheckBox
            (
                rightArea,
                "Hide grid border",
                _currentProfile.Grid_HideBorder,
                startX,
                startY
            );

            startY += _gridHideBorder.Height + 2;

            _gridContainerPreview = AddCheckBox
            (
                rightArea,
                "Enable container preview",
                _currentProfile.GridEnableContPreview,
                startX,
                startY
            );

            startY += _gridContainerPreview.Height + 2;

            text = AddLabel(rightArea, "Default columns", startX, startY);
            _gridColumns = AddHSlider(rightArea, 1, 20, _currentProfile.Grid_DefaultColumns, startX + text.Width + 5, startY, 150);
            startY += text.Height + 4;

            text = AddLabel(rightArea, "Default rows", startX, startY);
            _gridRows = AddHSlider(rightArea, 1, 20, _currentProfile.Grid_DefaultRows, startX + text.Width + 5, startY, 150);
            startY += text.Height + 4;

            text = AddLabel(rightArea, "Grid scale %", startX, startY);
            _gridContainerScale = AddHSlider(rightArea, 50, 200, _currentProfile.GridContainersScale, startX + text.Width + 5, startY, 150);
            startY += text.Height + 4;

            text = AddLabel(rightArea, "Background opacity %", startX, startY);
            _gridContainerOpacity = AddHSlider(rightArea, 0, 100, _currentProfile.ContainerOpacity, startX + text.Width + 5, startY, 150);
            startY += text.Height + 4;

            text = AddLabel(rightArea, "Search mode", startX, startY);
            _gridSearchMode = AddCombobox(rightArea, new[] { "Filter", "Highlight" }, _currentProfile.GridContainerSearchMode, startX + text.Width + 5, startY, 120);
            startY += text.Height + 4;

            text = AddLabel(rightArea, "Grid line intensity %", startX, startY);
            _gridBorderOpacity = AddHSlider(rightArea, 0, 100, _currentProfile.GridBorderAlpha, startX + text.Width + 5, startY, 150);
            startY += text.Height + 4;

            text = AddLabel(rightArea, "Grid line color", startX, startY);
            _gridBorderHue = AddColorBox(rightArea, startX + text.Width + 5, startY, _currentProfile.GridBorderHue, string.Empty);
            startY += _gridBorderHue.Height + 4;

            text = AddLabel(rightArea, "Container border color", startX, startY);
            _gridContainerBorderHue = AddColorBox(rightArea, startX + text.Width + 5, startY, _currentProfile.GridContainerBorderHue, string.Empty);
            startY += _gridContainerBorderHue.Height + 6;

            startX = 5;

            _containerDoubleClickToLoot = AddCheckBox
            (
                rightArea,
                ResGumps.DoubleClickLootContainers,
                _currentProfile.DoubleClickToLootInsideContainers,
                startX,
                startY
            );

            startY += _containerDoubleClickToLoot.Height + 2;

            _relativeDragAnDropItems = AddCheckBox
            (
                rightArea,
                ResGumps.RelativeDragAndDropContainers,
                _currentProfile.RelativeDragAndDropItems,
                startX,
                startY
            );

            startY += _relativeDragAnDropItems.Height + 2;

            _highlightContainersWhenMouseIsOver = AddCheckBox
            (
                rightArea,
                ResGumps.HighlightContainerWhenSelected,
                _currentProfile.HighlightContainerWhenSelected,
                startX,
                startY
            );

            startY += _highlightContainersWhenMouseIsOver.Height + 2;

            _hueContainerGumps = AddCheckBox
            (
                rightArea,
                ResGumps.HueContainerGumps,
                _currentProfile.HueContainerGumps,
                startX,
                startY
            );

            startY += _hueContainerGumps.Height + 2;

            _overrideContainerLocation = AddCheckBox
            (
                rightArea,
                ResGumps.OverrideContainerGumpLocation,
                _currentProfile.OverrideContainerLocation,
                startX,
                startY
            );

            startX += _overrideContainerLocation.Width + 5;

            _overrideContainerLocationSetting = AddCombobox
            (
                rightArea,
                new[]
                {
                    ResGumps.ContLoc_NearContainerPosition, ResGumps.ContLoc_TopRight,
                    ResGumps.ContLoc_LastDraggedPosition, ResGumps.ContLoc_RememberEveryContainer
                },
                _currentProfile.OverrideContainerLocationSetting,
                startX,
                startY,
                200
            );

            startX = 5;
            startY += _overrideContainerLocation.Height + 2 + 10;

            NiceButton button = new NiceButton
            (
                startX,
                startY,
                130,
                30,
                ButtonAction.Activate,
                ResGumps.RebuildContainers
            )
            {
                ButtonParameter = -1,
                IsSelectable = true,
                IsSelected = true
            };

            button.MouseUp += (sender, e) => { World.ContainerManager.BuildContainerFile(true); };
            rightArea.Add(button);

            Add(rightArea, PAGE);
        }


        public override void ChangePage(int pageIndex)
        {
            base.ChangePage(pageIndex);

            if (_optionsSearchAppliedTerm.Length >= 2)
            {
                ResetOptionsSearchScrollForActivePage();
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == (int) Buttons.Last + 1)
            {
                // it's the macro buttonssss
                return;
            }

            switch ((Buttons) buttonID)
            {
                case Buttons.Disabled: break;

                case Buttons.Cancel:
                    Dispose();

                    break;

                case Buttons.Apply:
                    Apply();

                    break;

                case Buttons.Default:
                    SetDefault();

                    break;

                case Buttons.Ok:
                    Apply();
                    Dispose();

                    break;

                case Buttons.NewMacro: break;

                case Buttons.DeleteMacro: break;
                case Buttons.OpenIgnoreList:
                    // If other IgnoreManagerGump exist - Dispose it
                    UIManager.GetGump<IgnoreManagerGump>()?.Dispose();
                    // Open new
                    UIManager.Add(new IgnoreManagerGump(World));
                    break;
            }
        }

        private void SetDefault()
        {
            switch (ActivePage)
            {
                case 1: // general
                    _sliderFPS.Value = 60;
                    _reduceFPSWhenInactive.IsChecked = true;
                    _highlightObjects.IsChecked = true;
                    _enableTopbar.IsChecked = false;
                    _holdDownKeyTab.IsChecked = true;
                    _holdDownKeyAlt.IsChecked = true;
                    _closeAllAnchoredGumpsWithRClick.IsChecked = false;
                    _holdShiftForContext.IsChecked = false;
                    _holdAltToMoveGumps.IsChecked = false;
                    _holdShiftToSplitStack.IsChecked = false;
                    _enablePathfind.IsChecked = false;
                    _useShiftPathfind.IsChecked = false;
                    _alwaysRun.IsChecked = false;
                    _alwaysRunUnlessHidden.IsChecked = false;
                    _showHpMobile.IsChecked = false;
                    _useOldHealthBars.IsChecked = false;
                    _hpComboBox.SelectedIndex = 0;
                    _hpComboBoxShowWhen.SelectedIndex = 0;
                    _highlightByPoisoned.IsChecked = true;
                    _highlightByParalyzed.IsChecked = true;
                    _highlightByInvul.IsChecked = true;
                    _poisonColorPickerBox.Hue = 0x0044;
                    _paralyzedColorPickerBox.Hue = 0x014C;
                    _invulnerableColorPickerBox.Hue = 0x0030;
                    _drawRoofs.IsChecked = false;
                    _enableCaveBorder.IsChecked = false;
                    _treeToStumps.IsChecked = false;
                    _hideVegetation.IsChecked = false;
                    _hideCarpets.IsChecked = false;
                    _noColorOutOfRangeObjects.IsChecked = false;
                    _circleOfTranspRadius.Value = Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS;
                    _cotType.SelectedIndex = 0;
                    _useCircleOfTransparency.IsChecked = false;
                    _healtbarType.SelectedIndex = 0;
                    _fieldsType.SelectedIndex = 0;
                    _useStandardSkillsGump.IsChecked = true;
                    _showCorpseNameIncoming.IsChecked = true;
                    _showMobileNameIncoming.IsChecked = true;
                    _gridLoot.SelectedIndex = 0;
                    _sallosEasyGrab.IsChecked = false;
                    _partyInviteGump.IsChecked = false;
                    _showHouseContent.IsChecked = false;
                    _objectsFading.IsChecked = true;
                    _textFading.IsChecked = true;
                    _enableDragSelect.IsChecked = false;
                    _dragSelectHumanoidsOnly.IsChecked = false;
                    _showTargetRangeIndicator.IsChecked = false;
                    _customBars.IsChecked = false;
                    _customBarsBBG.IsChecked = false;
                    _autoOpenCorpse.IsChecked = false;
                    _autoOpenDoors.IsChecked = false;
                    _smoothDoors.IsChecked = false;
                    _avoidObstacles.IsChecked = false;
                    _avoidObstaclesIgnoreHumanoids.IsChecked = false;
                    _skipEmptyCorpse.IsChecked = false;
                    _saveHealthbars.IsChecked = false;
                    _use_smooth_boat_movement.IsChecked = false;
                    _hideScreenshotStoredInMessage.IsChecked = false;
                    _use_old_status_gump.IsChecked = false;
                    _auraType.SelectedIndex = 0;
                    _fieldsType.SelectedIndex = 0;

                    _showSkillsMessage.IsChecked = true;
                    _showSkillsMessageDelta.Value = 1;
                    _showStatsMessage.IsChecked = true;

                    _dragSelectStartX.Value = 100;
                    _dragSelectStartY.Value = 100;
                    _dragSelectAsAnchor.IsChecked = false;

                    break;

                case 2: // sounds
                    _enableSounds.IsChecked = true;
                    _enableMusic.IsChecked = true;
                    _combatMusic.IsChecked = true;
                    _soundsVolume.Value = 100;
                    _musicVolume.Value = 100;
                    _musicInBackground.IsChecked = false;
                    _footStepsSound.IsChecked = true;
                    _loginMusicVolume.Value = 100;
                    _loginMusic.IsChecked = true;
                    _soundsVolume.IsVisible = _enableSounds.IsChecked;
                    _musicVolume.IsVisible = _enableMusic.IsChecked;

                    break;

                case 3: // video
                    _windowBorderless.IsChecked = false;
                    _zoomCheckbox.IsChecked = false;
                    _restorezoomCheckbox.IsChecked = false;
                    _gameWindowWidth.SetText("600");
                    _gameWindowHeight.SetText("480");
                    _gameWindowPositionX.SetText("20");
                    _gameWindowPositionY.SetText("20");
                    _gameWindowLock.IsChecked = false;
                    _gameWindowFullsize.IsChecked = false;
                    _enableDeathScreen.IsChecked = true;
                    _enableBlackWhiteEffect.IsChecked = true;
                    Client.Game.Scene.Camera.Zoom = 1f;
                    _currentProfile.DefaultScale = 1f;
                    _lightBar.Value = 0;
                    _enableLight.IsChecked = false;
                    _lightLevelType.SelectedIndex = 0;
                    _useColoredLights.IsChecked = false;
                    _darkNights.IsChecked = false;
                    _enableShadows.IsChecked = true;
                    _enableShadowsStatics.IsChecked = true;
                    _terrainShadowLevel.Value = 15;
                    _runMouseInSeparateThread.IsChecked = true;
                    _auraMouse.IsChecked = true;
                    _partyAura.IsChecked = true;
                    _animatedWaterEffect.IsChecked = false;
                    _partyAuraColorPickerBox.Hue = 0x0044;

                    break;

                case 4: // macros
                    break;

                case 5: // tooltip
                    _use_tooltip.IsChecked = true;
                    _tooltip_font_hue.Hue = 0xFFFF;
                    _delay_before_display_tooltip.Value = 200;
                    _tooltip_background_opacity.Value = 70;
                    _tooltip_zoom.Value = 100;
                    _tooltip_font_selector.SetSelectedFont(1);

                    break;

                case 6: // fonts
                    _fontSelectorChat.SetSelectedFont(0);
                    _overrideAllFonts.IsChecked = false;
                    _overrideAllFontsIsUnicodeCheckbox.SelectedIndex = 1;

                    break;

                case 7: // speech
                    _scaleSpeechDelay.IsChecked = true;
                    _sliderSpeechDelay.Value = 100;
                    _speechColorPickerBox.Hue = 0x02B2;
                    _emoteColorPickerBox.Hue = 0x0021;
                    _yellColorPickerBox.Hue = 0x0021;
                    _whisperColorPickerBox.Hue = 0x0033;
                    _partyMessageColorPickerBox.Hue = 0x0044;
                    _guildMessageColorPickerBox.Hue = 0x0044;
                    _allyMessageColorPickerBox.Hue = 0x0057;
                    _chatMessageColorPickerBox.Hue = 0x0256;
                    _chatAfterEnter.IsChecked = false;
                    UIManager.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
                    _chatAdditionalButtonsCheckbox.IsChecked = true;
                    _chatShiftEnterCheckbox.IsChecked = true;
                    _saveJournalCheckBox.IsChecked = false;
                    _hideChatGradient.IsChecked = false;
                    _ignoreGuildMessages.IsChecked = false;
                    _ignoreAllianceMessages.IsChecked = false;

                    break;

                case 8: // combat
                    _innocentColorPickerBox.Hue = 0x005A;
                    _friendColorPickerBox.Hue = 0x0044;
                    _crimialColorPickerBox.Hue = 0x03b2;
                    _canAttackColorPickerBox.Hue = 0x03b2;
                    _murdererColorPickerBox.Hue = 0x0023;
                    _enemyColorPickerBox.Hue = 0x0031;
                    _queryBeforAttackCheckbox.IsChecked = true;
                    _queryBeforeBeneficialCheckbox.IsChecked = false;
                    _uiButtonsSingleClick.IsChecked = false;
                    _buffBarTime.IsChecked = false;
                    _enableFastSpellsAssign.IsChecked = false;
                    _beneficColorPickerBox.Hue = 0x0059;
                    _harmfulColorPickerBox.Hue = 0x0020;
                    _neutralColorPickerBox.Hue = 0x03b2;
                    _spellFormatBox.SetText(ResGumps.SpellFormat_Default);
                    _spellColoringCheckbox.IsChecked = false;
                    _spellFormatCheckbox.IsChecked = false;

                    break;

                case 9: // counters
                    _enableCounters.IsChecked = false;
                    _highlightOnUse.IsChecked = false;
                    _enableAbbreviatedAmount.IsChecked = false;
                    _columns.SetText("1");
                    _rows.SetText("1");
                    _cellSize.Value = 40;
                    _highlightOnAmount.IsChecked = false;
                    _highlightAmount.SetText("5");
                    _abbreviatedAmount.SetText("1000");

                    break;

                case 10: // info bar


                    break;

                case 11: // containers
                    _containersScale.Value = 100;
                    _containerScaleItems.IsChecked = false;
                    _useLargeContianersGumps.IsChecked = false;

                    _gridContainerEnabled.IsChecked = false;
                    _gridHideBorder.IsChecked = false;
                    _gridContainerPreview.IsChecked = true;
                    _gridColumns.Value = 4;
                    _gridRows.Value = 4;
                    _gridContainerScale.Value = 100;
                    _gridContainerOpacity.Value = 80;
                    _gridSearchMode.SelectedIndex = 0;
                    _gridBorderOpacity.Value = 35;
                    _gridBorderHue.Hue = 946;
                    _gridContainerBorderHue.Hue = 946;

                    _containerDoubleClickToLoot.IsChecked = false;
                    _relativeDragAnDropItems.IsChecked = false;
                    _highlightContainersWhenMouseIsOver.IsChecked = false;
                    _overrideContainerLocation.IsChecked = false;
                    _overrideContainerLocationSetting.SelectedIndex = 0;
                    _backpackStyle.SelectedIndex = 0;
                    _hueContainerGumps.IsChecked = true;

                    break;

                case 12: // experimental

                    _disableDefaultHotkeys.IsChecked = false;
                    _disableArrowBtn.IsChecked = false;
                    _disableTabBtn.IsChecked = false;
                    _disableCtrlQWBtn.IsChecked = false;
                    _disableAutoMove.IsChecked = false;

                    break;
            }
        }

        private void Apply()
        {
            WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();

            // general
            if (Settings.GlobalSettings.FPS != _sliderFPS.Value)
            {
                Client.Game.SetRefreshRate(_sliderFPS.Value);
            }

            _currentProfile.HighlightGameObjects = _highlightObjects.IsChecked;
            _currentProfile.ReduceFPSWhenInactive = _reduceFPSWhenInactive.IsChecked;
            _currentProfile.EnablePathfind = _enablePathfind.IsChecked;
            _currentProfile.UseShiftToPathfind = _useShiftPathfind.IsChecked;
            _currentProfile.AlwaysRun = _alwaysRun.IsChecked;
            _currentProfile.AlwaysRunUnlessHidden = _alwaysRunUnlessHidden.IsChecked;
            _currentProfile.ShowMobilesHP = _showHpMobile.IsChecked;
            _currentProfile.UseOldHealthBars = _useOldHealthBars.IsChecked;
            _currentProfile.HighlightMobilesByPoisoned = _highlightByPoisoned.IsChecked;
            _currentProfile.HighlightMobilesByParalize = _highlightByParalyzed.IsChecked;
            _currentProfile.HighlightMobilesByInvul = _highlightByInvul.IsChecked;
            _currentProfile.PoisonHue = _poisonColorPickerBox.Hue;
            _currentProfile.ParalyzedHue = _paralyzedColorPickerBox.Hue;
            _currentProfile.InvulnerableHue = _invulnerableColorPickerBox.Hue;
            _currentProfile.MobileHPType = _hpComboBox.SelectedIndex;
            _currentProfile.MobileHPShowWhen = _hpComboBoxShowWhen.SelectedIndex;
            _currentProfile.HoldDownKeyTab = _holdDownKeyTab.IsChecked;
            _currentProfile.HoldDownKeyAltToCloseAnchored = _holdDownKeyAlt.IsChecked;

            _currentProfile.CloseAllAnchoredGumpsInGroupWithRightClick = _closeAllAnchoredGumpsWithRClick.IsChecked;

            _currentProfile.HoldShiftForContext = _holdShiftForContext.IsChecked;
            _currentProfile.HoldAltToMoveGumps = _holdAltToMoveGumps.IsChecked;
            _currentProfile.HoldShiftToSplitStack = _holdShiftToSplitStack.IsChecked;
            _currentProfile.CloseHealthBarType = _healtbarType.SelectedIndex;
            _currentProfile.HideScreenshotStoredInMessage = _hideScreenshotStoredInMessage.IsChecked;

            if (_currentProfile.DrawRoofs == _drawRoofs.IsChecked)
            {
                _currentProfile.DrawRoofs = !_drawRoofs.IsChecked;

                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            if (_currentProfile.TopbarGumpIsDisabled != _enableTopbar.IsChecked)
            {
                if (_enableTopbar.IsChecked)
                {
                    UIManager.GetGump<TopBarGump>()?.Dispose();
                }
                else
                {
                    TopBarGump.Create(World);
                }

                _currentProfile.TopbarGumpIsDisabled = _enableTopbar.IsChecked;
            }

            if (_currentProfile.EnableCaveBorder != _enableCaveBorder.IsChecked)
            {
                StaticFilters.CleanCaveTextures();
                _currentProfile.EnableCaveBorder = _enableCaveBorder.IsChecked;
            }

            if (_currentProfile.TreeToStumps != _treeToStumps.IsChecked)
            {
                StaticFilters.CleanTreeTextures();
                _currentProfile.TreeToStumps = _treeToStumps.IsChecked;
            }

            _currentProfile.FieldsType = _fieldsType.SelectedIndex;
            _currentProfile.HideVegetation = _hideVegetation.IsChecked;
            _currentProfile.HideCarpets = _hideCarpets.IsChecked;

            // Visual Helpers (Dust765) - Highlight tiles on range
            _currentProfile.LTHighlightRangeOnActivated = _ltHighlightRangeOnActivated.IsChecked;
            _currentProfile.LTHighlightRangeOnActivatedRange = _ltHighlightRangeOnActivatedRange.Value;
            _currentProfile.LTHighlightRangeOnActivatedHue = _ltHighlightRangeOnActivatedHue.Hue;
            _currentProfile.LTHighlightRangeOnCast = _ltHighlightRangeOnCast.IsChecked;
            _currentProfile.LTHighlightRangeOnCastRange = _ltHighlightRangeOnCastRange.Value;
            _currentProfile.LTHighlightRangeOnCastHue = _ltHighlightRangeOnCastHue.Hue;
            _currentProfile.LTHighlightRangeOutlinePixels = _ltHighlightRangeOutlinePixels.Value;
            _currentProfile.HighlightMirrorImageClones = _highlightMirrorClones.IsChecked;
            _currentProfile.ShowBandageRingTimer = _showBandageRingTimer.IsChecked;
            if (string.IsNullOrWhiteSpace(_bandageRingTimerX?.Text))
            {
                _currentProfile.BandageRingTimerX = -1;
            }
            else if (int.TryParse(_bandageRingTimerX.Text, out int timerX))
            {
                _currentProfile.BandageRingTimerX = timerX;
            }

            if (string.IsNullOrWhiteSpace(_bandageRingTimerY?.Text))
            {
                _currentProfile.BandageRingTimerY = -1;
            }
            else if (int.TryParse(_bandageRingTimerY.Text, out int timerY))
            {
                _currentProfile.BandageRingTimerY = timerY;
            }
            _currentProfile.MirrorImageCloneHue = _mirrorCloneHue.Hue;
            _currentProfile.GlowingWeaponsType = _glowingWeaponsType.SelectedIndex;
            _currentProfile.HighlightGlowingWeaponsTypeHue = _highlightGlowingWeaponsTypeHue.Hue;
            _currentProfile.HighlightLastTargetType = _highlightLastTargetType.SelectedIndex;
            _currentProfile.HighlighFriendsGuildType = _highlighFriendsGuildType.SelectedIndex;
            _currentProfile.HighlightLastTargetTypePoison = _highlightLastTargetTypePoison.SelectedIndex;
            _currentProfile.HighlightLastTargetTypePara = _highlightLastTargetTypePara.SelectedIndex;
            _currentProfile.HighlightLastTargetTypeStunned = _highlightLastTargetTypeStunned.SelectedIndex;
            _currentProfile.HighlightLastTargetTypeMortalled = _highlightLastTargetTypeMortalled.SelectedIndex;
            _currentProfile.HighlightLastTargetTypeHue = _highlightLastTargetTypeHue.Hue;
            _currentProfile.HighlighFriendsGuildTypeHue = _highlighFriendsGuildTypeHue.Hue;
            _currentProfile.HighlightLastTargetTypePoisonHue = _highlightLastTargetTypePoisonHue.Hue;
            _currentProfile.HighlightLastTargetTypeParaHue = _highlightLastTargetTypeParaHue.Hue;
            _currentProfile.HighlightLastTargetTypeStunnedHue = _highlightLastTargetTypeStunnedHue.Hue;
            _currentProfile.HighlightLastTargetTypeMortalledHue = _highlightLastTargetTypeMortalledHue.Hue;
            bool energyFieldAutoAvoid = _energyFieldWallOfStoneAutoAvoid.IsChecked;
            _currentProfile.BlockEnergyFArtForceAoS = energyFieldAutoAvoid;

            if (_currentProfile.BlockEnergyF != energyFieldAutoAvoid)
            {
                ushort eFieldGraphic = (ushort)Math.Min(_currentProfile.BlockEnergyFArt, ushort.MaxValue);

                if (energyFieldAutoAvoid)
                {
                    ref StaticTiles tile = ref TileDataLoader.Instance.StaticData[eFieldGraphic];
                    tile.Flags |= TileFlag.Impassable;
                }
                else
                {
                    ref StaticTiles tile = ref TileDataLoader.Instance.StaticData[eFieldGraphic];
                    tile.Flags &= ~TileFlag.Impassable;
                }

                _currentProfile.BlockEnergyF = energyFieldAutoAvoid;
            }

            _currentProfile.NameOverheadToggled = _nameOverheadAlwaysOn.IsChecked;
            _currentProfile.EnableUoDreamsNetworkOptimizer = _enableUoDreamsNetworkOptimizer.IsChecked;
            _currentProfile.EnableFullSocketDrain = _enableFullSocketDrain.IsChecked;
            _currentProfile.FastRotation = _fastRotation.IsChecked;
            MovementSpeed.FastRotation = _fastRotation.IsChecked;

            if (int.TryParse(_movementTurnDelay.Text, out int movementTurnDelay))
            {
                _currentProfile.MovementTurnDelay = Math.Clamp(movementTurnDelay, 40, 1000);
            }

            if (int.TryParse(_movementTurnDelayFast.Text, out int movementTurnDelayFast))
            {
                _currentProfile.MovementTurnDelayFast = Math.Clamp(movementTurnDelayFast, 40, 1000);
            }

            if (int.TryParse(_movementWalkingDelay.Text, out int movementWalkingDelay))
            {
                _currentProfile.MovementWalkingDelay = Math.Clamp(movementWalkingDelay, 40, 1000);
            }

            if (int.TryParse(_movementPlayerWalkingDelay.Text, out int movementPlayerWalkingDelay))
            {
                _currentProfile.MovementPlayerWalkingDelay = Math.Clamp(movementPlayerWalkingDelay, 40, 1000);
            }

            _currentProfile.HidePersistentNPCNames = _hidePersistentNPCNames.IsChecked;
            _currentProfile.ShowAllLayersPaperdoll = _showAllLayersPaperdoll.IsChecked;
            _currentProfile.AutoOpenUiOnLogin = _autoOpenUiOnLogin.IsChecked;

            _currentProfile.NoColorObjectsOutOfRange = _noColorOutOfRangeObjects.IsChecked;
            _currentProfile.UseCircleOfTransparency = _useCircleOfTransparency.IsChecked;
            _currentProfile.CircleOfTransparencyRadius = _circleOfTranspRadius.Value;
            _currentProfile.CircleOfTransparencyType = _cotType.SelectedIndex;
            _currentProfile.StandardSkillsGump = _useStandardSkillsGump.IsChecked;

            if (_useStandardSkillsGump.IsChecked)
            {
                SkillGumpAdvanced newGump = UIManager.GetGump<SkillGumpAdvanced>();

                if (newGump != null)
                {
                    UIManager.Add(new StandardSkillsGump(World) { X = newGump.X, Y = newGump.Y });

                    newGump.Dispose();
                }
            }
            else
            {
                StandardSkillsGump standardGump = UIManager.GetGump<StandardSkillsGump>();

                if (standardGump != null)
                {
                    UIManager.Add(new SkillGumpAdvanced(World) { X = standardGump.X, Y = standardGump.Y });

                    standardGump.Dispose();
                }
            }

            _currentProfile.ShowNewMobileNameIncoming = _showMobileNameIncoming.IsChecked;
            _currentProfile.ShowNewCorpseNameIncoming = _showCorpseNameIncoming.IsChecked;
            _currentProfile.GridLootType = _gridLoot.SelectedIndex;
            _currentProfile.SallosEasyGrab = _sallosEasyGrab.IsChecked;
            _currentProfile.PartyInviteGump = _partyInviteGump.IsChecked;
            _currentProfile.UseObjectsFading = _objectsFading.IsChecked;
            _currentProfile.TextFading = _textFading.IsChecked;
            _currentProfile.UseSmoothBoatMovement = _use_smooth_boat_movement.IsChecked;

            if (_currentProfile.ShowHouseContent != _showHouseContent.IsChecked)
            {
                _currentProfile.ShowHouseContent = _showHouseContent.IsChecked;
                NetClient.Socket.Send_ShowPublicHouseContent(_currentProfile.ShowHouseContent);
            }


            // sounds
            _currentProfile.EnableSound = _enableSounds.IsChecked;
            _currentProfile.EnableMusic = _enableMusic.IsChecked;
            _currentProfile.EnableFootstepsSound = _footStepsSound.IsChecked;
            _currentProfile.EnableCombatMusic = _combatMusic.IsChecked;
            _currentProfile.ReproduceSoundsInBackground = _musicInBackground.IsChecked;
            _currentProfile.SoundVolume = _soundsVolume.Value;
            _currentProfile.MusicVolume = _musicVolume.Value;
            Settings.GlobalSettings.LoginMusicVolume = _loginMusicVolume.Value;
            Settings.GlobalSettings.LoginMusic = _loginMusic.IsChecked;

            Client.Game.Audio.UpdateCurrentMusicVolume();
            Client.Game.Audio.UpdateCurrentSoundsVolume();

            if (!_currentProfile.EnableMusic)
            {
                Client.Game.Audio.StopMusic();
            }

            if (!_currentProfile.EnableSound)
            {
                Client.Game.Audio.StopSounds();
            }

            // speech
            _currentProfile.ScaleSpeechDelay = _scaleSpeechDelay.IsChecked;
            _currentProfile.SpeechDelay = _sliderSpeechDelay.Value;
            _currentProfile.SpeechHue = _speechColorPickerBox.Hue;
            _currentProfile.EmoteHue = _emoteColorPickerBox.Hue;
            _currentProfile.YellHue = _yellColorPickerBox.Hue;
            _currentProfile.WhisperHue = _whisperColorPickerBox.Hue;
            _currentProfile.PartyMessageHue = _partyMessageColorPickerBox.Hue;
            _currentProfile.GuildMessageHue = _guildMessageColorPickerBox.Hue;
            _currentProfile.AllyMessageHue = _allyMessageColorPickerBox.Hue;
            _currentProfile.ChatMessageHue = _chatMessageColorPickerBox.Hue;

            if (_currentProfile.ActivateChatAfterEnter != _chatAfterEnter.IsChecked)
            {
                UIManager.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
                _currentProfile.ActivateChatAfterEnter = _chatAfterEnter.IsChecked;
            }

            _currentProfile.ActivateChatAdditionalButtons = _chatAdditionalButtonsCheckbox.IsChecked;
            _currentProfile.ActivateChatShiftEnterSupport = _chatShiftEnterCheckbox.IsChecked;
            _currentProfile.SaveJournalToFile = _saveJournalCheckBox.IsChecked;

            // video
            _currentProfile.EnableDeathScreen = _enableDeathScreen.IsChecked;
            _currentProfile.EnableBlackWhiteEffect = _enableBlackWhiteEffect.IsChecked;

            var camera = Client.Game.Scene.Camera;
            _currentProfile.DefaultScale = camera.Zoom = (_sliderZoom.Value * camera.ZoomStep) + camera.ZoomMin;

            _currentProfile.EnableMousewheelScaleZoom = _zoomCheckbox.IsChecked;
            _currentProfile.RestoreScaleAfterUnpressCtrl = _restorezoomCheckbox.IsChecked;

            if (!CUOEnviroment.IsOutlands && _use_old_status_gump.IsChecked != _currentProfile.UseOldStatusGump)
            {
                StatusGumpBase status = StatusGumpBase.GetStatusGump();

                _currentProfile.UseOldStatusGump = _use_old_status_gump.IsChecked;

                if (status != null)
                {
                    status.Dispose();
                    UIManager.Add(StatusGumpBase.AddStatusGump(World, status.ScreenCoordinateX, status.ScreenCoordinateY));
                }
            }


            int.TryParse(_gameWindowWidth.Text, out int gameWindowSizeWidth);
            int.TryParse(_gameWindowHeight.Text, out int gameWindowSizeHeight);

            if (gameWindowSizeWidth != Client.Game.Scene.Camera.Bounds.Width || gameWindowSizeHeight != Client.Game.Scene.Camera.Bounds.Height)
            {
                if (vp != null)
                {
                    Point n = vp.ResizeGameWindow(new Point(gameWindowSizeWidth, gameWindowSizeHeight));

                    _gameWindowWidth.SetText(n.X.ToString());
                    _gameWindowHeight.SetText(n.Y.ToString());
                }
            }

            int.TryParse(_gameWindowPositionX.Text, out int gameWindowPositionX);
            int.TryParse(_gameWindowPositionY.Text, out int gameWindowPositionY);

            if (gameWindowPositionX != camera.Bounds.X || gameWindowPositionY != camera.Bounds.Y)
            {
                if (vp != null)
                {
                    vp.SetGameWindowPosition(new Point(gameWindowPositionX, gameWindowPositionY));
                    _currentProfile.GameWindowPosition = vp.Location;
                }
            }

            if (_currentProfile.GameWindowLock != _gameWindowLock.IsChecked)
            {
                if (vp != null)
                {
                    vp.CanMove = !_gameWindowLock.IsChecked;
                }

                _currentProfile.GameWindowLock = _gameWindowLock.IsChecked;
            }

            if (_currentProfile.GameWindowFullSize != _gameWindowFullsize.IsChecked)
            {
                Point n = Point.Zero, loc = Point.Zero;

                if (_gameWindowFullsize.IsChecked)
                {
                    if (vp != null)
                    {
                        n = vp.ResizeGameWindow(new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height));
                        vp.SetGameWindowPosition(new Point(-5, -5));
                        _currentProfile.GameWindowPosition = vp.Location;
                    }
                }
                else
                {
                    if (vp != null)
                    {
                        n = vp.ResizeGameWindow(new Point(600, 480));
                        vp.SetGameWindowPosition(new Point(20, 20));
                        _currentProfile.GameWindowPosition = vp.Location;
                    }
                }

                _gameWindowPositionX.SetText(loc.X.ToString());
                _gameWindowPositionY.SetText(loc.Y.ToString());
                _gameWindowWidth.SetText(n.X.ToString());
                _gameWindowHeight.SetText(n.Y.ToString());

                _currentProfile.GameWindowFullSize = _gameWindowFullsize.IsChecked;
            }

            if (_currentProfile.WindowBorderless != _windowBorderless.IsChecked)
            {
                _currentProfile.WindowBorderless = _windowBorderless.IsChecked;
                Client.Game.SetWindowBorderless(_windowBorderless.IsChecked);
            }

            _currentProfile.UseAlternativeLights = _altLights.IsChecked;
            _currentProfile.UseCustomLightLevel = _enableLight.IsChecked;
            _currentProfile.LightLevel = (byte) (_lightBar.MaxValue - _lightBar.Value);
            _currentProfile.LightLevelType = _lightLevelType.SelectedIndex;

            if (_enableLight.IsChecked)
            {
                World.Light.Overall = _currentProfile.LightLevelType == 1 ? Math.Min(World.Light.RealOverall, _currentProfile.LightLevel) : _currentProfile.LightLevel;
                World.Light.Personal = 0;
            }
            else
            {
                World.Light.Overall = World.Light.RealOverall;
                World.Light.Personal = World.Light.RealPersonal;
            }

            _currentProfile.UseColoredLights = _useColoredLights.IsChecked;
            _currentProfile.UseDarkNights = _darkNights.IsChecked;
            _currentProfile.ShadowsEnabled = _enableShadows.IsChecked;
            _currentProfile.ShadowsStatics = _enableShadowsStatics.IsChecked;
            _currentProfile.TerrainShadowsLevel = _terrainShadowLevel.Value;
            _currentProfile.AuraUnderFeetType = _auraType.SelectedIndex;

            Client.Game.IsMouseVisible = Settings.GlobalSettings.RunMouseInASeparateThread = _runMouseInSeparateThread.IsChecked;

            _currentProfile.AuraOnMouse = _auraMouse.IsChecked;
            _currentProfile.AnimatedWaterEffect = _animatedWaterEffect.IsChecked;
            _currentProfile.PartyAura = _partyAura.IsChecked;
            _currentProfile.PartyAuraHue = _partyAuraColorPickerBox.Hue;
            _currentProfile.HideChatGradient = _hideChatGradient.IsChecked;
            _currentProfile.IgnoreGuildMessages = _ignoreGuildMessages.IsChecked;
            _currentProfile.IgnoreAllianceMessages = _ignoreAllianceMessages.IsChecked;

            // fonts
            _currentProfile.ForceUnicodeJournal = _forceUnicodeJournal.IsChecked;
            byte _fontValue = _fontSelectorChat.GetSelectedFont();
            bool fontSettingsChanged =
                _currentProfile.OverrideAllFonts != _overrideAllFonts.IsChecked
                || _currentProfile.OverrideAllFontsIsUnicode != (_overrideAllFontsIsUnicodeCheckbox.SelectedIndex == 1)
                || _currentProfile.ForceUnicodeJournal != _forceUnicodeJournal.IsChecked
                || _currentProfile.ChatFont != _fontValue;
            _currentProfile.OverrideAllFonts = _overrideAllFonts.IsChecked;
            _currentProfile.OverrideAllFontsIsUnicode = _overrideAllFontsIsUnicodeCheckbox.SelectedIndex == 1;

            if (_currentProfile.ChatFont != _fontValue)
            {
                _currentProfile.ChatFont = _fontValue;
                UIManager.SystemChat.TextBoxControl.Font = _fontValue;
            }

            if (fontSettingsChanged)
            {
                JournalManager.RefreshOpenJournalGumps();

                foreach (PaperDollGump paperdoll in UIManager.Gumps.OfType<PaperDollGump>())
                {
                    paperdoll.RefreshTitleFont();
                }
            }

            // combat
            _currentProfile.InnocentHue = _innocentColorPickerBox.Hue;
            _currentProfile.FriendHue = _friendColorPickerBox.Hue;
            _currentProfile.CriminalHue = _crimialColorPickerBox.Hue;
            _currentProfile.CanAttackHue = _canAttackColorPickerBox.Hue;
            _currentProfile.EnemyHue = _enemyColorPickerBox.Hue;
            _currentProfile.MurdererHue = _murdererColorPickerBox.Hue;
            _currentProfile.EnabledCriminalActionQuery = _queryBeforAttackCheckbox.IsChecked;
            _currentProfile.EnabledBeneficialCriminalActionQuery = _queryBeforeBeneficialCheckbox.IsChecked;
            _currentProfile.CastSpellsByOneClick = _uiButtonsSingleClick.IsChecked;
            _currentProfile.BuffBarTime = _buffBarTime.IsChecked;
            _currentProfile.FastSpellsAssign = _enableFastSpellsAssign.IsChecked;

            _currentProfile.BeneficHue = _beneficColorPickerBox.Hue;
            _currentProfile.HarmfulHue = _harmfulColorPickerBox.Hue;
            _currentProfile.NeutralHue = _neutralColorPickerBox.Hue;
            _currentProfile.EnabledSpellHue = _spellColoringCheckbox.IsChecked;
            _currentProfile.EnabledSpellFormat = _spellFormatCheckbox.IsChecked;
            _currentProfile.SpellDisplayFormat = _spellFormatBox.Text;

            // macros
            World.Macros.Save();

            // counters

            bool before = _currentProfile.CounterBarEnabled;
            _currentProfile.CounterBarEnabled = _enableCounters.IsChecked;
            _currentProfile.CounterBarCellSize = _cellSize.Value;

            if (!int.TryParse(_rows.Text, out int v))
            {
                v = 1;
                _rows.SetText("1");
            }

            _currentProfile.CounterBarRows = v;

            if (!int.TryParse(_columns.Text, out v))
            {
                v = 1;
                _columns.SetText("1");
            }
            _currentProfile.CounterBarColumns = v;
            _currentProfile.CounterBarHighlightOnUse = _highlightOnUse.IsChecked;

            if (!int.TryParse(_highlightAmount.Text, out v))
            {
                v = 5;
                _highlightAmount.SetText("5");
            }
            _currentProfile.CounterBarHighlightAmount = v;

            if (!int.TryParse(_abbreviatedAmount.Text, out v))
            {
                v = 1000;
                _abbreviatedAmount.SetText("1000");
            }
            _currentProfile.CounterBarAbbreviatedAmount = v;
            _currentProfile.CounterBarHighlightOnAmount = _highlightOnAmount.IsChecked;
            _currentProfile.CounterBarDisplayAbbreviatedAmount = _enableAbbreviatedAmount.IsChecked;

            CounterBarGump counterGump = UIManager.GetGump<CounterBarGump>();

            counterGump?.SetLayout(_currentProfile.CounterBarCellSize, _currentProfile.CounterBarRows, _currentProfile.CounterBarColumns);


            if (before != _currentProfile.CounterBarEnabled)
            {
                if (counterGump == null)
                {
                    if (_currentProfile.CounterBarEnabled)
                    {
                        UIManager.Add
                        (
                            new CounterBarGump
                            (
                                World,
                                200,
                                200,
                                _currentProfile.CounterBarCellSize,
                                _currentProfile.CounterBarRows,
                                _currentProfile.CounterBarColumns
                            )
                        );
                    }
                }
                else
                {
                    counterGump.IsEnabled = counterGump.IsVisible = _currentProfile.CounterBarEnabled;
                }
            }

            // experimental
            // Reset nested checkboxes if parent checkbox is unchecked
            if (!_disableDefaultHotkeys.IsChecked)
            {
                _disableArrowBtn.IsChecked = false;
                _disableTabBtn.IsChecked = false;
                _disableCtrlQWBtn.IsChecked = false;
                _disableAutoMove.IsChecked = false;
            }

            // NOTE: Keep these assignments AFTER the code above that resets nested checkboxes if parent checkbox is unchecked
            _currentProfile.DisableDefaultHotkeys = _disableDefaultHotkeys.IsChecked;
            _currentProfile.DisableArrowBtn = _disableArrowBtn.IsChecked;
            _currentProfile.DisableTabBtn = _disableTabBtn.IsChecked;
            _currentProfile.DisableCtrlQWBtn = _disableCtrlQWBtn.IsChecked;
            _currentProfile.DisableAutoMove = _disableAutoMove.IsChecked;
            _currentProfile.AutoOpenDoors = _autoOpenDoors.IsChecked;
            _currentProfile.SmoothDoors = _smoothDoors.IsChecked;
            _currentProfile.AvoidObstacles = _avoidObstacles.IsChecked;
            _currentProfile.AvoidObstaclesIgnoreHumanoids = _avoidObstaclesIgnoreHumanoids.IsChecked;
            _currentProfile.AutoOpenCorpses = _autoOpenCorpse.IsChecked;
            _currentProfile.AutoOpenCorpseRange = int.Parse(_autoOpenCorpseRange.Text);
            _currentProfile.CorpseOpenOptions = _autoOpenCorpseOptions.SelectedIndex;
            _currentProfile.SkipEmptyCorpse = _skipEmptyCorpse.IsChecked;

            _currentProfile.EnableDragSelect = _enableDragSelect.IsChecked;
            _currentProfile.DragSelectModifierKey = _dragSelectModifierKey.SelectedIndex;
            _currentProfile.DragSelectHumanoidsOnly = _dragSelectHumanoidsOnly.IsChecked;
            _currentProfile.DragSelectStartX = _dragSelectStartX.Value;
            _currentProfile.DragSelectStartY = _dragSelectStartY.Value;
            _currentProfile.DragSelectAsAnchor = _dragSelectAsAnchor.IsChecked;

            _currentProfile.ShowSkillsChangedMessage = _showSkillsMessage.IsChecked;
            _currentProfile.ShowSkillsChangedDeltaValue = _showSkillsMessageDelta.Value;
            _currentProfile.ShowStatsChangedMessage = _showStatsMessage.IsChecked;

            _currentProfile.OverrideContainerLocation = _overrideContainerLocation.IsChecked;
            _currentProfile.OverrideContainerLocationSetting = _overrideContainerLocationSetting.SelectedIndex;

            _currentProfile.ShowTargetRangeIndicator = _showTargetRangeIndicator.IsChecked;
            _currentProfile.ForceGargoyleWalk = _forceGargoyleWalk.IsChecked;
            _currentProfile.ShowEquipmentDurabilityButton = _showEquipmentDurabilityButton.IsChecked;
            _currentProfile.UseModernJournal = _useModernJournal.IsChecked;
            _currentProfile.HideJournalTimestamp = _hideJournalTimestamp.IsChecked;
            _currentProfile.InvisibleHousesEnabled = _invisibleHousesEnabled.IsChecked;
            _currentProfile.InvisibleHousesZ = _invisibleHousesZ.Value;


            bool updateHealthBars = _currentProfile.CustomBarsToggled != _customBars.IsChecked;
            _currentProfile.CustomBarsToggled = _customBars.IsChecked;

            if (updateHealthBars)
            {
                if (_currentProfile.CustomBarsToggled)
                {
                    List<HealthBarGump> hbgstandard = UIManager.Gumps.OfType<HealthBarGump>().ToList();

                    foreach (HealthBarGump healthbar in hbgstandard)
                    {
                        UIManager.Add(new HealthBarGumpCustom(World, healthbar.LocalSerial) { X = healthbar.X, Y = healthbar.Y });

                        healthbar.Dispose();
                    }
                }
                else
                {
                    List<HealthBarGumpCustom> hbgcustom = UIManager.Gumps.OfType<HealthBarGumpCustom>().ToList();

                    foreach (HealthBarGumpCustom customhealthbar in hbgcustom)
                    {
                        UIManager.Add(new HealthBarGump(World, customhealthbar.LocalSerial) { X = customhealthbar.X, Y = customhealthbar.Y });

                        customhealthbar.Dispose();
                    }
                }
            }

            _currentProfile.CBBlackBGToggled = _customBarsBBG.IsChecked;
            _currentProfile.SaveHealthbars = _saveHealthbars.IsChecked;


            // infobar
            _currentProfile.ShowInfoBar = _showInfoBar.IsChecked;
            _currentProfile.InfoBarHighlightType = _infoBarHighlightType.SelectedIndex;

            World.InfoBars.Clear();

            for (int i = 0; i < _infoBarBuilderControls.Count; i++)
            {
                if (!_infoBarBuilderControls[i].IsDisposed)
                {
                    World.InfoBars.AddItem(new InfoBarItem(_infoBarBuilderControls[i].LabelText, _infoBarBuilderControls[i].Var, _infoBarBuilderControls[i].Hue));
                }
            }

            World.InfoBars.Save();

            InfoBarGump infoBarGump = UIManager.GetGump<InfoBarGump>();

            if (_currentProfile.ShowInfoBar)
            {
                if (infoBarGump == null)
                {
                    UIManager.Add(new InfoBarGump(World) { X = 300, Y = 300 });
                }
                else
                {
                    infoBarGump.ResetItems();
                    infoBarGump.SetInScreen();
                }
            }
            else
            {
                if (infoBarGump != null)
                {
                    infoBarGump.Dispose();
                }
            }


            // containers
            int containerScale = _currentProfile.ContainersScale;

            if ((byte) _containersScale.Value != containerScale || _currentProfile.ScaleItemsInsideContainers != _containerScaleItems.IsChecked)
            {
                containerScale = _currentProfile.ContainersScale = (byte) _containersScale.Value;
                UIManager.ContainerScale = containerScale / 100f;
                _currentProfile.ScaleItemsInsideContainers = _containerScaleItems.IsChecked;

                foreach (ContainerGump resizableGump in UIManager.Gumps.OfType<ContainerGump>())
                {
                    resizableGump.RequestUpdateContents();
                }
            }

            _currentProfile.UseLargeContainerGumps = _useLargeContianersGumps.IsChecked;

            _currentProfile.GridContainerEnabled = _gridContainerEnabled.IsChecked;
            if (_gridContainerEnabled.IsChecked && World.Player != null)
            {
                Item backpack = World.Player.FindItemByLayer(Layer.Backpack);

                if (backpack != null)
                {
                    GridContainer.SetContainerGridStyle(backpack.Serial, false);
                }
            }
            _currentProfile.Grid_HideBorder = _gridHideBorder.IsChecked;
            _currentProfile.GridEnableContPreview = _gridContainerPreview.IsChecked;
            _currentProfile.Grid_DefaultColumns = _gridColumns.Value;
            _currentProfile.Grid_DefaultRows = _gridRows.Value;
            _currentProfile.GridContainersScale = _gridContainerScale.Value;
            _currentProfile.ContainerOpacity = _gridContainerOpacity.Value;
            _currentProfile.GridContainerSearchMode = _gridSearchMode.SelectedIndex;
            _currentProfile.GridBorderAlpha = _gridBorderOpacity.Value;
            _currentProfile.GridBorderHue = _gridBorderHue.Hue;
            _currentProfile.GridContainerBorderHue = _gridContainerBorderHue.Hue;

            for (LinkedListNode<Gump> node = UIManager.Gumps.First; node != null; node = node.Next)
            {
                if (node.Value is GridContainer gridContainer && !gridContainer.IsDisposed)
                {
                    gridContainer.RefreshBorderOptions();
                }
            }

            _currentProfile.DoubleClickToLootInsideContainers = _containerDoubleClickToLoot.IsChecked;
            _currentProfile.RelativeDragAndDropItems = _relativeDragAnDropItems.IsChecked;
            _currentProfile.HighlightContainerWhenSelected = _highlightContainersWhenMouseIsOver.IsChecked;
            _currentProfile.HueContainerGumps = _hueContainerGumps.IsChecked;

            if (_currentProfile.BackpackStyle != _backpackStyle.SelectedIndex)
            {
                _currentProfile.BackpackStyle = _backpackStyle.SelectedIndex;
                UIManager.GetGump<PaperDollGump>(World.Player.Serial)?.RequestUpdateContents();
                Item backpack = World.Player.FindItemByLayer(Layer.Backpack);
                GameActions.DoubleClick(World, backpack);
            }


            // tooltip
            _currentProfile.UseTooltip = _use_tooltip.IsChecked;
            _currentProfile.TooltipTextHue = _tooltip_font_hue.Hue;
            _currentProfile.TooltipDelayBeforeDisplay = _delay_before_display_tooltip.Value;
            _currentProfile.TooltipBackgroundOpacity = _tooltip_background_opacity.Value;
            _currentProfile.TooltipDisplayZoom = _tooltip_zoom.Value;
            _currentProfile.TooltipFont = _tooltip_font_selector.GetSelectedFont();

            _currentProfile?.Save(World, ProfileManager.ProfilePath);
        }

        internal void UpdateVideo()
        {
            var camera = Client.Game.Scene.Camera;

            _gameWindowPositionX.SetText(camera.Bounds.X.ToString());
            _gameWindowPositionY.SetText(camera.Bounds.Y.ToString());
            _gameWindowWidth.SetText(camera.Bounds.Width.ToString());
            _gameWindowHeight.SetText(camera.Bounds.Height.ToString());
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.Draw
            (
                LogoTexture,
                new Rectangle
                (
                    x + 190,
                    y + 20,
                    WIDTH - 250,
                    400
                ),
                hueVector
            );

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width,
                Height,
                hueVector
            );

            bool drawn = base.Draw(batcher, x, y);

            if (_optionsSearchAppliedTerm.Length >= 2)
            {
                Texture2D white = SolidColorTextureCache.GetTexture(Color.White);
                const int border = 2;
                const int pad = 1;

                foreach (Control ch in Children)
                {
                    if (ch is not NiceButton nb || ch.Page != 0 || nb.IsDisposed || !nb.IsVisible)
                    {
                        continue;
                    }

                    if (nb.ButtonParameter < 1 || nb.ButtonParameter > 13)
                    {
                        continue;
                    }

                    if (!_optionsSearchPagesWithHits.Contains(nb.ButtonParameter))
                    {
                        continue;
                    }

                    GetControlPositionInOptionsGump(this, nb, out int tx, out int ty);
                    DrawOptionsSearchHighlightBorder(batcher, white, hueVector, x + tx - pad, y + ty - pad, Math.Max(4, nb.Width) + pad * 2, Math.Max(4, nb.Height) + pad * 2, border);
                }

                if (ActivePage > 0 && _optionsSearchAppliedTerm.Length < 2)
                {
                    for (int i = 0; i < _optionsSearchMatches.Count; i++)
                    {
                        Control c = _optionsSearchMatches[i];

                        if (c == null || c.IsDisposed || !c.IsVisible || OptionsSearchContentPage(c) != ActivePage)
                        {
                            continue;
                        }

                        GetControlPositionInOptionsGump(this, c, out int gx, out int gy);
                        int w = Math.Max(4, c.Width);
                        int h = Math.Max(4, c.Height);

                        if (!OptionsSearchHighlightVisible(c, gx, gy, w, h))
                        {
                            continue;
                        }

                        DrawOptionsSearchHighlightBorder(batcher, white, hueVector, x + gx - pad, y + gy - pad, w + pad * 2, h + pad * 2, border);
                    }
                }
            }

            return drawn;
        }

        private void SetMovementDelayInputs(int turnDelay, int fastTurnDelay, int walkingDelay, int playerWalkingDelay)
        {
            _movementTurnDelay?.SetText(Math.Clamp(turnDelay, 40, 1000).ToString());
            _movementTurnDelayFast?.SetText(Math.Clamp(fastTurnDelay, 40, 1000).ToString());
            _movementWalkingDelay?.SetText(Math.Clamp(walkingDelay, 40, 1000).ToString());
            _movementPlayerWalkingDelay?.SetText(Math.Clamp(playerWalkingDelay, 40, 1000).ToString());
        }

        private InputField AddInputField
        (
            ScrollArea area,
            int x,
            int y,
            int width,
            int height,
            string label = null,
            int maxWidth = 0,
            bool set_down = false,
            bool numbersOnly = false,
            int maxCharCount = -1
        )
        {
            InputField elem = new InputField
            (
                0x0BB8,
                FONT,
                HUE_FONT,
                true,
                width,
                height,
                maxWidth,
                maxCharCount
            )
            {
                NumbersOnly = numbersOnly,
                X = x,
                Y = y
            };


            if (area != null)
            {
                Label text = AddLabel(area, label, x, y);

                if (set_down)
                {
                    elem.Y = text.Bounds.Bottom + 2;
                }
                else
                {
                    elem.X = text.Bounds.Right + 2;
                }

                area.Add(elem);
            }

            return elem;
        }

        private Label AddLabel(ScrollArea area, string text, int x, int y)
        {
            Label label = new Label(text, true, HUE_FONT)
            {
                X = x,
                Y = y
            };

            area?.Add(label);

            return label;
        }

        private Checkbox AddCheckBox(ScrollArea area, string text, bool ischecked, int x, int y)
        {
            Checkbox box = new Checkbox
            (
                0x00D2,
                0x00D3,
                text,
                FONT,
                HUE_FONT
            )
            {
                IsChecked = ischecked,
                X = x,
                Y = y
            };

            area?.Add(box);

            return box;
        }

        private Combobox AddCombobox
        (
            ScrollArea area,
            string[] values,
            int currentIndex,
            int x,
            int y,
            int width
        )
        {
            Combobox combobox = new Combobox(x, y, width, values)
            {
                SelectedIndex = currentIndex
            };

            area?.Add(combobox);

            return combobox;
        }

        private HSliderBar AddHSlider
        (
            ScrollArea area,
            int min,
            int max,
            int value,
            int x,
            int y,
            int width
        )
        {
            HSliderBar slider = new HSliderBar
            (
                x,
                y,
                width,
                min,
                max,
                value,
                HSliderBarStyle.MetalWidgetRecessedBar,
                true,
                FONT,
                HUE_FONT
            );

            area?.Add(slider);

            return slider;
        }

        private ClickableColorBox AddColorBox(ScrollArea area, int x, int y, ushort hue, string text)
        {
            ClickableColorBox box = new ClickableColorBox
            (
                this.World,
                x,
                y,
                13,
                14,
                hue
            );

            area?.Add(box);

            area?.Add
            (
                new Label(text, true, HUE_FONT)
                {
                    X = x + box.Width + 10,
                    Y = y
                }
            );

            return box;
        }

        private SettingsSection AddSettingsSection(DataBox area, string label)
        {
            SettingsSection section = new SettingsSection(label, area.Width);
            area.Add(section);
            area.WantUpdateSize = true;
            //area.ReArrangeChildren();

            return section;
        }

        protected override void OnDragBegin(int x, int y)
        {
            if (UIManager.MouseOverControl?.RootParent == this)
            {
                UIManager.MouseOverControl.InvokeDragBegin(new Point(x, y));
            }

            base.OnDragBegin(x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (UIManager.MouseOverControl?.RootParent == this)
            {
                UIManager.MouseOverControl.InvokeDragEnd(new Point(x, y));
            }

            base.OnDragEnd(x, y);
        }

        private enum Buttons
        {
            Disabled, //no action will be done on these buttons, at least not by OnButtonClick()
            Cancel,
            Apply,
            Default,
            Ok,
            SpeechColor,
            EmoteColor,
            PartyMessageColor,
            GuildMessageColor,
            AllyMessageColor,
            InnocentColor,
            FriendColor,
            CriminalColor,
            EnemyColor,
            MurdererColor,

            OpenIgnoreList,
            NewMacro,
            DeleteMacro,

            Last = DeleteMacro
        }


        private class SettingsSection : Control
        {
            private readonly DataBox _databox;
            private int _indent;

            public SettingsSection(string title, int width)
            {
                CanMove = true;
                AcceptMouseInput = true;
                WantUpdateSize = true;


                Label label = new Label(title, true, HUE_FONT, font: FONT);
                label.X = 5;
                base.Add(label);

                base.Add
                (
                    new Line
                    (
                        0,
                        label.Height,
                        width - 30,
                        1,
                        0xFFbabdc2
                    )
                );

                Width = width;
                Height = label.Height + 1;

                _databox = new DataBox(label.X + 10, label.Height + 4, 0, 0);

                base.Add(_databox);
            }

            public void PushIndent()
            {
                _indent += 40;
            }

            public void PopIndent()
            {
                _indent -= 40;
            }


            public void AddRight(Control c, int offset = 15)
            {
                int i = _databox.Children.Count - 1;

                for (; i >= 0; --i)
                {
                    if (_databox.Children[i].IsVisible)
                    {
                        break;
                    }
                }

                c.X = i >= 0 ? _databox.Children[i].Bounds.Right + offset : _indent;

                c.Y = i >= 0 ? _databox.Children[i].Bounds.Top : 0;

                _databox.Add(c);
                _databox.WantUpdateSize = true;
                SyncContentHeight();
            }

            public void SyncContentHeight()
            {
                const int headerGap = 4;
                int titleHeight = Children.Count > 0 && Children[0] is Label title ? title.Height + 1 : 0;
                int maxChildBottom = 0;

                for (int i = 0; i < _databox.Children.Count; i++)
                {
                    Control child = _databox.Children[i];

                    if (child.IsVisible)
                    {
                        maxChildBottom = Math.Max(maxChildBottom, child.Bounds.Bottom);
                    }
                }

                Height = titleHeight + headerGap + maxChildBottom;
            }

            public override void Add(Control c, int page = 0)
            {
                int i = _databox.Children.Count - 1;
                int bottom = 0;

                for (; i >= 0; --i)
                {
                    if (_databox.Children[i].IsVisible)
                    {
                        if (bottom == 0 || bottom < _databox.Children[i].Bounds.Bottom + 2)
                        {
                            bottom = _databox.Children[i].Bounds.Bottom + 2;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                c.X = _indent;
                c.Y = bottom;

                _databox.Add(c, page);
                _databox.WantUpdateSize = true;
                SyncContentHeight();
            }

            public void ApplySearchFilter(bool filter, string term, HashSet<Control> matches)
            {
                if (!filter)
                {
                    IsVisible = true;

                    for (int i = 0; i < _databox.Children.Count; i++)
                    {
                        _databox.Children[i].IsVisible = true;
                    }

                    _databox.WantUpdateSize = true;
                    SyncContentHeight();

                    return;
                }

                bool sectionTitleMatch = Children.Count > 0
                    && Children[0] is Label title
                    && !string.IsNullOrEmpty(title.Text)
                    && title.Text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
                bool anyVisible = sectionTitleMatch;

                for (int i = 0; i < _databox.Children.Count; i++)
                {
                    _databox.Children[i].IsVisible = false;
                }

                for (int i = 0; i < _databox.Children.Count; i++)
                {
                    Control child = _databox.Children[i];

                    if (!matches.Contains(child))
                    {
                        continue;
                    }

                    ShowOptionsSearchRowPeers(_databox, child);
                    anyVisible = true;
                }

                IsVisible = anyVisible;
                _databox.WantUpdateSize = true;
                SyncContentHeight();
            }
        }

        private class FontSelector : Control
        {
            private readonly RadioButton[] _buttons;

            public FontSelector(int max_font, int current_font_index, string markup)
            {
                CanMove = false;
                CanCloseWithRightClick = false;

                int y = 0;

                _buttons = new RadioButton[max_font];

                for (byte i = 0; i < max_font; i++)
                {
                    if (FontsLoader.Instance.UnicodeFontExists(i))
                    {
                        Add
                        (
                            _buttons[i] = new RadioButton
                            (
                                0,
                                0x00D0,
                                0x00D1,
                                markup,
                                i,
                                HUE_FONT
                            )
                            {
                                Y = y,
                                Tag = i,
                                IsChecked = current_font_index == i
                            }
                        );

                        y += 25;
                    }
                }
            }

            public byte GetSelectedFont()
            {
                for (byte i = 0; i < _buttons.Length; i++)
                {
                    RadioButton b = _buttons[i];

                    if (b != null && b.IsChecked)
                    {
                        return i;
                    }
                }

                return 0xFF;
            }

            public void SetSelectedFont(int index)
            {
                if (index >= 0 && index < _buttons.Length && _buttons[index] != null)
                {
                    _buttons[index].IsChecked = true;
                }
            }
        }

        private class InputField : Control
        {
            private readonly StbTextBox _textbox;

            public InputField
            (
                ushort backgroundGraphic,
                byte font,
                ushort hue,
                bool unicode,
                int width,
                int height,
                int maxWidthText = 0,
                int maxCharsCount = -1
            )
            {
                WantUpdateSize = false;
                AcceptMouseInput = true;

                Width = width;
                Height = height;

                ResizePic background = new ResizePic(backgroundGraphic)
                {
                    Width = width,
                    Height = height,
                    AcceptMouseInput = false
                };

                _textbox = new StbTextBox
                (
                    font,
                    maxCharsCount,
                    maxWidthText,
                    unicode,
                    FontStyle.BlackBorder,
                    hue
                )
                {
                    X = 2,
                    Y = 2,
                    Width = Math.Max(1, width - 4),
                    Height = Math.Max(1, height - 4)
                };

                Add(background);
                Add(_textbox);

                // After _textbox exists: property getter/setter forwards to it.
                AcceptKeyboardInput = true;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    base.Draw(batcher, x, y);

                    batcher.ClipEnd();
                }

                return true;
            }


            public string Text => _textbox.Text;

            public override bool AcceptKeyboardInput
            {
                get => _textbox.AcceptKeyboardInput;
                set => _textbox.AcceptKeyboardInput = value;
            }

            public bool NumbersOnly
            {
                get => _textbox.NumbersOnly;
                set => _textbox.NumbersOnly = value;
            }


            public void SetText(string text)
            {
                _textbox.SetText(text);
            }
        }
    }
}