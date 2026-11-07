namespace DnD.Core
{
    public enum GamePhase
    {
        Setup,
        Exploration,
        Combat,
        PostCombat,
        GameOver
    }

    [System.Serializable]
    public class GameState
    {
        public GamePhase CurrentPhase { get; set; } = GamePhase.Setup;
        public int CurrentRound { get; set; }
        public int TotalRoundsPlayed { get; set; }
    }
}
