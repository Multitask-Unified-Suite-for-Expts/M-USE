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
using System.Collections.Generic;
using System.Linq;
using USE_States;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using FeatureUncertaintyWM_Namespace;
using USE_UI;
using UnityEngine.UI;
using ConfigDynamicUI;


public class FeatureUncertaintyWM_TrialLevel : ControlLevel_Trial_Template
{
    private GameObject taskCanvas;
    public USE_StartButton USE_FBSquare;
    public FeatureUncertaintyWM_TrialDef CurrentTrialDef => GetCurrentTrialDef<FeatureUncertaintyWM_TrialDef>();
    public FeatureUncertaintyWM_TaskLevel CurrentTaskLevel => GetTaskLevel<FeatureUncertaintyWM_TaskLevel>();
    public FeatureUncertaintyWM_TaskDef CurrentTask => GetTaskDef<FeatureUncertaintyWM_TaskDef>();

    // Each trial in the block should have two variables that only one is selected to build the multicomp stim. A certain prop and objectType, and uncertain prop and objectType
    // whether a trial uses uncertain or certain varibles then is defined by the running accuracy criterion and the min/max number of trials defined

    // Block Uncertainty and End Variables
    public List<int> runningAcc;
    public int MinTrials, MaxTrials;
    public int MinUncertainTrials, MaxUncertainTrials;
    
    
    // MultiComponent Variables
    private List<List<List<GameObject>>> allMcComponentGameObjs;
    private List<List<GameObject>> mcComponentGameObjs;
    private GameObject mcCompHolder;
    private int[] mcCompStimIndices;


    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    FeatureUncertaintyWM_MultiCompStimDef selectedSD = null;

    // Stimuli Variables
    private StimGroup multiCompStims, sampleStims;
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
        taskCanvas = GameObject.Find("FeatureUncertaintyWM_Canvas");
        allMcComponentGameObjs = new  List<List<List<GameObject>>>();
        

        Add_ControlLevel_InitializationMethod(() =>
	        {
		        mcCompHolder = new GameObject();
                mcCompHolder.transform.localPosition = new Vector3(0, 0, 0);

                FeatureUncertaintyWM_BlockDef bDef = TaskLevel.GetCurrentBlockDef<FeatureUncertaintyWM_BlockDef>();
		        //load stimuli from file used for component stims
		        //set them inactive
		        

		        mcCompStimIndices = bDef.blockMcCompStimIndices;

		        int maxCompObjs = bDef.maxComp;
                //this should be obtained by looping through all trials in TrialDefs and finding max # component stims.
                for (int iStim = 0; iStim < bDef.numMcStim; iStim++)
                {
                    mcComponentGameObjs = new List<List<GameObject>>();
                    for (int iMC = 0; iMC < mcCompStimIndices.Length; iMC++)
                    {
                        int compStimIndex = mcCompStimIndices[iMC];
                        List<GameObject> compStimCopies = new List<GameObject>();
                        for (int iComp = 0; iComp < maxCompObjs; iComp++)
                        {
                            GameObject compGO = new GameObject();//give it name
                            compGO.transform.parent = mcCompHolder.transform;
                            RawImage compGOImage = compGO.AddComponent<RawImage>();
                            string stimPath = ExternalStims.stimDefs[compStimIndex].FileName;
                            compGOImage.texture = LoadExternalPNG(stimPath);
                            compGOImage.rectTransform.localScale = new Vector3(0.05f, 0.05f, 0.05f); //add the var in the config later                      
                            compGO.GetComponent<RawImage>().raycastTarget = false;
                            compStimCopies.Add(compGO);
                            compStimCopies[iComp].SetActive(false);
                            

                        }
                        mcComponentGameObjs.Add(compStimCopies);
                    }
                    allMcComponentGameObjs.Add(mcComponentGameObjs);
                }
                //LoadTextures(ContextExternalFilePath); //done at USE_TrialLevel, no longer needed here
                // Initialize FB Controller Values
                HaloFBController.SetHaloSize(5f);
                HaloFBController.SetHaloIntensity(5);


                //instantiate that # of all component objects
                //set all to inactive
                //n
            }
        );

        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            //Set the Stimuli Light/Shadow settings
            SetShadowType(ShadowType, "FeatureUncertaintyWM_DirectionalLight");
            if (StimFacingCamera)
                MakeStimFaceCamera();

            if (StartButton == null)
            {
                StartButton = Session.USE_StartButton.CreateStartButton(taskCanvas.GetComponent<Canvas>(), StartButtonPosition, StartButtonScale);
                Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                // USE_StartButton.SetButtonColor(color: Color.black);
            }

            DeactivateChildren(taskCanvas);

            if (!configUIVariablesLoaded) LoadConfigUIVariables();
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, SearchDisplay);
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, StartButtonScale, taskCanvas, true);

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            if (Session.SessionDef.MacMainDisplayBuild & !Application.isEditor && !AdjustedPositionsForMac) //adj text positions if running build with mac as main display
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

        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatches(Session.USE_StartButton.StartButtonChildren), DisplaySample, () =>
        {
            //Set the token bar settings
            TokenFBController.enabled = true;
            TokenFBController
                .SetRevealTime(tokenRevealDuration.value)
                .SetUpdateTime(tokenUpdateDuration.value)
                .SetFlashingTime(tokenFlashingDuration.value);

            CurrentTaskLevel.SetBlockSummaryString();
            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();

            sampleStims.stimDefs[0].StimGameObject.transform.localPosition = CurrentTrialDef.sampleCompLocations[0];
            sampleStims.stimDefs[0].StimGameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            sampleStims.stimDefs[0].StimGameObject.transform.localScale = new Vector3(1, 1, 1);
            sampleStims.stimDefs[0].StimGameObject.GetComponent<RawImage>().raycastTarget = false;
        });

        // Show the target/sample by itself for some time
        DisplaySample.AddTimer(() => displaySampleDuration.value, Delay, () =>
        {
            StateAfterDelay = SearchDisplay;
            DelayDuration = postSampleDelayDuration.value;

        });


        // Show the target/sample with some other distractors
        // Wait for a click and provide feedback accordingly
        SearchDisplay.AddSpecificInitializationMethod(() =>
        {
            CreateTextOnExperimenterDisplay();
            multiCompStims.ToggleVisibility(true);

            Session.EventCodeManager.SendCodeNextFrame("TokenBarVisible");
            
            choiceMade = false;
            // Handler.HandlerActive = true;
            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
            
            foreach(StimDef sd in multiCompStims.stimDefs)
                sd.StimGameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
        });

        SearchDisplay.AddUpdateMethod(() =>
        {
            if (Handler.SuccessfulSelections.Count > 0)
            {
                selectedGO = Handler.LastSuccessfulSelection.SelectedGameObject.GetComponent<StimDefPointer>().StimDef.StimGameObject;
                Debug.Log("selected stim" + selectedGO);
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
                Session.EventCodeManager.SendCodeNextFrame("CorrectResponse");
            }
            else
            {
                NumErrors_InBlock++;
                CurrentTaskLevel.NumErrors_InTask++;
                Session.EventCodeManager.SendCodeNextFrame("IncorrectResponse");
                
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
            Session.EventCodeManager.SendCodeNextFrame("NoChoice");
            Session.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
            AbortCode = 6;

            aborted = true;
            NumAborted_InBlock++;
            CurrentTaskLevel.NumAborted_InTask++;
        });

        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            SearchDuration = SearchDisplay.TimingInfo.Duration;
            SearchDurations_InBlock.Add(SearchDuration);
            CurrentTaskLevel.SearchDurations_InTask.Add(SearchDuration);
            SetTrialSummaryString();

            int? depth = Session.Using2DStim ? 10 : (int?)null;


            if (CorrectSelection)
            {
                HaloFBController.ShowPositive(selectedGO, depth);
                // HaloFBController.PositiveHaloPrefab.transform.SetParent(null);
                // // Vector3 goWorldPos = TaskLevel.TaskCam.WorldToScreenPoint(selectedGO.transform.position);
                // HaloFBController.PositiveHaloPrefab.transform.position = selectedGO.transform.position;
            }
            else
            {
                HaloFBController.ShowNegative(selectedGO, depth);
                // HaloFBController.NegativeHaloPrefab.transform.SetParent(null);
                // // Vector3 goWorldPos = TaskLevel.TaskCam.WorldToScreenPoint(selectedGO.transform.position);
                // HaloFBController.NegativeHaloPrefab.transform.position = selectedGO.transform.position;
            }
        });

        SelectionFeedback.AddTimer(() => selectionFbDuration.value, TokenFeedback, () =>
        {
            HaloFBController.Destroy();
        });


        // The state that will handle the token feedback and wait for any animations
        TokenFeedback.AddSpecificInitializationMethod(() =>
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
            if (TokenFBController.IsTokenBarFull())
            {
                NumTokenBarFull_InBlock++;
                CurrentTaskLevel.NumTokenBarFull_InTask++;
                if (Session.SyncBoxController != null)
                {
                    Session.SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                   // SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses", CurrentTrialDef.NumPulses)); moved to syncbox class
                    NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                    CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
                    RewardGiven = true;
                    TokenFBController.ResetTokenBarFull();
                }
            }
        });

        ITI.AddSpecificInitializationMethod(() =>
        {
            if (NeutralITI)
            {
                ContextName = "itiImage";
                StartCoroutine(HandleSkybox(GetContextNestedFilePath(ContextExternalFilePath, ContextName)));
                Session.EventCodeManager.SendCodeNextFrame("ContextOff");
            }

            //Setting back the parent to the mcCompHolder for individual components
            FeatureUncertaintyWM_BlockDef bDef = TaskLevel.GetCurrentBlockDef<FeatureUncertaintyWM_BlockDef>();
            int maxCompObjs = bDef.maxComp;
            mcCompHolder = new GameObject();
            for (int iStim = 0; iStim < bDef.numMcStim; iStim++)
            {
                for (int iMC = 0; iMC < mcCompStimIndices.Length; iMC++)
                {
                    for (int iComp = 0; iComp < maxCompObjs; iComp++)
                    {
                        
                        GameObject compGO = allMcComponentGameObjs[iStim][iMC][iComp];//give it name
                        compGO.SetActive(true);
                        
                        compGO.transform.localPosition = new Vector3(0, 0, 0);
                        //compGO.transform.parent = mcCompHolder.transform;
                        compGO.transform.SetParent(mcCompHolder.transform, false);

                        compGO.SetActive(false);

                    }
                }
                Debug.Log("mcCompPosition:" + mcCompHolder.transform.position);
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
        sampleStims.ToggleVisibility(false);
        if (AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"])
        {
            aborted = true;
            NumAborted_InBlock++;
            CurrentTaskLevel.NumAborted_InTask++;
            CurrentTaskLevel.CurrentBlockSummaryString.Clear();
            CurrentTaskLevel.CurrentBlockSummaryString.AppendLine("");
        }

        TokenFBController.ResetTokenBarFull();
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
         mcCompPanel.AddComponent<RectTransform>();
        mcCompPanel.GetComponent<RectTransform>().SetParent(TaskLevel.StimCanvas_2D.GetComponent<RectTransform>());
        mcCompPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(5, 5);
        //mcCompPanel.transform.SetParent(taskCanvas.transform, true);

        mcCompPanel.transform.localPosition = new Vector3(0, 0, 0);
        mcCompPanel.transform.localRotation = Quaternion.Euler(0, 0, 0);
        mcCompPanel.transform.localScale = new Vector3(20, 20, 20);
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

        int[] compInds = Enumerable.Range(0, sd.totalObjectCount).ToArray();
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
        // Debug.Log(compLocations.Length);
        // Debug.Log(compInds.Max());
        // Debug.Log(permutedCompInds.Max());
        counter = 0;
        for (int iComp = 0; iComp < sd.compObjIndices.Length; iComp++)
        {
	        int[] thisCompIndices = new int[1];
	        //populate thisCompIndices with every index of sd.CompObjIndices[iComp] in allCompObjIndices
	        for (int iGO = 0; iGO < sd.compObjNumber[iComp]; iGO++)
	        {
		        GameObject compGO = allMcComponentGameObjs[sd.mcStimInd][sd.compObjIndices[iComp]][iGO];
                //compGO.transform.parent = mcCompPanel.transform;
                compGO.transform.SetParent(mcCompPanel.transform, false);

                compGO.transform.localPosition = compLocations[permutedCompInds[counter]];
                compGO.transform.localRotation = Quaternion.Euler(0, 0, 0);
                // Debug.Log("Comp Locations are:" + compLocations[permutedCompInds[counter]]);
                //set parent to canvas
                compGO.SetActive(true);
                counter++;
            }
        }
        
        // sd.StimGameObject = mcCompPanel;
        //mcCompPanel.AddComponent<Rigidbody2D>();
        // mcCompPanel.GetComponent<Rigidbody2D>().isKinematic = false;
        //mcCompPanel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        Image imageComponent = mcCompPanel.AddComponent<Image>();
        imageComponent.color = Color.clear;
        // To make an image transparent, we set its color to clear
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

        multiCompStims = new StimGroup("MultiCompStims", GetStateFromName("SearchDisplay"), GetStateFromName("SelectionFeedback")); // can add state control of onset/offset

        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        sampleStims = new StimGroup("SampleStims", group, CurrentTrialDef.sampleCompIndices);
        sampleStims.SetVisibilityOnOffStates(GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        // sampleComp = new StimGroup("SampleComp", ExternalStims, CurrentTrialDef.sampleCompIndices);

        // multiCompStims.SetLocations(CurrentTrialDef.multiCompStimLocations);
        //for (int iStim = 0; iStim < CurrentTrialDef.sampleCompIndices.Length; iStim++)
        //{
        //    FeatureUncertaintyWM_StimDef sdSample = (FeatureUncertaintyWM_StimDef) sampleComp.stimDefs[iStim];
        //    sampleComp.AddStims(sdSample);
           
        //    //Debug.Log("Active Sample?" + sampleComp.IsActive);
        //    Debug.Log("SampleComp : " + sampleComp.stimDefs[iStim].FileName);
        //}

        for (int iStim = 0; iStim < CurrentTrialDef.numMcStim; iStim++)
        {
            FeatureUncertaintyWM_MultiCompStimDef sd = new FeatureUncertaintyWM_MultiCompStimDef(); // populate with appropriate values
            Debug.Log("istim is:" + iStim);
            sd.compObjIndices = CurrentTrialDef.mcCompObjIndices[iStim];
            sd.compObjNumber = CurrentTrialDef.mcCompObjNumber[iStim];
            sd.angleOffset = CurrentTrialDef.mcAngleOffset[iStim];
            sd.totalObjectCount = CurrentTrialDef.mcTotalObjectCount[iStim];
            sd.numCircles = CurrentTrialDef.mcNumCircles[iStim];
            sd.radius = CurrentTrialDef.mcRadius[iStim];
            sd.StimTrialRewardMag = chooseReward(CurrentTrialDef.mcStimTokenReward[iStim]);

            Debug.Log("reward" + CurrentTrialDef.mcStimTokenReward[iStim]);
            sd.mcStimInd = iStim;
            if (sd.StimTrialRewardMag > 0)
            {
                
                sd.IsTarget = true; //sets the isTarget value to true in the SearchStim Group
            }
            else sd.IsTarget = false;

            //sd.whatever = CurrentTrialDef.whatever;
            //do with all other stimdef fields
            sd.StimGameObject = GenerateMultiCompStim(sd);
            sd.StimName = "MC_Stim_" + (iStim + 1);
            sd.AssignStimDefPointerToObjectHierarchy(sd.StimGameObject, sd);
            multiCompStims.AddStims(sd); //make a new stim group, add it
            sd.AssignStimDefPointerToObjectHierarchy(multiCompStims.stimDefs[iStim].StimGameObject, sd);
            sd.StimGameObject.transform.localPosition = CurrentTrialDef.mcStimLocations[iStim];
            sd.StimGameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            sd.StimGameObject.SetActive(false);
            sd.StimLocation = CurrentTrialDef.mcStimLocations[iStim];
            
           //  Debug.Log(sd.StimGameObject);
           // // sd.AssignStimDefPointeToObjectHierarchy(multiCompStims.stimDefs[iStim].StimGameObject, sd);
           // // multiCompStims.stimDefs[iStim].AssignStimDefPointeToObjectHierarchy(multiCompStims.stimDefs[iStim].StimGameObject, multiCompStims.stimDefs[iStim]);
           //  Debug.Log(sd.StimGameObject.GetComponent<StimDefPointer>());
           //  Debug.Log("mcStimLocsd" + sd.StimLocation);
           //  Debug.Log("mcstimdef" + multiCompStims.stimDefs[iStim].StimGameObject.GetComponent<StimDefPointer>());
            
        }

        // multiCompStims.SetLocations(CurrentTrialDef.mcStimLocations);
        TrialStims.Add(multiCompStims);
        TrialStims.Add(sampleStims);
        sampleStims.SetLocations(CurrentTrialDef.sampleCompLocations);

        //sampleComp.SetVisibilityOnOffStates(GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
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
        FrameData.AddDatum("SampleStimVisibility", () => sampleStims.IsActive);
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
            for (int iMc = 0; iMc<multiCompStims.stimDefs.Count; iMc++)
            {
                FeatureUncertaintyWM_MultiCompStimDef sd = multiCompStims.stimDefs[iMc] as FeatureUncertaintyWM_MultiCompStimDef;
//                FeatureUncertaintyWM_MultiCompStimDef sd = (FeatureUncertaintyWM_MultiCompStimDef)multiCompStims.stimDefs[iMc];
                Debug.Log("what is" + sd.IsTarget);
                Debug.Log(multiCompStims.stimDefs[iMc]);
                
                if (sd.IsTarget)
                {
                    textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(sd.StimLocation), playerViewParent);
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


