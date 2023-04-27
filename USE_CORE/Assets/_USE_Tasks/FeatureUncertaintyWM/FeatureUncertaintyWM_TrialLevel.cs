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
    
    
    private StimGroup multiCompStims;
    private StimGroup subCompStim;

    private List<List<GameObject>> mcComponentGameObjs;
    private GameObject mcCompHolder;
    private int[] mcCompStimIndices;






    public override void DefineControlLevel()
    {
        Add_ControlLevel_InitializationMethod(() =>
	        {
		        mcCompHolder = new GameObject();
		        FeatureUncertaintyWM_BlockDef bDef = TaskLevel.GetCurrentBlockDef<FeatureUncertaintyWM_BlockDef>();
		        //load stimuli from file used for component stims
		        //set them inactive
		        taskCanvas = GameObject.Find("FeatureUncertaintyWM_Canvas");

		        mcCompStimIndices = bDef.blockMcCompStimIndices;

		        int maxCompObjs = 20;
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
		        
		        
		        //instantiate that # of all component objects
		        //set all to inactive
		        //n

	        }
        );
        
        //############################
        //at end of state where MC objs appear, reset component objects parent to top level of hierarchy and set inactive
        


    }

    private GameObject GenerateMultiCompStim(FeatureUncertaintyWM_MultiCompStimDef sd)
    {
        //this has got to be replaced by a working version of the stuff commented out below, it returns a single 
        //multicomponent object (which is composed of multiple component objects)

        //but instead of looping through componentObjectTypes, we pass in componentObjIndices
        //this is just a vector which is easy to store at the trial level
        //same indices as the sample stims

        //probably to save processing, at the beginning of a block you might want to instantiate all the component objects used in that block
        //each of them as many times as the max times it is used in any trial... then set them all inactive, and set active in this method as needed

        GameObject multiCompPanel = new GameObject("multiCompPanel");
        multiCompPanel.AddComponent<CanvasRenderer>();
        multiCompPanel.GetComponent<RectTransform>().SetParent(taskCanvas.GetComponent<RectTransform>());

       subCompStim = new StimGroup("TargetStim", GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));

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
        
        IEnumerable<int> cumulativeComp = numCompOnCircles
            .Select((n, i) => numCompOnCircles
            .Take(i + 1)
            .Aggregate(0, (sum, x) => sum + x));
        IEnumerable<int> startCompInds = new[] { 1 }.Concat(cumulativeComp);


        counter = 0;
        for (int iComp = 0; iComp < sd.compObjIndices.Length; iComp++)
        {
            for (int iSubComp = startCompInds.ElementAt(iComp); iSubComp < startCompInds.ElementAt(iComp+1); iSubComp++)
            {

                allCompObjIndices[permutedCompInds[counter]] = sd.compObjIndices[iComp];
                counter++;
            }

        }

        for (int iComp = 0; iComp < sd.compObjIndices.Length; iComp++)
        {
	        int[] thisCompIndices = new int[1];
	        //populate thisCompIndices with every index of sd.CompObjIndices[iComp] in allCompObjIndices
	        for (int iGO = 0; iGO < thisCompIndices.Length; iGO++)
	        {
		        GameObject compGO = mcComponentGameObjs[iComp][thisCompIndices[iGO]];
		        compGO.transform.position = compLocations[thisCompIndices[iGO]];
		        //set parent to canvas
		        compGO.SetActive(true);
	        }
        }

        return multiCompPanel;
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

        multiCompStims = new StimGroup("MultiCompStims"); // can add state control of onset/offset
        // multiCompStims.SetLocations(CurrentTrialDef.multiCompStimLocations);
       
        for (int iStim = 0; iStim < CurrentTrialDef.numMcStim; iStim++)
        {
            FeatureUncertaintyWM_MultiCompStimDef sd = new FeatureUncertaintyWM_MultiCompStimDef(); // populate with appropriate values
            sd.compObjIndices = CurrentTrialDef.mcCompObjIndices[iStim];
            //sd.whatever = CurrentTrialDef.whatever;
            //do with all other stimdef fields
            
            multiCompStims.AddStims(GenerateMultiCompStim(sd)); //make a new stim group, add it
            sd.AssignStimDefPointeToObjectHierarchy(sd.StimGameObject, sd);
        }
        
        TrialStims.Add(multiCompStims);

        // // searchStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("SelectionFeedback")); MAKING QUADDLES TWITCH BETWEEN STATES
        // //   distractorStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("SelectionFeedback"));
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


