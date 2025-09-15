using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class LeaderboardItem : MonoBehaviour
{
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private RawImage playerImage;

    public void SetData(int number, string name, int score, string photoUrl)
    {
        if(numberText != null)
            numberText.text = "" + number;

        if (playerNameText != null)
            playerNameText.text = name;

        if (scoreText != null)
            scoreText.text = "" + score;

        if (!string.IsNullOrEmpty(photoUrl) && playerImage != null)
        {
            StartCoroutine(LoadPlayerImage(photoUrl));
        }
    }

    private IEnumerator LoadPlayerImage(string url)
    {
        // Исправляем домен если нужно
        url = url.Replace("misterimrt.ru", "misterimrt.online");
        Debug.Log($"Loading image from URL: {url}"); // добавляем лог

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                if (texture != null && this != null && playerImage != null)
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.anisoLevel = 16;
                    playerImage.texture = texture;
                    Debug.Log($"Image loaded successfully");
                }
            }
            else
            {
                Debug.LogError($"Failed to load image: {www.error}\nURL: {url}\nResponse Code: {www.responseCode}");
            }
        }
    }

    private void OnDestroy()
    {
        if (playerImage != null && playerImage.texture != null)
        {
            Destroy(playerImage.texture);
        }
    }
}
