using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_UI;

public class TouchDebugger : MonoBehaviour
{
    private static UI_Debugger debugger;
    private static string text;

    private void Start()
    {
        debugger = new UI_Debugger();
        text = "";
        debugger.InitDebugger(GameObject.Find("InitScreenCanvas").GetComponent<Canvas>(), new Vector2(550, 100), new Vector3(0f, 400f, 0f), text);
        debugger.SetTextColor(Color.green);
        debugger.ActivateDebugText();
    }

    private void Update()
    {
        if (InputBroker.GetMouseButtonDown(0))
        {
            text = "Touch Pos: " + InputBroker.mousePosition.ToString();
            debugger.SetDebugText(text);
        }
    }
}
