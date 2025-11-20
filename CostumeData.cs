using System;
using UnityEngine;

[Serializable]
public class CostumeData
{
    public string name;
    public string description;
    public int basePrice;
    public int bonusPerClick; // Бонус к очкам за клик
    public bool isPurchased;
    public GameObject costumeObject; // Родительский объект костюма
    public Sprite costumeIcon;

    public int GetCurrentPrice()
    {
        return basePrice; // Можно заменить формулой расчета цены
    }
}
