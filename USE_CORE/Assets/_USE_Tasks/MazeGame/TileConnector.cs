using UnityEngine;
using UnityEngine.UI.Extensions;

public class TileConnector : MonoBehaviour
{
    public GameObject startObject; // The starting object
    public GameObject endObject;   // The ending object
    public float animationDuration = 2.0f; // Duration of the animation in seconds

    private UILineRenderer lineRenderer;
    private float elapsedTime = 0f;
    private bool isAnimating = false;

    void Start()
    {
        lineRenderer = GetComponent<UILineRenderer>();

        // Set the initial transparency
        SetLineMaterialTransparent(0f);
    }

    void Update()
    {
        if (isAnimating)
        {
            // Gradually fill up the line
            if (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;

                float t = Mathf.Clamp01(elapsedTime / animationDuration);
                lineRenderer.Points = new Vector2[] {  startObject.transform.position, Vector2.Lerp(startObject.transform.position, endObject.transform.position, t) };

                // Increase transparency from right to left
                SetLineMaterialTransparent(t);
                Debug.LogWarning("ANIMATING");
            }
            else
            {
                // Animation complete, do something when the animation finishes
                // e.g., Light up the subsequent object
                SetLineMaterialOpaque();

                // Reset variables for potential future animations
                ResetAnimation();
            }
        }
    }

    void SetLineMaterialTransparent(float transparency)
    {
        Color color = lineRenderer.material.color;
        color.a = transparency;
        lineRenderer.material.color = color;
    }

    void SetLineMaterialOpaque()
    {
        SetLineMaterialTransparent(1f);
    }

    void ResetAnimation()
    {
        elapsedTime = 0f;
        isAnimating = false;
    }

    // Call this method to start the animation
    public void StartAnimation(GameObject startTile, GameObject endTile)
    {
        isAnimating = true;
        startObject = startTile;
        endObject = endTile;
    }
}
