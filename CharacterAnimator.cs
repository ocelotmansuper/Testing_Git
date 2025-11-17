using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;

public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private Transform characterRoot;

    [Header("Character Parts")]
    [SerializeField] private Transform bodyGroup; // Родительский объект для костюмов (группа)
    [SerializeField] private Transform headGroup; // Родительский объект для головы (дочерняя логика не изменяется)

    [Header("Animation Settings")]
    [SerializeField] private float squashDuration = 0.5f; // Исправлено имя переменной
    [SerializeField] private float squashScale = 0.8f;

    private Transform activeCostume; // Активный костюм, чтобы переключать позы внутри него

    // Метод, который нужно вызывать из обработчика нажатия существующей кнопки
    public void OnClick()
    {
        if (activeCostume == null)
        {
            Debug.LogWarning("Активный костюм не установлен! Убедитесь, что костюм установлен перед кликом.");
            return;
        }

        ShowRandomChild(activeCostume); // Меняем отображаемую позу только внутри активного костюма
        ShowRandomChild(headGroup); // Никакие изменения для головы
        AnimateSquash();
    }

    public void SetActiveCostume(Transform costume)
    {
        activeCostume = costume;
        Debug.Log($"Активный костюм установлен: {costume.name}");

        // При установке активного костюма сразу выбираем случайную позу
        ShowRandomChild(activeCostume);
    }

    private void ShowRandomChild(Transform group)
    {
        if (group == null || group.childCount == 0) return;

        // Деактивируем всех детей
        foreach (Transform child in group)
        {
            child.gameObject.SetActive(false);
        }

        // Включаем случайного ребенка
        int randomIndex = Random.Range(0, group.childCount);
        group.GetChild(randomIndex).gameObject.SetActive(true);
    }

    private void AnimateSquash()
    {
        if (characterRoot != null)
        {
            // Сжимаем масштаб root
            characterRoot.DOScale(Vector3.one * squashScale, squashDuration / 2)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Возвращаем к исходному размеру
                    characterRoot.DOScale(Vector3.one, squashDuration / 2)
                        .SetEase(Ease.InQuad);
                });
        }
    }
}
