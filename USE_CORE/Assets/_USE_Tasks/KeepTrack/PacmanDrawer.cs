using UnityEngine;
using UnityEngine.UI;

public class PacmanDrawer : MonoBehaviour
{
    private float MouthAngle;
    private Image image;

    private Texture2D CircleTexture;
    private int OriginalTextureWidth;
    private int OriginalTextureHeight;

    private Color[] BasePixels;

    public int ClosedLineThickness = 4; // default is 4, but they specify in the object config.

    public void ManualStart()
    {
        image = GetComponent<Image>();

        if (image == null)
        {
            Debug.LogError("PacmanDrawer: Image component not found.");
            return;
        }

        if (image.sprite == null)
        {
            Debug.LogError("PacmanDrawer: Image sprite is missing.");
            return;
        }

        CircleTexture = image.sprite.texture;

        if (CircleTexture == null)
        {
            Debug.LogError("PacmanDrawer: Sprite texture is missing.");
            return;
        }

        OriginalTextureWidth = CircleTexture.width;
        OriginalTextureHeight = CircleTexture.height;

        // Cache the original pixels once so we do not keep reading them from the texture.
        BasePixels = CircleTexture.GetPixels();
    }

    public void DrawMouth(float mouthAngle)
    {
        if (!IsReady())
            return;

        MouthAngle = mouthAngle;

        Texture2D texture = new Texture2D(OriginalTextureWidth, OriginalTextureHeight);
        ResetToFullCircle(texture);

        Color[] pixels = texture.GetPixels();
        for (int x = 0; x < OriginalTextureWidth; x++)
        {
            for (int y = 0; y < OriginalTextureHeight; y++)
            {
                if (IsInsideWedge(x, y) && IsInsideCircle(x, y))
                {
                    pixels[x + y * OriginalTextureWidth] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite newSprite = Sprite.Create(
            texture,
            new Rect(0, 0, OriginalTextureWidth, OriginalTextureHeight),
            new Vector2(0.5f, 0.5f)
        );

        image.sprite = newSprite;
    }

    public void DrawClosedMouth()
    {
        if (!IsReady())
            return;

        Texture2D texture = new Texture2D(OriginalTextureWidth, OriginalTextureHeight);
        ResetToFullCircle(texture);

        Color[] pixels = texture.GetPixels();

        int middleX = OriginalTextureWidth / 2;
        int middleY = OriginalTextureHeight / 2;
        int middleRightX = OriginalTextureWidth - 1;

        int startX = Mathf.Clamp(middleX, 0, OriginalTextureWidth - 1);
        int endX = Mathf.Clamp(middleRightX, 0, OriginalTextureWidth - 1);
        int startY = Mathf.Clamp(middleY - ClosedLineThickness / 2, 0, OriginalTextureHeight - 1);
        int endY = Mathf.Clamp(middleY + ClosedLineThickness / 2, 0, OriginalTextureHeight - 1);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                pixels[x + y * OriginalTextureWidth] = Color.black;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite newSprite = Sprite.Create(
            texture,
            new Rect(0, 0, OriginalTextureWidth, OriginalTextureHeight),
            new Vector2(0.5f, 0.5f)
        );

        image.sprite = newSprite;
    }

    private void ResetToFullCircle(Texture2D texture)
    {
        texture.SetPixels(BasePixels);
        texture.Apply();
    }

    private bool IsReady()
    {
        return image != null
            && CircleTexture != null
            && BasePixels != null
            && OriginalTextureWidth > 0
            && OriginalTextureHeight > 0;
    }

    bool IsInsideWedge(int x, int y)
    {
        Vector2 pixelPos = new Vector2(x, y);
        Vector2 pacmanCenter = new Vector2(OriginalTextureWidth / 2f, OriginalTextureHeight / 2f);
        Vector2 dirToPixel = pixelPos - pacmanCenter;

        float angleToPixel = Vector2.Angle(Vector2.right, dirToPixel);
        return angleToPixel <= MouthAngle * 0.5f;
    }

    bool IsInsideCircle(int x, int y)
    {
        Vector2 pacmanCenter = new Vector2(OriginalTextureWidth / 2f, OriginalTextureHeight / 2f);
        Vector2 pixelPos = new Vector2(x, y);

        return Vector2.Distance(pixelPos, pacmanCenter) <= OriginalTextureWidth / 2f;
    }
}