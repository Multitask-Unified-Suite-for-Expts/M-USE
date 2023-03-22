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

        public SelectionTracker(Dictionary<string, SelectionHandler> shs, bool useDefaultHandlers = true)
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
            // DEFINE RAYCAST ON SELECTION HANDLING ------------------------------------
            SelectionHandler raycastOnSelectionHandler = new SelectionHandler();
            raycastOnSelectionHandler.InitConditions.Add () =>
            {
                // when would a selection begin in a raycast selection
                if (raycastOnSelectionHandler.FindCurrentTarget(InputBroker.mousePosition) != null)
                    return true;
                return false;
            };
            // raycastOnSelectionHandler.UpdateConditions.Add null;
            raycastOnSelectionHandler.TerminationConditions.Add () =>
            {
                if (raycastOnSelectionHandler.OngoingSelection.Duration >= raycastOnSelectionHandler.MinDuration &&
                    raycastOnSelectionHandler.OngoingSelection.Duration <= raycastOnSelectionHandler.MaxDuration)
                    return true;
                return false;
            };
            ActiveSelectionHandlers.Add("RaycastOnSelection", raycastOnSelectionHandler);

            // DEFINE RAYCAST ON OFF SELECTION HANDLING ------------------------------------
            SelectionHandler raycastOnOffSelectionHandler = new SelectionHandler();
            raycastOnOffSelectionHandler.Add () =>
            {
                // when would a selection begin in a raycast selection
                if (raycastOnOffSelectionHandler.FindCurrentTarget(InputBroker.mousePosition) != null)
                    return true;
                return false;
            };
            raycastOnOffSelectionHandler.UpdateConditions.Add null;
            raycastOnOffSelectionHandler.TerminationConditions.Add () =>
            {
                if (raycastOnOffSelectionHandler.OngoingSelection.Duration >= raycastOnOffSelectionHandler.MinDuration &&
                    raycastOnOffSelectionHandler.OngoingSelection.Duration <= raycastOnOffSelectionHandler.MaxDuration && 
                    raycastOnOffSelectionHandler.FindCurrentTarget(InputBroker.mousePosition) == null)
                    return true;
                return false;
            };
            ActiveSelectionHandlers.Add("RaycastOnSelection", raycastOnSelectionHandler);
            
            // DEFINE LEFT MOUSE BUTTON DOWN SELECTION HANDLING ------------------------------------
            SelectionHandler leftMouseButtonDown = new SelectionHandler();
            leftMouseButtonDown.InitConditions.Add () =>
            {
                if (InputBroker.GetMouseButtonDown(0))
                    return true;
                return false;
            };
            leftMouseButtonDown.UpdateCondition = null; // same as the init condition
            ActiveSelectionHandlers.Add("LeftMouseButtonDown", leftMouseButtonDown);

            // DEFINE LEFT MOUSE BUTTON CLICK SELECTION HANDLING ------------------------------------
            SelectionHandler leftMouseButtonClick = new SelectionHandler();
            leftMouseButtonClick.InitCondition = () =>
            {
                if (InputBroker.GetMouseButtonDown(0))
                    return true;
                return false;
            };
            leftMouseButtonClick.UpdateCondition = null;
            leftMouseButtonClick.TerminationCondition = () =>
            {
                if (InputBroker.GetMouseButtonUp(0))
                    return true;
                return false;
                
                // SHOULD WE ADD MIN/MAX DURATION HERE??
            };
            ActiveSelectionHandlers.Add("LeftMouseButtonClick", leftMouseButtonClick);
            
            
            
            //raycast hits object, button 0 down (init + update), button 0 down (init + update) and up (termination), 
            
            // include other mouse buttons??

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
        public List<BoolDelegate> InitConditions, UpdateConditions, TerminationConditions, 
            InitErrorTriggers, UpdateErrorTriggers, TerminationErrorTriggers;
        public InputDelegate CurrentInputLocation;

        public SelectionHandler()
        {
            
        }
        public SelectionHandler(float? minDuration, float? maxDuration, int? maxPixelDisplacement, List<BoolDelegate> initConditions, List<BoolDelegate> updateConditions = null,
            List<BoolDelegate> terminationConditions = null)
        {
            MinDuration = minDuration;
            MaxDuration = maxDuration;
            MaxPixelDisplacement = maxPixelDisplacement;
            
            InitConditions = initConditions;
            
            if (updateConditions == null)
                UpdateConditions = initConditions;
            else
                UpdateConditions = updateConditions;

            if (terminationConditions == null)
                TerminationConditions = UpdateConditions;
            else
                TerminationConditions = terminationConditions;

        }

        public void UpdateSelections()
        {
            GameObject go = FindCurrentTarget(CurrentInputLocation());
            if (OngoingSelection == null && go != null) // there is no ongoing selection
            {
                if (CheckAllConditions(InitConditions)) // intialization condition is true (e.g. mouse button is down)
                    OngoingSelection = new Selection(go); // create a new ongoing selection
            }
            else if (go != null)
                if (go == OngoingSelection.SelectedGameObject)
                {
                    if (CheckAllConditions(UpdateConditions)) // update condition is true (e.g. mouse buton is being held down)
                        OngoingSelection.UpdateSelection(); // will track duration and other custom functions while selecting
                    if (CheckAllConditions(TerminationConditions)) 
                        OngoingSelection.CompleteSelection();
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
        public bool CheckAllConditions(IEnumerable<BoolDelegate> boolList)
        {
            if (boolList != null)
            {
                bool returnVal = true;
                foreach (BoolDelegate bd in boolList)
                {
                    if (!bd())
                        return false;
                }
                return returnVal;
            }
            else
                return false;
        }
    }
    

}
