using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


//Can be attached to a GameObject that has a Text child or a TextMeshPro child, and will change the font color on hover

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Color text_OriginalColor;
    private Color text_HoverColor = new Color(0, 0, 0);

    private Text text;
    private TextMeshProUGUI tmp_Text;

    private void Awake()
    {
        text = gameObject.GetComponentInChildren<Text>();
        if (text != null)
            text_OriginalColor = text.color;

        tmp_Text = gameObject.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp_Text != null)
            text_OriginalColor = tmp_Text.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (text != null)
            text.color = text_HoverColor;

        if (tmp_Text != null)
            tmp_Text.color = text_HoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (text != null)
            text.color = text_OriginalColor;

        if (tmp_Text != null)
            tmp_Text.color = text_OriginalColor;
    }

}
