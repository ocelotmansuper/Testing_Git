public interface IGameUI
{
    void UpdateScore(int score);
    void ShowLeaderboard(bool show);
    void UpdateLeaderboard(PlayerData[] players); // �������� ���� �����
    void SetupUI(GameManager gameManager);
}
