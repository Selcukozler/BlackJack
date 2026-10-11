using BlackJack.ViewModels;

namespace BlackJack.Services
{
    public interface IBlackjackService
    {
        GameViewModel StartNewGame(int betAmount);
        GameViewModel Hit(GameViewModel currentGame);
        GameViewModel Stand(GameViewModel currentGame);
        GameViewModel DoubleDown(GameViewModel currentGame);
        GameViewModel Split(GameViewModel currentGame);
        Task SaveGameResultAsync(string userId, int betAmount, int chipChange, string result);
    }
}