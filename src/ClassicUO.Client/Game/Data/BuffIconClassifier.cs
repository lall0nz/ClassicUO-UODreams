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

using System.Collections.Generic;

namespace ClassicUO.Game.Data
{
    /// <summary>
    /// Classifies buff icons as beneficial vs harmful using the server
    /// <see cref="BuffIconType"/> identity. Pixel sampling of gump art is
    /// unreliable (mixed colors / empty pixel buffers), so classification is
    /// driven by an explicit harmful-type set plus *Debuff name matches.
    /// Blue state icons (meditation, stealth, etc.) stay on the buff bar.
    /// </summary>
    internal static class BuffIconClassifier
    {
        // Harmful / red-bar types. Caster-side and green-buff counterparts
        // (BloodOathCaster, EnemyOfOne, Feint, RageFocusingBuff, etc.) are omitted.
        private static readonly HashSet<BuffIconType> _harmfulTypes = new HashSet<BuffIconType>
        {
            BuffIconType.DismountPrevention,
            BuffIconType.NoRearm,
            BuffIconType.DeathStrike,
            BuffIconType.EvilOmen,
            BuffIconType.HonoredDebuff,
            BuffIconType.AchievePerfection,
            BuffIconType.BloodOathCurse,
            BuffIconType.CorpseSkin,
            BuffIconType.Mindrot,
            BuffIconType.PainSpike,
            BuffIconType.Strangle,
            BuffIconType.Thunderstorm,
            BuffIconType.EssenceOfWind,
            BuffIconType.MortalStrike,
            BuffIconType.Paralyze,
            BuffIconType.Poison,
            BuffIconType.Bleed,
            BuffIconType.Clumsy,
            BuffIconType.FeebleMind,
            BuffIconType.Weaken,
            BuffIconType.Curse,
            BuffIconType.MassCurse,
            BuffIconType.Sleep,
            BuffIconType.SpellPlague,
            BuffIconType.MassSleep,
            BuffIconType.TribulationTarget,
            BuffIconType.DespairTarget,
            BuffIconType.HitLowerAttack,
            BuffIconType.HitLowerDefense,
            BuffIconType.SpellFocusingDebuff,
            BuffIconType.RageFocusingDebuff,
            BuffIconType.ForceArrow,
            BuffIconType.Disarm,
            BuffIconType.TalonStrike,
            BuffIconType.PsychicAttack,
            BuffIconType.EnemyOfOneDebuff,
            BuffIconType.FanDancerFanFire,
            BuffIconType.Rage,
            BuffIconType.Webbing,
            BuffIconType.MedusaStone,
            BuffIconType.TrueFear,
            BuffIconType.AuraOfNausea,
            BuffIconType.HowlOfCacophony,
            BuffIconType.GazeDespair,
            BuffIconType.RuneBeetleCorruption,
            BuffIconType.BloodwormAnemia,
            BuffIconType.RotwormBloodDisease,
            BuffIconType.SkillUseDelay,
            BuffIconType.FactionStatLoss,
            BuffIconType.HeatOfBattleStatus,
            BuffIconType.CriminalStatus,
            BuffIconType.ArmorPierce,
            BuffIconType.SplinteringEffect,
            BuffIconType.SwingSpeedDebuff,
            BuffIconType.HumilityDebuff,
            BuffIconType.Stagger,
            BuffIconType.Pierce,
            BuffIconType.Onslaught,
            BuffIconType.ElementalFuryDebuff,
            BuffIconType.DeathRayDebuff,
            BuffIconType.InjectedStrikeDebuff,
            BuffIconType.PlayingTheOddsDebuff,
            BuffIconType.DragonTurtleDebuff,
            BuffIconType.ThrustDebuff,
            BuffIconType.Sparks,
            BuffIconType.Swarm,
            BuffIconType.BoneBreaker,
            BuffIconType.UnknownRedDrop,
            BuffIconType.FeintDebuff,
            BuffIconType.UnknownDebuff,
        };

        public static bool IsDebuff(BuffIcon icon)
        {
            if (icon == null)
            {
                return false;
            }

            return IsDebuff(icon.Type, icon.Graphic);
        }

        public static bool IsDebuff(BuffIconType type, ushort graphic)
        {
            if (_harmfulTypes.Contains(type))
            {
                return true;
            }

            // Catch future / renamed *Debuff enum members without maintaining the set.
            string name = type.ToString();

            if (name.IndexOf("Debuff", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // Unused graphic argument kept for call-site compatibility.
            _ = graphic;

            return false;
        }
    }
}
