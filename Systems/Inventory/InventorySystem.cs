using System.Collections.Generic;

namespace DnD.Systems.Inventory
{
    [System.Serializable]
    public class InventorySystem
    {
        private readonly List<WeaponData> _weapons = new List<WeaponData>();
        private readonly List<ArmorData> _armors = new List<ArmorData>();
        private readonly List<ConsumableData> _consumables = new List<ConsumableData>();

        public IReadOnlyList<WeaponData> Weapons => _weapons;
        public IReadOnlyList<ArmorData> Armors => _armors;
        public IReadOnlyList<ConsumableData> Consumables => _consumables;

        public void AddWeapon(WeaponData weapon)
        {
            if (weapon == null) throw new System.ArgumentNullException(nameof(weapon));
            _weapons.Add(weapon);
        }

        public void AddArmor(ArmorData armor)
        {
            if (armor == null) throw new System.ArgumentNullException(nameof(armor));
            _armors.Add(armor);
        }

        public void AddConsumable(ConsumableData item)
        {
            if (item == null) throw new System.ArgumentNullException(nameof(item));
            _consumables.Add(item);
        }

        public bool RemoveWeapon(WeaponData weapon) => _weapons.Remove(weapon);
        public bool RemoveArmor(ArmorData armor) => _armors.Remove(armor);
        public bool RemoveConsumable(ConsumableData item) => _consumables.Remove(item);
    }
}
