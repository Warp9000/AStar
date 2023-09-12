using System;
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
    private int gridSize = 16;
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
            AStar<Node>.Bresenham(oldMousePosition, mousePosition).ForEach(point => grid[point.x, point.y].IsWalkable = false);
        }
        if (Mouse.GetState().RightButton == ButtonState.Pressed)
        {
            AStar<Node>.Bresenham(oldMousePosition, mousePosition).ForEach(point => grid[point.x, point.y].IsWalkable = true);
        }

        if (Keyboard.GetState().IsKeyDown(Keys.P))
            playerPosition = mousePosition;
        if (Keyboard.GetState().IsKeyDown(Keys.T))
            targetPosition = mousePosition;

        if (Keyboard.GetState().IsKeyDown(Keys.Space) && oldKeyboardState.IsKeyUp(Keys.Space))
        {
            foreach (var item in grid)
            {
                item.F = 0;
                item.G = 0;
                item.H = 0;
                item.Parent = null;
                item.Walked = false;
            }
            AStar<Node> aStar = new(grid);
            Path path = aStar.GetPath(playerPosition, targetPosition);
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
            foreach (var item in grid)
            {
                item.F = 0;
                item.G = 0;
                item.H = 0;
                item.Parent = null;
                item.Walked = false;
            }
            AStar<Node> aStar = new(grid);
            Path path = aStar.GetSmoothPath(playerPosition, targetPosition);
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

        oldKeyboardState = Keyboard.GetState();
        oldMouseState = Mouse.GetState();

        base.Update(gameTime);
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
        spriteBatch.End();
        base.Draw(gameTime);
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