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

    private Color OriginalPlayTextColor;
    public Color HoverPlayTextColor; //Set in inspector

    private void Start()
    {
        OriginalPlayTextColor = PlayText.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        backgroundPanelImage.color = new Color(0f, 0f, 0f, .4f);
        //PlayText.color = HoverPlayTextColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        backgroundPanelImage.color = new Color(0f, 0f, 0f, 0f);
        //PlayText.color = OriginalPlayTextColor;
    }
}
