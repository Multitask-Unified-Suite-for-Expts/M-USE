using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_States;
using USE_StimulusManagement;
using VisualSearch_Namespace;

public class VisualSearch_TrialLevel : ControlLevel_Trial_Template
{
    public VisualSearch_TrialDef CurrentTrialDef => GetCurrentTrialDef<VisualSearch_TrialDef>();

    private StimGroup targetStims;
    private StimGroup distractorStims;
    
    public override void DefineControlLevel()
    {
        State initTrial = new State("InitTrial");
        State searchDisplay = new State("SearchDisplay");
        State selectionFeedback = new State("SelectionFeedback");
        State tokenFeedback = new State("TokenFeedback");
        State trialEnd = new State("TrialEnd");
        AddActiveStates(new List<State> {initTrial, searchDisplay, selectionFeedback, tokenFeedback, trialEnd});
        
        
        SetupTrial.SpecifyTermination(() => true, initTrial);
        
        initTrial.AddTimer(()=>CurrentTrialDef.initTrialDuration, searchDisplay);
        //adapt StartButton from whatwhenwhere task

        bool responseMade = false;
        searchDisplay.AddInitializationMethod(() => responseMade = false);
        //add update function where choice is made
        searchDisplay.SpecifyTermination(()=> responseMade, selectionFeedback);
        searchDisplay.AddTimer(()=>CurrentTrialDef.maxSearchDuration, FinishTrial);

        
        selectionFeedback.AddInitializationMethod(() => { });
        //adapt from ChoseWrong/Right in whatwhenwhere task
        selectionFeedback.AddTimer(()=> CurrentTrialDef.selectionFbDuration, tokenFeedback);
        
        
        bool tokenUpdated = false;
        tokenFeedback.AddInitializationMethod(() => tokenUpdated = false);
        //wait for Marcus to integrate token fb
        tokenFeedback.SpecifyTermination(() => true, trialEnd); //()=> tokenUpdated, tokenFeedback);

        trialEnd.AddTimer(()=>CurrentTrialDef.trialEndDuration, FinishTrial);
        
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        targetStims = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.TargetIndices);
        targetStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        targetStims.SetLocations(CurrentTrialDef.TargetLocations);
        foreach (VisualSearch_StimDef sd in targetStims.stimDefs)
            sd.IsTarget = true;
        TrialStims.Add(targetStims);
        
        
        distractorStims = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.DistractorIndices);
        distractorStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        distractorStims.SetLocations(CurrentTrialDef.DistractorLocations);
        foreach (VisualSearch_StimDef sd in distractorStims.stimDefs)
            sd.IsTarget = false;
        TrialStims.Add(distractorStims);
    }
    
}
