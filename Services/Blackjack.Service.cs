using BlackJack.Models;
using BlackJack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BlackJack.Services
{
    public class BlackjackService : IBlackjackService
    {
        private readonly ApplicationDbContext _context;

        // Dependency Injection ile DbContext alıyoruz (EF Core kullanımı)
        public BlackjackService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Standart 52'lik desteyi oluşturur (Örn format: "Hearts-10", "Spades-A")
        private List<string> GetFullDeck()
        {
            var suits = new[] { "Hearts", "Diamonds", "Clubs", "Spades" };
            var values = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            
            var deck = new List<string>();
            foreach (var suit in suits)
            {
                foreach (var val in values)
                {
                    deck.Add($"{suit}-{val}");
                }
            }
            return deck;
        }

        // Masada zaten bulunan kartları (cardsInPlay) desteden çıkarıp rastgele 1 kart verir
        private string DrawCard(List<string> cardsInPlay)
        {
            var fullDeck = GetFullDeck();
            
            // LINQ ile masadaki kartları desteden filtreliyoruz
            var availableCards = fullDeck.Except(cardsInPlay).ToList();
            
            if (!availableCards.Any())
                throw new Exception("Deste bitti!"); 

            var rand = new Random();
            int index = rand.Next(availableCards.Count);
            return availableCards[index];
        }

        // Eldeki kartların Blackjack kurallarına göre skorunu hesaplar
        public int CalculateScore(List<string> hand)
        {
            int score = 0;
            int aces = 0;

            foreach (var card in hand)
            {
                var value = card.Split('-')[1]; // Örn: "Hearts-10" içinden "10" değerini alır

                if (value == "A")
                {
                    aces++;
                    score += 11;
                }
                else if (value == "K" || value == "Q" || value == "J")
                {
                    score += 10;
                }
                else
                {
                    score += int.Parse(value);
                }
            }

            // As (Ace) mantığı: Skor 21'i geçerse as değerini 11 yerine 1 yap (10 çıkar)
            while (score > 21 && aces > 0)
            {
                score -= 10;
                aces--;
            }

            return score;
        }

        public GameViewModel StartNewGame(int betAmount)
        {
            var game = new GameViewModel
            {
                CurrentBet = betAmount,
                PlayerHand = new List<string>(),
                DealerHand = new List<string>(),
                IsGameOver = false,
                GameMessage = "Oyun başladı! Kart çek (Hit) veya dur (Stand)."
            };

            var cardsInPlay = new List<string>();

            // Hem oyuncuya hem kurpiyere 2'şer kart dağıtılıyor
            for (int i = 0; i < 2; i++)
            {
                var playerCard = DrawCard(cardsInPlay);
                game.PlayerHand.Add(playerCard);
                cardsInPlay.Add(playerCard);

                var dealerCard = DrawCard(cardsInPlay);
                game.DealerHand.Add(dealerCard);
                cardsInPlay.Add(dealerCard);
            }

            game.PlayerScore = CalculateScore(game.PlayerHand);
            game.DealerScore = CalculateScore(game.DealerHand);

            // Başlangıçta Blackjack yapma ihtimali kontrolü
            if (game.PlayerScore == 21)
            {
                game.IsGameOver = true;
                if (game.DealerScore == 21)
                    game.GameMessage = "Berabere! İkinizde de Blackjack var.";
                else
                    game.GameMessage = "Blackjack! Kazandınız.";
            }

            return game;
        }

        public GameViewModel Hit(GameViewModel game)
        {
            if (game.IsGameOver) return game;

            var cardsInPlay = game.PlayerHand.Concat(game.DealerHand).ToList();
            var newCard = DrawCard(cardsInPlay);
            
            game.PlayerHand.Add(newCard);
            game.PlayerScore = CalculateScore(game.PlayerHand);

            if (game.PlayerScore > 21)
            {
                game.IsGameOver = true;
                game.GameMessage = "21'i geçtiniz! Kaybettiniz (Bust).";
            }

            return game;
        }

        public GameViewModel Stand(GameViewModel game)
        {
            if (game.IsGameOver) return game;

            var cardsInPlay = game.PlayerHand.Concat(game.DealerHand).ToList();

            // Kurpiyer kuralı: Skoru 17 veya daha büyük olana kadar zorunlu kart çeker
            while (game.DealerScore < 17)
            {
                var newCard = DrawCard(cardsInPlay);
                game.DealerHand.Add(newCard);
                cardsInPlay.Add(newCard);
                game.DealerScore = CalculateScore(game.DealerHand);
            }

            game.IsGameOver = true;

            // Sonuçları karşılaştırma
            if (game.DealerScore > 21)
            {
                game.GameMessage = "Kurpiyer 21'i geçti (Bust)! Kazandınız.";
            }
            else if (game.DealerScore > game.PlayerScore)
            {
                game.GameMessage = "Kurpiyer kazandı.";
            }
            else if (game.DealerScore < game.PlayerScore)
            {
                game.GameMessage = "Kazandınız!";
            }
            else
            {
                game.GameMessage = "Berabere (Push).";
            }

            return game;
        }

        // Oyun bittiğinde çağrılacak veritabanı kayıt işlemi
        public async Task SaveGameResultAsync(string userId, int betAmount, string result)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                // Bakiye (Chip) hesaplaması
                if (result.Contains("Kazan"))
                {
                    user.TotalChips += betAmount; 
                }
                else if (result.Contains("Kaybet") || result.Contains("Bust") || result.Contains("Kurpiyer kazandı"))
                {
                    user.TotalChips -= betAmount;
                }
                // Berabere durumunda bakiye değişmez

                var history = new GameHistory
                {
                    UserId = userId,
                    BetAmount = betAmount,
                    Result = result,
                    PlayDate = DateTime.Now
                };

                _context.GameHistories.Add(history);
                await _context.SaveChangesAsync(); // Değişiklikleri SQL Server'a kaydet
            }
        }
    }
}