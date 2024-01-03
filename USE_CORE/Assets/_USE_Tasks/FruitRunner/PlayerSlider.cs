using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlider : MonoBehaviour
{
    public Slider slider;
    public GameObject player;

    public Canvas canvas;


    public void ManualStart(Canvas parent)
    {
        canvas = parent;

        GameObject sliderGO = Instantiate(Resources.Load<GameObject>("Prefabs/Player_Slider"));
        slider = sliderGO.GetComponent<Slider>();

        slider.value = 1;

        slider.gameObject.name = "PlayerMovementSlider";
        slider.gameObject.transform.SetParent(canvas.transform);
        slider.gameObject.transform.localPosition = new Vector3(0f, -(Screen.height * .45f), 0f);
        slider.gameObject.transform.localScale = Vector3.one;
        slider.gameObject.transform.localRotation = Quaternion.identity;

        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        Debug.LogWarning("CHANGED");
        float targetX = Mathf.Lerp(-1f, 1f, value);
    }

}
