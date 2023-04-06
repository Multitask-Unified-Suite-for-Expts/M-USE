using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using FeatureUncertaintyWM_Namespace;

public class FeatureUncertaintyWM_TrialLevel : ControlLevel_Trial_Template
{
    private GameObject taskCanvas;
    
    public FeatureUncertaintyWM_TrialDef CurrentTrialDef => GetCurrentTrialDef<FeatureUncertaintyWM_TrialDef>();

    public override void DefineControlLevel()
    {
        Add_ControlLevel_InitializationMethod(() =>
            {
                //load stimuli from file used for component stims
                //set them inactive
                taskCanvas = GameObject.Find("FeatureUncertaintyWM_Canvas");
            }
        );
        
        //somewhere in trial, call
        // GameObject mcPanel = GenerateMultiCompStim(int[] objsPerCircle, GameObject[] componentObjectTypes,
        //     float[] objProportions);
        
        //during FinishTrial/ITI?
        //mcPanel.Destroy();


    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
    }


    private GameObject GenerateMultiCompStim(int[] objsPerCircle, GameObject[] componentObjectTypes, float[] objProportions)
    {
        //objsPerCircle = 1 array element per circle used to compose multicompstim, # indicates number of smaller stim on that circle
        //objNames = strings representing the files (preloaded textures?) used for each of the distinct sub stimulus types
        //objProportions = floats representing the proportion of total objects that will be composed of each sub stimulus type

        
        //error checking
        if (componentObjectTypes.Length != objProportions.Length)
        {
            Debug.LogError("MultiComponent Stimulus Generation failed due to different # of elemnts in ObjNames and ObjProportions");
        }
        if (objProportions.Sum() != 1) // normalize proportions
            for (int iObj = 0; iObj < objProportions.Length; iObj++)
            {
                objProportions[iObj] = objProportions[iObj] / objProportions.Sum();
            }
        
        int totalObjectCount = objsPerCircle.Sum();
        
        //define each circle - locations specified as x/y proportions of panel
        //obtain appropriate # of equally-spaced coordinates on each circle
        //see shotgunraycast stuff below - assign compLocations values
    
        GameObject multiCompPanel = new GameObject("multiCompPanel");
        multiCompPanel.AddComponent<CanvasRenderer>();
        multiCompPanel.GetComponent<RectTransform>().SetParent(taskCanvas.GetComponent<RectTransform>());
        //assign appropriate location and size to multiCompPanel

        // List<GameObject> componentObjectInstances = new List<GameObject>();
        Vector3[] compLocations = new Vector3[totalObjectCount];
        int totalObjCounter = 0;
        
        //get actual # of each object type
        int[] numObjsOfEachType = new int[componentObjectTypes.Length];

        for (int iObjType = 0; iObjType < componentObjectTypes.Length; iObjType++)
        {
            for (int iObj = 1; iObj < numObjsOfEachType[iObjType]; iObjType++)
            {
                    
                GameObject g = Instantiate(componentObjectTypes[iObjType]);
                g.GetComponent<RectTransform>().SetParent(multiCompPanel.GetComponent<RectTransform>());
                g.GetComponent<RectTransform>().anchoredPosition = compLocations[iObjType];
                // componentObjectInstances.Add(g);
            }
        }

        return multiCompPanel;

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


