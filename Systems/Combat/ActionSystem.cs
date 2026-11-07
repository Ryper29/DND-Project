using DnD.Core;
using DnD.Entities;
using DnD.Systems.Dice;

namespace DnD.Systems.Combat
{
    /// <summary>
    /// Standalone action helpers for manual combat control (as opposed to CombatResolver auto-combat).
    /// </summary>
    public static class ActionSystem
    {
        public static void PerformMeleeAttack(ICombatant attacker, ICombatant target)
        {
            var roll = DiceRoller.Roll(DiceType.D20);
            GameEvents.RaiseDiceRolled(attacker.Name, roll);

            if (roll.IsCriticalFail)
            {
                GameEvents.RaiseCombatLog($"{attacker.Name}'s attack misses!");
                return;
            }

            int damage = attacker.AttackPower + roll.Value;
            if (roll.IsCriticalHit)
            {
                damage *= 2;
                GameEvents.RaiseCombatLog($"CRITICAL HIT! {attacker.Name} devastates {target.Name}!");
            }

            GameEvents.RaiseAttackPerformed(attacker, target, damage);
            target.TakeDamage(damage);
        }

        public static void PerformRangedAttack(ICombatant attacker, ICombatant target, int rangeModifier = 0)
        {
            var roll = DiceRoller.Roll(DiceType.D20);
            GameEvents.RaiseDiceRolled(attacker.Name, roll);

            if (roll.Value <= 3)
            {
                GameEvents.RaiseCombatLog($"{attacker.Name}'s ranged attack misses!");
                return;
            }

            int damage = System.Math.Max(1, attacker.AttackPower + roll.Value - rangeModifier);
            GameEvents.RaiseAttackPerformed(attacker, target, damage);
            target.TakeDamage(damage);
        }
    }
}
