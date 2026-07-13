#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.

#endregion

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game
{
    /// <summary>
    /// Dust765-style invisible houses: hide static/multi/item tiles above the player
    /// (with configurable Z offset) that sit well above the land Z at that tile.
    /// Classic/pre-built houses often place walls at the same Z as the raised foundation;
    /// those are handled via visual height inside known house bounds.
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

            world.Map.GetMapZ(obj.X, obj.Y, out sbyte landZ, out _);

            if ((obj.Z - landZ) > profile.DontRemoveHouseBelowZ)
            {
                return true;
            }

            // Custom houses work with the land-Z check above. Pre-built houses (L-shape,
            // castle, tower, etc.) raise terrain to foundation level, so walls share that Z
            // while still drawing tall graphics — hide by visual height inside house bounds.
            if (!TryGetTileData(obj, out StaticTiles itemData) || !IsInsideAnyHouse(obj, world))
            {
                return false;
            }

            if (IsHouseFloorTile(obj, ref itemData))
            {
                return false;
            }

            int effectiveHeight = GetEffectiveHeight(ref itemData);

            if (effectiveHeight <= 0)
            {
                return false;
            }

            return obj.Z + effectiveHeight - player.Z > profile.InvisibleHousesZ;
        }

        private static bool TryGetTileData(GameObject obj, out StaticTiles itemData)
        {
            switch (obj)
            {
                case Static s:
                    itemData = s.ItemData;

                    return true;

                case Multi m:
                    itemData = m.ItemData;

                    return true;

                case Item i when !i.IsCorpse:
                    itemData = i.IsMulti
                        ? TileDataLoader.Instance.StaticData[i.MultiGraphic]
                        : i.ItemData;

                    return true;

                default:
                    itemData = default;

                    return false;
            }
        }

        private static bool IsInsideAnyHouse(GameObject obj, World world)
        {
            foreach (House house in world.HouseManager.Houses)
            {
                if (world.HouseManager.EntityIntoHouse(house.Serial, obj))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsHouseFloorTile(GameObject obj, ref StaticTiles itemData)
        {
            if (obj is Multi multi)
            {
                if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0)
                {
                    return true;
                }

                // Classic house foundation/floor multis from multi.mul.
                if (
                    (multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE) != 0
                    && itemData.IsSurface
                )
                {
                    return true;
                }
            }

            return itemData.IsSurface && itemData.Height <= 0;
        }

        private static int GetEffectiveHeight(ref StaticTiles itemData)
        {
            byte height = itemData.Height;

            if (height == 0)
            {
                if (itemData.IsBackground || itemData.IsSurface)
                {
                    return 0;
                }

                height = 10;
            }

            if ((itemData.Flags & TileFlag.Bridge) != 0)
            {
                height = (byte)(height / 2);
            }

            return height;
        }
    }
}
