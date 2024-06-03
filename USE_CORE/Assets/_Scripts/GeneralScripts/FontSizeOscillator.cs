using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FontSizeOscillator : MonoBehaviour
{
    private TextMeshProUGUI text;
    public float minFontSize = 20f;
    public float maxFontSize = 40f;
    public float speed = 1f;

    private float elapsedTime = 0f;

    void Start()
    {
        if (text == null)
        {
            text = GetComponent<TextMeshProUGUI>();
        }

        if (text == null)
        {
            Debug.LogError("TextMeshPro component not found on the GameObject.");
        }
    }

    void Update()
    {
        if (text != null)
        {
            elapsedTime += Time.deltaTime * speed;
            float fontSize = Mathf.Lerp(minFontSize, maxFontSize, (Mathf.Sin(elapsedTime) + 1f) / 2f);
            text.fontSize = fontSize;
        }
    }
}
