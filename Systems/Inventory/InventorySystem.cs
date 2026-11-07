using System.Collections.Generic;

namespace DnD.Systems.Inventory
{
    [System.Serializable]
    public class InventorySystem
    {
        private readonly List<WeaponData> _weapons = new List<WeaponData>();
        private readonly List<ArmorData> _armors = new List<ArmorData>();

        public IReadOnlyList<WeaponData> Weapons => _weapons;
        public IReadOnlyList<ArmorData> Armors => _armors;

        public void AddWeapon(WeaponData weapon) => _weapons.Add(weapon);
        public void AddArmor(ArmorData armor) => _armors.Add(armor);
        public bool RemoveWeapon(WeaponData weapon) => _weapons.Remove(weapon);
        public bool RemoveArmor(ArmorData armor) => _armors.Remove(armor);
    }
}
