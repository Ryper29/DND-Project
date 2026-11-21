namespace DnD.Systems.Combat
{
    // ponytail: flat speed and single action budget; upgrade to multiclass/extra-attack and haste modifiers when class levels advance.
    public class TurnResources
    {
        public const int DefaultSpeed = 30;

        public int ActionsRemaining { get; set; }
        public int BonusActionsRemaining { get; set; }
        public int ReactionsRemaining { get; set; }
        public int MovementRemaining { get; set; }
        public int Speed { get; set; }
        public bool IsDisengaged { get; set; }

        public bool CanAct => ActionsRemaining > 0;
        public bool CanBonusAct => BonusActionsRemaining > 0;
        public bool CanReact => ReactionsRemaining > 0;
        public bool CanMove(int distance) => distance >= 0 && MovementRemaining >= distance;

        public TurnResources(int speed = DefaultSpeed)
        {
            Speed = System.Math.Max(0, speed);
            Reset();
        }

        public bool UseAction()
        {
            if (ActionsRemaining <= 0) return false;
            ActionsRemaining--;
            return true;
        }

        public bool UseBonusAction()
        {
            if (BonusActionsRemaining <= 0) return false;
            BonusActionsRemaining--;
            return true;
        }

        public bool UseReaction()
        {
            if (ReactionsRemaining <= 0) return false;
            ReactionsRemaining--;
            return true;
        }

        public bool UseMovement(int distance)
        {
            if (distance < 0 || MovementRemaining < distance) return false;
            MovementRemaining -= distance;
            return true;
        }

        public void Reset()
        {
            ActionsRemaining = 1;
            BonusActionsRemaining = 1;
            ReactionsRemaining = 1;
            MovementRemaining = Speed;
            IsDisengaged = false;
        }
    }
}
