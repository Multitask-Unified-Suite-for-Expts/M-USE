using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


public class ImageHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image backgroundImage;
    private Color originalBackgroundColor;

    //Set in inspector:
    public Color backgroundHoverColor;


    private void Start()
    {
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
            Debug.LogError("IMAGE IS NULL");
        originalBackgroundColor = backgroundImage.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        backgroundImage.color = backgroundHoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        backgroundImage.color = originalBackgroundColor;
    }
}
