namespace Minesweeper.Models;

public class GameBoard
{
    public int Rows { get; set; } = 9;
    public int Cols { get; set; } = 9;
    public int MineCount { get; set; } = 10;
    public Cell[][] Board { get; set; } = null!;
    public GameStatus Status { get; set; } = GameStatus.Playing;
    public bool IsInitialized { get; set; } = true;
    public Difficulty CurrentDifficulty { get; set; } = Difficulty.Easy;

    public GameBoard(Difficulty difficulty)
    {
        // Set difficulty settings
        var settings = DifficultySettings.GetSettings(difficulty);
        Rows = settings.rows;
        Cols = settings.cols;
        MineCount = settings.mines;
        CurrentDifficulty = difficulty;
        
        InitializeBoard();
    }

    public GameBoard(int rows, int cols, int mineCount)
    {
        // Custom settings
        Rows = Math.Max(5, Math.Min(30, rows));     // Clamp rows between 5-30
        Cols = Math.Max(5, Math.Min(50, cols));     // Clamp cols between 5-50
        
        // Ensure mine count is reasonable (at least 1, max 80% of total cells)
        int maxMines = (int)(Rows * Cols * 0.8);
        MineCount = Math.Max(1, Math.Min(maxMines, mineCount));
        
        CurrentDifficulty = Difficulty.Custom;
        
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        // Initialize board
        Board = new Cell[Rows][];
        for (int i = 0; i < Rows; i++)
        {
            Board[i] = new Cell[Cols];
            for (int j = 0; j < Cols; j++)
            {
                Board[i][j] = new Cell
                {
                    IsMine = false,
                    IsRevealed = false,
                    IsFlagged = false,
                    AdjacentMines = 0
                };
            }
        }
        
        // Place mines
        PlaceMines();
        
        // Calculate numbers
        CalculateNumbers();
    }

    private void PlaceMines()
    {
        var random = new Random();
        int minesPlaced = 0;
        
        while (minesPlaced < MineCount)
        {
            int row = random.Next(0, Rows);
            int col = random.Next(0, Cols);
            
            if (!Board[row][col].IsMine)
            {
                Board[row][col].IsMine = true;
                minesPlaced++;
            }
        }
    }

    private void CalculateNumbers()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                // Only calculate numbers for non-mine cells
                if (!Board[row][col].IsMine)
                {
                    int count = 0;
                    
                    // Check all 8 adjacent cells
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            if (dr == 0 && dc == 0) continue; // Skip center cell
                            
                            int newRow = row + dr;
                            int newCol = col + dc;
                            
                            // Check bounds properly
                            if (newRow >= 0 && newRow < Rows && newCol >= 0 && newCol < Cols)
                            {
                                if (Board[newRow][newCol].IsMine)
                                {
                                    count++;
                                }
                            }
                        }
                    }
                    
                    Board[row][col].AdjacentMines = count;
                }
                else
                {
                    // Mines should have AdjacentMines = 0 (not used, but for clarity)
                    Board[row][col].AdjacentMines = 0;
                }
            }
        }
    }

    public void RevealCell(int row, int col)
    {
        // Basic validation
        if (row < 0 || row >= Rows || col < 0 || col >= Cols) return;
        if (Board[row][col].IsRevealed) return;
        if (Board[row][col].IsFlagged) return;
        if (Status != GameStatus.Playing) return;

        // Check if it's a mine
        if (Board[row][col].IsMine)
        {
            Board[row][col].IsRevealed = true;
            Status = GameStatus.Lost;
            
            // Reveal all mines when game is lost
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    if (Board[i][j].IsMine)
                    {
                        Board[i][j].IsRevealed = true;
                    }
                }
            }
            return;
        }

        // Reveal this cell and cascade if it has 0 adjacent mines
        RevealCellRecursive(row, col);

        // Check win condition
        CheckWinCondition();
    }

    private void RevealCellRecursive(int row, int col)
    {
        // Basic validation - check bounds first
        if (row < 0 || row >= Rows || col < 0 || col >= Cols) return;
        
        // CRITICAL: Check if this is a mine FIRST - never reveal mines during cascade
        if (Board[row][col].IsMine) 
        {
            return;
        }
        
        // Don't reveal if already revealed or flagged
        if (Board[row][col].IsRevealed) return;
        if (Board[row][col].IsFlagged) return;

        // Reveal this cell (only safe cells get here)
        Board[row][col].IsRevealed = true;

        // If this cell has 0 adjacent mines, cascade to reveal adjacent cells
        if (Board[row][col].AdjacentMines == 0)
        {
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue; // Skip center cell
                    
                    int newRow = row + dr;
                    int newCol = col + dc;
                    
                    // Recursively reveal adjacent cells (they will be checked for mines)
                    RevealCellRecursive(newRow, newCol);
                }
            }
        }
    }

    public void ToggleFlag(int row, int col)
    {
        if (row < 0 || row >= Rows || col < 0 || col >= Cols) return;
        if (Board[row][col].IsRevealed) return;
        if (Status != GameStatus.Playing) return;

        Board[row][col].IsFlagged = !Board[row][col].IsFlagged;
    }

    private void CheckWinCondition()
    {
        int revealedSafeCells = 0;
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (Board[i][j].IsRevealed && !Board[i][j].IsMine)
                {
                    revealedSafeCells++;
                }
            }
        }

        int totalSafeCells = (Rows * Cols) - MineCount;
        if (revealedSafeCells == totalSafeCells)
        {
            Status = GameStatus.Won;
        }
    }

    public List<string> ValidateBoard()
    {
        var errors = new List<string>();
        
        // First, count actual mines on the board
        int actualMineCount = 0;
        var minePositions = new List<string>();
        
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (Board[i][j].IsMine)
                {
                    actualMineCount++;
                    minePositions.Add($"({i},{j})");
                }
            }
        }
        
        if (actualMineCount != MineCount)
        {
            errors.Add($"Expected {MineCount} mines, but found {actualMineCount} mines at positions: {string.Join(", ", minePositions)}");
        }
        
        // Then validate number calculations for non-mine cells
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (!Board[i][j].IsMine)
                {
                    int actualCount = 0;
                    
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            if (dr == 0 && dc == 0) continue; // Skip center cell
                            
                            int newRow = i + dr;
                            int newCol = j + dc;
                            
                            if (newRow >= 0 && newRow < Rows && newCol >= 0 && newCol < Cols)
                            {
                                if (Board[newRow][newCol].IsMine)
                                {
                                    actualCount++;
                                }
                            }
                        }
                    }
                    
                    if (Board[i][j].AdjacentMines != actualCount)
                    {
                        errors.Add($"Cell ({i},{j}): Expected {actualCount} adjacent mines, but has {Board[i][j].AdjacentMines}");
                    }
                }
            }
        }
        
        return errors;
    }

    public int GetActualMineCount()
    {
        int count = 0;
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (Board[i][j].IsMine)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public List<(int row, int col)> GetMinePositions()
    {
        var positions = new List<(int row, int col)>();
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (Board[i][j].IsMine)
                {
                    positions.Add((i, j));
                }
            }
        }
        return positions;
    }

    public int GetFlagCount()
    {
        int flagCount = 0;
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (Board[i][j].IsFlagged)
                {
                    flagCount++;
                }
            }
        }
        return flagCount;
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
    Hard,
    Custom
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
            Difficulty.Custom => (9, 9, 10),   // Default for custom, will be overridden
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
            Difficulty.Custom => "Custom",
            _ => "Easy"
        };
    }
}