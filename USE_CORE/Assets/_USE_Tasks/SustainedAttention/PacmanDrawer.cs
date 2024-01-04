using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PacmanDrawer : MonoBehaviour
{
    private float MouthAngle;
    private Image image;

    private Texture2D CircleTexture;
    private int OriginalTextureWidth;
    private int OriginalTextureHeight;

    public int ClosedLineThickness = 4; //default is 4, but they specify in the object config. 

    public void ManualStart()
    {
        image = GetComponent<Image>();
        CircleTexture = image.sprite.texture;
        OriginalTextureWidth = CircleTexture.width;
        OriginalTextureHeight = CircleTexture.height;
    }

    public void DrawMouth(float mouthAngle)
    {
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
                    pixels[x + y * OriginalTextureWidth] = Color.clear; // Make the pixels in the wedge transparent
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, OriginalTextureWidth, OriginalTextureHeight), new Vector2(0.5f, 0.5f));
        image.sprite = newSprite;
    }

    public void DrawClosedMouth()
    {
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

        Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, OriginalTextureWidth, OriginalTextureHeight), new Vector2(0.5f, 0.5f));
        image.sprite = newSprite;
    }

    private void ResetToFullCircle(Texture2D texture)
    {
        texture.SetPixels(CircleTexture.GetPixels());
        texture.Apply();
    }

    bool IsInsideWedge(int x, int y)
    {
        Vector2 pixelPos = new Vector2(x, y);
        Vector2 pacmanCenter = new Vector2(OriginalTextureWidth / 2, OriginalTextureHeight / 2);
        Vector2 dirToPixel = pixelPos - pacmanCenter;

        float angleToPixel = Vector2.Angle(Vector2.right, dirToPixel);
        return angleToPixel <= MouthAngle * 0.5f;
    }

    bool IsInsideCircle(int x, int y)
    {
        Vector2 pacmanCenter = new Vector2(OriginalTextureWidth / 2, OriginalTextureHeight / 2);
        Vector2 pixelPos = new Vector2(x, y);

        return Vector2.Distance(pixelPos, pacmanCenter) <= OriginalTextureWidth / 2;
    }
}
