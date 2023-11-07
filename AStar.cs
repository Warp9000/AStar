using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace AStar;

public class AStar<T> where T : INode
{
    private readonly T[,] grid;

    private readonly bool dir4;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="dir4">
    ///     If true, the algorithm will only consider the 4 cardinal directions (up, down, left, right).
    /// </param>
    public AStar(T[,] grid, bool dir4 = false)
    {
        this.grid = grid;
        this.dir4 = dir4;
    }

    public Path? GetPath(Position from, Position to)
    {
        T start = grid[from.x, from.y];
        T end = grid[to.x, to.y];

        List<T> openList = new();
        List<T> closedList = new();

        openList.Add(start);

        while (openList.Count > 0)
        {
            T current = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].F < current.F || openList[i].F == current.F && openList[i].H < current.H)
                    current = openList[i];
            }

            openList.Remove(current);
            closedList.Add(current);

            if (current.Equals(end))
            {
                List<Direction> directions = new();
                while (current.Parent != null)
                {
                    directions.Add(GetDirectionToNeighbor(current.Parent, current));
                    current = (T)current.Parent;
                }
                directions.Reverse();
                return new Path(directions);
            }

            ForEachNeighbor(grid, current.Position, neighbor =>
            {
                if (!neighbor.IsWalkable || closedList.Contains(neighbor))
                    return;

                float movementCost = current.G + GetDistance(current, neighbor);
                if (movementCost < neighbor.G || !openList.Contains(neighbor))
                {
                    neighbor.G = movementCost;
                    neighbor.H = GetDistance(neighbor, end);
                    neighbor.F = neighbor.G + neighbor.H;
                    neighbor.Parent = current;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }, dir4);
        }
        return null;
    }

    public List<Position>? GetCornerPoints(Position from, Position to)
    {
        Path? path = GetPath(from, to);
        if (path == null)
            return null;

        List<Position> positions = new();
        Position current = from;
        positions.Add(current);

        foreach (Direction direction in path.Directions)
        {
            Position offset = GetOffset(direction);
            current.x += offset.x;
            current.y += offset.y;
            positions.Add(current);
        }

        List<Position> fewestPositions = new() { positions[0] };
        for (int i = 1; i < positions.Count - 1; i++)
        {
            if (!IsWalkableBetween(grid[fewestPositions[^1].x, fewestPositions[^1].y], grid[positions[i].x, positions[i].y]))
                fewestPositions.Add(positions[i - 1]);
        }
        fewestPositions.Add(positions[^1]);

        return fewestPositions;
    }

    public Path? GetSmoothPath(Position from, Position to)
    {
        List<Position>? fewestPositions = GetCornerPoints(from, to);
        if (fewestPositions == null)
            return null;

        List<Position> smoothedPositions = new();
        for (int i = 1; i < fewestPositions.Count; i++)
        {
            List<Position> bresenhamPositions = Bresenham(fewestPositions[i - 1], fewestPositions[i]);
            smoothedPositions.AddRange(bresenhamPositions);
        }

        for (int i = 1; i < smoothedPositions.Count; i++)
        {
            if (smoothedPositions[i - 1].x == smoothedPositions[i].x && smoothedPositions[i - 1].y == smoothedPositions[i].y)
            {
                smoothedPositions.RemoveAt(i);
                i--;
            }
        }

        List<Direction> directions = new();
        for (int i = 1; i < smoothedPositions.Count; i++)
        {
            var dir = GetDirectionToNeighbor(grid[smoothedPositions[i - 1].x, smoothedPositions[i - 1].y], grid[smoothedPositions[i].x, smoothedPositions[i].y]);
            if (dir4)
            {
                var pos = smoothedPositions[i - 1];
                switch (dir)
                {
                    case Direction.UpLeft:
                        var offsetPos = pos + GetOffset(Direction.Up);
                        if (grid[offsetPos.x, offsetPos.y].IsWalkable)
                        {
                            directions.Add(Direction.Up);
                            directions.Add(Direction.Left);
                        }
                        else
                        {
                            directions.Add(Direction.Left);
                            directions.Add(Direction.Up);
                        }
                        break;
                    case Direction.UpRight:
                        offsetPos = pos + GetOffset(Direction.Up);
                        if (grid[offsetPos.x, offsetPos.y].IsWalkable)
                        {
                            directions.Add(Direction.Up);
                            directions.Add(Direction.Right);
                        }
                        else
                        {
                            directions.Add(Direction.Right);
                            directions.Add(Direction.Up);
                        }
                        break;
                    case Direction.DownLeft:
                        offsetPos = pos + GetOffset(Direction.Down);
                        if (grid[offsetPos.x, offsetPos.y].IsWalkable)
                        {
                            directions.Add(Direction.Down);
                            directions.Add(Direction.Left);
                        }
                        else
                        {
                            directions.Add(Direction.Left);
                            directions.Add(Direction.Down);
                        }
                        break;
                    case Direction.DownRight:
                        offsetPos = pos + GetOffset(Direction.Down);
                        if (grid[offsetPos.x, offsetPos.y].IsWalkable)
                        {
                            directions.Add(Direction.Down);
                            directions.Add(Direction.Right);
                        }
                        else
                        {
                            directions.Add(Direction.Right);
                            directions.Add(Direction.Down);
                        }
                        break;
                    default:
                        directions.Add(dir);
                        break;
                }
            }
            else
            {
                directions.Add(dir);
            }
        }
        return new Path(directions);
    }

    public static Position GetOffset(Direction direction)
    {
        return direction switch
        {
            Direction.Up => new Position(0, -1),
            Direction.Down => new Position(0, 1),
            Direction.Left => new Position(-1, 0),
            Direction.Right => new Position(1, 0),
            Direction.UpLeft => new Position(-1, -1),
            Direction.UpRight => new Position(1, -1),
            Direction.DownLeft => new Position(-1, 1),
            Direction.DownRight => new Position(1, 1),
            _ => new Position(0, 0)
        };
    }

    public static List<Position> Bresenham(Position from, Position to)
    {
        // https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
        int x0 = from.x;
        int y0 = from.y;
        int x1 = to.x;
        int y1 = to.y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        List<Position> positions = new();
        while (true)
        {
            positions.Add(new Position(x0, y0));
            if (x0 == x1 && y0 == y1)
                break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        return positions;
    }

    private bool IsWalkableBetween(T a, T b)
    {
        int x0 = a.Position.x;
        int y0 = a.Position.y;
        int x1 = b.Position.x;
        int y1 = b.Position.y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (!grid[x0, y0].IsWalkable)
                return false;
            if (x0 == x1 && y0 == y1)
                break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        return true;
    }

    private static float GetDistance(T a, T b)
    {
        return MathF.Sqrt(MathF.Pow(a.Position.x - b.Position.x, 2) + MathF.Pow(a.Position.y - b.Position.y, 2));
    }

    private static Direction GetDirectionToNeighbor(INode from, INode to)
    {
        if (from.Position.x < to.Position.x && from.Position.y == to.Position.y)
            return Direction.Right;
        if (from.Position.x > to.Position.x && from.Position.y == to.Position.y)
            return Direction.Left;
        if (from.Position.x == to.Position.x && from.Position.y < to.Position.y)
            return Direction.Down;
        if (from.Position.x == to.Position.x && from.Position.y > to.Position.y)
            return Direction.Up;
        if (from.Position.x < to.Position.x && from.Position.y < to.Position.y)
            return Direction.DownRight;
        if (from.Position.x > to.Position.x && from.Position.y < to.Position.y)
            return Direction.DownLeft;
        if (from.Position.x < to.Position.x && from.Position.y > to.Position.y)
            return Direction.UpRight;
        if (from.Position.x > to.Position.x && from.Position.y > to.Position.y)
            return Direction.UpLeft;
        throw new IndexOutOfRangeException("The nodes are not neighbors.");
    }

    private static Direction GetDirectionToNeighbor4(INode from, INode to)
    {
        var dir = GetDirectionToNeighbor(from, to);
        return dir switch
        {
            Direction.Right => Direction.Right,
            Direction.Left => Direction.Left,
            Direction.Down => Direction.Down,
            Direction.Up => Direction.Up,
            _ => throw new IndexOutOfRangeException("The nodes are not cardinal neighbors.")
        };

    }

    private static void ForEachNeighbor<N>(N[,] grid, Position position, Action<N> action, bool dir4 = false) where N : INode
    {
        for (int y = position.y - 1; y - position.y < 2; y++)
        {
            for (int x = position.x - 1; x - position.x < 2; x++)
            {
                if (x == position.x && y == position.y)
                    continue;
                if (x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1))
                    continue;
                if (dir4 && x != position.x && y != position.y)
                    continue;
                action(grid[x, y]);
            }
        }
    }
}

public interface INode
{
    public bool IsWalkable { get; set; }
    public float F { get; set; }
    public float G { get; set; }
    public float H { get; set; }
    public Position Position { get; set; }
    public INode? Parent { get; set; }
}

public class Path
{
    public List<Direction> Directions { get; set; }
    public Path(List<Direction> directions)
    {
        Directions = directions;
    }
}

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    UpRight,
    DownLeft,
    DownRight
}

[DebuggerDisplay("({x}, {y})")]
public struct Position
{
    public int x;
    public int y;

    public Position(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Position((int x, int y) position)
    {
        this.x = position.x;
        this.y = position.y;
    }

    public static Position operator +(Position a, Position b)
    {
        return new Position(a.x + b.x, a.y + b.y);
    }

    public static Position operator -(Position a, Position b)
    {
        return new Position(a.x - b.x, a.y - b.y);
    }

    public static bool operator ==(Position a, Position b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Position a, Position b)
    {
        return !(a == b);
    }

    public static implicit operator Position((int x, int y) position)
    {
        return new Position(position);
    }

    public static implicit operator (int x, int y)(Position position)
    {
        return (position.x, position.y);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Position position && x == position.x && y == position.y;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(x, y);
    }

    public override string ToString()
    {
        return $"({x}, {y})";
    }
}