using UnityEngine;
using UnityEngine.UI;

public class GradientBackground : MonoBehaviour
{
    public Image backgroundImage;
    public Color topColor = new Color();
    public Color bottomColor = new Color();

    private Texture2D gradientTexture;

    private void Start()
    {
        CreateGradientTexture();
    }

    void CreateGradientTexture()
    {
        int width = 256;
        int height = 256;
        gradientTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            Color pixelColor = Color.Lerp(topColor, bottomColor, t);

            for (int x = 0; x < width; x++)
            {
                gradientTexture.SetPixel(x, y, pixelColor);
            }
        }

        gradientTexture.Apply();
        gradientTexture.wrapMode = TextureWrapMode.Clamp;

        backgroundImage.sprite = Sprite.Create(gradientTexture,
            new Rect(0, 0, width, height), Vector2.one * 0.5f);
    }

    // Метод для изменения цветов во время игры
    public void SetGradientColors(Color top, Color bottom)
    {
        topColor = top;
        bottomColor = bottom;
        CreateGradientTexture();
    }
}
