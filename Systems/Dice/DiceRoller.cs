namespace DnD.Systems.Dice
{
    /// <summary>
    /// Stateless dice-rolling service. Inject a seed for deterministic replays/tests.
    /// </summary>
    public static class DiceRoller
    {
        private static System.Random _random = new System.Random();

        public static void SetSeed(int seed) => _random = new System.Random(seed);

        public static RollResult Roll(DiceType diceType)
        {
            int maxValue = (int)diceType;
            int result = _random.Next(1, maxValue + 1);
            return new RollResult(diceType, result);
        }

        public static RollResult[] RollMultiple(DiceType diceType, int count)
        {
            var results = new RollResult[count];
            for (int i = 0; i < count; i++)
                results[i] = Roll(diceType);
            return results;
        }

        public static RollResult RollWithAdvantage(DiceType diceType)
        {
            var a = Roll(diceType);
            var b = Roll(diceType);
            return a.Value >= b.Value ? a : b;
        }

        public static RollResult RollWithDisadvantage(DiceType diceType)
        {
            var a = Roll(diceType);
            var b = Roll(diceType);
            return a.Value <= b.Value ? a : b;
        }
    }
}
