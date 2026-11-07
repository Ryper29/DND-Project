using DnD.Core;
using DnD.Systems.Inventory;

namespace DnD.Entities
{
    [System.Serializable]
    public abstract class BaseEntity : ICombatant, ILevelable
    {
        private string _name;
        private Race _race;
        private Stats _baseStats;
        private int _level;
        private int _experience;
        private int _experienceToNextLevel;
        private int _totalDamageTaken;
        private bool _isAlive;
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
                return _baseStats.BaseHealth + (_level * _baseStats.HealthPerLevel) + armorBonus;
            }
        }

        public int CurrentHealth => System.Math.Max(0, MaxHealth - _totalDamageTaken);

        public int AttackPower
        {
            get
            {
                int weaponBonus = _equippedWeapon?.AttackValue ?? 0;
                return _baseStats.BaseAttack + (_level * _baseStats.AttackPerLevel) + weaponBonus;
            }
        }

        public int DefensePower => _equippedArmor?.DefenseValue ?? 0;

        public int Level => _level;
        public int Experience => _experience;
        public int ExperienceToNextLevel => _experienceToNextLevel;

        public WeaponData EquippedWeapon => _equippedWeapon;
        public ArmorData EquippedArmor => _equippedArmor;

        protected BaseEntity(string name, Race race, Stats baseStats)
        {
            _name = name;
            _race = race;
            _baseStats = baseStats;
            _level = 1;
            _experience = 0;
            _experienceToNextLevel = 50;
            _totalDamageTaken = 0;
            _isAlive = true;
        }

        public void EquipWeapon(WeaponData weapon) => _equippedWeapon = weapon;
        public void EquipArmor(ArmorData armor) => _equippedArmor = armor;

        public void TakeDamage(int rawDamage)
        {
            int mitigated = System.Math.Max(0, rawDamage - DefensePower);
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
            if (!_isAlive) return;
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

        public void FullHeal() => _totalDamageTaken = 0;

        public abstract void PerformAttack(ICombatant target);
    }
}
