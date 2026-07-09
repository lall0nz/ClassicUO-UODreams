#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    internal sealed class DurabilityManager : IDisposable
    {
        private readonly World _world;
        private readonly ConcurrentDictionary<uint, DurabiltyProp> _itemLayerSlots = new ConcurrentDictionary<uint, DurabiltyProp>();

        private static readonly Layer[] _equipLayers =
        {
            Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Arms, Layer.Torso, Layer.Tunic,
            Layer.Ring, Layer.Bracelet, Layer.Gloves, Layer.Skirt, Layer.Robe, Layer.Waist, Layer.Necklace,
            Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded, Layer.Talisman
        };

        public DurabilityManager(World world)
        {
            _world = world;
            _world.OPL.EntryUpdated += OnOPLUpdated;
        }

        public IReadOnlyList<DurabiltyProp> Durabilities => _itemLayerSlots.Values.ToList();

        public bool TryGetDurability(uint serial, out DurabiltyProp durability)
        {
            return _itemLayerSlots.TryGetValue(serial, out durability);
        }

        private void OnOPLUpdated(uint serial, string data)
        {
            if (!SerialHelper.IsValid(serial) || !SerialHelper.IsItem(serial))
            {
                return;
            }

            if (!_world.Items.TryGetValue(serial, out Item item) || item.IsDestroyed)
            {
                _itemLayerSlots.TryRemove(serial, out _);
                return;
            }

            if (item.Container == _world.Player?.Serial && _equipLayers.Contains(item.Layer))
            {
                DurabiltyProp durability = ParseDurability((int)serial, data);
                _itemLayerSlots.AddOrUpdate(serial, durability, (_, _) => durability);
            }
            else
            {
                _itemLayerSlots.TryRemove(serial, out _);
            }

            UIManager.GetGump<DurabilitysGump>()?.RequestUpdateContents();
        }

        private static DurabiltyProp ParseDurability(int serial, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new DurabiltyProp();
            }

            MatchCollection matches = Regex.Matches(data, @"(?<=Durability )(\d*) / (\d*)");

            if (matches.Count == 0)
            {
                return new DurabiltyProp();
            }

            string[] parts = data.Substring(matches[0].Index, matches[0].Length).Split('/');

            return int.TryParse(parts[0].Trim(), out int min) && int.TryParse(parts[1].Trim(), out int max)
                ? new DurabiltyProp(serial, min, max)
                : new DurabiltyProp();
        }

        public void Dispose()
        {
            _world.OPL.EntryUpdated -= OnOPLUpdated;
            _itemLayerSlots.Clear();
        }
    }

    internal sealed class DurabiltyProp
    {
        public int Serial { get; set; }
        public int Durabilty { get; set; }
        public int MaxDurabilty { get; set; }

        public float Percentage => MaxDurabilty > 0 ? (float)Durabilty / MaxDurabilty : 0;

        public DurabiltyProp(int serial, int current, int max)
        {
            Serial = serial;
            Durabilty = current;
            MaxDurabilty = max;
        }

        public DurabiltyProp() : this(0, 0, 0)
        {
        }
    }
}
