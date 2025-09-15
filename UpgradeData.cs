using System;
using UnityEngine;

[Serializable]
public class UpgradeData
{
    public string name;
    public int level;
    public float pointsPerSecond;
    public int basePrice;
    public float priceMultiplier;

    public int GetCurrentPrice()
    {
        return Mathf.RoundToInt(basePrice * Mathf.Pow(priceMultiplier, level));
    }

    // ƒобавим конструктор дл€ создани€ копии
    public UpgradeData Clone()
    {
        return new UpgradeData
        {
            name = this.name,
            basePrice = this.basePrice,
            pointsPerSecond = this.pointsPerSecond,
            level = this.level
        };
    }

    public float GetCurrentPointsPerSecond()
    {
        return pointsPerSecond * level;
    }
}