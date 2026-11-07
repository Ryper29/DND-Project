using System;
using DnD.Entities;
using DnD.Systems.Dice;

namespace DnD.Core
{
    /// <summary>
    /// Static event bus. Core logic raises events here; presentation layers subscribe.
    /// In Unity, a MonoBehaviour subscribes in OnEnable and unsubscribes in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<string, RollResult> OnDiceRolled;
        public static event Action<ICombatant, ICombatant, int> OnAttackPerformed;
        public static event Action<ICombatant, int, int> OnDamageTaken;
        public static event Action<ICombatant> OnEntityDefeated;
        public static event Action<ICombatant> OnTurnChanged;
        public static event Action<ICombatant, int> OnExperienceGained;
        public static event Action<ICombatant, int> OnLevelUp;
        public static event Action<string> OnCombatLog;
        public static event Action<int> OnRoundStarted;

        public static void RaiseDiceRolled(string rollerName, RollResult result)
            => OnDiceRolled?.Invoke(rollerName, result);

        public static void RaiseAttackPerformed(ICombatant attacker, ICombatant target, int damage)
            => OnAttackPerformed?.Invoke(attacker, target, damage);

        public static void RaiseDamageTaken(ICombatant entity, int damageReceived, int remainingHp)
            => OnDamageTaken?.Invoke(entity, damageReceived, remainingHp);

        public static void RaiseEntityDefeated(ICombatant entity)
            => OnEntityDefeated?.Invoke(entity);

        public static void RaiseTurnChanged(ICombatant entity)
            => OnTurnChanged?.Invoke(entity);

        public static void RaiseExperienceGained(ICombatant entity, int amount)
            => OnExperienceGained?.Invoke(entity, amount);

        public static void RaiseLevelUp(ICombatant entity, int newLevel)
            => OnLevelUp?.Invoke(entity, newLevel);

        public static void RaiseCombatLog(string message)
            => OnCombatLog?.Invoke(message);

        public static void RaiseRoundStarted(int roundNumber)
            => OnRoundStarted?.Invoke(roundNumber);

        /// <summary>
        /// Clears all subscribers. Call between tests or scene transitions.
        /// </summary>
        public static void ClearAll()
        {
            OnDiceRolled = null;
            OnAttackPerformed = null;
            OnDamageTaken = null;
            OnEntityDefeated = null;
            OnTurnChanged = null;
            OnExperienceGained = null;
            OnLevelUp = null;
            OnCombatLog = null;
            OnRoundStarted = null;
        }
    }
}
