// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using System;

namespace ClassicUO.Game.Managers
{
    // UODreams housing areas are flooded with stone-roof items (graphic 0x0577) whose
    // tooltip (OPL) requests/replies swamp the client. When enabled, never request
    // tooltips for them and stub the OPL locally instead.
    internal static class UoDreamsNetworkOptimizer
    {
        public const ushort StoneRoofGraphic = 0x0577;

        internal static bool IsEnabled =>
            ProfileManager.CurrentProfile?.EnableUoDreamsNetworkOptimizer == true;

        internal static bool IsShard(World world)
        {
            if (world == null)
            {
                return false;
            }

            if (world.ServerName != null && world.ServerName.IndexOf("uodreams", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string ip = Settings.GlobalSettings.IP;

            return !string.IsNullOrEmpty(ip)
                && ip.IndexOf("uodreams", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsStoneRoofProblemItem(World world, Item item)
        {
            return IsEnabled && item != null && IsShard(world) && item.Graphic == StoneRoofGraphic;
        }

        internal static void EnsureStubOpl(World world, uint serial, Item item, uint revision = 1)
        {
            if (world.OPL.TryGetRevision(serial, out _))
            {
                return;
            }

            string name = item?.Name;

            if (string.IsNullOrEmpty(name) && item != null)
            {
                name = item.ItemData.Name;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "stone roof";
            }

            world.OPL.Add(serial, revision, name, string.Empty, 0);
        }
    }
}
