namespace Minesweeper.Models;

public class GameBoard
{
    public int Rows { get; set; }
    public int Cols { get; set; }
    public int MineCount { get; set; }
    public Cell[,] Board { get; set; }
    public GameStatus Status { get; set; }
    public bool IsInitialized { get; set; }
    public Difficulty CurrentDifficulty { get; set; }

    public GameBoard(Difficulty difficulty)
    {
        var (rows, cols, mines) = DifficultySettings.GetSettings(difficulty);
        Rows = rows;
        Cols = cols;
        MineCount = mines;
        CurrentDifficulty = difficulty;
        Board = new Cell[rows, cols];
        Status = GameStatus.Playing;
        IsInitialized = false;
        
        InitializeBoard();
        PlaceMines();
        CalculateNumbers();
        IsInitialized = true;
    }

    private void InitializeBoard()
    {
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                Board[i, j] = new Cell();
            }
        }
    }

    private void PlaceMines()
    {
        var random = new Random();
        int minesPlaced = 0;

        while (minesPlaced < MineCount)
        {
            int row = random.Next(Rows);
            int col = random.Next(Cols);

            if (!Board[row, col].IsMine)
            {
                Board[row, col].IsMine = true;
                minesPlaced++;
            }
        }
    }

    private void CalculateNumbers()
    {
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (!Board[i, j].IsMine)
                {
                    Board[i, j].AdjacentMines = CountAdjacentMines(i, j);
                }
            }
        }
    }

    private int CountAdjacentMines(int row, int col)
    {
        int count = 0;
        
        for (int i = row - 1; i <= row + 1; i++)
        {
            for (int j = col - 1; j <= col + 1; j++)
            {
                // Skip the center cell
                if (i == row && j == col) continue;
                
                // Check if position is valid and has a mine
                if (i >= 0 && i < Rows && j >= 0 && j < Cols && Board[i, j].IsMine)
                {
                    count++;
                }
            }
        }
        
        return count;
    }

    public void RevealCell(int row, int col)
    {
        if (!IsValidCell(row, col) || Board[row, col].IsRevealed || Board[row, col].IsFlagged || Status != GameStatus.Playing)
            return;

        Board[row, col].IsRevealed = true;

        if (Board[row, col].IsMine)
        {
            Status = GameStatus.Lost;
            RevealAllMines();
            return;
        }

        // Only cascade if this cell has no adjacent mines
        if (Board[row, col].AdjacentMines == 0)
        {
            RevealAdjacentSafeCells(row, col);
        }

        CheckWinCondition();
    }

    private void RevealAdjacentSafeCells(int row, int col)
    {
        for (int i = row - 1; i <= row + 1; i++)
        {
            for (int j = col - 1; j <= col + 1; j++)
            {
                // Skip the center cell
                if (i == row && j == col) continue;
                
                // Check if position is valid
                if (!IsValidCell(i, j)) continue;
                
                var cell = Board[i, j];
                
                // ONLY reveal if: NOT a mine, NOT already revealed, NOT flagged
                if (!cell.IsMine && !cell.IsRevealed && !cell.IsFlagged)
                {
                    cell.IsRevealed = true;
                    
                    // Continue cascade only if this cell also has no adjacent mines
                    if (cell.AdjacentMines == 0)
                    {
                        RevealAdjacentSafeCells(i, j);
                    }
                }
            }
        }
    }

    private void RevealAllMines()
    {
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (Board[i, j].IsMine)
                {
                    Board[i, j].IsRevealed = true;
                }
            }
        }
    }

    public void ToggleFlag(int row, int col)
    {
        if (!IsValidCell(row, col) || Board[row, col].IsRevealed || Status != GameStatus.Playing)
            return;

        Board[row, col].IsFlagged = !Board[row, col].IsFlagged;
    }

    private void CheckWinCondition()
    {
        int revealedCount = 0;
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (Board[i, j].IsRevealed && !Board[i, j].IsMine)
                {
                    revealedCount++;
                }
            }
        }

        if (revealedCount == (Rows * Cols - MineCount))
        {
            Status = GameStatus.Won;
        }
    }

    private bool IsValidCell(int row, int col)
    {
        return row >= 0 && row < Rows && col >= 0 && col < Cols;
    }

    public List<string> ValidateBoard()
    {
        var errors = new List<string>();
        
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                var cell = Board[i, j];
                if (!cell.IsMine)
                {
                    int actualCount = CountAdjacentMines(i, j);
                    if (cell.AdjacentMines != actualCount)
                    {
                        errors.Add($"Cell ({i},{j}): Expected {actualCount} adjacent mines, but has {cell.AdjacentMines}");
                    }
                }
            }
        }
        
        return errors;
    }
}

public class Cell
{
    public bool IsMine { get; set; }
    public bool IsRevealed { get; set; }
    public bool IsFlagged { get; set; }
    public int AdjacentMines { get; set; }
}

public enum GameStatus
{
    Playing,
    Won,
    Lost
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}

public static class DifficultySettings
{
    public static (int rows, int cols, int mines) GetSettings(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => (9, 9, 10),     // Beginner: 9x9 with 10 mines
            Difficulty.Medium => (16, 16, 40), // Intermediate: 16x16 with 40 mines
            Difficulty.Hard => (16, 30, 99),   // Expert: 16x30 with 99 mines
            _ => (9, 9, 10)
        };
    }
    
    public static string GetDisplayName(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "Easy (9x9, 10 mines)",
            Difficulty.Medium => "Medium (16x16, 40 mines)",
            Difficulty.Hard => "Hard (16x30, 99 mines)",
            _ => "Easy"
        };
    }
}