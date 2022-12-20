using System;
using ConfigDynamicUI;
using System.Collections.Generic;
using System.IO;
using EffortControl_Namespace;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_Settings;
using USE_States;
using USE_StimulusManagement;
using WorkingMemory_Namespace;

public class WorkingMemory_TrialLevel : ControlLevel_Trial_Template
{
    public WorkingMemory_TrialDef CurrentTrialDef => GetCurrentTrialDef<WorkingMemory_TrialDef>();

    private StimGroup searchStims, targetStim, postSampleDistractorStims; // targetDistractorStims, sampleStims;
    public string MaterialFilePath;
    private GameObject startButton;
    private bool variablesLoaded;
    public Vector3 buttonPosition, buttonScale;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display

    //block end variables
    public List<int> runningAcc;
    public int MinTrials, MaxTrials;
    
    //configui variables
    [HideInInspector]
    public ConfigNumber tokenRevealDuration, tokenUpdateDuration, trialEndDuration, initTrialDuration, baselineDuration, 
        maxSearchDuration, selectionFbDuration, displaySampleDuration, postSampleDelayDuration, 
        displayPostSampleDistractorsDuration, preTargetDelayDuration, itiDuration;
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
        SetupTrial.AddInitializationMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }          
        });

        SetupTrial.SpecifyTermination(() => true, initTrial);

        // Show blue start button and wait for click
        MouseTracker.AddSelectionHandler(mouseHandler, initTrial);
        initTrial.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + CurrentTrialDef.ContextName + ".png");
            TokenFBController
            .SetRevealTime(tokenRevealDuration.value)
            .SetUpdateTime(tokenUpdateDuration.value);

            startButton.SetActive(true);
            TokenFBController.enabled = false;
        });
        initTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(startButton),
            displaySample, () => {
                startButton.SetActive(false);
                TokenFBController.enabled = true;
                RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + CurrentTrialDef.ContextName + ".png");
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]); //CHECK THIS TIMING MIGHT BE OFF
                //EventCodeManager.SendCodeNextFrame(TaskEventCodes["TargetStimOn"]); ADD THIS TO THE EVENT CODES
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarReset"]);
            });
        
        // Show nothing for some time
        initTrial.AddTimer(() => initTrialDuration.value, delay, () =>
          {
              startButton.SetActive(false);
              stateAfterDelay = displaySample;
              delayDuration = baselineDuration.value;
          });
        
        // displaySample.AddInitializationMethod(() =>
        // {
        //     for (int iTarg = 0; iTarg < targetStim.stimDefs.Count; iTarg++)
        //     {
        //         targetStim.stimDefs[iTarg].StimLocation = CurrentTrialDef.TargetSampleLocation[iTarg];
        //         targetStim.stimDefs[iTarg].ToggleVisibility(true);
        //     }
        // });
        
        // Show the target/sample by itself for some time
        displaySample.AddTimer(() => displaySampleDuration.value, delay, () =>
          {
              stateAfterDelay = displayPostSampleDistractors;
              delayDuration = postSampleDelayDuration.value;
          });

        // Show some distractors without the target/sample
        displayPostSampleDistractors.AddTimer(() => displayPostSampleDistractorsDuration.value, delay, () =>
          {
              stateAfterDelay = searchDisplay;
              delayDuration = preTargetDelayDuration.value;
              
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
        searchDisplay.AddTimer(() => maxSearchDuration.value, FinishTrial);

        selectionFeedback.AddInitializationMethod(() =>
        {
            if (!selected) return;
            else
            {//CHECK THIS
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
            }
            if (correct)
            {
                HaloFBController.ShowPositive(selected);
                runningAcc.Add(1);
            }
            else
            {
                HaloFBController.ShowNegative(selected);
                runningAcc.Add(0);
            };
        });
        selectionFeedback.AddTimer(() => selectionFbDuration.value, tokenFeedback, () => 
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
        });


        // The state that will handle the token feedback and wait for any animations
        tokenFeedback.AddInitializationMethod(() =>
        {
            HaloFBController.Destroy();
            if (selectedSD.StimTrialRewardMag == 0)
            {
                if (correct) AudioFBController.Play("Positive");
                else AudioFBController.Play("Negative");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                return;
            }
            if (selectedSD.StimTrialRewardMag > 0)
            {
                TokenFBController.AddTokens(selected, selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
            }
            else
            {
                TokenFBController.RemoveTokens(selected, -selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
            }
        });
        tokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), trialEnd);

        // Wait for some time at the end
        trialEnd.AddTimer(() => itiDuration.value, FinishTrial);

        TrialData.AddDatum("SelectedName", () => selected != null ? selected.name : null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("SelectionCorrect", () => correct ? 1 : 0);
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish

        searchStims = new StimGroup("SearchStims", ExternalStims, CurrentTrialDef.SearchStimIndices);
        searchStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        searchStims.SetLocations(CurrentTrialDef.SearchStimLocations);

        List<StimDef> rewardedStimdefs = new List<StimDef>();

        targetStim = new StimGroup("TargetStim", GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        for (int iStim = 0; iStim < CurrentTrialDef.SearchStimIndices.Length; iStim++)
        {
            WorkingMemory_StimDef sd = (WorkingMemory_StimDef)searchStims.stimDefs[iStim];
            sd.StimTrialRewardMag = ChooseTokenReward(CurrentTrialDef.SearchStimTokenReward[iStim]);
            if (sd.StimTrialRewardMag > 0)
            {
                // StimDef tempsd = sd.CopyStimDef();
                WorkingMemory_StimDef newTarg = sd.CopyStimDef<WorkingMemory_StimDef>() as WorkingMemory_StimDef;
                targetStim.AddStims(newTarg);
                newTarg.IsTarget = true;//Holds true if the target stim receives non-zero reward
                // targetStim = new StimGroup("TargetStim", ExternalStims, new int[] {CurrentTrialDef.SearchStimIndices[iStim]});
                // targetStim.SetVisibilityOnOffStates(GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
                // targetStim.SetLocations(CurrentTrialDef.TargetSampleLocation);
            } 
            else sd.IsTarget = false;
        }
        
        // for (int iT)
        targetStim.SetLocations(CurrentTrialDef.TargetSampleLocation);
        targetStim.SetVisibilityOnOffStates(GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        TrialStims.Add(searchStims);
        TrialStims.Add(targetStim);

        postSampleDistractorStims = new StimGroup("PostSampleDistractor", ExternalStims, CurrentTrialDef.PostSampleDistractorIndices);
        postSampleDistractorStims.SetVisibilityOnOffStates(GetStateFromName("DisplayPostSampleDistractors"), GetStateFromName("DisplayPostSampleDistractors"));
        postSampleDistractorStims.SetLocations(CurrentTrialDef.PostSampleDistractorLocations);
        TrialStims.Add(postSampleDistractorStims);
    }
    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
            CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, MinTrials,
            TrialDefs.Count);
    }

    public void loadVariables()
    {
        Texture2D buttonTex = LoadPNG(MaterialFilePath + Path.DirectorySeparatorChar + "StartButtonImage.png");
        startButton = CreateStartButton(buttonTex, new Rect(new Vector2(0, 0), new Vector2(1, 1)));

        playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
        //config UI variables
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration"); 
        trialEndDuration = ConfigUiVariables.get<ConfigNumber>("trialEndDuration"); 
        initTrialDuration = ConfigUiVariables.get<ConfigNumber>("initTrialDuration");
        baselineDuration = ConfigUiVariables.get<ConfigNumber>("baselineDuration"); 
        maxSearchDuration = ConfigUiVariables.get<ConfigNumber>("maxSearchDuration");
        selectionFbDuration = ConfigUiVariables.get<ConfigNumber>("selectionFbDuration");
        displaySampleDuration = ConfigUiVariables.get<ConfigNumber>("displaySampleDuration");
        postSampleDelayDuration = ConfigUiVariables.get<ConfigNumber>("postSampleDelayDuration");
        displayPostSampleDistractorsDuration = ConfigUiVariables.get<ConfigNumber>("displayPostSampleDistractorsDuration");
        preTargetDelayDuration = ConfigUiVariables.get<ConfigNumber>("preTargetDelayDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        
        variablesLoaded = true;
    }
    private GameObject CreateStartButton(Texture2D tex, Rect rect)
    {
        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer sr = startButton.AddComponent<SpriteRenderer>() as SpriteRenderer;
        sr.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        startButton.AddComponent<BoxCollider>();
        startButton.transform.localScale = buttonScale;
        startButton.transform.position = buttonPosition;
        return startButton;
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
}
