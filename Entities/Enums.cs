namespace DnD.Entities
{
    public enum CharacterClass { Fighter, Wizard, Rogue, Cleric }
    public enum Race { Human, Elf, Dwarf, Halfling, SemiDragon, Goblin, Orc }
    public enum DamageType { Physical, Magical, Fire, Ice, Lightning }
    public enum EntityType { Player, Enemy, NPC }
    public enum ConditionType { None, Prone, Stunned, Poisoned, Blinded, Blessed }
    public enum AdvantageState { Normal, Advantage, Disadvantage }
    public enum AbilityScore { None = 0, Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma }
}
