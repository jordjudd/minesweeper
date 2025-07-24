using System.Collections.Generic;

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

    public GameBoard(int rows, int cols, int mineCount)
    {
        Rows = rows;
        Cols = cols;
        MineCount = mineCount;
        CurrentDifficulty = Difficulty.Easy; // Default
        Board = new Cell[rows, cols];
        Status = GameStatus.Playing;
        IsInitialized = false;
        InitializeBoard();
        PlaceMinesRandomly(); // Place mines immediately
    }

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
        PlaceMinesRandomly(); // Place mines immediately
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

    private void PlaceMinesRandomly()
    {
        // Clear any existing mines first
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                Board[i, j].IsMine = false;
                Board[i, j].AdjacentMines = 0;
            }
        }
        
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

        // Recalculate all numbers after mine placement
        CalculateNumbers();
        IsInitialized = true;
    }

    private void PlaceMines(int firstRow, int firstCol)
    {
        // This method is no longer used, but keeping for compatibility
        var random = new Random();
        int minesPlaced = 0;

        while (minesPlaced < MineCount)
        {
            int row = random.Next(Rows);
            int col = random.Next(Cols);

            if (!Board[row, col].IsMine && !(row == firstRow && col == firstCol))
            {
                Board[row, col].IsMine = true;
                minesPlaced++;
            }
        }

        CalculateNumbers();
        IsInitialized = true;
    }

    private void CalculateNumbers()
    {
        // First, ensure all mines are properly placed
        int actualMineCount = 0;
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (Board[i, j].IsMine) actualMineCount++;
            }
        }
        
        // Now calculate adjacent mine counts
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                var cell = Board[i, j];
                if (!cell.IsMine)
                {
                    cell.AdjacentMines = CountAdjacentMines(i, j);
                }
                else
                {
                    // Mines should not have adjacent counts
                    cell.AdjacentMines = 0;
                }
            }
        }
    }

    private int CountAdjacentMines(int row, int col)
    {
        int count = 0;
        
        // Define the 8 adjacent positions explicitly
        int[] deltaRows = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] deltaCols = { -1, 0, 1, -1, 1, -1, 0, 1 };
        
        for (int i = 0; i < 8; i++)
        {
            int newRow = row + deltaRows[i];
            int newCol = col + deltaCols[i];
            
            // Check if the adjacent cell is valid and contains a mine
            if (IsValidCell(newRow, newCol) && Board[newRow, newCol].IsMine)
            {
                count++;
            }
        }
        
        return count;
    }

    public void RevealCell(int row, int col)
    {
        // Validate input and game state
        if (!IsValidCell(row, col) || Board[row, col].IsRevealed || Board[row, col].IsFlagged || Status != GameStatus.Playing)
            return;

        var cell = Board[row, col];
        
        // Reveal the clicked cell
        cell.IsRevealed = true;

        // If it's a mine, game over
        if (cell.IsMine)
        {
            Status = GameStatus.Lost;
            RevealAllMines();
            return;
        }

        // If it has no adjacent mines, start cascade reveal
        if (cell.AdjacentMines == 0)
        {
            RevealAdjacentCells(row, col);
        }

        // Check if player won
        CheckWinCondition();
    }

    private void RevealAdjacentCells(int row, int col)
    {
        // Use a queue to avoid deep recursion and ensure proper order
        var cellsToReveal = new Queue<(int row, int col)>();
        cellsToReveal.Enqueue((row, col));
        
        while (cellsToReveal.Count > 0)
        {
            var (currentRow, currentCol) = cellsToReveal.Dequeue();
            
            // Check all 8 adjacent cells
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    // Skip the center cell
                    if (i == 0 && j == 0) continue;
                    
                    int newRow = currentRow + i;
                    int newCol = currentCol + j;
                    
                    // Check bounds
                    if (!IsValidCell(newRow, newCol)) continue;
                    
                    var cell = Board[newRow, newCol];
                    
                    // CRITICAL: Only reveal if it's NOT a mine, not already revealed, and not flagged
                    if (!cell.IsMine && !cell.IsRevealed && !cell.IsFlagged)
                    {
                        cell.IsRevealed = true;
                        
                        // If this cell has no adjacent mines, add it to the queue for further expansion
                        if (cell.AdjacentMines == 0)
                        {
                            cellsToReveal.Enqueue((newRow, newCol));
                        }
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