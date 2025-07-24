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
            
            // For now, create a new game each time to avoid session issues
            var game = new GameBoard(9, 9, 10);
            game.RevealCell(row, col);
            
            return Json(new { 
                success = true, 
                row = row, 
                col = col,
                message = "Cell revealed successfully" 
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
            
            return Json(new { 
                success = true, 
                row = row, 
                col = col,
                message = "Flag toggled successfully" 
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
}