namespace BlackJack.ViewModels
{
    public class GameViewModel
    {
        public List<string> PlayerHand { get; set; } = new List<string>();
        public List<string> DealerHand { get; set; } = new List<string>();
        public int PlayerScore { get; set; }
        public int DealerScore { get; set; }
        public int CurrentBet { get; set; }
        public bool IsGameOver { get; set; }
        public string GameMessage { get; set; } = string.Empty; // Uyarı Giderildi
    }
}