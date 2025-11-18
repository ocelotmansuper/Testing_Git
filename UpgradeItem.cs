using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UpgradeItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text pointsPerSecondText;
    [SerializeField] private Button buyButton;

    private int index;
    private AutoClickManager autoClickManager;
    private BaseGameUI gameUI; // Добавили поле для BaseGameUI

    public void Initialize(int upgradeIndex, AutoClickManager manager, string upgradeName, BaseGameUI ui)
    {
        index = upgradeIndex;
        autoClickManager = manager;
        gameUI = ui; // Сохраняем ссылку на UI
        nameText.text = upgradeName;
        buyButton.onClick.AddListener(OnBuyClicked);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (autoClickManager == null)
        {
            Debug.LogError("AutoClickManager is null in UpgradeItem");
            return;
        }

        var upgrade = autoClickManager.upgrades[index];
        //Debug.Log($"Updating UI for upgrade {index}, level: {upgrade.level}");

        if (levelText != null) levelText.text = $"Уровень: {upgrade.level}";
        if (priceText != null) priceText.text = $"Цена: {upgrade.GetCurrentPrice()}";
        if (pointsPerSecondText != null) pointsPerSecondText.text = $"+{upgrade.pointsPerSecond} очков/сек";
    }

    private void OnBuyClicked()
    {
        Debug.Log($"Buy button clicked for upgrade {index}");
        if (autoClickManager == null)
        {
            Debug.LogError("AutoClickManager is null in UpgradeItem");
            return;
        }

        if (gameUI == null)
        {
            Debug.LogError("GameUI is null in UpgradeItem");
            return;
        }

        if (autoClickManager.TryBuyUpgrade(index))
        {
            UpdateUI();
            gameUI.UpdatePointsPerSecond(); // Обновляем отображение общего количества очков в секунду
        }
    }

    private void OnDestroy()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
        }
    }
}