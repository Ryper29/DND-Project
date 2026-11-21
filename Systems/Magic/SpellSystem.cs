using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Combat;
using DnD.Systems.Dice;

namespace DnD.Systems.Magic
{
    // ponytail: simplified spell resolution without line-of-sight raycasting; upgrade to Bresenham raycast when walls/cover are added.
    public static class SpellSystem
    {
        public static int CalculateSpellSaveDC(PlayerCharacter caster)
        {
            if (caster == null) return 10;
            int prof = 2 + (caster.Level - 1) / 4;
            int abilityMod = caster.CharacterClass switch
            {
                CharacterClass.Wizard => (caster.BaseStats.Intelligence - 10) / 2,
                CharacterClass.Cleric => (caster.BaseStats.Wisdom - 10) / 2,
                _ => (caster.BaseStats.Charisma - 10) / 2
            };
            return 8 + prof + abilityMod;
        }

        public static bool RollSavingThrow(ICombatant target, AbilityScore ability, int dc, IRandomSource random = null)
        {
            if (target == null) return false;
            var rand = random ?? DiceRoller.RandomSource;

            int mod = 0;
            if (target is BaseEntity b)
            {
                mod = ability switch
                {
                    AbilityScore.Strength => (b.BaseStats.Strength - 10) / 2,
                    AbilityScore.Dexterity => (b.BaseStats.Dexterity - 10) / 2,
                    AbilityScore.Constitution => (b.BaseStats.Constitution - 10) / 2,
                    AbilityScore.Intelligence => (b.BaseStats.Intelligence - 10) / 2,
                    AbilityScore.Wisdom => (b.BaseStats.Wisdom - 10) / 2,
                    AbilityScore.Charisma => (b.BaseStats.Charisma - 10) / 2,
                    _ => 0
                };
            }

            var roll = DiceRoller.Roll(DiceType.D20, rand);
            int total = roll.Value + mod;
            bool success = total >= dc;
            GameEvents.RaiseCombatLog($"{target.Name} rolled {ability} Save: {total} ({roll.Value} + {mod}) vs DC {dc} -> {(success ? "SUCCESS" : "FAILED")}");
            return success;
        }

        public static bool CheckConcentration(PlayerCharacter caster, int damageTaken, IRandomSource random = null)
        {
            if (caster == null || caster.ActiveConcentrationSpell == null) return true;
            int dc = Math.Max(10, damageTaken / 2);
            bool passed = RollSavingThrow(caster, AbilityScore.Constitution, dc, random);
            if (!passed)
                BreakConcentration(caster);

            return passed;
        }

        public static void BreakConcentration(PlayerCharacter caster)
        {
            if (caster?.ActiveConcentrationSpell == null) return;
            var spell = caster.ActiveConcentrationSpell;
            caster.ActiveConcentrationSpell = null;

            if (caster.ConcentrationTarget != null && spell.AppliedCondition != ConditionType.None)
                StatusEffectSystem.RemoveCondition(caster.ConcentrationTarget, spell.AppliedCondition);

            caster.ConcentrationTarget = null;
            GameEvents.RaiseCombatLog($"{caster.Name} lost concentration on {spell.Name}!");
        }

        public static bool CastSpell(PlayerCharacter caster, Spell spell, int targetX = 0, int targetY = 0,
                                     BattleGrid grid = null, ICombatant singleTarget = null,
                                     TurnResources resources = null, IRandomSource random = null)
        {
            if (caster == null || spell == null || !caster.IsAlive) return false;

            if (StatusEffectSystem.HasCondition(caster, ConditionType.Stunned))
            {
                GameEvents.RaiseCombatLog($"{caster.Name} is stunned and cannot cast spells!");
                return false;
            }

            if (resources != null && !resources.UseAction())
            {
                GameEvents.RaiseCombatLog($"{caster.Name} has no action remaining to cast a spell.");
                return false;
            }

            if (!caster.SpellSlots.SpendSlot(spell.Level))
            {
                GameEvents.RaiseCombatLog($"{caster.Name} has no Level {spell.Level} spell slots available!");
                return false;
            }

            var rand = random ?? DiceRoller.RandomSource;
            int dc = CalculateSpellSaveDC(caster);

            // Range check with grid
            if (grid != null)
            {
                var casterPos = grid.GetPosition(caster);
                if (casterPos.HasValue)
                {
                    int dist = BattleGrid.CalculateDistance(casterPos.Value.X, casterPos.Value.Y, targetX, targetY);
                    if (dist > spell.RangeFeet)
                    {
                        GameEvents.RaiseCombatLog($"Target is out of range ({dist}ft > {spell.RangeFeet}ft)!");
                        return false;
                    }
                }
            }

            // Concentration handling
            if (spell.RequiresConcentration)
            {
                if (caster.ActiveConcentrationSpell != null)
                    BreakConcentration(caster);

                caster.ActiveConcentrationSpell = spell;
            }

            // AoE Resolution
            if (spell.IsAoE && grid != null)
            {
                GameEvents.RaiseCombatLog($"{caster.Name} casts {spell.Name} at ({targetX}, {targetY})!");
                for (int x = 0; x < grid.Width; x++)
                {
                    for (int y = 0; y < grid.Height; y++)
                    {
                        var entity = grid.GetEntityAt(x, y);
                        if (entity != null && entity.IsAlive)
                        {
                            int dist = BattleGrid.CalculateDistance(targetX, targetY, x, y);
                            if (dist <= spell.AoERadiusFeet)
                            {
                                int damage = spell.BaseValue;
                                if (spell.SaveAbility != AbilityScore.None)
                                {
                                    bool saved = RollSavingThrow(entity, spell.SaveAbility, dc, rand);
                                    if (saved) damage /= 2;
                                    else if (spell.AppliedCondition != ConditionType.None)
                                        StatusEffectSystem.ApplyCondition(entity, spell.AppliedCondition, spell.ConditionDurationRounds);
                                }
                                GameEvents.RaiseCombatLog($"{spell.Name} hits {entity.Name} for {damage} damage.");
                                GameEvents.RaiseAttackPerformed(caster, entity, damage);
                                entity.TakeDamage(damage);
                            }
                        }
                    }
                }
                return true;
            }

            // Single Target Resolution
            var target = singleTarget ?? (grid != null ? grid.GetEntityAt(targetX, targetY) : null);
            if (target == null || !target.IsAlive)
            {
                GameEvents.RaiseCombatLog("No valid target found.");
                return false;
            }

            if (spell.Type == SpellType.Healing)
            {
                if (target is BaseEntity b)
                    b.Heal(spell.BaseValue);
                GameEvents.RaiseCombatLog($"{caster.Name} casts {spell.Name} on {target.Name}, restoring {spell.BaseValue} HP.");
                return true;
            }

            if (spell.Type == SpellType.Buff)
            {
                if (spell.AppliedCondition != ConditionType.None)
                {
                    StatusEffectSystem.ApplyCondition(target, spell.AppliedCondition, spell.ConditionDurationRounds);
                    if (spell.RequiresConcentration)
                        caster.ConcentrationTarget = target;
                }
                GameEvents.RaiseCombatLog($"{caster.Name} casts {spell.Name} on {target.Name}.");
                return true;
            }

            if (spell.Type == SpellType.SavingThrow)
            {
                bool saved = RollSavingThrow(target, spell.SaveAbility, dc, rand);
                int damage = saved ? spell.BaseValue / 2 : spell.BaseValue;
                if (!saved && spell.AppliedCondition != ConditionType.None)
                {
                    StatusEffectSystem.ApplyCondition(target, spell.AppliedCondition, spell.ConditionDurationRounds);
                    if (spell.RequiresConcentration)
                        caster.ConcentrationTarget = target;
                }
                if (damage > 0)
                {
                    GameEvents.RaiseAttackPerformed(caster, target, damage);
                    target.TakeDamage(damage);
                }
                return true;
            }

            // Attack roll spell (e.g. Fire Bolt)
            var attackRoll = DiceRoller.Roll(DiceType.D20, rand);
            GameEvents.RaiseDiceRolled(caster.Name, attackRoll);

            if (attackRoll.IsCriticalFail || attackRoll.Value + ((caster.BaseStats.Intelligence - 10) / 2) < target.DefensePower)
            {
                GameEvents.RaiseCombatLog($"{caster.Name}'s {spell.Name} misses {target.Name}!");
                return true;
            }

            int spellDamage = spell.BaseValue + (attackRoll.IsCriticalHit ? spell.BaseValue : 0);
            GameEvents.RaiseAttackPerformed(caster, target, spellDamage);
            target.TakeDamage(spellDamage);
            GameEvents.RaiseCombatLog($"{caster.Name}'s {spell.Name} hits {target.Name} for {spellDamage} damage!");
            return true;
        }
    }
}
