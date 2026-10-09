using Microsoft.AspNetCore.Identity;

namespace BlackJack.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int TotalChips { get; set; } = 1000;
        // Liste başlangıçta null olmasın diye boş liste atıyoruz:
        public ICollection<GameHistory> GameHistories { get; set; } = new List<GameHistory>(); 
    }
}