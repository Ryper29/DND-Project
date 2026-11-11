using System;
using System.Collections.Generic;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Combat;
using DnD.Systems.Dice;
using Xunit;

namespace DnD.Tests.Systems.Combat
{
    public class CombatArgumentValidationTests
    {
        [Fact]
        public void ResolverConstructor_RejectsNullGameManager()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new CombatResolver(null, new SystemRandomSource(1)));

            Assert.Equal("gameManager", exception.ParamName);
        }

        [Fact]
        public void ResolverConstructor_RejectsNullRandomSource()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new CombatResolver(new GameManager(), null));

            Assert.Equal("random", exception.ParamName);
        }

        [Fact]
        public void RunAutoCombat_RejectsNullParty()
        {
            var resolver = new CombatResolver(new GameManager(), new SystemRandomSource(1));

            var exception = Assert.Throws<ArgumentNullException>(() => resolver.RunAutoCombat(null, new List<ICombatant>()));

            Assert.Equal("party", exception.ParamName);
        }

        [Fact]
        public void RunAutoCombat_RejectsNullEnemies()
        {
            var resolver = new CombatResolver(new GameManager(), new SystemRandomSource(1));

            var exception = Assert.Throws<ArgumentNullException>(() => resolver.RunAutoCombat(new List<ICombatant>(), null));

            Assert.Equal("enemies", exception.ParamName);
        }

        [Fact]
        public void InitializeTurnOrder_RejectsNullCombatants()
        {
            var turnManager = new TurnManager();

            var exception = Assert.Throws<ArgumentNullException>(() => turnManager.InitializeTurnOrder(null));

            Assert.Equal("combatants", exception.ParamName);
        }

        [Fact]
        public void InitializeTurnOrder_RejectsNullCombatantEntries()
        {
            var turnManager = new TurnManager();

            var exception = Assert.Throws<ArgumentException>(() => turnManager.InitializeTurnOrder(new List<ICombatant> { null }));

            Assert.Equal("combatants", exception.ParamName);
        }
    }
}
