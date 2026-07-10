#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game
{
    internal static class VisualHighlightHelper
    {
        public const ushort BrightWhiteColor = 0x080A;
        public const ushort BrightPinkColor = 0x0503;
        public const ushort BrightIceColor = 0x0480;
        public const ushort BrightFireColor = 0x0496;
        public const ushort BrightPoisonColor = 0x0A0B;
        public const ushort BrightParalyzeColor = 0x0A13;

        public static ushort LastFriendHue(World world, Mobile mobile, ushort hue)
        {
            WMapEntity wme = world.WMapManager.GetEntity(mobile.Serial);

            bool isFriendOrGuild = mobile.NotorietyFlag == NotorietyFlag.Ally
                || world.Party.Contains(mobile.Serial)
                || (wme != null && wme.IsGuild);

            if (!isFriendOrGuild)
            {
                return hue;
            }

            Profile profile = ProfileManager.CurrentProfile;

            return profile.HighlighFriendsGuildType switch
            {
                1 => BrightWhiteColor,
                2 => BrightPinkColor,
                3 => BrightIceColor,
                4 => BrightFireColor,
                5 => profile.HighlighFriendsGuildTypeHue,
                _ => hue
            };
        }

        public static ushort WeaponsHue(ushort hue)
        {
            Profile profile = ProfileManager.CurrentProfile;

            return profile.GlowingWeaponsType switch
            {
                1 => BrightWhiteColor,
                2 => BrightPinkColor,
                3 => BrightIceColor,
                4 => BrightFireColor,
                5 => profile.HighlightGlowingWeaponsTypeHue,
                _ => hue
            };
        }

        public static ushort LastTargetHue(Mobile mobile, ushort hue)
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile.HighlightLastTargetType == 1)
            {
                hue = BrightWhiteColor;
            }
            else if (profile.HighlightLastTargetType == 2)
            {
                hue = BrightPinkColor;
            }
            else if (profile.HighlightLastTargetType == 3)
            {
                hue = BrightIceColor;
            }
            else if (profile.HighlightLastTargetType == 4)
            {
                hue = BrightFireColor;
            }
            else if (profile.HighlightLastTargetType == 5)
            {
                hue = profile.HighlightLastTargetTypeHue;
            }

            if (mobile.IsPoisoned)
            {
                hue = profile.HighlightLastTargetTypePoison switch
                {
                    1 => BrightWhiteColor,
                    2 => BrightPinkColor,
                    3 => BrightIceColor,
                    4 => BrightFireColor,
                    5 => BrightPoisonColor,
                    6 => profile.HighlightLastTargetTypePoisonHue,
                    _ => hue
                };
            }

            if (mobile.IsParalyzed)
            {
                hue = profile.HighlightLastTargetTypePara switch
                {
                    1 => BrightWhiteColor,
                    2 => BrightPinkColor,
                    3 => BrightIceColor,
                    4 => BrightFireColor,
                    5 => BrightParalyzeColor,
                    6 => profile.HighlightLastTargetTypeParaHue,
                    _ => hue
                };
            }

            if (mobile.IsParalyzed)
            {
                hue = profile.HighlightLastTargetTypeStunned switch
                {
                    1 => BrightWhiteColor,
                    2 => BrightPinkColor,
                    3 => BrightIceColor,
                    4 => BrightFireColor,
                    5 => BrightParalyzeColor,
                    6 => profile.HighlightLastTargetTypeStunnedHue,
                    _ => hue
                };
            }

            if (mobile.IsYellowHits)
            {
                hue = profile.HighlightLastTargetTypeMortalled switch
                {
                    1 => BrightWhiteColor,
                    2 => BrightPinkColor,
                    3 => BrightIceColor,
                    4 => BrightFireColor,
                    5 => 0x0035,
                    6 => profile.HighlightLastTargetTypeMortalledHue,
                    _ => hue
                };
            }

            return hue;
        }
    }
}
