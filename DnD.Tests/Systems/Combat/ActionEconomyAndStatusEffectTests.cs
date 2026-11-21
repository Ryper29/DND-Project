using System.Collections.Generic;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Combat;
using DnD.Systems.Dice;
using Xunit;

namespace DnD.Tests.Systems.Combat
{
    public class ActionEconomyAndStatusEffectTests
    {
        [Fact]
        public void TurnResources_InitializesAndResetsProperly()
        {
            var res = new TurnResources(30);

            Assert.True(res.CanAct);
            Assert.True(res.CanBonusAct);
            Assert.True(res.CanReact);
            Assert.Equal(30, res.MovementRemaining);

            Assert.True(res.UseAction());
            Assert.False(res.CanAct);
            Assert.False(res.UseAction());

            Assert.True(res.UseBonusAction());
            Assert.False(res.CanBonusAct);

            Assert.True(res.UseReaction());
            Assert.False(res.CanReact);

            Assert.True(res.UseMovement(20));
            Assert.Equal(10, res.MovementRemaining);
            Assert.False(res.UseMovement(15));

            res.Reset();
            Assert.True(res.CanAct);
            Assert.True(res.CanBonusAct);
            Assert.True(res.CanReact);
            Assert.Equal(30, res.MovementRemaining);
        }

        [Fact]
        public void StatusEffectSystem_AppliesTicksAndExpiresConditions()
        {
            StatusEffectSystem.ClearAll();
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));

            StatusEffectSystem.ApplyCondition(hero, ConditionType.Poisoned, 2);

            Assert.True(StatusEffectSystem.HasCondition(hero, ConditionType.Poisoned));
            Assert.Equal(2, StatusEffectSystem.GetConditionDuration(hero, ConditionType.Poisoned));

            StatusEffectSystem.TickConditions(hero);
            Assert.True(StatusEffectSystem.HasCondition(hero, ConditionType.Poisoned));
            Assert.Equal(1, StatusEffectSystem.GetConditionDuration(hero, ConditionType.Poisoned));

            StatusEffectSystem.TickConditions(hero);
            Assert.False(StatusEffectSystem.HasCondition(hero, ConditionType.Poisoned));
            Assert.Equal(0, StatusEffectSystem.GetConditionDuration(hero, ConditionType.Poisoned));
        }

        [Fact]
        public void StatusEffectSystem_CalculatesAdvantageAndDisadvantageCorrectly()
        {
            StatusEffectSystem.ClearAll();
            var attacker = new PlayerCharacter("Attacker", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));
            var target = new Enemy("Goblin", Race.Goblin, new Stats(50, 5, 2, 1), "Scout");

            // Normal state
            var (normalState, _) = StatusEffectSystem.GetAttackModifier(attacker, target, isRanged: false);
            Assert.Equal(AdvantageState.Normal, normalState);

            // Attacker blinded => Disadvantage
            StatusEffectSystem.ApplyCondition(attacker, ConditionType.Blinded, 1);
            var (blindState, _) = StatusEffectSystem.GetAttackModifier(attacker, target, isRanged: false);
            Assert.Equal(AdvantageState.Disadvantage, blindState);

            // Target also stunned => both advantage and disadvantage => cancels to Normal
            StatusEffectSystem.ApplyCondition(target, ConditionType.Stunned, 1);
            var (cancelledState, _) = StatusEffectSystem.GetAttackModifier(attacker, target, isRanged: false);
            Assert.Equal(AdvantageState.Normal, cancelledState);

            // Remove blind from attacker => only stunned target remains => Advantage
            StatusEffectSystem.RemoveCondition(attacker, ConditionType.Blinded);
            var (advantageState, _) = StatusEffectSystem.GetAttackModifier(attacker, target, isRanged: false);
            Assert.Equal(AdvantageState.Advantage, advantageState);

            // Target prone: Melee gets Advantage, Ranged gets Disadvantage
            StatusEffectSystem.ClearAll();
            StatusEffectSystem.ApplyCondition(target, ConditionType.Prone, 1);
            var (meleeProneState, _) = StatusEffectSystem.GetAttackModifier(attacker, target, isRanged: false);
            var (rangedProneState, _) = StatusEffectSystem.GetAttackModifier(attacker, target, isRanged: true);
            Assert.Equal(AdvantageState.Advantage, meleeProneState);
            Assert.Equal(AdvantageState.Disadvantage, rangedProneState);
        }

        [Fact]
        public void TurnManager_StunnedCombatantLosesTurnActions()
        {
            StatusEffectSystem.ClearAll();
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));
            var enemy = new Enemy("Orc", Race.Orc, new Stats(80, 8, 3, 1), "Warrior");

            StatusEffectSystem.ApplyCondition(hero, ConditionType.Stunned, 1);

            var turnManager = new TurnManager();
            turnManager.InitializeTurnOrder(new List<ICombatant> { hero, enemy });

            var heroRes = turnManager.CurrentResources;
            Assert.Equal(0, heroRes.ActionsRemaining);
            Assert.Equal(0, heroRes.BonusActionsRemaining);
            Assert.Equal(0, heroRes.MovementRemaining);
            Assert.False(heroRes.CanAct);

            // Advancing turn ticks hero's stunned condition
            turnManager.AdvanceTurn();
            Assert.False(StatusEffectSystem.HasCondition(hero, ConditionType.Stunned));
        }

        [Fact]
        public void ActionSystem_ActionsConsumeResourcesProperly()
        {
            StatusEffectSystem.ClearAll();
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));
            var enemy = new Enemy("Orc", Race.Orc, new Stats(80, 8, 3, 1), "Warrior");
            var res = new TurnResources(30);

            // Attack consumes Action
            bool attack1 = ActionSystem.PerformMeleeAttack(hero, enemy, res);
            Assert.True(attack1);
            Assert.False(res.CanAct);

            // Second attack without action fails
            bool attack2 = ActionSystem.PerformMeleeAttack(hero, enemy, res);
            Assert.False(attack2);

            // Opportunity attack consumes Reaction
            bool oppAttack = ActionSystem.PerformOpportunityAttack(hero, enemy, res);
            Assert.True(oppAttack);
            Assert.False(res.CanReact);
            Assert.False(ActionSystem.PerformOpportunityAttack(hero, enemy, res));

            // Stand up consumes half movement
            StatusEffectSystem.ApplyCondition(hero, ConditionType.Prone, 1);
            Assert.True(ActionSystem.StandUp(hero, res));
            Assert.Equal(15, res.MovementRemaining);
            Assert.False(StatusEffectSystem.HasCondition(hero, ConditionType.Prone));

            // Dash increases movement
            res.Reset();
            Assert.True(ActionSystem.PerformDash(hero, res));
            Assert.Equal(60, res.MovementRemaining);
            Assert.False(res.CanAct);
        }
    }
}
