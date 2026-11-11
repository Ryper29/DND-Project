using System.Collections.Generic;
using System.Linq;
using System;
using DnD.Entities;

namespace DnD.Systems.Combat
{
    public class TurnManager
    {
        private List<ICombatant> _turnOrder = new List<ICombatant>();
        private int _currentIndex;

        public ICombatant CurrentCombatant =>
            _turnOrder.Count > 0 && _currentIndex < _turnOrder.Count ? _turnOrder[_currentIndex] : null;

        public IReadOnlyList<ICombatant> TurnOrder => _turnOrder;
        public bool AllTurnsComplete => _currentIndex >= _turnOrder.Count;

        public void InitializeTurnOrder(List<ICombatant> combatants)
        {
            if (combatants == null) throw new ArgumentNullException(nameof(combatants));
            if (combatants.Any(combatant => combatant == null))
                throw new ArgumentException("Combatants cannot contain null entries.", nameof(combatants));

            _turnOrder = combatants.Where(c => c.IsAlive).ToList();
            _currentIndex = 0;
        }

        public ICombatant AdvanceTurn()
        {
            _currentIndex++;
            while (_currentIndex < _turnOrder.Count && !_turnOrder[_currentIndex].IsAlive)
                _currentIndex++;
            return _currentIndex < _turnOrder.Count ? _turnOrder[_currentIndex] : null;
        }

        public void ResetTurnOrder()
        {
            _turnOrder = _turnOrder.Where(c => c.IsAlive).ToList();
            _currentIndex = 0;
        }

        public bool IsCombatOver()
        {
            return _turnOrder.Count(c => c.IsAlive) <= 1;
        }
    }
}
