using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_States;
using USE_StimulusManagement;
using WorkingMemory_Namespace;

public class WorkingMemory_TrialLevel : ControlLevel_Trial_Template
{
    public WorkingMemory_TrialDef CurrentTrialDef => GetCurrentTrialDef<WorkingMemory_TrialDef>();

    private StimGroup sampleStims, targetStims, postSampleDistractorStims, targetDistractorStims;

    public GameObject StartButton;

    public override void DefineControlLevel()
    {
        State initTrial = new State("InitTrial");
        State delay = new State("Delay");
        State displaySample = new State("DisplaySample");
        State displayPostSampleDistractors = new State("DisplayPostSampleDistractors");
        State searchDisplay = new State("SearchDisplay");
        State selectionFeedback = new State("SelectionFeedback");
        State tokenFeedback = new State("TokenFeedback");
        State trialEnd = new State("TrialEnd");

        SelectionHandler<WorkingMemory_StimDef> mouseHandler = new SelectionHandler<WorkingMemory_StimDef>();

        AddActiveStates(new List<State> { initTrial, delay, displaySample, displayPostSampleDistractors, searchDisplay, selectionFeedback, tokenFeedback, trialEnd });

        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        delay.AddTimer(() => delayDuration, () => stateAfterDelay);

        // Show blue start button and wait for click
        MouseTracker.AddSelectionHandler(mouseHandler, SetupTrial);
        SetupTrial.AddInitializationMethod(() =>
        {
            StartButton.SetActive(true);
        });
        SetupTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton),
            initTrial, () => StartButton.SetActive(false));

        // Show nothing for some time
        initTrial.AddTimer(() => CurrentTrialDef.initTrialDuration, delay, () =>
          {
              stateAfterDelay = displaySample;
              delayDuration = CurrentTrialDef.baselineDuration;
          });

        // Show the target/sample by itself for some time
        displaySample.AddTimer(() => CurrentTrialDef.displaySampleDuration, delay, () =>
          {
              stateAfterDelay = displayPostSampleDistractors;
              delayDuration = CurrentTrialDef.postSampleDelayDuration;
          });

        // Show some distractors without the target/sample
        displayPostSampleDistractors.AddTimer(() => CurrentTrialDef.displayPostSampleDistractorsDuration, delay, () =>
          {
              stateAfterDelay = searchDisplay;
              delayDuration = CurrentTrialDef.preTargetDelayDuration;
          });

        // Show the target/sample with some other distractors
        // Wait for a click and provide feedback accordingly
        bool correct = false;
        GameObject selected = null;
        WorkingMemory_StimDef selectedSD = null;
        MouseTracker.AddSelectionHandler(mouseHandler, searchDisplay);
        searchDisplay.AddInitializationMethod(() => selected = null);
        searchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, selectionFeedback, () => {
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            correct = selectedSD.IsTarget;
        });
        searchDisplay.AddTimer(() => CurrentTrialDef.maxSearchDuration, FinishTrial);

        selectionFeedback.AddInitializationMethod(() =>
        {
            if (!selected) return;
            if (correct) HaloFBController.ShowPositive(selected);
            else HaloFBController.ShowNegative(selected);
        });
        selectionFeedback.AddTimer(() => CurrentTrialDef.selectionFbDuration, tokenFeedback);

        // The state that will handle the token feedback and wait for any animations
        tokenFeedback.AddInitializationMethod(() =>
        {
            HaloFBController.Destroy();
            if (selectedSD.TokenUpdate == 0)
            {
                if (correct) AudioFBController.Play("Positive");
                else AudioFBController.Play("Negative");
                return;
            }
            if (selectedSD.TokenUpdate > 0)
            {
                TokenFBController.AddTokens(selected, selectedSD.TokenUpdate);
            }
            else
            {
                TokenFBController.RemoveTokens(selected, -selectedSD.TokenUpdate);
            }
        });
        tokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), trialEnd);

        // Wait for some time at the end
        trialEnd.AddTimer(() => CurrentTrialDef.trialEndDuration, FinishTrial);

        TrialData.AddDatum("SelectedName", () => selected != null ? selected.name : null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("SelectionCorrect", () => correct ? 1 : 0);
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish


        sampleStims = new StimGroup("SampleStims", ExternalStims, CurrentTrialDef.TargetIndices);
        sampleStims.SetVisibilityOnOffStates(GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        sampleStims.SetLocations(CurrentTrialDef.TargetSampleLocations);
        TrialStims.Add(sampleStims);

        targetStims = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.TargetIndices);
        targetStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        targetStims.SetLocations(CurrentTrialDef.TargetSearchLocations);
        int i = 0;
        foreach (WorkingMemory_StimDef sd in targetStims.stimDefs) {
            sd.IsTarget = true;
            sd.TokenUpdate = CurrentTrialDef.TargetTokenUpdates[i];
            ++i;
        }
        TrialStims.Add(targetStims);

        postSampleDistractorStims = new StimGroup("PostSampleDistractor", ExternalStims, CurrentTrialDef.PostSampleDistractorIndices);
        postSampleDistractorStims.SetVisibilityOnOffStates(GetStateFromName("DisplayPostSampleDistractors"), GetStateFromName("DisplayPostSampleDistractors"));
        postSampleDistractorStims.SetLocations(CurrentTrialDef.PostSampleDistractorLocations);
        TrialStims.Add(postSampleDistractorStims);

        targetDistractorStims = new StimGroup("PreTargetDistractor", ExternalStims, CurrentTrialDef.TargetDistractorIndices);
        targetDistractorStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        targetDistractorStims.SetLocations(CurrentTrialDef.TargetDistractorLocations);
        i = 0;
        foreach (WorkingMemory_StimDef sd in targetDistractorStims.stimDefs)
        {
            sd.TokenUpdate = CurrentTrialDef.DistractorTokenUpdates[i];
            ++i;
        }
        TrialStims.Add(targetDistractorStims);
    }
}
