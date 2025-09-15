public interface IGameUI
{
    void UpdateScore(int score);
    void ShowLeaderboard(bool show);
    void UpdateLeaderboard(PlayerData[] players); // Добавили этот метод
    void SetupUI(GameManager gameManager);
}
