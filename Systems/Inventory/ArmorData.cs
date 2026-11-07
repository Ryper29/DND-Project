namespace DnD.Systems.Inventory
{
    [System.Serializable]
    public class ArmorData
    {
        public string Name { get; set; }
        public int DefenseValue { get; set; }
        public int BonusHealth { get; set; }

        public ArmorData(string name, int defenseValue, int bonusHealth = 0)
        {
            Name = name;
            DefenseValue = defenseValue;
            BonusHealth = bonusHealth;
        }

        public override string ToString() => $"{Name} (DEF: {DefenseValue}, +{BonusHealth} HP)";
    }
}
