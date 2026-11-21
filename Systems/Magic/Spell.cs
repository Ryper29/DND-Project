using System;
using System.Collections.Generic;
using DnD.Entities;

namespace DnD.Systems.Magic
{
    public enum SpellType { Attack, SavingThrow, Healing, Buff }

    // ponytail: fixed spell struct without upcasting damage scaling; upgrade to upcasting slots when spell heighten system is added.
    public class Spell
    {
        public string Name { get; }
        public int Level { get; } // 0 = Cantrip, 1-9 = Leveled
        public SpellType Type { get; }
        public DamageType DamageType { get; }
        public int BaseValue { get; }
        public int RangeFeet { get; }
        public int AoERadiusFeet { get; }
        public AbilityScore SaveAbility { get; }
        public bool RequiresConcentration { get; }
        public ConditionType AppliedCondition { get; }
        public int ConditionDurationRounds { get; }

        public bool IsCantrip => Level == 0;
        public bool IsAoE => AoERadiusFeet > 0;

        public Spell(string name, int level, SpellType type, int baseValue,
                     int rangeFeet = 60, int aoeRadiusFeet = 0,
                     DamageType damageType = DamageType.Magical,
                     AbilityScore saveAbility = AbilityScore.None,
                     bool requiresConcentration = false,
                     ConditionType appliedCondition = ConditionType.None,
                     int conditionDurationRounds = 0)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Level = Math.Clamp(level, 0, 9);
            Type = type;
            BaseValue = baseValue;
            RangeFeet = rangeFeet;
            AoERadiusFeet = aoeRadiusFeet;
            DamageType = damageType;
            SaveAbility = saveAbility;
            RequiresConcentration = requiresConcentration;
            AppliedCondition = appliedCondition;
            ConditionDurationRounds = conditionDurationRounds;
        }
    }

    // ponytail: standard full-caster progression table; upgrade to multiclass pact-magic parser when warlock is added.
    public class SpellSlotManager
    {
        private readonly Dictionary<int, int> _maxSlots = new();
        private readonly Dictionary<int, int> _currentSlots = new();

        public int CasterLevel { get; private set; }

        public SpellSlotManager(int casterLevel = 1)
        {
            SetCasterLevel(casterLevel);
        }

        public void SetCasterLevel(int level)
        {
            CasterLevel = Math.Max(1, level);
            _maxSlots.Clear();
            _currentSlots.Clear();

            int slot1 = CasterLevel >= 3 ? 4 : (CasterLevel == 2 ? 3 : 2);
            _maxSlots[1] = slot1;
            _currentSlots[1] = slot1;

            if (CasterLevel >= 3)
            {
                int slot2 = CasterLevel >= 4 ? 3 : 2;
                _maxSlots[2] = slot2;
                _currentSlots[2] = slot2;
            }

            if (CasterLevel >= 5)
            {
                _maxSlots[3] = 2;
                _currentSlots[3] = 2;
            }
        }

        public int GetMaxSlots(int spellLevel) => _maxSlots.TryGetValue(spellLevel, out int max) ? max : 0;
        public int GetCurrentSlots(int spellLevel) => _currentSlots.TryGetValue(spellLevel, out int cur) ? cur : 0;

        public bool HasSlot(int spellLevel)
        {
            if (spellLevel <= 0) return true; // Cantrip
            return GetCurrentSlots(spellLevel) > 0;
        }

        public bool SpendSlot(int spellLevel)
        {
            if (spellLevel <= 0) return true; // Cantrips cost no slot
            if (!HasSlot(spellLevel)) return false;
            _currentSlots[spellLevel]--;
            return true;
        }

        public void RestoreSlot(int spellLevel, int amount = 1)
        {
            if (spellLevel <= 0 || amount <= 0) return;
            if (_maxSlots.TryGetValue(spellLevel, out int max))
            {
                int cur = _currentSlots.TryGetValue(spellLevel, out int c) ? c : 0;
                _currentSlots[spellLevel] = Math.Min(max, cur + amount);
            }
        }

        public void RestoreAll()
        {
            foreach (var kvp in _maxSlots)
                _currentSlots[kvp.Key] = kvp.Value;
        }
    }

    public static class SpellDatabase
    {
        public static readonly Spell FireBolt = new Spell(
            "Fire Bolt", level: 0, SpellType.Attack, baseValue: 10, rangeFeet: 60, damageType: DamageType.Fire);

        public static readonly Spell SacredFlame = new Spell(
            "Sacred Flame", level: 0, SpellType.SavingThrow, baseValue: 8, rangeFeet: 60,
            damageType: DamageType.Magical, saveAbility: AbilityScore.Dexterity);

        public static readonly Spell CureWounds = new Spell(
            "Cure Wounds", level: 1, SpellType.Healing, baseValue: 15, rangeFeet: 5);

        public static readonly Spell Bless = new Spell(
            "Bless", level: 1, SpellType.Buff, baseValue: 0, rangeFeet: 30,
            requiresConcentration: true, appliedCondition: ConditionType.Blessed, conditionDurationRounds: 3);

        public static readonly Spell Fireball = new Spell(
            "Fireball", level: 3, SpellType.SavingThrow, baseValue: 28, rangeFeet: 60, aoeRadiusFeet: 15,
            damageType: DamageType.Fire, saveAbility: AbilityScore.Dexterity);

        public static readonly Spell HoldPerson = new Spell(
            "Hold Person", level: 2, SpellType.SavingThrow, baseValue: 0, rangeFeet: 60,
            saveAbility: AbilityScore.Wisdom, requiresConcentration: true,
            appliedCondition: ConditionType.Stunned, conditionDurationRounds: 2);
    }
}
