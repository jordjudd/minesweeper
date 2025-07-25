using Microsoft.AspNetCore.Mvc;
using Minesweeper.Models;

namespace Minesweeper.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private static GameBoard? _staticGame; // Temporary static storage for testing

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index(string difficulty = "Easy", int rows = 9, int cols = 9, int mines = 10)
    {
        try
        {
            Console.WriteLine($"=== Controller Index() called with difficulty: {difficulty} ===");
            
            GameBoard game;
            if (difficulty == "Custom")
            {
                Console.WriteLine($"Creating custom game: {rows}x{cols} with {mines} mines");
                game = new GameBoard(rows, cols, mines);
            }
            else if (Enum.TryParse<Difficulty>(difficulty, out var diff))
            {
                Console.WriteLine($"Creating {diff} game");
                game = new GameBoard(diff);
            }
            else
            {
                Console.WriteLine("Creating default Easy game");
                game = new GameBoard(Difficulty.Easy);
            }
            
            _staticGame = game; // Initialize static game
            Console.WriteLine($"✅ Game created: {game.Rows}x{game.Cols} with {game.MineCount} mines, difficulty: {game.CurrentDifficulty}");
            return View(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Index action");
            Console.WriteLine($"❌ Error in Index: {ex.Message}");
            var fallbackGame = new GameBoard(Difficulty.Easy);
            _staticGame = fallbackGame;
            return View(fallbackGame);
        }
    }

    [HttpPost]
    public IActionResult RevealCell(int row, int col)
    {
        try
        {
            _logger.LogInformation($"RevealCell called with row={row}, col={col}");
            
            // Use static storage for game state
            var game = _staticGame ?? new GameBoard(Difficulty.Easy);
            if (_staticGame == null)
            {
                _staticGame = game;
            }
            
            // Log the cell state before revealing
            var cellBefore = game.Board[row][col];
            _logger.LogInformation($"Cell ({row},{col}) before reveal: IsMine={cellBefore.IsMine}, IsRevealed={cellBefore.IsRevealed}, AdjacentMines={cellBefore.AdjacentMines}");
            
            game.RevealCell(row, col);
            
            // Update static game
            _staticGame = game;
            
            // Log any mines that got revealed (this should never happen)
            var revealedMines = new List<object>();
            for (int i = 0; i < game.Rows; i++)
            {
                for (int j = 0; j < game.Cols; j++)
                {
                    var cell = game.Board[i][j];
                    if (cell.IsRevealed && cell.IsMine)
                    {
                        revealedMines.Add(new { row = i, col = j });
                    }
                }
            }
            
            if (revealedMines.Count > 0)
            {
                _logger.LogError($"MINES REVEALED DURING CASCADE: {string.Join(", ", revealedMines.Select(m => $"({((dynamic)m).row},{((dynamic)m).col})"))}");
            }
            
            // Create a list of revealed cells to update the UI
            var revealedCells = new List<object>();
            for (int i = 0; i < game.Rows; i++)
            {
                for (int j = 0; j < game.Cols; j++)
                {
                    var cell = game.Board[i][j];
                    if (cell.IsRevealed)
                    {
                        // During normal play (cascade), never send mines to client
                        // Only send mines when game is lost (all mines revealed)
                        if (cell.IsMine && game.Status == GameStatus.Playing)
                        {
                            _logger.LogError($"CRITICAL: Mine at ({i},{j}) was revealed during cascade!");
                            continue; // Skip mines during cascade
                        }
                        
                        revealedCells.Add(new
                        {
                            row = i,
                            col = j,
                            isRevealed = cell.IsRevealed,
                            isMine = cell.IsMine,
                            isFlagged = cell.IsFlagged,
                            adjacentMines = cell.AdjacentMines
                        });
                    }
                }
            }
            
            // If game is lost, send all mine positions to show them
            var allMines = new List<object>();
            if (game.Status == GameStatus.Lost)
            {
                var minePositions = game.GetMinePositions();
                foreach (var mine in minePositions)
                {
                    allMines.Add(new { row = mine.row, col = mine.col });
                }
            }

            return Json(new { 
                success = true, 
                clickedRow = row, 
                clickedCol = col,
                revealedCells = revealedCells,
                gameStatus = game.Status.ToString(),
                revealedMinesCount = revealedMines.Count,
                allMines = allMines
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in RevealCell: row={row}, col={col}");
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult ToggleFlag(int row, int col)
    {
        try
        {
            _logger.LogInformation($"ToggleFlag called with row={row}, col={col}");
            
            var game = _staticGame ?? new GameBoard(Difficulty.Easy);
            if (_staticGame == null) _staticGame = game;
            
            game.ToggleFlag(row, col);
            _staticGame = game;
            
            var cell = game.Board[row][col];
            return Json(new { 
                success = true, 
                row = row, 
                col = col,
                isFlagged = cell.IsFlagged,
                gameStatus = game.Status.ToString(),
                flagCount = game.GetFlagCount()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in ToggleFlag: row={row}, col={col}");
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult NewGame(string difficulty = "Easy")
    {
        try
        {
            _logger.LogInformation($"NewGame called with difficulty: {difficulty}");
            Console.WriteLine($"🎮 NewGame called with difficulty: {difficulty}");
            
            if (Enum.TryParse<Difficulty>(difficulty, out var diff))
            {
                var game = new GameBoard(diff);
                _staticGame = game;
                Console.WriteLine($"✅ Created {diff} game: {game.Rows}x{game.Cols} with {game.MineCount} mines");
                return Json(new { success = true, message = $"New {diff} game started" });
            }
            else
            {
                Console.WriteLine($"❌ Invalid difficulty: {difficulty}");
                return Json(new { success = false, error = "Invalid difficulty" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in NewGame");
            Console.WriteLine($"❌ Error in NewGame: {ex.Message}");
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult NewCustomGame(int rows, int cols, int mines)
    {
        try
        {
            _logger.LogInformation($"NewCustomGame called with rows: {rows}, cols: {cols}, mines: {mines}");
            
            var game = new GameBoard(rows, cols, mines);
            _staticGame = game;
            
            return Json(new { 
                success = true, 
                message = "Custom game started",
                actualRows = game.Rows,
                actualCols = game.Cols,
                actualMines = game.MineCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in NewCustomGame");
            return Json(new { success = false, error = ex.Message });
        }
    }




}