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



using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_Settings;
using USE_StimulusManagement;
using ConfigDynamicUI;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Trial;
using System.Collections;
using USE_Def_Namespace;
using System.Collections.Specialized;
using TMPro;
using UnityEngine.SceneManagement;

namespace USE_ExperimentTemplate_Task
{
    public abstract class ControlLevel_Task_Template : ControlLevel
    {
        public string ConfigFolderName;
        public string TaskName;
        public string TaskProjectFolder;

        [HideInInspector] public int BlockCount;
        
        // protected int NumBlocksInTask;
        [HideInInspector] public int NumAbortedTrials_InTask;
        [HideInInspector] public int NumAbortedTrials_InBlock;

        [HideInInspector] public int NumRewardPulses_InBlock;
        [HideInInspector] public int NumRewardPulses_InTask;

        [HideInInspector] public int StimulationPulsesGiven_Task = 0;

        [HideInInspector] public int? TotalTouches_InBlock;
        [HideInInspector] public int? TotalIncompleteTouches_InBlock;

        [HideInInspector] public int MinTrials_InBlock;
        [HideInInspector] public int MaxTrials_InBlock;


        [HideInInspector] public bool ForceTaskEnd;
        public ControlLevel_Trial_Template TrialLevel;
        [HideInInspector] public BlockData BlockData;
        [HideInInspector] public FrameData FrameData;
        [HideInInspector] public TrialData TrialData;

        [HideInInspector] public string TaskConfigPath, TaskDataPath;

        [HideInInspector]
        public string TaskResourcesPath
        {
            get
            {
                if (TaskConfigPath == null)
                    Debug.LogError("TASK CONFIG PATH IS NULL WHEN TRYING TO GET TASK RESOURCES PATH");
                return TaskConfigPath + "/TaskResources";
            }
        }

        [HideInInspector]
        public StringBuilder CurrentBlockSummaryString, CurrentTaskSummaryString, PreviousBlockSummaryString;

        public GameObject TaskDirectionalLight;

        private int TaskStringsAdded = 0;
        public Camera TaskCam;
        public Canvas[] TaskCanvasses;
        public GameObject StimCanvas_2D;
        public TrialDef[] AllTrialDefs;
        protected TrialDef[] CurrentBlockTrialDefs;
        public TaskDef TaskDef;
        public BlockDef[] BlockDefs;
        private BlockDef CurrentBlockDef;

        public BlockDef currentBlockDef
        {
            get { return CurrentBlockDef; }
        }

        public TaskStims TaskStims;
        [HideInInspector] public StimGroup PreloadedStims, PrefabStims, ExternalStims, RuntimeStims;
        public List<GameObject> PreloadedStimGameObjects;
        public List<string> PrefabStimPaths;
        public ConfigUI configUI;
        public ConfigVarStore ConfigUiVariables;
        public Dictionary<string, EventCode> CustomTaskEventCodes;

        public Type TaskLevelType;
        public Type TrialLevelType, TaskDefType, BlockDefType, TrialDefType, StimDefType;
        protected State VerifyTask, SetupTask, RunBlock, BlockFeedback, FinishTask;
        protected bool TaskFbFinished;
        public TaskLevelTemplate_Methods TaskLevel_Methods;
        public List<GameObject> ActiveSceneElements;

        // protected int? MinTrials, MaxTrials;
        [HideInInspector] public RenderTexture DrawRenderTexture;
        [HideInInspector] public event EventHandler TaskSkyboxSet_Event;
        [HideInInspector] public bool TaskLevelDefined;

        [HideInInspector] public List<CustomSettings> customSettings;

        [HideInInspector] public bool TrialAndBlockDefsHandled;
        [HideInInspector] public bool StimsHandled;

        [HideInInspector] public AudioClip BlockResults_AudioClip; //Passed by SessionLevel
        [HideInInspector] public GameObject TaskResultsGO;

        private bool TaskResults_ContinueButtonClicked;

        //Passed by session level
        [HideInInspector] public ImportSettings_Level importSettings_Level;
        [HideInInspector] public VerifyTask_Level verifyTask_Level;


        private GameObject TaskLoadingControllerGO;


        [HideInInspector] public int BlockStimulationCode = 0;


        public virtual void SpecifyTypes()
        {
            TaskLevelType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TaskLevelType;
            TrialLevelType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TrialLevelType;
            TaskDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TaskDefType;
            BlockDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].BlockDefType;
            TrialDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TrialDefType;
            StimDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].StimDefType;
        }

        public T GetTaskDef<T>() where T : TaskDef
        {
            return (T)TaskDef;
        }

        public void DefineTaskLevel()
        {
            Session.TaskLevel = this;
            TaskLevelDefined = false;

            TaskLevel_Methods = new TaskLevelTemplate_Methods();

            RunBlock = new State("RunBlock");
            BlockFeedback = new State("BlockFeedback");
            FinishTask = new State("FinishTask");
            RunBlock.AddChildLevel(TrialLevel);
            AddActiveStates(new List<State> { RunBlock, BlockFeedback, FinishTask });

            TrialLevel.TrialDefType = TrialDefType;
            TrialLevel.StimDefType = StimDefType;
            TrialLevel.TaskLevel = this;
            TaskLevel_Methods.TrialLevel = TrialLevel;

            Add_ControlLevel_InitializationMethod(() =>
            {
                if(TaskLoadingControllerGO == null)
                {
                    TaskLoadingControllerGO = Instantiate(Resources.Load<GameObject>("LoadingCanvas_New"));
                    TaskLoadingControllerGO.name = "LoadingCanvas_Task";
                    Canvas loadingCanvas = TaskLoadingControllerGO.GetComponent<Canvas>();
                    if (loadingCanvas != null)
                    {
                        loadingCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                        loadingCanvas.worldCamera = TaskCam;
                    }
                    else
                        Debug.LogError("CANVAS IS NULL");

                }

                TaskLoadingControllerGO.SetActive(true);


                if (TaskDirectionalLight != null)
                {
                    TaskDirectionalLight.GetComponent<Light>().intensity = TaskDef.TaskDirectionalLightIntensity; //Set light for the task. 
                }

                if (TaskCanvasses != null)
                    foreach (Canvas canvas in TaskCanvasses)
                        canvas.gameObject.SetActive(true);

                BlockCount = -1;
                CurrentBlockSummaryString = new StringBuilder();
                PreviousBlockSummaryString = new StringBuilder();
                CurrentTaskSummaryString = new StringBuilder();

                NumRewardPulses_InTask = 0;
                NumAbortedTrials_InTask = 0;

                if (!Session.WebBuild && TaskName != "GazeCalibration")
                {
                    if (configUI == null)
                        configUI = FindObjectOfType<ConfigUI>();
                    configUI.clear();
                    if (ConfigUiVariables != null)
                        configUI.store = ConfigUiVariables;
                    else
                        configUI.store = new ConfigVarStore();
                    configUI.GenerateUI();
                }

                Session.InputManager.SetActive(true);


                GameObject taskCanvasGO = GameObject.Find(TaskName + "_Canvas");
                if(taskCanvasGO != null)
                {
                    Canvas canvas = taskCanvasGO.GetComponent<Canvas>();

                    if(canvas != null)
                    {
                        TrialLevel.DialogueController.Canvas = canvas;
                        TrialLevel.MaskFBController.Canvas = canvas;

                        if(Session.SessionDef.IsHuman)
                        {
                            Session.HumanStartPanel.CreateHumanStartPanel(FrameData, canvas, TaskName);
                        }

                    }
                    else
                        Debug.LogWarning("NO CANVAS COMPONENT WAS FOUND ON GAMEOBJECT " + TaskName + "_Canvas");


                }
                else
                    Debug.LogWarning("UNABLE TO FIND A GAMEOBJECT NAMED: " + TaskName + "_Canvas");


                if (Session.SessionDef.FlashPanelsActive)
                    GameObject.Find("UI_Canvas").GetComponent<Canvas>().worldCamera = TaskCam;

            });

            //RunBlock State-----------------------------------------------------------------------------------------------------
            RunBlock.AddUniversalInitializationMethod(() =>
            {
                TaskCam.gameObject.SetActive(true);

                StartCoroutine(TurnOffLoadingCanvas());

                //For web build have to start each task with DirectionalLight off since only 1 display so all tasks verified during task selection scene and causing lighting issues.
                if (TaskDirectionalLight != null)
                {
                    TaskDirectionalLight.SetActive(true);  
                }

                BlockCount++;

                NumAbortedTrials_InBlock = 0;
                NumRewardPulses_InBlock = 0;
                TotalTouches_InBlock = 0;
                TotalIncompleteTouches_InBlock = 0;
                Session.MouseTracker.ResetClicks();

                CurrentBlockDef = BlockDefs[BlockCount];

                TrialLevel.BlockCount = BlockCount;
                if (BlockCount == 0)
                    TrialLevel.TrialCount_InTask = -1;
                TrialLevel.TrialDefs = CurrentBlockDef.TrialDefs;

                TrialLevel.TaskStims = TaskStims;
                TrialLevel.PreloadedStims = PreloadedStims;
                TrialLevel.PrefabStims = PrefabStims;
                TrialLevel.ExternalStims = ExternalStims;
                TrialLevel.RuntimeStims = RuntimeStims;
                TrialLevel.ConfigUiVariables = ConfigUiVariables;

                TrialLevel.ForceBlockEnd = false;
                TrialLevel.ReachedCriterion = false;

                Session.EventCodeManager.SendRangeCodeThisFrame("RunBlockStarts", BlockCount);
            });

            //Hotkeys for WebGL build so we can end task and go to next block
            if (Session.WebBuild)
            {
                RunBlock.AddUpdateMethod(() =>
                {
                    if (TrialLevel != null)
                        HandleWebBuildHotKeys();
                });
            }

            RunBlock.AddLateUpdateMethod(() =>
            {
                // Check the case that the FrameData is deactivated when InTask_GazeCalibration is running
                if (FrameData.gameObject.activeSelf)
                    StartCoroutine(FrameData.AppendDataToBuffer());
            });
            RunBlock.SpecifyTermination(() => TrialLevel.Terminated, BlockFeedback);
            
            //BlockFeedback State-----------------------------------------------------------------------------------------------------
            BlockFeedback.AddUniversalInitializationMethod(() =>
            {
                Session.EventCodeManager.SendCodeThisFrame("BlockFeedbackStarts");
            });
            BlockFeedback.AddLateUpdateMethod(() =>
            {
               StartCoroutine(FrameData.AppendDataToBuffer());
            });
            BlockFeedback.SpecifyTermination(() => true && BlockCount < BlockDefs.Length - 1, RunBlock);
            BlockFeedback.SpecifyTermination(() => true && BlockCount == BlockDefs.Length - 1, FinishTask);
            BlockFeedback.AddDefaultTerminationMethod(() =>
            {
                SetTaskSummaryString();

                StartCoroutine(BlockData.AppendDataToBuffer());
                StartCoroutine(BlockData.AppendDataToFile());

                StartCoroutine(FrameData.AppendDataToBuffer());
                StartCoroutine(FrameData.AppendDataToFile());
            });

            //FinishTask State-----------------------------------------------------------------------------------------------------
            float taskFeedbackDuration = 0f;
            FinishTask.AddDefaultInitializationMethod(() =>
            {
                taskFeedbackDuration = Session.SessionDef.TaskResultsDuration;

                OrderedDictionary taskResults = GetTaskResultsData();
                if (taskFeedbackDuration > 0 && taskResults != null && taskResults.Count > 0)
                    DisplayTaskResults(taskResults);
                else
                    taskFeedbackDuration = 0f;

                if (TrialLevel.TouchFBController != null && TrialLevel.TouchFBController.TouchFbEnabled)
                    TrialLevel.TouchFBController.DisableTouchFeedback();

                if (TrialLevel.TouchFBController != null && TrialLevel.TokenFBController.enabled)
                    TrialLevel.TokenFBController.enabled = false;

                if (CheckForcedTaskEnd() && Session.StoreData) //If they used end task hotkey, still write the block data!
                {
                    StartCoroutine(BlockData.AppendDataToBuffer());
                    StartCoroutine(BlockData.AppendDataToFile());
                }

                if (Session.SessionDef.EventCodesActive)
                    Session.EventCodeManager.SendCodeThisFrame("FinishTaskStarts");

                //Clear trialsummarystring and Blocksummarystring at end of task:
                if (TrialLevel.TrialSummaryString != null && CurrentBlockSummaryString != null)
                {
                    TrialLevel.TrialSummaryString = "";
                    CurrentBlockSummaryString.Clear();
                    CurrentBlockSummaryString.AppendLine("");
                }

                ClearActiveTaskHandlers();

                if (Session.SessionDef.EyeTrackerActive && TaskName != "GazeCalibration")
                {
                    Session.GazeCalibrationController.ResetCreatedTaskSerialAndGazeDataFiles();
                    Session.GazeCalibrationController.OriginalTaskLevel = null;
                    Session.GazeCalibrationController.OriginalTrialLevel = null;
                }

            });
            FinishTask.AddUpdateMethod(() =>
            {
                if (TaskResults_ContinueButtonClicked || (Time.time - FinishTask.TimingInfo.StartTimeAbsolute >= taskFeedbackDuration))
                    TaskFbFinished = true;
                else
                    TaskFbFinished = false;
            });
            FinishTask.SpecifyTermination(() => TaskFbFinished, () => null);
            FinishTask.AddDefaultTerminationMethod(() =>
            {
                TaskResults_ContinueButtonClicked = false;

                if (TaskResultsGO != null)
                    TaskResultsGO.SetActive(false);

               
            });

            AddDefaultControlLevelTerminationMethod(() =>
            {
                if (Session.SessionDataControllers != null && TaskName != "GazeCalibration")
                {
                    Session.SessionDataControllers.RemoveDataController("BlockData_" + TaskName);
                    Session.SessionDataControllers.RemoveDataController("TrialData_" + TaskName);
                    Session.SessionDataControllers.RemoveDataController("FrameData_" + TaskName);

                    if (GameObject.Find("InputManager")?.transform.Find("FeedbackControllers(Clone)") != null)
                        Destroy(GameObject.Find("InputManager").transform.Find("FeedbackControllers(Clone)").gameObject);
                }

                if (TaskStims != null)
                {
                    int sgNum = TaskStims.AllTaskStimGroups.Count;
                    for (int iSg = 0; iSg < sgNum; iSg++)
                    {
                        StimGroup[] taskSgs = new StimGroup[TaskStims.AllTaskStimGroups.Count];
                        TaskStims.AllTaskStimGroups.Values.CopyTo(taskSgs, 0);
                        StimGroup sg = taskSgs[0];

                        if (sg.stimDefs != null)
                        {
                            while (sg.stimDefs.Count > 0)
                            {
                                sg.stimDefs[0].DestroyStimGameObject();
                                sg.stimDefs.RemoveAt(0);
                            }

                            sg.DestroyStimGroup();
                        }

                    }

                    TaskStims.AllTaskStims.DestroyStimGroup();

                }

                TaskCam.gameObject.SetActive(false);

                NumAbortedTrials_InBlock = 0;
                NumRewardPulses_InBlock = 0;

                if (TaskCanvasses != null)
                    foreach (Canvas canvas in TaskCanvasses)
                        canvas.gameObject.SetActive(false);



                if (!Session.WebBuild)
                {
                    foreach (Transform child in GameObject.Find("MainCameraCopy").transform)
                        Destroy(child.gameObject);
                }
            });

            TaskLevelDefined = true;
        }
        public bool CheckForcedTaskEnd()
        {
            if (ForceTaskEnd)
            {
                ForceTaskEnd = false;
                return true;
            }
            return false;
        }


        private IEnumerator TurnOffLoadingCanvas()
        {
            yield return new WaitForSeconds(0.5f);
            TaskLoadingControllerGO.SetActive(false);
        }


        public void SetSkyBox(string contextName)
        {
            string contextFilePath = "";
            if (Session.UsingDefaultConfigs)
                contextFilePath = $"{Session.SessionDef.ContextExternalFilePath}/{contextName}";
            else if (Session.UsingServerConfigs)
                contextFilePath = $"{ServerManager.ServerURL}/{Session.SessionDef.ContextExternalFilePath}/{contextName}.png";
            else if (Session.UsingLocalConfigs)
                contextFilePath = TrialLevel.GetContextNestedFilePath(Session.SessionDef.ContextExternalFilePath, contextName, "LinearDark");

            StartCoroutine(HandleSkybox(contextFilePath));
        }

        private void HandleWebBuildHotKeys()
        {
            if (InputBroker.GetKeyUp(KeyCode.P)) //Pause Game HotKey:
            {
                Time.timeScale = Time.timeScale == 1 ? 0 : 1;
            }

            if (InputBroker.GetKeyUp(KeyCode.E)) //End Task HotKey
            {
                Time.timeScale = 1; //if paused, unpause before ending task

                TrialLevel.AbortCode = 5;
                Session.EventCodeManager.SendRangeCodeThisFrame("CustomAbortTrial", TrialLevel.AbortCodeDict["EndTask"]);
                TrialLevel.ForceBlockEnd = true;
                TrialLevel.FinishTrialCleanup();
                TrialLevel.ClearActiveTrialHandlers();
                SpecifyCurrentState(FinishTask);
            }

            if (InputBroker.GetKeyUp(KeyCode.N)) //Next Block HotKey
            {

                Time.timeScale = 1; //if paused, unpause before ending block

                if (TrialLevel.TokenFBController != null)
                {
                    TrialLevel.TokenFBController.animationPhase = TokenFBController.AnimationPhase.None;
                    TrialLevel.TokenFBController.enabled = false;
                }

                if (Session.HumanStartPanel.HumanStartPanelGO != null)
                    Session.HumanStartPanel.HumanStartPanelGO.SetActive(false);

                if (TrialLevel.AudioFBController.IsPlaying())
                    TrialLevel.AudioFBController.audioSource.Stop();
                TrialLevel.AbortCode = 3;
                Session.EventCodeManager.SendRangeCodeThisFrame("CustomAbortTrial", TrialLevel.AbortCodeDict["EndBlock"]);
                TrialLevel.ForceBlockEnd = true;
                TrialLevel.SpecifyCurrentState(TrialLevel.GetStateFromName("FinishTrial"));
            }
        }

        public float CalculateAverageDuration(List<float?> durations)
        {
            float avgDuration;
            if (durations.Any(item => item.HasValue))
            {
                avgDuration = (float)durations
                    .Where(item => item.HasValue)
                    .Average(item => item.Value);
            }
            else
            {
                avgDuration = 0f;
            }

            return avgDuration;
        }
        
        public float CalculateStdDevDuration(List<float?> durations)
        {
            float stdDevDuration;

            // Filter out null values and convert to double for standard deviation calculation
            var nonNullDurations = durations.Where(item => item.HasValue).Select(item => (double)item.Value);

            if (nonNullDurations.Any())
            {
                double mean = nonNullDurations.Average();
                double sumOfSquares = nonNullDurations.Sum(value => Math.Pow(value - mean, 2));
                double variance = sumOfSquares / nonNullDurations.Count();
                stdDevDuration = (float)Math.Sqrt(variance);
            }
            else
            {
                stdDevDuration = 0f;
            }

            return stdDevDuration;
        }


        private void HandleTaskContinueButtonClicked()
        {
            TaskResults_ContinueButtonClicked = true;
        }

        private void DisplayTaskResults(OrderedDictionary taskResults)
        {
            if(taskResults == null)
            {
                Debug.LogWarning("NO TASK RESULTS");
                return;
            }
            
            GameObject taskCanvas = GameObject.Find(TaskName + "_Canvas");
            if (taskCanvas != null)
            {
                TaskResultsGO = Instantiate(Resources.Load<GameObject>("TaskResults"));
                TaskResultsGO.name = "TaskResults";
                TaskResultsGO.transform.SetParent(taskCanvas.transform);
                TaskResultsGO.transform.localScale = Vector3.one;
                TaskResultsGO.transform.localPosition = Vector3.zero;

                //Set rotation of TaskResults to same rotation as camera so its straight on:
                TaskResultsGO.transform.rotation = Camera.main.transform.rotation;

                TaskResultsGO.transform.Find("Background").transform.Find("HeaderPanel").GetComponentInChildren<TextMeshProUGUI>().text = "Task Results";

                GameObject continueButtonGO = TaskResultsGO.transform.Find("Background").transform.Find("ContinueButton").gameObject;
                if (continueButtonGO != null)
                    continueButtonGO.AddComponent<Button>().onClick.AddListener(HandleTaskContinueButtonClicked);

                Transform gridParent = TaskResultsGO.transform.Find("Background").transform.Find("GridSection");

                float height = 150f * taskResults.Count;
                if (height > 750f)
                    height = 750f;

                gridParent.GetComponent<RectTransform>().sizeDelta = new Vector2(1050f, height);

                AudioSource audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.clip = BlockResults_AudioClip;
                audioSource.volume = .9f;
                audioSource.Play();

                int count = 0;
                foreach (DictionaryEntry entry in taskResults)
                {
                    audioSource.Play();
                    GameObject gridItem = Instantiate(Resources.Load<GameObject>("TaskResults_GridItem"), gridParent);
                    gridItem.name = entry.Key.ToString();
                    TextMeshProUGUI itemText = gridItem.GetComponentInChildren<TextMeshProUGUI>();
                    itemText.text = $"{entry.Key}:  <color=#0681B5><b>{entry.Value}</b></color>";

                    count++;
                }
            }
            else
                Debug.Log("Didn't find a Task Canvas named: " + TaskName + "_Canvas");
        }


        public void ClearActiveTaskHandlers()
        {
            if (Session.SelectionTracker.TaskHandlerNames.Count > 0)
            {
                List<string> toRemove = new List<string>();

                foreach (string handlerName in Session.SelectionTracker.TaskHandlerNames)
                {
                    if (Session.SelectionTracker.ActiveSelectionHandlers.ContainsKey(handlerName))
                    {
                        Session.SelectionTracker.ActiveSelectionHandlers.Remove(handlerName);
                        toRemove.Add(handlerName);
                    }
                }

                foreach (string handlerName in toRemove)
                    Session.SelectionTracker.TaskHandlerNames.Remove(handlerName);
            }
        }


        public virtual OrderedDictionary GetTaskSummaryData()
        {
            return new OrderedDictionary
            {
                ["Total Trials"] = TrialLevel.TrialCount_InTask + 1,
                ["Aborted Trials"] = NumAbortedTrials_InTask,
                ["Reward Pulses"] = NumRewardPulses_InTask
            };
        }

        public virtual OrderedDictionary GetTaskResultsData()
        {
            return new OrderedDictionary
            {
                //["--Total Trials"] = TrialLevel.TrialCount_InTask + 1,
                //["--Aborted Trials"] = NumAbortedTrials_InTask,
                //["--Reward Pulses"] = NumRewardPulses_InTask
            };
        }

        public virtual List<CustomSettings> DefineCustomSettings()
        {
            return null;
        }


        //handling of block and trial defs so that each BlockDef contains a TrialDef[] array
        public void HandleTrialAndBlockDefs(bool verifyOnly)
        {
            if (AllTrialDefs == null || AllTrialDefs.Count() == 0) //no trialDefs have been imported from settings files
            {
                if (BlockDefs == null)
                    Debug.LogError("Neither BlockDef nor TrialDef config files provided in " + TaskName +
                                   " folder, no trials generated as a result.");
                else
                {
                    // if (!verifyOnly)
                    // {
                    for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                    {
                        BlockDefs[iBlock].RandomNumGenerator = new System.Random((int)DateTime.Now.Ticks + iBlock);
                        BlockDefs[iBlock].GenerateTrialDefsFromBlockDef();
                    }
                    // }
                }
            }
            else //trialDefs imported from settings files
            {
                if (BlockDefs == null) //no blockDef file, trialdefs should be complete
                {
                    Debug.Log("TrialDef config file provided without BlockDef config file in " + TaskName +
                              " folder, BlockDefs will be generated with default values for all fields from TrialDefs.");
                    if (AllTrialDefs[AllTrialDefs.Length - 1].BlockCount != 0)
                    {
                        if (AllTrialDefs[0].BlockCount == 0)
                            BlockDefs = new BlockDef[AllTrialDefs[AllTrialDefs.Length - 1].BlockCount];
                        else if (AllTrialDefs[0].BlockCount == 1)
                            BlockDefs = new BlockDef[AllTrialDefs[AllTrialDefs.Length - 1].BlockCount - 1];
                        else
                            Debug.LogError("TrialDef config file in " + TaskName +
                                           " folder includes BlockCounts that are neither 0- nor 1-indexed.");
                    }
                    else
                    {
                        Debug.Log("TrialDef config file in " + TaskName +
                                  " folder only generates a single block (this is not a problem if you do not intend to use a block structure in your experiment).");
                        BlockDefs = new BlockDef[1];
                    }

                    for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                    {
                        if (BlockDefs[iBlock] == null)
                            BlockDefs[iBlock] = new BlockDef();
                        BlockDefs[iBlock].BlockCount = iBlock;
                        BlockDefs[iBlock].TrialDefs = GetTrialDefsInBlock(iBlock, AllTrialDefs);
                    }
                }
                else //there is a blockDef file, its information may need to be added to TrialDefs
                {
                    for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                    {
                        BlockDefs[iBlock].TrialDefs = GetTrialDefsInBlock(iBlock + 1, AllTrialDefs);
                        BlockDefs[iBlock].RandomNumGenerator = new System.Random((int)DateTime.Now.Ticks + iBlock);
                        BlockDefs[iBlock].AddToTrialDefsFromBlockDef();
                    }
                }
            }

            TrialAndBlockDefsHandled = true;
        }

        public virtual Type GetTaskCustomSettingsType(string typeName)
        {
            return null;
        }

        public virtual void ProcessCustomSettingsFiles()
        {

        }

        public virtual Dictionary<string, object> SummarizeTask()
        {
            return new Dictionary<string, object>();
        }

        public void FindStims()
        {
            MethodInfo addTaskStimDefsToTaskStimGroup = GetType().GetMethod(nameof(this.AddTaskStimDefsToTaskStimGroup))
                .MakeGenericMethod(new Type[] { StimDefType });

            TaskStims.AllTaskStimGroups.Add("PreloadedStims", PreloadedStims);
            TaskStims.AllTaskStimGroups.Add("PrefabStims", PrefabStims);
            TaskStims.AllTaskStimGroups.Add("ExternalStims", ExternalStims);
            TaskStims.AllTaskStimGroups.Add("RuntimeStims", RuntimeStims);

            DefinePreloadedStims();

            if (PrefabStims.stimDefs != null && PrefabStims.stimDefs.Count > 0)
                DefinePrefabStims();

            if (ExternalStims.stimDefs != null && ExternalStims.stimDefs.Count > 0)
                DefineExternalStims();

            StimsHandled = true;
        }

        protected virtual void DefinePreloadedStims()
        {
            MethodInfo taskStimDefFromGameObject = GetType().GetMethod(nameof(TaskStimDefFromGameObject))
                .MakeGenericMethod((new Type[] { StimDefType }));
            if (PreloadedStimGameObjects != null && PreloadedStimGameObjects.Count > 0)
            {
                foreach (GameObject go in PreloadedStimGameObjects)
                    taskStimDefFromGameObject.Invoke(this, new object[] { go, PreloadedStims });

                PreloadedStims.AddStims(PreloadedStimGameObjects);
            }
        }

        protected virtual void DefinePrefabStims()
        {
            MethodInfo taskStimDefFromPrefabPath = GetType().GetMethod(nameof(TaskStimDefFromPrefabPath))
                .MakeGenericMethod((new Type[] { StimDefType }));

            GameObject taskCanvasGO = GameObject.Find(TaskName + "_Canvas");
            foreach (StimDef sd in PrefabStims.stimDefs)
            {
                sd.StimScale = TaskDef.ExternalStimScale;
                sd.CanvasGameObject = taskCanvasGO; //adding task canvas in case default stim end up being 2D
            }

            if (PrefabStimPaths != null && PrefabStimPaths.Count > 0)
            {
                foreach (string path in PrefabStimPaths)
                    taskStimDefFromPrefabPath.Invoke(this, new object[] { path, PrefabStims });
            }
        }

        protected virtual void DefineInternalStims()
        {

        }

        protected virtual void DefineExternalStims()
        {
            GameObject taskCanvasGO = GameObject.Find(TaskName + "_Canvas");
            if (taskCanvasGO == null)
                Debug.LogError("COULDNT FIND A CANVAS CALLED: " + TaskName + "_Canvas");

            foreach (StimDef sd in ExternalStims.stimDefs)
            {
                sd.StimFolderPath = TaskDef.ExternalStimFolderPath;
                sd.StimScale = TaskDef.ExternalStimScale;
                sd.CanvasGameObject = taskCanvasGO;
                sd.FileName = sd.FileName.Trim();
                sd.StimExtension = "." + sd.FileName.Split('.')[1];
            }
        }



        public void ReadCustomSingleTypeArray<T>(string filePath, string settingsName, string serverFileString = null)
            where T : CustomSettings
        {
            SessionSettings.ImportSettings_SingleTypeArray<T>(settingsName, filePath, serverFileString);
        }

        public void ReadCustomMultipleTypes<T>(string filePath, string settingsName, string serverFileString = null)
            where T : CustomSettings
        {
            SessionSettings.ImportSettings_MultipleType(settingsName, filePath, serverFileString);
        }

        public void ReadCustomSingleTypeJson<T>(string filePath, string settingsName, string serverFileString = null)
            where T : CustomSettings
        {
            SessionSettings.ImportSettings_SingleTypeJSON<T>(settingsName, filePath, serverFileString);
        }



        public void AddTaskStimDefsToTaskStimGroup<T>(StimGroup sg, IEnumerable<T> stimDefs) where T : StimDef
        {
            sg.AddStims(stimDefs);
        }

        public T TaskStimDefFromGameObject<T>(GameObject go, StimGroup sg = null) where T : StimDef, new()
        {
            StimDef sd = new T();
            sd.StimGameObject = go;
            sd.StimName = go.name;
            if (sg != null)
                sd.AddToStimGroup(sg);
            return (T)sd;
        }

        public T TaskStimDefFromPrefabPath<T>(string prefabPath, StimGroup sg = null) where T : StimDef, new()
        {
            StimDef sd = new T();
            sd.StimName = Path.GetFileName(prefabPath);
            if (sg != null)
                sd.AddToStimGroup(sg);
            return (T)sd;
        }

        public List<TrialDef> GetTrialDefsInBlock(int BlockNum, TrialDef[] trialDefs)
        {
            List<TrialDef> trialList = new List<TrialDef>();
            int currentBlockCount = -1;
            for (int iTrial = 0; (currentBlockCount <= BlockNum) & (iTrial < trialDefs.Length); iTrial++)
            {
                currentBlockCount = trialDefs[iTrial].BlockCount;
                if (currentBlockCount == BlockNum)
                    trialList.Add(trialDefs[iTrial]);
            }

            return trialList;
        }

        public T GetCurrentBlockDef<T>() where T : BlockDef
        {
            return (T)CurrentBlockDef;
        }

        public virtual void SetTaskSummaryString()
        {
            CurrentTaskSummaryString.Clear();

            decimal percentAbortedTrials = 0;
            if (TrialLevel.TrialCount_InTask > 0)
                percentAbortedTrials =
                    (Math.Round(decimal.Divide(NumAbortedTrials_InTask, (TrialLevel.TrialCount_InTask)), 2)) * 100;
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>" +
                                            $"\n<b># Trials:</b> {TrialLevel.TrialCount_InTask} ({percentAbortedTrials}% aborted)" +
                                            $"\t<b># Blocks:</b> {BlockCount}" +
                                            $"\t<b># Reward Pulses:</b> {NumRewardPulses_InTask}");

        }
        
        public int DetermineTrialDefDifficultyLevel(int difficultyLevel, List<int> runningPerformance, int posStep,
            int negStep, int maxDiffLevel)
        {
            if (runningPerformance.Count == 0)
                return difficultyLevel;

            if (runningPerformance.Last() == 1)
            {
                difficultyLevel -= negStep;
                if (difficultyLevel < 1)
                {
                    Debug.Log("DIFFICULTYLEVEL HIT 0");
                    difficultyLevel = 0;
                }
            }

            else if (runningPerformance.Last() == 0)
            {
                difficultyLevel += posStep;
                if (difficultyLevel >= maxDiffLevel)
                {
                    Debug.Log("DIFFICULTYLEVEL HIT MAX AT " + maxDiffLevel);
                    difficultyLevel = maxDiffLevel;
                }
            }

            return difficultyLevel;
        }

        public void ActivateTaskDataControllers()
        {
            BlockData.gameObject.SetActive(true);
            TrialData.gameObject.SetActive(true);
            FrameData.gameObject.SetActive(true);
        }
        public void DeactivateTaskDataControllers()
        {
            BlockData.gameObject.SetActive(false);
            TrialData.gameObject.SetActive(false);
            FrameData.gameObject.SetActive(false);
        }

        public void DeactivateAllSceneElements(ControlLevel_Task_Template taskLevel)
        {
            Scene targetScene = SceneManager.GetSceneByName(taskLevel.TaskName);
            if (targetScene.IsValid())
            {
                GameObject[] allObjects = targetScene.GetRootGameObjects();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name == $"{taskLevel.TaskName}_Scripts")
                    {
                        // Skip the task level script and task level camera
                        continue;
                    }

                    obj.SetActive(false);
                }
            }
        }

        public void ActivateAllSceneElements(ControlLevel_Task_Template taskLevel)
        {
            foreach (GameObject obj in ActiveSceneElements)
            {
                obj.SetActive(true);
            }
            ActiveSceneElements.Clear();
        }
    }
    
    

public class TaskLevelTemplate_Methods
    {
        public ControlLevel_Trial_Template TrialLevel;

        //CALCULATE ADPATIVE TRIAL DEF 
        public int DetermineTrialDefDifficultyLevel()
        {
            int difficultyLevel = 0;
            // DETERMINE DIFFICULTY BASED ON PERFORMANCE OF LAST TRIAL
            
            
            //PASS IN THE DLS, max & min, pos step, # of trials before pos dl switch, neg step, # of trials before neg dl switch
            return difficultyLevel;
        }
        public bool CheckBlockEnd(string blockEndType, IEnumerable<float?> runningTrialPerformance, float performanceThreshold = 1,
            int? minTrials = null, int? maxTrials = null)
        {
            // Takes in accuracy info from the current trial to determine whether to end the block
            List<float?> rTrialPerformance = (List<float?>)runningTrialPerformance;
            if (CheckTrialRange(rTrialPerformance.Count, minTrials, maxTrials) != null)
                return CheckTrialRange(rTrialPerformance.Count, minTrials, maxTrials).Value;

            // Add null to the running trial performance to indicate an aborted/incomplete trial

            switch (blockEndType)
            {
                case "CurrentTrialPercentError":
                    Debug.LogWarning("CHECKING BLOCK END - rTrialPerformance.Count: " + rTrialPerformance.Count + ", PERCENT ERROR " + (rTrialPerformance[rTrialPerformance.Count - 1]));
                    if (rTrialPerformance[rTrialPerformance.Count - 1] != null && rTrialPerformance[rTrialPerformance.Count-1] <= performanceThreshold)
                    {
                        TrialLevel.ReachedCriterion = true;
                        Debug.Log("Block ending due to trial performance below threshold.");
                        return true;
                    }
                    else
                        return false;
                case "CurrentTrialErrorCount":
                    Debug.Log("CHECKING BLOCK END - ERROR COUNT: " + rTrialPerformance[rTrialPerformance.Count - 1] + "THRESHOLD: " + performanceThreshold);
                    if (rTrialPerformance[rTrialPerformance.Count - 1] != null && rTrialPerformance[rTrialPerformance.Count-1] <= performanceThreshold)
                    {
                        TrialLevel.ReachedCriterion = true;
                        Debug.Log("Block ending due to trial performance below threshold.");
                        return true;
                    }
                    else
                        return false;
                default:
                    return false;
            }
        }
        public bool CheckBlockEnd(string blockEndType, IEnumerable<int> runningAcc, float accThreshold = 1, int windowSize = 1, int? minTrials = null, int? maxTrials = null)
        {
            //takes in accuracy information from the current block and determines if the block should end

            List<int> rAcc = (List<int>)runningAcc;
            float? immediateAvg; //this is the running average over the past n trials, where n = windowSize
            float? prevAvg; //this is the running average over the n trials PRIOR to the trials used for immediateAvg
                            //(allows easy comparison of changes between performance across two windows
            int? sumdif; //the simple sum of the number of different trial outcomes in the windows used to compute 
                         //immediateAvg and prevAvg
                         

            if (rAcc.Count >= windowSize)
            {
                immediateAvg = (float)rAcc.GetRange(rAcc.Count - windowSize, windowSize).Average();
                Debug.Log("###IMMEDIATE AVG: " + immediateAvg);
                Debug.Log("###rACC: " + String.Join(",",rAcc));
            }
            else
                immediateAvg = null;

            if (rAcc.Count >= windowSize * 2)
            {
                Debug.Log("###IS THIS BREAKING IT: rAcc.Count >= windowSize * 2?");
                prevAvg = (float)rAcc.GetRange(rAcc.Count - windowSize * 2, windowSize).Average();
                sumdif = rAcc.GetRange(rAcc.Count - windowSize * 2, windowSize).Sum() -
                         rAcc.GetRange(rAcc.Count - windowSize, windowSize).Sum();
                Debug.Log($"###prevAvg: {prevAvg}, sumdif: {sumdif}");

            }
            else
            {
                prevAvg = null;
                sumdif = null;
            }

            if (CheckTrialRange(rAcc.Count, minTrials, maxTrials) != null)
                return CheckTrialRange(rAcc.Count, minTrials, maxTrials).Value;

            switch (blockEndType)
            {
                case "SimpleThreshold":
                    Debug.Log("checkingthreshold #################################################");
                    Debug.Log("Immediate: " + immediateAvg + ", threshold: " + accThreshold);
                    if (immediateAvg >= accThreshold)
                    {
                        TrialLevel.ReachedCriterion = true;
                        Debug.Log("Block ending due to performance above threshold.");
                        return true;
                    }
                    else
                        return false;
                case "ThresholdAndPeak":
                    if (immediateAvg >= accThreshold && immediateAvg <= prevAvg)
                    {
                        TrialLevel.ReachedCriterion = true;
                        Debug.Log("Block ending due to performance above threshold and no continued improvement.");
                        return true;
                    }
                    else
                        return false;
                case "ThresholdOrAsymptote":
                    if (sumdif != null && sumdif.Value <= 1)
                    {
                        TrialLevel.ReachedCriterion = true;
                        Debug.Log("Block ending due to asymptotic performance.");
                        return true;
                    }
                    else if (immediateAvg >= accThreshold)
                    {
                        TrialLevel.ReachedCriterion = true;
                        Debug.Log("Block ending due to performance above threshold.");
                        return true;
                    }
                    else
                        return false;
                default:
                    return false;
            }
        }

        private bool? CheckTrialRange(int nTrials, int? minTrials = null, int? maxTrials = null)
        {
            if (nTrials < minTrials)
                return false;
            if (nTrials >= maxTrials)
                return true;
            return null;
        }
    }

    public class TaskStims
    {
        public StimGroup AllTaskStims;
        public Dictionary<string, StimGroup> AllTaskStimGroups;
        public string TaskStimFolderPath;
        public string TaskStimExtension;

        public TaskStims()
        {
            AllTaskStims = new StimGroup("AllTaskStims");
            AllTaskStimGroups = new Dictionary<string, StimGroup>();
        }

        public void CreateStimDef(StimGroup sg)
        {
            StimDef sd = new StimDef(sg);
            CheckPathAndDuplicate(sd);
        }

        public void CreateStimDef(StimGroup sg, GameObject obj)
        {
            StimDef sd = new StimDef(sg, obj);
            CheckPathAndDuplicate(sd);
        }

        public StimGroup CreateStimGroup(string groupName, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            StimGroup sg = new StimGroup(groupName, setActiveOnInit, setInactiveOnTerm);
            AllTaskStimGroups.Add(groupName, sg);
            return sg;
        }

        public StimGroup CreateStimGroup(string groupName, IEnumerable<StimDef> stims, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            StimGroup sg = new StimGroup(groupName, stims, setActiveOnInit, setInactiveOnTerm);
            AllTaskStimGroups.Add(groupName, sg);
            AddNewStims(sg.stimDefs);
            return sg;
        }

        public StimGroup CreateStimGroup(string groupName, string TaskName, string stimDefFilePath, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            StimGroup sg = new StimGroup(groupName, TaskName, stimDefFilePath, setActiveOnInit, setInactiveOnTerm);
            AllTaskStimGroups.Add(groupName, sg);
            AddNewStims(sg.stimDefs);
            return sg;
        }

        public StimGroup CreateStimGroup(string groupName, StimGroup sgOrig, IEnumerable<int> stimSubsetIndices, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            StimGroup sg = new StimGroup(groupName, sgOrig, stimSubsetIndices, setActiveOnInit, setInactiveOnTerm);
            if (!AllTaskStimGroups.ContainsKey(groupName))
                AllTaskStimGroups.Add(groupName, sg);
            else
            {
                Debug.LogWarning("");
                AllTaskStimGroups[groupName] = sg;
            }
            AddNewStims(sg.stimDefs);
            return sg;
        }

        private StimDef CheckPathAndDuplicate(StimDef sd)
        {
            if (!string.IsNullOrEmpty(TaskStimFolderPath) && string.IsNullOrEmpty(sd.StimFolderPath))
                sd.StimFolderPath = TaskStimFolderPath;
            //if (!string.IsNullOrEmpty(TaskStimExtension) && string.IsNullOrEmpty(sd.StimExtension))
            //    sd.StimExtension = TaskStimExtension;

            if (!AllTaskStims.stimDefs.Contains(sd))
                AllTaskStims.AddStims(sd);
            else
                Debug.LogWarning("Attempted to add duplicate StimDef " + sd.StimName + " to AllTaskStims, " +
                                 "duplication of object has been avoided.");

            return sd;
        }

        private void AddNewStims(List<StimDef> sds)
        {
            foreach (StimDef sd in sds)
            {
                if (!AllTaskStims.stimDefs.Contains(sd))
                {
                    CheckPathAndDuplicate(sd);
                }
            }
        }
    }

    public class CustomSettings
    {
        public string SearchString;
        public Type SettingsType;
        public string SettingsParsingStyle;
        public object ParsedResult;
        public Action<object> UpdateAction;
        
        public CustomSettings(string searchString, Type settingsType, string settingsParsingStyle, object parsedResult)
        {
            SearchString = searchString;
            SettingsType = settingsType;
            SettingsParsingStyle = settingsParsingStyle;
            ParsedResult = parsedResult;
        }

        public T AssignCustomSetting<T>()
        {
            return (T)ParsedResult;
        }
        
    }
}
