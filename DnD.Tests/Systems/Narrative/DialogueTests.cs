using System.Collections.Generic;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Dice;
using DnD.Systems.Narrative;
using Xunit;

namespace DnD.Tests.Systems.Narrative
{
    public class DialogueTests
    {
        [Fact]
        public void Dialogue_BranchesAndExitsCorrectly()
        {
            var gameState = new GameState();
            var engine = new DialogueEngine(gameState);

            var root = new DialogueNode("start", "Guard", "Halt! Who goes there?", new[]
            {
                new DialogueOption("I come in peace.", "peace"),
                new DialogueOption("None of your business! (Attack)", "hostile"),
                new DialogueOption("Goodbye.", isExit: true)
            });

            var peaceNode = new DialogueNode("peace", "Guard", "Welcome to our camp, traveler.", new[]
            {
                new DialogueOption("Thank you.", isExit: true)
            });

            var hostileNode = new DialogueNode("hostile", "Guard", "Draw your weapon!", onEnter: state => { });

            engine.AddNode(root);
            engine.AddNode(peaceNode);
            engine.AddNode(hostileNode);

            // Start dialogue
            Assert.True(engine.StartDialogue("start"));
            Assert.Equal("start", engine.CurrentNode.Id);

            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats());

            // Choose "I come in peace." (index 0)
            var result = engine.SelectOption(0, hero);
            Assert.True(result.Success);
            Assert.Equal("peace", engine.CurrentNode.Id);

            // Exit
            var exitResult = engine.SelectOption(0, hero);
            Assert.True(exitResult.DialogueEnded);
            Assert.False(engine.IsActive);
        }

        [Fact]
        public void Dialogue_SkillCheckPassAndFailBranches()
        {
            var gameState = new GameState();
            var engine = new DialogueEngine(gameState);

            var checkOption = new DialogueOption(
                text: "You wouldn't want any trouble with me.",
                nextNodeId: "success_node",
                failNodeId: "fail_node",
                check: new SkillCheck(SkillType.Intimidation, dc: 15)
            );

            var root = new DialogueNode("root", "Bandit", "Empty your pockets!", new[] { checkOption });
            var successNode = new DialogueNode("success_node", "Bandit", "Okay, okay! Just don't hurt me!");
            var failNode = new DialogueNode("fail_node", "Bandit", "Ha! You don't scare me!");

            engine.AddNode(root);
            engine.AddNode(successNode);
            engine.AddNode(failNode);

            // Hero with CHA 10 (mod 0)
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats { Charisma = 10 });

            // 1. Failed check: roll 10 + 0 = 10 < DC 15 -> branches to fail_node
            engine.StartDialogue("root");
            var failRoll = new FixedRollSource(10);
            var failResult = engine.SelectOption(0, hero, failRoll);

            Assert.False(failResult.Success);
            Assert.True(failResult.CheckAttempted);
            Assert.Equal("fail_node", engine.CurrentNode.Id);

            // 2. Successful check: roll 16 + 0 = 16 >= DC 15 -> branches to success_node
            engine.StartDialogue("root");
            var passRoll = new FixedRollSource(16);
            var passResult = engine.SelectOption(0, hero, passRoll);

            Assert.True(passResult.Success);
            Assert.True(passResult.CheckAttempted);
            Assert.Equal("success_node", engine.CurrentNode.Id);
        }

        [Fact]
        public void Dialogue_RequiresAndSetsStoryFlags()
        {
            var gameState = new GameState();
            var engine = new DialogueEngine(gameState);

            var opt1 = new DialogueOption("Here is 50 gold.", nextNodeId: "bribed", setFlagOnSelect: "guard_bribed");
            var secretOpt = new DialogueOption("I know about the secret passage.", nextNodeId: "secret", requiredFlag: "knows_passage");

            var root = new DialogueNode("root", "Guard", "Passage is closed.", new[] { opt1, secretOpt });
            var bribedNode = new DialogueNode("bribed", "Guard", "Pass quickly.");
            var secretNode = new DialogueNode("secret", "Guard", "How did you find out?");

            engine.AddNode(root);
            engine.AddNode(bribedNode);
            engine.AddNode(secretNode);

            engine.StartDialogue("root");

            // secretOpt is hidden because "knows_passage" is not set
            var available = engine.GetAvailableOptions();
            Assert.Single(available);
            Assert.Equal("Here is 50 gold.", available[0].Text);

            // Give gold -> sets "guard_bribed" flag
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats());
            engine.SelectOption(0, hero);
            Assert.True(gameState.HasFlag("guard_bribed"));

            // If flag "knows_passage" is acquired:
            gameState.SetFlag("knows_passage");
            engine.StartDialogue("root");
            Assert.Equal(2, engine.GetAvailableOptions().Count);
        }

        private sealed class FixedRollSource : IRandomSource
        {
            private readonly int _val;
            public FixedRollSource(int val) => _val = val;
            public int Next(int min, int max) => _val;
        }
    }
}
