using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using USE_States;
using USE_StimulusManagement;
using ConfigDynamicUI;
using JetBrains.Annotations;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Task;
using SelectionTracking;
using USE_UI;
using System.IO.Ports;
using Tobii.Research;
using Tobii.Research.Unity;
using USE_DisplayManagement;
using USE_ExperimentTemplate_Block;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEditor.SearchService;

namespace USE_ExperimentTemplate_Trial
{
    public abstract class ControlLevel_Trial_Template : ControlLevel
    {
        [HideInInspector] public TrialData TrialData;
        [HideInInspector] public FrameData FrameData;
        [HideInInspector] public USE_ExperimentTemplate_Data.GazeData GazeData;
        [HideInInspector] public SerialSentData SerialSentData;
        [HideInInspector] public SerialRecvData SerialRecvData;
        [HideInInspector] public int BlockCount, TrialCount_InTask, TrialCount_InBlock, AbortCode;
        protected int NumTrialsInBlock;
        [HideInInspector] public SessionDataControllers SessionDataControllers;

        [HideInInspector] public bool StoreData, ForceBlockEnd, SerialPortActive, EyetrackerActive;
        [HideInInspector] public string TaskDataPath, FilePrefix, TrialSummaryString;

        protected State SetupTrial, FinishTrial, Delay, GazeCalibration;
        
        protected State StateAfterDelay = null;
        protected float DelayDuration = 0;

        public ControlLevel_Task_Template TaskLevel;
        public List<TrialDef> TrialDefs;

        [HideInInspector] public TaskStims TaskStims;
        [HideInInspector] public StimGroup PreloadedStims, PrefabStims, ExternalStims, RuntimeStims;
        [HideInInspector] public List<StimGroup> TrialStims;

        [HideInInspector] public ConfigVarStore ConfigUiVariables;
        [HideInInspector] public ExperimenterDisplayController ExperimenterDisplayController;
        [HideInInspector] public SessionInfoPanel SessionInfoPanel;
        [HideInInspector] public float TrialCompleteTime;

        [HideInInspector] public SelectionTracker SelectionTracker;

        // Feedback Controllers
        [HideInInspector] public TouchFBController TouchFBController;
        [HideInInspector] public AudioFBController AudioFBController;
        [HideInInspector] public HaloFBController HaloFBController;
        [HideInInspector] public TokenFBController TokenFBController;
        [HideInInspector] public SliderFBController SliderFBController;
        
        // Input Trackers
        [HideInInspector] public MouseTracker MouseTracker;
        [HideInInspector] public GazeTracker GazeTracker;
        [HideInInspector] public TobiiEyeTrackerController TobiiEyeTrackerController;
        [HideInInspector] public ScreenBasedCalibration ScreenBasedCalibration;
        [HideInInspector] public DisplayArea DisplayArea;

        [HideInInspector] public string SelectionType;
        [HideInInspector] public bool EyeTrackerActive;
        [HideInInspector] public EyeTrackerData_Namespace.TobiiGazeSample TobiiGazeSample;
        [HideInInspector] public bool runCalibration;
        private ControlLevel_Task_Template GazeCalibrationTaskLevel;

        [HideInInspector] public SerialPortThreaded SerialPortController;
        [HideInInspector] public SyncBoxController SyncBoxController;
        [HideInInspector] public EventCodeManager EventCodeManager;
        [HideInInspector] public Dictionary<string, EventCode> TaskEventCodes;
        [HideInInspector] public Dictionary<string, EventCode> SessionEventCodes;

        [HideInInspector] public DisplayController DisplayController;


        [HideInInspector] public int InitialTokenAmount;

        [HideInInspector] public Dictionary<string, int> AbortCodeDict;

        [HideInInspector] public float ShotgunRaycastSpacing_DVA;
        [HideInInspector] public float ParticipantDistance_CM;
        [HideInInspector] public float ShotgunRaycastCircleSize_DVA;

        [HideInInspector] public bool UseDefaultConfigs;

        [HideInInspector] public bool IsHuman;
        [HideInInspector] public HumanStartPanel HumanStartPanel;
        [HideInInspector] public GameObject TaskSelectionCanvasGO;



        // Texture Variables
        [HideInInspector] public Texture2D HeldTooLongTexture, HeldTooShortTexture, 
            BackdropStripesTexture, THR_BackdropTexture;
        //[HideInInspector] public bool Grating;
        
        //protected TrialDef CurrentTrialDef;
        public T GetCurrentTrialDef<T>() where T : TrialDef
        {
            return (T)TrialDefs[TrialCount_InBlock];
        }

        public T GetTaskLevel<T>() where T: ControlLevel_Task_Template
        {
            return (T)TaskLevel;
        }

        public Type TrialDefType, StimDefType;

        public void DefineTrialLevel()
        {
            SetupTrial = new State("SetupTrial");
            FinishTrial = new State("FinishTrial");
            Delay = new State("Delay");
            GazeCalibration = new State("Calibration");
            
            if (GameObject.Find("Calibration(Clone)") != null)
            {
                GazeCalibrationTaskLevel = GameObject.Find("Calibration(Clone)").transform.Find("Calibration_Scripts"). GetComponent<GazeCalibration_TaskLevel>();
                GazeCalibration.AddChildLevel(GazeCalibrationTaskLevel);
                GazeCalibrationTaskLevel.TrialLevel.TaskLevel = GazeCalibrationTaskLevel;

            }

            AddActiveStates(new List<State> { SetupTrial, FinishTrial, Delay, GazeCalibration });
            // A state that just waits for some time;
            Delay.AddTimer(() => DelayDuration, () => StateAfterDelay);

            Cursor.visible = false;
            if (TokenFBController != null)
                TokenFBController.enabled = false;

            AddAbortCodeKeys();

            if (IsHuman)
                HumanStartPanel.SetTrialLevel(this);


            //DefineTrial();
            Add_ControlLevel_InitializationMethod(() =>
            {
                #if (!UNITY_WEBGL)
                        SessionInfoPanel = GameObject.Find("SessionInfoPanel").GetComponent<SessionInfoPanel>();
                #endif

                TrialCount_InBlock = -1;
                TrialStims = new List<StimGroup>();
                AudioFBController.UpdateAudioSource();
                //DetermineNumTrialsInBlock();
            });

            SetupTrial.AddUniversalInitializationMethod(() =>
            {
                TaskSelectionCanvasGO.SetActive(false);

                EventCodeManager.SendCodeImmediate(SessionEventCodes["SetupTrialStarts"]);

                Input.ResetInputAxes();

                AbortCode = 0;

                #if (!UNITY_WEBGL)
                    SessionInfoPanel.UpdateSessionSummaryValues(("totalTrials",1));
                #endif

                #if (UNITY_WEBGL)
                    Cursor.visible = true;
                #endif

                TrialCount_InTask++;
                TrialCount_InBlock++;
                FrameData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, FilePrefix);
                GazeData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, FilePrefix);
                // DO THIS FOR THE GAZE DATA!!!!
                if (TaskLevel.SerialPortActive)
                {
                    SerialRecvData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, FilePrefix);
                    SerialSentData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, FilePrefix);
                }

                // FrameData.fileName =
                //     FilePrefix + "__FrameData_Trial_" + FrameData.GetNiceIntegers(4, TrialCount_InTask + 1);
                // FrameData.CreateFile();
                DefineTrialStims();
                ResetRelativeStartTime();

                foreach (StimGroup sg in TrialStims)
                {
                    sg.LoadStims();
                }

                ResetTrialVariables();
            });
            SetupTrial.AddDefaultTerminationMethod(() =>
            {
                if (IsHuman)
                    HumanStartPanel.AdjustPanelBasedOnTrialNum(TrialCount_InTask, TrialCount_InBlock);
                
            });

            FinishTrial.AddInitializationMethod(() => EventCodeManager.SendCodeImmediate(SessionEventCodes["FinishTrialStarts"]));
            FinishTrial.SpecifyTermination(() => runCalibration && TaskLevel.TaskName != "GazeCalibration", () => GazeCalibration);

            FinishTrial.SpecifyTermination(() => CheckBlockEnd(), () => null);
            FinishTrial.SpecifyTermination(() => CheckForcedBlockEnd(), () => null);
            FinishTrial.SpecifyTermination(() => TrialCount_InBlock < TrialDefs.Count - 1, SetupTrial);
            FinishTrial.SpecifyTermination(() => TrialCount_InBlock == TrialDefs.Count - 1, () => null);

            FinishTrial.AddUniversalTerminationMethod(() =>
            {
                TrialCompleteTime = FinishTrial.TimingInfo.StartTimeAbsolute + (Time.time - FinishTrial.TimingInfo.StartTimeAbsolute);

                FinishTrialCleanup();
                ClearActiveTrialHandlers();

                int nStimGroups = TrialStims.Count;
                for (int iG = 0; iG < nStimGroups; iG++)
                {
                    TrialStims[0].DestroyStimGroup();
                    TrialStims.RemoveAt(0);
                }
                WriteDataFiles();
            });
            
            GazeCalibration.AddInitializationMethod(() =>
            {
                GazeCalibrationTaskLevel.TaskCam = TaskLevel.TaskCam;

                GazeCalibrationTaskLevel.ConfigName = "GazeCalibration";
                GazeCalibrationTaskLevel.TaskName = "GazeCalibration";

                UnityEngine.SceneManagement.Scene originalScene = SceneManager.GetSceneByName(TaskLevel.TaskName);
                GameObject[] rootObjects = originalScene.GetRootGameObjects();
                TaskLevel.TaskCanvasses = rootObjects.SelectMany(go => go.GetComponentsInChildren<Canvas>(true)).ToArray();

                foreach (Canvas canvas in TaskLevel.TaskCanvasses)
                {
                    canvas.gameObject.SetActive(false);
                }

                var GazeCalibrationCanvas = GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Canvas");
                var GazeCalibrationScripts = GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Scripts");
                //  var CalibrationGazeTrail = GameObject.Find("TobiiEyeTrackerController").transform.Find("GazeTrail(Clone)");
                //  var CalibrationCube = GameObject.Find("TobiiEyeTrackerController").transform.Find("Cube");

                GazeCalibrationCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                GazeCalibrationCanvas.GetComponent<Canvas>().worldCamera = Camera.main;
                GazeCalibrationCanvas.gameObject.SetActive(true);
             //   CalibrationGazeTrail.gameObject.SetActive(true);
                GazeCalibrationScripts.gameObject.SetActive(true);
              //  CalibrationCube.gameObject.SetActive(true);

            });

           GazeCalibration.SpecifyTermination(() => !runCalibration, () => SetupTrial, () =>
           {
               GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Canvas").gameObject.SetActive(false);
               foreach (Canvas canvas in TaskLevel.TaskCanvasses)
               {
                   canvas.gameObject.SetActive(true);
               }
               if (GazeCalibrationTaskLevel.EyeTrackerActive && TobiiEyeTrackerController.Instance.isCalibrating)
               {
                   TobiiEyeTrackerController.Instance.isCalibrating = false;
                   TobiiEyeTrackerController.Instance.ScreenBasedCalibration.LeaveCalibrationMode();
               }
           });

            DefineControlLevel();
            TrialData.ManuallyDefine();
            TrialData.AddStateTimingData(this);
            TrialData.CreateFile();
        }
        

        protected void SetDelayState(State stateAfterDelay, float duration)
        {
            StateAfterDelay = stateAfterDelay;
            DelayDuration = duration;
        }

        public virtual void FinishTrialCleanup()
        {
        }

        public void ClearActiveTrialHandlers()
        {
            if (SelectionTracker.TrialHandlerNames.Count > 0)
            {
                List<string> toRemove = new List<string>();

                foreach (string handlerName in SelectionTracker.TrialHandlerNames)
                {
                    if (SelectionTracker.ActiveSelectionHandlers.ContainsKey(handlerName))
                    {
                        SelectionTracker.ActiveSelectionHandlers.Remove(handlerName);
                        toRemove.Add(handlerName);
                    }
                }

                foreach (string handlerName in toRemove)
                    SelectionTracker.TrialHandlerNames.Remove(handlerName);
            }
        }

        public void WriteDataFiles()
        {
            TrialData.AppendData();
            TrialData.WriteData();
            FrameData.AppendData();
            FrameData.WriteData();
            GazeData.WriteData();
            // ADD WRITE AND APPEND GAZE DATA
            if (SerialPortActive)
            {
                SerialRecvData.WriteData();
                SerialSentData.WriteData();
            }
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
                TrialData.AppendData();
                TrialData.WriteData();
            }
        }

        private void AddAbortCodeKeys()
        {
            AbortCodeDict = new Dictionary<string, int>();

            if (!AbortCodeDict.ContainsKey("Pause"))
                AbortCodeDict.Add("Pause", 1);

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

        // MethodInfo taskStimDefFromPrefabPath = GetType().GetMethod(nameof(TaskStimDefFromPrefabPath))
        // 		.MakeGenericMethod((new Type[] {StimDefType}));
        // 		taskStimDefFromPrefabPath.Invoke(this, new object[] {path, PreloadedStims});


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
        public void SetShadowType(String ShadowType, String LightName)
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
                contextPath = filePaths[0];
            else
            {
                contextPath = Directory.GetFiles(MaterialFilePath, backupContextName, SearchOption.AllDirectories)[0];
                Debug.Log($"Context File Path Not Found. Defaulting to {backupContextName}.");
            }

            return contextPath;
        }

        public void LoadTexturesFromResources()
        {
            HeldTooLongTexture = Resources.Load<Texture2D>("DefaultResources/Contexts/TaskRelatedImages/HorizontalStripes");
            HeldTooShortTexture = Resources.Load<Texture2D>("DefaultResources/Contexts/TaskRelatedImages/VerticalStripes");
            BackdropStripesTexture = Resources.Load<Texture2D>("DefaultResources/Contexts/TaskRelatedImages/bg");
            THR_BackdropTexture = Resources.Load<Texture2D>("DefaultResources/Contexts/TaskRelatedImages/Concrete4");

            TouchFBController.HeldTooLong_Texture = HeldTooLongTexture;
            TouchFBController.HeldTooShort_Texture = HeldTooShortTexture;
            TouchFBController.MovedTooFar_Texture = BackdropStripesTexture;
        }

        public void LoadTextures(String ContextExternalFilePath)
        {
            HeldTooLongTexture = LoadPNG(GetContextNestedFilePath(ContextExternalFilePath, "HorizontalStripes.png"));
            HeldTooShortTexture = LoadPNG(GetContextNestedFilePath(ContextExternalFilePath, "VerticalStripes.png"));
            BackdropStripesTexture = LoadPNG(GetContextNestedFilePath(ContextExternalFilePath, "bg.png"));
            THR_BackdropTexture = LoadPNG(GetContextNestedFilePath(ContextExternalFilePath, "Concrete4.png"));

            TouchFBController.HeldTooLong_Texture = HeldTooLongTexture;
            TouchFBController.HeldTooShort_Texture = HeldTooShortTexture;
            TouchFBController.MovedTooFar_Texture = BackdropStripesTexture;
        }

        public virtual void ResetTrialVariables()
        {

        }
    }

    public class TrialStims : TaskStims
    {

    }


    public abstract class TrialDef
    {
        public int BlockCount, TrialCountInBlock, TrialCountInTask;
        public TrialStims TrialStims;
    }

    public class TrialLevel_Methods
    {

    }
}
