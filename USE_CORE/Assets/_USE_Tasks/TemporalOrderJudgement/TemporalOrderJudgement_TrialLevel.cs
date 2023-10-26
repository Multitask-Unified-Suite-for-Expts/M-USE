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



using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using TemporalOrderJudgement_Namespace;
using UnityEngine.UI;
using ConfigDynamicUI;

public class TemporalOrderJudgement_TrialLevel : ControlLevel_Trial_Template
{
    public TemporalOrderJudgement_TrialDef CurrentTrial => GetCurrentTrialDef<TemporalOrderJudgement_TrialDef>();
    public TemporalOrderJudgement_TaskLevel CurrentTaskLevel => GetTaskLevel<TemporalOrderJudgement_TaskLevel>();
    public TemporalOrderJudgement_TaskDef CurrentTask => GetTaskDef<TemporalOrderJudgement_TaskDef>();

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration;

    public GameObject TOJ_CanvasGO;

    private GameObject StartButton;

    private enum TrialType_Enum { Audio, Visual, Both};
    private TrialType_Enum CurrentTrialType; //use this to control logic 

    private GameObject VisualStimGO;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State FixationCross = new State("FixationCross");
        State Flash = new State("Flash");
        State Beep = new State("Beep");
        State BeepAndFlash = new State("BeepAndFlash");
        State Response = new State("Response");
        State Feedback = new State("Feedback");

        Add_ControlLevel_InitializationMethod(() =>
        {
            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(TOJ_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }
        });

        //SetupTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddDefaultInitializationMethod(() =>
        {
            SetTrialType();
            CreateVisualStim();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, Response); //may need to change which state to end on
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 30, TOJ_CanvasGO, true); 

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), FixationCross); //NOT SURE WHERE IT SHOULD GO NEXT

        //FixationCross state ----------------------------------------------------------------------------------------------------------------------------------------------
        FixationCross.AddSpecificInitializationMethod(() =>
        {
            Debug.LogWarning("CROSS STATE!");
        });

        //Flash state ----------------------------------------------------------------------------------------------------------------------------------------------
        Flash.AddSpecificInitializationMethod(() =>
        {
            Debug.LogWarning("FLASH STATE!");
        });

        //Beep state ----------------------------------------------------------------------------------------------------------------------------------------------
        Beep.AddSpecificInitializationMethod(() =>
        {
            Debug.LogWarning("BEEP STATE!");
        });

        //BeepAndFlash state ----------------------------------------------------------------------------------------------------------------------------------------------
        BeepAndFlash.AddSpecificInitializationMethod(() =>
        {
            Debug.LogWarning("BeepAndFlash STATE!");
        });

        //Response state ----------------------------------------------------------------------------------------------------------------------------------------------
        Response.AddSpecificInitializationMethod(() =>
        {
            Debug.LogWarning("Response STATE!");
        });

        //Feedback state ----------------------------------------------------------------------------------------------------------------------------------------------
        Feedback.AddSpecificInitializationMethod(() =>
        {
            Debug.LogWarning("Feedback STATE!");
        });


    }

    private void CreateVisualStim()
    {
        if (VisualStimGO != null)
            Destroy(VisualStimGO);

        VisualStimGO = new GameObject("SpatialCue");
        VisualStimGO.SetActive(false);
        VisualStimGO.transform.parent = TOJ_CanvasGO.transform;
        VisualStimGO.transform.localScale = Vector3.one;
        RectTransform rect = VisualStimGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(CurrentTrial.VisualStimSize, CurrentTrial.VisualStimSize); //set to size specified in trial def
        Image image = VisualStimGO.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>(CurrentTrial.VisualStimIdentity); //Load the stim's image from resources!
    }

    private void SetTrialType()
    {
        switch(CurrentTrial.TrialTypeString)
        {
            case "Audio":
                CurrentTrialType = TrialType_Enum.Audio;
                break;
            case "Visual":
                CurrentTrialType = TrialType_Enum.Visual;
                break;
            case "Both":
                CurrentTrialType = TrialType_Enum.Both;
                break;
        }
    }

    protected override void DefineTrialStims()
    {
    }
    
}
