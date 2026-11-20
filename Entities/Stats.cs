namespace DnD.Entities
{
    [System.Serializable]
    public class Stats
    {
        // 6 Ability Scores (D&D standard)
        public int Strength { get; set; }    // melee damage
        public int Dexterity { get; set; }   // AC, dodge, initiative
        public int Constitution { get; set; }// HP
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }

        // Combat stats
        public int Health { get; set; }
        public int Attack { get; set; }
        public int BaseHealth { get => Health; set => Health = value; }
        public int BaseAttack { get => Attack; set => Attack = value; }
        public int ArmorClass { get; set; }
        public int Mana { get; set; }

        // Per-level growth
        public int HealthPerLevel { get; set; }
        public int AttackPerLevel { get; set; }
        public int ManaPerLevel { get; set; }

        public Stats() : this(10, 10, 10, 10, 10, 10, 100, 10, 10, 20, 20, 5, 2) { }

        public Stats(int baseHealth, int baseAttack, int healthPerLevel, int attackPerLevel,
                     int baseMana = 0, int manaPerLevel = 0)
            : this(10, 10, 10, 10, 10, 10, baseHealth, baseAttack, 10, baseMana,
                   healthPerLevel, attackPerLevel, manaPerLevel) { }

        public Stats(
            int strength, int dexterity, int constitution,
            int intelligence, int wisdom, int charisma,
            int health, int attack, int armorClass, int mana,
            int healthPerLevel, int attackPerLevel, int manaPerLevel)
        {
            Strength = strength;
            Dexterity = dexterity;
            Constitution = constitution;
            Intelligence = intelligence;
            Wisdom = wisdom;
            Charisma = charisma;
            Health = health;
            Attack = attack;
            ArmorClass = armorClass;
            Mana = mana;
            HealthPerLevel = healthPerLevel;
            AttackPerLevel = attackPerLevel;
            ManaPerLevel = manaPerLevel;
        }

        public Stats Clone() =>
            new Stats(
                Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma,
                Health, Attack, ArmorClass, Mana,
                HealthPerLevel, AttackPerLevel, ManaPerLevel);
    }
}
