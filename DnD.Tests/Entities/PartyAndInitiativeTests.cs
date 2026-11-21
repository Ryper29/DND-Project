using System;
using System.Collections.Generic;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Combat;
using DnD.Systems.Dice;
using Xunit;

namespace DnD.Tests.Entities
{
    public class PartyAndInitiativeTests
    {
        [Fact]
        public void Party_AddRemoveAndCapacity()
        {
            var party = new Party(maxMembers: 3);

            var p1 = new PlayerCharacter("P1", Race.Human, CharacterClass.Fighter, new Stats());
            var p2 = new PlayerCharacter("P2", Race.Elf, CharacterClass.Wizard, new Stats());
            var p3 = new PlayerCharacter("P3", Race.Dwarf, CharacterClass.Cleric, new Stats());
            var p4 = new PlayerCharacter("P4", Race.Halfling, CharacterClass.Rogue, new Stats());

            Assert.True(party.AddMember(p1));
            Assert.True(party.AddMember(p2));
            Assert.True(party.AddMember(p3));
            Assert.True(party.IsFull);

            // Rejects 4th member
            Assert.False(party.AddMember(p4));
            Assert.Equal(3, party.Count);

            // Rejects duplicates and null
            Assert.False(party.AddMember(p1));
            Assert.False(party.AddMember(null));

            // Leader is first member
            Assert.Equal(p1, party.Leader);

            // Remove member
            Assert.True(party.RemoveMember(p2));
            Assert.Equal(2, party.Count);
            Assert.False(party.IsFull);
        }

        [Fact]
        public void Party_RestAndExperienceDistribution()
        {
            var party = new Party();
            var p1 = new PlayerCharacter("P1", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));
            var p2 = new PlayerCharacter("P2", Race.Elf, CharacterClass.Wizard, new Stats(100, 10, 5, 2));
            party.AddMember(p1);
            party.AddMember(p2);

            p1.TakeDamage(40);
            p2.SpendMana(5);

            party.RestAll();
            Assert.Equal(p1.MaxHealth, p1.CurrentHealth);
            Assert.Equal(p2.MaxMana, p2.CurrentMana);

            // Distribute 60 XP -> 30 each (under 50 XP level up threshold)
            party.DistributeExperience(60);
            Assert.Equal(30, p1.Experience);
            Assert.Equal(30, p2.Experience);
            Assert.Equal(1, p1.Level);

            // Distribute 40 more XP -> 20 each -> total 50 -> level up to 2
            party.DistributeExperience(40);
            Assert.Equal(2, p1.Level);
            Assert.Equal(2, p2.Level);
        }

        [Fact]
        public void TurnManager_OrdersByInitiativeDescending()
        {
            var turnManager = new TurnManager();

            // P1 has DEX 10 (mod +0)
            var stats1 = new Stats { Dexterity = 10 };
            var p1 = new PlayerCharacter("Slow", Race.Human, CharacterClass.Fighter, stats1);

            // P2 has DEX 18 (mod +4)
            var stats2 = new Stats { Dexterity = 18 };
            var p2 = new PlayerCharacter("Fast", Race.Elf, CharacterClass.Rogue, stats2);

            // Deterministic random rolls: first roll (for p1) = 10, second roll (for p2) = 10
            // p1 total = 10 + 0 = 10
            // p2 total = 10 + 4 = 14
            var fakeRandom = new StepRandomSource(10, 10);

            turnManager.InitializeTurnOrderWithInitiative(new List<ICombatant> { p1, p2 }, fakeRandom);

            // Elf racial bonus gives +2 DEX (18 -> 20, mod +5). 10 + 5 = 15.
            Assert.Equal(p2, turnManager.TurnOrder[0]);
            Assert.Equal(p1, turnManager.TurnOrder[1]);
            Assert.Equal(15, turnManager.InitiativeScores[p2]);
            Assert.Equal(10, turnManager.InitiativeScores[p1]);
        }

        [Fact]
        public void TurnManager_InitiativeValidatesArguments()
        {
            var turnManager = new TurnManager();
            Assert.Throws<ArgumentNullException>(() => turnManager.InitializeTurnOrderWithInitiative(null));
            Assert.Throws<ArgumentException>(() => turnManager.InitializeTurnOrderWithInitiative(new List<ICombatant> { null }));
        }

        private sealed class StepRandomSource : IRandomSource
        {
            private readonly Queue<int> _values;
            public StepRandomSource(params int[] values) => _values = new Queue<int>(values);
            public int Next(int min, int max) => _values.Count > 0 ? _values.Dequeue() : min;
        }
    }
}
