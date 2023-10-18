using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using USE_UI;

public class TouchPositionDebug : MonoBehaviour, IPointerClickHandler
{
    private static UI_Debugger debugger;
    private static string text;


    private void Start()
    {
        if (debugger == null)
        {
            debugger = new UI_Debugger();
            string text = "";
            debugger.InitDebugger(GameObject.Find("InitScreenCanvas").GetComponent<Canvas>(), new Vector2(450, 100), new Vector3(500f, -200f, 0f), text);
            debugger.SetTextColor(Color.cyan);
            debugger.ActivateDebugText();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        text = "Circle Click Pos: \n" + eventData.position.x + ", " + eventData.position.y;
        debugger.SetDebugText(text);
    }
}
