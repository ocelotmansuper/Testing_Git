using UnityEngine;
using UnityEngine.EventSystems;

public class PlatfromController : MonoBehaviour
{
    [SerializeField] private GameObject mobileVersionPrefab;
    [SerializeField] private GameObject desktopVersionPrefab;
    [SerializeField] private GameManager gameManager;

    private IGameUI currentUI;

    private void Start()
    {
        EnsureEventSystemExists();
        InitializePlatformUI();
    }

    private void EnsureEventSystemExists()
    {
        // Проверяем наличие EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            // Создаем новый GameObject с EventSystem и Input Module
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }

    private void InitializePlatformUI()
    {
        Debug.Log("InitializePlatformUI called");

        // Определяем платформу
        bool isMobile = IsMobilePlatform();

        Debug.Log($"Platform detected as: {(isMobile ? "Mobile" : "Desktop")}");

        // Проверяем префабы
        if (mobileVersionPrefab == null)
            Debug.LogError("Mobile UI prefab is not set!");
        if (desktopVersionPrefab == null)
            Debug.LogError("Desktop UI prefab is not set!");
        if (gameManager == null)
            Debug.LogError("GameManager reference is not set!");

        // Создаем нужную версию UI
        GameObject uiPrefab = isMobile ? mobileVersionPrefab : desktopVersionPrefab;
        if (uiPrefab == null)
        {
            Debug.LogError($"UI prefab is null. Mobile: {mobileVersionPrefab != null}, Desktop: {desktopVersionPrefab != null}");
            return;
        }

        GameObject uiInstance = Instantiate(uiPrefab);
        Debug.Log($"UI instance created: {uiInstance != null}");

        // Получаем компонент UI
        currentUI = uiInstance.GetComponent<IGameUI>();
        currentUI?.SetupUI(gameManager);
        if (currentUI == null)
        {
            Debug.LogError("UI prefab doesn't implement IGameUI interface!");
            return;
        }

        // Проверяем, что GameManager существует
        if (gameManager == null)
        {
            Debug.LogError("GameManager is not set in PlatformController!");
            return;
        }

        // Настройка UI и связывание с GameManager
        currentUI.SetupUI(gameManager);
        gameManager.SetUI(currentUI);

        Debug.Log("Platform UI initialization completed");
    }

    private bool IsMobilePlatform()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        bool isMobile = IsMobileDevice();
        Debug.Log($"[Platform Detection] Running in WebGL - Platform detected: {(isMobile ? "Mobile" : "Desktop")}");
        return isMobile;
#else
        bool isMobile = Application.isMobilePlatform;
        Debug.Log($"[Platform Detection] Running in Unity - Platform detected: {(isMobile ? "Mobile" : "Desktop")}");
        return isMobile;
#endif
    }

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern bool IsMobileDevice();

    // Метод для проверки состояния UI в редакторе
    private void OnValidate()
    {
        if (mobileVersionPrefab == null)
            Debug.LogWarning("Mobile UI prefab is not set in PlatformController");
        if (desktopVersionPrefab == null)
            Debug.LogWarning("Desktop UI prefab is not set in PlatformController");
        if (gameManager == null)
            Debug.LogWarning("GameManager reference is not set in PlatformController");
    }
}
