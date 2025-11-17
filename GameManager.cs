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

    private long lastPauseTime = 0; // ����� ����� � ���

    public CharacterAnimator characterAnimator;

    public CostumeManager costumeManager;

    public int BonusPerClick; // ����� �� �������

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
        StartCoroutine(LoadCostumeData());
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

                        // ������������ ������� ��������
                        CalculateOfflineProgress();

                        // ��������� UI ���� ���������
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

        const long MIN_OFFLINE_SECONDS = 60; // 1 ������
        if (timeDifference < MIN_OFFLINE_SECONDS) return;
        // ������������ ������������ ����� ������� ��������� (��������, 24 ����)
        const long MAX_OFFLINE_SECONDS = 24 * 60 * 60;
        timeDifference = (long)Mathf.Min(timeDifference, MAX_OFFLINE_SECONDS); // ��������� ����� ���������� �����

        if (timeDifference <= 0) return;

        // ������������ ����� ���������� ����� � ������� �� ���� ���������
        float pointsPerSecond = autoClickManager.GetTotalPointsPerSecond();

        // ������������ ������������ ����
        int earnedPoints = Mathf.FloorToInt(pointsPerSecond * timeDifference);

        if (earnedPoints > 0)
        {
            // ���������� ��������� � ������������ �����
            ShowOfflineEarningsPopup(earnedPoints, TimeSpan.FromSeconds(timeDifference));

            // ��������� ����
            AddScoreWithoutSave(earnedPoints);
            SaveScore(); // ��������� ����� ����
        }
        else return;
    }

    private void ShowOfflineEarningsPopup(int earnedPoints, TimeSpan offlineTime)
    {
        string timeText;
        if (offlineTime.TotalHours >= 1)
        {
            timeText = $"{(int)offlineTime.TotalHours} ����� {offlineTime.Minutes} �����";
        }
        else if (offlineTime.Minutes > 0)
        {
            timeText = $"{offlineTime.Minutes} �����";
        }
        else
        {
            timeText = "����� ������";
        }

        var baseGameUI = currentUI as BaseGameUI;
        if (baseGameUI != null)
        {
            baseGameUI.ShowOfflineEarningsPopup(earnedPoints, timeText);
        }
    }

    public void IncreaseScore()
    {
        int bonus = costumeManager != null ? costumeManager.GetBonusPerClick() : 0;
        int scoreToAdd = 1 + bonus; // ����� �� ���� + ������� ����

        currentScore += scoreToAdd;

        characterAnimator.OnClick();
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

        // ������� ��������� JSON ��� ���������
        string initialUpgrades = autoClickManager.GetInitialUpgradesJson(); // ����� �����

        PlayerData playerData = new PlayerData
        {
            vk_id = vkId,
            name = playerName,
            photo_url = photoUrl,
            score = currentScore,
            upgrades = initialUpgrades, // ���������� ��������� JSON
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
        currentScore += amount + BonusPerClick; // ���������������� ������
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

            string upgradesJson = "[]"; // �������� �� ���������
            if (autoClickManager != null)
            {
                upgradesJson = autoClickManager.GetUpgradesJson();
                // ���������, ��� �� ���������� ������ ������, ���� ���� ���������
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

    public IEnumerator SendGetRequest(string endpoint, Action<string> callback)
    {
        if (string.IsNullOrEmpty(vkId))
        {
            Debug.LogError("VK ID is null or empty, cannot send GET request.");
            yield break;
        }

        string url = $"{API_URL}{endpoint}?vk_id={vkId}";
        Debug.Log($"Sending GET request to: {url}");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"GET request successful. Response: {www.downloadHandler.text}");
                callback?.Invoke(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"GET request failed: {www.error}");
                callback?.Invoke(null); // ���� ��������� ������, �������� null
            }
        }
    }

    public IEnumerator SendPostRequest(string endpoint, string jsonData)
    {
        if (string.IsNullOrEmpty(vkId))
        {
            Debug.LogError("VK ID is null or empty, cannot send POST request.");
            yield break;
        }

        string url = $"{API_URL}?action={endpoint}"; // Добавляем параметр action
        Debug.Log($"Sending POST request to: {url} with data: {jsonData}");

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"POST request successful. Server response: {www.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"POST request failed: {www.error}\nURL: {url}");
            }
        }
    }

    public IEnumerator SaveCostumeData(List<string> purchasedCostumes, string lastEquippedCostume)
    {
        var saveData = new CostumeSaveData
        {
            vk_id = vkId,
            purchasedCostumes = purchasedCostumes.ToArray(),
            lastEquipped = lastEquippedCostume
        };

        string jsonData = JsonUtility.ToJson(saveData);
        string url = $"{API_URL}?action=player_costumes";

        Debug.Log($"[Save] Sending POST request to {url}");
        Debug.Log($"[Save] JSON Data: {jsonData}");

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[Save] Server response: {www.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"[Save] Failed to save data. Error: {www.error}");
            }
        }
    }

    private IEnumerator LoadCostumeData()
    {
        string url = $"{API_URL}?action=player_costumes&vk_id={vkId}";

        Debug.Log($"[Load] Sending GET request to {url}");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[Load] Response data: {www.downloadHandler.text}");

                var response = JsonUtility.FromJson<CostumeResponse>(www.downloadHandler.text);
                if (response.success)
                {
                    costumeManager.ReapplyCostumeData(response.data.purchased_costumes, response.data.last_equipped_costume);
                    Debug.Log("[Load] Costume data successfully applied.");
                }
                else
                {
                    Debug.LogWarning("[Load] No costume data found on server.");
                }
            }
            else
            {
                Debug.LogError($"[Load] Failed to load costume data. Error: {www.error}");
            }
        }
    }

    [Serializable]
    public class CostumeResponse
    {
        public bool success;
        public CostumeServerData data;
    }

    [Serializable]
    public class CostumeServerData
    {
        public string purchased_costumes;
        public string last_equipped_costume;
    }

    private void OnApplicationPause(string pauseStatus)
    {
        bool isPaused = pauseStatus == "true";

        if (isPaused)
        {
            Debug.Log("[GameManager] ������� �������������. ��������� ��������.");
            SaveProgress(); // ���������� ���������
            lastPauseTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // ��������� ����� �����
        }
        else
        {
            Debug.Log("[GameManager] ������� ������������. ������������ ��������� �����.");
            HandleReturnFromPause(); // ������������ ����� �� ����� ����������
        }
    }

    private void HandleReturnFromPause()
    {
        if (lastPauseTime == 0) return; // ���� ����� �� �����������, ������ �� ������

        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long timeDiff = currentTime - lastPauseTime;

        float pointsPerSecond = autoClickManager?.GetTotalPointsPerSecond() ?? 0;
        int earnedPoints = Mathf.FloorToInt(pointsPerSecond * timeDiff);

        if (earnedPoints > 0)
        {
            AddScore(earnedPoints); // ��������� ������������ ����
            Debug.Log($"[GameManager] ��������� {earnedPoints} �� {timeDiff} ������ ����������.");
        }

        lastPauseTime = 0; // ���������� ����� ����� � ���
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }
}
