using System.Collections.Generic;
using DnD.Entities;

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
        public Party Party { get; set; } = new Party();
        public HashSet<string> StoryFlags { get; set; } = new HashSet<string>();

        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && StoryFlags.Contains(flag);
        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag))
                StoryFlags.Add(flag);
        }
        public void RemoveFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag))
                StoryFlags.Remove(flag);
        }
    }
}
