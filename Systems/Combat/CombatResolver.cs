using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Dice;

namespace DnD.Systems.Combat
{
    public class CombatResolver
    {
        private readonly GameManager _gameManager;
        private readonly IRandomSource _random;

        public CombatResolver(GameManager gameManager) : this(gameManager, DiceRoller.RandomSource)
        {
        }

        public CombatResolver(GameManager gameManager, IRandomSource random)
        {
            _gameManager = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public void RunAutoCombat(List<ICombatant> party, List<ICombatant> enemies, int maxRounds = 20)
        {
            if (party == null) throw new ArgumentNullException(nameof(party));
            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            var combatants = party.Concat(enemies).ToList();
            _gameManager.StartCombat(combatants);
            for (int round = 0; round < maxRounds && party.Any(p => p.IsAlive) && enemies.Any(e => e.IsAlive); round++)
            {
                foreach (var member in party.Where(p => p.IsAlive))
                {
                    var targets = enemies.Where(e => e.IsAlive).ToList();
                    if (targets.Count == 0) break;
                    if (member is IRandomizedCombatant randomizedMember)
                        randomizedMember.PerformAttack(targets[_random.Next(0, targets.Count)], _random);
                    else
                        member.PerformAttack(targets[_random.Next(0, targets.Count)]);
                }
                foreach (var enemy in enemies.Where(e => e.IsAlive))
                {
                    var targets = party.Where(p => p.IsAlive).ToList();
                    if (targets.Count == 0) break;
                    if (enemy is IRandomizedCombatant randomizedEnemy)
                        randomizedEnemy.PerformAttack(targets[_random.Next(0, targets.Count)], _random);
                    else
                        enemy.PerformAttack(targets[_random.Next(0, targets.Count)]);
                }
                if (round + 1 < maxRounds && party.Any(p => p.IsAlive) && enemies.Any(e => e.IsAlive))
                    _gameManager.AdvanceRound();
            }
            _gameManager.EndCombat();
        }

        public void RunTurnBasedCombat(List<ICombatant> party, List<ICombatant> enemies)
        {
            if (party == null) throw new ArgumentNullException(nameof(party));
            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            if (party.Count == 0) throw new ArgumentException("Party cannot be empty.", nameof(party));
            if (enemies.Count == 0) throw new ArgumentException("Enemies cannot be empty.", nameof(enemies));

            _gameManager.StartCombat(party.Concat(enemies).ToList());
            var rewardedEnemies = new HashSet<ICombatant>();
            int round = 0;

            while (party.Any(p => p.IsAlive) && enemies.Any(e => e.IsAlive))
            {
                round++;
                GameEvents.RaiseCombatLog($"\n--- Round {round} ---");

                foreach (var member in party.Where(p => p.IsAlive).ToList())
                {
                    var targets = enemies.Where(e => e.IsAlive).ToList();
                    if (targets.Count == 0) break;
                    GameEvents.RaiseTurnChanged(member);
                    Console.WriteLine($"\n>>> {member.Name}'s turn <<<");
                    Console.WriteLine("1. Attack");
                    Console.WriteLine("2. Class ability");
                    Console.WriteLine("3. Defend");
                    Console.WriteLine("4. Use consumable");
                    var choice = GetPlayerChoice(4);

                    if (choice == 1)
                    {
                        var target = SelectTarget(targets);
                        if (member is IRandomizedCombatant randomizedMember)
                            randomizedMember.PerformAttack(target, _random);
                        else
                            member.PerformAttack(target);
                    }
                    else if (choice == 2 && member is PlayerCharacter character)
                    {
                        var target = character.CharacterClass == CharacterClass.Cleric ? character : SelectTarget(targets);
                        character.UseClassAbility(target, _random);
                    }
                    else if (choice == 3 && member is BaseEntity entity)
                    {
                        entity.Defend();
                        GameEvents.RaiseCombatLog($"{member.Name} braces for the next hit.");
                    }
                    else if (choice == 4 && member is PlayerCharacter itemUser)
                    {
                        if (itemUser.Inventory.Consumables.Count == 0)
                            GameEvents.RaiseCombatLog("No consumables available.");
                        else
                        {
                            for (int i = 0; i < itemUser.Inventory.Consumables.Count; i++)
                                Console.WriteLine($"{i + 1}. {itemUser.Inventory.Consumables[i].Name}");
                            int index = GetPlayerChoice(itemUser.Inventory.Consumables.Count) - 1;
                            if (!itemUser.UseConsumable(index))
                                GameEvents.RaiseCombatLog("Item cannot be used now.");
                        }
                    }
                    else
                        GameEvents.RaiseCombatLog("Action unavailable for this combatant.");
                }

                foreach (var defeated in enemies.Where(e => !e.IsAlive && !rewardedEnemies.Contains(e)))
                {
                    rewardedEnemies.Add(defeated);
                    if (defeated is Enemy enemyReward && enemyReward.ExperienceReward > 0)
                    {
                        foreach (var winner in party.OfType<PlayerCharacter>().Where(p => p.IsAlive))
                            winner.GainExperience(enemyReward.ExperienceReward);
                    }
                }

                foreach (var enemy in enemies.Where(e => e.IsAlive).ToList())
                {
                    var targets = party.Where(p => p.IsAlive).ToList();
                    if (targets.Count == 0) break;
                    GameEvents.RaiseTurnChanged(enemy);
                    var target = targets[_random.Next(0, targets.Count)];
                    if (enemy is IRandomizedCombatant randomizedEnemy)
                        randomizedEnemy.PerformAttack(target, _random);
                    else
                        enemy.PerformAttack(target);
                }

                if (party.Any(p => p.IsAlive) && enemies.Any(e => e.IsAlive))
                    _gameManager.AdvanceRound();
            }

            GameEvents.RaiseCombatLog(enemies.Any(e => e.IsAlive)
                ? "The party has been defeated."
                : "All enemies defeated! Victory!");
            _gameManager.EndCombat();
        }

        private static ICombatant SelectTarget(List<ICombatant> targets)
        {
            for (int i = 0; i < targets.Count; i++)
                Console.WriteLine($"{i + 1}. {targets[i].Name} ({targets[i].CurrentHealth}/{targets[i].MaxHealth} HP)");
            return targets[GetPlayerChoice(targets.Count) - 1];
        }

        private static int GetPlayerChoice(int maximum)
        {
            while (true)
            {
                Console.Write($"Choose 1-{maximum}: ");
                if (int.TryParse(Console.ReadLine(), out var choice) && choice >= 1 && choice <= maximum)
                    return choice;
                Console.WriteLine("Invalid choice.");
            }
        }

        private void ExecuteWizardSpell(PlayerCharacter wizard, ICombatant target)
        {
            int manaCost = 5;
            if (wizard.CurrentMana >= manaCost)
            {
                wizard.SpendMana(manaCost);
                int spellDamage = wizard.AttackPower + 20;
                GameEvents.RaiseCombatLog($"{wizard.Name} casts 'Fireball' at {target.Name}!");
                GameEvents.RaiseAttackPerformed(wizard, target, spellDamage);
                target.TakeDamage(spellDamage);
                wizard.GainExperience(40);
            }
            else
            {
                GameEvents.RaiseCombatLog($"{wizard.Name} is out of mana!");
            }
        }
    }
}
