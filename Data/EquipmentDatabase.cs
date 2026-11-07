using System.Collections.Generic;
using DnD.Entities;
using DnD.Systems.Inventory;

namespace DnD.Data
{
    /// <summary>
    /// Static equipment catalog. Future: load from JSON / ScriptableObject asset bundles.
    /// </summary>
    public static class EquipmentDatabase
    {
        public static readonly List<WeaponData> Weapons = new List<WeaponData>
        {
            new WeaponData("Dragon Claw", 15, DamageType.Physical),
            new WeaponData("Iron Sword", 20, DamageType.Physical),
            new WeaponData("Wooden Staff", 10, DamageType.Magical),
            new WeaponData("Giant Spiked Club", 25, DamageType.Physical),
            new WeaponData("Enchanted Dagger", 12, DamageType.Magical),
        };

        public static readonly List<ArmorData> Armors = new List<ArmorData>
        {
            new ArmorData("Dragon Scale", 20, 150),
            new ArmorData("Light Armor", 30, 230),
            new ArmorData("Cloth Robe", 10, 80),
            new ArmorData("Hardened Skin", 40, 400),
            new ArmorData("Leather Vest", 15, 100),
        };

        public static WeaponData GetWeapon(string name) => Weapons.Find(w => w.Name == name);
        public static ArmorData GetArmor(string name) => Armors.Find(a => a.Name == name);
    }
}
