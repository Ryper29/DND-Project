using System.Collections.Generic;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Combat;
using DnD.Systems.Dice;
using Xunit;

namespace DnD.Tests.Systems.Combat
{
    public class CombatResolverTests
    {
        [Fact]
        public void Resolver_UsesSharedRandomSourceForTargetSelectionAndAttacks()
        {
            GameEvents.ClearAll();
            try
            {
                var random = new SequenceRandomSource(1, 20, 0, 20, 0, 20);
                var fighter = new PlayerCharacter("Fighter", Race.Human, CharacterClass.Fighter, new Stats(100, 1, 0, 0));
                var firstEnemy = new Enemy("First", Race.Goblin, new Stats(100, 1, 0, 0), "basic");
                var selectedEnemy = new Enemy("Selected", Race.Goblin, new Stats(100, 1, 0, 0), "basic");
                var gameManager = new GameManager();
                var resolver = new CombatResolver(gameManager, random);
                var attackedTargets = new List<string>();
                GameEvents.OnAttackPerformed += (_, target, _) => attackedTargets.Add(target.Name);

                resolver.RunAutoCombat(new List<ICombatant> { fighter }, new List<ICombatant> { firstEnemy, selectedEnemy }, 1);

                Assert.Equal(new[] { "Selected", "Fighter", "Fighter" }, attackedTargets);
                Assert.Equal(6, random.Calls);
                Assert.Equal(new[] { (0, 2), (1, 21), (0, 1), (1, 21), (0, 1), (1, 21) }, random.Ranges);
                Assert.Equal(GamePhase.PostCombat, gameManager.State.CurrentPhase);
            }
            finally
            {
                GameEvents.ClearAll();
            }
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly Queue<int> _values;

            public int Calls { get; private set; }
            public List<(int MinValue, int MaxValue)> Ranges { get; } = new List<(int, int)>();

            public SequenceRandomSource(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public int Next(int minValue, int maxValue)
            {
                Calls++;
                Ranges.Add((minValue, maxValue));
                var value = _values.Dequeue();
                Assert.InRange(value, minValue, maxValue - 1);
                return value;
            }
        }
    }
}
