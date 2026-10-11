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
    public class GameController : Controller
    {
        private readonly IBlackjackService _blackjackService;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string GameSessionKey = "CurrentGameState"; 

        public GameController(IBlackjackService blackjackService, UserManager<ApplicationUser> userManager)
        {
            _blackjackService = blackjackService;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Start(int betAmount)
        {
            if (betAmount <= 0) { ModelState.AddModelError("", "Geçerli bir bahis girin."); return View("Index"); }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); 

            if (user.TotalChips < betAmount)
            {
                ModelState.AddModelError("", $"Yetersiz bakiye. Mevcut çipiniz: {user.TotalChips}");
                return View("Index");
            }

            var game = _blackjackService.StartNewGame(betAmount);
            if (game.IsGameOver) await _blackjackService.SaveGameResultAsync(user.Id, game.CurrentBet, game.ChipChange, game.GameMessage);
            
            HttpContext.Session.SetObject(GameSessionKey, game);
            return View("Play", game);
        }

        [HttpPost]
        public async Task<IActionResult> Hit()
        {
            var game = HttpContext.Session.GetObject<GameViewModel>(GameSessionKey);
            if (game == null) return RedirectToAction("Index");

            game = _blackjackService.Hit(game);
            HttpContext.Session.SetObject(GameSessionKey, game);

            if (game.IsGameOver)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null) await _blackjackService.SaveGameResultAsync(user.Id, game.CurrentBet, game.ChipChange, game.GameMessage);
            }
            return View("Play", game);
        }

        [HttpPost]
        public async Task<IActionResult> DoubleDown()
        {
            var game = HttpContext.Session.GetObject<GameViewModel>(GameSessionKey);
            if (game == null) return RedirectToAction("Index");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); 

            // İkiye katlamak için kasada o anki bahsi kadar DAHASI olması lazım
            if (user.TotalChips < game.CurrentBet * 2)
            {
                // UI'da hata göstermek için mesaj ekleyebilirsiniz, şimdilik işlemi iptal edip geri dönüyor
                return View("Play", game);
            }

            game = _blackjackService.DoubleDown(game);
            HttpContext.Session.SetObject(GameSessionKey, game);

            if (game.IsGameOver)
                await _blackjackService.SaveGameResultAsync(user.Id, game.CurrentBet, game.ChipChange, game.GameMessage);

            return View("Play", game);
        }

        [HttpPost]
        public async Task<IActionResult> Split()
        {
            var game = HttpContext.Session.GetObject<GameViewModel>(GameSessionKey);
            if (game == null) return RedirectToAction("Index");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); 

            // Bölmek için de mevcut bahsi kadar ekstra çipe ihtiyaç var
            if (user.TotalChips < game.CurrentBet * 2) return View("Play", game);

            game = _blackjackService.Split(game);
            HttpContext.Session.SetObject(GameSessionKey, game);

            return View("Play", game);
        }

        [HttpPost]
        public async Task<IActionResult> Stand()
        {
            var game = HttpContext.Session.GetObject<GameViewModel>(GameSessionKey);
            if (game == null) return RedirectToAction("Index");

            game = _blackjackService.Stand(game);
            HttpContext.Session.SetObject(GameSessionKey, game);

            if (game.IsGameOver)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null) await _blackjackService.SaveGameResultAsync(user.Id, game.CurrentBet, game.ChipChange, game.GameMessage);
            }
            return View("Play", game);
        }
    }
}