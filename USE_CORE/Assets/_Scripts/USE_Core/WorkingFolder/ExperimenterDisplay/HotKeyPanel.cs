using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Cursor = UnityEngine.Cursor;
using ConfigDynamicUI;
using Newtonsoft.Json.Serialization;
using UnityEngine.EventSystems;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Session;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;

public class HotKeyPanel : ExperimenterDisplayPanel
{
    public HotKeyList HKList;
    public HotKeyList ConfigUIList;
    public GameObject hotKeyText;
    public delegate bool BoolDelegate();
    public delegate void VoidDelegate();


    public GameObject hotKeyPanel;

    public override void CustomPanelInitialization()
    {
        HKList = new HotKeyList();
        HKList.Initialize(this);

        ConfigUIList = new HotKeyList();
        ConfigUIList.InitializeConfigUI();
        hotKeyPanel = GameObject.Find("HotKeyPanel");
        hotKeyText = GameObject.Find("HotKeyText");
        hotKeyText.transform.SetParent(hotKeyPanel.GetComponent<Transform>());

        hotKeyText.GetComponent<Text>().supportRichText = true;
        hotKeyText.GetComponent<Text>().alignment = TextAnchor.UpperCenter;
        hotKeyText.GetComponent<Text>().text = "<size=25><b><color=#2d3436ff>Hot Keys</color></b></size>" + "\n\n<size=20>" + HKList.GenerateHotKeyDescriptions() + "</size>" + "\n-----------------------------------" +
            "\n\n<size=25><b><color=#2d3436ff>ConfigUI Control</color></b></size>" + "\n\n<size=20>" + ConfigUIList.GenerateConfigUIHotKeyDescriptions() + "</size>";

        
    }
    public override void CustomPanelUpdate()

    {
        HKList.CheckAllHotKeyConditions();
        ConfigUIList.CheckAllHotKeyConditions();
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
        List<Selectable> m_orderedSelectables = new List<Selectable>();
        List<HotKey> ConfigUIHotKeys = new List<HotKey>();
        private HotKeyPanel HkPanel;

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
        public string GenerateConfigUIHotKeyDescriptions()
        {
            string completeString = "";
            foreach (HotKey hk in ConfigUIHotKeys)
            {
                completeString = completeString + hk.GenerateTextDescription() + "\n";
            }

            // Debug.Log("ConfigUIHotKeyDescriptions: " + completeString);

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

            foreach (HotKey hk in ConfigUIHotKeys)
            {
                if (hk.hotKeyCondition())
                {
                    hk.hotKeyAction();
                }
            }
        }

        public void Initialize(HotKeyPanel hkPanel, Func<List<HotKey>> CustomHotKeyList = null)
        {
            HkPanel = hkPanel;
            if (CustomHotKeyList == null)
            {
                HotKeys = DefaultHotKeyList(); //this is your default function
            }

            else
            {
                HotKeys = CustomHotKeyList(); //allows users to specify task-specific lists - this will end up looking something like the various task-specific classes like WWW_TaskDef or whatever
                //ConfigUIHotKeys = CustomConfigUIHotKeyList();
            }


            //GenerateTextForPanel(); //method that loops through each hotkey and creates the string to show the hotkey options, using the GenerateTextDescription function of each on
        }

        public void InitializeConfigUI(Func<List<HotKey>> CustomConfigUIHotKeyList = null)
        {
            if (CustomConfigUIHotKeyList == null)
            {
                ConfigUIHotKeys = DefaultConfigUIHotKeyList();
            }

            else
            {
                ConfigUIHotKeys = CustomConfigUIHotKeyList(); //allows users to specify task-specific lists - this will end up looking something like the various task-specific classes like WWW_TaskDef or whatever

            }


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

            //End Block Hot Key
            HotKey endBlock = new HotKey
            {
                keyDescription = "N",
                actionName = "End Block",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.N),
                hotKeyAction = () =>
                {
                    HkPanel.TrialLevel.ForceBlockEnd = true;
                    HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("FinishTrial"));
                }
            };
            HotKeyList.Add(endBlock);

            // Quit Game Hot Key
            HotKey quitGame = new HotKey
            {
                keyDescription = "Esc",
                actionName = "Quit",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.Escape),
                hotKeyAction = () =>
                {
                    HkPanel.TaskLevel.Terminated = true;
                    HkPanel.SessionLevel.TasksFinished = true;
                }
            };
            HotKeyList.Add(quitGame);

            // Pause Game Hot Key
            HotKey pauseGame = new HotKey
            {
                keyDescription = "P",
                actionName = "Pause/Unpause Game",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.P),
                hotKeyAction = () =>
                {
                    if (!HkPanel.TaskLevel.Paused)
                    {
                        HkPanel.TaskLevel.Paused = true;
                    }
                    else
                    {
                        HkPanel.TaskLevel.Paused = false;
                    }
                }
            };
            HotKeyList.Add(pauseGame);
            
            

            return (HotKeyList);
        }

        public List<HotKey> DefaultConfigUIHotKeyList()
        {
            List<HotKey> ConfigUIHotKeyList = new List<HotKey>();
            List<Selectable> m_orderedSelectables = new List<Selectable>();
            ConfigUI configUIPanelController = GameObject.Find("Config UI").GetComponent<ConfigUI>();// new ConfigUI();

            //Scroll ConfigUI HotKey
            HotKey scrollConfig = new HotKey
            {
                keyDescription = "Tab",
                actionName = "Scroll",
                hotKeyCondition = () => Input.GetKeyDown(KeyCode.Tab),
                hotKeyAction = () =>
                {
                    configUIPanelController.HandleHotkeySelect(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift), true, false); // Navigate backward when holding shift, else navigate forward.
                }

            };
            ConfigUIHotKeyList.Add(scrollConfig);

            HotKey selection = new HotKey
            {
                keyDescription = "Enter",
                actionName = "Select",
                hotKeyCondition = () => (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)),
                hotKeyAction = () =>
                {
                    configUIPanelController.HandleHotkeySelect(false, false, true);
                }
            };
            ConfigUIHotKeyList.Add(selection);

            return (ConfigUIHotKeyList);

        }
        
    }
    
        

}







