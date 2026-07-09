#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game
{
    /// <summary>
    /// Dust765-style invisible houses: hide static/multi/item tiles above the player
    /// (with configurable Z offset) that sit well above the land Z at that tile.
    /// </summary>
    internal static class InvisibleHousesHelper
    {
        public static bool ShouldHide(GameObject obj, World world)
        {
            Profile profile = ProfileManager.CurrentProfile;
            Mobile player = world?.Player;

            if (profile == null || !profile.InvisibleHousesEnabled || player == null)
            {
                return false;
            }

            if (obj is Mobile or Land)
            {
                return false;
            }

            if ((obj.Z - player.Z) <= profile.InvisibleHousesZ)
            {
                return false;
            }

            sbyte groundZ = world.Map.GetTileZ(obj.X, obj.Y);

            return (obj.Z - groundZ) > profile.DontRemoveHouseBelowZ;
        }
    }
}
