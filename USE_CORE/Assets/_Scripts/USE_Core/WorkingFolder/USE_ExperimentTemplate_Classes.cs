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
}
