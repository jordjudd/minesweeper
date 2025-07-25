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

    public IActionResult Index()
    {
        try
        {
            Console.WriteLine("=== Controller Index() called ===");
            var game = new GameBoard(Difficulty.Easy);
            Console.WriteLine("=== Controller Index() returning view ===");
            return View(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Index action");
            var fallbackGame = new GameBoard(Difficulty.Easy);
            return View(fallbackGame);
        }
    }

    [HttpPost]
    public IActionResult RevealCell(int row, int col)
    {
        try
        {
            _logger.LogInformation($"RevealCell called with row={row}, col={col}");
            
            // Use static storage for testing instead of session
            var game = _staticGame ?? new GameBoard(Difficulty.Easy);
            if (_staticGame == null)
            {
                _staticGame = game;
                _logger.LogInformation("Created new static game");
            }
            else
            {
                _logger.LogInformation("Using existing static game");
            }
            
            // Log the cell state before revealing
            var cellBefore = game.Board[row][col];
            _logger.LogInformation($"Cell ({row},{col}) before reveal: IsMine={cellBefore.IsMine}, IsRevealed={cellBefore.IsRevealed}, AdjacentMines={cellBefore.AdjacentMines}");
            
            game.RevealCell(row, col);
            
            // Debug: Check revealed cells before saving to session
            int revealedCount = 0;
            for (int i = 0; i < game.Rows; i++)
            {
                for (int j = 0; j < game.Cols; j++)
                {
                    if (game.Board[i][j].IsRevealed) revealedCount++;
                }
            }
            _logger.LogInformation($"Before saving to session: {revealedCount} cells revealed");
            
            // Update static game
            _staticGame = game;
            _logger.LogInformation("Updated static game");
            
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
            
            return Json(new { 
                success = true, 
                clickedRow = row, 
                clickedCol = col,
                revealedCells = revealedCells,
                gameStatus = game.Status.ToString(),
                revealedMinesCount = revealedMines.Count
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
                gameStatus = game.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in ToggleFlag: row={row}, col={col}");
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult NewGame()
    {
        try
        {
            _logger.LogInformation("NewGame called");
            
            var game = new GameBoard(Difficulty.Easy);
            _staticGame = game;
            
            return Json(new { success = true, message = "New game started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in NewGame");
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetBoardState()
    {
        try
        {
            // Use static game for testing
            var game = _staticGame;
            bool usingSession = game != null;
            if (game == null)
            {
                game = new GameBoard(Difficulty.Easy);
                _staticGame = game;
                _logger.LogWarning("GetBoardState: No static game found, created new game");
            }
            else
            {
                _logger.LogInformation("GetBoardState: Using static game");
            }
            
            // Get detailed mine information
            var detailedMineInfo = game.GetDetailedMineInfo();
            var boardStats = game.GetBoardStatistics();
            var validationErrors = game.ValidateBoard();
            
            // Create a simplified board state for the client
            var boardState = new List<object>();
            
            for (int i = 0; i < game.Rows; i++)
            {
                for (int j = 0; j < game.Cols; j++)
                {
                    var cell = game.Board[i][j];
                    
                    boardState.Add(new
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
            
            return Json(new { 
                success = true, 
                board = boardState,
                gameStatus = game.Status.ToString(),
                isInitialized = game.IsInitialized,
                difficulty = game.CurrentDifficulty.ToString(),
                rows = game.Rows,
                cols = game.Cols,
                detailedMineInfo = detailedMineInfo,
                boardStatistics = boardStats,
                validationErrors = validationErrors,
                usingStaticGame = usingSession
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetBoardState");
            return Json(new { success = false, error = ex.Message });
        }
    }


}