namespace DnD.Systems.Dice
{
    public static class DiceRoller
    {
        private static IRandomSource _random = new SystemRandomSource();

        public static IRandomSource RandomSource => _random;

        public static void SetSeed(int seed) => _random = new SystemRandomSource(seed);

        public static RollResult Roll(DiceType diceType, IRandomSource random)
        {
            int maxValue = (int)diceType;
            int result = random.Next(1, maxValue + 1);
            return new RollResult(diceType, result);
        }

        public static RollResult Roll(DiceType diceType) => Roll(diceType, _random);

        public static RollResult[] RollMultiple(DiceType diceType, int count, IRandomSource random)
        {
            var results = new RollResult[count];
            for (int i = 0; i < count; i++)
                results[i] = Roll(diceType, random);
            return results;
        }

        public static RollResult[] RollMultiple(DiceType diceType, int count) => RollMultiple(diceType, count, _random);

        public static RollResult RollWithAdvantage(DiceType diceType, IRandomSource random)
        {
            var a = Roll(diceType, random);
            var b = Roll(diceType, random);
            return a.Value >= b.Value ? a : b;
        }

        public static RollResult RollWithAdvantage(DiceType diceType) => RollWithAdvantage(diceType, _random);

        public static RollResult RollWithDisadvantage(DiceType diceType, IRandomSource random)
        {
            var a = Roll(diceType, random);
            var b = Roll(diceType, random);
            return a.Value <= b.Value ? a : b;
        }

        public static RollResult RollWithDisadvantage(DiceType diceType) => RollWithDisadvantage(diceType, _random);
    }
}
