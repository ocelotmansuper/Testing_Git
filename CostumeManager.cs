using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;

public class CostumeManager : MonoBehaviour
{
    public GameManager gameManager; // Ссылка на GameManager
    public List<CostumeData> costumes;

    public Transform costumesContent; // Контейнер UI для костюмов
    public GameObject costumeItemPrefab; // Префаб UI элемента костюма

    private CostumeData equippedCostume; // Текущий экипированный костюм

    [SerializeField] private CharacterAnimator characterAnimator;

    private void Start()
    {
        // Установка костюма по умолчанию
        if (costumes != null && costumes.Count > 0)
        {
            foreach (var costume in costumes) costume.isPurchased = false; // Сбрасываем состояние покупки
            costumes[0].isPurchased = true; // Первый костюм доступен по умолчанию
            EquipCostume(costumes[0]); // Устанавливаем первый костюм
        }
    }
    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        Debug.Log("CostumeManager успешно инициализирован с GameManager");
        LoadCostumeData();
        InitializeCostumesUI();
    }

    private void LoadCostumeData()
    {
        // Запрос данных с сервера
        StartCoroutine(gameManager.SendGetRequest("player_costumes", (response) =>
        {
            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("Failed to load costume data from server.");
                return;
            }

            var saveData = JsonUtility.FromJson<CostumeSaveData>(response);

            // Восстанавливаем состояния покупки костюмов
            foreach (var costume in costumes)
            {
                costume.isPurchased = Array.Exists(saveData.purchasedCostumes, name => name == costume.name);
            }

            // Надеваем последний надетый костюм, если он есть
            var lastEquippedCostume = costumes.Find(c => c.name == saveData.lastEquipped);
            if (lastEquippedCostume != null)
            {
                EquipCostume(lastEquippedCostume);
            }
        }));
    }

    public void ReapplyCostumeData(string purchasedCostumesJson, string lastEquippedCostumeName)
    {
        Debug.Log($"Raw JSON for purchased costumes: {purchasedCostumesJson}");

        if (string.IsNullOrEmpty(purchasedCostumesJson))
        {
            Debug.LogWarning("Empty or null purchased costumes JSON received. No data to apply.");
            return;
        }

        string[] purchasedCostumes;
        try
        {
            // Попытка десериализации; обработка пустого массива
            purchasedCostumes = JsonUtility.FromJson<string[]>(purchasedCostumesJson);

            if (purchasedCostumes == null)
            {
                Debug.LogWarning("Purchased costumes array is null after deserialization.");
                purchasedCostumes = new string[0];
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deserializing purchased costumes JSON: {e.Message}");
            return;
        }

        // Обработка купленных костюмов
        foreach (var costume in costumes)
        {
            costume.isPurchased = System.Array.Exists(purchasedCostumes, name => name == costume.name);
            Debug.Log($"Costume {costume.name} purchased status: {costume.isPurchased}");
        }

        // Установка последнего надетого костюма
        var lastEquippedCostume = costumes.Find(c => c.name == lastEquippedCostumeName);
        if (lastEquippedCostume != null)
        {
            EquipCostume(lastEquippedCostume);
            Debug.Log($"Equipped costume: {lastEquippedCostumeName}");
        }
        else
        {
            Debug.LogWarning($"Last equipped costume not found: {lastEquippedCostumeName}");
        }
    }

    private void InitializeCostumesUI()
    {
        foreach (Transform child in costumesContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var costume in costumes)
        {
            var itemGO = Instantiate(costumeItemPrefab, costumesContent);
            var costumeItem = itemGO.GetComponent<CostumeItemUI>();

            if (costumeItem != null)
                costumeItem.Initialize(costume, this);
        }

        Debug.Log("Инициализация списка костюмов завершена");
    }

    public void PurchaseCostume(CostumeData costume)
    {
        if (costume.isPurchased)
        {
            Debug.LogWarning($"Костюм \"{costume.name}\" уже куплен!");
            return;
        }

        int price = costume.GetCurrentPrice();
        if (gameManager.CanSpendScore(price))
        {
            gameManager.SpendScore(price);
            costume.isPurchased = true;

            // Автоматически надеваем костюм после покупки
            EquipCostume(costume);

            // Сохраняем изменения на сервер
            var purchasedCostumes = GetPurchasedCostumes();
            gameManager.StartCoroutine(gameManager.SaveCostumeData(purchasedCostumes, costume.name));
        }
        else
        {
            Debug.LogWarning($"Недостаточно средств для покупки костюма \"{costume.name}\".");
        }
    }

    public void EquipCostume(CostumeData costume)
    {
        if (!costume.isPurchased)
        {
            Debug.LogWarning($"Костюм \"{costume.name}\" не куплен!");
            return;
        }

        equippedCostume = costume;

        // Отображаем только текущий костюм
        foreach (var c in costumes)
        {
            c.costumeObject.SetActive(c == equippedCostume);
        }

        // Устанавливаем костюм в CharacterAnimator
        characterAnimator.SetActiveCostume(equippedCostume.costumeObject.transform);

        // Обновляем бонус за клик
        gameManager.BonusPerClick = equippedCostume.bonusPerClick;

        // Сохраняем изменения на сервер
        var purchasedCostumes = GetPurchasedCostumes();
        gameManager.StartCoroutine(gameManager.SaveCostumeData(purchasedCostumes, costume.name));

        Debug.Log($"Костюм \"{costume.name}\" экипирован!");
    }

    private List<string> GetPurchasedCostumes()
    {
        var purchased = new List<string>();
        foreach (var costume in costumes)
        {
            if (costume.isPurchased)
            {
                purchased.Add(costume.name);
            }
        }
        return purchased;
    }

    private void SaveCostumesToServer()
    {
        // Формируем данные для отправки
        var purchasedCostumes = new List<string>();
        foreach (var costume in costumes)
        {
            if (costume.isPurchased)
            {
                purchasedCostumes.Add(costume.name);
            }
        }

        var jsonData = JsonUtility.ToJson(new CostumeSaveData
        {
            purchasedCostumes = purchasedCostumes.ToArray(),
            lastEquipped = equippedCostume != null ? equippedCostume.name : ""
        });

        // Отправляем данные на сервер
        StartCoroutine(gameManager.SendPostRequest("player_costumes", jsonData));
    }

    public GameObject GetEquippedCostumeObject()
    {
        return equippedCostume != null ? equippedCostume.costumeObject : null;
    }
    public CostumeData GetEquippedCostume()
    {
        return equippedCostume;
    }
    private Transform[] GetPoses(GameObject costumeParent)
    {
        List<Transform> poses = new List<Transform>();

        foreach (Transform child in costumeParent.transform)
        {
            poses.Add(child); // Собираем все дочерние объекты, которые считаются позами
        }

        return poses.ToArray();
    }

    public int GetBonusPerClick()
    {
        return equippedCostume != null ? equippedCostume.bonusPerClick : 0;
    }
}

[System.Serializable]
public class CostumeSaveData
{
    public string vk_id; // ID пользователя
    public string[] purchasedCostumes; // Список купленных костюмов
    public string lastEquipped; // Последний надетый костюм
}
