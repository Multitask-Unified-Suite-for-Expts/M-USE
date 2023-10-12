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
using USE_Def_Namespace;
using Random = UnityEngine.Random;


namespace USE_ExperimentTemplate_Trial
{
    public abstract class ControlLevel_Trial_Template : ControlLevel
    {
        [HideInInspector] public TrialData TrialData;
        [HideInInspector] public FrameData FrameData;

        [HideInInspector] public int BlockCount, TrialCount_InTask, TrialCount_InBlock, AbortCode;
        
        public int difficultyLevel;
        public int posStep;
        public int negStep;
        public string TrialDefSelectionStyle;
        public int maxDiffLevel;
        public int avgDiffLevel;
        public int diffLevelJitter;
        
        protected int NumTrialsInBlock;
        public List<int> runningPerformance;

        [HideInInspector] public bool ForceBlockEnd;
        [HideInInspector] public string TaskDataPath, TrialSummaryString;
        protected State LoadTrialStims, SetupTrial, FinishTrial, Delay, GazeCalibration;
        
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
        
        [HideInInspector] public bool runCalibration;
        private ControlLevel_Task_Template GazeCalibrationTaskLevel;

        [HideInInspector] public Dictionary<string, EventCode> TaskEventCodes;

        [HideInInspector] public int InitialTokenAmount;

        [HideInInspector] public Dictionary<string, int> AbortCodeDict;

        [HideInInspector] public UI_Debugger UI_Debugger;
        [HideInInspector] public GameObject PauseIconGO;

        [HideInInspector] public bool TrialStimsLoaded;

        // Texture Variables
        [HideInInspector] public Texture2D HeldTooLongTexture, HeldTooShortTexture, MovedTooFarTexture, THR_BackdropTexture;
        
        private float Camera_PulseSentTime = 0f;
        
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
            //Debug.LogWarning("CurrentTrialDefIndex: " + CurrentTrialDefIndex);
            return (T)TrialDefs[CurrentTrialDefIndex];
        }

        public int DetermineCurrentTrialDefIndex()
        {
            switch (TrialDefSelectionStyle)
            {
                case "adaptive":
                    difficultyLevel = TaskLevel.DetermineTrialDefDifficultyLevel(difficultyLevel, runningPerformance, posStep, negStep, maxDiffLevel);
                    Debug.LogWarning("cur difficulty level (after determine): " + difficultyLevel);
                    //Debug.LogWarning("TrialCount_InBlock: " + TrialCount_InBlock + " ------ TrialDefs size: " + TrialDefs.Count);
                    
                    List<int> tieIndices = TrialDefs
                        .Select((trialDef, index) => new { TrialDef = trialDef, Index = index })
                        .Where(item => 
                        {
                            Debug.LogWarning("item.TrialDef.BlockCount: " + item.TrialDef.BlockCount + " /////// BlockCount: " + BlockCount);
                            return (item.TrialDef.DifficultyLevel == difficultyLevel && item.TrialDef.BlockCount == BlockCount);
                        })
                        //.Where(item => item.TrialDef.DifficultyLevel == difficultyLevel && item.TrialDef.BlockCount == BlockCount)
                        .Select(item => item.Index)
                        .ToList();
                    return tieIndices[Random.Range(0, tieIndices.Count)];

                default:
                    Debug.LogWarning("selection style: " + TrialDefSelectionStyle);
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
            LoadTrialStims = new State("LoadTrialStims");
            SetupTrial = new State("SetupTrial");
            FinishTrial = new State("FinishTrial");
            Delay = new State("Delay");
            GazeCalibration = new State("GazeCalibration");


            if (GameObject.Find("GazeCalibration(Clone)") != null)// && TaskLevel.TaskName != "GazeCalibration")
            {
                GazeCalibrationTaskLevel = GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Scripts"). GetComponent<GazeCalibration_TaskLevel>();
                //GazeCalibrationTaskLevel.ConfigName = TaskLevel.TaskName;
                GazeCalibrationTaskLevel.TrialLevel.TaskLevel = GazeCalibrationTaskLevel;
                
                if (TaskLevel.TaskName != "GazeCalibration")
                {
                    GazeCalibration.AddChildLevel(GazeCalibrationTaskLevel);
                    GazeCalibrationTaskLevel.DefineTaskLevel();
                    GazeCalibrationTaskLevel.BlockData.gameObject.SetActive(false);
                    GazeCalibrationTaskLevel.FrameData.gameObject.SetActive(false);
                    GazeCalibrationTaskLevel.TrialData.gameObject.SetActive(false);
                }
                
                // Set the GazeData path back to the current config folder
                SessionValues.GazeData.folderPath = TaskLevel.TaskDataPath + Path.DirectorySeparatorChar +  "GazeData";
            }

            AddActiveStates(new List<State> { LoadTrialStims, SetupTrial, FinishTrial, Delay, GazeCalibration });
            // A state that just waits for some time;
            Delay.AddTimer(() => DelayDuration, () => StateAfterDelay);

            Cursor.visible = false;
            if (TokenFBController != null)
                TokenFBController.enabled = false;

            AddAbortCodeKeys();

            if (SessionValues.SessionDef.IsHuman)
                SessionValues.HumanStartPanel.SetTrialLevel(this);

            UI_Debugger = new UI_Debugger();

            //DefineTrial();
            Add_ControlLevel_InitializationMethod(() =>
            {
                TrialCount_InBlock = -1;
                
                if (TrialCount_InBlock <= 0)
                {
                    DefineCustomTrialDefSelection();
                }
                
                TrialStims = new List<StimGroup>();
                AudioFBController.UpdateAudioSource();
                
                //DetermineNumTrialsInBlock();
            });

            LoadTrialStims.AddUniversalInitializationMethod(() =>
            {
                AbortCode = 0;

                TrialCount_InTask++;
                TrialCount_InBlock++;

                if(!SessionValues.WebBuild && TrialCount_InTask != 0)
                    SessionValues.SessionInfoPanel.UpdateSessionSummaryValues(("totalTrials", 1));

                FrameData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, SessionValues.FilePrefix);
                if(SessionValues.SessionDef.EyeTrackerActive)
                    SessionValues.GazeData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, SessionValues.FilePrefix);

                if (SessionValues.SessionDef.SerialPortActive)
                {
                    SessionValues.SerialRecvData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, SessionValues.FilePrefix);
                    SessionValues.SerialSentData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, SessionValues.FilePrefix);
                }

                SessionValues.ClearStimLists();
                DefineTrialStims();
                StartCoroutine(HandleLoadingStims());
            });
            LoadTrialStims.SpecifyTermination(() => TrialStimsLoaded, SetupTrial, () => TrialStimsLoaded = false);

            SetupTrial.AddUniversalInitializationMethod(() =>
            {
                CurrentTrialDefIndex = DetermineCurrentTrialDefIndex();
                SessionValues.LoadingController.DeactivateLoadingCanvas();

                if (SessionValues.WebBuild)
                    Cursor.visible = true;

                //turning off instructions text at start of each trial, in case they left them on during last trial.
                TurnOffInstructionsText();

                TokenFBController.RecalculateTokenBox(); //recalculate tokenbox incase they switch to fullscreen mode

                SessionValues.EventCodeManager.SendRangeCode("SetupTrialStarts", TrialCount_InTask);

                ResetRelativeStartTime();

                ResetTrialVariables();


                //Send Trial Reward Pulses for Ansen's Camera (if min time between pulses has been elapsed):
                if (SessionValues.SessionDef.SendCameraPulses && SessionValues.SyncBoxController != null && SessionValues.SessionDef.SyncBoxActive)
                {
                    if (Time.time - Camera_PulseSentTime > SessionValues.SessionDef.Camera_TrialPulseMinGap_Sec)
                    {
                        SessionValues.SyncBoxController.SendCameraSyncPulses(SessionValues.SessionDef.Camera_TrialStart_NumPulses, SessionValues.SessionDef.Camera_PulseSize_Ticks);
                        Camera_PulseSentTime = Time.time;
                    }
                }

            });

            SetupTrial.AddDefaultTerminationMethod(() =>
            {
                Input.ResetInputAxes();
                if (SessionValues.SessionDef.IsHuman)
                    SessionValues.HumanStartPanel.AdjustPanelBasedOnTrialNum(TrialCount_InTask, TrialCount_InBlock);
                
                AddToStimLists(); //Seems to work here instead of each task having to call it themselves from InitTrial.

                //Disable the Task's MUSE Background that's set in Session Level's SetTasksMainBackground() method:

                StartCoroutine(DisableTaskMainBackground());


            });

            FinishTrial.AddSpecificInitializationMethod(() =>
            {
                SessionValues.EventCodeManager.SendCodeImmediate("FinishTrialStarts");
            });
            FinishTrial.SpecifyTermination(() => runCalibration && TaskLevel.TaskName != "GazeCalibration", () => GazeCalibration);
            FinishTrial.SpecifyTermination(() => CheckBlockEnd(), () => null);
            FinishTrial.SpecifyTermination(() => CheckForcedBlockEnd(), () => null);
            FinishTrial.SpecifyTermination(() => TrialCount_InBlock < TrialDefs.Count - 1, LoadTrialStims);
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
                
                
                TouchFBController.ClearErrorCounts();
                Resources.UnloadUnusedAssets();
                TrialSummaryString = "";

                SessionValues.ClearStimLists();
            });
            
            GazeCalibration.AddSpecificInitializationMethod(() =>
            {
                GazeCalibrationTaskLevel.TaskCam = TaskLevel.TaskCam;

                GazeCalibrationTaskLevel.ConfigFolderName = "GazeCalibration";
                GazeCalibrationTaskLevel.TaskName = "GazeCalibration";

                UnityEngine.SceneManagement.Scene originalScene = SceneManager.GetSceneByName(TaskLevel.TaskName);
                GameObject[] rootObjects = originalScene.GetRootGameObjects();
                TaskLevel.TaskCanvasses = rootObjects.SelectMany(go => go.GetComponentsInChildren<Canvas>(true)).ToArray();

                foreach (Canvas canvas in TaskLevel.TaskCanvasses)
                {
                    canvas.gameObject.SetActive(false);
                }

                var GazeCalibrationCanvas = GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Canvas");
                var CalibrationCube = GazeCalibrationCanvas.Find("CalibrationCube");
                var GazeCalibrationScripts = GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Scripts");
                var CalibrationGazeTrail = GameObject.Find("TobiiEyeTrackerController").transform.Find("GazeTrail(Clone)");
                //  var CalibrationCube = GameObject.Find("TobiiEyeTrackerController").transform.Find("Cube");

                GazeCalibrationCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                GazeCalibrationCanvas.GetComponent<Canvas>().worldCamera = Camera.main;
                GazeCalibrationCanvas.gameObject.SetActive(true);
                CalibrationGazeTrail.gameObject.SetActive(true);
                CalibrationCube.gameObject.SetActive(true);
                GazeCalibrationScripts.gameObject.SetActive(true);

                GazeCalibrationTaskLevel.BlockData.gameObject.SetActive(true);
                GazeCalibrationTaskLevel.FrameData.gameObject.SetActive(true);
                GazeCalibrationTaskLevel.TrialData.gameObject.SetActive(true);

                // Set the GazeDataPath to be inside the GazeCalibration Folder
                SessionValues.GazeData.folderPath = GazeCalibrationTaskLevel.TaskDataPath + Path.DirectorySeparatorChar + "GazeData";
            });

            GazeCalibration.SpecifyTermination(() => !runCalibration, () => SetupTrial, () =>
           {
               GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Canvas").gameObject.SetActive(false);
               foreach (Canvas canvas in TaskLevel.TaskCanvasses)
               {
                   canvas.gameObject.SetActive(true);
               }
               if (SessionValues.SessionDef.EyeTrackerActive && TobiiEyeTrackerController.Instance.isCalibrating)
               {
                   TobiiEyeTrackerController.Instance.isCalibrating = false;
                   TobiiEyeTrackerController.Instance.ScreenBasedCalibration.LeaveCalibrationMode();
               }

               GazeCalibrationTaskLevel.BlockData.gameObject.SetActive(false);
               GazeCalibrationTaskLevel.FrameData.gameObject.SetActive(false);
               GazeCalibrationTaskLevel.TrialData.gameObject.SetActive(false);

               // Set the Gaze Data Path back to the outer level task folder
               SessionValues.GazeData.folderPath = TaskLevel.TaskDataPath + Path.DirectorySeparatorChar + "GazeData";

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
            if (SessionValues.SelectionTracker.TrialHandlerNames.Count > 0)
            {
                List<string> toRemove = new List<string>();

                foreach (string handlerName in SessionValues.SelectionTracker.TrialHandlerNames)
                {
                    if (SessionValues.SelectionTracker.ActiveSelectionHandlers.ContainsKey(handlerName))
                    {
                        SessionValues.SelectionTracker.ActiveSelectionHandlers.Remove(handlerName);
                        toRemove.Add(handlerName);
                    }
                }

                foreach (string handlerName in toRemove)
                    SessionValues.SelectionTracker.TrialHandlerNames.Remove(handlerName);
            }
        }

        public void WriteDataFiles()
        {
            StartCoroutine(TrialData.AppendDataToBuffer());
            StartCoroutine(TrialData.AppendDataToFile());

            StartCoroutine(FrameData.AppendDataToBuffer());
            StartCoroutine(FrameData.AppendDataToFile());

            if (SessionValues.GazeData != null)
                StartCoroutine(SessionValues.GazeData.AppendDataToFile());

            if(SessionValues.SerialRecvData != null)
                StartCoroutine(SessionValues.SerialRecvData.AppendDataToFile());
            if(SessionValues.SerialSentData != null)
                StartCoroutine(SessionValues.SerialSentData.AppendDataToFile());
         
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

        private void OnApplicationQuit()
        {
            if (TrialData != null)
            {
                StartCoroutine(TrialData.AppendDataToBuffer());
                StartCoroutine(TrialData.AppendDataToFile());
            }
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
            if (SessionValues.SessionDef.IsHuman && SessionValues.HumanStartPanel != null && SessionValues.HumanStartPanel.InstructionsOn)
            {
                SessionValues.HumanStartPanel.InstructionsGO.SetActive(false);
                SessionValues.HumanStartPanel.InstructionsOn = false;
                SessionValues.EventCodeManager.SendCodeImmediate(SessionValues.EventCodeManager.SessionEventCodes["InstructionsOff"]);
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
            ShadowType = ShadowType.ToLower();
            //User options are None, Soft, Hard
            switch (ShadowType)
            {
                case "none":
                    GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.None;
                    GameObject.Find(LightName).GetComponent<Light>().shadows = LightShadows.None;
                    break;
                case "soft":
                    GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Soft;
                    GameObject.Find(LightName).GetComponent<Light>().shadows = LightShadows.Soft;
                    break;
                case "hard":
                    GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Hard;
                    GameObject.Find(LightName).GetComponent<Light>().shadows = LightShadows.Hard;
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
                Debug.Log($"Context File Path Not Found. Defaulting to {backupContextName}.");
                contextPath = Directory.GetFiles(MaterialFilePath, backupContextName, SearchOption.AllDirectories)[0];
            }

            return contextPath;
        }

        //Timing is off:
        //public void LoadTexturesFromServer()
        //{
        //    StartCoroutine(ServerManager.LoadTextureFromServer("Resources/Contexts/HeldTooLong.png", result =>
        //    {
        //        if (result != null)
        //        {
        //            HeldTooLongTexture = result;
        //            TouchFBController.HeldTooLong_Texture = HeldTooLongTexture;
        //        }
        //        else
        //            Debug.Log("HELDTOOLONG TEXTURE NULL FROM SERVER!");
        //    }));

        //    StartCoroutine(ServerManager.LoadTextureFromServer("Resources/Contexts/HeldTooShort.png", result =>
        //    {
        //        if (result != null)
        //        {
        //            HeldTooShortTexture = result;
        //            TouchFBController.HeldTooShort_Texture = HeldTooShortTexture;
        //        }
        //        else
        //            Debug.Log("HELDTOOSHORT TEXTURE NULL FROM SERVER!");
        //    }));

        //    StartCoroutine(ServerManager.LoadTextureFromServer("Resources/Contexts/bg.png", result =>
        //    {
        //        if (result != null)
        //        {
        //            MoveTooFarTexture = result;
        //            TouchFBController.MovedTooFar_Texture = MoveTooFarTexture;
        //        }
        //        else
        //            Debug.Log("BACKDROP_STRIPES_TEXTURE NULL FROM SERVER");

        //    }));

        //    StartCoroutine(ServerManager.LoadTextureFromServer("Resources/Contexts/THR_Backdrop.png", result =>
        //    {
        //        if (result != null)
        //        {
        //            THR_BackdropTexture = result;
        //        }
        //        else
        //            Debug.Log("THR BACKDROP TEXTURE NULL FROM SERVER");
        //    }));
        //}

        public IEnumerator LoadSharedTrialTextures()
        {
            if (SessionValues.UsingDefaultConfigs)
            {
                HeldTooLongTexture = Resources.Load<Texture2D>($"{SessionValues.DefaultContextFolderPath}/HeldTooLong");
                HeldTooShortTexture = Resources.Load<Texture2D>($"{SessionValues.DefaultContextFolderPath}/HeldTooShort");
                MovedTooFarTexture = Resources.Load<Texture2D>($"{SessionValues.DefaultContextFolderPath}/bg");
            }
            else if (SessionValues.UsingLocalConfigs)
            {
                HeldTooLongTexture = LoadExternalPNG(GetContextNestedFilePath(SessionValues.SessionDef.ContextExternalFilePath, "HeldTooLong.png"));
                HeldTooShortTexture = LoadExternalPNG(GetContextNestedFilePath(SessionValues.SessionDef.ContextExternalFilePath, "HeldTooShort.png"));
                MovedTooFarTexture = LoadExternalPNG(GetContextNestedFilePath(SessionValues.SessionDef.ContextExternalFilePath, "bg.png"));
            }
            else if (SessionValues.UsingServerConfigs)
            {
                yield return StartCoroutine(ServerManager.LoadTextureFromServer($"{ServerManager.ServerContextFolderPath}/HeldTooLong.png", result =>
                {
                    if (result != null)
                        HeldTooLongTexture = result;
                    else
                        Debug.LogWarning("HELDTOOLONG TEXTURE NULL FROM SERVER!");
                }));

                yield return StartCoroutine(ServerManager.LoadTextureFromServer($"{ServerManager.ServerContextFolderPath}/HeldTooShort.png", result =>
                {
                    if (result != null)
                        HeldTooShortTexture = result;
                    else
                        Debug.LogWarning("HELDTOOSHORT TEXTURE NULL FROM SERVER!");
                }));

                yield return StartCoroutine(ServerManager.LoadTextureFromServer($"{ServerManager.ServerContextFolderPath}/bg.png", result =>
                {
                    if (result != null)
                    {
                        MovedTooFarTexture = result;
                        TouchFBController.MovedTooFar_Texture = MovedTooFarTexture;
                    }
                    else
                        Debug.LogWarning("BACKDROP_STRIPES_TEXTURE NULL FROM SERVER");
                }));
            }
            TouchFBController.HeldTooLong_Texture = HeldTooLongTexture;
            TouchFBController.HeldTooShort_Texture = HeldTooShortTexture;
            TouchFBController.MovedTooFar_Texture = MovedTooFarTexture;
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
