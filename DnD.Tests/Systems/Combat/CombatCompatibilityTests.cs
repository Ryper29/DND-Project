using System.Collections.Generic;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Combat;
using DnD.Systems.Dice;
using Xunit;

namespace DnD.Tests.Systems.Combat
{
    public class CombatCompatibilityTests
    {
        [Fact]
        public void LegacyCombatResolverConstructor_UsesSharedDiceSourceAndOldCombatantApi()
        {
            GameEvents.ClearAll();
            try
            {
                var legacyPartyMember = new LegacyCombatant("Legacy party");
                var legacyEnemy = new LegacyCombatant("Legacy enemy");
                var manager = new GameManager();
                var resolver = new CombatResolver(manager);

                resolver.RunAutoCombat(new List<ICombatant> { legacyPartyMember }, new List<ICombatant> { legacyEnemy }, 1);

                Assert.Equal(1, legacyPartyMember.AttackCalls);
                Assert.Equal(1, legacyEnemy.AttackCalls);
                Assert.Equal(GamePhase.PostCombat, manager.State.CurrentPhase);
            }
            finally
            {
                GameEvents.ClearAll();
                DiceRoller.SetSeed(0);
            }
        }

        private sealed class LegacyCombatant : ICombatant
        {
            public string Name { get; }
            public Race Race => Race.Human;
            public bool IsAlive => true;
            public int CurrentHealth => 100;
            public int MaxHealth => 100;
            public int AttackPower => 1;
            public int DefensePower => 0;
            public int AttackCalls { get; private set; }

            public LegacyCombatant(string name)
            {
                Name = name;
            }

            public void TakeDamage(int rawDamage)
            {
            }

            public void PerformAttack(ICombatant target)
            {
                AttackCalls++;
                DiceRoller.Roll(DiceType.D20);
            }
        }
    }
}
