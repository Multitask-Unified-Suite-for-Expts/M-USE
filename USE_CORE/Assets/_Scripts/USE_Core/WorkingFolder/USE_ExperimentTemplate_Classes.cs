using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using UnityEngine;
using USE_ExperimentTemplate;

namespace USE_ExperimentTemplate_Classes 
{
    public class EventCode
    {
        public int? Value;
        public int[] Range;
        public string Description;

        public int GetRangedValue(int position)
        {
            int newVal = Range[0] + position - 1;
            if (newVal <= Range[1])
                Value = newVal;
            else
            {
                Value = Range[1];
                Debug.LogWarning("Attempted to provide value of " + position + " to event code but this exceeds the maximum range.");
            }

            return Value.Value;
        }
    }

    public class TaskLevelTemplate_Methods
    {
        public bool CheckBlockEnd(string blockEndType, IEnumerable<int> runningAcc, float accThreshold = 1, int windowSize = 1, int? minTrials = null, int? maxTrials = null)
        {
            //takes in accuracy information from the current block and determines if the block should end
            
            List<int> rAcc = (List<int>) runningAcc;
            float? immediateAvg; //this is the running average over the past n trials, where n = windowSize
            float? prevAvg; //this is the running average over the n trials PRIOR to the trials used for immediateAvg
                            //(allows easy comparison of changes between performance across two windows
            int? sumdif; //the simple sum of the number of different trial outcomes in the windows used to compute 
                            //immediateAvg and prevAvg


            if (rAcc.Count >= windowSize)
                immediateAvg = (float) rAcc.GetRange(rAcc.Count - windowSize + 1, windowSize).Average();
            else
                immediateAvg = null;

            if (rAcc.Count >= windowSize * 2)
            {
                prevAvg = (float) rAcc.GetRange(rAcc.Count - windowSize * 2 + 1, windowSize).Average();
                sumdif = rAcc.GetRange(rAcc.Count - windowSize * 2 + 1, windowSize).Sum() -
                         rAcc.GetRange(rAcc.Count - windowSize + 1, windowSize).Sum();
            }
            else
            {
                prevAvg = null;
                sumdif = null;
            }

            if (CheckTrialRange(rAcc.Count, minTrials, maxTrials) != null)
                return CheckTrialRange(rAcc.Count, minTrials, maxTrials).Value;

            switch (blockEndType)
            {
                case "SimpleThreshold":
                    if (immediateAvg > accThreshold)
                    {
                        Debug.Log("Block ending due to performance above threshold.");
                        return true;
                    }
                    else
                        return false;
                case "ThresholdAndPeak":
                    if (immediateAvg > accThreshold && immediateAvg <= prevAvg)
                    {
                        Debug.Log("Block ending due to performance above threshold and no continued improvement.");
                        return true;
                    }
                    else
                        return false;
                case "ThresholdOrAsymptote":
                    if (sumdif != null && sumdif.Value <= 1)
                    {
                        Debug.Log("Block ending due to asymptotic performance.");
                        return true;
                    }
                    else if (immediateAvg > accThreshold)
                    {
                        Debug.Log("Block ending due to performance above threshold.");
                        return true;
                    }
                    else
                        return false;
                default:
                    return false;
            }
        }

        private bool? CheckTrialRange(int nTrials, int? minTrials = null, int? maxTrials = null)
        {
            if (nTrials < minTrials)
                return false;
            if (nTrials >= maxTrials)
                return true;
            return null;
        }
    }

    public class TrialLevel_Methods
    {
        
    }
}
