using System;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Dice;

namespace DnD.Systems.Combat
{
    // ponytail: basic action economy action map; upgrade to reaction trigger events when grid/spatial combat is wired.
    public static class ActionSystem
    {
        public static bool PerformMeleeAttack(ICombatant attacker, ICombatant target, TurnResources resources = null, IRandomSource random = null, BattleGrid grid = null)
        {
            if (attacker == null || target == null || !attacker.IsAlive || !target.IsAlive) return false;

            if (grid != null)
            {
                var dist = grid.GetDistance(attacker, target);
                if (!dist.HasValue || dist.Value > BattleGrid.FeetPerTile)
                {
                    GameEvents.RaiseCombatLog($"{target.Name} is out of melee range ({(dist.HasValue ? dist.Value.ToString() : "unknown")}ft > {BattleGrid.FeetPerTile}ft)!");
                    return false;
                }
            }

            if (StatusEffectSystem.HasCondition(attacker, ConditionType.Stunned))
            {
                GameEvents.RaiseCombatLog($"{attacker.Name} is stunned and cannot attack!");
                return false;
            }

            if (resources != null && !resources.UseAction())
            {
                GameEvents.RaiseCombatLog($"{attacker.Name} has no action remaining this turn.");
                return false;
            }

            var rand = random ?? DiceRoller.RandomSource;
            var roll = StatusEffectSystem.RollAttackD20(attacker, target, isRanged: false, rand);
            GameEvents.RaiseDiceRolled(attacker.Name, roll);

            int blessBonus = 0;
            if (StatusEffectSystem.HasCondition(attacker, ConditionType.Blessed))
            {
                var blessRoll = DiceRoller.Roll(DiceType.D4, rand);
                blessBonus = blessRoll.Value;
                GameEvents.RaiseCombatLog($"Bless bonus adds +{blessBonus} to {attacker.Name}'s attack roll.");
            }

            if (roll.IsCriticalFail)
            {
                GameEvents.RaiseCombatLog($"{attacker.Name}'s attack misses!");
                return true;
            }

            int damage = attacker.AttackPower + roll.Value + blessBonus;
            if (roll.IsCriticalHit)
            {
                damage *= 2;
                GameEvents.RaiseCombatLog($"CRITICAL HIT! {attacker.Name} devastates {target.Name}!");
            }

            GameEvents.RaiseAttackPerformed(attacker, target, damage);
            target.TakeDamage(damage);
            return true;
        }

        public static bool PerformRangedAttack(ICombatant attacker, ICombatant target, int rangeModifier = 0, TurnResources resources = null, IRandomSource random = null, BattleGrid grid = null)
        {
            if (attacker == null || target == null || !attacker.IsAlive || !target.IsAlive) return false;

            if (grid != null)
            {
                var dist = grid.GetDistance(attacker, target);
                if (!dist.HasValue || dist.Value > 60)
                {
                    GameEvents.RaiseCombatLog($"{target.Name} is out of range ({(dist.HasValue ? dist.Value.ToString() : "unknown")}ft > 60ft)!");
                    return false;
                }
            }

            if (StatusEffectSystem.HasCondition(attacker, ConditionType.Stunned))
            {
                GameEvents.RaiseCombatLog($"{attacker.Name} is stunned and cannot attack!");
                return false;
            }

            if (resources != null && !resources.UseAction())
            {
                GameEvents.RaiseCombatLog($"{attacker.Name} has no action remaining this turn.");
                return false;
            }

            var rand = random ?? DiceRoller.RandomSource;
            var roll = StatusEffectSystem.RollAttackD20(attacker, target, isRanged: true, rand);
            GameEvents.RaiseDiceRolled(attacker.Name, roll);

            int blessBonus = 0;
            if (StatusEffectSystem.HasCondition(attacker, ConditionType.Blessed))
            {
                var blessRoll = DiceRoller.Roll(DiceType.D4, rand);
                blessBonus = blessRoll.Value;
                GameEvents.RaiseCombatLog($"Bless bonus adds +{blessBonus} to {attacker.Name}'s attack roll.");
            }

            if (roll.IsCriticalFail || roll.Value <= 3)
            {
                GameEvents.RaiseCombatLog($"{attacker.Name}'s ranged attack misses!");
                return true;
            }

            int damage = Math.Max(1, attacker.AttackPower + roll.Value + blessBonus - rangeModifier);
            if (roll.IsCriticalHit)
            {
                damage *= 2;
                GameEvents.RaiseCombatLog($"CRITICAL HIT! {attacker.Name}'s shot pierces {target.Name}!");
            }

            GameEvents.RaiseAttackPerformed(attacker, target, damage);
            target.TakeDamage(damage);
            return true;
        }

        public static bool PerformDash(ICombatant combatant, TurnResources resources)
        {
            if (combatant == null || resources == null || !combatant.IsAlive) return false;
            if (StatusEffectSystem.HasCondition(combatant, ConditionType.Stunned)) return false;

            if (!resources.UseAction())
            {
                GameEvents.RaiseCombatLog($"{combatant.Name} has no action to Dash.");
                return false;
            }

            resources.MovementRemaining += resources.Speed;
            GameEvents.RaiseCombatLog($"{combatant.Name} uses Dash (+{resources.Speed}ft movement).");
            return true;
        }

        public static bool PerformDodge(ICombatant combatant, TurnResources resources)
        {
            if (combatant == null || resources == null || !combatant.IsAlive) return false;
            if (StatusEffectSystem.HasCondition(combatant, ConditionType.Stunned)) return false;

            if (!resources.UseAction())
            {
                GameEvents.RaiseCombatLog($"{combatant.Name} has no action to Dodge.");
                return false;
            }

            if (combatant is BaseEntity entity)
                entity.Defend();

            GameEvents.RaiseCombatLog($"{combatant.Name} takes the Dodge action.");
            return true;
        }

        public static bool PerformDisengage(ICombatant combatant, TurnResources resources)
        {
            if (combatant == null || resources == null || !combatant.IsAlive) return false;
            if (StatusEffectSystem.HasCondition(combatant, ConditionType.Stunned)) return false;

            if (!resources.UseAction())
            {
                GameEvents.RaiseCombatLog($"{combatant.Name} has no action to Disengage.");
                return false;
            }

            resources.IsDisengaged = true;
            GameEvents.RaiseCombatLog($"{combatant.Name} disengages; movement will not provoke opportunity attacks.");
            return true;
        }

        public static bool PerformOpportunityAttack(ICombatant attacker, ICombatant target, TurnResources resources = null, IRandomSource random = null)
        {
            if (attacker == null || target == null || !attacker.IsAlive || !target.IsAlive) return false;
            if (StatusEffectSystem.HasCondition(attacker, ConditionType.Stunned)) return false;

            if (resources != null && !resources.UseReaction())
            {
                GameEvents.RaiseCombatLog($"{attacker.Name} has no reaction remaining for opportunity attack.");
                return false;
            }

            GameEvents.RaiseCombatLog($"{attacker.Name} takes an Opportunity Attack against {target.Name}!");
            return PerformMeleeAttack(attacker, target, resources: null, random: random);
        }

        public static bool StandUp(ICombatant combatant, TurnResources resources = null)
        {
            if (combatant == null || !combatant.IsAlive) return false;
            if (!StatusEffectSystem.HasCondition(combatant, ConditionType.Prone)) return false;

            int cost = (resources?.Speed ?? 30) / 2;
            if (resources != null && !resources.UseMovement(cost))
            {
                GameEvents.RaiseCombatLog($"{combatant.Name} lacks movement ({cost}ft needed) to stand up.");
                return false;
            }

            StatusEffectSystem.RemoveCondition(combatant, ConditionType.Prone);
            GameEvents.RaiseCombatLog($"{combatant.Name} stands up from prone.");
            return true;
        }

        public static bool PerformSecondWind(PlayerCharacter character, TurnResources resources = null, IRandomSource random = null)
        {
            if (character == null || !character.IsAlive) return false;
            if (StatusEffectSystem.HasCondition(character, ConditionType.Stunned)) return false;

            if (resources != null && !resources.UseBonusAction())
            {
                GameEvents.RaiseCombatLog($"{character.Name} has no bonus action remaining.");
                return false;
            }

            var rand = random ?? DiceRoller.RandomSource;
            int roll = DiceRoller.Roll(DiceType.D10, rand).Value;
            int healed = character.Heal(roll + character.Level);
            GameEvents.RaiseCombatLog($"{character.Name} uses Second Wind and recovers {healed} HP.");
            return true;
        }
    }
}
