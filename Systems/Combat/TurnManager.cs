using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Dice;

namespace DnD.Systems.Combat
{
    // ponytail: simplified round tick on AdvanceTurn; upgrade to initiative-based trigger queues when reactions/legendary actions added.
    public class TurnManager
    {
        private List<ICombatant> _turnOrder = new List<ICombatant>();
        private int _currentIndex;
        private readonly Dictionary<ICombatant, TurnResources> _resources = new();
        private readonly Dictionary<ICombatant, int> _initiatives = new();

        public ICombatant CurrentCombatant =>
            _turnOrder.Count > 0 && _currentIndex < _turnOrder.Count ? _turnOrder[_currentIndex] : null;

        public IReadOnlyList<ICombatant> TurnOrder => _turnOrder;
        public IReadOnlyDictionary<ICombatant, int> InitiativeScores => _initiatives;
        public bool AllTurnsComplete => _currentIndex >= _turnOrder.Count;

        public TurnResources CurrentResources => CurrentCombatant != null ? GetResources(CurrentCombatant) : null;

        public TurnResources GetResources(ICombatant combatant)
        {
            if (combatant == null) return null;
            if (!_resources.TryGetValue(combatant, out var resources))
            {
                resources = new TurnResources();
                _resources[combatant] = resources;
            }
            return resources;
        }

        public void InitializeTurnOrder(List<ICombatant> combatants)
        {
            if (combatants == null) throw new ArgumentNullException(nameof(combatants));
            if (combatants.Any(combatant => combatant == null))
                throw new ArgumentException("Combatants cannot contain null entries.", nameof(combatants));

            _turnOrder = combatants.Where(c => c.IsAlive).ToList();
            _currentIndex = 0;
            _resources.Clear();

            foreach (var combatant in _turnOrder)
                _resources[combatant] = new TurnResources();

            PrepareCurrentTurn();
        }

        public void InitializeTurnOrderWithInitiative(List<ICombatant> combatants, IRandomSource random = null)
        {
            if (combatants == null) throw new ArgumentNullException(nameof(combatants));
            if (combatants.Any(combatant => combatant == null))
                throw new ArgumentException("Combatants cannot contain null entries.", nameof(combatants));

            var rand = random ?? DiceRoller.RandomSource;
            _initiatives.Clear();

            var living = combatants.Where(c => c.IsAlive).ToList();
            foreach (var c in living)
            {
                int dex = (c is BaseEntity b) ? b.BaseStats.Dexterity : 10;
                int dexMod = (dex - 10) / 2;
                int roll = DiceRoller.Roll(DiceType.D20, rand).Value;
                int total = roll + dexMod;
                _initiatives[c] = total;
                GameEvents.RaiseCombatLog($"{c.Name} rolled initiative {total} ({roll} + {dexMod}).");
            }

            _turnOrder = living
                .OrderByDescending(c => _initiatives[c])
                .ThenByDescending(c => (c is BaseEntity b) ? b.BaseStats.Dexterity : 10)
                .ThenBy(c => c.Name)
                .ToList();

            _currentIndex = 0;
            _resources.Clear();

            foreach (var combatant in _turnOrder)
                _resources[combatant] = new TurnResources();

            PrepareCurrentTurn();
        }

        public ICombatant AdvanceTurn()
        {
            var previous = CurrentCombatant;
            if (previous != null)
                StatusEffectSystem.TickConditions(previous);

            _currentIndex++;
            while (_currentIndex < _turnOrder.Count && !_turnOrder[_currentIndex].IsAlive)
                _currentIndex++;

            if (_currentIndex < _turnOrder.Count)
            {
                PrepareCurrentTurn();
                return _turnOrder[_currentIndex];
            }

            return null;
        }

        public void ResetTurnOrder()
        {
            _turnOrder = _turnOrder.Where(c => c.IsAlive).ToList();
            _currentIndex = 0;
            if (_turnOrder.Count > 0)
                PrepareCurrentTurn();
        }

        public bool IsCombatOver()
        {
            return _turnOrder.Count(c => c.IsAlive) <= 1;
        }

        private void PrepareCurrentTurn()
        {
            var current = CurrentCombatant;
            if (current == null) return;

            var res = GetResources(current);
            res.Reset();

            if (StatusEffectSystem.HasCondition(current, ConditionType.Stunned))
            {
                res.ActionsRemaining = 0;
                res.BonusActionsRemaining = 0;
                res.MovementRemaining = 0;
                GameEvents.RaiseCombatLog($"{current.Name} is stunned and cannot act this turn!");
            }
        }
    }
}
