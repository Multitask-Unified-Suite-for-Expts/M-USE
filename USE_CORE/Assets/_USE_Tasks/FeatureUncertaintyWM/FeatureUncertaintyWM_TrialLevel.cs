using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using FeatureUncertaintyWM_Namespace;
using System;
using Newtonsoft.Json.Linq;
using System.Reflection;
using USE_UI;
using UnityEngine.UI;
using ConfigDynamicUI;
using WorkingMemory_Namespace;

public class FeatureUncertaintyWM_TrialLevel : ControlLevel_Trial_Template
{
    private GameObject taskCanvas;
    public USE_StartButton USE_StartButton;
    public USE_StartButton USE_FBSquare;
    public FeatureUncertaintyWM_TrialDef CurrentTrialDef => GetCurrentTrialDef<FeatureUncertaintyWM_TrialDef>();
    public FeatureUncertaintyWM_TaskLevel CurrentTaskLevel => GetTaskLevel<FeatureUncertaintyWM_TaskLevel>();

    // Each trial in the block should have two variables that only one is selected to build the multicomp stim. A certain prop and objectType, and uncertain prop and objectType
    // whether a trial uses uncertain or certain varibles then is defined by the running accuracy criterion and the min/max number of trials defined

    // Block Uncertainty and End Variables
    public List<int> runningAcc;
    public int MinTrials, MaxTrials;
    public int MinUncertainTrials, MaxUncertainTrials;
    
    
    // MultiComponent Variables
    private List<List<GameObject>> mcComponentGameObjs;
    private GameObject mcCompHolder;
    private int[] mcCompStimIndices;


    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    FeatureUncertaintyWM_MultiCompStimDef selectedSD = null;

    // Stimuli Variables
    private StimGroup multiCompStims, sampleComp;
    private GameObject StartButton;

    // Config Loading Variables
    private bool configUIVariablesLoaded = false;
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, gratingSquareDuration, tokenRevealDuration, tokenUpdateDuration, tokenFlashingDuration, selectObjectDuration, selectionFbDuration, displaySampleDuration, postSampleDelayDuration,
          itiDuration;
    private float tokenFbDuration;

    public string ContextExternalFilePath;
    public Vector3 StartButtonPosition;
    public float StartButtonScale;
    public bool StimFacingCamera;
    public string ShadowType;
    public bool NeutralITI;

    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList = new List<GameObject>();
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;

    // Block Data Variables
    public string ContextName = "";
    public int NumCorrect_InBlock;
    public List<float> SearchDurations_InBlock = new List<float>();
    public int NumErrors_InBlock;
    public int NumRewardPulses_InBlock;
    public int NumTokenBarFull_InBlock;
    public int TotalTokensCollected_InBlock;
    public float Accuracy_InBlock;
    public float AverageSearchDuration_InBlock;
    public int NumAborted_InBlock;

    // Trial Data Variables
    private int? SelectedStimIndex = null;
    private string selectedStimName = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool RewardGiven = false;
    private bool TouchDurationError = false;
    private bool aborted = false;
    private float? selectionDuration = null;
    private bool choiceMade = false;

    [HideInInspector] public float TouchFeedbackDuration;

    [HideInInspector] public bool MacMainDisplayBuild;
    [HideInInspector] public bool AdjustedPositionsForMac;




    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State DisplaySample = new State("DisplaySample");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");

        AddActiveStates(new List<State> { InitTrial, DisplaySample, SearchDisplay, SelectionFeedback, TokenFeedback, ITI });


        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject();


        Add_ControlLevel_InitializationMethod(() =>
	        {
		        mcCompHolder = new GameObject();
		        FeatureUncertaintyWM_BlockDef bDef = TaskLevel.GetCurrentBlockDef<FeatureUncertaintyWM_BlockDef>();
		        //load stimuli from file used for component stims
		        //set them inactive
		        taskCanvas = GameObject.Find("FeatureUncertaintyWM_Canvas");

		        mcCompStimIndices = bDef.blockMcCompStimIndices;

		        int maxCompObjs = bDef.maxComp;
		        //this should be obtained by looping through all trials in TrialDefs and finding max # component stims.
		        for (int iMC = 0; iMC < bDef.numMcStim; iMC++)
		        {
			        int compStimIndex = mcCompStimIndices[iMC];
			        List<GameObject> compStimCopies = new List<GameObject>();
			        for (int iComp = 0; iComp < maxCompObjs; iComp++)
			        {
				        GameObject compGO = new GameObject();//give it name
				        compGO.transform.parent = mcCompHolder.transform;

				        RawImage compGOImage = compGO.AddComponent<RawImage>();

				        string stimPath = ExternalStims.stimDefs[compStimIndex].StimPath;
				        compGOImage.texture = LoadPNG(stimPath);
				        compStimCopies[iComp] = compGO;
				        compStimCopies[iComp].SetActive(false);
			        }
			        mcComponentGameObjs.Add(compStimCopies);
		        }
                LoadTextures(ContextExternalFilePath);
                // Initialize FB Controller Values
                HaloFBController.SetHaloSize(5f);
                HaloFBController.SetHaloIntensity(5);


                //instantiate that # of all component objects
                //set all to inactive
                //n

            }
        );

        SetupTrial.AddInitializationMethod(() =>
        {
            //Set the Stimuli Light/Shadow settings
            SetShadowType(ShadowType, "WorkingMemory_DirectionalLight");
            if (StimFacingCamera)
                MakeStimFaceCamera();

            if (StartButton == null)
            {
                USE_StartButton = new USE_StartButton(taskCanvas.GetComponent<Canvas>(), StartButtonPosition, StartButtonScale);
                StartButton = USE_StartButton.StartButtonGO;
                USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
            }

            DeactivateChildren(taskCanvas);

            if (!configUIVariablesLoaded) LoadConfigUIVariables();
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
            TokenFBController.ResetTokenBarFull();
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", MouseTracker, InitTrial, SearchDisplay);
        TouchFBController.EnableTouchFeedback(Handler, TouchFeedbackDuration, StartButtonScale, taskCanvas);

        InitTrial.AddInitializationMethod(() =>
        {
            if (MacMainDisplayBuild & !Application.isEditor && !AdjustedPositionsForMac) //adj text positions if running build with mac as main display
            {
                Vector3 biggerScale = TokenFBController.transform.localScale * 2f;
                TokenFBController.transform.localScale = biggerScale;
                TokenFBController.tokenSize = 200;
                TokenFBController.RecalculateTokenBox();
                AdjustedPositionsForMac = true;
            }

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();

            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;
        });

        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatches(StartButton), DisplaySample, () =>
        {
            //Set the token bar settings
            TokenFBController.enabled = true;
            TokenFBController
                .SetRevealTime(tokenRevealDuration.value)
                .SetUpdateTime(tokenUpdateDuration.value)
                .SetFlashingTime(tokenFlashingDuration.value);
            EventCodeManager.SendCodeImmediate(SessionEventCodes["StartButtonSelected"]);

            CurrentTaskLevel.SetBlockSummaryString();
            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();
        });

        // Show the target/sample by itself for some time
        DisplaySample.AddTimer(() => displaySampleDuration.value, Delay, () =>
        {
            StateAfterDelay = SearchDisplay;
            DelayDuration = postSampleDelayDuration.value;
        });


        // Show the target/sample with some other distractors
        // Wait for a click and provide feedback accordingly
        SearchDisplay.AddInitializationMethod(() =>
        {
            CreateTextOnExperimenterDisplay();
            multiCompStims.ToggleVisibility(true);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["TokenBarVisible"]);
            choiceMade = false;

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
        });

        SearchDisplay.AddUpdateMethod(() =>
        {
            if (Handler.SuccessfulSelections.Count > 0)
            {
                selectedGO = Handler.LastSuccessfulSelection.SelectedGameObject;
                selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<FeatureUncertaintyWM_MultiCompStimDef>();
                Handler.ClearSelections();
                if (selectedSD != null)
                    choiceMade = true;
            }
        });

        SearchDisplay.SpecifyTermination(() => choiceMade, SelectionFeedback, () =>
        {
            CorrectSelection = selectedSD.IsTarget;

            if (CorrectSelection)
            {
                NumCorrect_InBlock++;
                CurrentTaskLevel.NumCorrect_InTask++;
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["Button0PressedOnTargetObject"]);//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["CorrectResponse"]);
            }
            else
            {
                NumErrors_InBlock++;
                CurrentTaskLevel.NumErrors_InTask++;
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["Button0PressedOnDistractorObject"]);//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["IncorrectResponse"]);
            }

            if (selectedGO != null)
            {
                SelectedStimIndex = selectedSD.StimIndex;
                SelectedStimLocation = selectedSD.StimLocation;
            }
            Accuracy_InBlock = NumCorrect_InBlock / (TrialCount_InBlock + 1);
            SetTrialSummaryString();
        });


        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            //means the player got timed out and didn't click on anything

            aborted = true;
            NumAborted_InBlock++;
            CurrentTaskLevel.NumAborted_InTask++;
            AbortCode = 6;
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["NoChoice"]);

        });

        SelectionFeedback.AddInitializationMethod(() =>
        {
            SearchDuration = SearchDisplay.TimingInfo.Duration;
            SearchDurations_InBlock.Add(SearchDuration);
            CurrentTaskLevel.SearchDurations_InTask.Add(SearchDuration);
            SetTrialSummaryString();

            if (CorrectSelection)
                HaloFBController.ShowPositive(selectedGO);
            else
                HaloFBController.ShowNegative(selectedGO);
        });

        SelectionFeedback.AddTimer(() => selectionFbDuration.value, TokenFeedback, () =>
        {
            HaloFBController.Destroy();
        });


        // The state that will handle the token feedback and wait for any animations
        TokenFeedback.AddInitializationMethod(() =>
        {
            if (GameObject.Find("MainCameraCopy").transform.childCount != 0)
                DestroyChildren(GameObject.Find("MainCameraCopy"));
            multiCompStims.ToggleVisibility(false);
            if (selectedSD.IsTarget)
            {
                TokenFBController.AddTokens(selectedGO, selectedSD.StimTrialRewardMag);
                TotalTokensCollected_InBlock += selectedSD.StimTrialRewardMag;
                CurrentTaskLevel.TotalTokensCollected_InTask += selectedSD.StimTrialRewardMag;
            }
            else
            {
                TokenFBController.RemoveTokens(selectedGO, -selectedSD.StimTrialRewardMag);
                TotalTokensCollected_InBlock -= selectedSD.StimTrialRewardMag;
                CurrentTaskLevel.TotalTokensCollected_InTask -= selectedSD.StimTrialRewardMag;
            }
        });

        TokenFeedback.AddTimer(() => tokenFbDuration, ITI, () =>
        {
            if (TokenFBController.isTokenBarFull())
            {
                NumTokenBarFull_InBlock++;
                CurrentTaskLevel.NumTokenBarFull_InTask++;
                if (SyncBoxController != null)
                {
                    SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                    SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses", CurrentTrialDef.NumPulses));
                    NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                    CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
                    RewardGiven = true;
                }
            }
        });

        ITI.AddInitializationMethod(() =>
        {
            if (NeutralITI)
            {
                ContextName = "itiImage";
                RenderSettings.skybox = CreateSkybox(GetContextNestedFilePath(ContextExternalFilePath, ContextName), true);
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOff"]);
            }

            //Setting back the parent to the mcCompHolder for individual components
            FeatureUncertaintyWM_BlockDef bDef = TaskLevel.GetCurrentBlockDef<FeatureUncertaintyWM_BlockDef>();
            int maxCompObjs = bDef.maxComp;
            for (int iMC = 0; iMC < bDef.numMcStim; iMC++)
            {
                for (int iComp = 0; iComp < maxCompObjs; iComp++)
                {

                    GameObject compGO = mcComponentGameObjs[iComp][iMC];//give it name
                    compGO.transform.parent = mcCompHolder.transform;
                    compGO.SetActive(false);

                }
            }

        });

        // Wait for some time at the end
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        AssignFrameData();
        AssignTrialData();
    }
    public void MakeStimFaceCamera()
    {
        foreach (StimGroup group in TrialStims)
            foreach (var stim in group.stimDefs)
            {
                stim.StimGameObject.transform.LookAt(Camera.main.transform);
            }
    }
    public override void FinishTrialCleanup()
    {
        // Remove the Stimuli, Context, and Token Bar from the Player View and move to neutral ITI State
        if (GameObject.Find("MainCameraCopy").transform.childCount != 0)
            DestroyChildren(GameObject.Find("MainCameraCopy"));
        TokenFBController.enabled = false;
        multiCompStims.ToggleVisibility(false);
        sampleComp.ToggleVisibility(false);
        if (AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"])
        {
            aborted = true;
            NumAborted_InBlock++;
            CurrentTaskLevel.NumAborted_InTask++;
            CurrentTaskLevel.BlockSummaryString.Clear();
            CurrentTaskLevel.BlockSummaryString.AppendLine("");
        }
    }

    public void ResetBlockVariables()
    {
        SearchDurations_InBlock.Clear();
        AverageSearchDuration_InBlock = 0;
        NumErrors_InBlock = 0;
        NumCorrect_InBlock = 0;
        NumRewardPulses_InBlock = 0;
        NumTokenBarFull_InBlock = 0;
        Accuracy_InBlock = 0;
        TotalTokensCollected_InBlock = 0;
        NumAborted_InBlock = 0;
    }




    //############################
    //at end of state where MC objs appear, reset component objects parent to top level of hierarchy and set inactive





private GameObject GenerateMultiCompStim(FeatureUncertaintyWM_MultiCompStimDef sd)
    {
        //this has got to be replaced by a working version of the stuff commented out below, it returns a single 
        //multicomponent object (which is composed of multiple component objects)

        //but instead of looping through componentObjectTypes, we pass in componentObjIndices
        //this is just a vector which is easy to store at the trial level
        //same indices as the sample stims

        //probably to save processing, at the beginning of a block you might want to instantiate all the component objects used in that block
        //each of them as many times as the max times it is used in any trial... then set them all inactive, and set active in this method as needed

        GameObject mcCompPanel = new GameObject("multiCompPanel");
        mcCompPanel.AddComponent<CanvasRenderer>();
        mcCompPanel.GetComponent<RectTransform>().SetParent(taskCanvas.GetComponent<RectTransform>());


        // Getting total number of components, number of component for each object index, number of circles,  radius and angle offset of of circles
        // from the stimDef and assign a location and an object index for each component

        Vector3[] compLocations = new Vector3[sd.totalObjectCount];
        int[] allCompObjIndices = new int[sd.totalObjectCount];
        int[] numCompOnCircles = new int[sd.numCircles];

        numCompOnCircles = sd.radius.Select(x => Mathf.RoundToInt(sd.totalObjectCount * x / sd.radius.Sum())).ToArray();
        int remainngComp = sd.totalObjectCount - numCompOnCircles.Sum();

        // Add or subtract the remaining objects to the last circle
        if (remainngComp != 0)
        {
            numCompOnCircles[sd.numCircles - 1] += remainngComp;
        }


        // Initialize the coordinates of object locations on all circles

        int counter = 0;
        for (int j = 0; j < sd.numCircles; j++)
        {
            for (int i = 0; i < numCompOnCircles[j]; i++)
            {
                float angle = 2 * Mathf.PI * i / numCompOnCircles[j];
                float x = sd.angleOffset[j] + sd.radius[j] * Mathf.Cos(angle);
                float y = sd.angleOffset[j] + sd.radius[j] * Mathf.Sin(angle);
                compLocations[counter] = new Vector3(x, y, 0);
                counter++;
            }
        }

        // Assign random location for each component

        int[] compInds = Enumerable.Range(1, sd.totalObjectCount).ToArray();
        System.Random random = new System.Random();
        int[] permutedCompInds = compInds.OrderBy(x => random.Next()).ToArray();
        
        //IEnumerable<int> cumulativeComp = sd.compObjNumber
        //    .Select((n, i) => sd.compObjNumber
        //    .Take(i + 1)
        //    .Aggregate(0, (sum, x) => sum + x));
        //IEnumerable<int> startCompInds = new[] { 1 }.Concat(cumulativeComp);


        //counter = 0;
        //for (int iComp = 0; iComp < sd.compObjIndices.Length; iComp++)
        //{
        //    for (int iSubComp = startCompInds.ElementAt(iComp); iSubComp < startCompInds.ElementAt(iComp+1); iSubComp++)
        //    {

        //        allCompObjIndices[permutedCompInds[counter]] = sd.compObjIndices[iComp];
        //        counter++;
        //    }

        //}

        for (int iComp = 0; iComp < sd.compObjIndices.Length; iComp++)
        {
	        int[] thisCompIndices = new int[1];
	        //populate thisCompIndices with every index of sd.CompObjIndices[iComp] in allCompObjIndices
	        for (int iGO = 0; iGO < sd.compObjNumber[iComp]; iGO++)
	        {
		        GameObject compGO = mcComponentGameObjs[iComp][iGO];
                compGO.transform.parent = mcCompPanel.transform;
                compGO.transform.position = compLocations[permutedCompInds[counter]];
                //set parent to canvas
                compGO.SetActive(true);
	        }
        }

        return mcCompPanel;
        //return new GameObject(); // this line is just here so I don't have to comment out stuff below... the function returns the multiccomp object
    }

    //private GameObject GenerateMultiCompStim(FeatureUncertaintyWM_StimDef sd)// int[] objsPerCircle, GameObject[] componentObjectTypes, float[] objProportions)
    //{
    //    //objsPerCircle = 1 array element per circle used to compose multicompstim, # indicates number of smaller stim on that circle
    //    //objNames = strings representing the files(preloaded textures ?) used for each of the distinct sub stimulus types
    //    //objProportions = floats representing the proportion of total objects that will be composed of each sub stimulus type


    //     //error checking
    //     if (componentObjectTypes.Length != objProportions.Length)
    //        {
    //            Debug.LogError("MultiComponent Stimulus Generation failed due to different # of elemnts in ObjNames and ObjProportions");
    //        }
    //    if (objProportions.Sum() != 1) // normalize proportions
    //        for (int iObj = 0; iObj < objProportions.Length; iObj++)
    //        {
    //            objProportions[iObj] = objProportions[iObj] / objProportions.Sum();
    //        }

    //    int totalObjectCount = objsPerCircle.Sum();

    //    //define each circle - locations specified as x/y proportions of panel
    //    //obtain appropriate # of equally-spaced coordinates on each circle
    //    //see shotgunraycast stuff below - assign compLocations values

    //    GameObject multiCompPanel = new GameObject("multiCompPanel");
    //    multiCompPanel.AddComponent<CanvasRenderer>();
    //    multiCompPanel.GetComponent<RectTransform>().SetParent(taskCanvas.GetComponent<RectTransform>());
    //    //assign appropriate location and size to multiCompPanel

    //    // List<GameObject> componentObjectInstances = new List<GameObject>();
    //    //

    //    // not sure whether it is a correct way of implementing that???
    //    GameObject[] multiCompPanel1 = new GameObject[numProbedStim];

    //    for (int i = 0; i < numProbedStim; i++)
    //    {
    //        multiCompPanel1[i].AddComponent<CanvasRenderer>();
    //        multiCompPanel1[i].GetComponent<RectTransform>().SetParent(taskCanvas.GetComponent<RectTransform>());
    //        multiCompPanel1[i].GetComponent<RectTransform>().anchoredPosition = probedStimPosition[i];
    //        multiCompPanel1[i].GetComponent<RectTransform>().sizeDelta = probedStimSize[i];

    //    }



    //    //defining each circle and distributing total objecect count on the circles evenly based on their radius
    //    // Variables to be taken: 'totalObjectCount' 'angelOffset', 'radius', 'numCircles', 'compScale'
    //    //Currently it is written in a way that unity estimates the number of objects per circle in a uniformely distributed fashion based on the circle radius
    //    //which one is better? predefined objpercircle or the later?

    //    Vector3[] compLocations = new Vector3[totalObjectCount];
    //    Vector3 compScale = new Vector3(1f, 1f, 1f);
    //    int numCircles = 3;
    //    float[] radius = new float[totalObjectCount];
    //    float[] angelOffset = new float[totalObjectCount];
    //    int[] numCompOnCircles = new int[numCircles];

    //    numCompOnCircles = radius.Select(x => Mathf.RoundToInt(totalObjectCount * x / radius.Sum())).ToArray();
    //    int remainngComp = totalObjectCount - numCompOnCircles.Sum();

    //    // Add or subtract the remaining objects to the last circle
    //    if (remainngComp != 0)
    //    {
    //        numCompOnCircles[numCircles - 1] += remainngComp;
    //    }

    //    // Initialize the coordinates of object locations on all circles

    //    int counter = 0;
    //    for (int j = 0; j < numCircles; j++)
    //    {
    //        for (int i = 0; i < numCompOnCircles[j]; i++)
    //        {
    //            float angle = 2 * Mathf.PI * i / numCompOnCircles[j];
    //            float x = angelOffset[j] + radius[j] * Mathf.Cos(angle);
    //            float y = angelOffset[j] + radius[j] * Mathf.Sin(angle);
    //            compLocations[counter] = new Vector3(x, y, 0);
    //            counter++;
    //        }
    //    }


    //    int totalObjCounter = 0;

    //    //get actual # of each object type
    //    int[] numObjsOfEachType = new int[componentObjectTypes.Length];

    //    for (int iObjType = 0; iObjType < componentObjectTypes.Length; iObjType++)
    //    {
    //        for (int iObj = 1; iObj < numObjsOfEachType[iObjType]; iObj++)
    //        {

    //            GameObject g = Instantiate(componentObjectTypes[iObjType]);
    //            g.GetComponent<RectTransform>().SetParent(multiCompPanel.GetComponent<RectTransform>());
    //            g.GetComponent<RectTransform>().anchoredPosition = compLocations[iObjType];
    //            g.GetComponent<RectTransform>().localScale = compScale;
    //            // componentObjectInstances.Add(g);
    //        }
    //    }

    //    return multiCompPanel;

    //}

    protected override void DefineTrialStims()
    {
        //do all the Sample stuff up here


        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        //StimGroup constructor which creates a subset of an already-existing StimGroup 

        multiCompStims = new StimGroup("MultiCompStims", GetStateFromName("SearchDisplay"), GetStateFromName("SearchDisplay")); // can add state control of onset/offset
        sampleComp = new StimGroup("SampleComp", ExternalStims, CurrentTrialDef.sampleCompIndices);

        // multiCompStims.SetLocations(CurrentTrialDef.multiCompStimLocations);
        for (int iStim = 0; iStim < CurrentTrialDef.sampleCompIndices.Length; iStim++)
        {
            FeatureUncertaintyWM_StimDef sdSample = (FeatureUncertaintyWM_StimDef) sampleComp.stimDefs[iStim];
            sampleComp.AddStims(sdSample);
        }


        for (int iStim = 0; iStim < CurrentTrialDef.numMcStim; iStim++)
        {
            FeatureUncertaintyWM_MultiCompStimDef sd = new FeatureUncertaintyWM_MultiCompStimDef(); // populate with appropriate values
            sd.compObjIndices = CurrentTrialDef.mcCompObjIndices[iStim];
            sd.compObjNumber = CurrentTrialDef.mcCompObjNumber[iStim];
            sd.angleOffset = CurrentTrialDef.mcAngleOffset[iStim];
            sd.totalObjectCount = CurrentTrialDef.mcTotalObjectCount[iStim];
            sd.numCircles = CurrentTrialDef.mcNumCircles[iStim];
            sd.radius = CurrentTrialDef.mcRadius[iStim];
            sd.StimTrialRewardMag = chooseReward(CurrentTrialDef.mcStimTokenReward[iStim]);
            if (sd.StimTrialRewardMag > 0)
            {

                sd.IsTarget = true; //sets the isTarget value to true in the SearchStim Group
            }
            else sd.IsTarget = false;

            //sd.whatever = CurrentTrialDef.whatever;
            //do with all other stimdef fields

            multiCompStims.AddStims(GenerateMultiCompStim(sd)); //make a new stim group, add it
            sd.AssignStimDefPointeToObjectHierarchy(sd.StimGameObject, sd);
        }

        multiCompStims.SetLocations(CurrentTrialDef.mcStimLocations);
        TrialStims.Add(multiCompStims);
        TrialStims.Add(sampleComp);
        sampleComp.SetLocations(CurrentTrialDef.sampleCompLocations);
        sampleComp.SetVisibilityOnOffStates(GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        
        // // searchStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("SelectionFeedback")); MAKING QUADDLES TWITCH BETWEEN STATES
        // //   distractorStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("SelectionFeedback"));
    }


    public void LoadConfigUIVariables()
    {
        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration"); 
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        selectionFbDuration = ConfigUiVariables.get<ConfigNumber>("selectionFbDuration");
        displaySampleDuration = ConfigUiVariables.get<ConfigNumber>("displaySampleDuration");
        postSampleDelayDuration = ConfigUiVariables.get<ConfigNumber>("postSampleDelayDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        gratingSquareDuration = ConfigUiVariables.get<ConfigNumber>("gratingSquareDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
        tokenFlashingDuration = ConfigUiVariables.get<ConfigNumber>("tokenFlashingDuration");

        tokenFbDuration = (tokenFlashingDuration.value + tokenUpdateDuration.value + tokenRevealDuration.value);//ensures full flashing duration within
        ////configured token fb duration
        configUIVariablesLoaded = true;
    }
    public override void ResetTrialVariables()
    {
        SelectedStimIndex = null;
        SelectedStimLocation = null;
        SearchDuration = 0;
        CorrectSelection = false;
        RewardGiven = false;
        TouchDurationError = false;
        aborted = false;
        choiceMade = false;

        selectedGO = null;
        selectedSD = null;
    }
    private void AssignTrialData()
    {
        // All AddDatum commands for the Trial Data
        TrialData.AddDatum("Context", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("SelectedStimCode", () => selectedSD?.StimCode ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("CorrectSelection", () => CorrectSelection ? 1 : 0);
        TrialData.AddDatum("SearchDuration", () => SearchDuration);
        TrialData.AddDatum("RewardGiven", () => RewardGiven);
    }
    private void AssignFrameData()
    {
        // All AddDatum commmands from the Frame Data
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("StartButtonVisibility", () => StartButton.activeSelf);
        FrameData.AddDatum("SearchStimVisibility", () => multiCompStims.IsActive);
        FrameData.AddDatum("SampleStimVisibility", () => sampleComp.IsActive);
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Code: " + SelectedStimIndex +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\n\nCorrect Selection: " + CorrectSelection +
                             "\nTouch Duration Error: " + TouchDurationError +
                             "\n" +
                             "\nSearch Duration: " + SearchDuration +
                             "\n" +
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue();
    }
    private void CreateTextOnExperimenterDisplay()
    {
        playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
        if (!playerViewLoaded)
        {
            //Create corresponding text on player view of experimenter display
            foreach (FeatureUncertaintyWM_MultiCompStimDef stim in multiCompStims.stimDefs)
            {
                if (stim.IsTarget)
                {
                    textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation), playerViewParent);
                    textLocation.y += 50;
                    Vector2 textSize = new Vector2(200, 200);
                    playerViewText = playerView.CreateTextObject("TargetText", "TARGET",Color.red, textLocation, textSize, playerViewParent);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewTextList.Add(playerViewText);
                    playerViewLoaded = true;
                }
            }
        }
    }




    ///from shotgunraycast:
    /*
    //Determine appropriate number of circles and increase in radius between them (in worldspace units, both at the screen and distance rayLength from it)
    int numCircles = (int)Mathf.Ceil(radWorld[1] / raycastSpacingDVA);
    //		print (numCircles);
    float[] radStepSize = new float[2] { radWorld[0] / numCircles, radWorld[1] / numCircles };


            //iterate from the smallest circle to the largest
            for (int i = 0; i < numCircles; i++)
            {


                //determine appropriate number of rays to place around this circle,
                //arc distance between them (in worldspace units, both at the screen and distance rayLength from it),
                //and angle between them
                //rad[0] is radius of current circle
                int numRays = (int)Mathf.Ceil((2 * Mathf.PI * rad[1]) / raycastSpacingDVA);
                float[] rayStepSize = new float[2] { (2 * Mathf.PI * rad[0]) / numRays}; 
                float angleStepSize = rayStepSize[0] / rad[0];

                //jitter the starting point of raycasts on each circle so that they are as un-aligned as possible
                float angleJitter = UnityEngine.Random.Range(0f, angleStepSize);

                //iterate around the circle
                for (int j = 0; j < numRays; j++)
                {

                    float angle = angleStepSize * j + angleJitter;

                    //find start and end points of current ray - see https://stackoverflow.com/questions/27714014/3d-point-on-circumference-of-a-circle-with-a-center-radius-and-normal-vector
                    Vector3 startPoint = new Vector3(centres[0].x + rad[0] * (orthonormals[0].x * Mathf.Cos(angle) + orthonormals[1].x * Mathf.Sin(angle)),
                        centres[0].y + rad[0] * (orthonormals[0].y * Mathf.Cos(angle) + orthonormals[1].y * Mathf.Sin(angle)),
                        centres[0].z + rad[0] * (orthonormals[0].z * Mathf.Cos(angle) + orthonormals[1].z * Mathf.Sin(angle)));

                        ///this is a very complicated calculation of location  - can be simplified using standard 2d geometry
                }
            }
            */

}

