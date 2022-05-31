using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Cursor = UnityEngine.Cursor;

public class HotKeyPanel : MonoBehaviour
{
    public HotKeyList HkList;
    public GameObject hotKeyText;
    public delegate bool BoolDelegate();
    public delegate void VoidDelegate();
    

    public void Start()
    {
        HkList = new HotKeyList();
        HkList.Initialize();
        hotKeyText = transform.Find("HotKey2").gameObject;

        hotKeyText.GetComponent<Text>().supportRichText = true;
        hotKeyText.GetComponent<Text>().text = "<size=35><b>Hot Keys</b></size>" + "\n<size=20>" + HkList.GenerateHotKeyDescriptions() + "</size>";
    }
    

    public void Update()

    {
        HkList.CheckAllHotKeyConditions();
    }

    public class HotKey
    {
        public string keyDescription;
        public string actionName;
        public VoidDelegate hotKeyAction;
        public BoolDelegate hotKeyCondition;

        public string GenerateTextDescription()
        {
            return keyDescription + " -> " + actionName;
        }

    }

    public class HotKeyList
    {
        List<HotKey> HotKeys = new List<HotKey>();
        
        public string GenerateHotKeyDescriptions()
        {
            string completeString = "";
            foreach (HotKey hk in HotKeys)
            {
                completeString = completeString + hk.GenerateTextDescription() + "\n";
            }
            
            Debug.Log("HotKeyDescriptions: " + completeString);
            
            return completeString;
        }
        
        public void CheckAllHotKeyConditions()
        {
            
            foreach (HotKey hk in HotKeys)
            {
                if (hk.hotKeyCondition())
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
                actionName = "Toggle Displays",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.W),
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
            HotKey toggleCursor = new HotKey
            {
                keyDescription = "C",
                actionName = "Cursor Visibility",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.C),
                hotKeyAction = () =>
                {
                    if (Cursor.visible)
                        Cursor.visible = false;
                    else
                        Cursor.visible = true;
                }

            };
            HotKeyList.Add(toggleCursor);
            
            // Quit Game Hot Key
            HotKey quitGame = new HotKey
            {
                keyDescription = "Esc",
                actionName = "Quit",
                hotKeyCondition = () => Input.GetKey("escape"),
                hotKeyAction = () => 
                {
                    #if UNITY_EDITOR
                    {
                        UnityEditor.EditorApplication.isPlaying = false;
                    }
                    #endif
                    {
                        Application.Quit();
                    }
                }
            };
            HotKeyList.Add(quitGame);

            // Pause Game Hot Key
            HotKey pauseGame = new HotKey
            {
                keyDescription = "P",
                actionName = "Pause",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.P),
                hotKeyAction = () =>
                {
                    #if UNITY_EDITOR
                    {
                        if (!UnityEditor.EditorApplication.isPaused)
                        {
                           
                        }
                    }
                    #endif
                    {
                        Application.Quit();
                    }
                }
            };
            HotKeyList.Add(pauseGame);

            return (HotKeyList);
        }



    }


}







