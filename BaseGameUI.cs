using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseGameUI : MonoBehaviour, IGameUI
{
    [Header("Common UI References")]
    [SerializeField] protected TMP_Text scoreText;
    [SerializeField] protected Button increaseButton;
    [SerializeField] protected Button showLeaderboardButton;
    [SerializeField] protected GameObject leaderboardPanel;
    [SerializeField] protected Transform leaderboardContent;
    [SerializeField] protected GameObject leaderboardItemPrefab;

    [Header("Upgrade System UI")]
    [SerializeField] protected GameObject upgradesPanel;
    [SerializeField] protected Transform upgradesContent;
    [SerializeField] protected GameObject upgradeItemPrefab;
    [SerializeField] protected TMP_Text totalPointsPerSecondText;
    [SerializeField] protected Button showUpgradesButton;

    protected GameManager gameManager;
    protected AutoClickManager autoClickManager;
    protected UpgradeItem[] upgradeItems;

    [Header("Offline Earnings")]
    [SerializeField] private GameObject offlineEarningsPopup;
    [SerializeField] private TextMeshProUGUI offlineEarningsText;

    public virtual void SetupUI(GameManager manager)
    {
        Debug.Log("SetupUI called");
        gameManager = manager;
        autoClickManager = manager.GetComponent<AutoClickManager>();

        // ��������� ��� ����������
        Debug.Log($"SetupUI components check:\n" +
                 $"gameManager: {(gameManager != null)}\n" +
                 $"autoClickManager: {(autoClickManager != null)}\n" +
                 $"totalPointsPerSecondText: {(totalPointsPerSecondText != null)}\n" +
                 $"upgradeItemPrefab: {(upgradeItemPrefab != null)}\n" +
                 $"upgradesContent: {(upgradesContent != null)}");

        if (increaseButton != null)
        {
            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(gameManager.IncreaseScore);
        }

        if (showLeaderboardButton != null)
            showLeaderboardButton.onClick.AddListener(() => ShowLeaderboard(true));

        if (showUpgradesButton != null)
        {
            Debug.Log("Setting up showUpgradesButton");
            showUpgradesButton.onClick.AddListener(() => ShowUpgrades(true));
        }

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (upgradesPanel != null)
        {
            Debug.Log("Setting up upgradesPanel");
            upgradesPanel.SetActive(false);
        }

        InitializeUpgrades();
    }

    protected virtual void InitializeUpgrades()
    {
        Debug.Log("InitializeUpgrades called");
        if (autoClickManager == null)
        {
            Debug.LogError("InitializeUpgrades: autoClickManager is null");
            return;
        }

        if (upgradeItemPrefab == null)
        {
            Debug.LogError("InitializeUpgrades: upgradeItemPrefab is null");
            return;
        }

        if (upgradesContent == null)
        {
            Debug.LogError("InitializeUpgrades: upgradesContent is null");
            return;
        }

        upgradeItems = new UpgradeItem[autoClickManager.upgrades.Length];

        // ������� ������������ ��������
        foreach (Transform child in upgradesContent)
        {
            Destroy(child.gameObject);
        }

        // ������� �������� ���������
        for (int i = 0; i < autoClickManager.upgrades.Length; i++)
        {
            GameObject itemGO = Instantiate(upgradeItemPrefab, upgradesContent);
            var item = itemGO.GetComponent<UpgradeItem>();
            if (item != null)
            {
                Debug.Log($"Initializing upgrade item {i}");
                item.Initialize(i, autoClickManager, autoClickManager.upgrades[i].name, this);
                upgradeItems[i] = item;
            }
            else
            {
                Debug.LogError($"UpgradeItem component not found on prefab instance {i}");
            }
        }

        UpdateUpgradesUI(); // ��������� ���� �����
        UpdatePointsPerSecond();
    }

    public virtual void ShowOfflineEarningsPopup(int earnedPoints, string timeText)
    {
        // ���� � ��� ���� UI ��� ������:
        if (offlineEarningsPopup != null)
        {
            offlineEarningsPopup.SetActive(true);

            // �����������, ��� � ��� ���� TextMeshProUGUI ���������� ��� ����������� ������
            if (offlineEarningsText != null)
            {
                offlineEarningsText.text = $"���� ��� �� ���� �� ���������� {earnedPoints} �����";
            }
        }
        else
        {
            // ���� ��� UI ��� ������, ������ ���������� � �������
            Debug.Log($"Offline earnings: {earnedPoints} points earned in {timeText}");
        }
    }

    public virtual void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    public virtual void ShowLeaderboard(bool show)
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(show);
            if (show)
            {
                ClearLeaderboard();
                gameManager.LoadLeaderboard();
            }
        }
    }

    public virtual void ShowUpgrades(bool show)
    {
        Debug.Log($"ShowUpgrades called with show={show}");
        if (upgradesPanel != null)
        {
            upgradesPanel.SetActive(show);
            if (show)
            {
                Debug.Log("Updating points per second when showing upgrades panel");
                UpdatePointsPerSecond();
            }
        }
        else
        {
            Debug.LogError("ShowUpgrades: upgradesPanel is null");
        }
    }

    public virtual void UpdatePointsPerSecond()
    {
        // ��������� �������
        Debug.Log("UpdatePointsPerSecond called");

        if (autoClickManager == null)
        {
            Debug.LogError("UpdatePointsPerSecond: autoClickManager is null");
            return;
        }

        if (totalPointsPerSecondText == null)
        {
            Debug.LogError("UpdatePointsPerSecond: totalPointsPerSecondText is null");
            return;
        }

        float pointsPerSecond = autoClickManager.GetTotalPointsPerSecond();
        string newText = $"{pointsPerSecond}/���";
        Debug.Log($"Updating totalPointsPerSecondText from '{totalPointsPerSecondText.text}' to '{newText}'");
        totalPointsPerSecondText.text = newText;
    }

    public void UpdateUpgradesUI()
    {
        if (upgradeItems == null) return;

        Debug.Log($"Updating {upgradeItems.Length} upgrade items");
        foreach (var item in upgradeItems)
        {
            if (item != null)
            {
                item.UpdateUI();
                Debug.Log($"Updated UI for upgrade: {item.name}");
            }
        }
    }

    public virtual void UpdateLeaderboard(PlayerData[] players)
    {
        if (leaderboardContent == null || leaderboardItemPrefab == null) return;

        ClearLeaderboard();

        for (int i = 0; i < players.Length; i++)
        {
            GameObject itemGO = Instantiate(leaderboardItemPrefab, leaderboardContent);
            var item = itemGO.GetComponent<LeaderboardItem>();
            if (item != null)
            {
                item.SetData(
                    i + 1, // ����� � �������
                    players[i].name,
                    players[i].score,
                    players[i].photo_url
                );
            }
        }
    }

    protected virtual void ClearLeaderboard()
    {
        if (leaderboardContent == null) return;

        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (increaseButton != null)
            increaseButton.onClick.RemoveAllListeners();

        if (showLeaderboardButton != null)
            showLeaderboardButton.onClick.RemoveAllListeners();

        if (showUpgradesButton != null)
            showUpgradesButton.onClick.RemoveAllListeners();
    }
}
