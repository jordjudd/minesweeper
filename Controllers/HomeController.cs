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
            var game = new GameBoard(9, 9, 10);
            HttpContext.Session.Set("game", game);
            return View(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Index action");
            var fallbackGame = new GameBoard(9, 9, 10);
            return View(fallbackGame);
        }
    }

    [HttpPost]
    public IActionResult RevealCell(int row, int col)
    {
        try
        {
            _logger.LogInformation($"RevealCell called with row={row}, col={col}");
            
            var game = HttpContext.Session.Get<GameBoard>("game") ?? new GameBoard(9, 9, 10);
            game.RevealCell(row, col);
            HttpContext.Session.Set("game", game);
            
            // Return the updated cell information
            var cell = game.Board[row, col];
            return Json(new { 
                success = true, 
                row = row, 
                col = col,
                isRevealed = cell.IsRevealed,
                isMine = cell.IsMine,
                isFlagged = cell.IsFlagged,
                adjacentMines = cell.AdjacentMines,
                gameStatus = game.Status.ToString()
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
            
            var game = HttpContext.Session.Get<GameBoard>("game") ?? new GameBoard(9, 9, 10);
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
            
            var game = new GameBoard(9, 9, 10);
            HttpContext.Session.Set("game", game);
            
            return Json(new { success = true, gameState = game });
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
            var game = HttpContext.Session.Get<GameBoard>("game") ?? new GameBoard(9, 9, 10);
            
            // Create a simplified board state for the client
            var boardState = new object[game.Rows, game.Cols];
            for (int i = 0; i < game.Rows; i++)
            {
                for (int j = 0; j < game.Cols; j++)
                {
                    var cell = game.Board[i, j];
                    boardState[i, j] = new
                    {
                        isRevealed = cell.IsRevealed,
                        isMine = cell.IsMine,
                        isFlagged = cell.IsFlagged,
                        adjacentMines = cell.AdjacentMines
                    };
                }
            }
            
            return Json(new { 
                success = true, 
                board = boardState,
                gameStatus = game.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetBoardState");
            return Json(new { success = false, error = ex.Message });
        }
    }
}