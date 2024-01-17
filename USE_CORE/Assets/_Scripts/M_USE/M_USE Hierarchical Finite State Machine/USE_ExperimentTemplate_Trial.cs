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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using USE_States;
using USE_StimulusManagement;
using ConfigDynamicUI;
using JetBrains.Annotations;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Task;
using USE_UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;
using UnityEngine.Serialization;
using USE_Def_Namespace;
using Random = UnityEngine.Random;


namespace USE_ExperimentTemplate_Trial
{
    public abstract class ControlLevel_Trial_Template : ControlLevel
    {
        [HideInInspector] public TrialData TrialData;
        [HideInInspector] public FrameData FrameData;
        

        [HideInInspector] public int BlockCount, TrialCount_InTask, TrialCount_InBlock, AbortCode;
        protected int NumTrialsInBlock;
        
        public List<int> runningPerformance;
        public int difficultyLevel;
        public int posStep;
        public int negStep;
        public string TrialDefSelectionStyle;
        public int maxDiffLevel;
        public int avgDiffLevel;
        public int diffLevelJitter;
        public int NumReversalsUntilTerm;
        public int MinTrialsBeforeTermProcedure;
        public int TerminationWindowSize;
        public int reversalsCount;
        public float blockAccuracy;
        public List<int> DiffLevelsAtReversals;
        public List<float> TimingValuesAtReversals;
        public List<int> DiffLevelsSummary;
        public int calculatedThreshold;
        public float calculatedThreshold_timing;

        [HideInInspector] public bool ForceBlockEnd;
        [HideInInspector] public string TaskDataPath, TrialSummaryString;
        protected State LoadTrialTextures, LoadTrialStims, SetupTrial, FinishTrial, Delay, GazeCalibration;
        
        protected State StateAfterDelay = null;
        protected float DelayDuration = 0;

        public ControlLevel_Task_Template TaskLevel;
        public List<TrialDef> TrialDefs;

        [HideInInspector] public TaskStims TaskStims;
        [HideInInspector] public StimGroup PreloadedStims, PrefabStims, ExternalStims, RuntimeStims;
        [HideInInspector] public List<StimGroup> TrialStims;

        [HideInInspector] public ConfigVarStore ConfigUiVariables;
        [HideInInspector] public SessionInfoPanel SessionInfoPanel;
        [HideInInspector] public float TrialCompleteTime;

        // Feedback Controllers
        [HideInInspector] public TouchFBController TouchFBController;
        [HideInInspector] public AudioFBController AudioFBController;
        [HideInInspector] public HaloFBController HaloFBController;
        [HideInInspector] public TokenFBController TokenFBController;
        [HideInInspector] public SliderFBController SliderFBController;
        

        [HideInInspector] public Dictionary<string, EventCode> TaskEventCodes;
        
        [HideInInspector] public Dictionary<string, int> AbortCodeDict;

        [HideInInspector] public UI_Debugger Debugger;
        [HideInInspector] public GameObject PauseIconGO;

        [HideInInspector] public bool TrialStimsLoaded;
        public Material SkyboxMaterial;
        // Texture Variables
        [HideInInspector] public Texture2D HeldTooLongTexture, HeldTooShortTexture, MovedTooFarTexture, MovedTooFarSquareTexture, HeldTooShortSquareTexture, HeldTooLongSquareTexture;


        public delegate IEnumerator FileLoadingMethod();
        public FileLoadingMethod FileLoadingDelegate; //Delegate that tasks can set to their own specific method
        public bool TrialFilesLoaded;

        public int CurrentTrialDefIndex;

        //if anyone uses this test it!
        public List<GameObject> AssignStimToList(StimGroup sg, List<GameObject> existingList = null)
        {
            if (existingList == null)
                existingList = new List<GameObject>();

            foreach (var stim in sg.stimDefs)
                existingList.Add(stim.StimGameObject);

            return existingList;
        }

        public virtual void DefineCustomTrialDefSelection()
        {
        }

        public T GetCurrentTrialDef<T>() where T : TrialDef
        {
            return (T)TrialDefs[CurrentTrialDefIndex];
        }
        
        public int DetermineCurrentTrialDefIndex()
        {
            switch (TrialDefSelectionStyle)
            {
                case "adaptive":
                    difficultyLevel = TaskLevel.DetermineTrialDefDifficultyLevel(difficultyLevel, runningPerformance, posStep, negStep, maxDiffLevel);
                    Debug.Log("cur difficulty level (after determine): " + difficultyLevel);
                    List<int> tieIndices = TrialDefs
                        .Select((trialDef, index) => new { TrialDef = trialDef, Index = index })
                        .Where(item => 
                        {
                            return (item.TrialDef.DifficultyLevel == difficultyLevel && item.TrialDef.BlockCount - 1 == BlockCount);
                        })
                        .Select(item => item.Index)
                        .ToList();
                    return tieIndices[Random.Range(0, tieIndices.Count)];

                default:
                    return TrialCount_InBlock;
            }
        }

        public T GetTaskLevel<T>() where T: ControlLevel_Task_Template
        {
            return (T)TaskLevel;
        }
        
        
        public T GetTaskDef<T>() where T: TaskDef
        {
            return (T)TaskLevel.TaskDef;
        }

        public Type TrialDefType, StimDefType;

        public void DefineTrialLevel()
        {
            Session.TrialLevel = this;

            LoadTrialTextures = new State("LoadTrialTextures");
            LoadTrialStims = new State("LoadTrialStims");
            SetupTrial = new State("SetupTrial");
            FinishTrial = new State("FinishTrial");
            Delay = new State("Delay");
            GazeCalibration = new State("GazeCalibration");
            
            if(Session.SessionDef.EyeTrackerActive)
                GazeCalibration.AddChildLevel(Session.GazeCalibrationController.GazeCalibrationTaskLevel);

            AddActiveStates(new List<State> { LoadTrialTextures, LoadTrialStims, SetupTrial, FinishTrial, Delay, GazeCalibration });
            // A state that just waits for some time;
            Delay.AddTimer(() => DelayDuration, () => StateAfterDelay);

            Cursor.visible = false;
            if (TokenFBController != null)
                TokenFBController.enabled = false;

            AddAbortCodeKeys();

            Debugger = new UI_Debugger();

            //DefineTrial();
            Add_ControlLevel_InitializationMethod(() =>
            {

                TrialCount_InBlock = -1;
                if (TrialCount_InBlock <= 0)
                {
                    DefineCustomTrialDefSelection();
                }
                TrialStims = new List<StimGroup>();
                AudioFBController?.UpdateAudioSource();

                if (Session.SessionDef.EyeTrackerActive)
                {
                    Session.GazeCalibrationController.ReassignGazeCalibrationDataFolderPath(Session.SessionDataPath + Path.DirectorySeparatorChar + "GazeCalibration" + Path.DirectorySeparatorChar + "TaskData");
                    StartCoroutine(Session.GazeCalibrationController.GazeCalibrationTaskLevel.BlockData.CreateFile());
                    StartCoroutine(Session.GazeCalibrationController.GazeCalibrationTrialLevel.TrialData.CreateFile());
                    StartCoroutine(Session.GazeCalibrationController.GazeCalibrationTaskLevel.FrameData.CreateFile());

                }
            });

            LoadTrialTextures.AddUniversalInitializationMethod(() =>
            {
                AbortCode = 0;
                TrialCount_InTask++;
                TrialCount_InBlock++;

                CurrentTrialDefIndex = DetermineCurrentTrialDefIndex();

                if (FileLoadingDelegate != null)
                    StartCoroutine(FileLoadingDelegate?.Invoke());
                else
                    TrialFilesLoaded = true;
            });
            LoadTrialTextures.SpecifyTermination(() => TrialFilesLoaded, LoadTrialStims);



            LoadTrialStims.AddUniversalInitializationMethod(() =>
            {
                if(!Session.WebBuild && TrialCount_InTask != 0)
                    Session.SessionInfoPanel.UpdateSessionSummaryValues(("totalTrials", 1));

                FrameData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, Session.FilePrefix);
                if (Session.SessionDef.EyeTrackerActive)
                {
                    Session.GazeData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, Session.FilePrefix);
                }

                if (Session.SessionDef.SerialPortActive)
                {
                    Session.SerialRecvData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, Session.FilePrefix);
                    Session.SerialSentData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, Session.FilePrefix);
                }

                Session.ClearStimLists();
                DefineTrialStims();
                StartCoroutine(HandleLoadingStims());
            });
            LoadTrialStims.SpecifyTermination(() => TrialStimsLoaded, SetupTrial, () => TrialStimsLoaded = false);

            SetupTrial.AddUniversalInitializationMethod(() =>
            {
                Session.LoadingController.DeactivateLoadingCanvas();

                if (Session.WebBuild)
                    Cursor.visible = true;

                //turning off instructions text at start of each trial, in case they left them on during last trial.
                TurnOffInstructionsText();

                TokenFBController?.RecalculateTokenBox(); //recalculate tokenbox incase they switch to fullscreen mode

                Session.EventCodeManager.SendRangeCode("SetupTrialStarts", TrialCount_InTask);

                ResetRelativeStartTime();

                ResetTrialVariables();

            });

            SetupTrial.AddDefaultTerminationMethod(() =>
            {
                Input.ResetInputAxes();
                if (Session.SessionDef.IsHuman)
                    Session.HumanStartPanel.AdjustPanelBasedOnTrialNum(TrialCount_InTask, TrialCount_InBlock);

                AddToStimLists(); //Seems to work here instead of each task having to call it themselves from InitTrial.

                //Disable the Task's MUSE Background that's set in Session Level's SetTasksMainBackground() method:
                StartCoroutine(DisableTaskMainBackground());

            });

            SetupTrial.AddUniversalTerminationMethod(() =>
            {
                Scene targetScene = SceneManager.GetSceneByName(TaskLevel.TaskName);
                if (targetScene.IsValid())
                {
                    GameObject[] allObjects = targetScene.GetRootGameObjects();
                    foreach (GameObject obj in allObjects)
                    {
                        if (obj.activeSelf)
                        {
                            // Store the object if it's currently active
                            TaskLevel.ActiveSceneElements.Add(obj);
                        }
                    }
                }

                
            });

            FinishTrial.AddSpecificInitializationMethod(() =>
            {
                Session.EventCodeManager.AddToFrameEventCodeBuffer("FinishTrialStarts");
            });
            
            if(Session.SessionDef.EyeTrackerActive)
                FinishTrial.SpecifyTermination(() => Session.GazeCalibrationController.RunCalibration && TaskLevel.TaskName != "GazeCalibration", () => GazeCalibration);
            FinishTrial.SpecifyTermination(() => CheckBlockEnd(), () => null);
            FinishTrial.SpecifyTermination(() => CheckForcedBlockEnd(), () => null);
            FinishTrial.SpecifyTermination(() => TrialCount_InBlock < TrialDefs.Count - 1, LoadTrialTextures);
            FinishTrial.SpecifyTermination(() => TrialCount_InBlock == TrialDefs.Count - 1, () => null);

            FinishTrial.AddUniversalLateTerminationMethod(() =>
            {
                TrialCompleteTime = FinishTrial.TimingInfo.StartTimeAbsolute + (Time.time - FinishTrial.TimingInfo.StartTimeAbsolute);

                int nStimGroups = TrialStims.Count;
                for (int iG = 0; iG < nStimGroups; iG++)
                {
                    TrialStims[0].DestroyStimGroup();
                    TrialStims.RemoveAt(0);
                }

                WriteDataFiles();
                
                FinishTrialCleanup();
                ClearActiveTrialHandlers();
                
                TouchFBController?.ClearErrorCounts();
                Resources.UnloadUnusedAssets();
                TrialSummaryString = "";
                

                Session.ClearStimLists();
            });

            string serialRecvDataFileName = "", serialSentDataFileName = "", gazeDataFileName = "";
            GazeCalibration.AddSpecificInitializationMethod(() =>
            {
                // Deactivate Task Scene Elements
                SkyboxMaterial = RenderSettings.skybox;
                if (TokenFBController)
                    TokenFBController.enabled = false;
                TaskLevel.DeactivateAllSceneElements(TaskLevel);

                // Organize Serial and Gaze data to write to the GazeCalibration folder, but store the original file name so they can be reassigned when exiting the GazeCalibration level
                if (Session.SessionDef.SerialPortActive)
                {
                    serialRecvDataFileName = Session.SerialRecvData.fileName;
                    serialSentDataFileName = Session.SerialSentData.fileName;
                    Session.SerialRecvData.folderPath = Session.SessionDataPath + Path.DirectorySeparatorChar + "GazeCalibration" + Path.DirectorySeparatorChar + "TaskData" + Path.DirectorySeparatorChar + "SerialRecvData";
                    Session.SerialSentData.folderPath = Session.SessionDataPath + Path.DirectorySeparatorChar + "GazeCalibration" + Path.DirectorySeparatorChar + "TaskData" + Path.DirectorySeparatorChar + "SerialSentData";
                }

                if (Session.SessionDef.EyeTrackerActive)
                {
                    StartCoroutine(Session.GazeData.AppendDataToFile());
                    gazeDataFileName = Session.GazeData.fileName;
                    Session.GazeData.folderPath = Session.SessionDataPath + Path.DirectorySeparatorChar + "GazeCalibration" + Path.DirectorySeparatorChar + "TaskData"  + Path.DirectorySeparatorChar + "GazeData";
                }


                // Activate Gaze Calibration components
                Session.GazeCalibrationController.ActivateGazeCalibrationComponents();

                // Assign experimenter display render texture to the GazeCalibration_TaskLevel.TaskCam
                Session.SessionLevel.AssignExperimenterDisplayRenderTexture(Session.GazeCalibrationController.GazeCalibrationTaskLevel.TaskCam);
            });

            GazeCalibration.SpecifyTermination(() => !Session.GazeCalibrationController.RunCalibration, () => SetupTrial, () =>
            {
                // Check and exit calibration mode for Tobii eye tracker
                if (Session.SessionDef.EyeTrackerActive && Session.TobiiEyeTrackerController.isCalibrating)
                {
                    Session.TobiiEyeTrackerController.isCalibrating = false;
                    Session.TobiiEyeTrackerController.ScreenBasedCalibration.LeaveCalibrationMode();
                }
               
                // Deactivate Gaze Calibration components
                Session.GazeCalibrationController.DectivateGazeCalibrationComponents();

                // Activate all elements in the task scene
                TaskLevel.ActivateAllSceneElements(TaskLevel);
                Session.SessionLevel.AssignExperimenterDisplayRenderTexture(TaskLevel.TaskCam);
                RenderSettings.skybox = SkyboxMaterial; ;

                // Set the Gaze Data Path back to the outer level task folder
                // Reset level and task references
                Session.GazeData.fileName = gazeDataFileName;
                Session.GazeData.folderPath = TaskLevel.TaskDataPath + Path.DirectorySeparatorChar + "GazeData";

                Session.SerialRecvData.fileName = serialRecvDataFileName;
                Session.SerialSentData.fileName = serialSentDataFileName;
                Session.SerialRecvData.folderPath = TaskLevel.TaskDataPath + Path.DirectorySeparatorChar + "SerialRecvData";
                Session.SerialSentData.folderPath = TaskLevel.TaskDataPath + Path.DirectorySeparatorChar + "SerialSentData";


                // Set the current task and trial levels
                Session.TaskLevel = TaskLevel;
                Session.TrialLevel = this;
            });


            DefineControlLevel();
            TrialData.ManuallyDefine();
            TrialData.AddStateTimingData(this);
            StartCoroutine(TrialData.CreateFile());

        }

        private IEnumerator DisableTaskMainBackground()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (TaskLevel.TaskCam != null)
            {
                if (TaskLevel.TaskCam.gameObject.TryGetComponent<Skybox>(out var skyboxComponent))
                    skyboxComponent.enabled = false;
            }
        }

        private IEnumerator HandleLoadingStims()
        {
            foreach (StimGroup sg in TrialStims)
            {
                yield return StartCoroutine(sg.LoadStims());
            }
            TrialStimsLoaded = true;
        }

        protected void SetDelayState(State stateAfterDelay, float duration)
        {
            StateAfterDelay = stateAfterDelay;
            DelayDuration = duration;
        }

        //Used for EventCodes:
        public virtual void AddToStimLists()
        {

        }

        public virtual void FinishTrialCleanup()
        {
        }

        public void ClearActiveTrialHandlers()
        {
            if (Session.SelectionTracker.TrialHandlerNames.Count > 0)
            {
                List<string> toRemove = new List<string>();

                foreach (string handlerName in Session.SelectionTracker.TrialHandlerNames)
                {
                    if (Session.SelectionTracker.ActiveSelectionHandlers.ContainsKey(handlerName))
                    {
                        Session.SelectionTracker.ActiveSelectionHandlers.Remove(handlerName);
                        toRemove.Add(handlerName);
                    }
                }

                foreach (string handlerName in toRemove)
                    Session.SelectionTracker.TrialHandlerNames.Remove(handlerName);
            }
        }

        public void WriteDataFiles()
        {
            StartCoroutine(TrialData.AppendDataToBuffer());
            StartCoroutine(TrialData.AppendDataToFile());

            StartCoroutine(TaskLevel.FrameData.AppendDataToBuffer());
            StartCoroutine(TaskLevel.FrameData.AppendDataToFile());

            if (Session.GazeData != null)
                StartCoroutine(Session.GazeData.AppendDataToFile());

            if(Session.SerialRecvData != null)
                StartCoroutine(Session.SerialRecvData.AppendDataToFile());
            if(Session.SerialSentData != null)
                StartCoroutine(Session.SerialSentData.AppendDataToFile());
         
        }
        
        public bool CheckForcedBlockEnd()
        {
            if (ForceBlockEnd)
            {
                ForceBlockEnd = false;
                return true;
            }
            return false;
        }

       
        protected virtual bool CheckBlockEnd()
        {
            return false;
        }

        protected virtual void DefineTrialStims()
        {

        }
        private void AddAbortCodeKeys()
        {
            AbortCodeDict = new Dictionary<string, int>();

            if (!AbortCodeDict.ContainsKey("EndTrial"))
                AbortCodeDict.Add("EndTrial", 1);

            if (!AbortCodeDict.ContainsKey("RestartBlock"))
                AbortCodeDict.Add("RestartBlock", 2);

            if (!AbortCodeDict.ContainsKey("EndBlock"))
                AbortCodeDict.Add("EndBlock", 3);

            if (!AbortCodeDict.ContainsKey("PreviousBlock"))
                AbortCodeDict.Add("PreviousBlock", 4);

            if (!AbortCodeDict.ContainsKey("EndTask"))
                AbortCodeDict.Add("EndTask", 5);
            
            if (!AbortCodeDict.ContainsKey("NoSelectionMade"))
                AbortCodeDict.Add("NoSelectionMade", 6);
        }

        public void AddRigidBody(GameObject go)
        {
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }


        //Added helper methods for trials.
        public void ActivateChildren(GameObject parent)
        {
            foreach (Transform child in parent.transform)
                child.gameObject.SetActive(true);
        }

        public void DeactivateChildren(GameObject parent)
        {
            foreach (Transform child in parent.transform)
                child.gameObject.SetActive(false);
        }

        public void DestroyChildren(GameObject parent)
        {
            foreach (Transform child in parent.transform)
                Destroy(child.gameObject);
        }

        public void ChangeColor(GameObject go, Color color)
        {
            go.GetComponent<Renderer>().material.color = color;
        }

        public void ChangeColor(List<GameObject> objects, Color color)
        {
            foreach (GameObject go in objects)
                go.GetComponent<Renderer>().material.color = color;
        }

        private void TurnOffInstructionsText()
        {
            if (Session.SessionDef.IsHuman && Session.HumanStartPanel != null && Session.HumanStartPanel.InstructionsOn)
            {
                Session.HumanStartPanel.InstructionsGO.SetActive(false);
                Session.HumanStartPanel.InstructionsOn = false;
                Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["InstructionsOff"]);
            }
        }





        public StimGroup CreateStimGroup(string groupName, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            TaskStims.CreateStimGroup(groupName, setActiveOnInit, setInactiveOnTerm);
            return TaskStims.AllTaskStimGroups[groupName];
        }

        public StimGroup CreateStimGroup(string groupName, IEnumerable<StimDef> stims, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            TaskStims.CreateStimGroup(groupName, stims, setActiveOnInit, setInactiveOnTerm);
            return TaskStims.AllTaskStimGroups[groupName];
        }

        public StimGroup CreateStimGroup(string groupName, IEnumerable<int[]> dimValGroup, string folderPath,
            IEnumerable<string[]> featureNames, string neutralPatternedColorName, Camera cam, float scale = 1, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            TaskStims.CreateStimGroup(groupName, dimValGroup, folderPath, featureNames, neutralPatternedColorName, cam,
                scale, setActiveOnInit, setInactiveOnTerm);
            return TaskStims.AllTaskStimGroups[groupName];
        }

        public StimGroup CreateStimGroup(string groupName, string TaskName, string stimDefFilePath, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            TaskStims.CreateStimGroup(groupName, TaskName, stimDefFilePath, setActiveOnInit, setInactiveOnTerm);
            return TaskStims.AllTaskStimGroups[groupName];
        }

        public StimGroup CreateStimGroup(string groupName, StimGroup sgOrig, IEnumerable<int> stimSubsetIndices, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            TaskStims.CreateStimGroup(groupName, sgOrig, stimSubsetIndices, setActiveOnInit, setInactiveOnTerm);
            return TaskStims.AllTaskStimGroups[groupName];
        }

        public void DestroyStimGroup(StimGroup sg)
        {
            sg.DestroyStimGroup();
            TaskStims.AllTaskStimGroups.Remove(sg.stimGroupName);
        }

        public void DestroyStimGroup(string sgName)
        {
            TaskStims.AllTaskStimGroups[sgName].DestroyStimGroup();
            TaskStims.AllTaskStimGroups.Remove(sgName);
        }

        protected T GetGameObjectStimDefComponent<T>(GameObject go) where T : StimDef
        {
            // return (T) go.GetComponent<StimDef>();
            MethodInfo getStimDef = GetType().GetMethod(nameof(StimDefPointer.GetStimDef)).MakeGenericMethod((new Type[] { StimDefType }));
            return (T)getStimDef.Invoke(this, new object[] { go });

        }

        //MOVED TASK HELPER METHODS, MAYBE MOVE TO TRIALlEVEL_METHODS BELOW##########################
        public Vector2 ScreenToPlayerViewPosition(Vector3 position, Transform playerViewParent)
        {
            Vector2 pvPosition = new Vector2((position[0] / Screen.width) * playerViewParent.GetComponent<RectTransform>().sizeDelta.x, (position[1] / Screen.height) * playerViewParent.GetComponent<RectTransform>().sizeDelta.y);
            return pvPosition;
        }
        public GameObject CreateSquare(string name, Texture2D tex, Vector3 pos, Vector3 scale)
        {
            GameObject SquareGO = GameObject.CreatePrimitive(PrimitiveType.Cube);

            Renderer SquareRenderer = SquareGO.GetComponent<Renderer>();
            SquareGO.name = name;
            SquareRenderer.material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            SquareRenderer.material.SetFloat("_SpecularHighlights",0f);
            SquareRenderer.material.mainTexture = tex;
            SquareGO.transform.position = pos;
            SquareGO.transform.localScale = scale;
            SquareGO.SetActive(false);
            return SquareGO;
        }
        public int chooseReward(Reward[] rewards)
        {
            float totalProbability = 0;
            for (int i = 0; i < rewards.Length; i++)
            {
                totalProbability += rewards[i].Probability;
            }

            if (Math.Abs(totalProbability - 1) > 0.001)
                Debug.LogError("Sum of reward probabilities on this trial is " + totalProbability + ", probabilities will be scaled to sum to 1.");

            float randomNumber = UnityEngine.Random.Range(0, totalProbability);

            Reward selectedReward = rewards[0];
            float curProbSum = 0;
            foreach (Reward r in rewards)
            {
                curProbSum += r.Probability;
                if (curProbSum >= randomNumber)
                {
                    selectedReward = r;
                    break;
                }
            }
            return selectedReward.NumReward;
        }

        public void SetShadowType(string ShadowType, string LightName)
        {
            Light taskLight = GameObject.Find(LightName).GetComponent<Light>();

            if(taskLight == null)
            {
                Debug.LogWarning("NOT SETTING SHADOW TYPE! COULDNT FIND " + LightName + " WHEN TRYING TO SET SHADOW TYPE! ");
                return;
            }

            ShadowType = ShadowType.ToLower(); //User options are None, Soft, Hard

            switch (ShadowType)
            {
                case "none":
                    taskLight.shadows = LightShadows.None;
                    break;
                case "soft":
                    taskLight.shadows = LightShadows.Soft;
                    break;
                case "hard":
                    taskLight.shadows = LightShadows.Hard;
                    break;
                default:
                    Debug.Log("User did not Input None, Soft, or Hard for the Shadow Type");
                    break;
            }
        }
        public string GetContextNestedFilePath(string MaterialFilePath, string contextName, [CanBeNull] string backupContextName = null)
        {
            string contextPath = "";
            string[] filePaths = Directory.GetFiles(MaterialFilePath, $"{contextName}*", SearchOption.AllDirectories);

            if (filePaths.Length >= 1)
            {
                contextPath = filePaths[0];
            }
            else
            {
                Debug.LogWarning($"Context File Path Not Found. Defaulting to {backupContextName}.");
                contextPath = Directory.GetFiles(MaterialFilePath, backupContextName, SearchOption.AllDirectories)[0];
            }

            return contextPath;
        }


        public void LoadSharedTrialTextures()
        {
            try
            {
                HeldTooLongTexture = Resources.Load<Texture2D>($"{Session.DefaultContextFolderPath}/HeldTooLong");
                HeldTooShortTexture = Resources.Load<Texture2D>($"{Session.DefaultContextFolderPath}/HeldTooShort");
                MovedTooFarTexture = Resources.Load<Texture2D>($"{Session.DefaultContextFolderPath}/MovedTooFar");
                TouchFBController.HeldTooLong_Texture = HeldTooLongTexture;
                TouchFBController.HeldTooShort_Texture = HeldTooShortTexture;
                TouchFBController.MovedTooFar_Texture = MovedTooFarTexture;
                HeldTooLongSquareTexture = Resources.Load<Texture2D>($"{Session.DefaultContextFolderPath}/HeldTooLong_Square");
                HeldTooShortSquareTexture = Resources.Load<Texture2D>($"{Session.DefaultContextFolderPath}/HeldTooShort_Square");
                MovedTooFarSquareTexture = Resources.Load<Texture2D>($"{Session.DefaultContextFolderPath}/MovedTooFar_Square");
            }
            catch(Exception e)
            {
                Debug.LogError("FAILED LOADING SHARED TRIAL TEXTURES FROM RESOURCES! " + e.Message.ToString());
            }
        }


        public virtual void ResetTrialVariables()
        {

        }
    }

    public class TrialStims : TaskStims
    {

    }

    public class TrialLevel_Methods
    {

    }
}
