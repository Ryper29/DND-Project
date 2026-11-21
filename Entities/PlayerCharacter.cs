using DnD.Core;
using DnD.Systems.Dice;
using DnD.Systems.Inventory;

namespace DnD.Entities
{
    [System.Serializable]
    public class PlayerCharacter : BaseEntity
    {
        private CharacterClass _characterClass;
        private string _combatStyle;
        private int _currentMana;
        private readonly InventorySystem _inventory = new InventorySystem();

        public InventorySystem Inventory => _inventory;
        public CharacterClass CharacterClass => _characterClass;
        public string CombatStyle => _combatStyle;
        protected override int AbilityAttackBonus => (_characterClass switch
        {
            CharacterClass.Wizard => BaseStats.Intelligence,
            CharacterClass.Rogue => BaseStats.Dexterity,
            CharacterClass.Cleric => BaseStats.Wisdom,
            _ => BaseStats.Strength
        } - 10) / 2;
        public int CurrentMana { get => _currentMana; private set => _currentMana = value; }
        public bool SpendMana(int amount)
        {
            if (amount < 0 || _currentMana < amount) return false;
            _currentMana -= amount;
            return true;
        }
        public int MaxMana => Level * (_characterClass == CharacterClass.Wizard || _characterClass == CharacterClass.Cleric ? 10 : 2);

        public void RestoreMana(int amount)
        {
            if (amount > 0) _currentMana = System.Math.Min(MaxMana, _currentMana + amount);
        }

        public void SetCurrentMana(int amount)
        {
            _currentMana = System.Math.Clamp(amount, 0, MaxMana);
        }

        public bool UseConsumable(int index)
        {
            if (index < 0 || index >= _inventory.Consumables.Count || !IsAlive) return false;
            var item = _inventory.Consumables[index];
            if (item.Effect == ConsumableEffect.Heal)
            {
                if (CurrentHealth >= MaxHealth) return false;
                Heal(item.Amount);
            }
            else
            {
                if (CurrentMana >= MaxMana) return false;
                RestoreMana(item.Amount);
            }
            _inventory.RemoveConsumable(item);
            GameEvents.RaiseCombatLog($"{Name} uses {item.Name}.");
            return true;
        }

        public void UseClassAbility(ICombatant target, IRandomSource randomSource)
        {
            if (!IsAlive || target == null || !target.IsAlive) return;
            switch (_characterClass)
            {
                case CharacterClass.Wizard:
                    if (SpendMana(5)) DealAbilityDamage(target, AttackPower + 20, "casts Fireball");
                    else GameEvents.RaiseCombatLog($"{Name} is out of mana.");
                    break;
                case CharacterClass.Cleric:
                    if (SpendMana(4))
                    {
                        int healed = Heal(20 + Level * 5);
                        GameEvents.RaiseCombatLog($"{Name} uses Healing Light and restores {healed} HP.");
                    }
                    else GameEvents.RaiseCombatLog($"{Name} lacks mana for Healing Light.");
                    break;
                case CharacterClass.Rogue:
                    var roll = DiceRoller.Roll(DiceType.D20, randomSource);
                    GameEvents.RaiseDiceRolled(Name, roll);
                    if (roll.Value != 1) DealAbilityDamage(target, AttackPower + roll.Value * 2, "uses Sneak Attack on");
                    else GameEvents.RaiseCombatLog($"{Name}'s Sneak Attack misses.");
                    break;
                default:
                    var fighterRoll = DiceRoller.Roll(DiceType.D20, randomSource);
                    GameEvents.RaiseDiceRolled(Name, fighterRoll);
                    if (fighterRoll.Value != 1) DealAbilityDamage(target, AttackPower + fighterRoll.Value * 2, "uses Power Strike on");
                    else GameEvents.RaiseCombatLog($"{Name}'s Power Strike misses.");
                    break;
            }
        }

        private void DealAbilityDamage(ICombatant target, int damage, string action)
        {
            GameEvents.RaiseCombatLog($"{Name} {action} {target.Name}!");
            GameEvents.RaiseAttackPerformed(this, target, damage);
            target.TakeDamage(damage);
        }

        public PlayerCharacter(string name, Race race, CharacterClass characterClass,
                               Stats baseStats, string combatStyle = "")
            : base(name, race, baseStats)
        {
            _characterClass = characterClass;
            _combatStyle = combatStyle;
            _currentMana = MaxMana;
        }

        public override void PerformAttack(ICombatant target) => PerformAttack(target, DiceRoller.RandomSource);

        public override void PerformAttack(ICombatant target, IRandomSource randomSource)
        {
            if (!IsAlive) return;

            GameEvents.RaiseTurnChanged(this);

            var roll = DiceRoller.Roll(DiceType.D20, randomSource);
            GameEvents.RaiseDiceRolled(Name, roll);

            switch (_characterClass)
            {
                case CharacterClass.Fighter:
                    ExecuteFighterAttack(target, roll);
                    break;
                case CharacterClass.Wizard:
                    ExecuteWizardAttack(target, roll);
                    break;
                default:
                    ExecuteBasicAttack(target, roll);
                    break;
            }
        }

        private void ExecuteFighterAttack(ICombatant target, RollResult roll)
        {
            if (roll.Value == 1)
            {
                GameEvents.RaiseCombatLog($"CRITICAL FAIL! {Name}'s attack misses completely!");
                return;
            }

            int totalDamage = AttackPower + (roll.Value * 2);
            string weaponName = EquippedWeapon?.Name ?? "fists";
            GameEvents.RaiseCombatLog($"{Name} slashes {target.Name} with {weaponName}!");
            GameEvents.RaiseAttackPerformed(this, target, totalDamage);
            target.TakeDamage(totalDamage);
            GainExperience(30);
        }

        private void ExecuteWizardAttack(ICombatant target, RollResult roll)
        {
            int manaCost = 5;

            if (_currentMana >= manaCost && roll.Value > 5)
            {
                _currentMana -= manaCost;
                int spellDamage = AttackPower + 20;
                GameEvents.RaiseCombatLog($"{Name} casts 'Fireball' at {target.Name}!");
                GameEvents.RaiseAttackPerformed(this, target, spellDamage);
                target.TakeDamage(spellDamage);
                GainExperience(40);
            }
            else if (_currentMana < manaCost)
            {
                GameEvents.RaiseCombatLog($"{Name} is out of mana! Strikes with staff instead.");
                int staffDamage = System.Math.Max(2, AttackPower / 3);
                GameEvents.RaiseAttackPerformed(this, target, staffDamage);
                target.TakeDamage(staffDamage);
                GainExperience(10);
            }
            else
            {
                GameEvents.RaiseCombatLog($"Spell fizzles! Dice roll too low ({roll.Value}).");
                GainExperience(10);
            }
        }

        private void ExecuteBasicAttack(ICombatant target, RollResult roll)
        {
            if (roll.Value == 1)
            {
                GameEvents.RaiseCombatLog($"CRITICAL FAIL! {Name}'s attack misses!");
                return;
            }

            int totalDamage = AttackPower + roll.Value;
            GameEvents.RaiseCombatLog($"{Name} attacks {target.Name}!");
            GameEvents.RaiseAttackPerformed(this, target, totalDamage);
            target.TakeDamage(totalDamage);
            GainExperience(20);
        }
    }
}
