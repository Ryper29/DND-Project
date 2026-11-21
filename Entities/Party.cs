using System;
using System.Collections.Generic;
using System.Linq;

namespace DnD.Entities
{
    // ponytail: fixed list party model; upgrade to formation marching order and camp storage when camp system is added.
    public class Party
    {
        public const int DefaultMaxMembers = 4;

        private readonly List<PlayerCharacter> _members = new();

        public int MaxMembers { get; }
        public IReadOnlyList<PlayerCharacter> Members => _members;
        public int Count => _members.Count;
        public bool IsFull => _members.Count >= MaxMembers;
        public bool IsWipedOut => _members.Count > 0 && _members.All(m => !m.IsAlive);
        public IReadOnlyList<PlayerCharacter> LivingMembers => _members.Where(m => m.IsAlive).ToList();
        public PlayerCharacter Leader => _members.FirstOrDefault();

        public Party(int maxMembers = DefaultMaxMembers)
        {
            MaxMembers = Math.Max(1, maxMembers);
        }

        public bool AddMember(PlayerCharacter character)
        {
            if (character == null || IsFull || _members.Contains(character))
                return false;

            _members.Add(character);
            return true;
        }

        public bool RemoveMember(PlayerCharacter character)
        {
            if (character == null) return false;
            return _members.Remove(character);
        }

        public void RestAll()
        {
            foreach (var member in LivingMembers)
            {
                member.FullHeal();
                member.RestoreMana(member.MaxMana);
                member.SpellSlots.RestoreAll();
            }
        }

        public void DistributeExperience(int totalXp)
        {
            if (totalXp <= 0) return;
            var living = LivingMembers;
            if (living.Count == 0) return;

            int xpPerMember = totalXp / living.Count;
            if (xpPerMember <= 0) xpPerMember = 1;

            foreach (var member in living)
                member.GainExperience(xpPerMember);
        }

        public List<ICombatant> AsCombatants() => _members.Cast<ICombatant>().ToList();
    }
}
