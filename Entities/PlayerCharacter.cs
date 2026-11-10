using DnD.Core;
using DnD.Systems.Dice;

namespace DnD.Entities
{
    [System.Serializable]
    public class PlayerCharacter : BaseEntity
    {
        private CharacterClass _characterClass;
        private string _combatStyle;
        private int _currentMana;

        public CharacterClass CharacterClass => _characterClass;
        public string CombatStyle => _combatStyle;
        public int CurrentMana { get => _currentMana; private set => _currentMana = value; }
        public int MaxMana => Level * (_characterClass == CharacterClass.Wizard ? 10 : 2);

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
