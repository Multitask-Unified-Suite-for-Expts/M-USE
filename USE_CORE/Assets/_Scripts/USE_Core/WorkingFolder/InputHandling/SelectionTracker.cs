using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;
using USE_States;

namespace SelectionTracking
{
    public class SelectionTracker
    {
        // Start is called before the first frame update
        public Dictionary<string, SelectionHandler> ActiveSelectionHandlers;

        public SelectionTracker(bool useDefaultHandlers = true)
        {
            if (useDefaultHandlers)
                ActiveSelectionHandlers = AssignDefaultSelectionHandlers();
            else
            {
                Debug.LogError("A selection tracker is being initialized with UseDefaultHandlers set to false, " +
                               "but no alternative dictionary of trackers has been provided as an argument.");
            }
        }

        public SelectionTracker(Dictionary<string, List<SelectionHandler>> shs, bool useDefaultHandlers = true)
        {
            if (useDefaultHandlers)
            {
                ActiveSelectionHandlers = AssignDefaultSelectionHandlers();
                foreach (string key in shs.Keys)
                    ActiveSelectionHandlers.Add(key, shs[key]);
            }
            else
                ActiveSelectionHandlers = shs;

        }
        
        public void UpdateActiveSelections()
        {
            foreach (string key in ActiveSelectionHandlers.Keys)
            {
                ActiveSelectionHandlers[key].UpdateSelections();
            }
        }
        
        
        private Dictionary<string, SelectionHandler> AssignDefaultSelectionHandlers()
        {
            return new Dictionary<string, SelectionHandler>()
            {
//build in default selections
            };
        }

    }

    public abstract class Selection
    {
        public float? Duration, StartTime, EndTime;
        public int StartFrame, EndFrame;
        public GameObject SelectedGameObject;
        public bool WasSuccessful;

        public Selection(GameObject go)
        {
            SelectedGameObject = go;
            Duration = 0;
            StartFrame = Time.frameCount;
            StartTime = Time.time;
            CustomSelectionInit();
        }

        public void UpdateSelection()
        {
            Duration = Time.time - StartTime;
            CustomSelectionUpdate();
        }

        public void CompleteSelection(bool success = true)
        {
            EndTime = Time.time;
            Duration = EndTime - StartTime;
            WasSuccessful = success;
            CustomSelectionTermination();
        }


        public virtual void CustomSelectionInit()
        {
        }
        public virtual void CustomSelectionUpdate()
        {
        }
        public virtual void CustomSelectionTermination()
        {
        }
    }

    public abstract class SelectionHandler
    {
        public List<Selection> AllSelections, SuccessfulSelections, UnsuccessfulSelections;
        public Selection OngoingSelection;
        public float? minDuration, maxDuration;
        public int? maxPixelDisplacement;
        public BoolDelegate InitCondition, UpdateCondition, TerminationCondition;

        public void UpdateSelections()
        {
            if (OngoingSelection == null)
                if (InitCondition())
                    OngoingSelection = new Selection();
        }
    }

}
