using System.Collections.Generic;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Combat;
using DnD.Systems.Dice;
using DnD.Systems.Magic;
using Xunit;

namespace DnD.Tests.Systems.Magic
{
    public class MagicSystemTests
    {
        [Fact]
        public void SpellSlotManager_TracksSlotsAndRestores()
        {
            var slots = new SpellSlotManager(casterLevel: 3);

            // Level 3 full caster has 4 x 1st level, 2 x 2nd level slots
            Assert.Equal(4, slots.GetMaxSlots(1));
            Assert.Equal(2, slots.GetMaxSlots(2));

            // Cantrip costs 0
            Assert.True(slots.HasSlot(0));
            Assert.True(slots.SpendSlot(0));
            Assert.Equal(4, slots.GetCurrentSlots(1));

            // Spend level 1 slots
            Assert.True(slots.SpendSlot(1));
            Assert.True(slots.SpendSlot(1));
            Assert.Equal(2, slots.GetCurrentSlots(1));

            // Restore all
            slots.RestoreAll();
            Assert.Equal(4, slots.GetCurrentSlots(1));
        }

        [Fact]
        public void SpellSystem_CalculatesDCAndExecutesSavingThrow()
        {
            // Wizard with INT 16 -> mod +3. Level 1 -> prof +2. DC = 8 + 2 + 3 = 13.
            var wizard = new PlayerCharacter("Mage", Race.Human, CharacterClass.Wizard,
                new Stats { Intelligence = 16, Dexterity = 10 });
            int dc = SpellSystem.CalculateSpellSaveDC(wizard);
            Assert.Equal(13, dc);

            var enemy = new Enemy("Goblin", Race.Goblin, new Stats { Dexterity = 14 }, "Scout");

            // Roll 8 + DEX mod (14 -> +2) = 10 < 13 => FAIL
            var failRandom = new FixedRandomSource(8);
            Assert.False(SpellSystem.RollSavingThrow(enemy, AbilityScore.Dexterity, dc, failRandom));

            // Roll 12 + DEX mod (14 -> +2) = 14 >= 13 => SUCCESS
            var passRandom = new FixedRandomSource(12);
            Assert.True(SpellSystem.RollSavingThrow(enemy, AbilityScore.Dexterity, dc, passRandom));
        }

        [Fact]
        public void SpellSystem_ConcentrationBreaksOnFailedSave()
        {
            StatusEffectSystem.ClearAll();
            var cleric = new PlayerCharacter("Cleric", Race.Human, CharacterClass.Cleric,
                new Stats { Wisdom = 16, Constitution = 10 });
            var ally = new PlayerCharacter("Fighter", Race.Human, CharacterClass.Fighter, new Stats());

            // Cast Bless on ally (concentration)
            Assert.True(SpellSystem.CastSpell(cleric, SpellDatabase.Bless, singleTarget: ally));
            Assert.Equal(SpellDatabase.Bless, cleric.ActiveConcentrationSpell);
            Assert.True(StatusEffectSystem.HasCondition(ally, ConditionType.Blessed));

            // Cleric takes 30 damage -> DC = max(10, 30/2) = 15.
            // CON 10 has mod 0. Roll 5 < 15 => fails concentration!
            var failRandom = new FixedRandomSource(5);
            bool passed = SpellSystem.CheckConcentration(cleric, 30, failRandom);

            Assert.False(passed);
            Assert.Null(cleric.ActiveConcentrationSpell);
            Assert.False(StatusEffectSystem.HasCondition(ally, ConditionType.Blessed));
        }

        [Fact]
        public void SpellSystem_AoEFireballDamagesEntitiesInRadius()
        {
            StatusEffectSystem.ClearAll();
            var wizard = new PlayerCharacter("Mage", Race.Human, CharacterClass.Wizard,
                new Stats { Intelligence = 18 });
            wizard.SpellSlots.SetCasterLevel(5); // Level 5 gives 3rd level slots

            var grid = new BattleGrid(10, 10);
            grid.PlaceEntity(wizard, 0, 0);

            var closeEnemy1 = new Enemy("Close1", Race.Goblin, new Stats(50, 5, 2, 1), "Scout");
            var closeEnemy2 = new Enemy("Close2", Race.Goblin, new Stats(50, 5, 2, 1), "Scout");
            var farEnemy = new Enemy("Far", Race.Goblin, new Stats(50, 5, 2, 1), "Scout");

            grid.PlaceEntity(closeEnemy1, 5, 5); // Target center
            grid.PlaceEntity(closeEnemy2, 6, 5); // 5ft away (inside 15ft radius)
            grid.PlaceEntity(farEnemy, 9, 9);    // > 15ft away (outside radius)

            int hp1Before = closeEnemy1.CurrentHealth;
            int hp2Before = closeEnemy2.CurrentHealth;
            int hpFarBefore = farEnemy.CurrentHealth;

            // Cast Fireball at (5, 5) with a roll that fails saves
            var failRandom = new FixedRandomSource(1); // Natural 1 fails
            Assert.True(SpellSystem.CastSpell(wizard, SpellDatabase.Fireball, 5, 5, grid: grid, random: failRandom));

            // Enemies in radius take damage
            Assert.True(closeEnemy1.CurrentHealth < hp1Before);
            Assert.True(closeEnemy2.CurrentHealth < hp2Before);

            // Far enemy takes NO damage
            Assert.Equal(hpFarBefore, farEnemy.CurrentHealth);
        }

        [Fact]
        public void SpellSystem_CantripsCastWithoutSpendingSlots()
        {
            var wizard = new PlayerCharacter("Mage", Race.Human, CharacterClass.Wizard, new Stats());
            var enemy = new Enemy("Orc", Race.Orc, new Stats(50, 5, 2, 1), "Warrior");

            int slotCount = wizard.SpellSlots.GetCurrentSlots(1);

            // FireBolt is a Cantrip (level 0)
            Assert.True(SpellSystem.CastSpell(wizard, SpellDatabase.FireBolt, singleTarget: enemy));

            // Level 1 slots remain unchanged
            Assert.Equal(slotCount, wizard.SpellSlots.GetCurrentSlots(1));
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly int _value;
            public FixedRandomSource(int value) => _value = value;
            public int Next(int min, int max) => _value;
        }
    }
}
