using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


public class ImageHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image backgroundPanelImage;
    public TextMeshProUGUI PlayText;

    public Color HoverPlayTextColor; //Set in inspector


    public void OnPointerEnter(PointerEventData eventData)
    {
        backgroundPanelImage.color = new Color(0f, 0f, 0f, .4f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        backgroundPanelImage.color = new Color(0f, 0f, 0f, 0f);
    }
}
