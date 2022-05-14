using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotKeys : MonoBehaviour
{
    string KeyDescription;
    string ActionName;
    //Func<string> HotkeyAction;
    bool HotkeyCondition;
    public bool toggleDisplay = false;
    public bool removeCursor = false;
    public bool showCursor = false;
    public bool quitGame = false;
    // Use this for initialization
    void Start()
    {
        
    }

    void ToggleDisplay()
    {
        if (toggleDisplay)
        {
            var cams = GameObject.FindObjectsOfType<Camera>();
            foreach (Camera c in cams)
            {
                c.targetDisplay = 1 - c.targetDisplay; // 1 - 0 = 1; 1 - 1 = 0
            }

            var canvases = GameObject.FindObjectsOfType<Canvas>();
            foreach (Canvas c in canvases)
            {
                c.targetDisplay = 1 - c.targetDisplay; // 1 - 0 = 1; 1 - 1 = 0
            }
            toggleDisplay = false;
        }
    }

    void RemoveCursor()
    {
        if (removeCursor)
        {
            Cursor.visible = false;
            removeCursor = false;
        }
    }

    void ShowCursor()
    {
        if (showCursor)
        {
            Cursor.visible = true;
            showCursor = false;
        }
    }

    void QuitGame()
    {
        if (quitGame)
        {
           Application.Quit();
           quitGame = false;
        }
    }

    void Update()
    {
        if (InputBroker.GetKeyUp(KeyCode.W))
        {
            //toggle between displays
            toggleDisplay = true;
        }
        if ((InputBroker.GetKey(KeyCode.LeftShift) || InputBroker.GetKey(KeyCode.RightShift)) && InputBroker.GetKeyUp(KeyCode.X))
        {
            //remove cursor from the screen
            removeCursor = true;
        }
        if ((InputBroker.GetKey(KeyCode.LeftShift) || InputBroker.GetKey(KeyCode.RightShift)) && InputBroker.GetKeyUp(KeyCode.C))
        {
            //cursor reappears on the screen
            showCursor = true;
        }
        if (Input.GetKey("escape"))
        {
            //immediately terminates game
            quitGame = true;
        }

        ToggleDisplay();
        RemoveCursor();
        ShowCursor();
        QuitGame();
    }
}
