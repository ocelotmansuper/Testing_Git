using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class CostumeItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;         // Название костюма
    [SerializeField] private TMP_Text descriptionText;  // Описание костюма
    [SerializeField] private TMP_Text priceText;        // Цена костюма
    [SerializeField] private TMP_Text buttonText;       // Текст кнопки действия
    [SerializeField] private Button actionButton;       // Кнопка покупки/экипировки
    [SerializeField] private GameObject priceText_Holder;
    [SerializeField] private Image icon;

    private CostumeData costumeData;                    // Данные костюма
    private CostumeManager costumeManager;              // Ссылка на менеджер костюмов

    public void Initialize(CostumeData data, CostumeManager manager)
    {
        costumeData = data;
        costumeManager = manager;

        icon.sprite = costumeData.costumeIcon;
        nameText.text = costumeData.name;
        descriptionText.text = costumeData.description; // Устанавливаем описание
        UpdateUI();

        actionButton.onClick.AddListener(OnActionButtonClicked);
    }

    private void UpdateUI()
    {
        // Устанавливаем текст цены
        if (!costumeData.isPurchased)
        {
            priceText.text = $"{costumeData.GetCurrentPrice()}";
        }
        else
        {
            priceText_Holder.SetActive(false);
            priceText.text = ""; // Если куплено, цена не отображается
        }

        // Устанавливаем текст кнопки
        if (costumeData.isPurchased)
        {
            if (costumeManager.GetEquippedCostume() == costumeData)
            {
                buttonText.text = "Надето";
            }
            else
            {
                buttonText.text = "Надеть";
            }
        }
        else
        {
            buttonText.text = "Купить";
        }
    }

    private void OnActionButtonClicked()
    {
        if (costumeData.isPurchased)
        {
            costumeManager.EquipCostume(costumeData); // Экипировка костюма
        }
        else
        {
            costumeManager.PurchaseCostume(costumeData); // Покупка костюма
        }

        UpdateUI(); // Обновляем интерфейс после действия
    }

    private void OnDestroy()
    {
        actionButton.onClick.RemoveAllListeners();
    }
}
