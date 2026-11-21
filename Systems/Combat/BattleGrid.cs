using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DnD.Core;
using DnD.Entities;
using DnD.Systems.Dice;

namespace DnD.Systems.Combat
{
    // ponytail: 2D array grid without elevation or terrain cost; upgrade to 3D voxel / NavMesh when moving to a 3D engine.
    public class BattleGrid
    {
        public const int DefaultWidth = 10;
        public const int DefaultHeight = 10;
        public const int FeetPerTile = 5;

        public int Width { get; }
        public int Height { get; }

        private readonly HashSet<(int X, int Y)> _obstacles = new();
        private readonly Dictionary<ICombatant, (int X, int Y)> _entityPositions = new();

        public BattleGrid(int width = DefaultWidth, int height = DefaultHeight)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
        }

        public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsObstacle(int x, int y) => _obstacles.Contains((x, y));

        public bool IsOccupied(int x, int y) =>
            IsObstacle(x, y) || _entityPositions.Any(kvp => kvp.Key.IsAlive && kvp.Value == (x, y));

        public void AddObstacle(int x, int y)
        {
            if (IsInBounds(x, y))
                _obstacles.Add((x, y));
        }

        public void RemoveObstacle(int x, int y) => _obstacles.Remove((x, y));

        public bool PlaceEntity(ICombatant entity, int x, int y)
        {
            if (entity == null || !IsInBounds(x, y) || IsOccupied(x, y)) return false;

            _entityPositions[entity] = (x, y);
            if (entity is BaseEntity baseEntity)
                baseEntity.SetPosition(x, y);

            return true;
        }

        public bool RemoveEntity(ICombatant entity)
        {
            if (entity == null) return false;
            return _entityPositions.Remove(entity);
        }

        public (int X, int Y)? GetPosition(ICombatant entity)
        {
            if (entity == null || !_entityPositions.TryGetValue(entity, out var pos)) return null;
            return pos;
        }

        public ICombatant GetEntityAt(int x, int y) =>
            _entityPositions.FirstOrDefault(kvp => kvp.Key.IsAlive && kvp.Value == (x, y)).Key;

        public static int CalculateDistance(int x1, int y1, int x2, int y2) =>
            Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2)) * FeetPerTile;

        public int? GetDistance(ICombatant a, ICombatant b)
        {
            var posA = GetPosition(a);
            var posB = GetPosition(b);
            if (!posA.HasValue || !posB.HasValue) return null;
            return CalculateDistance(posA.Value.X, posA.Value.Y, posB.Value.X, posB.Value.Y);
        }

        public bool IsAdjacent(ICombatant a, ICombatant b) =>
            GetDistance(a, b) == FeetPerTile;

        public bool MoveEntity(ICombatant entity, int targetX, int targetY,
                               TurnResources resources = null,
                               Func<ICombatant, TurnResources> resourceProvider = null,
                               IRandomSource random = null)
        {
            if (entity == null || !entity.IsAlive || !IsInBounds(targetX, targetY) || IsOccupied(targetX, targetY))
                return false;

            if (StatusEffectSystem.HasCondition(entity, ConditionType.Stunned))
            {
                GameEvents.RaiseCombatLog($"{entity.Name} is stunned and cannot move!");
                return false;
            }

            var currentPos = GetPosition(entity);
            if (!currentPos.HasValue) return false;

            int distance = CalculateDistance(currentPos.Value.X, currentPos.Value.Y, targetX, targetY);
            if (resources != null && !resources.CanMove(distance))
            {
                GameEvents.RaiseCombatLog($"{entity.Name} lacks movement ({distance}ft required, {resources.MovementRemaining}ft left).");
                return false;
            }

            // Check Opportunity Attacks if leaving melee range without Disengage
            bool disengaged = resources != null && resources.IsDisengaged;
            if (!disengaged)
            {
                var threats = _entityPositions
                    .Where(kvp => kvp.Key != entity && kvp.Key.IsAlive && IsEnemy(entity, kvp.Key))
                    .Where(kvp => CalculateDistance(currentPos.Value.X, currentPos.Value.Y, kvp.Value.X, kvp.Value.Y) <= FeetPerTile)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var threat in threats)
                {
                    var threatPos = _entityPositions[threat];
                    int newDistance = CalculateDistance(targetX, targetY, threatPos.X, threatPos.Y);
                    if (newDistance > FeetPerTile)
                    {
                        var threatRes = resourceProvider?.Invoke(threat);
                        ActionSystem.PerformOpportunityAttack(threat, entity, threatRes, random);

                        if (!entity.IsAlive)
                        {
                            GameEvents.RaiseCombatLog($"{entity.Name} was defeated mid-move by an opportunity attack!");
                            return false;
                        }
                    }
                }
            }

            resources?.UseMovement(distance);
            _entityPositions[entity] = (targetX, targetY);
            if (entity is BaseEntity baseEntity)
                baseEntity.SetPosition(targetX, targetY);

            GameEvents.RaiseCombatLog($"{entity.Name} moved to ({targetX}, {targetY}) [{distance}ft].");
            return true;
        }

        private static bool IsEnemy(ICombatant a, ICombatant b)
        {
            bool aIsPlayer = a is PlayerCharacter;
            bool bIsPlayer = b is PlayerCharacter;
            return aIsPlayer != bIsPlayer;
        }

        public string RenderAscii()
        {
            var sb = new StringBuilder();

            sb.Append("   ");
            for (int x = 0; x < Width; x++)
                sb.Append($"{x % 10} ");
            sb.AppendLine();

            sb.Append("  +");
            for (int x = 0; x < Width; x++)
                sb.Append("--");
            sb.AppendLine();

            for (int y = 0; y < Height; y++)
            {
                sb.Append($"{y % 10} |");
                for (int x = 0; x < Width; x++)
                {
                    if (IsObstacle(x, y))
                        sb.Append("# ");
                    else
                    {
                        var entity = GetEntityAt(x, y);
                        if (entity == null)
                            sb.Append(". ");
                        else if (entity is PlayerCharacter)
                            sb.Append("P ");
                        else if (entity is Enemy)
                            sb.Append("E ");
                        else
                            sb.Append("C ");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("Legend: [P] Player | [E] Enemy | [#] Obstacle | [.] Empty");
            return sb.ToString();
        }
    }
}
