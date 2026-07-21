// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal static class SwingTimerService
    {
        private static bool _wasSwingReady;
        private static uint _microFreezeUntil;

        public static bool IsSwingReady { get; private set; }

        public static bool JustBecameReady => IsSwingReady && !_wasSwingReady;

        public static bool IsMovingShotActive(PlayerMobile player)
        {
            if (player == null)
            {
                return false;
            }

            for (int i = 0; i < 2; i++)
            {
                byte raw = (byte)player.Abilities[i];

                if ((raw & 0x7F) == (byte)Ability.MovingShot && (raw & 0x80) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsMicroFreezeActive()
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || !profile.SwingReadyMicroFreezeEnabled || Time.Ticks >= _microFreezeUntil)
            {
                return false;
            }

            if (IsMovingShotActive(Client.Game?.UO?.World?.Player))
            {
                return false;
            }

            return true;
        }

        public static void ArmMicroFreeze()
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || !profile.SwingReadyMicroFreezeEnabled)
            {
                return;
            }

            if (IsMovingShotActive(Client.Game?.UO?.World?.Player))
            {
                return;
            }

            int ms = Math.Clamp(profile.SwingReadyMicroFreezeDurationMs, 200, 800);
            _microFreezeUntil = Time.Ticks + (uint)ms;
        }

        internal static void SetSwingReady(bool ready)
        {
            _wasSwingReady = IsSwingReady;
            IsSwingReady = ready;
        }
    }
}
