using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using USE_UI;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RawImage image;
    private Vector3 originalScale;

    private void Awake()
    {
        image = GetComponent<RawImage>();
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


    public void SetToInitialSize()
    {
        transform.localScale = originalScale;
    }
}