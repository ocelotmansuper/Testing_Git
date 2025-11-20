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
    [SerializeField] protected TMP_Text shop_totalPointsPerSecondText;
    [SerializeField] protected Button showUpgradesButton;
    [SerializeField] protected CharacterAnimator characterAnimator;

    [Header("Costume System UI")]
    [SerializeField] protected GameObject costumesPanel;
    [SerializeField] protected Transform costumesContent;
    [SerializeField] protected GameObject costumeItemPrefab;
    [SerializeField] protected Button showCostumesButton;

    protected GameManager gameManager;
    public CostumeManager costumeManager;
    public StarsSpawner starsSpawner;
    protected AutoClickManager autoClickManager;
    protected UpgradeItem[] upgradeItems;

    [Header("Offline Earnings")]
    [SerializeField] private GameObject offlineEarningsPopup;
    [SerializeField] private TextMeshProUGUI offlineEarningsText;

    public void Start()
    {
        gameManager.characterAnimator = characterAnimator;
        gameManager.costumeManager = costumeManager;
        gameManager.starSpawner = starsSpawner;
    }

    public virtual void SetupUI(GameManager manager)
    {
        Debug.Log("SetupUI called");
        gameManager = manager;
        autoClickManager = manager.GetComponent<AutoClickManager>();

        // Проверяем все компоненты
        Debug.Log($"SetupUI components check:\n" +
                 $"gameManager: {(gameManager != null)}\n" +
                 $"autoClickManager: {(autoClickManager != null)}\n" +
                 $"totalPointsPerSecondText: {(totalPointsPerSecondText != null)}\n" +
                 $"upgradeItemPrefab: {(upgradeItemPrefab != null)}\n" +
                 $"upgradesContent: {(upgradesContent != null)}");

        if (costumeManager != null)
        {
            costumeManager.Initialize(gameManager); // Инициализируем CostumeManager с GameManager
        }

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

        showCostumesButton.onClick.AddListener(() => ShowCostumesPanel(true));

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (upgradesPanel != null)
        {
            Debug.Log("Setting up upgradesPanel");
            upgradesPanel.SetActive(false);
        }

        if (showCostumesButton != null)
        {
            showCostumesButton.onClick.AddListener(() => ShowCostumesPanel(true));
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

        // Очищаем существующие элементы
        foreach (Transform child in upgradesContent)
        {
            Destroy(child.gameObject);
        }

        // Создаем элементы улучшений
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

        UpdateUpgradesUI(); // Добавляем этот вызов
        UpdatePointsPerSecond();
    }

    public virtual void ShowOfflineEarningsPopup(int earnedPoints, string timeText)
    {
        // Если у вас есть UI для попапа:
        if (offlineEarningsPopup != null)
        {
            offlineEarningsPopup.SetActive(true);

            // Предполагая, что у вас есть TextMeshProUGUI компоненты для отображения текста
            if (offlineEarningsText != null)
            {
                offlineEarningsText.text = $"Пока вас не было вы заработали {earnedPoints} очков";
            }
        }
        else
        {
            // Если нет UI для попапа, просто показываем в консоли
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

    public virtual void ShowCostumesPanel(bool show)
    {
        if (costumesPanel)
            costumesPanel.SetActive(show);
    }

    public virtual void UpdatePointsPerSecond()
    {
        // Подробная отладка
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
        string newText = $"{pointsPerSecond}/сек";
        Debug.Log($"Updating totalPointsPerSecondText from '{totalPointsPerSecondText.text}' to '{newText}'");
        totalPointsPerSecondText.text = newText;
        shop_totalPointsPerSecondText.text = newText;
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
                //Debug.Log($"Updated UI for upgrade: {item.name}");
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
                    i + 1, // номер в таблице
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
