using Godot;
using System.Collections.Generic;

public partial class Main : Node2D
{
    private const int GridSize = 32;
    private const int BoardWidth = 24;
    private const int BoardHeight = 16;
    private const int InitialSnakeLength = 4;
    private const float MoveInterval = 0.18f;

    private readonly List<Vector2I> _snake = new();
    private Vector2I _direction = Vector2I.Right;
    private Vector2I _pendingDirection = Vector2I.Right;
    private int _growPending = 0;
    private Vector2I _fruit = new(15, 10);
    private readonly HashSet<Vector2I> _obstacles = new();
    private bool _gameOver = false;
    private float _moveAccumulator = 0f;
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
        CreateLevelObstacles();
        ResetGame();
    }

    public override void _Process(double delta)
    {
        if (_gameOver)
        {
            if (Input.IsActionJustPressed("ui_accept"))
            {
                ResetGame();
            }

            QueueRedraw();
            return;
        }

        ReadInput();
        _moveAccumulator += (float)delta;

        while (_moveAccumulator >= MoveInterval)
        {
            _moveAccumulator -= MoveInterval;
            StepGame();
            if (_gameOver)
            {
                break;
            }
        }

        QueueRedraw();
    }

    private void ReadInput()
    {
        if (Input.IsActionJustPressed("ui_left"))
        {
            _pendingDirection = Vector2I.Left;
        }
        else if (Input.IsActionJustPressed("ui_right"))
        {
            _pendingDirection = Vector2I.Right;
        }
        else if (Input.IsActionJustPressed("ui_up"))
        {
            _pendingDirection = Vector2I.Up;
        }
        else if (Input.IsActionJustPressed("ui_down"))
        {
            _pendingDirection = Vector2I.Down;
        }
    }

    private void StepGame()
    {
        if (_pendingDirection + _direction != Vector2I.Zero)
        {
            _direction = _pendingDirection;
        }

        Vector2I nextHead = _snake[0] + _direction;
        if (IsOutside(nextHead) || IsObstacle(nextHead) || SnakeContains(nextHead))
        {
            _gameOver = true;
            return;
        }

        _snake.Insert(0, nextHead);
        if (_growPending > 0)
        {
            _growPending -= 1;
        }
        else
        {
            _snake.RemoveAt(_snake.Count - 1);
        }

        TryEatFruit();
        ApplyGravity();
    }

    private void ApplyGravity()
    {
        while (!IsSupported())
        {
            for (int i = 0; i < _snake.Count; i++)
            {
                Vector2I down = _snake[i] + Vector2I.Down;
                if (IsOutside(down))
                {
                    _gameOver = true;
                    return;
                }

                _snake[i] = down;
            }

            TryEatFruit();
        }
    }

    private bool IsSupported()
    {
        foreach (Vector2I segment in _snake)
        {
            Vector2I down = segment + Vector2I.Down;
            if (IsOutside(down) || IsObstacle(down) || SnakeContains(down))
            {
                return true;
            }
        }

        return false;
    }

    private void TryEatFruit()
    {
        if (_snake[0] == _fruit)
        {
            _growPending += 1;
            SpawnFruit();
        }
    }

    private void SpawnFruit()
    {
        List<Vector2I> freeCells = new();
        for (int y = 0; y < BoardHeight; y++)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                Vector2I cell = new(x, y);
                if (!IsObstacle(cell) && !SnakeContains(cell))
                {
                    freeCells.Add(cell);
                }
            }
        }

        if (freeCells.Count == 0)
        {
            return;
        }

        int index = _rng.RandiRange(0, freeCells.Count - 1);
        _fruit = freeCells[index];
    }

    private bool SnakeContains(Vector2I cell)
    {
        return _snake.Contains(cell);
    }

    private bool IsObstacle(Vector2I cell)
    {
        return _obstacles.Contains(cell);
    }

    private static bool IsOutside(Vector2I cell)
    {
        return cell.X < 0 || cell.X >= BoardWidth || cell.Y < 0 || cell.Y >= BoardHeight;
    }

    private void ResetGame()
    {
        _snake.Clear();
        for (int i = 0; i < InitialSnakeLength; i++)
        {
            _snake.Add(new Vector2I(6 - i, 4));
        }

        _direction = Vector2I.Right;
        _pendingDirection = _direction;
        _growPending = 0;
        _gameOver = false;
        _moveAccumulator = 0f;
        SpawnFruit();
    }

    private void CreateLevelObstacles()
    {
        _obstacles.Clear();

        for (int x = 4; x < 12; x++)
        {
            _obstacles.Add(new Vector2I(x, 9));
        }

        for (int y = 6; y < 11; y++)
        {
            _obstacles.Add(new Vector2I(17, y));
        }

        for (int x = 15; x < 20; x++)
        {
            _obstacles.Add(new Vector2I(x, 6));
        }

        for (int x = 1; x < 5; x++)
        {
            _obstacles.Add(new Vector2I(x, 13));
        }

        _obstacles.Add(new Vector2I(4, 12));
    }

    public override void _Draw()
    {
        Rect2 boardRect = new(Vector2.Zero, new Vector2(BoardWidth, BoardHeight) * GridSize);
        DrawRect(boardRect, new Color("1f1f30"), true);
        DrawRect(boardRect, new Color("6fa6ff"), false, 3.0f);

        foreach (Vector2I cell in _obstacles)
        {
            DrawCell(cell, new Color("4b5a76"));
        }

        for (int i = _snake.Count - 1; i >= 0; i--)
        {
            Color color = i == 0 ? new Color("50e36c") : new Color("1bcf64");
            DrawCell(_snake[i], color);
        }

        DrawCell(_fruit, new Color("ff5d5d"));

        if (_gameOver)
        {
            DrawString(GetThemeDefaultFont(), new Vector2(140, 220), "Game Over! Enter 重开", HorizontalAlignment.Left, -1, 28, Colors.White);
        }

        DrawString(GetThemeDefaultFont(), new Vector2(10, 24), "方向键移动 | 重力持续生效", HorizontalAlignment.Left, -1, 18, new Color("cce4ff"));
    }

    private static Rect2 CellRect(Vector2I cell)
    {
        Vector2 pos = new(cell.X, cell.Y);
        pos *= GridSize;
        return new Rect2(pos + Vector2.One, Vector2.One * (GridSize - 2));
    }

    private void DrawCell(Vector2I cell, Color color)
    {
        DrawRect(CellRect(cell), color, true);
    }
}
