using UnityEngine;
using UnityEngine.EventSystems;


public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * 1.1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }

    public void SetToInitialSize() //Used by sessionLevel to reset square size after grey'd out. 
    {
        transform.localScale = originalScale;
    }

}