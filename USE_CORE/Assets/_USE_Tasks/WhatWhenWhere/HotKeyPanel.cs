using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HotKeyPanel : MonoBehaviour
{
    public HotKeyList HotKeyList;


    public void Update()

    {
        HotKeyList.CheckAllHotKeyConditions();
    }

    public class HotKey
    {
        public string keyDescription;
        public string actionName;
        public Action hotKeyAction;
        public bool hotKeyCondition;

        string GenerateTextDescription()
        {
            return keyDescription + " -> " + actionName;
        }

    }

    public class HotKeyList
    {
        List<HotKey> HotKeys = new List<HotKey>();

        public void CheckAllHotKeyConditions()
        {
            foreach (HotKey hk in HotKeys)
            {
                if (hk.hotKeyCondition)
                {
                    hk.hotKeyAction();
                }
            }
        }

        public void Initialize(Func<List<HotKey>> CustomHotKeyList = null)
        {
            if (CustomHotKeyList == null)
                HotKeys = DefaultHotKeyList(); //this is your default function
            else
                HotKeys = CustomHotKeyList(); //allows users to specify task-specific lists - this will end up looking something like the various task-specific classes like WWW_TaskDef or whatever

            //GenerateTextForPanel(); //method that loops through each hotkey and creates the string to show the hotkey options, using the GenerateTextDescription function of each on
        }

        public List<HotKey> DefaultHotKeyList()
        {
            List<HotKey> HotKeyList = new List<HotKey>();
            // Toggle Displays HotKey
            HotKey toggleDisplays = new HotKey
            {
                keyDescription = "W",
                actionName = "ToggleDisplays",
                hotKeyCondition = InputBroker.GetKeyUp(KeyCode.W),
                hotKeyAction = () =>
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
                }
            };
            HotKeyList.Add(toggleDisplays);
            // Remove Cursor Hot Key
            HotKey removeCursor = new HotKey
            {
                keyDescription = "Shift + X",
                actionName = "RemoveCursor",
                hotKeyCondition = (InputBroker.GetKey(KeyCode.LeftShift) || InputBroker.GetKey(KeyCode.RightShift)) && InputBroker.GetKeyUp(KeyCode.X),
                hotKeyAction = () => Cursor.visible = false
            };
            HotKeyList.Add(removeCursor);

            // Show Cursor Hot Key
            HotKey showCursor = new HotKey
            {
                keyDescription = "Shift + C",
                actionName = "ShowCursor",
                hotKeyCondition = (InputBroker.GetKey(KeyCode.LeftShift) || InputBroker.GetKey(KeyCode.RightShift)) && InputBroker.GetKeyUp(KeyCode.C),
                hotKeyAction = () => Cursor.visible = true
            };

            HotKeyList.Add(showCursor);

            // Quit Game Hot Key
            HotKey quitGame = new HotKey
            {
                keyDescription = "Esc",
                actionName = "QuitGame",
                hotKeyCondition = Input.GetKey("escape"),
                hotKeyAction = () => Application.Quit()
            };
            HotKeyList.Add(quitGame);

            return (HotKeyList);
        }



    }


}







