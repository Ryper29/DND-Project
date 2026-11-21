using System;
using System.Collections.Generic;
using DnD.Core;
using DnD.Entities;

namespace DnD.Systems.Narrative
{
    public enum SkillType
    {
        None = 0,
        Persuasion,
        Deception,
        Intimidation,
        Insight,
        Investigation,
        Athletics,
        Stealth
    }

    public class SkillCheck
    {
        public SkillType Skill { get; }
        public int DC { get; }

        public AbilityScore GoverningAbility => Skill switch
        {
            SkillType.Persuasion => AbilityScore.Charisma,
            SkillType.Deception => AbilityScore.Charisma,
            SkillType.Intimidation => AbilityScore.Charisma,
            SkillType.Insight => AbilityScore.Wisdom,
            SkillType.Investigation => AbilityScore.Intelligence,
            SkillType.Athletics => AbilityScore.Strength,
            SkillType.Stealth => AbilityScore.Dexterity,
            _ => AbilityScore.Charisma
        };

        public SkillCheck(SkillType skill, int dc)
        {
            Skill = skill;
            DC = Math.Max(1, dc);
        }

        public override string ToString() => $"[{Skill} DC {DC}]";
    }

    public class DialogueOption
    {
        public string Text { get; }
        public string NextNodeId { get; }
        public string FailNodeId { get; }
        public SkillCheck Check { get; }
        public string RequiredFlag { get; }
        public string SetFlagOnSelect { get; }
        public bool IsExit { get; }

        public DialogueOption(string text, string nextNodeId = null, string failNodeId = null,
                              SkillCheck check = null, string requiredFlag = null,
                              string setFlagOnSelect = null, bool isExit = false)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            NextNodeId = nextNodeId;
            FailNodeId = failNodeId;
            Check = check;
            RequiredFlag = requiredFlag;
            SetFlagOnSelect = setFlagOnSelect;
            IsExit = isExit;
        }

        public string GetDisplayText()
        {
            return Check != null ? $"{Check} {Text}" : Text;
        }
    }

    // ponytail: memory-graph node model without external localization JSON loader; upgrade to JSON/Ink dialogue importer when dialogue assets grow.
    public class DialogueNode
    {
        public string Id { get; }
        public string SpeakerName { get; }
        public string Text { get; }
        public List<DialogueOption> Options { get; } = new List<DialogueOption>();
        public Action<GameState> OnEnter { get; set; }

        public DialogueNode(string id, string speakerName, string text, IEnumerable<DialogueOption> options = null, Action<GameState> onEnter = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SpeakerName = speakerName ?? "Narrator";
            Text = text ?? "";
            if (options != null) Options.AddRange(options);
            OnEnter = onEnter;
        }
    }
}
