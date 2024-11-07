/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



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

        hotKeyText.GetComponent<Text>().text = HKList.GenerateHotKeyDescriptions();
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
            ControlLevel_Task_Template OriginalTaskLevel = null;
            ControlLevel_Trial_Template OriginalTrialLevel = null;

            // Toggle Displays HotKey
            //HotKey toggleDisplays = new HotKey
            //{
            //    keyDescription = "W",
            //    actionName = "Toggle Displays",
            //    hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.W),
            //    hotKeyAction = () =>
            //    {                    
            //        Debug.LogWarning("CLICKED TOGGLE DISPLAY HOTKEY!");

            //        if (Session.WebBuild)
            //            return;
                                        
            //        var allCameras = GameObject.FindObjectsOfType<Camera>();
            //        foreach (Camera c in allCameras)
            //        {
            //            Debug.Log($"--- CAMERA {c.name} BEFORE: {c.targetDisplay} ---");
            //            c.targetDisplay = 1 - c.targetDisplay;
            //            Debug.Log($"--- CAMERA {c.name} AFTER: {c.targetDisplay} ---");
            //        }

            //        //Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
            //        var allCanvases = GameObject.FindObjectsOfType<Canvas>();
            //        foreach (Canvas c in allCanvases) //ExperimenterCanvas: 1, TaskSelectionCanvas:0 (DC), InitScreenCanvas:1, CR_Canvas:0 (DC)
            //        {
            //            if(c.renderMode == RenderMode.ScreenSpaceOverlay)
            //            {
            //                Debug.Log($"--- CANVAS {c.name} BEFORE: {c.targetDisplay} ---");
            //                c.targetDisplay = 1 - c.targetDisplay;
            //                Debug.Log($"--- CANVAS {c.name} AFTER: {c.targetDisplay} ---");
            //            }
                        
                     
            //        }

            //        // Change display of the loading canvas which could be inactive
            //        Session.LoadingController_Session.gameObject.GetComponent<Canvas>().targetDisplay = 1 - Session.LoadingController_Session.gameObject.GetComponent<Canvas>().targetDisplay;
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


            // Pause Game Hot Key
            HotKey pauseGame = new HotKey
            {
                keyDescription = "P",
                actionName = "Pause Game",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.P),
                hotKeyAction = () =>
                {
                    Debug.Log("---PRESSED PAUSE GAME HOT KEY---");
                    if(Session.TrialLevel != null)
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
                    if(Session.TrialLevel != null)
                    {
                        if (Session.TrialLevel.AudioFBController.IsPlaying())
                            Session.TrialLevel.AudioFBController.audioSource.Stop();

                        Session.TrialLevel.AbortCode = 2;
                        Session.EventCodeManager.SendRangeCode("CustomAbortTrial", Session.TrialLevel.AbortCodeDict["RestartBlock"]);
                        Session.TrialLevel.ForceBlockEnd = true;
                        Session.TrialLevel.SpecifyCurrentState(Session.TrialLevel.GetStateFromName("FinishTrial"));
                        Session.TaskLevel.BlockCount--;
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
                    if(Session.TrialLevel != null)
                    {
                        if (Session.TrialLevel.AudioFBController.IsPlaying())
                            Session.TrialLevel.AudioFBController.audioSource.Stop();

                        Session.TrialLevel.AbortCode = 4;
                        Session.EventCodeManager.SendRangeCode("CustomAbortTrial", Session.TrialLevel.AbortCodeDict["PreviousBlock"]);
                        Session.TrialLevel.ForceBlockEnd = true;
                        Session.TrialLevel.SpecifyCurrentState(Session.TrialLevel.GetStateFromName("FinishTrial"));
                    
                        if (Session.TrialLevel.BlockCount == 0)
                        {
                            Debug.Log("Can't go to previous block, because this is the first block! Restarting Current Block instead.");
                            Session.TaskLevel.BlockCount--;
                        }
                        else
                            Session.TaskLevel.BlockCount -= 2;
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
                    if(Session.TrialLevel != null)
                    {
                        if (Session.TrialLevel.TokenFBController != null)
                        {
                            Session.TrialLevel.TokenFBController.animationPhase = TokenFBController.AnimationPhase.None;
                            Session.TrialLevel.TokenFBController.enabled = false;
                        }

                        if (Session.TrialLevel.AudioFBController.IsPlaying())
                            Session.TrialLevel.AudioFBController.audioSource.Stop();

                        if (Session.HumanStartPanel.HumanStartPanelGO != null)
                        {
                            Session.HumanStartPanel.HumanStartPanelGO.SetActive(false);
                        }

                        Session.TrialLevel.AbortCode = 3;
                        Session.EventCodeManager.SendRangeCode("CustomAbortTrial", Session.TrialLevel.AbortCodeDict["EndBlock"]);
                        Session.TrialLevel.ForceBlockEnd = true;
                        Session.TrialLevel.SpecifyCurrentState(Session.TrialLevel.GetStateFromName("FinishTrial"));
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
                    if(Session.TrialLevel != null)
                    {
                        //deactivate human start panel
                        if (Session.HumanStartPanel.HumanStartPanelGO != null)
                            Session.HumanStartPanel.HumanStartPanelGO.SetActive(false);

                        Session.TrialLevel.AbortCode = 5;
                        Session.EventCodeManager.SendRangeCode("CustomAbortTrial", Session.TrialLevel.AbortCodeDict["EndTask"]);
                        Session.TrialLevel.ForceBlockEnd = true;
                        Session.TrialLevel.FinishTrialCleanup();
                        Session.TrialLevel.ClearActiveTrialHandlers();
                        Session.TrialLevel.WriteDataFiles();
                        Session.TaskLevel.ForceTaskEnd = true;
                        Session.TaskLevel.SpecifyCurrentState(Session.TaskLevel.GetStateFromName("FinishTask"));
                    }
                }
            };
            HotKeyList.Add(endTask);

            // Quit Game Hot Key
            //ALREADY HANDLED BY ApplicationQuit class!
            //HotKey quitGame = new HotKey
            //{
            //    keyDescription = "Esc",
            //    actionName = "Quit",
            //    hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.Escape),
            //    hotKeyAction = () =>
            //    {
            //        Debug.Log("---PRESSED QUIT GAME HOT KEY---");
            //        if(Session.TrialLevel != null)
            //            Session.TrialLevel.ForceBlockEnd = true;

            //        if (Session.TaskLevel != null)
            //            Session.TaskLevel.Terminated = true;

            //        if(Session.SessionLevel != null)
            //        {
            //            Session.SessionLevel.TasksFinished = true;
            //            Session.SessionLevel.SpecifyCurrentState(Session.SessionLevel.GetStateFromName("FinishSession"));
            //        }
            //        Debug.LogWarning("HOT KEY QUIT");
            //        Application.Quit();
            //    }
            //};
            //HotKeyList.Add(quitGame);

            //Reward HotKey:
            HotKey reward = new HotKey
            {
                keyDescription = "G",
                actionName = "GiveReward",
                hotKeyCondition = () => InputBroker.GetKeyUp(KeyCode.G),
                hotKeyAction = () =>
                {
                    Debug.Log("---PRESSED GIVE REWARD HOT KEY---");
                    if(Session.TrialLevel != null)
                    {
                        if (Session.SyncBoxController != null)
                        {
                            Session.TrialLevel.AudioFBController.Play("Positive");
                            Session.SyncBoxController.SendRewardPulses(Session.SessionDef.RewardHotKeyNumPulses, Session.SessionDef.RewardHotKeyPulseSize);
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
                    Debug.Log("---PRESSED CALIBRATION HOT KEY---");

                    if (!Session.SessionDef.EyeTrackerActive)
                    {
                        Debug.LogWarning("EYETRACKER IS NOT ACTIVE! CANNOT TOGGLE CALIBRATION.");
                        return;
                    }

                    if (Session.TrialLevel != null)
                    {
                        Session.TrialLevel.AbortCode = 5;
                        Session.EventCodeManager.SendRangeCode("CustomAbortTrial", Session.TrialLevel.AbortCodeDict["EndTask"]);
                        Session.TrialLevel.FinishTrialCleanup();
                        Session.TrialLevel.ResetTrialVariables();
                        Session.TrialLevel.ClearActiveTrialHandlers();
                    }

                    if (Session.TaskLevel != null)
                    {
                        // The Hot Key is triggered during a task, either calibration or regular task
                        if (Session.TaskLevel.TaskName.Contains("GazeCalibration"))
                        {
                            // The Hot Key is triggered during the Calibration Task
                            Session.TrialLevel.ForceBlockEnd = true;
                            Session.TrialLevel.SpecifyCurrentState(Session.TrialLevel.GetStateFromName("FinishTrial"));

                            
                            // The Hot Key is triggered during the calibration task, exit the calibration task, and return to either task or session
                            if (OriginalTaskLevel != null)
                            {
                                // Restore the Original Task Level
                                OriginalTaskLevel.ActivateAllSceneElements(OriginalTaskLevel);
                                OriginalTaskLevel.ActivateTaskDataControllers();

                                Session.TaskLevel = OriginalTaskLevel;
                                Session.TrialLevel = OriginalTaskLevel.TrialLevel;
                            }
                            else
                            {
                                Session.SessionLevel.SessionCam.gameObject.SetActive(true);
                             //   Session.TaskLevel = null;
                               // Session.TrialLevel = null;
                            }
                            // Set calibration data controllers off
                            Session.GazeCalibrationController.DectivateGazeCalibrationComponents();
                            Session.GazeCalibrationController.GazeCalibrationTaskLevel.DeactivateTaskDataControllers();

                            Session.GazeCalibrationController.RunCalibration = false;
                        }

                        else
                        {
                            // The Hot Key is triggered during a regular task, prepare the calibration task and store the original task information

                            Session.GazeCalibrationController.RunCalibration = true;
                            Session.TrialLevel.SpecifyCurrentState(Session.TrialLevel.GetStateFromName("FinishTrial"));

                            OriginalTaskLevel = Session.TaskLevel;
                            OriginalTrialLevel = Session.TrialLevel;

                            Session.TaskLevel = Session.GazeCalibrationController.GazeCalibrationTaskLevel;
                            Session.TrialLevel = Session.GazeCalibrationController.GazeCalibrationTrialLevel;
                            Session.GazeCalibrationController.GazeCalibrationTrialLevel.SpecifyCurrentState(Session.GazeCalibrationController.GazeCalibrationTrialLevel.GetStateFromName("LoadTrialTextures"));
                            Session.GazeCalibrationController.GazeCalibrationTaskLevel.ActivateTaskDataControllers();
                            Session.GazeCalibrationController.ActivateGazeCalibrationComponents();

                            OriginalTaskLevel.DeactivateTaskDataControllers();
                            OriginalTaskLevel.DeactivateAllSceneElements(OriginalTaskLevel);

                        }
                    }
                    else
                    {
                        Debug.LogWarning("RUNNING SESSION LEVEL CALIBRATION !!");

                        // The Hot Key is triggered at the Session Level, prepare the calibration task
                        Session.GazeCalibrationController.RunCalibration = true;
                        Session.GazeCalibrationController.GazeCalibrationTrialLevel.SpecifyCurrentState(Session.GazeCalibrationController.GazeCalibrationTrialLevel.GetStateFromName("LoadTrialTextures"));
                        Session.GazeCalibrationController.GazeCalibrationTaskLevel.ActivateTaskDataControllers();
                        Session.GazeCalibrationController.ActivateGazeCalibrationComponents();
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







