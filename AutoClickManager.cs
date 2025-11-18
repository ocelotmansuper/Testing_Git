using UnityEngine;
using System;
using Unity.VisualScripting;

public class AutoClickManager : MonoBehaviour
{
    public GameManager gameManager;
    private float accumulatedPoints;
    public UpgradeData[] upgrades;

    private float saveTimer = 0f;
    private const float SAVE_INTERVAL = 10f; // Интервал сохранения в секундах
    private bool needsSave = false; // Флаг, указывающий на необходимость сохранения

    private void Awake()
    {

    }

    public void Initialize(GameManager manager)
    {
        Debug.Log("AutoClickManager initialized with GameManager");
        gameManager = manager;
        ProcessOfflineProgress();
    }

    private void Update()
    {
        if (gameManager == null) return;

        float pointsPerSecond = GetTotalPointsPerSecond();

        // Накапливаем очки
        if (pointsPerSecond > 0)
        {
            accumulatedPoints += pointsPerSecond * Time.deltaTime;
            needsSave = true; // Помечаем, что нужно сохранить прогресс
        }

        // Начисляем очки, когда накопилось достаточно
        if (accumulatedPoints >= 1f)
        {
            int points = Mathf.FloorToInt(accumulatedPoints);
            gameManager.AddScoreWithoutSave(points); // Используем версию без автосохранения
            accumulatedPoints -= points;
        }

        // Обработка таймера сохранения
        if (needsSave)
        {
            saveTimer += Time.deltaTime;
            if (saveTimer >= SAVE_INTERVAL)
            {
                SaveProgress();
                saveTimer = 0f;
                needsSave = false;
            }
        }
    }

    public float GetTotalPointsPerSecond()
    {
        float total = 0;
        foreach (var upgrade in upgrades)
        {
            total += upgrade.GetCurrentPointsPerSecond();
        }
        return total;
    }

    public string GetInitialUpgradesJson()
    {
        try
        {
            UpgradeData[] initialUpgrades = new UpgradeData[upgrades.Length];
            for (int i = 0; i < upgrades.Length; i++)
            {
                initialUpgrades[i] = new UpgradeData
                {
                    name = upgrades[i].name,
                    basePrice = upgrades[i].basePrice,
                    pointsPerSecond = upgrades[i].pointsPerSecond,
                    level = 0
                };
            }

            var wrapper = new UpgradesWrapper { upgrades = initialUpgrades };
            string json = JsonUtility.ToJson(wrapper);
            Debug.Log($"[AutoClickManager] Created initial upgrades JSON: {json}");
            return json;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AutoClickManager] Error in GetInitialUpgradesJson: {e.Message}");
            return "{\"upgrades\":[]}";
        }
    }


    public bool TryBuyUpgrade(int upgradeIndex)
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager is not initialized in AutoClickManager");
            return false;
        }

        if (upgradeIndex < 0 || upgradeIndex >= upgrades.Length)
            return false;

        var upgrade = upgrades[upgradeIndex];
        int price = upgrade.GetCurrentPrice();

        if (gameManager.CanSpendScore(price))
        {
            gameManager.SpendScore(price);
            upgrade.level++;
            SaveUpgrades();
            Debug.Log($"Upgrade {upgradeIndex} bought. New level: {upgrade.level}, Points per second: {GetTotalPointsPerSecond()}");
            return true;
        }

        return false;
    }

    private void SaveProgress()
    {
        if (gameManager != null)
        {
            Debug.Log("Saving progress after interval");
            gameManager.SaveProgress();
        }
    }

    public string GetUpgradesJson()
    {
        try
        {
            // Создаем копию массива улучшений для сериализации
            UpgradeData[] upgradesCopy = new UpgradeData[upgrades.Length];
            for (int i = 0; i < upgrades.Length; i++)
            {
                upgradesCopy[i] = upgrades[i].Clone();
            }

            var wrapper = new UpgradesWrapper { upgrades = upgradesCopy };
            string json = JsonUtility.ToJson(wrapper);
            Debug.Log($"[AutoClickManager] Generated upgrades JSON: {json}");
            return json;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AutoClickManager] Error in GetUpgradesJson: {e.Message}");
            return GetInitialUpgradesJson(); // Возвращаем начальное состояние в случае ошибки
        }
    }

    public bool PurchaseUpgrade(int index)
    {
        if (index < 0 || index >= upgrades.Length)
        {
            Debug.LogError($"[AutoClickManager] Invalid upgrade index: {index}");
            return false;
        }

        var upgrade = upgrades[index];
        int price = upgrade.GetCurrentPrice();

        if (gameManager.CanSpendScore(price))
        {
            gameManager.SpendScore(price);
            upgrade.level++;
            Debug.Log($"[AutoClickManager] Purchased upgrade {upgrade.name}, new level: {upgrade.level}");

            // Сразу сохраняем прогресс
            gameManager.SaveProgress();
            return true;
        }

        return false;
    }

    public void LoadUpgradesFromJson(string json)
    {
        Debug.Log($"[AutoClickManager] Starting LoadUpgradesFromJson with input: {json}");

        try
        {
            // ВАЖНОЕ ИЗМЕНЕНИЕ: Если получаем "[]", сохраняем текущее состояние
            if (json == "[]" || json == "\"[]\"") // проверяем оба варианта, так как сервер может вернуть в кавычках
            {
                Debug.Log("[AutoClickManager] Received empty array from server, saving current state instead of resetting");
                gameManager.SaveProgress(); // Сохраняем текущее состояние обратно на сервер
                return; // Выходим из метода, сохраняя текущие значения
            }

            // Проверяем входные данные
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[AutoClickManager] Empty JSON received, using initial state");
                json = GetInitialUpgradesJson();
            }

            // Проверяем формат JSON и убираем лишние кавычки если они есть
            json = json.Trim('"');
            if (json.Trim().StartsWith("["))
            {
                json = $"{{\"upgrades\":{json}}}";
            }

            Debug.Log($"[AutoClickManager] Processing JSON after formatting: {json}");
            var wrapper = JsonUtility.FromJson<UpgradesWrapper>(json);

            if (wrapper == null || wrapper.upgrades == null)
            {
                Debug.LogError("[AutoClickManager] Failed to deserialize upgrades wrapper");
                return; // Сохраняем текущие значения
            }

            // Проверяем соответствие количества улучшений
            if (wrapper.upgrades.Length != upgrades.Length)
            {
                Debug.LogWarning($"[AutoClickManager] Upgrades count mismatch. Expected: {upgrades.Length}, Got: {wrapper.upgrades.Length}");
                return; // Сохраняем текущие значения
            }

            // Обновляем данные улучшений только если они не все нулевые
            bool allZero = true;
            for (int i = 0; i < wrapper.upgrades.Length; i++)
            {
                if (wrapper.upgrades[i].level > 0)
                {
                    allZero = false;
                    break;
                }
            }

            if (!allZero) // Загружаем данные только если есть хотя бы одно улучшение с уровнем > 0
            {
                for (int i = 0; i < upgrades.Length; i++)
                {
                    if (wrapper.upgrades[i] != null)
                    {
                        upgrades[i].level = wrapper.upgrades[i].level;
                        //Debug.Log($"[AutoClickManager] Updated upgrade {upgrades[i].name} to level {upgrades[i].level}");
                    }
                }
            }
            else
            {
                Debug.Log("[AutoClickManager] All loaded upgrades are level 0, keeping current state");
                if (HasAnyUpgrades()) // Если есть улучшения, сохраняем их
                {
                    gameManager.SaveProgress();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AutoClickManager] Error loading upgrades: {e.Message}\nJSON: {json}");
        }
    }

    public bool HasAnyUpgrades()
    {
        if (upgrades == null) return false;
        foreach (var upgrade in upgrades)
        {
            if (upgrade.level > 0) return true;
        }
        return false;
    }

    private void ProcessOfflineProgress()
    {
        if (gameManager == null) return;

        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastOnlineTime = gameManager.GetLastOnlineTime();

        if (lastOnlineTime > 0)
        {
            long timeDiff = currentTime - lastOnlineTime;
            float offlinePoints = GetTotalPointsPerSecond() * timeDiff;

            if (offlinePoints >= 1)
            {
                gameManager.AddScore(Mathf.FloorToInt(offlinePoints));
            }
        }
    }

    private void SaveUpgrades()
    {
        SaveProgress();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveProgress();
        }
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }
}
