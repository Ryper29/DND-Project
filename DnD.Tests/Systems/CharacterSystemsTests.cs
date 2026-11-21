using System;
using System.IO;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Inventory;
using Xunit;

namespace DnD.Tests.Systems
{
    public class CharacterSystemsTests
    {
        [Fact]
        public void DefendReducesNextDamageAndThenExpires()
        {
            var entity = new Enemy("Target", Race.Human, new Stats(100, 1, 0, 0), "Test");
            entity.Defend();
            entity.TakeDamage(15);
            Assert.Equal(entity.MaxHealth - 5, entity.CurrentHealth);
            entity.TakeDamage(15);
            Assert.Equal(entity.MaxHealth - 20, entity.CurrentHealth);
        }

        [Fact]
        public void RaceModifiersDoNotMutateOriginalStats()
        {
            var stats = new Stats(10, 10, 10, 10, 10, 10, 100, 10, 10, 10, 0, 0, 0);
            var elf = new PlayerCharacter("Elf", Race.Elf, CharacterClass.Rogue, stats);
            Assert.Equal(12, elf.BaseStats.Dexterity);
            Assert.Equal(10, stats.Dexterity);
        }

        [Fact]
        public void ConsumableRestoresHealthAndIsRemoved()
        {
            var player = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats(100, 1, 0, 0));
            var potion = new ConsumableData("Potion", ConsumableEffect.Heal, 20);
            player.Inventory.AddConsumable(potion);
            player.TakeDamage(30);
            Assert.True(player.UseConsumable(0));
            Assert.Empty(player.Inventory.Consumables);
            Assert.Equal(player.MaxHealth - 10, player.CurrentHealth);
        }

        [Fact]
        public void SaveLoadRoundTripRestoresCharacterAndProgress()
        {
            var player = new PlayerCharacter("Hero", Race.Elf, CharacterClass.Rogue,
                new Stats(15, 14, 13, 12, 10, 8, 100, 10, 10, 20, 20, 5, 2));
            player.GainExperience(50);
            player.TakeDamage(25);
            player.Inventory.AddConsumable(new ConsumableData("Potion", ConsumableEffect.Heal, 25));
            var save = SaveSystem.FromCharacter(player, 2, false);
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            try
            {
                SaveSystem.Save(path, save);
                var restored = SaveSystem.ToCharacter(SaveSystem.Load(path));
                Assert.Equal(player.Name, restored.Name);
                Assert.Equal(player.Race, restored.Race);
                Assert.Equal(player.Level, restored.Level);
                Assert.Equal(player.CurrentHealth, restored.CurrentHealth);
                Assert.Equal(player.BaseStats.Dexterity, restored.BaseStats.Dexterity);
                Assert.Single(restored.Inventory.Consumables);
                Assert.Equal("Potion", restored.Inventory.Consumables[0].Name);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}