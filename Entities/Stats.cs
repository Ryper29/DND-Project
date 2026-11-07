namespace DnD.Entities
{
    [System.Serializable]
    public class Stats
    {
        public int BaseHealth { get; set; }
        public int BaseAttack { get; set; }
        public int HealthPerLevel { get; set; }
        public int AttackPerLevel { get; set; }
        public int BaseMana { get; set; }
        public int ManaPerLevel { get; set; }

        public Stats(int baseHealth, int baseAttack, int healthPerLevel, int attackPerLevel,
                     int baseMana = 0, int manaPerLevel = 0)
        {
            BaseHealth = baseHealth;
            BaseAttack = baseAttack;
            HealthPerLevel = healthPerLevel;
            AttackPerLevel = attackPerLevel;
            BaseMana = baseMana;
            ManaPerLevel = manaPerLevel;
        }

        public Stats Clone() =>
            new Stats(BaseHealth, BaseAttack, HealthPerLevel, AttackPerLevel, BaseMana, ManaPerLevel);
    }
}
