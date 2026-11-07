using DnD.Core;
using DnD.Systems.Dice;

namespace DnD.Entities
{
    [System.Serializable]
    public class Enemy : BaseEntity
    {
        private string _combatStyle;
        private int _experienceReward;

        public string CombatStyle => _combatStyle;
        public int ExperienceReward => _experienceReward;

        public Enemy(string name, Race race, Stats baseStats,
                     string combatStyle, int experienceReward = 50)
            : base(name, race, baseStats)
        {
            _combatStyle = combatStyle;
            _experienceReward = experienceReward;
        }

        public override void PerformAttack(ICombatant target)
        {
            if (!IsAlive) return;

            GameEvents.RaiseTurnChanged(this);

            var roll = DiceRoller.Roll(DiceType.D20);
            GameEvents.RaiseDiceRolled(Name, roll);

            if (roll.Value == 1)
            {
                GameEvents.RaiseCombatLog($"CRITICAL FAIL! {Name}'s attack misses completely!");
                return;
            }

            int totalDamage = AttackPower + (roll.Value * 2);
            string weaponName = EquippedWeapon?.Name ?? "claws";
            GameEvents.RaiseCombatLog($"{Name} strikes {target.Name} with {weaponName}!");
            GameEvents.RaiseAttackPerformed(this, target, totalDamage);
            target.TakeDamage(totalDamage);
        }
    }
}
