using Microsoft.AspNetCore.Mvc;
using Minesweeper.Models;

namespace Minesweeper.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

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
            
            var game = HttpContext.Session.Get<GameBoard>("game") ?? new GameBoard(Difficulty.Easy);
            
            // Log the cell state before revealing
            var cellBefore = game.Board[row, col];
            _logger.LogInformation($"Cell ({row},{col}) before reveal: IsMine={cellBefore.IsMine}, IsRevealed={cellBefore.IsRevealed}, AdjacentMines={cellBefore.AdjacentMines}");
            
            game.RevealCell(row, col);
            HttpContext.Session.Set("game", game);
            
            // Log any mines that got revealed (this should never happen)
            var revealedMines = new List<object>();
            for (int i = 0; i < game.Rows; i++)
            {
                for (int j = 0; j < game.Cols; j++)
                {
                    var cell = game.Board[i, j];
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
            
            // Create a list of all revealed cells to update the UI
            var revealedCells = new List<object>();
            for (int i = 0; i < game.Rows; i++)
            {
                for (int j = 0; j < game.Cols; j++)
                {
                    var cell = game.Board[i, j];
                    if (cell.IsRevealed)
                    {
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
            
            var game = HttpContext.Session.Get<GameBoard>("game") ?? new GameBoard(Difficulty.Easy);
            game.ToggleFlag(row, col);
            HttpContext.Session.Set("game", game);
            
            var cell = game.Board[row, col];
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
            HttpContext.Session.Set("game", game);
            
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
            // Create a fresh game for testing
            var game = new GameBoard(Difficulty.Easy);
            
            // Create a simplified board state for the client
            var boardState = new List<object>();
            int mineCount = 0;
            var minePositions = new List<object>();
            
            for (int i = 0; i < game.Rows; i++)
            {
                for (int j = 0; j < game.Cols; j++)
                {
                    var cell = game.Board[i, j];
                    if (cell.IsMine) 
                    {
                        mineCount++;
                        minePositions.Add(new { row = i, col = j });
                    }
                    
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
            
            var validationErrors = game.ValidateBoard();
            
            return Json(new { 
                success = true, 
                board = boardState,
                gameStatus = game.Status.ToString(),
                isInitialized = game.IsInitialized,
                totalMines = mineCount,
                expectedMines = game.MineCount,
                difficulty = game.CurrentDifficulty.ToString(),
                rows = game.Rows,
                cols = game.Cols,
                minePositions = minePositions,
                validationErrors = validationErrors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetBoardState");
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult TestReveal(int row, int col)
    {
        try
        {
            // Create a fresh game and test the reveal logic
            var game = new GameBoard(Difficulty.Easy);
            
            _logger.LogInformation($"Testing reveal on fresh board at ({row},{col})");
            
            // Log initial state
            var initialCell = game.Board[row, col];
            _logger.LogInformation($"Initial cell state: IsMine={initialCell.IsMine}, AdjacentMines={initialCell.AdjacentMines}");
            
            // Reveal the cell
            game.RevealCell(row, col);
            
            // Count revealed mines (should be 0 unless we clicked on a mine)
            int revealedMines = 0;
            for (int i = 0; i < game.Rows; i++)
            {
                for (int j = 0; j < game.Cols; j++)
                {
                    if (game.Board[i, j].IsRevealed && game.Board[i, j].IsMine)
                    {
                        revealedMines++;
                    }
                }
            }
            
            return Json(new { 
                success = true, 
                clickedCell = new { 
                    row = row, 
                    col = col, 
                    wasMine = initialCell.IsMine,
                    adjacentMines = initialCell.AdjacentMines
                },
                revealedMines = revealedMines,
                gameStatus = game.Status.ToString(),
                message = revealedMines > 1 ? "ERROR: Multiple mines revealed!" : "OK"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in TestReveal: row={row}, col={col}");
            return Json(new { success = false, error = ex.Message });
        }
    }
}