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
            _gameManager = gameManager;
            _random = random;
        }

        public void RunAutoCombat(List<ICombatant> party, List<ICombatant> enemies, int maxRounds = 20)
        {
            var allCombatants = new List<ICombatant>();
            allCombatants.AddRange(party);
            allCombatants.AddRange(enemies);

            _gameManager.StartCombat(allCombatants);

            for (int round = 1; round <= maxRounds; round++)
            {
                GameEvents.RaiseCombatLog($"\n--- Round {round} ---");

                foreach (var member in party.Where(p => p.IsAlive))
                {
                    var aliveEnemies = enemies.Where(e => e.IsAlive).ToList();
                    if (aliveEnemies.Count == 0) break;
                    var target = aliveEnemies[_random.Next(0, aliveEnemies.Count)];
                    if (member is IRandomizedCombatant randomizedMember)
                        randomizedMember.PerformAttack(target, _random);
                    else
                        member.PerformAttack(target);
                }

                if (!enemies.Any(e => e.IsAlive))
                {
                    GameEvents.RaiseCombatLog("All enemies defeated! Victory!");
                    break;
                }

                foreach (var enemy in enemies.Where(e => e.IsAlive))
                {
                    var aliveParty = party.Where(p => p.IsAlive).ToList();
                    if (aliveParty.Count == 0) break;
                    var target = aliveParty[_random.Next(0, aliveParty.Count)];
                    if (enemy is IRandomizedCombatant randomizedEnemy)
                        randomizedEnemy.PerformAttack(target, _random);
                    else
                        enemy.PerformAttack(target);
                }

                if (!party.Any(p => p.IsAlive))
                {
                    GameEvents.RaiseCombatLog("The party has been wiped out... Defeat.");
                    break;
                }

                if (round < maxRounds)
                    _gameManager.AdvanceRound();
            }

            _gameManager.EndCombat();
        }
    }
}
