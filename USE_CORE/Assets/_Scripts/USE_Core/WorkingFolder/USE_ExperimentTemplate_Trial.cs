using System;
using System.Collections;
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

namespace USE_ExperimentTemplate_Trial
{
    public abstract class ControlLevel_Trial_Template : ControlLevel
    {
        [HideInInspector] public TrialData TrialData;
        [HideInInspector] public FrameData FrameData;
        [HideInInspector] public SerialSentData SerialSentData;
        [HideInInspector] public SerialRecvData SerialRecvData;
        [HideInInspector] public int BlockCount, TrialCount_InTask, TrialCount_InBlock, AbortCode;
        protected int NumTrialsInBlock;
        [HideInInspector] public SessionDataControllers SessionDataControllers;

        [HideInInspector] public bool StoreData, ForceBlockEnd, SerialPortActive, EyetrackerActive;
        [HideInInspector] public string TaskDataPath, FilePrefix, TrialSummaryString;

        protected State SetupTrial, FinishTrial;

        public ControlLevel_Task_Template TaskLevel;
        public List<TrialDef> TrialDefs;

        [HideInInspector] public TaskStims TaskStims;
        [HideInInspector] public StimGroup PreloadedStims, PrefabStims, ExternalStims, RuntimeStims;
        [HideInInspector] public List<StimGroup> TrialStims;

        [HideInInspector] public ConfigVarStore ConfigUiVariables;
        [HideInInspector] public ExperimenterDisplayController ExperimenterDisplayController;
        
        // Feedback Controllers
        [HideInInspector] public AudioFBController AudioFBController;
        [HideInInspector] public HaloFBController HaloFBController;
        [HideInInspector] public TokenFBController TokenFBController;
        // Input Trackers
        [HideInInspector] public MouseTracker MouseTracker;
        [HideInInspector] public GazeTracker GazeTracker;

        [HideInInspector] public string SelectionType;

        [HideInInspector] public SerialPortThreaded SerialPortController;
        [HideInInspector] public SyncBoxController SyncBoxController;
        [HideInInspector] public EventCodeManager EventCodeManager;
        [HideInInspector] public Dictionary<string, EventCode> TaskEventCodes;

        [HideInInspector] public int InitialTokenAmount;


        // Texture Variables
        [HideInInspector] public Texture2D StartButtonTexture, FBSquareTexture, HeldTooLongTexture, HeldTooShortTexture, BackdropStripesTexture, BackdropTexture;
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
            AddActiveStates(new List<State> { SetupTrial, FinishTrial });

            Cursor.visible = false;
            TokenFBController.enabled = false;

            //DefineTrial();
            Add_ControlLevel_InitializationMethod(() =>
            {
                TrialCount_InBlock = -1;
                TrialStims = new List<StimGroup>();
                AudioFBController.UpdateAudioSource();
                //DetermineNumTrialsInBlock();
            });

            SetupTrial.AddUniversalInitializationMethod(() =>
            {
                AbortCode = 0;
                TrialCount_InTask++;
                TrialCount_InBlock++;
                FrameData.CreateNewTrialIndexedFile(TrialCount_InTask + 1, FilePrefix);
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
            });

            FinishTrial.SpecifyTermination(() => CheckBlockEnd(), () => null);
            FinishTrial.SpecifyTermination(() => CheckForcedBlockEnd(), () => null);
            FinishTrial.SpecifyTermination(() => TrialCount_InBlock < TrialDefs.Count - 1, SetupTrial);
            FinishTrial.SpecifyTermination(() => TrialCount_InBlock == TrialDefs.Count - 1, () => null);

            FinishTrial.AddUniversalTerminationMethod(() =>
            {
                FinishTrialCleanup();

                int nStimGroups = TrialStims.Count;
                for (int iG = 0; iG < nStimGroups; iG++)
                {
                    TrialStims[0].DestroyStimGroup();
                    TrialStims.RemoveAt(0);
                }
                WriteDataFiles();
            });
            DefineControlLevel();
            TrialData.ManuallyDefine();
            TrialData.AddStateTimingData(this);
            TrialData.CreateFile();
           // TrialData.LogDataController(); //USING TO SEE FORMAT OF DATA CONTROLLER


        }

        public virtual void FinishTrialCleanup()
        {

        }

        public void WriteDataFiles()
        {
            TrialData.AppendData();
            TrialData.WriteData();
            FrameData.AppendData();
            FrameData.WriteData();
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

        public void DestroyChildren(GameObject container)
        {
            foreach (Transform child in container.transform)
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
        public Vector2 playerViewPosition(Vector3 position, Transform playerViewParent)
        {
            Vector2 pvPosition = new Vector2((position[0] / Screen.width) * playerViewParent.GetComponent<RectTransform>().sizeDelta.x, (position[1] / Screen.height) * playerViewParent.GetComponent<RectTransform>().sizeDelta.y);
            return pvPosition;
        }
        
        /*public IEnumerable GratedSquareFlash(Texture2D newTexture, GameObject square, float gratingSquareDuration)
        {
            //Grating = true;
            Color32 originalColor = square.GetComponent<Renderer>().material.color;
            Texture originalTexture = square.GetComponent<Renderer>().material.mainTexture;
            square.GetComponent<Renderer>().material.color = new Color32(224, 78, 92, 255);
            square.GetComponent<Renderer>().material.mainTexture = newTexture;
            yield return new WaitForSeconds(gratingSquareDuration);
            square.GetComponent<Renderer>().material.mainTexture = originalTexture;
            square.GetComponent<Renderer>().material.color = originalColor;
            //Grating = false;
            if (square.name == "FBSquare") square.SetActive(false);
        }*/
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
        public int ChooseTokenReward(TokenReward[] tokenRewards)
        {
            float totalProbability = 0;
            for (int i = 0; i < tokenRewards.Length; i++)
            {
                totalProbability += tokenRewards[i].Probability;
            }

            if (Math.Abs(totalProbability - 1) > 0.001)
                Debug.LogError("Sum of token reward probabilities on this trial is " + totalProbability + ", probabilities will be scaled to sum to 1.");

            float randomNumber = UnityEngine.Random.Range(0, totalProbability);

            TokenReward selectedReward = tokenRewards[0];
            float curProbSum = 0;
            foreach (TokenReward tr in tokenRewards)
            {
                curProbSum += tr.Probability;
                if (curProbSum >= randomNumber)
                {
                    selectedReward = tr;
                    break;
                }
            }
            return selectedReward.NumTokens;
        }
        public void SetShadowType(String ShadowType, String LightName)
        {
            //User options are None, Soft, Hard
            switch (ShadowType)
            {
                case "None":
                    GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.None;
                    GameObject.Find(LightName).GetComponent<Light>().shadows = LightShadows.None;
                    break;
                case "Soft":
                    GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Soft;
                    GameObject.Find(LightName).GetComponent<Light>().shadows = LightShadows.Soft;
                    break;
                case "Hard":
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
                contextPath = Directory.GetFiles(MaterialFilePath, backupContextName, SearchOption.AllDirectories)[0]; //Use Default LinearDark if can't find file.
                Debug.Log($"Context File Path Not Found. Defaulting to {backupContextName}.");
            }

            return contextPath;
        }
        public void LoadTextures(String ContextExternalFilePath)
        {
            StartButtonTexture = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "StartButtonImage.png");
            FBSquareTexture = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "Grey.png");
            HeldTooLongTexture = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "HorizontalStripes.png");
            HeldTooShortTexture = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "VerticalStripes.png");
            BackdropStripesTexture = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "bg.png");
            BackdropTexture = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "BackdropGrey.png");
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