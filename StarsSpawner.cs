using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StarsSpawner : MonoBehaviour
{
    [Header("Star Settings")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private Transform spawnPoint; // Позиция персонажа (в UI координатах)
    [SerializeField] private Canvas mainCanvas; // ✅ ДОБАВИЛ: главный Canvas
    [SerializeField] private int poolSize = 50;

    [Header("Spawn Settings")]
    [SerializeField] private Vector2 spawnForceMin = new Vector2(-200f, 100f);
    [SerializeField] private Vector2 spawnForceMax = new Vector2(200f, 400f);

    [Header("Rain Settings")]
    [SerializeField] private int rainThreshold = 10;
    [SerializeField] private float rainSpawnHeight = 300f;
    [SerializeField] private float rainSpeed = 200f;

    private Queue<StarScript> starPool;
    private int clickCombo = 0;
    private float lastClickTime = 0f;
    private float comboResetTime = 0.5f;
    private RectTransform canvasRect;

    private void Start()
    {
        // ✅ ДОБАВИЛ: Получаю Canvas RectTransform
        if (mainCanvas == null)
            mainCanvas = GetComponentInParent<Canvas>();

        canvasRect = mainCanvas.GetComponent<RectTransform>();

        InitializePool();
    }

    private void InitializePool()
    {
        starPool = new Queue<StarScript>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject star = Instantiate(starPrefab, transform);
            StarScript starScript = star.GetComponent<StarScript>();

            starScript.OnReturn += ReturnToPool;

            // ✅ ДОБАВИЛ: Убираю Layout Group если есть
            LayoutGroup layoutGroup = star.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
                layoutGroup.enabled = false;

            star.SetActive(false);
            starPool.Enqueue(starScript);
        }
    }

    public void SpawnStars(int amount)
    {
        if (Time.time - lastClickTime > comboResetTime)
        {
            clickCombo = 0;
        }
        clickCombo++;
        lastClickTime = Time.time;

        if (clickCombo >= rainThreshold)
        {
            SpawnRain(amount);
        }
        else
        {
            SpawnExplosion(amount);
        }
    }

    private void SpawnExplosion(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            StarScript star = GetStarFromPool();
            if (star == null) continue;

            star.gameObject.SetActive(true);

            // ✅ ИСПРАВИЛ: Позиция персонажа (уже в UI координатах)
            star.transform.position = spawnPoint.position;

            // Случайная сила в разные стороны
            Vector2 randomForce = new Vector2(
                UnityEngine.Random.Range(spawnForceMin.x, spawnForceMax.x),
                UnityEngine.Random.Range(spawnForceMin.y, spawnForceMax.y)
            );

            star.SetVelocity(randomForce);
            star.StartFloating();
        }
    }

    private void SpawnRain(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            StarScript star = GetStarFromPool();
            if (star == null) continue;

            star.gameObject.SetActive(true);

            // ✅ ИСПРАВИЛ: Использую Canvas размеры для правильного спавна сверху
            RectTransform starRect = star.GetComponent<RectTransform>();

            // Случайная позиция X в рамках Canvas
            float randomX = UnityEngine.Random.Range(-canvasRect.rect.width * 0.5f, canvasRect.rect.width * 0.5f);
            float spawnY = canvasRect.rect.height * 0.5f + rainSpawnHeight;

            starRect.anchoredPosition = new Vector2(randomX, spawnY);

            // Падает вниз
            Vector2 rainForce = new Vector2(UnityEngine.Random.Range(-30f, 30f), -rainSpeed);
            star.SetVelocity(rainForce);
            star.StartFloating();
        }
    }

    private StarScript GetStarFromPool()
    {
        if (starPool.Count > 0)
        {
            return starPool.Dequeue();
        }

        GameObject star = Instantiate(starPrefab, transform);
        StarScript starScript = star.GetComponent<StarScript>();
        starScript.OnReturn += ReturnToPool;

        LayoutGroup layoutGroup = star.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        return starScript;
    }

    private void ReturnToPool(StarScript star)
    {
        star.gameObject.SetActive(false);
        starPool.Enqueue(star);
    }
}
