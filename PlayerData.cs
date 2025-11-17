using System;

[Serializable]
public class PlayerData
{
    public string vk_id;
    public string name;
    public string photo_url;
    public int score;
    public string upgrades;
    public string costumes;
    public long last_online;
}

[Serializable]
public class ScoreUpdateData
{
    public string vk_id;
    public int score;
    public string upgrades;
    public long last_online;
}

[Serializable]
public class LeaderboardResponse
{
    public bool success;
    public PlayerData[] data;
}

[Serializable]
public class UpgradesWrapper
{
    public UpgradeData[] upgrades;
}

[Serializable]
public class VKUserData
{
    public string id;
    public string first_name;
    public string last_name;
    public string photo_200;
}