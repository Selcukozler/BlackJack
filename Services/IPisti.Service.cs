using BlackJack.ViewModels;

namespace BlackJack.Services
{
    public interface IPistiService
    {
        PistiViewModel StartNewGame(int betAmount);
        PistiViewModel PlayCard(PistiViewModel game, string cardId);
        Task SaveGameResultAsync(string userId, int betAmount, int chipChange, string result);
    }
}