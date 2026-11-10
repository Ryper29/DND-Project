using System.Collections.Generic;
using DnD.Systems.Dice;
using Xunit;

namespace DnD.Tests.Systems.Dice
{
    public class DiceRollerTests
    {
        [Fact]
        public void Roll_UsesInjectedRandomSource()
        {
            var random = new SequenceRandomSource(19);

            var result = DiceRoller.Roll(DiceType.D20, random);

            Assert.Equal(19, result.Value);
            Assert.Equal(1, random.Calls);
        }

        [Fact]
        public void SeededSource_RepeatsSameSequence()
        {
            var first = new SystemRandomSource(123);
            var second = new SystemRandomSource(123);

            var firstResults = DiceRoller.RollMultiple(DiceType.D20, 8, first);
            var secondResults = DiceRoller.RollMultiple(DiceType.D20, 8, second);

            Assert.Equal(firstResults, secondResults);
        }

        [Fact]
        public void AdvantageAndDisadvantage_ConsumeSameInjectedSequence()
        {
            var random = new SequenceRandomSource(4, 17, 12, 3);

            var advantage = DiceRoller.RollWithAdvantage(DiceType.D20, random);
            var disadvantage = DiceRoller.RollWithDisadvantage(DiceType.D20, random);

            Assert.Equal(17, advantage.Value);
            Assert.Equal(3, disadvantage.Value);
            Assert.Equal(4, random.Calls);
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly Queue<int> _values;

            public int Calls { get; private set; }

            public SequenceRandomSource(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public int Next(int minValue, int maxValue)
            {
                Calls++;
                var value = _values.Dequeue();
                Assert.InRange(value, minValue, maxValue - 1);
                return value;
            }
        }
    }
}
