namespace DnD.Systems.Dice
{
    [System.Serializable]
    public struct RollResult
    {
        public DiceType DiceUsed { get; }
        public int Value { get; }
        public bool IsCriticalHit => DiceUsed == DiceType.D20 && Value == 20;
        public bool IsCriticalFail => DiceUsed == DiceType.D20 && Value == 1;

        public RollResult(DiceType diceUsed, int value)
        {
            DiceUsed = diceUsed;
            Value = value;
        }

        public override string ToString() => $"[{DiceUsed}: {Value}]";
    }
}
