using System.Collections.Generic;
using UnityEngine;


public class MaskController : MonoBehaviour
{
    private List<Mask> Masks;
    public Sprite MaskSprite; //Assign in inspector
    [HideInInspector] public Canvas Canvas;



    public void Init()
    {
        Masks = new List<Mask>();
    }

    public string GetMaskVisibilityString()
    {
        if (Masks == null)
            return "[]";

        List<string> visibilities = new List<string>();

        if(Masks.Count > 0)
        {
            foreach(Mask mask in Masks)
            {
                if(mask.gameObject.activeInHierarchy)
                    visibilities.Add("MaskActive_" + mask.TargetGO.name);
            }
        }
        return visibilities.Count < 1 ? "[]" : $"[{string.Join(", ", visibilities)}]";
    }

    public string GetMaskPosString()
    {
        if (Masks == null)
            return "[]";

        List<string> positions = new List<string>();

        if (Masks.Count > 0)
        {
            foreach (Mask mask in Masks)
            {
                if (mask.gameObject.activeInHierarchy)
                    positions.Add(mask.gameObject.transform.position.ToString());
            }
        }
        return positions.Count < 1 ? "[]" : $"[{string.Join(", ", positions)}]";
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


        Mask maskComponent = maskObject.AddComponent<Mask>();
        maskComponent.TargetGO = targetObject;
        Masks.Add(maskComponent);

    }



    public void DestroyMask(GameObject maskObject)
    {
        Mask maskComponent = maskObject.GetComponent<Mask>();
        if (maskComponent != null)
        {
            if (Masks.Contains(maskComponent))
            {
                Masks.Remove(maskComponent);
                Destroy(maskObject);
            }
            else
                Debug.LogWarning("NOT IN THE MASK LIST!");
        }
        else
            Debug.LogWarning("TRIED TO DESTROY A MASK BUT THERES NO MASK COMPONENT ATTACHED");
    }

    public void DestroyAllMasks()
    {
        foreach(var mask in Masks)
        {
            Destroy(mask);
        }
        Masks.Clear();
    }

}



public class Mask : MonoBehaviour
{
    public GameObject TargetGO; //this is the stim that the mask is going to be over top of 
    public bool Masking
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
    }
    public Vector3 MaskPos
    {
        get
        {
            return gameObject.transform.localPosition;
        }
    }

}