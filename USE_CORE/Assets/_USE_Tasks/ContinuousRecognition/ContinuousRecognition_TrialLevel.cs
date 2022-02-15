using UnityEngine;
using System.Collections.Generic;
using USE_ExperimentTemplate;
using USE_States;
using USE_StimulusManagement;
using ContinuousRecognition_Namespace;

public class ContinuousRecognition_TrialLevel : ControlLevel_Trial_Template
{
    public ContinuousRecognition_TrialDef CurrentTrialDef => GetCurrentTrialDef<ContinuousRecognition_TrialDef>();

    public float DisplayStimsDuration, TrialEndDuration;

    public override void DefineControlLevel()
    {
        
        State initTrial = new State("InitTrial");
        State displayStims = new State("DisplayStims");
        State chooseStim = new State("ChooseStim");
        State touchFeedback = new State("TouchFeedback");
        State tokenFeedback = new State("TokenFeedback");
        State trialEnd = new State("TrialEnd");
        AddActiveStates(new List<State> {initTrial, displayStims, chooseStim, touchFeedback, tokenFeedback, trialEnd});
        
        SetupTrial.SpecifyTermination(() => true, initTrial);
        //initTrial follows same logic as StartButton in WhatWhenWhere task (see Seema/Nicole)

        displayStims.AddTimer(() => DisplayStimsDuration, chooseStim);

        bool StimIsChosen = false;
        chooseStim.AddInitializationMethod(()=>StimIsChosen = false);
        chooseStim.AddUpdateMethod(() =>
        {
            //add something that checks for click on object, checks its PreviouslyChosen bool for later token feedback, sets PreviouslyChosen and StimIsChosen to true; - see WWW
        });
        chooseStim.SpecifyTermination(() => StimIsChosen, touchFeedback);
        
        //touchfeedback performs touch feedback - see WWW
        bool touchFeedbackFinished = false;
        touchFeedback.SpecifyTermination(()=>touchFeedbackFinished, tokenFeedback);
        
        //tokenfeedback - make empty for now, automatically jump to trialEnd
        tokenFeedback.SpecifyTermination(()=>true, trialEnd);
        
        trialEnd.AddTimer(()=>TrialEndDuration, FinishTrial);

    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        StimGroup currentTrialStims = new StimGroup("CurrentTrialStims", ExternalStims, CurrentTrialDef.TrialStimIndices); //ExternalStims in this call will be replaced with CurrentBlockDef.BlockStims once Marcus gets that working
        currentTrialStims.SetLocations(CurrentTrialDef.TrialStimLocations);
        currentTrialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"), GetStateFromName("TokenFeedback"));
        TrialStims.Add(currentTrialStims);
    }
    
}
