using BlackJack.Models;
using BlackJack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BlackJack.Services
{
    public class PistiService : IPistiService
    {
        private readonly ApplicationDbContext _context;

        public PistiService(ApplicationDbContext context)
        {
            _context = context;
        }

        private List<string> GetShuffledDeck()
        {
            var suits = new[] { "Hearts", "Diamonds", "Clubs", "Spades" };
            var values = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            var deck = suits.SelectMany(s => values.Select(v => $"{s}-{v}")).OrderBy(x => Guid.NewGuid()).ToList();
            return deck;
        }

        public PistiViewModel StartNewGame(int betAmount)
        {
            var game = new PistiViewModel { CurrentBet = betAmount, Deck = GetShuffledDeck() };
            for (int i = 0; i < 4; i++) { game.Board.Add(game.Deck[0]); game.Deck.RemoveAt(0); }
            DealCards(game);
            return game;
        }

        private void DealCards(PistiViewModel game)
        {
            if (!game.Deck.Any()) return;
            for (int i = 0; i < 4; i++) { game.PlayerHand.Add(game.Deck[0]); game.Deck.RemoveAt(0); }
            for (int i = 0; i < 4; i++) { game.DealerHand.Add(game.Deck[0]); game.Deck.RemoveAt(0); }
        }

        public PistiViewModel PlayCard(PistiViewModel game, string card)
        {
            if (game.IsGameOver) return game;

            // Her tur başında animasyon verilerini sıfırla
            game.PlayerLastPlayedCard = card;
            game.DealerLastPlayedCard = "";
            game.DidPlayerCapture = false;
            game.PlayerCapturedCardsThisTurn.Clear();
            game.DidDealerCapture = false;
            game.DealerCapturedCardsThisTurn.Clear();

            // 1. Oyuncu Hamlesi
            game.PlayerHand.Remove(card);
            ProcessTurn(game, card, true);

            // 2. Kurpiyer (Bilgisayar) Hamlesi
            if (!game.IsGameOver && game.DealerHand.Any())
            {
                string dealerCard = ChooseDealerCard(game.DealerHand, game.Board.LastOrDefault());
                game.DealerLastPlayedCard = dealerCard;
                game.DealerHand.Remove(dealerCard);
                ProcessTurn(game, dealerCard, false);
            }

            // 3. Eller bittiyse yeni kart dağıt veya oyunu bitir
            if (!game.PlayerHand.Any() && !game.DealerHand.Any())
            {
                if (game.Deck.Any()) DealCards(game);
                else EndGame(game);
            }

            // Canlı Puanları Hesapla
            if (!game.IsGameOver)
            {
                game.PlayerScore = CalculateScore(game.PlayerCaptured) + (game.PlayerPistiCount * 10);
                game.DealerScore = CalculateScore(game.DealerCaptured) + (game.DealerPistiCount * 10);
            }

            return game;
        }

        private void ProcessTurn(PistiViewModel game, string playedCard, bool isPlayer)
        {
            string playedValue = playedCard.Split('-')[1];
            string topBoardCard = game.Board.LastOrDefault();
            string topValue = topBoardCard?.Split('-')[1];

            if (topBoardCard != null && (playedValue == topValue || playedValue == "J"))
            {
                if (game.Board.Count == 1 && playedValue != "J") {
                    if (isPlayer) game.PlayerPistiCount++; else game.DealerPistiCount++;
                }
                else if (game.Board.Count == 1 && playedValue == "J" && topValue == "J") {
                    if (isPlayer) game.PlayerPistiCount += 2; else game.DealerPistiCount += 2;
                }

                game.Board.Add(playedCard);
                
                // Kimin aldığını animasyon için işaretle
                if (isPlayer) {
                    game.DidPlayerCapture = true;
                    game.PlayerCapturedCardsThisTurn.AddRange(game.Board);
                    game.PlayerCaptured.AddRange(game.Board);
                    game.LastCapturer = "Player";
                } else {
                    game.DidDealerCapture = true;
                    game.DealerCapturedCardsThisTurn.AddRange(game.Board);
                    game.DealerCaptured.AddRange(game.Board);
                    game.LastCapturer = "Dealer";
                }
                game.Board.Clear();
            }
            else
            {
                game.Board.Add(playedCard);
            }
        }

        private string ChooseDealerCard(List<string> hand, string topBoardCard)
        {
            string topValue = topBoardCard?.Split('-')[1];
            var match = hand.FirstOrDefault(c => c.Split('-')[1] == topValue);
            if (match != null) return match;

            var jack = hand.FirstOrDefault(c => c.Split('-')[1] == "J");
            if (jack != null && topBoardCard != null) return jack;

            return hand.FirstOrDefault(c => c.Split('-')[1] != "J") ?? hand.First();
        }

        private void EndGame(PistiViewModel game)
        {
            game.IsGameOver = true;
            if (game.Board.Any())
            {
                // Yerde kalan kartları son alana uçması için loga ekliyoruz
                if (game.LastCapturer == "Player") {
                    game.DidPlayerCapture = true;
                    game.PlayerCapturedCardsThisTurn.AddRange(game.Board);
                    game.PlayerCaptured.AddRange(game.Board);
                } else {
                    game.DidDealerCapture = true;
                    game.DealerCapturedCardsThisTurn.AddRange(game.Board);
                    game.DealerCaptured.AddRange(game.Board);
                }
                game.Board.Clear();
            }

            game.PlayerScore = CalculateScore(game.PlayerCaptured) + (game.PlayerPistiCount * 10);
            game.DealerScore = CalculateScore(game.DealerCaptured) + (game.DealerPistiCount * 10);

            if (game.PlayerCaptured.Count > game.DealerCaptured.Count) game.PlayerScore += 3;
            else if (game.DealerCaptured.Count > game.PlayerCaptured.Count) game.DealerScore += 3;

            if (game.PlayerScore > game.DealerScore) { game.ChipChange = game.CurrentBet; game.GameMessage = "Tebrikler, kazandınız!"; }
            else if (game.DealerScore > game.PlayerScore) { game.ChipChange = -game.CurrentBet; game.GameMessage = "Bilgisayar kazandı."; }
            else { game.ChipChange = 0; game.GameMessage = "Oyun Berabere!"; }
        }

        private int CalculateScore(List<string> cards)
        {
            int score = 0;
            foreach (var card in cards)
            {
                if (card == "Diamonds-10") score += 3;
                else if (card == "Clubs-2") score += 2;
                else if (card.EndsWith("-A") || card.EndsWith("-J")) score += 1;
            }
            return score;
        }

        public async Task SaveGameResultAsync(string userId, int betAmount, int chipChange, string result)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.TotalChips += chipChange;
                _context.GameHistories.Add(new GameHistory { UserId = userId, BetAmount = betAmount, Result = $"Pişti: {result}", PlayDate = DateTime.Now });
                await _context.SaveChangesAsync();
            }
        }
    }
}