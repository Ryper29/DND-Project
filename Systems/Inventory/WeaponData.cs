using DnD.Entities;

namespace DnD.Systems.Inventory
{
    [System.Serializable]
    public class WeaponData
    {
        public string Name { get; set; }
        public int AttackValue { get; set; }
        public DamageType DamageType { get; set; }

        public WeaponData(string name, int attackValue, DamageType damageType = DamageType.Physical)
        {
            Name = name;
            AttackValue = attackValue;
            DamageType = damageType;
        }

        public override string ToString() => $"{Name} (ATK: {AttackValue}, {DamageType})";
    }
}
