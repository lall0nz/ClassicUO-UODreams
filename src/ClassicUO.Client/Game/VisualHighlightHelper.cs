#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using ClassicUO;
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
        public const ushort BrightMortalColor = 0x0035;

        private static Func<int, bool> _razorIsFriend;
        private static bool _razorResolveAttempted;
        private static uint _nextRazorResolveTicks;
        private static readonly Dictionary<uint, (bool isFriend, uint expire)> _friendCache = new();
        private const uint FriendCacheMs = 500;

        /// <summary>
        /// Friend/guild highlight color for a mobile, or <paramref name="hue"/> unchanged.
        /// Applies regardless of notoriety (innocent/murderer/etc). Never tints the local player.
        /// </summary>
        public static ushort LastFriendHue(World world, Mobile mobile, ushort hue)
        {
            if (!TryGetFriendsGuildHighlightHue(world, mobile, out ushort friendHue))
            {
                return hue;
            }

            return friendHue;
        }

        /// <summary>
        /// Name/overhead hue: always original notoriety/guild color.
        /// Friend Mods highlight applies to body/aura/mount only — not name text.
        /// </summary>
        public static ushort GetMobileNameHue(World world, Mobile mobile)
        {
            if (mobile == null)
            {
                return 0x0481;
            }

            return Notoriety.GetHue(mobile.NotorietyFlag);
        }

        /// <summary>
        /// True when Mods → Visual Helpers friend/guild coloring is enabled and this mobile
        /// qualifies (Ally, party, guild track, or Razor friend list) — notoriety ignored.
        /// Local player never qualifies.
        /// </summary>
        public static bool TryGetFriendsGuildHighlightHue(World world, Mobile mobile, out ushort hue)
        {
            hue = 0;

            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || profile.HighlighFriendsGuildType == 0)
            {
                return false;
            }

            if (mobile == null || world?.Player == null || mobile == world.Player || mobile.Serial == world.Player.Serial)
            {
                return false;
            }

            if (!IsFriendOrGuild(world, mobile))
            {
                return false;
            }

            hue = profile.HighlighFriendsGuildType switch
            {
                1 => BrightWhiteColor,
                2 => BrightPinkColor,
                3 => BrightIceColor,
                4 => BrightFireColor,
                5 => profile.HighlighFriendsGuildTypeHue,
                _ => (ushort)0
            };

            return profile.HighlighFriendsGuildType is >= 1 and <= 5;
        }

        public static bool IsFriendOrGuild(World world, Mobile mobile)
        {
            if (mobile == null || world == null)
            {
                return false;
            }

            // Ally notoriety (innocent friends/guildies). Murderers keep Murderer flag even when friended/guilded.
            if (mobile.NotorietyFlag == NotorietyFlag.Ally)
            {
                return true;
            }

            if (world.Party.Contains(mobile.Serial))
            {
                return true;
            }

            WMapEntity wme = world.WMapManager.GetEntity(mobile.Serial);

            if (wme != null && wme.IsGuild)
            {
                return true;
            }

            // Razor Enhanced friend list — works for reds/murderers too.
            if (IsRazorFriend(mobile.Serial))
            {
                return true;
            }

            return false;
        }

        private static bool IsRazorFriend(uint serial)
        {
            uint now = Time.Ticks;

            if (_friendCache.TryGetValue(serial, out var cached) && now < cached.expire)
            {
                return cached.isFriend;
            }

            bool result = QueryRazorIsFriend(serial);
            _friendCache[serial] = (result, now + FriendCacheMs);

            if (_friendCache.Count > 256)
            {
                PruneFriendCache(now);
            }

            return result;
        }

        private static void PruneFriendCache(uint now)
        {
            List<uint> expired = null;

            foreach (var kv in _friendCache)
            {
                if (now >= kv.Value.expire)
                {
                    expired ??= new List<uint>();
                    expired.Add(kv.Key);
                }
            }

            if (expired == null)
            {
                return;
            }

            for (int i = 0; i < expired.Count; i++)
            {
                _friendCache.Remove(expired[i]);
            }
        }

        private static bool QueryRazorIsFriend(uint serial)
        {
            // Primary path: RazorEnhanced is loaded in ClassicUO.Bootstrap (net472 ClassicUO.exe),
            // not in NativeAOT cuo.dll — AppDomain reflection here never sees Friend.IsFriend.
            try
            {
                IPluginHost host = Client.Game?.PluginHost;

                if (host != null)
                {
                    return host.IsFriend((int)serial);
                }
            }
            catch
            {
                // Fall through to same-process reflection (managed plugin load without bootstrap).
            }

            if (_razorIsFriend == null)
            {
                uint now = Time.Ticks;

                if (_razorResolveAttempted && now < _nextRazorResolveTicks)
                {
                    return false;
                }

                _razorResolveAttempted = true;
                _nextRazorResolveTicks = now + 5000;
                TryResolveRazorIsFriend();

                if (_razorIsFriend == null)
                {
                    return false;
                }
            }

            try
            {
                return _razorIsFriend((int)serial);
            }
            catch
            {
                return false;
            }
        }

        private static void TryResolveRazorIsFriend()
        {
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = asm.GetType("RazorEnhanced.Friend");

                    if (type == null)
                    {
                        continue;
                    }

                    MethodInfo method = type.GetMethod(
                        "IsFriend",
                        BindingFlags.Public | BindingFlags.Static,
                        binder: null,
                        types: new[] { typeof(int) },
                        modifiers: null
                    );

                    if (method == null)
                    {
                        continue;
                    }

                    _razorIsFriend = (Func<int, bool>)Delegate.CreateDelegate(typeof(Func<int, bool>), method);

                    return;
                }
            }
            catch
            {
                _razorIsFriend = null;
            }
        }

        public static ushort WeaponsHue(ushort hue)
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null)
            {
                return hue;
            }

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

        /// <summary>
        /// True when any Mods → Visual Helpers last-target highlight mode is enabled
        /// (base last target and/or a status overlay).
        /// </summary>
        public static bool HasAnyLastTargetHighlight(Profile profile)
        {
            if (profile == null)
            {
                return false;
            }

            return profile.HighlightLastTargetType != 0
                   || profile.HighlightLastTargetTypePoison != 0
                   || profile.HighlightLastTargetTypePara != 0
                   || profile.HighlightLastTargetTypeStunned != 0
                   || profile.HighlightLastTargetTypeMortalled != 0;
        }

        /// <summary>
        /// Frozen mobiles cannot be told apart as paralyze vs wrestling-stun on other players
        /// (both use Flags.Frozen). Both overlays key off <see cref="Mobile.IsParalyzed"/>.
        /// </summary>
        public static bool IsFrozenCombatStatus(Mobile mobile)
        {
            return mobile != null
                   && mobile.IsParalyzed
                   && mobile.NotorietyFlag != NotorietyFlag.Invulnerable;
        }

        /// <summary>
        /// Last-target body tint (Mods → Visual Helpers).
        /// Precedence when multiple statuses apply (later wins):
        /// base last-target → poison → paralyze → stun → mortal.
        /// Effective overlap order: Mortal &gt; Stun &gt; Paralyze &gt; Poison &gt; base LT.
        /// Base LT is applied only when HighlightLastTargetType != 0 (does not wipe prior
        /// friend/global-status hues when base LT is Off). Status overlays apply whenever
        /// their own mode != 0, even if base LT is Off.
        /// </summary>
        public static ushort LastTargetHue(Mobile mobile, ushort hue)
        {
            if (mobile == null)
            {
                return hue;
            }

            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null)
            {
                return hue;
            }

            if (profile.HighlightLastTargetType != 0)
            {
                hue = ResolveHighlightMode(
                    profile.HighlightLastTargetType,
                    profile.HighlightLastTargetTypeHue,
                    hue
                );
            }

            if (mobile.IsPoisoned && profile.HighlightLastTargetTypePoison != 0)
            {
                hue = ResolveStateHighlightMode(
                    profile.HighlightLastTargetTypePoison,
                    profile.HighlightLastTargetTypePoisonHue,
                    BrightPoisonColor,
                    hue
                );
            }

            if (IsFrozenCombatStatus(mobile) && profile.HighlightLastTargetTypePara != 0)
            {
                hue = ResolveStateHighlightMode(
                    profile.HighlightLastTargetTypePara,
                    profile.HighlightLastTargetTypeParaHue,
                    BrightParalyzeColor,
                    hue
                );
            }

            // Stun shares Flags.Frozen with paralyze (Dust765 / shard limitation).
            // When both Para and Stun modes are enabled, Stun wins (applied after Para).
            if (IsFrozenCombatStatus(mobile) && profile.HighlightLastTargetTypeStunned != 0)
            {
                hue = ResolveStateHighlightMode(
                    profile.HighlightLastTargetTypeStunned,
                    profile.HighlightLastTargetTypeStunnedHue,
                    BrightParalyzeColor,
                    hue
                );
            }

            if (mobile.IsYellowHits && profile.HighlightLastTargetTypeMortalled != 0)
            {
                hue = ResolveStateHighlightMode(
                    profile.HighlightLastTargetTypeMortalled,
                    profile.HighlightLastTargetTypeMortalledHue,
                    BrightMortalColor,
                    hue
                );
            }

            return hue;
        }

        /// <summary>
        /// Global poison / paral / mortal / mirror highlights (Options → General + Mods mirror).
        /// Applied after notoriety/friend so Mods colors win.
        /// Precedence when multiple apply (later wins): Poison → Paralyze → Mortal → Mirror.
        /// Effective: Mirror &gt; Mortal &gt; Paralyze &gt; Poison.
        /// </summary>
        public static ushort ApplyStatusHighlights(Mobile mobile, ushort hue)
        {
            if (mobile == null || mobile.IsDead)
            {
                return hue;
            }

            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null)
            {
                return hue;
            }

            if (profile.HighlightMobilesByPoisoned && mobile.IsPoisoned)
            {
                hue = profile.PoisonHue;
            }

            if (profile.HighlightMobilesByParalize && IsFrozenCombatStatus(mobile))
            {
                hue = profile.ParalyzedHue;
            }

            if (profile.HighlightMobilesByInvul && mobile.NotorietyFlag != NotorietyFlag.Invulnerable && mobile.IsYellowHits)
            {
                hue = profile.InvulnerableHue;
            }

            if (profile.HighlightMirrorImageClones && mobile.IsMirrorClone)
            {
                hue = profile.MirrorImageCloneHue;
            }

            return hue;
        }

        private static ushort ResolveHighlightMode(int mode, ushort customHue, ushort fallback)
        {
            return mode switch
            {
                1 => BrightWhiteColor,
                2 => BrightPinkColor,
                3 => BrightIceColor,
                4 => BrightFireColor,
                5 => customHue,
                _ => fallback
            };
        }

        private static ushort ResolveStateHighlightMode(int mode, ushort customHue, ushort specialHue, ushort fallback)
        {
            return mode switch
            {
                1 => BrightWhiteColor,
                2 => BrightPinkColor,
                3 => BrightIceColor,
                4 => BrightFireColor,
                5 => specialHue,
                6 => customHue,
                _ => fallback
            };
        }
    }
}
