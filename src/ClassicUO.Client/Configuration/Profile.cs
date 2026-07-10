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
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using ClassicUO.Configuration.Json;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    //[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
    [JsonSerializable(typeof(Profile), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(MessageType))]
    [JsonSerializable(typeof(MessageType[]))]
    [JsonSerializable(typeof(Dictionary<string, MessageType[]>))]
    sealed partial class ProfileJsonContext : JsonSerializerContext
    {
        sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

            public override string ConvertName(string name)
            {
                // Conversion to other naming convention goes here. Like SnakeCase, KebabCase etc.
                return string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
            }
        }

        private static Lazy<JsonSerializerOptions> _jsonOptions { get; } = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions();
            options.WriteIndented = true;
            options.PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance;
            return options;
        });

        public static ProfileJsonContext DefaultToUse { get; } = new ProfileJsonContext(_jsonOptions.Value);
    }



    internal sealed class Profile
    {
        [JsonIgnore] public string Username { get; set; }
        [JsonIgnore] public string ServerName { get; set; }
        [JsonIgnore] public string CharacterName { get; set; }

        // sounds
        public bool EnableSound { get; set; } = true;
        public int SoundVolume { get; set; } = 8;
        public bool EnableMusic { get; set; } = false;
        public int MusicVolume { get; set; } = 100;
        public bool EnableFootstepsSound { get; set; } = false;
        public bool EnableCombatMusic { get; set; } = false;
        public bool ReproduceSoundsInBackground { get; set; }

        // fonts and speech
        public byte ChatFont { get; set; } = 1;
        public int SpeechDelay { get; set; } = 100;
        public bool ScaleSpeechDelay { get; set; } = true;
        public bool SaveJournalToFile { get; set; } = false;
        public bool ForceUnicodeJournal { get; set; }
        public bool IgnoreAllianceMessages { get; set; }
        public bool IgnoreGuildMessages { get; set; }

        // hues
        public ushort SpeechHue { get; set; } = 0x02B2;
        public ushort WhisperHue { get; set; } = 0x0033;
        public ushort EmoteHue { get; set; } = 0x0021;
        public ushort YellHue { get; set; } = 0x0021;
        public ushort PartyMessageHue { get; set; } = 0x0044;
        public ushort GuildMessageHue { get; set; } = 0x0044;
        public ushort AllyMessageHue { get; set; } = 0x0057;
        public ushort ChatMessageHue { get; set; } = 0x0256;
        public ushort InnocentHue { get; set; } = 0x005A;
        public ushort PartyAuraHue { get; set; } = 0x0044;
        public ushort FriendHue { get; set; } = 0x0044;
        public ushort CriminalHue { get; set; } = 0x03B2;
        public ushort CanAttackHue { get; set; } = 0x03B2;
        public ushort EnemyHue { get; set; } = 0x0031;
        public ushort MurdererHue { get; set; } = 0x0023;
        public ushort BeneficHue { get; set; } = 0x0059;
        public ushort HarmfulHue { get; set; } = 0x0020;
        public ushort NeutralHue { get; set; } = 0x03B1;
        public bool EnabledSpellHue { get; set; } = true;
        public bool EnabledSpellFormat { get; set; } = true;
        public string SpellDisplayFormat { get; set; } = "{power} [{spell}]";
        public ushort PoisonHue { get; set; } = 0x0044;
        public ushort ParalyzedHue { get; set; } = 0x014C;
        public ushort InvulnerableHue { get; set; } = 0x0030;

        // visual
        public bool EnabledCriminalActionQuery { get; set; } = false;
        public bool EnabledBeneficialCriminalActionQuery { get; set; } = false;
        public bool EnableStatReport { get; set; } = true;
        public bool EnableSkillReport { get; set; } = true;
        public bool UseOldStatusGump { get; set; }
        public int BackpackStyle { get; set; }
        public bool HighlightGameObjects { get; set; }
        public bool HighlightMobilesByParalize { get; set; } = true;
        public bool HighlightMobilesByPoisoned { get; set; } = true;
        public bool HighlightMobilesByInvul { get; set; } = true;
        public bool ShowMobilesHP { get; set; } = true;
        public int MobileHPType { get; set; } = 2;     // 0 = %, 1 = line, 2 = both
        public int MobileHPShowWhen { get; set; } // 0 = Always, 1 - <100%
        // Old-style overhead bars (classic solid HP/Mana/Stamina bars)
        public bool UseOldHealthBars { get; set; } = true;
        public bool DrawRoofs { get; set; } = false;
        public bool TreeToStumps { get; set; } = true;
        public bool EnableCaveBorder { get; set; } = true;
        public bool HideVegetation { get; set; } = true;
        public int FieldsType { get; set; } = 2; // 0 = normal, 1 = static, 2 = tile
        public bool NoColorObjectsOutOfRange { get; set; }
        public bool UseCircleOfTransparency { get; set; } = true;
        public int CircleOfTransparencyRadius { get; set; } = 90;
        public int CircleOfTransparencyType { get; set; } // 0 = normal, 1 = like original client
        public int VendorGumpHeight { get; set; } = 60;   //original vendor gump size
        public float DefaultScale { get; set; } = 1.0f;
        public bool EnableMousewheelScaleZoom { get; set; } = true;
        public bool SaveScaleAfterClose { get; set; }
        public bool RestoreScaleAfterUnpressCtrl { get; set; }
        public bool BandageSelfOld { get; set; } = true;
        public bool EnableDeathScreen { get; set; } = true;
        public bool EnableBlackWhiteEffect { get; set; } = true;

        // tooltip
        public bool UseTooltip { get; set; } = true;
        public ushort TooltipTextHue { get; set; } = 0xFFFF;
        public int TooltipDelayBeforeDisplay { get; set; } = 250;
        public int TooltipDisplayZoom { get; set; } = 100;
        public int TooltipBackgroundOpacity { get; set; } = 70;
        public byte TooltipFont { get; set; } = 1;

        // movements
        public bool EnablePathfind { get; set; }
        public bool UseShiftToPathfind { get; set; }
        public bool AlwaysRun { get; set; } = true;
        public bool AlwaysRunUnlessHidden { get; set; } = true;
        public bool SmoothMovements { get; set; } = true;
        public bool HoldDownKeyTab { get; set; } = true;
        public bool HoldShiftForContext { get; set; } = false;
        public bool HoldShiftToSplitStack { get; set; } = true;
        public bool ForceGargoyleWalk { get; set; } = true;

        // general
        [JsonConverter(typeof(Point2Converter))] public Point WindowClientBounds { get; set; } = new Point(600, 480);
        [JsonConverter(typeof(Point2Converter))] public Point ContainerDefaultPosition { get; set; } = new Point(24, 24);
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowPosition { get; set; } = new Point(10, 10);
        public bool GameWindowLock { get; set; }
        public bool GameWindowFullSize { get; set; }
        public bool WindowBorderless { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowSize { get; set; } = new Point(600, 480);
        [JsonConverter(typeof(Point2Converter))] public Point TopbarGumpPosition { get; set; } = new Point(0, 0);
        public bool TopbarGumpIsMinimized { get; set; }
        public bool TopbarGumpIsDisabled { get; set; }
        public List<string> AutoOpenXmlGumps { get; set; } = new List<string>();
        public bool UseAlternativeLights { get; set; }
        public bool UseCustomLightLevel { get; set; } = true;
        public byte LightLevel { get; set; }
        public int LightLevelType { get; set; } // 0 = absolute, 1 = minimum
        public bool UseColoredLights { get; set; } = true;
        public bool UseDarkNights { get; set; }
        public int CloseHealthBarType { get; set; } // 0 = none, 1 == not exists, 2 == is dead
        public bool ActivateChatAfterEnter { get; set; }
        public bool ActivateChatAdditionalButtons { get; set; } = true;
        public bool ActivateChatShiftEnterSupport { get; set; } = true;
        public bool UseObjectsFading { get; set; } = true;
        public bool HoldDownKeyAltToCloseAnchored { get; set; } = true;
        public bool CloseAllAnchoredGumpsInGroupWithRightClick { get; set; } = false;
        public bool HoldAltToMoveGumps { get; set; }

        public bool HideScreenshotStoredInMessage { get; set; }

        // Experimental
        public bool CastSpellsByOneClick { get; set; }
        public bool BuffBarTime { get; set; } = true;
        public bool FastSpellsAssign { get; set; }
        public bool AutoOpenDoors { get; set; } = true;
        public bool SmoothDoors { get; set; } = true;
        public bool AutoOpenUiOnLogin { get; set; } = true;
        // Auto Avoid Obstacles
        public bool AvoidObstacles { get; set; } = true;
        public bool AvoidObstaclesIgnoreHumanoids { get; set; } = true;

        // Invisible Houses (Dust765): hide house multi tiles above the player so
        // you can see inside/under roofs and walls. Toggled by the
        // ToggleInvisibleHouses macro.
        public bool InvisibleHousesEnabled { get; set; } = false;
        public int InvisibleHousesZ { get; set; } = 0;
        public int DontRemoveHouseBelowZ { get; set; } = 6;

        // Visual Helpers (Dust765): "Highlight tiles on range". Colors the ring of
        // ground tiles at a fixed distance around the player. Two independent modes:
        // always-on (Activated) and only-while-casting (OnCast). Range and hue are
        // configurable from the Options gump (color picker + slider).
        public bool LTHighlightRangeOnActivated { get; set; } = false;
        public int LTHighlightRangeOnActivatedRange { get; set; } = 10;
        public ushort LTHighlightRangeOnActivatedHue { get; set; } = 0x0074;
        public bool LTHighlightRangeOnCast { get; set; } = true;
        public int LTHighlightRangeOnCastRange { get; set; } = 10;
        public ushort LTHighlightRangeOnCastHue { get; set; } = 0x0017;
        // Range ring diamond outline width in screen pixels (drawn in GameScene, not land tiles).
        public int LTHighlightRangeOutlinePixels { get; set; } = 2;

        // Mirror Image helper: ghost-detect Ninjitsu clones (first-seen heuristic).
        public bool HighlightMirrorImageClones { get; set; } = true;
        public ushort MirrorImageCloneHue { get; set; } = 0x038E;

        // Visual Helpers (Dust765): re-hue / highlight modes (0 = off, 1 = white, 2 = pink, 3 = ice, 4 = fire, 5 = custom).
        // State highlights also support 5 = special preset hue, 6 = custom.
        public int GlowingWeaponsType { get; set; } = 0;
        public ushort HighlightGlowingWeaponsTypeHue { get; set; } = 0x0044;
        public int HighlightLastTargetType { get; set; } = 0;
        public int HighlighFriendsGuildType { get; set; } = 0;
        public ushort HighlightLastTargetTypeHue { get; set; } = 0x0044;
        public ushort HighlighFriendsGuildTypeHue { get; set; } = 0x0044;
        public int HighlightLastTargetTypePoison { get; set; } = 0;
        public ushort HighlightLastTargetTypePoisonHue { get; set; } = 0x0044;
        public int HighlightLastTargetTypePara { get; set; } = 0;
        public ushort HighlightLastTargetTypeParaHue { get; set; } = 0x0044;
        public int HighlightLastTargetTypeStunned { get; set; } = 0;
        public ushort HighlightLastTargetTypeStunnedHue { get; set; } = 0x0044;
        public int HighlightLastTargetTypeMortalled { get; set; } = 0;
        public ushort HighlightLastTargetTypeMortalledHue { get; set; } = 0x0044;

        // UODreams: stub stone-roof tooltip spam instead of requesting OPL from server.
        public bool EnableUoDreamsNetworkOptimizer { get; set; } = true;

        // Drain all pending socket data each frame instead of one ~4KB read.
        public bool EnableFullSocketDrain { get; set; } = true;

        // Movement / ping tuning (Dust765): turn and walking delays in milliseconds.
        public int MovementTurnDelay { get; set; } = 100;
        public int MovementTurnDelayFast { get; set; } = 45;
        public int MovementWalkingDelay { get; set; } = 150;
        public int MovementPlayerWalkingDelay { get; set; } = 150;
        public bool AutoOpenCorpses { get; set; } = true;
        public int AutoOpenCorpseRange { get; set; } = 2;
        public int CorpseOpenOptions { get; set; } = 3;
        public bool SkipEmptyCorpse { get; set; } = true;
        public bool DisableDefaultHotkeys { get; set; }
        public bool DisableArrowBtn { get; set; }
        public bool DisableTabBtn { get; set; }
        public bool DisableCtrlQWBtn { get; set; }
        public bool DisableAutoMove { get; set; }
        public bool EnableDragSelect { get; set; } = true;
        public int DragSelectModifierKey { get; set; } // 0 = none, 1 = control, 2 = shift
        public bool OverrideContainerLocation { get; set; }

        public int OverrideContainerLocationSetting { get; set; } // 0 = container position, 1 = top right of screen, 2 = last dragged position, 3 = remember every container

        [JsonConverter(typeof(Point2Converter))] public Point OverrideContainerLocationPosition { get; set; } = new Point(200, 200);
        public bool HueContainerGumps { get; set; } = true;
        public bool DragSelectHumanoidsOnly { get; set; } = true;
        public int DragSelectStartX { get; set; } = 100;
        public int DragSelectStartY { get; set; } = 100;
        public bool DragSelectAsAnchor { get; set; } = false;
        public NameOverheadTypeAllowed NameOverheadTypeAllowed { get; set; } = NameOverheadTypeAllowed.Mobiles;
        public bool NameOverheadToggled { get; set; } = true;

        // When enabled, suppress the always-on overhead name for decorative /
        // invulnerable (yellow) NPCs - vendors, mannequins, parrots, statues, etc.
        // Single-click names still work; only the persistent label is hidden.
        public bool HidePersistentNPCNames { get; set; } = true;

        // Paperdoll (Dust765): show equipment slot boxes for EVERY wearable layer next
        // to the paperdoll (helmet, jewelry, weapons, robe, torso, arms, legs, etc.).
        // When off, only the base 6 left-column slots are shown.
        public bool ShowAllLayersPaperdoll { get; set; } = true;
        public bool ShowTargetRangeIndicator { get; set; }
        public bool PartyInviteGump { get; set; }
        public bool CustomBarsToggled { get; set; } = true;
        public bool CBBlackBGToggled { get; set; } = true;

        public bool ShowInfoBar { get; set; } = true;
        public int InfoBarHighlightType { get; set; } // 0 = text colour changes, 1 = underline

        public bool CounterBarEnabled { get; set; } = true;
        public bool CounterBarHighlightOnUse { get; set; } = true;
        public bool CounterBarHighlightOnAmount { get; set; } = true;
        public bool CounterBarDisplayAbbreviatedAmount { get; set; }
        public int CounterBarAbbreviatedAmount { get; set; } = 1000;
        public int CounterBarHighlightAmount { get; set; } = 5;
        public int CounterBarCellSize { get; set; } = 40;
        public int CounterBarRows { get; set; } = 10;
        public int CounterBarColumns { get; set; } = 1;

        public bool ShowSkillsChangedMessage { get; set; } = true;
        public int ShowSkillsChangedDeltaValue { get; set; } = 1;
        public bool ShowStatsChangedMessage { get; set; } = true;


        public bool ShadowsEnabled { get; set; } = false;
        public bool ShadowsStatics { get; set; } = false;
        public int TerrainShadowsLevel { get; set; } = 5;
        public int AuraUnderFeetType { get; set; } // 0 = NO, 1 = in warmode, 2 = ctrl+shift, 3 = always
        public bool AuraOnMouse { get; set; } = true;
        public bool AnimatedWaterEffect { get; set; } = false;

        public bool PartyAura { get; set; }

        public bool UseXBR { get; set; } = true;

        public bool HideChatGradient { get; set; } = false;

        public bool StandardSkillsGump { get; set; } = true;

        public bool ShowNewMobileNameIncoming { get; set; } = true;
        public bool ShowNewCorpseNameIncoming { get; set; } = false;

        public uint GrabBagSerial { get; set; }

        public int GridLootType { get; set; } = 2; // 0 = none, 1 = only grid, 2 = both

        public bool ReduceFPSWhenInactive { get; set; } = false;

        public bool OverrideAllFonts { get; set; }
        public bool OverrideAllFontsIsUnicode { get; set; } = true;

        public bool SallosEasyGrab { get; set; }

        public bool JournalDarkMode { get; set; }

        public byte ContainersScale { get; set; } = 100;

        public bool ScaleItemsInsideContainers { get; set; }

        public bool DoubleClickToLootInsideContainers { get; set; }

        public bool UseLargeContainerGumps { get; set; } = true;

        // ---- Grid Container ----
        public bool GridContainerEnabled { get; set; } = true;
        public int GridContainersScale { get; set; } = 100;
        public bool GridContainerScaleItems { get; set; } = true;
        [JsonConverter(typeof(Point2Converter))] public Point BackpackGridPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point BackpackGridSize { get; set; } = new Point(300, 300);
        public int Grid_DefaultColumns { get; set; } = 4;
        public int Grid_DefaultRows { get; set; } = 4;
        public bool Grid_UseContainerHue { get; set; } = false;
        public ushort AltGridContainerBackgroundHue { get; set; } = 0;
        public int ContainerOpacity { get; set; } = 50;
        public bool Grid_HideBorder { get; set; } = true;
        public int GridContainerSearchMode { get; set; } = 1; // 0 = filter/hide, 1 = highlight
        public bool CorpseSingleClickLoot { get; set; } = false;
        public ushort GridBorderHue { get; set; } = 0;
        public int GridBorderAlpha { get; set; } = 75;
        public bool GridEnableContPreview { get; set; } = true;

        public bool RelativeDragAndDropItems { get; set; }

        public bool HighlightContainerWhenSelected { get; set; }

        public bool ShowHouseContent { get; set; }
        public bool SaveHealthbars { get; set; } = true;
        public bool TextFading { get; set; } = true;

        public bool UseSmoothBoatMovement { get; set; } = false;

        public bool IgnoreStaminaCheck { get; set; } = false;

        public bool ShowJournalClient { get; set; } = true;
        public bool ShowJournalObjects { get; set; } = true;
        public bool ShowJournalSystem { get; set; } = true;
        public bool ShowJournalGuildAlly { get; set; } = true;

        // Modern journal (TazUO-style)
        public bool UseModernJournal { get; set; } = true;
        public bool HideJournalTimestamp { get; set; }
        public int JournalOpacity { get; set; } = 100;
        public int MaxJournalEntries { get; set; } = 200;
        public int LastJournalTab { get; set; }
        [JsonConverter(typeof(Point2Converter))] public Point JournalPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point ResizeJournalSize { get; set; } = new Point(400, 300);
        public Dictionary<string, MessageType[]> JournalTabs { get; set; } = CreateDefaultJournalTabs();

        // Equipment durability tracker
        public bool ShowEquipmentDurabilityButton { get; set; } = true;

        public int WorldMapWidth { get; set; } = 400;
        public int WorldMapHeight { get; set; } = 400;
        public int WorldMapFont { get; set; } = 3;
        public bool WorldMapFlipMap { get; set; } = true;
        public bool WorldMapTopMost { get; set; }
        public bool WorldMapFreeView { get; set; }
        public bool WorldMapShowParty { get; set; } = true;
        public int WorldMapZoomIndex { get; set; } = 4;
        public bool WorldMapShowCoordinates { get; set; } = true;
        public bool WorldMapShowMouseCoordinates { get; set; } = true;
        public bool WorldMapShowMobiles { get; set; } = true;
        public bool WorldMapShowPlayerName { get; set; } = true;
        public bool WorldMapShowPlayerBar { get; set; } = true;
        public bool WorldMapShowGroupName { get; set; } = true;
        public bool WorldMapShowGroupBar { get; set; } = true;
        public bool WorldMapShowMarkers { get; set; } = true;
        public bool WorldMapShowMarkersNames { get; set; } = true;
        public bool WorldMapShowMultis { get; set; } = true;
        public string WorldMapHiddenMarkerFiles { get; set; } = string.Empty;
        public string WorldMapHiddenZoneFiles { get; set; } = string.Empty;
        public bool WorldMapShowGridIfZoomed { get; set; } = true;
        public bool WorldMapAllowPositionalTarget { get; set; } = false;


        public static uint GumpsVersion { get; private set; }

        public static Dictionary<string, MessageType[]> CreateDefaultJournalTabs()
        {
            return new Dictionary<string, MessageType[]>
            {
                {
                    "All",
                    new[]
                    {
                        MessageType.Alliance, MessageType.Command, MessageType.Emote,
                        MessageType.Encoded, MessageType.Focus, MessageType.Guild,
                        MessageType.Label, MessageType.Limit3Spell, MessageType.Party,
                        MessageType.Regular, MessageType.Spell, MessageType.System,
                        MessageType.Whisper, MessageType.Yell
                    }
                },
                {
                    "Chat",
                    new[]
                    {
                        MessageType.Regular, MessageType.Guild, MessageType.Alliance,
                        MessageType.Emote, MessageType.Party, MessageType.Whisper, MessageType.Yell
                    }
                },
                {
                    "Guild|Party",
                    new[] { MessageType.Guild, MessageType.Alliance, MessageType.Party }
                },
                {
                    "System",
                    new[] { MessageType.System }
                }
            };
        }

        public void Save(World world, string path)
        {
            Log.Trace($"Saving path:\t\t{path}");

            // Save opened gumps first so grid backpack layout is written into the profile
            SaveGumps(world, path);

            // Save profile settings after gumps updated in-memory values (e.g. backpack grid position)
            ConfigurationResolver.Save(this, Path.Combine(path, "profile.json"), ProfileJsonContext.DefaultToUse.Profile);

            Log.Trace("Saving done!");
        }

        private void SaveGumps(World world, string path)
        {
            string gumpsXmlPath = Path.Combine(path, "gumps.xml");

            using (XmlTextWriter xml = new XmlTextWriter(gumpsXmlPath, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("gumps");

                UIManager.AnchorManager.Save(xml);

                LinkedList<Gump> gumps = new LinkedList<Gump>();

                foreach (Gump gump in UIManager.Gumps)
                {
                    if (!gump.IsDisposed && gump.CanBeSaved && !(gump is AnchorableGump anchored && UIManager.AnchorManager[anchored] != null))
                    {
                        gumps.AddLast(gump);
                    }
                }
                
                LinkedListNode<Gump> first = gumps.First;

                while (first != null)
                {
                    Gump gump = first.Value;

                    if (gump.LocalSerial != 0)
                    {
                        Item item = world.Items.Get(gump.LocalSerial);

                        if (item != null && !item.IsDestroyed && item.Opened)
                        {
                            while (SerialHelper.IsItem(item.Container))
                            {
                                item = world.Items.Get(item.Container);
                            }

                            SaveItemsGumpRecursive(item, xml, gumps);

                            if (first.List != null)
                            {
                                gumps.Remove(first);
                            }

                            first = gumps.First;

                            continue;
                        }
                    }

                    xml.WriteStartElement("gump");
                    gump.Save(xml);
                    xml.WriteEndElement();

                    if (first.List != null)
                    {
                        gumps.Remove(first);
                    }

                    first = gumps.First;
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }


            world.SkillsGroupManager.Save();
        }

        private static void SaveItemsGumpRecursive(Item parent, XmlTextWriter xml, LinkedList<Gump> list)
        {
            if (parent != null && !parent.IsDestroyed && parent.Opened)
            {
                SaveItemsGump(parent, xml, list);

                Item first = (Item) parent.Items;

                while (first != null)
                {
                    Item next = (Item) first.Next;

                    SaveItemsGumpRecursive(first, xml, list);

                    first = next;
                }
            }
        }

        private static void SaveItemsGump(Item item, XmlTextWriter xml, LinkedList<Gump> list)
        {
            if (item != null && !item.IsDestroyed && item.Opened)
            {
                LinkedListNode<Gump> first = list.First;

                while (first != null)
                {
                    LinkedListNode<Gump> next = first.Next;

                    if (first.Value.LocalSerial == item.Serial && !first.Value.IsDisposed)
                    {
                        xml.WriteStartElement("gump");
                        first.Value.Save(xml);
                        xml.WriteEndElement();

                        list.Remove(first);

                        break;
                    }

                    first = next;
                }
            }
        }


        public List<Gump> ReadGumps(World world, string path)
        {
            List<Gump> gumps = new List<Gump>();

            // load skillsgroup
            world.SkillsGroupManager.Load();

            // load gumps
            string gumpsXmlPath = Path.Combine(path, "gumps.xml");

            if (File.Exists(gumpsXmlPath))
            {
                XmlDocument doc = new XmlDocument();

                try
                {
                    doc.Load(gumpsXmlPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());

                    return gumps;
                }

                XmlElement root = doc["gumps"];

                if (root != null)
                {
                    foreach (XmlElement xml in root.ChildNodes /*.GetElementsByTagName("gump")*/)
                    {
                        if (xml.Name != "gump")
                        {
                            continue;
                        }

                        try
                        {
                            GumpType type = (GumpType) int.Parse(xml.GetAttribute(nameof(type)));
                            int x = int.Parse(xml.GetAttribute(nameof(x)));
                            int y = int.Parse(xml.GetAttribute(nameof(y)));
                            uint serial = uint.Parse(xml.GetAttribute(nameof(serial)));

                            Gump gump = null;

                            switch (type)
                            {
                                case GumpType.Buff:
                                    gump = new BuffGump(world);

                                    break;

                                case GumpType.Container:
                                    gump = new ContainerGump(world);

                                    break;

                                case GumpType.GridContainer:
                                    gump = new GridContainer(world, serial, 0);

                                    break;

                                case GumpType.CounterBar:
                                    gump = new CounterBarGump(world);

                                    break;

                                case GumpType.HealthBar:
                                    if (CustomBarsToggled)
                                    {
                                        gump = new HealthBarGumpCustom(world);
                                    }
                                    else
                                    {
                                        gump = new HealthBarGump(world);
                                    }

                                    break;

                                case GumpType.InfoBar:
                                    gump = new InfoBarGump(world);

                                    break;

                                case GumpType.Journal:
                                    if (UseModernJournal)
                                    {
                                        gump = new ResizableJournal(world);
                                    }
                                    else
                                    {
                                        gump = new JournalGump(world);
                                    }

                                    break;

                                case GumpType.DurabilityGump:
                                    gump = new DurabilitysGump(world);

                                    break;

                                case GumpType.MacroButton:
                                    gump = new MacroButtonGump(world);

                                    break;

                                case GumpType.MiniMap:
                                    gump = new MiniMapGump(world);

                                    break;

                                case GumpType.PaperDoll:
                                    gump = new PaperDollGump(world);

                                    break;

                                case GumpType.SkillMenu:
                                    if (StandardSkillsGump)
                                    {
                                        gump = new StandardSkillsGump(world);
                                    }
                                    else
                                    {
                                        gump = new SkillGumpAdvanced(world);
                                    }

                                    break;

                                case GumpType.SpellBook:
                                    gump = new SpellbookGump(world);

                                    break;

                                case GumpType.StatusGump:
                                    gump = StatusGumpBase.AddStatusGump(world, 0, 0);

                                    break;

                                //case GumpType.TipNotice:
                                //    gump = new TipNoticeGump();
                                //    break;
                                case GumpType.AbilityButton:
                                    gump = new UseAbilityButtonGump(world);

                                    break;

                                case GumpType.SpellButton:
                                    gump = new UseSpellButtonGump(world);

                                    break;

                                case GumpType.SkillButton:
                                    gump = new SkillButtonGump(world);

                                    break;

                                case GumpType.RacialButton:
                                    gump = new RacialAbilityButton(world);

                                    break;

                                case GumpType.WorldMap:
                                    gump = new WorldMapGump(world);

                                    break;

                                case GumpType.Debug:
                                    gump = new DebugGump(world, 100, 100);

                                    break;

                                case GumpType.NetStats:
                                    gump = new NetworkStatsGump(world, 100, 100);

                                    break;

                                case GumpType.NameOverHeadHandler:
                                    NameOverHeadHandlerGump.LastPosition = new Point(x, y);
                                    // Gump gets opened by NameOverHeadManager, we just want to save the last position from profile
                                    break;
                            }

                            if (gump == null)
                            {
                                continue;
                            }

                            gump.LocalSerial = serial;
                            gump.Restore(xml);
                            gump.X = x;
                            gump.Y = y;

                            if (gump.LocalSerial != 0)
                            {
                                UIManager.SavePosition(gump.LocalSerial, new Point(x, y));
                            }

                            if (!gump.IsDisposed)
                            {
                                gumps.Add(gump);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }

                    foreach (XmlElement group in root.GetElementsByTagName("anchored_group_gump"))
                    {
                        int matrix_width = int.Parse(group.GetAttribute("matrix_w"));
                        int matrix_height = int.Parse(group.GetAttribute("matrix_h"));

                        AnchorManager.AnchorGroup ancoGroup = new AnchorManager.AnchorGroup();
                        ancoGroup.ResizeMatrix(matrix_width, matrix_height, 0, 0);

                        foreach (XmlElement xml in group.GetElementsByTagName("gump"))
                        {
                            try
                            {
                                GumpType type = (GumpType) int.Parse(xml.GetAttribute("type"));
                                int x = int.Parse(xml.GetAttribute("x"));
                                int y = int.Parse(xml.GetAttribute("y"));
                                uint serial = uint.Parse(xml.GetAttribute("serial"));

                                int matrix_x = int.Parse(xml.GetAttribute("matrix_x"));
                                int matrix_y = int.Parse(xml.GetAttribute("matrix_y"));

                                AnchorableGump gump = null;

                                switch (type)
                                {
                                    case GumpType.SpellButton:
                                        gump = new UseSpellButtonGump(world);

                                        break;

                                    case GumpType.SkillButton:
                                        gump = new SkillButtonGump(world);

                                        break;

                                    case GumpType.HealthBar:
                                        if (CustomBarsToggled)
                                        {
                                            gump = new HealthBarGumpCustom(world);
                                        }
                                        else
                                        {
                                            gump = new HealthBarGump(world);
                                        }

                                        break;

                                    case GumpType.AbilityButton:
                                        gump = new UseAbilityButtonGump(world);

                                        break;

                                    case GumpType.MacroButton:
                                        gump = new MacroButtonGump(world);

                                        break;
                                }

                                if (gump != null)
                                {
                                    gump.LocalSerial = serial;
                                    gump.Restore(xml);
                                    gump.X = x;
                                    gump.Y = y;

                                    if (!gump.IsDisposed)
                                    {
                                        if (UIManager.AnchorManager[gump] == null && ancoGroup.IsEmptyDirection(matrix_x, matrix_y))
                                        {
                                            gumps.Add(gump);
                                            UIManager.AnchorManager[gump] = ancoGroup;
                                            ancoGroup.AddControlToMatrix(matrix_x, matrix_y, gump);
                                        }
                                        else
                                        {
                                            gump.Dispose();
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                        }
                    }
                }
            }

            return gumps;
        }
    }
}