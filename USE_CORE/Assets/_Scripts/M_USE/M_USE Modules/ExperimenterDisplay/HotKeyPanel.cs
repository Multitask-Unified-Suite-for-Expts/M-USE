using System.Collections.Generic;
using UnityEngine;
using System;
using USE_UI;
using UnityEngine.UI;
using Cursor = UnityEngine.Cursor;
using ConfigDynamicUI;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Trial;
using USE_ExperimentTemplate_Task;

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
        }

        public List<HotKey> DefaultHotKeyList()
        {
            List<HotKey> HotKeyList = new List<HotKey>();
            SessionInfoPanel SessionInfoPanel = GameObject.Find("SessionInfoPanel").GetComponent<SessionInfoPanel>();
            ControlLevel_Task_Template OriginalTaskLevel = null, GazeCalibrationTaskLevel = null;
            ControlLevel_Trial_Template OriginalTrialLevel = null, GazeCalibrationTrialLevel = null;

            // Toggle Displays HotKey
            HotKey toggleDisplays = new HotKey
            {
                keyDescription = "W",
                actionName = "Toggle Displays",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.W),
                hotKeyAction = () =>
                {
                    var cams = GameObject.FindObjectsOfType<Camera>();
                    foreach (Camera c in cams) //MirrorCam (0 to 1), BackgroundCamera (1 to 0), TaskCam (0 to 1), MainCameraCopy(1 DC!!)
                        c.targetDisplay = 1 - c.targetDisplay; // 1 - 0 = 1; 1 - 1 = 0
                    
                    var canvases = GameObject.FindObjectsOfType<Canvas>();
                    foreach (Canvas c in canvases) //ExperimenterCanvas: 1, TaskSelectionCanvas:0 (DC), InitScreenCanvas:1, CR_Canvas:0 (DC)
                    {
                        if (c.renderMode == RenderMode.ScreenSpaceCamera) //TaskSelectionCanvas (0 to 1 ),
                        {
                            Debug.Log("CAM REND BEFORE = " + c.name + " " + c.worldCamera.targetDisplay);
                            c.worldCamera.targetDisplay = 1 - c.worldCamera.targetDisplay;
                            Debug.Log("CAM REND AFTER = " + c.name + " " + c.worldCamera.targetDisplay);
                        }

                        else //ExperimenterCanvas (1 to 0), InitScreenCanvas (1 to 0),
                        {
                            Debug.Log("BEFORE = " + c.name + " " + c.targetDisplay);
                            c.targetDisplay = 1 - c.targetDisplay; // 1 - 0 = 1; 1 - 1 = 0
                            Debug.Log("AFTER = " + c.name + " " + c.targetDisplay);
                        }
                    }
                    //SceneManager.LoadScene(SceneManager.GetActiveScene().name); //Doesn't work. Will load task again but without TaskSelection scene.
                }
            };
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


            // Pause Game Hot Key
            HotKey pauseGame = new HotKey
            {
                keyDescription = "P",
                actionName = "Pause Game",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.P),
                hotKeyAction = () =>
                {
                    Debug.Log("---PRESSED PAUSE GAME HOT KEY---");
                    if(HkPanel.TrialLevel != null)
                        Time.timeScale = Time.timeScale == 1 ? 0 : 1;
                }
            };
            HotKeyList.Add(pauseGame);


            //RestartBlock Hot Key
            HotKey restartBlock = new HotKey
            {
                keyDescription = "R",
                actionName = "Restart Block",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.R),
                hotKeyAction = () =>
                {
                    Debug.Log("---PRESSED RESTART BLOCK HOTKEY---");
                    if(HkPanel.TrialLevel != null)
                    {
                        if (HkPanel.TrialLevel.AudioFBController.IsPlaying())
                            HkPanel.TrialLevel.AudioFBController.audioSource.Stop();

                        HkPanel.TrialLevel.AbortCode = 2;
                        HkPanel.TrialLevel.ForceBlockEnd = true;
                        HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("FinishTrial"));
                        HkPanel.TaskLevel.BlockCount--;
                    }
                }
            };
            HotKeyList.Add(restartBlock);

            //PreviousBlock Hot Key
            HotKey previousBlock = new HotKey
            {
                keyDescription = "B",
                actionName = "Previous Block",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.B),
                hotKeyAction = () =>
                {
                    Debug.Log("---PRESSED PREVIOUS BLOCK HOTKEY---");
                    if(HkPanel.TrialLevel != null)
                    {
                        if (HkPanel.TrialLevel.AudioFBController.IsPlaying())
                            HkPanel.TrialLevel.AudioFBController.audioSource.Stop();

                        HkPanel.TrialLevel.AbortCode = 4;
                        HkPanel.TrialLevel.ForceBlockEnd = true;
                        HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("FinishTrial"));
                    
                        if (HkPanel.TrialLevel.BlockCount == 0)
                        {
                            Debug.Log("Can't go to previous block, because this is the first block! Restarting Current Block instead.");
                            HkPanel.TaskLevel.BlockCount--;
                        }
                        else
                            HkPanel.TaskLevel.BlockCount -= 2;
                    }
                }
            };
            HotKeyList.Add(previousBlock);

            //End Block Hot Key
            HotKey endBlock = new HotKey
            {
                keyDescription = "N",
                actionName = "End Block",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.N),
                hotKeyAction = () =>
                {
                    Debug.Log("---PRESSED END BLOCK HOTKEY---");
                    if(HkPanel.TrialLevel != null)
                    {
                        if (HkPanel.TrialLevel.TokenFBController != null)
                            HkPanel.TrialLevel.TokenFBController.animationPhase = TokenFBController.AnimationPhase.None;

                        if (HkPanel.TrialLevel.AudioFBController.IsPlaying())
                            HkPanel.TrialLevel.AudioFBController.audioSource.Stop();
                        HkPanel.TrialLevel.AbortCode = 3;
                        HkPanel.TrialLevel.ForceBlockEnd = true;
                        HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("FinishTrial"));
                    }
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
                    Debug.Log("---PRESSED END TASK HOTKEY---");
                    if(HkPanel.TrialLevel != null)
                    {
                        HkPanel.TrialLevel.AbortCode = 5;
                        HkPanel.TrialLevel.ForceBlockEnd = true;
                        HkPanel.TrialLevel.FinishTrialCleanup();
                        HkPanel.TrialLevel.ClearActiveTrialHandlers();
                        HkPanel.TaskLevel.SpecifyCurrentState(HkPanel.TaskLevel.GetStateFromName("FinishTask"));
                    }
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
                    Debug.Log("---PRESSED QUIT GAME HOT KEY---");
                    if(HkPanel.TrialLevel != null)
                        HkPanel.TrialLevel.ForceBlockEnd = true;

                    if (HkPanel.TaskLevel != null)
                        HkPanel.TaskLevel.Terminated = true;

                    if(HkPanel.SessionLevel != null)
                    {
                        HkPanel.SessionLevel.TasksFinished = true;
                        HkPanel.SessionLevel.SpecifyCurrentState(HkPanel.SessionLevel.GetStateFromName("FinishSession"));
                    }
                    Application.Quit();
                }
            };
            HotKeyList.Add(quitGame);

            // End Trial Game Hot Key
            HotKey endTrial = new HotKey
            {
                keyDescription = "T",
                actionName = "End Trial",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.T),
                hotKeyAction = () =>
                {
                    Debug.Log("---PRESSED END TRIAL HOT KEY---");
                    if(HkPanel.TrialLevel != null)
                    {
                        GameObject controllers = GameObject.Find("InputManager");
                        if (!HkPanel.TrialLevel.Paused) 
                        {
                            HkPanel.TrialLevel.AbortCode = 1;
                            HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("FinishTrial")); //Go to end of trial
                            if (controllers != null) //Deactivate Controllers (so that tokenbar not still on screen):
                                controllers.SetActive(false);
                            HkPanel.TrialLevel.Paused = true;
                        }
                        else
                        {
                            if (controllers == null)
                                HkPanel.SessionLevel.FindInactiveGameObjectByName("Controllers").SetActive(true);
                            HkPanel.TrialLevel.Paused = false;
                        }
                    }
                }
            };
            HotKeyList.Add(endTrial);

            //Reward HotKey:
            HotKey reward = new HotKey
            {
                keyDescription = "G",
                actionName = "GiveReward",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.G),
                hotKeyAction = () =>
                {
                    Debug.Log("---PRESSED GIVE REWARD HOT KEY---");
                    if(HkPanel.TrialLevel != null)
                    {
                        if (SessionValues.SyncBoxController != null)
                        {
                            HkPanel.TrialLevel.AudioFBController.Play("Positive");
                            SessionValues.SyncBoxController.SendRewardPulses(SessionValues.SessionDef.RewardHotKeyNumPulses, SessionValues.SessionDef.RewardHotKeyPulseSize);
                        }
                        else
                            Debug.Log("Tried to send Reward but SyncBoxController is null!");
                    }
                }
            };
            HotKeyList.Add(reward);

            //Calibration HotKey:
            HotKey calibration = new HotKey
            {
                keyDescription = "Tab",
                actionName = "Calibration",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.Tab),
                hotKeyAction = () =>
                {
                    GazeCalibrationTaskLevel = HkPanel.SessionLevel.GazeCalibrationTaskLevel;
                    GazeCalibrationTrialLevel = (ControlLevel_Trial_Template)GazeCalibrationTaskLevel.GetStateFromName("RunBlock").ChildLevel;

                    Debug.Log("---PRESSED CALIBRATION HOT KEY---");

                    if (HkPanel.TrialLevel != null)
                    {
                        HkPanel.TrialLevel.AbortCode = 5;
                        HkPanel.TrialLevel.FinishTrialCleanup();
                        HkPanel.TrialLevel.ResetTrialVariables();
                        HkPanel.TrialLevel.ClearActiveTrialHandlers();
                    }

                    if (HkPanel.TaskLevel != null)
                    {
                        // The Hot Key is triggered during a task, either calibration or regular task
                        if (HkPanel.TaskLevel.TaskName.Contains("GazeCalibration"))
                        {
                            // The Hot Key is triggered during the Calibration Task
                            HkPanel.TrialLevel.ForceBlockEnd = true;
                            HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("FinishTrial"));

                            // Set calibration data controllers off
                            HkPanel.TaskLevel.FrameData.gameObject.SetActive(false);
                            HkPanel.TaskLevel.BlockData.gameObject.SetActive(false);
                            HkPanel.TaskLevel.TrialData.gameObject.SetActive(false);

                            // The Hot Key is triggered during the calibration task, exit the calibration task, and return to either task or session
                            if (OriginalTaskLevel != null)
                            {
                                // Restore the Original Task Level
                                OriginalTrialLevel.runCalibration = false;
                                SessionValues.ExperimenterDisplayController.ResetTask(OriginalTaskLevel, OriginalTrialLevel);
                                OriginalTaskLevel.FrameData.gameObject.SetActive(true);
                                OriginalTaskLevel.BlockData.gameObject.SetActive(true);
                                OriginalTaskLevel.TrialData.gameObject.SetActive(true);
                            }
                            else
                            {
                                // Return to the Session Level
                                HkPanel.SessionLevel.runSessionLevelCalibration = false;
                                SessionValues.ExperimenterDisplayController.ResetTask(null, null);
                            }
                        }

                        else
                        {
                            // The Hot Key is triggered during a regular task, prepare the calibration task and store the original task information
                            HkPanel.TrialLevel.runCalibration = true;
                            HkPanel.TrialLevel.SpecifyCurrentState(HkPanel.TrialLevel.GetStateFromName("FinishTrial"));

                            OriginalTaskLevel = HkPanel.TaskLevel;
                            OriginalTrialLevel = HkPanel.TrialLevel;

                            OriginalTaskLevel.FrameData.gameObject.SetActive(false);
                            OriginalTaskLevel.BlockData.gameObject.SetActive(false);
                            OriginalTaskLevel.TrialData.gameObject.SetActive(false);

                            GazeCalibrationTrialLevel.SpecifyCurrentState(GazeCalibrationTrialLevel.GetStateFromName("SetupTrial"));
                            SessionValues.ExperimenterDisplayController.ResetTask(GazeCalibrationTaskLevel, GazeCalibrationTrialLevel);
                            GazeCalibrationTaskLevel.FrameData.gameObject.SetActive(false);
                            GazeCalibrationTaskLevel.BlockData.gameObject.SetActive(false);
                            GazeCalibrationTaskLevel.TrialData.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        // The Hot Key is triggered at the Session Level, prepare the calibration task
                        HkPanel.SessionLevel.runSessionLevelCalibration = true;
                        GazeCalibrationTrialLevel.SpecifyCurrentState(GazeCalibrationTrialLevel.GetStateFromName("SetupTrial"));
                        SessionValues.ExperimenterDisplayController.ResetTask(GazeCalibrationTaskLevel, GazeCalibrationTrialLevel);
                        GazeCalibrationTaskLevel.FrameData.gameObject.SetActive(true);
                        GazeCalibrationTaskLevel.BlockData.gameObject.SetActive(true);
                        GazeCalibrationTaskLevel.TrialData.gameObject.SetActive(true);

                    }

                }
            };
            HotKeyList.Add(calibration);
            //InstructionsButton visibility HotKey:
            //HotKey instructionsButton = new HotKey
            //{
            //    keyDescription = "I",
            //    actionName = "InstructionsButton",
            //    hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.I),
            //    hotKeyAction = () =>
            //    {
            //        USE_Instructions.ToggleInstructions();
            //    }
            //};
            //HotKeyList.Add(instructionsButton);

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







