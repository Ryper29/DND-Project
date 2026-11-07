using System;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Dice;

namespace DnD.Presentation
{
    /// <summary>
    /// Subscribes to GameEvents and renders them to the console.
    /// In Unity, replace this with a MonoBehaviour that subscribes to the same events
    /// and updates TextMeshPro, health bars, animation triggers, etc.
    /// </summary>
    public class ConsoleUIRenderer : IDisposable
    {
        public ConsoleUIRenderer()
        {
            Subscribe();
        }

        private void Subscribe()
        {
            GameEvents.OnCombatLog += HandleCombatLog;
            GameEvents.OnDiceRolled += HandleDiceRolled;
            GameEvents.OnAttackPerformed += HandleAttackPerformed;
            GameEvents.OnDamageTaken += HandleDamageTaken;
            GameEvents.OnEntityDefeated += HandleEntityDefeated;
            GameEvents.OnTurnChanged += HandleTurnChanged;
            GameEvents.OnExperienceGained += HandleExperienceGained;
            GameEvents.OnLevelUp += HandleLevelUp;
            GameEvents.OnRoundStarted += HandleRoundStarted;
        }

        public void Unsubscribe()
        {
            GameEvents.OnCombatLog -= HandleCombatLog;
            GameEvents.OnDiceRolled -= HandleDiceRolled;
            GameEvents.OnAttackPerformed -= HandleAttackPerformed;
            GameEvents.OnDamageTaken -= HandleDamageTaken;
            GameEvents.OnEntityDefeated -= HandleEntityDefeated;
            GameEvents.OnTurnChanged -= HandleTurnChanged;
            GameEvents.OnExperienceGained -= HandleExperienceGained;
            GameEvents.OnLevelUp -= HandleLevelUp;
            GameEvents.OnRoundStarted -= HandleRoundStarted;
        }

        public void Dispose() => Unsubscribe();

        // --- Handlers ---

        private void HandleCombatLog(string message)
        {
            Console.WriteLine(message);
        }

        private void HandleDiceRolled(string rollerName, RollResult result)
        {
            string extra = "";
            if (result.IsCriticalHit) extra = " ★ NATURAL 20!";
            else if (result.IsCriticalFail) extra = " ✗ NATURAL 1!";
            Console.WriteLine($"  🎲 {rollerName} rolls {result.DiceUsed}: {result.Value}{extra}");
        }

        private void HandleAttackPerformed(ICombatant attacker, ICombatant target, int damage)
        {
            Console.WriteLine($"  ⚔ {attacker.Name} → {target.Name} for {damage} potential damage");
        }

        private void HandleDamageTaken(ICombatant entity, int damageReceived, int remainingHp)
        {
            Console.WriteLine($"  💥 {entity.Name} takes {damageReceived} damage (HP: {remainingHp}/{entity.MaxHealth})");
        }

        private void HandleEntityDefeated(ICombatant entity)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ☠ {entity.Name} has been defeated!");
            Console.ResetColor();
        }

        private void HandleTurnChanged(ICombatant entity)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n>> {entity.Name}'s turn ({entity.Race})");
            Console.ResetColor();
        }

        private void HandleExperienceGained(ICombatant entity, int amount)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ✦ {entity.Name} gains {amount} EXP");
            Console.ResetColor();
        }

        private void HandleLevelUp(ICombatant entity, int newLevel)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ★ LEVEL UP! {entity.Name} is now Level {newLevel}! (HP fully restored)");
            Console.ResetColor();
        }

        private void HandleRoundStarted(int roundNumber)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\n═══════════════════════════════════════");
            Console.WriteLine($"  ROUND {roundNumber}");
            Console.WriteLine($"═══════════════════════════════════════");
            Console.ResetColor();
        }

        // Utility: Print a character stat block
        public static void PrintCharacterStats(ICombatant combatant)
        {
            Console.WriteLine($"┌─── {combatant.Name} ({combatant.Race}) ───");
            Console.WriteLine($"│ HP:  {combatant.CurrentHealth}/{combatant.MaxHealth}");
            Console.WriteLine($"│ ATK: {combatant.AttackPower}");
            Console.WriteLine($"│ DEF: {combatant.DefensePower}");

            if (combatant is ILevelable levelable)
            {
                Console.WriteLine($"│ LVL: {levelable.Level}  EXP: {levelable.Experience}/{levelable.ExperienceToNextLevel}");
            }

            Console.WriteLine($"│ Status: {(combatant.IsAlive ? "Alive" : "DEFEATED")}");
            Console.WriteLine($"└{'─'.ToString().PadRight(30, '─')}");
        }
    }
}
