using Microsoft.AspNetCore.Mvc;
using Minesweeper.Models;

namespace Minesweeper.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var game = new GameBoard(9, 9, 10);
        return View(game);
    }

    [HttpPost]
    public IActionResult RevealCell(int row, int col)
    {
        var game = HttpContext.Session.Get<GameBoard>("game") ?? new GameBoard(9, 9, 10);
        game.RevealCell(row, col);
        HttpContext.Session.Set("game", game);
        return Json(new { success = true, gameState = game });
    }

    [HttpPost]
    public IActionResult ToggleFlag(int row, int col)
    {
        var game = HttpContext.Session.Get<GameBoard>("game") ?? new GameBoard(9, 9, 10);
        game.ToggleFlag(row, col);
        HttpContext.Session.Set("game", game);
        return Json(new { success = true, gameState = game });
    }

    [HttpPost]
    public IActionResult NewGame()
    {
        var game = new GameBoard(9, 9, 10);
        HttpContext.Session.Set("game", game);
        return Json(new { success = true, gameState = game });
    }
}