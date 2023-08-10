using System;
using UnityEngine;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using TobiiGaze_Namespace;
using Tobii.Research.Unity;


public class TobiiGaze_TrialLevel : ControlLevel_Trial_Template
{
    public TobiiGaze_TrialDef CurrentTrialDef => GetCurrentTrialDef<TobiiGaze_TrialDef>();
    public TobiiGaze_TaskLevel CurrentTaskLevel => GetTaskLevel<TobiiGaze_TaskLevel>();

    private Calibration calibration;
    private ScreenBasedSaveData screenBasedSaveData;
    private EyeTracker eyeTracker;
    
    //Task Level Variables
    [HideInInspector] public String ContextExternalFilePath;
    public override void DefineControlLevel()
    {
        State Calibrate = new State("Calibrate");
        State Results = new State("Results");
        AddActiveStates(new List<State> { Calibrate, Results });
        Add_ControlLevel_InitializationMethod(() =>
        {
            calibration = GameObject.Find("[Calibration]").GetComponent<Calibration>();
            screenBasedSaveData = GameObject.Find("[SaveData]").GetComponent<ScreenBasedSaveData>();
            eyeTracker = GameObject.Find("[EyeTracker]").GetComponent<EyeTracker>();
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            CurrentTaskLevel.SetSkyBox(GetContextNestedFilePath(ContextExternalFilePath, "DarkGrey"));
            
            // auto set save data true every trial, can turn off before calibration starts
            screenBasedSaveData.SaveData = true;
        });
        SetupTrial.SpecifyTermination(()=>calibration.CalibrationInProgress, Calibrate);
        Calibrate.SpecifyTermination(()=> !calibration.CalibrationInProgress, Results);
    }

    
    
}
