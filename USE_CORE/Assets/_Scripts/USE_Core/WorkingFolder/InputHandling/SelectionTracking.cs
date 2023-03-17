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
                AssignDefaultSelectionHandlers();
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
                AssignDefaultSelectionHandlers();
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


        private void AssignDefaultSelectionHandlers()
        {
            SelectionHandler shTemp = new SelectionHandler();
            ActiveSelectionHandlers.Add("RaycastHitsObject", shTemp);
            //raycast hits object, button 0 down (init + update), button 0 down (init + update) and up (termination), 

        }

    }

    public class Selection
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

    public class SelectionHandler
    {
        public List<Selection> AllSelections, SuccessfulSelections, UnsuccessfulSelections;
        public Selection OngoingSelection;
        public float? MinDuration, MaxDuration;
        public int? MaxPixelDisplacement;
        public BoolDelegate InitCondition, UpdateCondition, TerminationCondition;
        public InputDelegate CurrentInputLocation;

        public SelectionHandler()
        {
            
        }
        public SelectionHandler(float? minDuration, float? maxDuration, int? maxPixelDisplacement, BoolDelegate initCondition, BoolDelegate updateCondition = null,
            BoolDelegate terminationCondition = null)
        {
            MinDuration = minDuration;
            MaxDuration = maxDuration;
            MaxPixelDisplacement = maxPixelDisplacement;
            
            InitCondition = initCondition;
            
            if (updateCondition == null)
                UpdateCondition = initCondition;
            else
                UpdateCondition = updateCondition;

            if (terminationCondition == null)
                TerminationCondition = UpdateCondition;
            else
                TerminationCondition = terminationCondition;

        }

        public void UpdateSelections()
        {
            GameObject go = FindCurrentTarget(CurrentInputLocation());
            if (OngoingSelection == null && go != null) // there is no ongoing selection
            {
                if (InitCondition()) // intialization condition is true (e.g. mouse button is down)
                    OngoingSelection = new Selection(go); // create a new ongoing selection
            }
            else if (go != null)
                if (go == OngoingSelection.SelectedGameObject)
                {
                    if (UpdateCondition()) //
                    {
                        OngoingSelection.UpdateSelection();
                        if (TerminationCondition()) // this is kind of broken
                            OngoingSelection.CompleteSelection();
                    }
                    else
                    {
                        //selection has failed, do something (e.g. max duration exceeded
                    }
                }
        }


        public GameObject FindCurrentTarget(Vector3? inputLocation)
        {
            if (inputLocation.Value.x < 0 || inputLocation.Value.y < 0) //should also be if x or y is greater than screen
                inputLocation = null;

            if (inputLocation != null)
            {
                Vector3 direction = inputLocation.Value - Camera.main.transform.position;
                GameObject hitObject = InputBroker.RaycastBoth(inputLocation.Value, direction);

                if (hitObject != null)
                {
                    // if (InputBroker.GetMouseButton(0))
                    return hitObject;
                }
            }
            return null;
        }
        
        
        public delegate GameObject GoDelegate();

        public delegate Vector3 InputDelegate();


    }
    
    

}
