using System.Collections.Generic;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Combat;
using DnD.Systems.Dice;
using Xunit;

namespace DnD.Tests.Systems.Combat
{
    public class BattleGridTests
    {
        [Fact]
        public void BattleGrid_PlacementAndObstacles()
        {
            var grid = new BattleGrid(5, 5);
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));

            // Valid placement
            Assert.True(grid.PlaceEntity(hero, 1, 1));
            Assert.Equal(1, hero.PositionX);
            Assert.Equal(1, hero.PositionY);
            Assert.Equal(hero, grid.GetEntityAt(1, 1));

            // Out of bounds placement fails
            var enemy1 = new Enemy("Enemy1", Race.Goblin, new Stats(30, 5, 2, 1), "Scout");
            Assert.False(grid.PlaceEntity(enemy1, 5, 5));

            // Occupied tile fails
            Assert.False(grid.PlaceEntity(enemy1, 1, 1));

            // Obstacle blocks placement
            grid.AddObstacle(2, 2);
            Assert.True(grid.IsObstacle(2, 2));
            Assert.False(grid.PlaceEntity(enemy1, 2, 2));
        }

        [Fact]
        public void BattleGrid_DistanceAndAdjacency()
        {
            var grid = new BattleGrid(10, 10);
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));
            var enemy = new Enemy("Goblin", Race.Goblin, new Stats(30, 5, 2, 1), "Scout");

            grid.PlaceEntity(hero, 0, 0);
            grid.PlaceEntity(enemy, 1, 1); // Diagonal

            // 5e Chebyshev distance: (0,0) to (1,1) is 5ft
            Assert.Equal(5, grid.GetDistance(hero, enemy));
            Assert.True(grid.IsAdjacent(hero, enemy));

            // Move enemy further
            var res = new TurnResources(30);
            Assert.True(grid.MoveEntity(enemy, 3, 0, res));
            Assert.Equal(15, grid.GetDistance(hero, enemy));
            Assert.False(grid.IsAdjacent(hero, enemy));
        }

        [Fact]
        public void BattleGrid_MovementConsumesTurnResources()
        {
            var grid = new BattleGrid(10, 10);
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));
            grid.PlaceEntity(hero, 0, 0);

            var res = new TurnResources(30);
            // Move 4 tiles = 20ft
            Assert.True(grid.MoveEntity(hero, 4, 0, res));
            Assert.Equal(10, res.MovementRemaining);
            Assert.Equal(4, hero.PositionX);

            // Cannot move 3 tiles (15ft) with only 10ft left
            Assert.False(grid.MoveEntity(hero, 4, 3, res));
            Assert.Equal(0, hero.PositionY); // Remains at 0
        }

        [Fact]
        public void BattleGrid_MovingAwayTriggersOpportunityAttackUnlessDisengaged()
        {
            GameEvents.ClearAll();
            StatusEffectSystem.ClearAll();

            var grid = new BattleGrid(10, 10);
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));
            var enemy = new Enemy("Goblin", Race.Goblin, new Stats(30, 5, 2, 1), "Scout");

            grid.PlaceEntity(hero, 1, 1);
            grid.PlaceEntity(enemy, 1, 2); // Adjacent (5ft)

            var enemyRes = new TurnResources(30);
            var heroRes = new TurnResources(30);

            int attacksTriggered = 0;
            GameEvents.OnAttackPerformed += (attacker, _, _) =>
            {
                if (attacker == enemy) attacksTriggered++;
            };

            // Hero moves from (1,1) to (1,4), leaving melee range of enemy at (1,2)
            // Hero did NOT disengage -> triggers opportunity attack
            Assert.True(grid.MoveEntity(hero, 1, 4, heroRes, _ => enemyRes));
            Assert.Equal(1, attacksTriggered);
            Assert.False(enemyRes.CanReact); // Enemy reaction consumed

            // Second move: Enemy has no reaction left, so no second opportunity attack
            Assert.True(grid.MoveEntity(hero, 1, 3, heroRes, _ => enemyRes)); // Move adjacent to enemy at (1,2)
            heroRes.Reset();
            enemyRes.Reset();

            // Now Hero uses Disengage
            Assert.True(ActionSystem.PerformDisengage(hero, heroRes));
            attacksTriggered = 0;
            Assert.True(grid.MoveEntity(hero, 1, 6, heroRes, _ => enemyRes));
            Assert.Equal(0, attacksTriggered); // No opportunity attack because disengaged
        }

        [Fact]
        public void ActionSystem_RangeEnforcedWithBattleGrid()
        {
            var grid = new BattleGrid(10, 10);
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));
            var enemy = new Enemy("Goblin", Race.Goblin, new Stats(50, 5, 2, 1), "Scout");

            grid.PlaceEntity(hero, 0, 0);
            grid.PlaceEntity(enemy, 3, 3); // Distance = 15ft

            // Melee fails due to out of range
            Assert.False(ActionSystem.PerformMeleeAttack(hero, enemy, grid: grid));

            // Ranged succeeds within 60ft
            Assert.True(ActionSystem.PerformRangedAttack(hero, enemy, grid: grid));
        }

        [Fact]
        public void BattleGrid_RenderAsciiProducesVisualBoard()
        {
            var grid = new BattleGrid(4, 4);
            var hero = new PlayerCharacter("Hero", Race.Human, CharacterClass.Fighter, new Stats(100, 10, 5, 2));
            var enemy = new Enemy("Goblin", Race.Goblin, new Stats(30, 5, 2, 1), "Scout");

            grid.PlaceEntity(hero, 0, 0);
            grid.PlaceEntity(enemy, 3, 3);
            grid.AddObstacle(1, 1);

            string output = grid.RenderAscii();

            Assert.Contains("P", output);
            Assert.Contains("E", output);
            Assert.Contains("#", output);
            Assert.Contains("Legend", output);
        }
    }
}
