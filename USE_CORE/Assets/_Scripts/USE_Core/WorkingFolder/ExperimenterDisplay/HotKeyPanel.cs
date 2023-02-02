using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Cursor = UnityEngine.Cursor;
using ConfigDynamicUI;
using USE_ExperimenterDisplay;
using UnityEngine.SceneManagement;

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
            //HotKey toggleDisplays = new HotKey
            //{
            //    keyDescription = "W",
            //    actionName = "Toggle Displays",
            //    hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.W),
            //    hotKeyAction = () =>
            //    {
            //        var cams = GameObject.FindObjectsOfType<Camera>();
            //        foreach (Camera c in cams) //MirrorCam:0, BackgroundCamera:1, CR_Cam: 0, MainCameraCopy:1 (DC)
            //        {
            //            Debug.Log(c.name + " before:" + c.targetDisplay);
            //            c.targetDisplay = 1 - c.targetDisplay; // 1 - 0 = 1; 1 - 1 = 0
            //            Debug.Log(c.name + " after:" + c.targetDisplay);
            //        }
            //        var canvases = GameObject.FindObjectsOfType<Canvas>();
            //        foreach (Canvas c in canvases) //ExperimenterCanvas: 1, TaskSelectionCanvas:0 (DC), InitScreenCanvas:1, CR_Canvas:0 (DC)
            //        {
            //            Debug.Log(c.name + " before:" + c.targetDisplay);
            //            c.targetDisplay = 1 - c.targetDisplay; // 1 - 0 = 1; 1 - 1 = 0
            //            Debug.Log(c.name + " after:" + c.targetDisplay);
            //        }
            //        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            //    }
            //};
            //HotKeyList.Add(toggleDisplays);

            // Remove Cursor Hot Key
            HotKey toggleCursor = new HotKey
            {
                keyDescription = "C",
                actionName = "Cursor Visibility",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.C),
                hotKeyAction = () =>
                {
                    Cursor.visible = !Cursor.visible;
                }

            };
            HotKeyList.Add(toggleCursor);


            //RestartBlock Hot Key
            //NOT WORKING. CR starts over but it thinks its TrialCountInBlock1 cuz incremented in setupTrial. Also tokenbar not resetting. 
            //HotKey restartBlock = new HotKey
            //{
            //    keyDescription = "R",
            //    actionName = "Restart Block",
            //    hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.R),
            //    hotKeyAction = () =>
            //    {
            //        //HkPanel.TrialLevel.ForceBlockEnd = true;
            //        HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("FinishTrial"));
            //        HkPanel.TaskLevel.BlockCount--;
            //        HkPanel.TrialLevel.TrialCount_InBlock = 0;
            //    }
            //};
            //HotKeyList.Add(restartBlock);

            ////PreviousBlock Hot Key
            //HotKey previousBlock = new HotKey
            //{
            //    keyDescription = "B",
            //    actionName = "Previous Block",
            //    hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.B),
            //    hotKeyAction = () =>
            //    {
            //        if (HkPanel.TrialLevel.BlockCount == 0)
            //            return;
            //        else
            //        {
            //            //HkPanel.TrialLevel.ForceBlockEnd = true;
            //            HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("FinishTrial"));
            //            HkPanel.TaskLevel.BlockCount -= 2;
            //            HkPanel.TrialLevel.TrialCount_InBlock = 0;
            //        }
            //    }
            //};
            //HotKeyList.Add(previousBlock);

            //End Block Hot Key
            HotKey endBlock = new HotKey
            {
                keyDescription = "N",
                actionName = "End Block",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.N),
                hotKeyAction = () =>
                {
                    HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("ITI"));
                    HkPanel.TrialLevel.ForceBlockEnd = true;
                }
            };
            HotKeyList.Add(endBlock);

            //EndTask Hot Key
            HotKey endTask = new HotKey
            {
                keyDescription = "E",
                actionName = "End Task",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.E),
                hotKeyAction = () =>
                {
                    HkPanel.TrialLevel.ForceBlockEnd = true; 
                    HkPanel.TaskLevel.SpecifyCurrentState(HkPanel.TaskLevel.GetStateFromName("FinishTask"));
                }
            };
            HotKeyList.Add(endTask);

            // Quit Game Hot Key
            HotKey quitGame = new HotKey
            {
                keyDescription = "Esc",
                actionName = "Quit",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.Escape),
                hotKeyAction = () =>
                {
                    HkPanel.TrialLevel.ForceBlockEnd = true;
                    HkPanel.TaskLevel.Terminated = true;
                    HkPanel.SessionLevel.TasksFinished = true;
                    HkPanel.SessionLevel.SpecifyCurrentState(HkPanel.SessionLevel.GetStateFromName("FinishSession"));
                }
            };
            HotKeyList.Add(quitGame);

            // Pause Game Hot Key
            HotKey pauseGame = new HotKey
            {
                keyDescription = "P",
                actionName = "Pause/Unpause (ends trial)",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.P),
                hotKeyAction = () =>
                {
                    if (!HkPanel.TrialLevel.Paused) //Note: using paused variable and not hkpanel.trialLevel.paused, because I want it to fast forward to next trial while paused. 
                    {
                        //Fast forward to end of trial:
                        HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("ITI"));

                        int trialsInBlock = HkPanel.TaskLevel.currentBlockDef.TrialDefs.Count;
                        int trialCountInBlock = HkPanel.TrialLevel.TrialCount_InBlock;
                        //If more trials in current block, end the block:
                        if (trialsInBlock > 1 && trialCountInBlock+1 < trialsInBlock)
                            HkPanel.TrialLevel.ForceBlockEnd = true;

                        //Deactivate Controllers (so that tokenbar not still on screen):
                        GameObject controllers = GameObject.Find("Controllers");
                        if (controllers != null)
                            controllers.SetActive(false);

                        //Turn gray pause screen on:
                        HkPanel.SessionLevel.PauseCanvasGO.SetActive(true);
                        //Send abort code: 
                        HkPanel.TrialLevel.AbortCode = 1;
                        //Pause Trial Level
                        HkPanel.TrialLevel.Paused = true;
                    }
                    else
                    {
                        HkPanel.SessionLevel.PauseCanvasGO.SetActive(false);
                        GameObject controllers = GameObject.Find("Controllers");
                        if(controllers == null)
                            HkPanel.SessionLevel.FindInactiveGameObjectByName("Controllers").SetActive(true);
                        HkPanel.TrialLevel.Paused = false;
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







