using UnityEngine;

public class MobileGameUI : BaseGameUI
{
    [Header("Mobile Specific Settings")]
    [SerializeField] private float mobileTextScale = 1.5f;

    public override void SetupUI(GameManager manager)
    {
        base.SetupUI(manager);

        // Настройки специфичные для мобильной версии
        if (scoreText != null)
            scoreText.fontSize *= mobileTextScale;

        // Можно добавить дополнительные настройки для мобильной версии
    }
}
