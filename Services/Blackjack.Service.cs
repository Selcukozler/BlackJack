using BlackJack.Models;
using BlackJack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BlackJack.Services
{
    public class BlackjackService : IBlackjackService
    {
        private readonly ApplicationDbContext _context;

        public BlackjackService(ApplicationDbContext context)
        {
            _context = context;
        }

        private List<string> GetFullDeck()
        {
            var suits = new[] { "Hearts", "Diamonds", "Clubs", "Spades" };
            var values = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            var deck = new List<string>();
            foreach (var suit in suits)
                foreach (var val in values)
                    deck.Add($"{suit}-{val}");
            return deck;
        }

        private string DrawCard(List<string> cardsInPlay)
        {
            var availableCards = GetFullDeck().Except(cardsInPlay).ToList();
            if (!availableCards.Any()) throw new Exception("Deste bitti!"); 
            return availableCards[new Random().Next(availableCards.Count)];
        }

        public int CalculateScore(List<string> hand)
        {
            int score = 0, aces = 0;
            foreach (var card in hand)
            {
                var value = card.Split('-')[1];
                if (value == "A") { aces++; score += 11; }
                else if (value == "K" || value == "Q" || value == "J") score += 10;
                else score += int.Parse(value);
            }
            while (score > 21 && aces > 0) { score -= 10; aces--; }
            return score;
        }

        public GameViewModel StartNewGame(int betAmount)
        {
            var game = new GameViewModel { CurrentBet = betAmount };
            var cardsInPlay = new List<string>();

            for (int i = 0; i < 2; i++)
            {
                game.PlayerHand.Add(DrawCard(cardsInPlay));
                cardsInPlay.Add(game.PlayerHand.Last());
                game.DealerHand.Add(DrawCard(cardsInPlay));
                cardsInPlay.Add(game.DealerHand.Last());
            }

            game.PlayerScore = CalculateScore(game.PlayerHand);
            game.DealerScore = CalculateScore(game.DealerHand);

            // Kart değerleri aynıysa Split yapılabilir (Örn: 10 ve K da bölünebilir çünkü değerleri 10'dur)
            if (CalculateScore(new List<string> { game.PlayerHand[0] }) == CalculateScore(new List<string> { game.PlayerHand[1] }))
            {
                game.CanSplit = true;
            }

            if (game.PlayerScore == 21)
            {
                game.IsGameOver = true;
                if (game.DealerScore == 21) { game.GameMessage = "Berabere! İkinizde de Blackjack var."; game.ChipChange = 0; }
                else { game.GameMessage = "Blackjack! Kazandınız."; game.ChipChange = (int)(betAmount * 1.5); }
            }
            return game;
        }

        public GameViewModel Hit(GameViewModel game)
        {
            if (game.IsGameOver) return game;
            var cardsInPlay = game.PlayerHand.Concat(game.DealerHand).Concat(game.PlayerHand2).ToList();

            if (game.HasSplit && game.ActiveHand == 2)
            {
                game.PlayerHand2.Add(DrawCard(cardsInPlay));
                game.PlayerScore2 = CalculateScore(game.PlayerHand2);
                if (game.PlayerScore2 > 21) Stand(game); // 2. el de 21'i geçerse sıra kurpiyere/değerlendirmeye geçer
            }
            else
            {
                game.PlayerHand.Add(DrawCard(cardsInPlay));
                game.PlayerScore = CalculateScore(game.PlayerHand);
                if (game.PlayerScore > 21)
                {
                    if (game.HasSplit) game.ActiveHand = 2; // İlk el yandıysa 2. ele geç
                    else
                    {
                        game.IsGameOver = true;
                        game.GameMessage = "21'i geçtiniz! Kaybettiniz (Bust).";
                        game.ChipChange = -game.CurrentBet;
                    }
                }
            }
            return game;
        }

        public GameViewModel DoubleDown(GameViewModel game)
        {
            if (game.IsGameOver || !game.CanDoubleDown) return game;
            game.CurrentBet *= 2;
            game = Hit(game); // Tek kart çek
            if (!game.IsGameOver) game = Stand(game); // Ve zorunlu dur
            return game;
        }

        public GameViewModel Split(GameViewModel game)
        {
            if (game.IsGameOver || !game.CanSplit) return game;
            
            game.HasSplit = true;
            game.CanSplit = false;
            
            // Kartları iki ele ayır
            game.PlayerHand2.Add(game.PlayerHand[1]);
            game.PlayerHand.RemoveAt(1);

            // Her iki ele de birer yeni kart çek
            var cardsInPlay = game.PlayerHand.Concat(game.DealerHand).Concat(game.PlayerHand2).ToList();
            game.PlayerHand.Add(DrawCard(cardsInPlay));
            cardsInPlay.Add(game.PlayerHand.Last());
            game.PlayerHand2.Add(DrawCard(cardsInPlay));

            game.PlayerScore = CalculateScore(game.PlayerHand);
            game.PlayerScore2 = CalculateScore(game.PlayerHand2);
            game.ActiveHand = 1;

            return game;
        }

        public GameViewModel Stand(GameViewModel game)
        {
            if (game.IsGameOver) return game;

            // Split varsa ve 1. elde stand yapıldıysa 2. ele geç
            if (game.HasSplit && game.ActiveHand == 1)
            {
                game.ActiveHand = 2;
                return game;
            }

            var cardsInPlay = game.PlayerHand.Concat(game.DealerHand).Concat(game.PlayerHand2).ToList();

            // Kurpiyer kuralı
            while (game.DealerScore < 17)
            {
                game.DealerHand.Add(DrawCard(cardsInPlay));
                cardsInPlay.Add(game.DealerHand.Last());
                game.DealerScore = CalculateScore(game.DealerHand);
            }

            game.IsGameOver = true;
            EvaluateGame(game);
            return game;
        }

        private void EvaluateGame(GameViewModel game)
        {
            if (game.HasSplit)
            {
                int hand1Change = EvaluateSingleHand(game.PlayerScore, game.DealerScore, game.CurrentBet);
                int hand2Change = EvaluateSingleHand(game.PlayerScore2, game.DealerScore, game.CurrentBet);
                game.ChipChange = hand1Change + hand2Change;
                
                string res1 = hand1Change > 0 ? "Kazandı" : (hand1Change < 0 ? "Kaybetti" : "Berabere");
                string res2 = hand2Change > 0 ? "Kazandı" : (hand2Change < 0 ? "Kaybetti" : "Berabere");
                game.GameMessage = $"1. El: {res1} | 2. El: {res2}";
            }
            else
            {
                game.ChipChange = EvaluateSingleHand(game.PlayerScore, game.DealerScore, game.CurrentBet);
                if (game.ChipChange > 0) game.GameMessage = "Kazandınız!";
                else if (game.ChipChange < 0) game.GameMessage = game.PlayerScore > 21 ? "Bust oldunuz, kaybettiniz." : "Kurpiyer kazandı.";
                else game.GameMessage = "Berabere (Push).";
            }
        }

        private int EvaluateSingleHand(int playerScore, int dealerScore, int bet)
        {
            if (playerScore > 21) return -bet;
            if (dealerScore > 21) return bet;
            if (playerScore > dealerScore) return bet;
            if (playerScore < dealerScore) return -bet;
            return 0;
        }

        public async Task SaveGameResultAsync(string userId, int betAmount, int chipChange, string result)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.TotalChips += chipChange;
                
                _context.GameHistories.Add(new GameHistory
                {
                    UserId = userId,
                    BetAmount = betAmount,
                    Result = result,
                    PlayDate = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}