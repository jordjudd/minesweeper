namespace Minesweeper.Models;

public class GameBoard
{
    public int Rows { get; set; } = 9;
    public int Cols { get; set; } = 9;
    public int MineCount { get; set; } = 10;
    public Cell[,] Board { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Playing;
    public bool IsInitialized { get; set; } = true;
    public Difficulty CurrentDifficulty { get; set; } = Difficulty.Easy;

    public GameBoard(Difficulty difficulty)
    {
        // Force to Easy for now
        Rows = 9;
        Cols = 9;
        MineCount = 10;
        CurrentDifficulty = Difficulty.Easy;
        
        // Initialize board
        Board = new Cell[9, 9];
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                Board[i, j] = new Cell
                {
                    IsMine = false,
                    IsRevealed = false,
                    IsFlagged = false,
                    AdjacentMines = 0
                };
            }
        }
        
        // Place exactly 10 mines
        PlaceMines();
        
        // Calculate numbers
        CalculateNumbers();
    }

    private void PlaceMines()
    {
        var random = new Random();
        int minesPlaced = 0;
        
        while (minesPlaced < 10)
        {
            int row = random.Next(0, 9);
            int col = random.Next(0, 9);
            
            if (!Board[row, col].IsMine)
            {
                Board[row, col].IsMine = true;
                minesPlaced++;
            }
        }
    }

    private void CalculateNumbers()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (!Board[row, col].IsMine)
                {
                    int count = 0;
                    
                    // Check all 8 adjacent cells
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            if (dr == 0 && dc == 0) continue; // Skip center
                            
                            int newRow = row + dr;
                            int newCol = col + dc;
                            
                            if (newRow >= 0 && newRow < 9 && newCol >= 0 && newCol < 9)
                            {
                                if (Board[newRow, newCol].IsMine)
                                {
                                    count++;
                                }
                            }
                        }
                    }
                    
                    Board[row, col].AdjacentMines = count;
                }
            }
        }
    }

    public void RevealCell(int row, int col)
    {
        // Basic validation
        if (row < 0 || row >= 9 || col < 0 || col >= 9) return;
        if (Board[row, col].IsRevealed) return;
        if (Board[row, col].IsFlagged) return;
        if (Status != GameStatus.Playing) return;

        // Check if it's a mine
        if (Board[row, col].IsMine)
        {
            Board[row, col].IsRevealed = true;
            Status = GameStatus.Lost;
            
            // Reveal all mines when game is lost
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (Board[i, j].IsMine)
                    {
                        Board[i, j].IsRevealed = true;
                    }
                }
            }
            return;
        }

        // Only reveal this single cell - NO CASCADE
        Board[row, col].IsRevealed = true;

        // Check win condition
        CheckWinCondition();
    }

    public void ToggleFlag(int row, int col)
    {
        if (row < 0 || row >= 9 || col < 0 || col >= 9) return;
        if (Board[row, col].IsRevealed) return;
        if (Status != GameStatus.Playing) return;

        Board[row, col].IsFlagged = !Board[row, col].IsFlagged;
    }

    private void CheckWinCondition()
    {
        int revealedSafeCells = 0;
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (Board[i, j].IsRevealed && !Board[i, j].IsMine)
                {
                    revealedSafeCells++;
                }
            }
        }

        if (revealedSafeCells == (81 - 10)) // 9x9 - 10 mines = 71 safe cells
        {
            Status = GameStatus.Won;
        }
    }

    public List<string> ValidateBoard()
    {
        var errors = new List<string>();
        
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (!Board[i, j].IsMine)
                {
                    int actualCount = 0;
                    
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            if (dr == 0 && dc == 0) continue;
                            
                            int newRow = i + dr;
                            int newCol = j + dc;
                            
                            if (newRow >= 0 && newRow < 9 && newCol >= 0 && newCol < 9)
                            {
                                if (Board[newRow, newCol].IsMine)
                                {
                                    actualCount++;
                                }
                            }
                        }
                    }
                    
                    if (Board[i, j].AdjacentMines != actualCount)
                    {
                        errors.Add($"Cell ({i},{j}): Expected {actualCount} adjacent mines, but has {Board[i, j].AdjacentMines}");
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