using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class MaskController : MonoBehaviour
{
    private List<GameObject> MaskList;
    public Sprite MaskSprite; //Assign in inspector
    [HideInInspector] public Canvas Canvas;



    public void Init(DataController frameData)
    {
        //frameData.AddDatum("HeldTooLong", () => Error_Dict["HeldTooLong"]);

        MaskList = new List<GameObject>();
    }

    public void CreateMask(GameObject targetObject, float transparency)
    {
        GameObject maskObject = new GameObject("Mask_" + targetObject.name)
        {
            layer = LayerMask.NameToLayer("Ignore Raycast")
        };

        StimDefPointer pointer = targetObject?.GetComponent<StimDefPointer>();
        if (pointer != null)
            pointer.StimDef.MaskGameObject = maskObject;
        else
            Debug.LogError("NO POINTER FOUND ON THE TARGET OBJECT!");

        SpriteRenderer maskRenderer = maskObject.AddComponent<SpriteRenderer>();

        if(transparency > 1 || transparency < 0)
            Debug.LogError("TRANSPARENCY VALUE NOT BETWEEN 0 AND 1");

        int alphaValue = Mathf.RoundToInt(transparency * 255);
        maskRenderer.color = new Color32(120, 120, 120, (byte)alphaValue);

        maskRenderer.sprite = MaskSprite;
        maskRenderer.sortingOrder = 999; // High number to ensure it's rendered in front

        maskObject.transform.SetParent(Canvas.transform, false);

        Vector3 worldPosition;
        float diameter;

        if (targetObject.transform != null && targetObject.GetComponent<RectTransform>() == null)
        {
            // For 3D objects:
            Transform objectTransform = targetObject.transform;
            worldPosition = objectTransform.position;
            diameter = Mathf.Max(objectTransform.localScale.x, objectTransform.localScale.y) * 250f;
        }
        else
        {
            // For 2D objects:
            RectTransform rectTransform = targetObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector3[] worldCorners = new Vector3[4];
                rectTransform.GetWorldCorners(worldCorners);

                float width = Vector3.Distance(worldCorners[0], worldCorners[3]);
                float height = Vector3.Distance(worldCorners[0], worldCorners[1]);
                diameter = Mathf.Max(width, height);

                worldPosition = rectTransform.position;
            }
            else
            {
                Debug.LogWarning("NO RECT TRANSFORM!");
                return;
            }
        }

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas.GetComponent<RectTransform>(), screenPosition, Camera.main, out Vector2 canvasPosition);
        maskObject.transform.localPosition = canvasPosition;

        maskObject.transform.localScale = new Vector3(diameter, diameter, 1);

        MaskList.Add(maskObject);

    }



    public void DestroyMask(GameObject maskObject)
    {
        if (MaskList.Contains(maskObject))
        {
            MaskList.Remove(maskObject);
            Destroy(maskObject);
        }
        else
            Debug.LogWarning("CAN NOT DESTROY BECAUSE THE GAMEOBJECT IS NOT IN THE MASK-LIST!");
    }

    public void DestroyAllMasks()
    {
        foreach(var mask in MaskList)
        {
            Destroy(mask);
        }
        MaskList.Clear();
    }

}
