using BlackJack.ViewModels;

namespace BlackJack.Services
{
    public interface IBlackjackService
    {
        GameViewModel StartNewGame(int betAmount);
        GameViewModel Hit(GameViewModel currentGame);
        GameViewModel Stand(GameViewModel currentGame);
        Task SaveGameResultAsync(string userId, int betAmount, string result);
    }
}