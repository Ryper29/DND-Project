using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Dice;

namespace DnD.Systems.Narrative
{
    public class DialogueResult
    {
        public bool Success { get; }
        public bool CheckAttempted { get; }
        public int RollValue { get; }
        public int TotalValue { get; }
        public int DC { get; }
        public DialogueNode NextNode { get; }
        public bool DialogueEnded => NextNode == null;

        public DialogueResult(bool success, bool checkAttempted, int rollValue, int totalValue, int dc, DialogueNode nextNode)
        {
            Success = success;
            CheckAttempted = checkAttempted;
            RollValue = rollValue;
            TotalValue = totalValue;
            DC = dc;
            NextNode = nextNode;
        }
    }

    // ponytail: linear node traversal without branching dialogue history rollback; upgrade to undo/save-state stack when journal system is added.
    public class DialogueEngine
    {
        private readonly GameState _gameState;
        private readonly Dictionary<string, DialogueNode> _nodes = new();

        public DialogueNode CurrentNode { get; private set; }
        public bool IsActive => CurrentNode != null;
        public IReadOnlyDictionary<string, DialogueNode> Nodes => _nodes;

        public DialogueEngine(GameState gameState = null)
        {
            _gameState = gameState ?? new GameState();
        }

        public void AddNode(DialogueNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _nodes[node.Id] = node;
        }

        public bool StartDialogue(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || !_nodes.TryGetValue(nodeId, out var node))
                return false;

            CurrentNode = node;
            CurrentNode.OnEnter?.Invoke(_gameState);
            GameEvents.RaiseCombatLog($"[{CurrentNode.SpeakerName}]: {CurrentNode.Text}");
            return true;
        }

        public IReadOnlyList<DialogueOption> GetAvailableOptions()
        {
            if (CurrentNode == null) return Array.Empty<DialogueOption>();

            return CurrentNode.Options
                .Where(opt => string.IsNullOrEmpty(opt.RequiredFlag) || _gameState.HasFlag(opt.RequiredFlag))
                .ToList();
        }

        public DialogueResult SelectOption(int optionIndex, PlayerCharacter character, IRandomSource random = null)
        {
            if (CurrentNode == null)
                return new DialogueResult(false, false, 0, 0, 0, null);

            var available = GetAvailableOptions();
            if (optionIndex < 0 || optionIndex >= available.Count)
                return new DialogueResult(false, false, 0, 0, 0, CurrentNode);

            var option = available[optionIndex];
            var rand = random ?? DiceRoller.RandomSource;

            bool checkSuccess = true;
            bool checkAttempted = false;
            int rollVal = 0;
            int totalVal = 0;
            int dc = 0;

            if (option.Check != null)
            {
                checkAttempted = true;
                dc = option.Check.DC;
                int mod = GetSkillModifier(character, option.Check.GoverningAbility);
                var roll = DiceRoller.Roll(DiceType.D20, rand);
                rollVal = roll.Value;
                totalVal = rollVal + mod;
                checkSuccess = totalVal >= dc;

                GameEvents.RaiseCombatLog($"[{option.Check.Skill} Check] Roll: {totalVal} ({rollVal} + {mod}) vs DC {dc} -> {(checkSuccess ? "SUCCESS" : "FAILED")}");
            }

            string nextTargetId = null;
            if (checkSuccess)
            {
                if (!string.IsNullOrEmpty(option.SetFlagOnSelect))
                    _gameState.SetFlag(option.SetFlagOnSelect);

                nextTargetId = option.NextNodeId;
            }
            else
            {
                nextTargetId = option.FailNodeId ?? option.NextNodeId;
            }

            if (option.IsExit || string.IsNullOrEmpty(nextTargetId))
            {
                CurrentNode = null;
                GameEvents.RaiseCombatLog("Dialogue ended.");
                return new DialogueResult(checkSuccess, checkAttempted, rollVal, totalVal, dc, null);
            }

            CurrentNode = _nodes.TryGetValue(nextTargetId, out var next) ? next : null;
            CurrentNode?.OnEnter?.Invoke(_gameState);
            if (CurrentNode != null)
                GameEvents.RaiseCombatLog($"[{CurrentNode.SpeakerName}]: {CurrentNode.Text}");

            return new DialogueResult(checkSuccess, checkAttempted, rollVal, totalVal, dc, CurrentNode);
        }

        private static int GetSkillModifier(PlayerCharacter character, AbilityScore ability)
        {
            if (character == null) return 0;
            int score = ability switch
            {
                AbilityScore.Strength => character.BaseStats.Strength,
                AbilityScore.Dexterity => character.BaseStats.Dexterity,
                AbilityScore.Constitution => character.BaseStats.Constitution,
                AbilityScore.Intelligence => character.BaseStats.Intelligence,
                AbilityScore.Wisdom => character.BaseStats.Wisdom,
                AbilityScore.Charisma => character.BaseStats.Charisma,
                _ => 10
            };
            return (score - 10) / 2;
        }
    }
}
