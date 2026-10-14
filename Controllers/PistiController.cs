using BlackJack.Extensions;
using BlackJack.Models;
using BlackJack.Services;
using BlackJack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlackJack.Controllers
{
    [Authorize]
    public class PistiController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPistiService _pistiService;
        private const string PistiSessionKey = "PistiGameState";

        public PistiController(UserManager<ApplicationUser> userManager, IPistiService pistiService)
        {
            _userManager = userManager;
            _pistiService = pistiService;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Start(int betAmount)
        {
            if (betAmount <= 0) return RedirectToAction("Index");

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.TotalChips < betAmount)
            {
                ModelState.AddModelError("", "Yetersiz bakiye.");
                return View("Index");
            }

            var game = _pistiService.StartNewGame(betAmount);
            HttpContext.Session.SetObject(PistiSessionKey, game);
            return View("Play", game);
        }

        [HttpPost]
        public async Task<IActionResult> PlayCard(string card)
        {
            var game = HttpContext.Session.GetObject<PistiViewModel>(PistiSessionKey);
            if (game == null) return RedirectToAction("Index");

            game = _pistiService.PlayCard(game, card);
            HttpContext.Session.SetObject(PistiSessionKey, game);

            if (game.IsGameOver)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null) await _pistiService.SaveGameResultAsync(user.Id, game.CurrentBet, game.ChipChange, game.GameMessage);
            }

            return View("Play", game);
        }
    }
}