namespace BlackJack.ViewModels
{
    public class PistiViewModel
    {
        public List<string> PlayerHand { get; set; } = new();
        public List<string> DealerHand { get; set; } = new();
        public List<string> Board { get; set; } = new();
        public List<string> Deck { get; set; } = new();
        
        public List<string> PlayerCaptured { get; set; } = new();
        public List<string> DealerCaptured { get; set; } = new();
        
        public int PlayerScore { get; set; }
        public int DealerScore { get; set; }
        public int PlayerPistiCount { get; set; }
        public int DealerPistiCount { get; set; }
        
        public string LastCapturer { get; set; } = "";
        public int CurrentBet { get; set; }
        public bool IsGameOver { get; set; }
        public string GameMessage { get; set; } = "";
        public int ChipChange { get; set; }

        // SIRALI ANİMASYONLAR İÇİN YENİ ÖZELLİKLER
        public string PlayerLastPlayedCard { get; set; } = "";
        public string DealerLastPlayedCard { get; set; } = "";
        public bool DidPlayerCapture { get; set; }
        public List<string> PlayerCapturedCardsThisTurn { get; set; } = new();
        public bool DidDealerCapture { get; set; }
        public List<string> DealerCapturedCardsThisTurn { get; set; } = new();
    }
}