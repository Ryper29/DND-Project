using System.Collections.Generic;
using DnD.Entities;
using DnD.Systems.Combat;

namespace DnD.Core
{
    public class GameManager
    {
        private readonly GameState _state;
        private readonly TurnManager _turnManager;

        public GameState State => _state;
        public TurnManager TurnManager => _turnManager;

        public GameManager()
        {
            _state = new GameState();
            _turnManager = new TurnManager();
        }

        public void StartCombat(List<ICombatant> combatants)
        {
            _state.CurrentPhase = GamePhase.Combat;
            _state.CurrentRound = 1;
            _turnManager.InitializeTurnOrder(combatants);
            GameEvents.RaiseRoundStarted(_state.CurrentRound);
            GameEvents.RaiseCombatLog("Combat has begun!");
        }

        public void AdvanceRound()
        {
            _state.CurrentRound++;
            _state.TotalRoundsPlayed++;
            _turnManager.ResetTurnOrder();
            GameEvents.RaiseRoundStarted(_state.CurrentRound);
        }

        public void EndCombat()
        {
            _state.CurrentPhase = GamePhase.PostCombat;
            GameEvents.RaiseCombatLog("Combat has ended.");
        }
    }
}
