using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;

public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private Transform characterRoot;

    [Header("Character Parts")]
    [SerializeField] private Transform bodyGroup;
    [SerializeField] private Transform headGroup;

    [Header("Animation Settings")]
    [SerializeField] private float squashDuration = 0.2f;
    [SerializeField] private float squashScale = 0.98f;

    [Header("Breathing Animation")]
    [SerializeField] private float breathingSpeed = 1.5f;
    [SerializeField] private float breathingAmount = 0.08f;
    [SerializeField] private AnimationCurve breathingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Transform activeCostume;
    private Vector3 originalBodyScale;
    private Vector3 originalHeadScale;
    private float breathingTimer = 0f;
    private bool isBreathing = true;

    private void Start()
    {
        if (bodyGroup != null)
            originalBodyScale = bodyGroup.localScale;
        if (headGroup != null)
            originalHeadScale = headGroup.localScale;
    }

    private void Update()
    {
        // Постоянное дыхание в фоне
        if (isBreathing && bodyGroup != null && headGroup != null)
        {
            UpdateBreathing();
        }
    }

    private void UpdateBreathing()
    {
        breathingTimer += Time.deltaTime * breathingSpeed;

        if (breathingTimer > 1f)
            breathingTimer -= 1f;

        float breathePhase = Mathf.Sin(breathingTimer * Mathf.PI * 2f) * 0.5f + 0.5f;
        breathePhase = breathingCurve.Evaluate(breathePhase);

        float breatheScale = 1f - (breathingAmount * (1f - breathePhase));

        // Сжимаем обе части одновременно
        bodyGroup.localScale = originalBodyScale * breatheScale;
        headGroup.localScale = originalHeadScale * breatheScale;
    }

    // ========== ВСЕ ТВОИ ОРИГИНАЛЬНЫЕ МЕТОДЫ ==========

    public void OnClick()
    {
        if (activeCostume == null)
        {
            Debug.LogWarning("Активный костюм не установлен! Убедитесь, что костюм установлен перед кликом.");
            return;
        }

        ShowRandomChild(activeCostume);
        ShowRandomChild(headGroup);
        AnimateSquash();
    }

    public void SetActiveCostume(Transform costume)
    {
        activeCostume = costume;
        Debug.Log($"Активный костюм установлен: {costume.name}");

        ShowRandomChild(activeCostume);
    }

    private void ShowRandomChild(Transform group)
    {
        if (group == null || group.childCount == 0) return;

        foreach (Transform child in group)
        {
            child.gameObject.SetActive(false);
        }

        int randomIndex = Random.Range(0, group.childCount);
        group.GetChild(randomIndex).gameObject.SetActive(true);
    }

    private void AnimateSquash()
    {
        if (bodyGroup != null && headGroup != null)
        {
            isBreathing = false; // Отключаем дыхание на время анимации клика

            // Сжимаем обе части одновременно при клике
            bodyGroup.DOScale(originalBodyScale * squashScale, squashDuration / 2)
                .SetEase(Ease.OutQuad);

            headGroup.DOScale(originalHeadScale * squashScale, squashDuration / 2)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Возвращаем обе части в исходное состояние
                    bodyGroup.DOScale(originalBodyScale, squashDuration / 2)
                        .SetEase(Ease.InQuad);

                    headGroup.DOScale(originalHeadScale, squashDuration / 2)
                        .SetEase(Ease.InQuad)
                        .OnComplete(() =>
                        {
                            isBreathing = true; // Возобновляем дыхание
                        });
                });
        }
        else if (characterRoot != null)
        {
            // Fallback на старый способ, если части не установлены
            isBreathing = false;

            characterRoot.DOScale(Vector3.one * squashScale, squashDuration / 2)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    characterRoot.DOScale(Vector3.one, squashDuration / 2)
                        .SetEase(Ease.InQuad)
                        .OnComplete(() =>
                        {
                            isBreathing = true;
                        });
                });
        }
    }
}
