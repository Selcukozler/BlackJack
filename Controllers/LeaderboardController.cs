using BlackJack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlackJack.Controllers
{
    [Authorize]
    public class LeaderboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaderboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Tüm kullanıcıları çip miktarına göre çoktan aza (Descending) sıralayıp ilk 10'unu alıyoruz
            var topPlayers = await _userManager.Users
                .OrderByDescending(u => u.TotalChips)
                .Take(10)
                .ToListAsync();

            return View(topPlayers);
        }
    }
}