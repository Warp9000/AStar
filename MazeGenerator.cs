using System;
using System.Collections.Generic;

namespace MazeGenerator;

public static class MazeGenerator
{
    // https://en.wikipedia.org/wiki/Maze_generation_algorithm
    public static bool[,] GenerateMaze(int width, int height, (int x, int y) start, (int x, int y) end)
    {
        bool[,] maze = new bool[width, height];
        for (int x = 0; x < maze.GetLength(0); x++)
            for (int y = 0; y < maze.GetLength(1); y++)
                maze[x, y] = true;

        Stack<(int x, int y)> stack = new();
        stack.Push(start);
        maze[start.x, start.y] = false;

        while (stack.Count > 0)
        {
            (int x, int y) current = stack.Pop();
            List<(int x, int y)> neighbors = new();
            if (current.x - 2 >= 0)
                neighbors.Add((current.x - 2, current.y));
            if (current.x + 2 < maze.GetLength(0))
                neighbors.Add((current.x + 2, current.y));
            if (current.y - 2 >= 0)
                neighbors.Add((current.x, current.y - 2));
            if (current.y + 2 < maze.GetLength(1))
                neighbors.Add((current.x, current.y + 2));

            neighbors.Shuffle();
            foreach ((int x, int y) neighbor in neighbors)
            {
                if (maze[neighbor.x, neighbor.y])
                {
                    maze[neighbor.x, neighbor.y] = false;
                    maze[(neighbor.x + current.x) / 2, (neighbor.y + current.y) / 2] = false;
                    stack.Push(current);
                    stack.Push(neighbor);
                    break;
                }
            }
        }

        maze[end.x, end.y] = false;
        // invert
        for (int x = 0; x < maze.GetLength(0); x++)
            for (int y = 0; y < maze.GetLength(1); y++)
                maze[x, y] = !maze[x, y];
        return maze;
    }

    private static void Shuffle<T>(this List<T> list)
    {
        Random random = new();
        for (int i = 0; i < list.Count; i++)
        {
            int j = random.Next(i, list.Count);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}