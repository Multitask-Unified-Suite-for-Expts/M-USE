using ConfigDynamicUI;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using USE_ExperimentTemplate_Trial;
using USE_Settings;
using USE_States;
using USE_StimulusManagement;
using WorkingMemory_Namespace;

public class WorkingMemory_TrialLevel : ControlLevel_Trial_Template
{
    public WorkingMemory_TrialDef CurrentTrialDef => GetCurrentTrialDef<WorkingMemory_TrialDef>();

    private StimGroup sampleStims, targetStims, postSampleDistractorStims, targetDistractorStims;
    public string MaterialFilePath;
    private GameObject startButton;
    private bool variablesLoaded;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display

    //configui variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, finalFbDuration, fbDuration, maxObjectTouchDuration, selectObjectDuration;
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
            //RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\Blank.png");
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + CurrentTrialDef.ContextName + ".png");
            TokenFBController
            .SetRevealTime(CurrentTrialDef.tokenRevealDuration)
            .SetUpdateTime(CurrentTrialDef.tokenUpdateDuration);

            startButton.SetActive(true);
            TokenFBController.enabled = false;
        });
        initTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(startButton),
            displaySample, () => {
                startButton.SetActive(false);
                TokenFBController.enabled = true;
                RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + CurrentTrialDef.ContextName + ".png");
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]); //CHECK THIS TIMING MIGHT BE OFF
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarReset"]);
            });
        
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
        searchDisplay.AddTimer(() => selectObjectDuration.value, FinishTrial);

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
            }
            else
            {
                HaloFBController.ShowNegative(selected);
            };
        });
        selectionFeedback.AddTimer(() => fbDuration.value, tokenFeedback, () => 
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
        });


        // The state that will handle the token feedback and wait for any animations
        tokenFeedback.AddInitializationMethod(() =>
        {
            HaloFBController.Destroy();
            if (selectedSD.TokenUpdate == 0)
            {
                if (correct) AudioFBController.Play("Positive");
                else AudioFBController.Play("Negative");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                return;
            }
            if (selectedSD.TokenUpdate > 0)
            {
                TokenFBController.AddTokens(selected, selectedSD.TokenUpdate);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
            }
            else
            {
                TokenFBController.RemoveTokens(selected, -selectedSD.TokenUpdate);
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

    public void loadVariables()
    {
        Texture2D buttonTex = LoadPNG(MaterialFilePath + Path.DirectorySeparatorChar + "StartButtonImage.png");
        startButton = CreateStartButton(buttonTex, new Rect(new Vector2(0, 0), new Vector2(1, 1)));

        playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");

        variablesLoaded = true;
    }
    private GameObject CreateStartButton(Texture2D tex, Rect rect)
    {
        Vector3 buttonPosition = Vector3.zero;
        Vector3 buttonScale = Vector3.zero;
        string TaskName = "WorkingMemory";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
        }
        else
        {
            Debug.Log("[ERROR] TaskDef is not in config folder");
        }

        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer sr = startButton.AddComponent<SpriteRenderer>() as SpriteRenderer;
        sr.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        startButton.AddComponent<BoxCollider>();
        startButton.transform.localScale = buttonScale;
        startButton.transform.position = buttonPosition;
        return startButton;
    }
}
