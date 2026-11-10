namespace DnD.Systems.Dice
{
    public interface IRandomSource
    {
        int Next(int minValue, int maxValue);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly System.Random _random;

        public SystemRandomSource() : this(new System.Random())
        {
        }

        public SystemRandomSource(int seed) : this(new System.Random(seed))
        {
        }

        private SystemRandomSource(System.Random random)
        {
            _random = random;
        }

        public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
    }
}
