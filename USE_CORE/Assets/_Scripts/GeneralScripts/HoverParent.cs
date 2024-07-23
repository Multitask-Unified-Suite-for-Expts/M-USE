using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class HoverParent : MonoBehaviour
{
    private HoverDetector[] hoverDetectors;

    public Image image;
    public Color hoverColor;
    Color originalColor;


    void Start()
    {
        hoverDetectors = GetComponentsInChildren<HoverDetector>();

        if (hoverDetectors == null || hoverDetectors.Length == 0)
            Debug.LogError("No HoverDetector components found in children!");
     
        if (image == null)
            Debug.LogError("Image is null! Need to set it in the inspector!");

        originalColor = image.color;
    }

    void Update()
    {
        if (image == null)
            return;

        bool hovering = false;

        foreach (var detector in hoverDetectors)
        {
            if (detector.isHovered)
            {
                hovering = true;
                break;
            }
        }

        image.color = hovering ? hoverColor : originalColor;
    }
}
