using System;
using System.Collections.Generic;

namespace AStar;

public class AStar<T> where T : INode
{
    private readonly T[,] grid;
    public AStar(T[,] grid)
    {
        this.grid = grid;
    }

    public Path GetPath((int x, int y) from, (int x, int y) to)
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
                    directions.Add(GetDirectionToNeighbor(current.Parent, current).Value);
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
            });
        }
        return null;
    }

    public List<(int x, int y)> GetCornerPoints((int x, int y) from, (int x, int y) to)
    {
        Path path = GetPath(from, to);
        if (path == null)
            return null;

        List<(int x, int y)> positions = new();
        (int x, int y) current = from;
        positions.Add(current);

        foreach (Direction direction in path.Directions)
        {
            (int x, int y) offset = GetOffset(direction);
            current.x += offset.x;
            current.y += offset.y;
            positions.Add(current);
        }

        List<(int x, int y)> fewestPositions = new() { positions[0] };
        for (int i = 1; i < positions.Count - 1; i++)
        {
            if (!IsWalkableBetween(grid[fewestPositions[^1].x, fewestPositions[^1].y], grid[positions[i].x, positions[i].y]))
                fewestPositions.Add(positions[i - 1]);
        }
        fewestPositions.Add(positions[^1]);

        return fewestPositions;
    }

    public Path GetSmoothPath((int x, int y) from, (int x, int y) to)
    {
        List<(int x, int y)> fewestPositions = GetCornerPoints(from, to);

        List<(int x, int y)> smoothedPositions = new();
        for (int i = 1; i < fewestPositions.Count; i++)
        {
            List<(int x, int y)> bresenhamPositions = Bresenham(fewestPositions[i - 1], fewestPositions[i]);
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
            directions.Add(GetDirectionToNeighbor(grid[smoothedPositions[i - 1].x, smoothedPositions[i - 1].y], grid[smoothedPositions[i].x, smoothedPositions[i].y]).Value);
        }
        return new Path(directions);
    }

    public static (int x, int y) GetOffset(Direction direction)
    {
        return direction switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            Direction.UpLeft => (-1, -1),
            Direction.UpRight => (1, -1),
            Direction.DownLeft => (-1, 1),
            Direction.DownRight => (1, 1),
            _ => (0, 0)
        };
    }

    public static List<(int x, int y)> Bresenham((int x, int y) from, (int x, int y) to)
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

        List<(int x, int y)> positions = new();
        while (true)
        {
            positions.Add((x0, y0));
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

    private static Direction? GetDirectionToNeighbor(INode from, INode to)
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
        return null;
    }

    private static void ForEachNeighbor<N>(N[,] grid, (int x, int y) position, Action<N> action) where N : INode
    {
        for (int y = position.y - 1; y - position.y < 2; y++)
        {
            for (int x = position.x - 1; x - position.x < 2; x++)
            {
                if (x == position.x && y == position.y)
                    continue;
                if (x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1))
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
    public (int x, int y) Position { get; set; }
    public INode Parent { get; set; }
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