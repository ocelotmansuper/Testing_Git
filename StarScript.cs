using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StarScript : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private float gravityScale = 0.5f;

    [Header("Visual")]
    [SerializeField] private Image starImage; // UI Image компонент звездочки

    private Vector2 velocity = Vector2.zero;
    private float elapsedTime = 0f;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    public event Action<StarScript> OnReturn;

    private void OnEnable()
    {
        elapsedTime = 0f;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (rectTransform == null) return;

        elapsedTime += Time.deltaTime;

        // ✅ Движение через anchoredPosition (UI координаты)
        velocity.y -= gravityScale * Time.deltaTime;
        rectTransform.anchoredPosition += velocity * Time.deltaTime;

        // Затухание
        float progress = elapsedTime / lifetime;
        if (canvasGroup != null)
            canvasGroup.alpha = fadeCurve.Evaluate(progress);

        // Возврат в пул
        if (elapsedTime >= lifetime)
        {
            OnReturn?.Invoke(this);
        }
    }

    // Метод для установки скорости
    public void SetVelocity(Vector2 newVelocity)
    {
        velocity = newVelocity;
    }

    // Метод для запуска звездочки
    public void StartFloating()
    {
        elapsedTime = 0f;
    }
}
