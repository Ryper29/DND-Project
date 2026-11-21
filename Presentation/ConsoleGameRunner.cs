using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Combat;
using DnD.Systems.Inventory;
using DnD.Data;

namespace DnD.Presentation
{
    /// <summary>
    /// Entry point for playing DnD in console mode.
    /// Demonstrates the full game loop: character creation, equipping, and combat.
    /// </summary>
    public static class ConsoleGameRunner
    {
        public static void Main(string[] args)
        {
            using var renderer = new ConsoleUIRenderer();

            PrintBanner();

            PlayerCharacter hero;
            int step = 0;
            bool questComplete = false;
            bool loadedGame = false;
            const string savePath = "dnd-save.json";
            if (File.Exists(savePath))
            {
                Console.WriteLine("1. New game\n2. Load saved game");
                if (ReadOption(2) == 2)
                {
                    var save = SaveSystem.Load(savePath);
                    hero = SaveSystem.ToCharacter(save);
                    loadedGame = true;
                    step = save.ExplorationStep;
                    questComplete = save.QuestComplete;
                }
                else hero = CreateCharacter();
            }
            else hero = CreateCharacter();

            if (hero.EquippedWeapon == null)
                hero.EquipWeapon(EquipmentDatabase.GetWeapon(hero.CharacterClass == CharacterClass.Wizard ? "Wooden Staff" : "Iron Sword"));
            if (hero.EquippedArmor == null)
                hero.EquipArmor(EquipmentDatabase.GetArmor(hero.CharacterClass == CharacterClass.Wizard ? "Cloth Robe" : "Light Armor"));
            if (!loadedGame)
                hero.Inventory.AddConsumable(new ConsumableData("Healing Potion", ConsumableEffect.Heal, 30));
            RunAdventure(hero, step, questComplete, savePath);
        }

private static void RunAdventure(PlayerCharacter hero, int step, bool questComplete, string savePath)
        {
            Console.WriteLine($"\nWelcome, {hero.Name}. A village elder asks you to defeat the Goblin King.");
            while (!questComplete && hero.IsAlive)
            {
                Console.WriteLine($"\n=== FOREST ROAD (step {step}) ===");
                Console.WriteLine("1. Explore north");
                Console.WriteLine("2. Rest (restore HP and mana)");
                Console.WriteLine("3. View character and inventory");
                Console.WriteLine("4. Save and quit");
                int choice = ReadOption(4);
                if (choice == 1)
                {
                    step++;
                    if (step >= 3)
                    {
                        var boss = new Enemy("Goblin King", Race.Goblin, new Stats(120, 8, 15, 3), "Club Fighter", 100);
                        boss.EquipWeapon(EquipmentDatabase.GetWeapon("Giant Spiked Club"));
                        boss.EquipArmor(EquipmentDatabase.GetArmor("Hardened Skin"));
                        Console.WriteLine("The Goblin King attacks!");
                        new CombatResolver(new GameManager()).RunTurnBasedCombat(
                            new List<ICombatant> { hero }, new List<ICombatant> { boss });
                        questComplete = !hero.IsAlive ? false : !boss.IsAlive;
                        if (questComplete)
                            Console.WriteLine("Quest complete: the village is safe.");
                        else if (hero.IsAlive) step = 2;
                    }
                    else
                    {
                        var encounter = new Enemy("Forest Goblin", Race.Goblin, new Stats(45, 6, 5, 2), "Scout", 20);
                        Console.WriteLine("A forest goblin blocks the road!");
                        new CombatResolver(new GameManager()).RunTurnBasedCombat(
                            new List<ICombatant> { hero }, new List<ICombatant> { encounter });
                        if (hero.IsAlive && !encounter.IsAlive)
                        {
                            hero.Inventory.AddConsumable(new ConsumableData("Healing Potion", ConsumableEffect.Heal, 30));
                            Console.WriteLine("You found a Healing Potion.");
                        }
                        else if (hero.IsAlive) step--;
                    }
                }
                else if (choice == 2)
                {
                    hero.FullHeal();
                    hero.RestoreMana(hero.MaxMana);
                    Console.WriteLine("You rest; health and mana restored.");
                }
                else if (choice == 3)
                {
                    ConsoleUIRenderer.PrintCharacterStats(hero);
                    PrintInventory(hero);
                }
                else
                {
                    SaveSystem.Save(savePath, SaveSystem.FromCharacter(hero, step, questComplete));
                    Console.WriteLine($"Game saved to {Path.GetFullPath(savePath)}");
                    return;
                }
                SaveSystem.Save(savePath, SaveSystem.FromCharacter(hero, step, questComplete));
            }
            Console.WriteLine(questComplete ? "Adventure complete!" : "Your adventure has ended.");
            if (File.Exists(savePath)) File.Delete(savePath);
        }

        private static PlayerCharacter CreateCharacter()
        {
            Console.WriteLine("\n=== CHARACTER CREATION ===");
            Console.Write("Character name: ");
            var name = Console.ReadLine()?.Trim();
            while (string.IsNullOrWhiteSpace(name))
            {
                Console.Write("Name cannot be empty. Enter name: ");
                name = Console.ReadLine()?.Trim();
            }

            var races = (Race[])Enum.GetValues(typeof(Race));
            Console.WriteLine("Choose race:");
            for (int i = 0; i < races.Length; i++) Console.WriteLine($"{i + 1}. {races[i]}");
            var race = races[ReadOption(races.Length) - 1];

            var classes = (CharacterClass[])Enum.GetValues(typeof(CharacterClass));
            Console.WriteLine("Choose class:");
            for (int i = 0; i < classes.Length; i++) Console.WriteLine($"{i + 1}. {classes[i]}");
            var characterClass = classes[ReadOption(classes.Length) - 1];

            Console.WriteLine("Assign scores 15, 14, 13, 12, 10, 8 to STR/DEX/CON/INT/WIS/CHA.");
            int[] available = { 15, 14, 13, 12, 10, 8 };
            string[] abilities = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
            int[] scores = new int[6];
            for (int i = 0; i < abilities.Length; i++)
            {
                Console.WriteLine($"Available scores: {string.Join(", ", available.Where(score => score > 0))}");
                Console.Write($"Choose score for {abilities[i]}: ");
                while (!int.TryParse(Console.ReadLine(), out scores[i]) || Array.IndexOf(available, scores[i]) < 0)
                {
                    Console.WriteLine("Choose one of the remaining scores.");
                    Console.Write($"Choose score for {abilities[i]}: ");
                }
                int chosen = Array.IndexOf(available, scores[i]);
                available[chosen] = -1;
            }

            var stats = new Stats(scores[0], scores[1], scores[2], scores[3], scores[4], scores[5],
                characterClass == CharacterClass.Fighter ? 120 : 90,
                characterClass == CharacterClass.Fighter ? 12 : 8,
                10, characterClass == CharacterClass.Wizard || characterClass == CharacterClass.Cleric ? 30 : 10,
                20, 5, 5);

            var character = new PlayerCharacter(name, race, characterClass, stats);
            Console.WriteLine($"\n{name} — {race} {characterClass} (racial bonuses included)");
            Console.WriteLine($"STR {character.BaseStats.Strength}  DEX {character.BaseStats.Dexterity}  CON {character.BaseStats.Constitution}");
            Console.WriteLine($"INT {character.BaseStats.Intelligence}  WIS {character.BaseStats.Wisdom}  CHA {character.BaseStats.Charisma}");
            return character;
        }

        private static int ReadOption(int maximum)
        {
            while (true)
            {
                Console.Write($"Select 1-{maximum}: ");
                if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= maximum)
                    return choice;
                Console.WriteLine("Invalid choice.");
            }
        }

        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(@"
   ╔══════════════════════════════════════════════════╗
   ║                                                  ║
   ║    ███╗   ███╗██╗   ██╗████████╗██╗  ██╗         ║
   ║    ████╗ ████║╚██╗ ██╔╝╚══██╔══╝██║  ██║         ║
   ║    ██╔████╔██║ ╚████╔╝    ██║   ███████║         ║
   ║    ██║╚██╔╝██║  ╚██╔╝     ██║   ██╔══██║         ║
   ║    ██║ ╚═╝ ██║   ██║      ██║   ██║  ██║         ║
   ║    ╚═╝     ╚═╝   ╚═╝      ╚═╝   ╚═╝  ╚═╝         ║
   ║              W E A V E R                          ║
   ║         A DnD-Inspired Combat Engine             ║
   ║                                                  ║
   ╚══════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        private static void PrintInventory(ICombatant character)
        {
            Console.WriteLine($"\n=== INVENTORY {character.Name} ===");
            Console.WriteLine($"HP: {character.CurrentHealth}/{character.MaxHealth}");
            if (character is BaseEntity entity)
            {
                Console.WriteLine($"Weapon: {entity.EquippedWeapon?.Name ?? "Fists"}");
                Console.WriteLine($"Armor: {entity.EquippedArmor?.Name ?? "None"}");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
