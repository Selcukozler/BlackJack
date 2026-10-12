using BlackJack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlackJack.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Veritabanından son 10 oyunu çekiyoruz
            var gameHistory = await _context.GameHistories
                .Where(g => g.UserId == user.Id)
                .OrderByDescending(g => g.PlayDate)
                .Take(10)
                .ToListAsync();

            ViewBag.History = gameHistory;
            return View(user);
        }
    }
}