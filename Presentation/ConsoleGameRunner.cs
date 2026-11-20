using System;
using System.Collections.Generic;
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

            var hero = CreateCharacter();
            hero.EquipWeapon(EquipmentDatabase.GetWeapon(
                hero.CharacterClass == CharacterClass.Wizard ? "Wooden Staff" : "Iron Sword"));
            hero.EquipArmor(EquipmentDatabase.GetArmor(
                hero.CharacterClass == CharacterClass.Wizard ? "Cloth Robe" : "Light Armor"));

            var goblinKing = new Enemy("Goblin King", Race.Goblin,
                new Stats(120, 8, 15, 3), combatStyle: "Club Fighter", experienceReward: 100);
            goblinKing.EquipWeapon(EquipmentDatabase.GetWeapon("Giant Spiked Club"));
            goblinKing.EquipArmor(EquipmentDatabase.GetArmor("Hardened Skin"));

            var party = new List<ICombatant> { hero };
            var enemies = new List<ICombatant> { goblinKing };
            Console.WriteLine("\n=== YOUR CHARACTER ===");
            ConsoleUIRenderer.PrintCharacterStats(hero);
            Console.WriteLine("\nA Goblin King blocks the path!");

            var gameManager = new GameManager();
            var combatResolver = new CombatResolver(gameManager);
            combatResolver.RunTurnBasedCombat(party, enemies);

            Console.WriteLine("\n=== COMBAT SUMMARY ===");
            ConsoleUIRenderer.PrintCharacterStats(hero);
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
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

            var random = new Random();
            var scores = new int[6];
            for (int i = 0; i < scores.Length; i++)
            {
                int a = random.Next(1, 7), b = random.Next(1, 7), c = random.Next(1, 7), d = random.Next(1, 7);
                scores[i] = a + b + c + d - Math.Min(Math.Min(a, b), Math.Min(c, d));
            }

            var stats = new Stats(scores[0], scores[1], scores[2], scores[3], scores[4], scores[5],
                characterClass == CharacterClass.Fighter ? 120 : 90,
                characterClass == CharacterClass.Fighter ? 12 : 8,
                10, characterClass == CharacterClass.Wizard || characterClass == CharacterClass.Cleric ? 30 : 10,
                20, 5, 5);

            Console.WriteLine($"\n{name} — {race} {characterClass}");
            Console.WriteLine($"STR {stats.Strength}  DEX {stats.Dexterity}  CON {stats.Constitution}");
            Console.WriteLine($"INT {stats.Intelligence}  WIS {stats.Wisdom}  CHA {stats.Charisma}");
            return new PlayerCharacter(name, race, characterClass, stats);
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
