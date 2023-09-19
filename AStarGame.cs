using System;
using System.Collections.Generic;
using System.Diagnostics;
using AStar;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AStarGame;

public class AStarGame : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;

    private Texture2D pixel;
    private Node[,] grid;
    List<(int x, int y)> LinePoints = new();
    private int gridSize = 4;
    private (int x, int y) playerPosition;
    private (int x, int y) targetPosition;

    public AStarGame()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        grid = new Node[graphics.PreferredBackBufferWidth / gridSize, graphics.PreferredBackBufferHeight / gridSize];
        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
    }

    private KeyboardState oldKeyboardState = Keyboard.GetState();
    private MouseState oldMouseState = Mouse.GetState();

    private Stopwatch stopwatch = new();

    private void brush(int x, int y, bool walkable)
    {
        if (x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1))
            return;
        grid[x, y].IsWalkable = walkable;

        if (x + 1 < grid.GetLength(0))
            grid[x + 1, y].IsWalkable = walkable;
        if (x - 1 >= 0)
            grid[x - 1, y].IsWalkable = walkable;
        if (y + 1 < grid.GetLength(1))
            grid[x, y + 1].IsWalkable = walkable;
        if (y - 1 >= 0)
            grid[x, y - 1].IsWalkable = walkable;
    }
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        (int x, int y) mousePosition = (Mouse.GetState().X / gridSize, Mouse.GetState().Y / gridSize);
        (int x, int y) oldMousePosition = (oldMouseState.X / gridSize, oldMouseState.Y / gridSize);
        if (mousePosition.x < 0 || mousePosition.x >= grid.GetLength(0) || mousePosition.y < 0 || mousePosition.y >= grid.GetLength(1))
        {
            oldKeyboardState = Keyboard.GetState();
            oldMouseState = Mouse.GetState();
            base.Update(gameTime);
            return;
        }
        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
        {
            AStar<Node>.Bresenham(oldMousePosition, mousePosition).ForEach(point => brush(point.x, point.y, false));
        }
        if (Mouse.GetState().RightButton == ButtonState.Pressed)
        {
            AStar<Node>.Bresenham(oldMousePosition, mousePosition).ForEach(point => brush(point.x, point.y, true));
        }

        if (Keyboard.GetState().IsKeyDown(Keys.P))
            playerPosition = mousePosition;
        if (Keyboard.GetState().IsKeyDown(Keys.T))
            targetPosition = mousePosition;

        if (Keyboard.GetState().IsKeyDown(Keys.M) && oldKeyboardState.IsKeyUp(Keys.M))
        {
            stopwatch.Restart();
            var bgrid = MazeGenerator.MazeGenerator.GenerateMaze(grid.GetLength(0), grid.GetLength(1), playerPosition, targetPosition);
            stopwatch.Stop();
            Console.WriteLine("maze: " + (stopwatch.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond).ToString("0.000") + "ms");

            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    grid[x, y].IsWalkable = bgrid[x, y];
        }

        if (Keyboard.GetState().IsKeyDown(Keys.Space) && oldKeyboardState.IsKeyUp(Keys.Space))
        {
            ResetGrid();
            AStar<Node> aStar = new(grid);
            stopwatch.Restart();
            Path path = aStar.GetPath(playerPosition, targetPosition);
            stopwatch.Stop();
            Console.WriteLine("path: " + (stopwatch.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond).ToString("0.000") + "ms");
            if (path != null)
            {
                foreach (Direction direction in path.Directions)
                {
                    (int x, int y) offset = AStar<Node>.GetOffset(direction);
                    playerPosition.x += offset.x;
                    playerPosition.y += offset.y;

                    grid[playerPosition.x, playerPosition.y].Walked = true;
                }
            }
        }
        if (Keyboard.GetState().IsKeyDown(Keys.S) && oldKeyboardState.IsKeyUp(Keys.S))
        {
            ResetGrid();
            AStar<Node> aStar = new(grid);
            stopwatch.Restart();
            Path path = aStar.GetSmoothPath(playerPosition, targetPosition);
            stopwatch.Stop();
            Console.WriteLine("smooth path: " + (stopwatch.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond).ToString("0.000") + "ms");
            if (path != null)
            {
                foreach (Direction direction in path.Directions)
                {
                    (int x, int y) offset = AStar<Node>.GetOffset(direction);
                    playerPosition.x += offset.x;
                    playerPosition.y += offset.y;

                    grid[playerPosition.x, playerPosition.y].Walked = true;
                }
            }
        }

        if (Keyboard.GetState().IsKeyDown(Keys.K) && oldKeyboardState.IsKeyUp(Keys.K))
        {
            ResetGrid();
            AStar<Node> aStar = new(grid);
            stopwatch.Restart();
            var points = aStar.GetCornerPoints(playerPosition, targetPosition);
            stopwatch.Stop();
            Console.WriteLine("corner points: " + (stopwatch.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond).ToString("0.000") + "ms");
            if (points != null)
            {
                LinePoints = points;
                for (int i = 0; i < points.Count; i++)
                {
                    grid[points[i].x, points[i].y].Walked = true;
                }
            }
        }

        if (Keyboard.GetState().IsKeyDown(Keys.C) && oldKeyboardState.IsKeyUp(Keys.C))
        {
            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    grid[x, y].IsWalkable = true;
        }

        oldKeyboardState = Keyboard.GetState();
        oldMouseState = Mouse.GetState();

        base.Update(gameTime);
    }

    private void ResetGrid()
    {
        foreach (var item in grid)
        {
            item.F = 0;
            item.G = 0;
            item.H = 0;
            item.Parent = null;
            item.Walked = false;
        }
        LinePoints.Clear();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        spriteBatch.Begin();
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] == null)
                    grid[x, y] = new Node { IsWalkable = true, Position = (x, y) };

                if (grid[x, y].IsWalkable)
                    spriteBatch.Draw(pixel, new Rectangle(x * gridSize, y * gridSize, gridSize, gridSize), Color.LightGray);

                if (grid[x, y].Walked)
                    spriteBatch.Draw(pixel, new Rectangle(x * gridSize, y * gridSize, gridSize, gridSize), Color.Green);

                if (grid[x, y].Position == playerPosition)
                    spriteBatch.Draw(pixel, new Rectangle(x * gridSize, y * gridSize, gridSize, gridSize), Color.Red);

                if (grid[x, y].Position == targetPosition)
                    spriteBatch.Draw(pixel, new Rectangle(x * gridSize, y * gridSize, gridSize, gridSize), Color.Blue);

                if (grid[x, y].F > 0)
                {
                    // logrithmic scale
                    var color = new Color(MathF.Log(grid[x, y].G, 2) / 10, MathF.Log(grid[x, y].H, 2) / 10, MathF.Log(grid[x, y].F, 2) / 10);
                    spriteBatch.Draw(pixel, new Rectangle(x * gridSize + gridSize / 4, y * gridSize + gridSize / 4, gridSize / 2, gridSize / 2), color);
                }
            }
        }
        if (LinePoints.Count >= 2)
        {
            for (int i = 0; i < LinePoints.Count - 1; i++)
            {
                DrawLine(spriteBatch, Color.Red, LinePoints[i], LinePoints[i + 1]);
            }
        }
        spriteBatch.End();
        base.Draw(gameTime);
    }

    protected void DrawLine(SpriteBatch spriteBatch, Color color, (int x, int y) start, (int x, int y) end)
    {
        Vector2 screenPos1 = new(start.x * gridSize + gridSize / 2, start.y * gridSize + gridSize / 2);
        Vector2 screenPos2 = new(end.x * gridSize + gridSize / 2, end.y * gridSize + gridSize / 2);

        float angle = (float)Math.Atan2(screenPos2.Y - screenPos1.Y, screenPos2.X - screenPos1.X);
        float length = Vector2.Distance(screenPos1, screenPos2);

        spriteBatch.Draw(pixel, screenPos1, null, color, angle, Vector2.Zero, new Vector2(length, 1), SpriteEffects.None, 0);
    }
}

public class Node : INode
{
    public bool Walked { get; set; }

    public bool IsWalkable { get; set; }
    public float G { get; set; }
    public float H { get; set; }
    public float F { get; set; }
    public INode Parent { get; set; }
    public (int x, int y) Position { get; set; }
}