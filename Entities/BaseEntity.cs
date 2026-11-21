using DnD.Core;
using DnD.Systems.Inventory;
using DnD.Systems.Dice;

namespace DnD.Entities
{
    [System.Serializable]
    public abstract class BaseEntity : ICombatant, ILevelable, IRandomizedCombatant
    {
        private string _name;
        private Race _race;
        private Stats _baseStats;
        private int _level;
        private int _experience;
        private int _experienceToNextLevel;
        private int _totalDamageTaken;
        private bool _isAlive;
        private bool _isDefending;
        private WeaponData _equippedWeapon;
        private ArmorData _equippedArmor;

        public string Name => _name;
        public Race Race => _race;
        public bool IsAlive => _isAlive;

        public int MaxHealth
        {
            get
            {
                int armorBonus = _equippedArmor?.BonusHealth ?? 0;
                return System.Math.Max(1, _baseStats.BaseHealth + (_level * _baseStats.HealthPerLevel) + ((_baseStats.Constitution - 10) / 2 * _level) + armorBonus);
            }
        }

        public int CurrentHealth => System.Math.Max(0, MaxHealth - _totalDamageTaken);

        protected virtual int AbilityAttackBonus => (_baseStats.Strength - 10) / 2;
        protected virtual int AbilityDefenseBonus => (_baseStats.Dexterity - 10) / 2;

        public int AttackPower
        {
            get
            {
                int weaponBonus = _equippedWeapon?.AttackValue ?? 0;
                return System.Math.Max(1, _baseStats.BaseAttack + (_level * _baseStats.AttackPerLevel) + weaponBonus + AbilityAttackBonus);
            }
        }

        public int DefensePower => System.Math.Max(0, (_equippedArmor?.DefenseValue ?? 0) + AbilityDefenseBonus + (_isDefending ? 10 : 0));
        public Stats BaseStats => _baseStats;

        public int Level => _level;
        public int Experience => _experience;
        public int ExperienceToNextLevel => _experienceToNextLevel;

        public WeaponData EquippedWeapon => _equippedWeapon;
        public ArmorData EquippedArmor => _equippedArmor;

        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public void SetPosition(int x, int y)
        {
            PositionX = x;
            PositionY = y;
        }

        protected BaseEntity(string name, Race race, Stats baseStats)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new System.ArgumentException("Name is required.", nameof(name));
            if (baseStats == null) throw new System.ArgumentNullException(nameof(baseStats));
            _name = name.Trim();
            _race = race;
            _baseStats = baseStats.Clone();
            switch (race)
            {
                case Race.Human:
                    _baseStats.Strength++; _baseStats.Dexterity++; _baseStats.Constitution++;
                    _baseStats.Intelligence++; _baseStats.Wisdom++; _baseStats.Charisma++;
                    break;
                case Race.Elf:
                case Race.Halfling:
                    _baseStats.Dexterity += 2;
                    break;
                case Race.Dwarf:
                    _baseStats.Constitution += 2;
                    break;
                case Race.SemiDragon:
                case Race.Orc:
                    _baseStats.Strength += 2;
                    break;
                case Race.Goblin:
                    _baseStats.Dexterity += 1;
                    break;
            }
            _level = 1;
            _experience = 0;
            _experienceToNextLevel = 50;
            _totalDamageTaken = 0;
            _isAlive = true;
        }

        public void EquipWeapon(WeaponData weapon) => _equippedWeapon = weapon;
        public void EquipArmor(ArmorData armor) => _equippedArmor = armor;

        public void Defend() => _isDefending = true;

        public void TakeDamage(int rawDamage)
        {
            if (!_isAlive || rawDamage <= 0) return;
            int mitigated = System.Math.Max(0, rawDamage - DefensePower);
            _isDefending = false;
            _totalDamageTaken += mitigated;

            GameEvents.RaiseDamageTaken(this, mitigated, CurrentHealth);

            if (CurrentHealth <= 0)
            {
                _isAlive = false;
                _totalDamageTaken = MaxHealth;
                GameEvents.RaiseEntityDefeated(this);
            }
        }

        public void GainExperience(int amount)
        {
            if (!_isAlive || amount <= 0) return;
            _experience += amount;
            GameEvents.RaiseExperienceGained(this, amount);

            while (_experience >= _experienceToNextLevel)
                LevelUp();
        }

        private void LevelUp()
        {
            _level++;
            _experience -= _experienceToNextLevel;
            _experienceToNextLevel += 50;
            _totalDamageTaken = 0;
            GameEvents.RaiseLevelUp(this, _level);
        }

        public int Heal(int amount)
        {
            if (amount <= 0 || !_isAlive) return 0;
            int before = CurrentHealth;
            _totalDamageTaken = System.Math.Max(0, _totalDamageTaken - amount);
            return CurrentHealth - before;
        }

        public void FullHeal() => _totalDamageTaken = 0;

        public void RestoreProgress(int level, int experience, int currentHealth)
        {
            if (level < 1 || level > 100 || experience < 0 || experience >= level * 50 || currentHealth < 0)
                throw new System.ArgumentOutOfRangeException(nameof(level));
            _level = level;
            _experienceToNextLevel = 50 + (level - 1) * 50;
            _experience = experience;
            _totalDamageTaken = System.Math.Clamp(MaxHealth - currentHealth, 0, MaxHealth);
            _isAlive = currentHealth > 0;
        }

        public abstract void PerformAttack(ICombatant target);

        public abstract void PerformAttack(ICombatant target, IRandomSource randomSource);
    }
}
