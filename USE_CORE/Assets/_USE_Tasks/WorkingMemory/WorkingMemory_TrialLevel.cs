using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_States;
using USE_StimulusManagement;
using WorkingMemory_Namespace;

public class WorkingMemory_TrialLevel : ControlLevel_Trial_Template
{
    public WorkingMemory_TrialDef CurrentTrialDef => GetCurrentTrialDef<WorkingMemory_TrialDef>();

    private StimGroup sampleStims, targetStims, distractorStims1, distractorStims2;
    
    public override void DefineControlLevel()
    {
        State initTrial = new State("InitTrial");
        State delay = new State("Delay");
        State displaySample = new State("DisplaySample");
        State displayDistractors1 = new State("DisplayDistractors1");
        State searchDisplay = new State("SearchDisplay");
        State selectionFeedback = new State("SelectionFeedback");
        State tokenFeedback = new State("TokenFeedback");
        State trialEnd = new State("TrialEnd");
        
        AddActiveStates(new List<State> {initTrial, delay, displaySample, displayDistractors1, searchDisplay, selectionFeedback, tokenFeedback, trialEnd});
        //

        State stateAfterDelay = null;
        float delayDuration = 0;
        delay.AddTimer(() => delayDuration, stateAfterDelay);
        
        SetupTrial.SpecifyTermination(() => true, initTrial);
        
        initTrial.AddTimer(()=>CurrentTrialDef.initTrialDuration, delay, ()=>
        {
            stateAfterDelay = displaySample;
            delayDuration = CurrentTrialDef.baselineDuration;
        });

        displaySample.AddTimer(()=>CurrentTrialDef.displaySampleDuration, delay, ()=>
        {
            stateAfterDelay = displayDistractors1;
            delayDuration = CurrentTrialDef.delay1Duration;
        });
        
        displayDistractors1.AddTimer(()=>CurrentTrialDef.displayDistractors1Duration, delay, ()=>
        {
            stateAfterDelay = searchDisplay;
            delayDuration = CurrentTrialDef.delay2Duration;
        });
        
        
        bool responseMade = false;
        searchDisplay.AddInitializationMethod(() => responseMade = false);
        //add update function where choice is made
        searchDisplay.SpecifyTermination(()=> responseMade, selectionFeedback);
        searchDisplay.AddTimer(()=>CurrentTrialDef.maxSearchduration, FinishTrial);
        
        selectionFeedback.AddInitializationMethod(() => { });
        //adapt from ChoseWrong/Right in whatwhenwhere task
        selectionFeedback.AddTimer(()=> CurrentTrialDef.selectionFbDuration, tokenFeedback);
        
        
        bool tokenUpdated = false;
        tokenFeedback.AddInitializationMethod(() => tokenUpdated = false);
        //wait for Marcus to integrate token fb
        tokenFeedback.SpecifyTermination(() => true, trialEnd); //()=> tokenUpdated, tokenFeedback);

        trialEnd.AddTimer(()=>CurrentTrialDef.trialEndDuration, FinishTrial);
        
        //adapt StartButton from whatwhenwhere task
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        
        
        sampleStims = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.TargetIndices);
        sampleStims.SetVisibilityOnOffStates(GetStateFromName("DisplaySample"), GetStateFromName("DisplayState"));
        sampleStims.SetLocations(CurrentTrialDef.TargetSampleLocations);
        TrialStims.Add(sampleStims);
        
        targetStims = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.TargetIndices);
        targetStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        targetStims.SetLocations(CurrentTrialDef.TargetSearchLocations);
        foreach (WorkingMemory_StimDef sd in targetStims.stimDefs)
            sd.IsTarget = true;
        TrialStims.Add(targetStims);
        
        distractorStims1 = new StimGroup("DistractorStims1", ExternalStims, CurrentTrialDef.DistractorIndices1);
        distractorStims1.SetVisibilityOnOffStates(GetStateFromName("DisplayDistractors1"), GetStateFromName("DisplayDistractors1"));
        distractorStims1.SetLocations(CurrentTrialDef.DistractorLocations1);
        TrialStims.Add(distractorStims1);
        
        distractorStims2 = new StimGroup("DistractorStims2", ExternalStims, CurrentTrialDef.DistractorIndices1);
        distractorStims2.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        distractorStims2.SetLocations(CurrentTrialDef.DistractorLocations2);
        TrialStims.Add(distractorStims2);
    }
    
}
