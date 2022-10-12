using System;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_StimulusManagement;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace ConReg_Namespace
{
    public class ConReg_TaskDef : TaskDef
    {
        public List<int> chosenStims;
    }

    public class ConReg_BlockDef : BlockDef
    { 
        public int[] BlockStimIndices, nObjectsMinMax, Ratio;

        //variables for the data from the Block Config file. 
        public float DisplayStimsDuration, ChooseStimDuration, TrialEndDuration, TouchFeedbackDuration,
            DisplayResultDuration,TokenRevealDuration, TokenUpdateDuration;
        public int row, col, ManuallySpecifyLocation;

        public List<int> PreviouslyChosenStim, PreviouslyNotChosenStimuli, TrialStimIndices, UnseenStims, NewStim;
        public Vector3[] BlockStimLocations, StimLocation;
        public int trialCount, PC_count, PNC_count, new_count;
        public Vector3 ContextColor;
        public string ContextName;

        //takes a block and generates corresponding trials. Gets called in the USE Template!
        public override void GenerateTrialDefsFromBlockDef()
        {
            int numGrid = row * col;
            Debug.Log("num grid is " + numGrid + "row is " + row + "; col is " + col);
            Vector3[] Locations = new Vector3[numGrid];
            
            // calculate horizontal and vertical offset
            float horizontal = 12f/(col*2.1f);
            float vertical = 7.7f /(row*2.1f);
            int gridIndex = 0;
            // edges
            float x = -3;
            float y = 2;
            float z = 0;
            
            // create grid by filling in location array
            for (int i = 0; i < row; i++)
            {
                x = -3;
                for (int j = 0; j < col; j++)
                {
                    Locations[gridIndex] = new Vector3(x, y, z);
                    x += horizontal;
                    gridIndex++;
                }
                y -= vertical;
            }

            // if user want to specify their own stim location, use user location, other wise, use grid
            if (ManuallySpecifyLocation == 1)
            {
                if (StimLocation.Length < nObjectsMinMax[1])
                {
                    Debug.Log("Did not specify enough locations!");
                    Debug.Break();
                }
                else
                {
                    BlockStimLocations = StimLocation;
                    Debug.Log("BlockStimLocations lengths is " + BlockStimLocations.Length);
                }
            }
            else
            {
                BlockStimLocations = Locations;
            }

            // init some lists
            PreviouslyChosenStim = new List<int>();
            PreviouslyNotChosenStimuli = new List<int>();
            UnseenStims = new List<int>();
            TrialStimIndices = new List<int>();
            NewStim = new List<int>();
            
            // calculate total number of trials
            int numTrials = nObjectsMinMax[1] - nObjectsMinMax[0] + 1;
            TrialDefs = new ConReg_TrialDef[numTrials];
            
            // number of stims on first trial (we increase by 1 after each iteration below)
            int numTrialStims = nObjectsMinMax[0];
            bool end = false;

            // trial loop 
            for (int iTrial = 0; iTrial < numTrials && !end; iTrial++)
            {
                ConReg_TrialDef trial = new ConReg_TrialDef();
                trial.BlockStimIndices = BlockStimIndices;
                trial.trialCount = trialCount;

                // set up stim location by randomly choosing positions from grid
                Vector3[] arr = new Vector3[nObjectsMinMax[0] + iTrial];
                for (int i = 0; i < numTrialStims; i++)
                {
                    int index = Random.Range(0, BlockStimLocations.Length);
                    while (Array.IndexOf(arr, BlockStimLocations[index]) != -1)
                    {
                        index = Random.Range(0, BlockStimLocations.Length);
                    }
                    arr[i] = BlockStimLocations[index];
                }
                
                trial.TrialStimLocations = arr;
                trial.Grid = Locations;
                trial.TrialStimIndices = TrialStimIndices;
                trial.PreviouslyChosenStim = PreviouslyChosenStim;
                trial.PreviouslyNotChosenStimuli = PreviouslyNotChosenStimuli;
                trial.nObjectsMinMax = nObjectsMinMax;
                trial.Ratio = Ratio;
                trial.UnseenStims = UnseenStims;
                trial.numTrialStims = numTrialStims;
                trial.maxNumTrials = numTrials;
                trial.DisplayStimsDuration = DisplayStimsDuration;
                trial.ChooseStimDuration = ChooseStimDuration;
                trial.TrialEndDuration = TrialEndDuration;
                trial.TouchFeedbackDuration = TouchFeedbackDuration;
                trial.DisplayResultDuration = DisplayResultDuration;
                trial.ManuallySpecifyLocation = ManuallySpecifyLocation;
                trial.row = row;
                trial.col = col;
                trial.PNC_count = PNC_count;
                trial.PC_count = PC_count;
                trial.new_Count = new_count;
                trial.ContextColor = ContextColor;
                trial.ContextName = ContextName;
                trial.TokenRevealDuration = TokenRevealDuration;
                trial.TokenUpdateDuration = TokenUpdateDuration;

                TrialDefs[iTrial] = trial;
                numTrialStims++;
                trialCount++;
            }
        }
    }

    public class ConReg_TrialDef : TrialDef
    {
        public int[] BlockStimIndices, nObjectsMinMax, Ratio, metrics;
        public Vector3[] TrialStimLocations;
        public int trialCount, numTrialStims, maxNumTrials;
        public bool isNewStim;
        public Vector3[] Grid;
        public Vector3 ContextColor;
        public string ContextName;

        public List<int> PreviouslyChosenStim, PreviouslyNotChosenStimuli, TrialStimIndices, UnseenStims;
        public int row, col, Context, PC_count, PNC_count, new_Count;

        public float
            DisplayStimsDuration,
            ChooseStimDuration,
            TrialEndDuration,
            TouchFeedbackDuration,
            DisplayResultDuration, TokenRevealDuration, TokenUpdateDuration;

        public int ManuallySpecifyLocation;
        
        //ObjectNums refers to items in a list of objects to be loaded from resources folder
        public int[] ObjectNums;
       
    }

    public class ConReg_StimDef : StimDef
    {
        public bool PreviouslyChosen;
    }
    
}