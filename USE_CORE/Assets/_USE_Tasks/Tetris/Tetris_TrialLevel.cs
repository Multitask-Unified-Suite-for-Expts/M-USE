using UnityEngine;
using System.Collections.Generic;
using USE_States;
using System;
using Random = UnityEngine.Random;
using ConfigDynamicUI;
using USE_ExperimentTemplate_Trial;
using System.Linq;
using USE_UI;
using Tetris_Namespace;
using ContinuousRecognition_Namespace;
using static TouchFBController;

public class Tetris_TrialLevel : ControlLevel_Trial_Template
{
    public Tetris_TrialDef currentTrial => GetCurrentTrialDef<Tetris_TrialDef>();
    public Tetris_TaskLevel currentTask => GetTaskLevel<Tetris_TaskLevel>();


    public GameObject Tetris_CanvasGO;
    [HideInInspector] public GameObject StartButton;
    [HideInInspector] public float ButtonScale;
    [HideInInspector] public Vector3 ButtonPosition;
    [HideInInspector] public float TouchFeedbackDuration;


    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration;

    public List<TetrisBlock> TetrisBlocks;
    public Transform[] BlockTransforms;

    public GameObject BorderGO;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State Play = new State("Play");
        State Feedback = new State("Feedback");
        AddActiveStates(new List<State> { InitTrial, Play, Feedback});


        //Add_ControlLevel_InitializationMethod(() =>
        //{
        //    BorderGO = GameObject.Find("Border");
        //    //Transform[] childTransforms = BorderGO.GetComponentsInChildren<Transform>();
        //    //BlockTransforms = childTransforms.Where(t => t.name.Contains("Block")).ToArray();
        //    //foreach (Transform blockTransform in BlockTransforms)
        //    //    blockTransform.gameObject.SetActive(false);
        //    BorderGO.SetActive(false);

        //    if (StartButton == null)
        //    {
        //        StartButton = SessionValues.USE_StartButton.CreateStartButton(Tetris_CanvasGO.GetComponent<Canvas>(), ButtonPosition, ButtonScale);
        //        SessionValues.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
        //    }
        //});

        //InitTrial State-------------------------------------------------------
        //var ShotgunHandler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", SessionValues.MouseTracker, InitTrial, InitTrial);
        //TouchFBController.EnableTouchFeedback(ShotgunHandler, TouchFeedbackDuration, ButtonScale, Tetris_CanvasGO);

        //InitTrial.AddInitializationMethod(() =>
        //{
        //    if (!Tetris_CanvasGO.activeInHierarchy)
        //        Tetris_CanvasGO.SetActive(true);

        //    if(ShotgunHandler.AllSelections.Count > 0)
        //        ShotgunHandler.ClearSelections();
        //    ShotgunHandler.MinDuration = minObjectTouchDuration.value;
        //    ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;
        //});
        //InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatches(SessionValues.USE_StartButton.StartButtonChildren), Play);


        ////Play State-------------------------------------------------------
        //Play.AddInitializationMethod(() =>
        //{
        //    BorderGO.SetActive(true);
        //});

        ////Feedback State-------------------------------------------------------
        //Feedback.AddInitializationMethod(() =>
        //{

        //});

    }



}
