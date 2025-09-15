using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Text;

public class GameManager : MonoBehaviour
{
    [Header("API Settings")]
    private const string API_URL = "https://misterimrt.online/api/";

    private int currentScore = 0;
    private string vkId;
    private string playerName;
    private string photoUrl;
    private IGameUI currentUI;
    private bool isUserDataSaved = false;

    private AutoClickManager autoClickManager;
    private long lastOnlineTime;

    private void Start()
    {
        if (currentUI == null)
        {
            Debug.LogError("UI is not set! Make sure PlatformController is properly configured.");
            return;
        }

        autoClickManager = GetComponent<AutoClickManager>();
        if (autoClickManager == null)
        {
            Debug.LogWarning("AutoClickManager not found, adding it");
            autoClickManager = gameObject.AddComponent<AutoClickManager>();
        }
        autoClickManager.Initialize(this);

        InitVK();
    }

    public void SetUI(IGameUI ui)
    {
        currentUI = ui;
        currentUI?.SetupUI(this);
        currentUI?.UpdateScore(currentScore);
    }

    private void InitVK()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        VKBridge.Send("VKWebAppInit");
        VKBridge.Send("VKWebAppGetUserInfo");
#else
        OnVKUserInfoReceived("{\"id\":\"1\",\"first_name\":\"Test\",\"last_name\":\"User\",\"photo_200\":\"https://vk.com/images/camera_200.png\"}");
#endif
    }

    public void OnVKUserInfoReceived(string result)
    {
        try
        {
            Debug.Log($"Received VK user info: {result}");

            VKUserData userData = JsonUtility.FromJson<VKUserData>(result);

            if (string.IsNullOrEmpty(userData.photo_200))
            {
                Debug.LogWarning("VK user photo URL is empty");
                userData.photo_200 = "https://vk.com/images/camera_200.png";
            }

            vkId = userData.id;
            playerName = $"{userData.first_name} {userData.last_name}";
            photoUrl = userData.photo_200;

            Debug.Log($"Parsed VK user data: ID={vkId}, Name={playerName}, Photo={photoUrl}");

            StartCoroutine(LoadAndInitPlayerData());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing VK user info: {e.Message}\nReceived data: {result}");
        }
    }

    private IEnumerator LoadAndInitPlayerData()
    {
        if (string.IsNullOrEmpty(vkId))
        {
            Debug.LogError("VK ID is null or empty");
            yield break;
        }

        Debug.Log($"Loading player data for VK ID: {vkId}");
        using (UnityWebRequest www = UnityWebRequest.Get($"{API_URL}?vk_id={vkId}"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error loading player data: {www.error}");
                yield break;
            }

            try
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"Received response: {responseText}");

                LeaderboardResponse response = JsonUtility.FromJson<LeaderboardResponse>(responseText);
                if (response.data == null || response.data.Length == 0)
                {
                    Debug.Log("No existing player data found, creating new user data");
                    StartCoroutine(SaveUserData());
                }
                else
                {
                    var playerData = response.data[0];
                    currentScore = playerData.score;
                    lastOnlineTime = playerData.last_online;

                    if (!string.IsNullOrEmpty(playerData.upgrades) && autoClickManager != null)
                    {
                        Debug.Log($"Loading upgrades: {playerData.upgrades}");
                        autoClickManager.LoadUpgradesFromJson(playerData.upgrades);

                        // Рассчитываем оффлайн прогресс
                        CalculateOfflineProgress();

                        // Обновляем UI всех улучшений
                        var baseGameUI = currentUI as BaseGameUI;
                        if (baseGameUI != null)
                        {
                            Debug.Log("Updating upgrades UI after loading data");
                            baseGameUI.UpdateUpgradesUI();
                            baseGameUI.UpdatePointsPerSecond();
                        }
                    }

                    currentUI?.UpdateScore(currentScore);
                    isUserDataSaved = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing player data: {e.Message}");
            }
        }
    }

    private void CalculateOfflineProgress()
    {
        if (lastOnlineTime <= 0 || autoClickManager == null) return;

        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long timeDifference = currentTime - lastOnlineTime;

        // Ограничиваем максимальное время оффлайн прогресса (например, 24 часа)
        const long MAX_OFFLINE_SECONDS = 24 * 60 * 60;
        timeDifference = (long)Mathf.Min(timeDifference, MAX_OFFLINE_SECONDS); // Добавляем явное приведение типов

        if (timeDifference <= 0) return;

        // Рассчитываем общее количество очков в секунду от всех улучшений
        float pointsPerSecond = autoClickManager.GetTotalPointsPerSecond();

        // Рассчитываем заработанные очки
        int earnedPoints = Mathf.FloorToInt(pointsPerSecond * timeDifference);

        if (earnedPoints > 0)
        {
            // Показываем сообщение о заработанных очках
            ShowOfflineEarningsPopup(earnedPoints, TimeSpan.FromSeconds(timeDifference));

            // Добавляем очки
            AddScoreWithoutSave(earnedPoints);
            SaveScore(); // Сохраняем новый счет
        }
    }

    private void ShowOfflineEarningsPopup(int earnedPoints, TimeSpan offlineTime)
    {
        string timeText;
        if (offlineTime.TotalHours >= 1)
        {
            timeText = $"{(int)offlineTime.TotalHours} часов {offlineTime.Minutes} минут";
        }
        else if (offlineTime.Minutes > 0)
        {
            timeText = $"{offlineTime.Minutes} минут";
        }
        else
        {
            timeText = "менее минуты";
        }

        var baseGameUI = currentUI as BaseGameUI;
        if (baseGameUI != null)
        {
            baseGameUI.ShowOfflineEarningsPopup(earnedPoints, timeText);
        }
    }

    public void IncreaseScore()
    {
        currentScore++;
        currentUI?.UpdateScore(currentScore);
        SaveScore();
    }

    private void SaveScore()
    {
        StartCoroutine(SendScoreToServer());
    }

    private IEnumerator SendScoreToServer()
    {
        Debug.Log($"Saving score: {currentScore}");
        var scoreData = new ScoreUpdateData
        {
            vk_id = vkId,
            score = currentScore
        };

        string jsonData = JsonUtility.ToJson(scoreData);
        Debug.Log($"Sending data: {jsonData}");

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(API_URL, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            Debug.Log($"Server response: {www.downloadHandler.text}");
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error saving score: {www.error}");
            }
        }
    }

    private IEnumerator SaveUserData()
    {
        Debug.Log($"Saving user data");

        // Создаем начальный JSON для улучшений
        string initialUpgrades = autoClickManager.GetInitialUpgradesJson(); // Новый метод

        PlayerData playerData = new PlayerData
        {
            vk_id = vkId,
            name = playerName,
            photo_url = photoUrl,
            score = currentScore,
            upgrades = initialUpgrades, // Используем начальный JSON
            last_online = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        string jsonData = JsonUtility.ToJson(playerData);
        Debug.Log($"User data JSON: {jsonData}");

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(API_URL, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error saving user data: {www.error}");
            }
            else
            {
                isUserDataSaved = true;
                Debug.Log("User data saved successfully");
            }
        }
    }

    public void LoadLeaderboard()
    {
        StartCoroutine(LoadLeaderboardCoroutine());
    }

    private IEnumerator LoadLeaderboardCoroutine()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(API_URL))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    LeaderboardResponse response = JsonUtility.FromJson<LeaderboardResponse>(www.downloadHandler.text);
                    if (response.success && response.data != null)
                    {
                        currentUI?.UpdateLeaderboard(response.data);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing leaderboard data: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Error loading leaderboard: {www.error}");
            }
        }
    }

    public void AddScoreWithoutSave(int amount)
    {
        currentScore += amount;
        currentUI?.UpdateScore(currentScore);
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        currentUI?.UpdateScore(currentScore);
        SaveScore();
    }

    public bool CanSpendScore(int amount)
    {
        return currentScore >= amount;
    }

    public void SpendScore(int amount)
    {
        if (CanSpendScore(amount))
        {
            currentScore -= amount;
            currentUI?.UpdateScore(currentScore);
            SaveScore();
        }
    }

    public long GetLastOnlineTime()
    {
        return lastOnlineTime;
    }

    public void SaveProgress()
    {
        if (!isUserDataSaved || string.IsNullOrEmpty(vkId))
        {
            Debug.LogWarning("[GameManager] Cannot save progress: user data not initialized or vkId is empty");
            return;
        }

        try
        {
            Debug.Log("[GameManager] Starting SaveProgress");
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string upgradesJson = "[]"; // значение по умолчанию
            if (autoClickManager != null)
            {
                upgradesJson = autoClickManager.GetUpgradesJson();
                // Проверяем, что не отправляем пустой массив, если есть улучшения
                if (upgradesJson == "[]" && autoClickManager.HasAnyUpgrades())
                {
                    Debug.LogWarning("[GameManager] Preventing save of empty upgrades when upgrades exist");
                    return;
                }
            }

            Debug.Log($"[GameManager] Current upgrades state: {upgradesJson}");

            var data = new ScoreUpdateData
            {
                vk_id = vkId,
                score = currentScore,
                upgrades = upgradesJson,
                last_online = currentTime
            };

            string jsonData = JsonUtility.ToJson(data);
            Debug.Log($"[GameManager] Saving progress with data: {jsonData}");

            StartCoroutine(SendRequest(jsonData));
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] Error in SaveProgress: {e.Message}");
        }
    }


    private IEnumerator SendRequest(string jsonData)
    {
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(API_URL, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GameManager] Error sending request: {www.error}");
                Debug.LogError($"[GameManager] Response: {www.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"[GameManager] Data saved successfully. Response: {www.downloadHandler.text}");
            }
        }
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
