using InventorySystem;
using System.Collections.Generic;
using System.IO;
using Utf8Json;

namespace GunGame.Game
{
    public static class WeaponManager
    {
        public static List<Gatito> AllWeapons => _allWeapons ?? LoadWeapons();
        private static List<Gatito> _allWeapons { get; set; }

        public static List<ItemType> AllAmmo => ammoCache ?? loadAmmoCache();
        private static List<ItemType> ammoCache;
        private static List<ItemType> loadAmmoCache()
        {
            try
            {
                ammoCache = new List<ItemType>();

                foreach (var item in InventoryItemLoader.AvailableItems)
                    if (item.Value.Category == ItemCategory.Ammo)
                        ammoCache.Add(item.Key);

                return ammoCache;
            }
            catch
            {
                return new List<ItemType> { };
            }

        }
        public static string FileName => "GunGameWeapons.json";
        public static string FilePath => Plugin.ConfigFolderPath + FileName;
        public static string ReadWeaponsFile()
        {
            if (File.Exists(FilePath))
                return File.ReadAllText(FilePath);
            else return null;
        }
        public static void SaveWeaponsToFile()
        {
            File.WriteAllText(FilePath, JsonSerializer.ToJsonString(AllWeapons, Utf8Json.Resolvers.StandardResolver.ExcludeNull));
        }
        public static List<Gatito> LoadWeapons()
        {
            string WeaponsJSON = ReadWeaponsFile();
            if (WeaponsJSON != null)
                _allWeapons = JsonSerializer.Deserialize<List<Gatito>>(WeaponsJSON);
            else
                WriteDefaultsToFile();
            return _allWeapons;
        }

        public static void WriteDefaultsToFile()
        {
            _allWeapons = new List<Gatito>() {
                new Gatito(ItemType.Jailbird, 2),
            };
            foreach (var x in InventoryItemLoader.AvailableItems)
                if (x.Value.Category == ItemCategory.Firearm)
                    AllWeapons.Add(new Gatito(x.Key));

            SaveWeaponsToFile();
        }

        public static void AddWeapon(Gatito weapon)
        {
            _allWeapons.Add(weapon);
            SaveWeaponsToFile();
        }

        public static string ListWeapons()
        {
            string ret = "";
            for (int i = 0; i < AllWeapons.Count; i++)
                ret += $"{i}: {AllWeapons[i].ItemType} {(AllWeapons[i].Mod != null ? $"\t- {AllWeapons[i].Mod}" : "")}\n";

            return ret;
        }

        public static void RemoveWeapon(int index)
        {
            _allWeapons.RemoveAt(index);
        }
    }

    public struct Gatito // Like Gat but smaller :3
    {
        public ItemType ItemType;
        public uint? Mod;
        public Gatito(ItemType item, uint mod)
        {
            ItemType = item;
            Mod = mod;
        }
        public Gatito(ItemType item)
        {
            ItemType = item;
            Mod = null;
        }
    }
}
