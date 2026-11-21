using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DnD.Entities;
using DnD.Data;
using DnD.Systems.Inventory;

namespace DnD.Core
{
    public sealed class GameSave
    {
        public string Name { get; set; }
        public Race Race { get; set; }
        public CharacterClass CharacterClass { get; set; }
        public int[] AbilityScores { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int CurrentHealth { get; set; }
        public int CurrentMana { get; set; }
        public string Weapon { get; set; }
        public string Armor { get; set; }
        public int ExplorationStep { get; set; }
        public bool QuestComplete { get; set; }
        public ConsumableSave[] Consumables { get; set; } = Array.Empty<ConsumableSave>();
    }

    public sealed class ConsumableSave
    {
        public string Name { get; set; }
        public ConsumableEffect Effect { get; set; }
        public int Amount { get; set; }
    }

    public static class SaveSystem
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

        public static void Save(string path, GameSave save)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Save path is required.", nameof(path));
            if (save == null) throw new ArgumentNullException(nameof(save));
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var temporaryPath = fullPath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(save, Options));
                File.Move(temporaryPath, fullPath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        public static GameSave Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Save path is required.", nameof(path));
            var save = JsonSerializer.Deserialize<GameSave>(File.ReadAllText(path));
            if (save == null || string.IsNullOrWhiteSpace(save.Name) || save.AbilityScores == null || save.AbilityScores.Length != 6 ||
                !Enum.IsDefined(typeof(Race), save.Race) || !Enum.IsDefined(typeof(CharacterClass), save.CharacterClass) ||
                save.Level < 1 || save.Level > 100 || save.Experience < 0 || save.Experience >= save.Level * 50 || save.CurrentHealth < 0 || save.CurrentMana < 0 || save.ExplorationStep < 0 || save.ExplorationStep > 3 ||
                save.AbilityScores.Any(score => score < 1 || score > 30) || (save.Consumables != null && save.Consumables.Length > 100) ||
                (save.Weapon != null && EquipmentDatabase.GetWeapon(save.Weapon) == null) ||
                (save.Armor != null && EquipmentDatabase.GetArmor(save.Armor) == null))
                throw new InvalidDataException("Save file has invalid or incomplete character data.");
            return save;
        }

        public static GameSave FromCharacter(PlayerCharacter character, int explorationStep, bool questComplete)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            var stats = character.BaseStats;
            return new GameSave
            {
                Name = character.Name,
                Race = character.Race,
                CharacterClass = character.CharacterClass,
                AbilityScores = RemoveRaceBonuses(character.Race, stats),
                Level = character.Level,
                Experience = character.Experience,
                CurrentHealth = character.CurrentHealth,
                CurrentMana = character.CurrentMana,
                Weapon = character.EquippedWeapon?.Name,
                Armor = character.EquippedArmor?.Name,
                ExplorationStep = explorationStep,
                QuestComplete = questComplete,
                Consumables = System.Linq.Enumerable.Select(character.Inventory.Consumables, item => new ConsumableSave
                {
                    Name = item.Name,
                    Effect = item.Effect,
                    Amount = item.Amount
                }).ToArray()
            };
        }

        private static int[] RemoveRaceBonuses(Race race, Stats stats)
        {
            var scores = new[] { stats.Strength, stats.Dexterity, stats.Constitution, stats.Intelligence, stats.Wisdom, stats.Charisma };
            switch (race)
            {
                case Race.Human: for (int i = 0; i < scores.Length; i++) scores[i]--; break;
                case Race.Elf:
                case Race.Halfling: scores[1] -= 2; break;
                case Race.Dwarf: scores[2] -= 2; break;
                case Race.SemiDragon:
                case Race.Orc: scores[0] -= 2; break;
                case Race.Goblin: scores[1]--; break;
            }
            return scores;
        }

        public static PlayerCharacter ToCharacter(GameSave save)
        {
            if (save == null || save.AbilityScores == null || save.AbilityScores.Length != 6)
                throw new InvalidDataException("Save data is invalid.");
            var stats = new Stats(save.AbilityScores[0], save.AbilityScores[1], save.AbilityScores[2],
                save.AbilityScores[3], save.AbilityScores[4], save.AbilityScores[5],
                save.CharacterClass == CharacterClass.Fighter ? 120 : 90,
                save.CharacterClass == CharacterClass.Fighter ? 12 : 8,
                10, save.CharacterClass == CharacterClass.Wizard || save.CharacterClass == CharacterClass.Cleric ? 30 : 10,
                20, 5, 5);
            var character = new PlayerCharacter(save.Name, save.Race, save.CharacterClass, stats);
            character.EquipWeapon(EquipmentDatabase.GetWeapon(save.Weapon));
            character.EquipArmor(EquipmentDatabase.GetArmor(save.Armor));
            character.RestoreProgress(save.Level, save.Experience, save.CurrentHealth);
            character.SetCurrentMana(save.CurrentMana);
            foreach (var item in save.Consumables ?? Array.Empty<ConsumableSave>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name) || item.Amount <= 0 ||
                    !Enum.IsDefined(typeof(ConsumableEffect), item.Effect))
                    throw new InvalidDataException("Save file contains invalid inventory data.");
                character.Inventory.AddConsumable(new ConsumableData(item.Name, item.Effect, item.Amount));
            }
            return character;
        }
    }
}