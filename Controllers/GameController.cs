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
        private const string GameSessionKey = "CurrentGameState"; // Session için anahtar kelime

        public GameController(IBlackjackService blackjackService, UserManager<ApplicationUser> userManager)
        {
            _blackjackService = blackjackService;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Start(int betAmount)
        {
            if (betAmount <= 0)
            {
                ModelState.AddModelError("", "Lütfen 0'dan büyük bir bahis girin.");
                return View("Index");
            }

            var user = await _userManager.GetUserAsync(User);

            // Bakiye kontrolü (Oyuncunun yeterli çipi var mı?)
            if (user.TotalChips < betAmount)
            {
                ModelState.AddModelError("", $"Yetersiz bakiye. Mevcut çipiniz: {user.TotalChips}");
                return View("Index");
            }

            var game = _blackjackService.StartNewGame(betAmount);

            // Eğer oyun başlar başlamaz bittiyse (İlk iki kartta Blackjack durumu)
            if (game.IsGameOver)
            {
                await _blackjackService.SaveGameResultAsync(user.Id, game.CurrentBet, game.GameMessage);
            }

            // Oyun durumunu JSON olarak Session'a atıyoruz
            HttpContext.Session.SetObject(GameSessionKey, game);
            
            return View("Play", game);
        }

        [HttpPost]
        public async Task<IActionResult> Hit()
        {
            // Session'dan mevcut oyun durumunu çek
            var game = HttpContext.Session.GetObject<GameViewModel>(GameSessionKey);
            if (game == null) return RedirectToAction("Index"); // Oyun yoksa başa dön

            if (game.IsGameOver) return View("Play", game);

            // Servis üzerinden kart çekme işlemini yap
            game = _blackjackService.Hit(game);

            // Yeni durumu Session'a kaydet
            HttpContext.Session.SetObject(GameSessionKey, game);

            // Eğer oyuncu kart çektikten sonra 21'i geçtiyse oyun biter, veritabanına kaydet
            if (game.IsGameOver)
            {
                var user = await _userManager.GetUserAsync(User);
                await _blackjackService.SaveGameResultAsync(user.Id, game.CurrentBet, game.GameMessage);
            }

            return View("Play", game);
        }

        [HttpPost]
        public async Task<IActionResult> Stand()
        {
            var game = HttpContext.Session.GetObject<GameViewModel>(GameSessionKey);
            if (game == null) return RedirectToAction("Index");

            if (game.IsGameOver) return View("Play", game);

            // Servis üzerinden Stand aksiyonunu çalıştır (Kurpiyer kart çeker)
            game = _blackjackService.Stand(game);

            // Oyun mutlaka bittiği için durumu Session'a kaydet
            HttpContext.Session.SetObject(GameSessionKey, game);

            // Sonucu veritabanına yaz
            var user = await _userManager.GetUserAsync(User);
            await _blackjackService.SaveGameResultAsync(user.Id, game.CurrentBet, game.GameMessage);

            return View("Play", game);
        }
    }
}