using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;

public class CostumeManager : MonoBehaviour
{
    public GameManager gameManager;
    public List<CostumeData> costumes;

    public Transform costumesContent;
    public GameObject costumeItemPrefab;

    private CostumeData equippedCostume;

    [SerializeField] private CharacterAnimator characterAnimator;

    private bool isInitialized = false; // Флаг инициализации

    public void Initialize(GameManager manager)
    {
        if (isInitialized) return; // Защита от двойной инициализации
        isInitialized = true;

        gameManager = manager;
        Debug.Log("CostumeManager инициализирован с GameManager");

        // ✅ Инициализируем костюмы в дефолт ЗДЕСЬ
        if (costumes != null && costumes.Count > 0)
        {
            foreach (var costume in costumes) costume.isPurchased = false;
            costumes[0].isPurchased = true;
            equippedCostume = costumes[0];
            costumes[0].costumeObject.SetActive(true);

            foreach (var c in costumes)
            {
                if (c != costumes[0])
                    c.costumeObject.SetActive(false);
            }

            characterAnimator.SetActiveCostume(costumes[0].costumeObject.transform);
            gameManager.BonusPerClick = costumes[0].bonusPerClick;
        }

        InitializeCostumesUI();
    }

    // Этот метод вызывается из GameManager ПОСЛЕ загрузки данных
    public void ReapplyCostumeData(string purchasedCostumesJson, string lastEquippedCostumeName)
    {
        Debug.Log($"Raw JSON for purchased costumes: {purchasedCostumesJson}");

        if (string.IsNullOrEmpty(purchasedCostumesJson))
        {
            Debug.LogWarning("Empty or null purchased costumes JSON received. No data to apply.");
            return;
        }

        string[] purchasedCostumes;
        //Debug.LogError($"Error deserializing purchased costumes JSON: {e.Message}");
        // Пробуем альтернативный способ парсинга
        try
        {
            var wrapper = JsonUtility.FromJson<StringArrayWrapper>($"{{\"items\":{purchasedCostumesJson}}}");
            if (wrapper != null && wrapper.items != null)
            {
                purchasedCostumes = wrapper.items;
            }
            else
            {
                return;
            }
        }
        catch (Exception e2)
        {
            Debug.LogError($"Alternative parsing also failed: {e2.Message}");
            return;
        }

        // Применяем статус покупки
        foreach (var costume in costumes)
        {
            costume.isPurchased = System.Array.Exists(purchasedCostumes, name => name == costume.name);
            Debug.Log($"Costume {costume.name} purchased status: {costume.isPurchased}");
        }

        // Применяем последний надетый костюм
        if (!string.IsNullOrEmpty(lastEquippedCostumeName))
        {
            var lastEquippedCostume = costumes.Find(c => c.name == lastEquippedCostumeName);
            if (lastEquippedCostume != null && lastEquippedCostume.isPurchased)
            {
                EquipCostume(lastEquippedCostume);
                Debug.Log($"Equipped costume: {lastEquippedCostumeName}");
            }
            else
            {
                Debug.LogWarning($"Last equipped costume not found or not purchased: {lastEquippedCostumeName}");
            }
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

        Debug.Log("Инициализирована панель костюмов");
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

            EquipCostume(costume);

            var purchasedCostumes = GetPurchasedCostumes();
            gameManager.StartCoroutine(gameManager.SaveCostumeData(purchasedCostumes, costume.name));
        }
        else
        {
            Debug.LogWarning($"Недостаточно очков для покупки костюма \"{costume.name}\".");
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

        // Деактивируем все остальные костюмы
        foreach (var c in costumes)
        {
            c.costumeObject.SetActive(c == equippedCostume);
        }

        characterAnimator.SetActiveCostume(equippedCostume.costumeObject.transform);
        gameManager.BonusPerClick = equippedCostume.bonusPerClick;

        var purchasedCostumes = GetPurchasedCostumes();
        gameManager.StartCoroutine(gameManager.SaveCostumeData(purchasedCostumes, costume.name));

        Debug.Log($"Костюм \"{costume.name}\" надет!");
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

    public GameObject GetEquippedCostumeObject()
    {
        return equippedCostume != null ? equippedCostume.costumeObject : null;
    }

    public CostumeData GetEquippedCostume()
    {
        return equippedCostume;
    }

    public int GetBonusPerClick()
    {
        return equippedCostume != null ? equippedCostume.bonusPerClick : 0;
    }
}

[System.Serializable]
public class CostumeSaveData
{
    public string vk_id;
    public string[] purchasedCostumes;
    public string lastEquipped;
}

// Вспомогательный класс для парсинга массива
[System.Serializable]
public class StringArrayWrapper
{
    public string[] items;
}