using DnD.Entities;

namespace DnD.Data
{
    /// <summary>
    /// Data container for character presets.
    /// In Unity, convert to ScriptableObject: inherit from ScriptableObject, add [CreateAssetMenu].
    /// </summary>
    [System.Serializable]
    public class CharacterTemplate
    {
        public string TemplateName;
        public CharacterClass Class;
        public Race DefaultRace;
        public Stats BaseStats;
        public string DefaultCombatStyle;

        public static CharacterTemplate Fighter() => new CharacterTemplate
        {
            TemplateName = "Fighter",
            Class = CharacterClass.Fighter,
            DefaultRace = Race.Human,
            BaseStats = new Stats(120, 12, 25, 6),
            DefaultCombatStyle = "Swordsman"
        };

        public static CharacterTemplate Wizard() => new CharacterTemplate
        {
            TemplateName = "Wizard",
            Class = CharacterClass.Wizard,
            DefaultRace = Race.Elf,
            BaseStats = new Stats(80, 8, 15, 4, 30, 10),
            DefaultCombatStyle = ""
        };

        public static CharacterTemplate Brute() => new CharacterTemplate
        {
            TemplateName = "Brute",
            Class = CharacterClass.Fighter,
            DefaultRace = Race.Orc,
            BaseStats = new Stats(200, 15, 30, 5),
            DefaultCombatStyle = "Brawler"
        };
    }
}
