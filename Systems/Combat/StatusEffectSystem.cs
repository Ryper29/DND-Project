using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Dice;

namespace DnD.Systems.Combat
{
    // ponytail: in-memory dictionary for status tracking; upgrade to entity component model when serializing full combat state.
    public static class StatusEffectSystem
    {
        private static readonly Dictionary<ICombatant, Dictionary<ConditionType, int>> _activeConditions = new();

        public static void ApplyCondition(ICombatant target, ConditionType condition, int durationRounds = 1)
        {
            if (target == null || condition == ConditionType.None || durationRounds <= 0) return;

            if (!_activeConditions.TryGetValue(target, out var targetConditions))
            {
                targetConditions = new Dictionary<ConditionType, int>();
                _activeConditions[target] = targetConditions;
            }

            if (targetConditions.TryGetValue(condition, out int currentDuration))
                targetConditions[condition] = Math.Max(currentDuration, durationRounds);
            else
                targetConditions[condition] = durationRounds;

            GameEvents.RaiseCombatLog($"{target.Name} is affected by {condition} ({durationRounds} round{(durationRounds > 1 ? "s" : "")}).");
        }

        public static bool RemoveCondition(ICombatant target, ConditionType condition)
        {
            if (target == null || condition == ConditionType.None) return false;

            if (_activeConditions.TryGetValue(target, out var targetConditions) && targetConditions.Remove(condition))
            {
                if (targetConditions.Count == 0)
                    _activeConditions.Remove(target);

                GameEvents.RaiseCombatLog($"{target.Name} is no longer {condition}.");
                return true;
            }

            return false;
        }

        public static bool HasCondition(ICombatant target, ConditionType condition)
        {
            if (target == null || condition == ConditionType.None) return false;
            return _activeConditions.TryGetValue(target, out var targetConditions) && targetConditions.ContainsKey(condition);
        }

        public static int GetConditionDuration(ICombatant target, ConditionType condition)
        {
            if (target == null || condition == ConditionType.None) return 0;
            if (_activeConditions.TryGetValue(target, out var targetConditions) && targetConditions.TryGetValue(condition, out int duration))
                return duration;
            return 0;
        }

        public static IReadOnlyList<ConditionType> GetConditions(ICombatant target)
        {
            if (target == null || !_activeConditions.TryGetValue(target, out var targetConditions))
                return Array.Empty<ConditionType>();

            return targetConditions.Keys.ToList();
        }

        public static void TickConditions(ICombatant target)
        {
            if (target == null || !_activeConditions.TryGetValue(target, out var targetConditions)) return;

            var expired = new List<ConditionType>();
            foreach (var condition in targetConditions.Keys.ToList())
            {
                targetConditions[condition]--;
                if (targetConditions[condition] <= 0)
                    expired.Add(condition);
            }

            foreach (var condition in expired)
            {
                targetConditions.Remove(condition);
                GameEvents.RaiseCombatLog($"{condition} on {target.Name} has expired.");
            }

            if (targetConditions.Count == 0)
                _activeConditions.Remove(target);
        }

        public static (AdvantageState State, bool IsBlessed) GetAttackModifier(ICombatant attacker, ICombatant target, bool isRanged = false)
        {
            bool hasAdvantage = false;
            bool hasDisadvantage = false;

            if (attacker != null)
            {
                if (HasCondition(attacker, ConditionType.Blinded)) hasDisadvantage = true;
                if (HasCondition(attacker, ConditionType.Poisoned)) hasDisadvantage = true;
                if (HasCondition(attacker, ConditionType.Prone)) hasDisadvantage = true;
            }

            if (target != null)
            {
                if (HasCondition(target, ConditionType.Blinded)) hasAdvantage = true;
                if (HasCondition(target, ConditionType.Stunned)) hasAdvantage = true;

                if (HasCondition(target, ConditionType.Prone))
                {
                    if (isRanged) hasDisadvantage = true;
                    else hasAdvantage = true;
                }
            }

            AdvantageState state = AdvantageState.Normal;
            if (hasAdvantage && !hasDisadvantage) state = AdvantageState.Advantage;
            else if (hasDisadvantage && !hasAdvantage) state = AdvantageState.Disadvantage;

            bool isBlessed = attacker != null && HasCondition(attacker, ConditionType.Blessed);
            return (state, isBlessed);
        }

        public static RollResult RollAttackD20(ICombatant attacker, ICombatant target, bool isRanged = false, IRandomSource random = null)
        {
            var rand = random ?? DiceRoller.RandomSource;
            var (state, _) = GetAttackModifier(attacker, target, isRanged);

            return state switch
            {
                AdvantageState.Advantage => DiceRoller.RollWithAdvantage(DiceType.D20, rand),
                AdvantageState.Disadvantage => DiceRoller.RollWithDisadvantage(DiceType.D20, rand),
                _ => DiceRoller.Roll(DiceType.D20, rand)
            };
        }

        public static void Clear(ICombatant target)
        {
            if (target != null) _activeConditions.Remove(target);
        }

        public static void ClearAll()
        {
            _activeConditions.Clear();
        }
    }
}
