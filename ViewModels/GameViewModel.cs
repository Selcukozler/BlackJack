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
        public string GameMessage { get; set; } = string.Empty;
        public int ChipChange { get; set; } // El sonunda kazanılan/kaybedilen net çip

        // Double Down & Split Kuralları
        public bool CanDoubleDown => PlayerHand.Count == 2 && !HasSplit;
        public bool CanSplit { get; set; } 
        
        public bool HasSplit { get; set; }
        public int ActiveHand { get; set; } = 1; // 1. el mi yoksa 2. el mi oynanıyor?
        public List<string> PlayerHand2 { get; set; } = new List<string>();
        public int PlayerScore2 { get; set; }
    }
}