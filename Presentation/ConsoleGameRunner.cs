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

            // --- Create Party ---
            var aurelius = new PlayerCharacter(
                "Aurelius", Race.SemiDragon, CharacterClass.Fighter,
                new Stats(100, 10, 20, 5), combatStyle: "Brawler");

            var andrea = new PlayerCharacter(
                "Andrea", Race.Human, CharacterClass.Fighter,
                new Stats(100, 10, 20, 5), combatStyle: "Swordsman");

            var kaelen = new PlayerCharacter(
                "Kaelen", Race.Elf, CharacterClass.Wizard,
                new Stats(80, 8, 15, 4, 30, 10));

            // --- Equip Party ---
            aurelius.EquipWeapon(EquipmentDatabase.GetWeapon("Dragon Claw"));
            aurelius.EquipArmor(EquipmentDatabase.GetArmor("Dragon Scale"));

            andrea.EquipWeapon(EquipmentDatabase.GetWeapon("Iron Sword"));
            andrea.EquipArmor(EquipmentDatabase.GetArmor("Light Armor"));

            kaelen.EquipWeapon(EquipmentDatabase.GetWeapon("Wooden Staff"));
            kaelen.EquipArmor(EquipmentDatabase.GetArmor("Cloth Robe"));

            // --- Create Boss Enemy ---
            var goblinKing = new Enemy(
                "Goblin King", Race.Goblin,
                new Stats(300, 15, 30, 8),
                combatStyle: "Giant Club Smasher",
                experienceReward: 100);

            goblinKing.EquipWeapon(EquipmentDatabase.GetWeapon("Giant Spiked Club"));
            goblinKing.EquipArmor(EquipmentDatabase.GetArmor("Hardened Skin"));

            // --- Display Stats ---
            Console.WriteLine("\n╔═══════════════════════════════════╗");
            Console.WriteLine("║        PARTY ROSTER               ║");
            Console.WriteLine("╚═══════════════════════════════════╝");

            ConsoleUIRenderer.PrintCharacterStats(aurelius);
            ConsoleUIRenderer.PrintCharacterStats(andrea);
            ConsoleUIRenderer.PrintCharacterStats(kaelen);

            Console.WriteLine("\n╔═══════════════════════════════════╗");
            Console.WriteLine("║        ENEMY ENCOUNTER            ║");
            Console.WriteLine("╚═══════════════════════════════════╝");

            ConsoleUIRenderer.PrintCharacterStats(goblinKing);

            // --- Run Combat ---
            Console.WriteLine("\n>>> COMBAT INITIATED: PARTY vs GOBLIN KING <<<\n");

            var gameManager = new GameManager();
            var combatResolver = new CombatResolver(gameManager);

            var party = new List<ICombatant> { aurelius, andrea, kaelen };
            var enemies = new List<ICombatant> { goblinKing };

            combatResolver.RunAutoCombat(party, enemies, maxRounds: 10);

            // --- Post-Combat Summary ---
            Console.WriteLine("\n╔═══════════════════════════════════╗");
            Console.WriteLine("║      POST-COMBAT SUMMARY          ║");
            Console.WriteLine("╚═══════════════════════════════════╝");

            foreach (var member in party)
            {
                ConsoleUIRenderer.PrintCharacterStats(member);
            }
            ConsoleUIRenderer.PrintCharacterStats(goblinKing);

            Console.WriteLine($"\nTotal Rounds: {gameManager.State.CurrentRound}");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
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
  ║                                                  ║
  ║         A DnD-Inspired Combat Engine             ║
  ║                                                  ║
  ╚══════════════════════════════════════════════════╝");
            Console.ResetColor();
        }
    }
}
