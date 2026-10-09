using System.ComponentModel.DataAnnotations;

namespace BlackJack.Models
{
    public class GameHistory
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty; // Uyarı Giderildi
        public ApplicationUser? User { get; set; } // '?' eklendi
        
        public int BetAmount { get; set; }
        public string Result { get; set; } = string.Empty; // Uyarı Giderildi
        public DateTime PlayDate { get; set; } = DateTime.Now;
    }
}