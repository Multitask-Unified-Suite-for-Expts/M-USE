using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using USE_States;
using USE_StimulusManagement;
using ConfigDynamicUI;
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
        [HideInInspector] public int BlockCount, TrialCount_InTask, TrialCount_InBlock, AbortCode;
        protected int NumTrialsInBlock;
        [HideInInspector] public SessionDataControllers SessionDataControllers;

        [HideInInspector] public bool StoreData, ForceBlockEnd;
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
            //DefineTrial();
            AddInitializationMethod(() =>
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
                if (TrialCount_InTask >= 999)
                    FrameData.fileName = FilePrefix + "__FrameData_Trial_" + (TrialCount_InTask + 1) + ".txt";
                else if (TrialCount_InTask >= 99)
                    FrameData.fileName = FilePrefix + "__FrameData_Trial_0" + (TrialCount_InTask + 1) + ".txt";
                else if (TrialCount_InTask >= 9)
                    FrameData.fileName = FilePrefix + "__FrameData_Trial_00" + (TrialCount_InTask + 1) + ".txt";
                else
                    FrameData.fileName = FilePrefix + "__FrameData_Trial_000" + (TrialCount_InTask + 1) + ".txt";
                FrameData.CreateFile();
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
                TrialData.AppendData();
                TrialData.WriteData();
                FrameData.AppendData();
                FrameData.WriteData();
                int nStimGroups = TrialStims.Count;
                for (int iG = 0; iG < nStimGroups; iG++)
                {
                    TrialStims[0].DestroyStimGroup();
                    TrialStims.RemoveAt(0);
                }
                //WriteDataFiles();
            });
            DefineControlLevel();
            TrialData.ManuallyDefine();
            TrialData.AddStateTimingData(this);
            TrialData.CreateFile();


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