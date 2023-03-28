using System;
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

        //public SelectionTracker(bool useDefaultHandlers = true)
        //{
        //    if (useDefaultHandlers)
        //        ActiveSelectionHandlers = AssignDefaultSelectionHandlers();
        //    else
        //    {
        //        Debug.LogError("A selection tracker is being initialized with UseDefaultHandlers set to false, " +
        //                       "but no alternative dictionary of trackers has been provided as an argument.");
        //    }
        //}

        public SelectionTracker(Dictionary<string, SelectionHandler> shs, bool useDefaultHandlers = true)
        {
            // if (useDefaultHandlers)
            // {
            //     AssignDefaultSelectionHandlers();
            //     foreach (string key in shs.Keys)
            //         ActiveSelectionHandlers.Add(key, shs[key]);
            // }
            // else
            //     ActiveSelectionHandlers = shs;

        }
        
        public void UpdateActiveSelections()
        {
            foreach (string key in ActiveSelectionHandlers.Keys)
            {
                ActiveSelectionHandlers[key].UpdateSelections();
            }
        }


        
        //private Dictionary<string, SelectionHandler> DefaultSelectionHandlers()
        //{
            
        //    // DEFINE RAYCAST ON SELECTION HANDLING ------------------------------------
        //    SelectionHandler tempH = new SelectionHandler();
            
        //    ActiveSelectionHandlers.Add("MouseOnObject", tempH);
        //    // raycastOnSelectionHandler.InitConditions.Add () =>
        //    // {
        //    //     // when would a selection begin in a raycast selection
        //    //     if (raycastOnSelectionHandler.FindCurrentTarget(InputBroker.mousePosition) != null)
        //    //         return true;
        //    //     return false;
        //    // };
        //    // // raycastOnSelectionHandler.UpdateConditions.Add null;
        //    // raycastOnSelectionHandler.TerminationConditions.Add () =>
        //    // {
        //    //     if (raycastOnSelectionHandler.OngoingSelection.Duration >= raycastOnSelectionHandler.MinDuration &&
        //    //         raycastOnSelectionHandler.OngoingSelection.Duration <= raycastOnSelectionHandler.MaxDuration)
        //    //         return true;
        //    //     return false;
        //    // };
        //    // ActiveSelectionHandlers.Add("RaycastOnSelection", raycastOnSelectionHandler);
        //    //
        //    // // DEFINE RAYCAST ON OFF SELECTION HANDLING ------------------------------------
        //    // SelectionHandler raycastOnOffSelectionHandler = new SelectionHandler();
        //    // raycastOnOffSelectionHandler.Add () =>
        //    // {
        //    //     // when would a selection begin in a raycast selection
        //    //     if (raycastOnOffSelectionHandler.FindCurrentTarget(InputBroker.mousePosition) != null)
        //    //         return true;
        //    //     return false;
        //    // };
        //    // raycastOnOffSelectionHandler.UpdateConditions.Add null;
        //    // raycastOnOffSelectionHandler.TerminationConditions.Add () =>
        //    // {
        //    //     if (raycastOnOffSelectionHandler.OngoingSelection.Duration >= raycastOnOffSelectionHandler.MinDuration &&
        //    //         raycastOnOffSelectionHandler.OngoingSelection.Duration <= raycastOnOffSelectionHandler.MaxDuration && 
        //    //         raycastOnOffSelectionHandler.FindCurrentTarget(InputBroker.mousePosition) == null)
        //    //         return true;
        //    //     return false;
        //    // };
        //    // ActiveSelectionHandlers.Add("RaycastOnSelection", raycastOnSelectionHandler);
        //    //
        //    // // DEFINE LEFT MOUSE BUTTON DOWN SELECTION HANDLING ------------------------------------
        //    // SelectionHandler leftMouseButtonDown = new SelectionHandler();
        //    // leftMouseButtonDown.InitConditions.Add () =>
        //    // {
        //    //     if (InputBroker.GetMouseButtonDown(0))
        //    //         return true;
        //    //     return false;
        //    // };
        //    // leftMouseButtonDown.UpdateCondition = null; // same as the init condition
        //    // ActiveSelectionHandlers.Add("LeftMouseButtonDown", leftMouseButtonDown);
        //    //
        //    // // DEFINE LEFT MOUSE BUTTON CLICK SELECTION HANDLING ------------------------------------
        //    // SelectionHandler leftMouseButtonClick = new SelectionHandler();
        //    // leftMouseButtonClick.InitCondition = () =>
        //    // {
        //    //     if (InputBroker.GetMouseButtonDown(0))
        //    //         return true;
        //    //     return false;
        //    // };
        //    // leftMouseButtonClick.UpdateCondition = null;
        //    // leftMouseButtonClick.TerminationCondition = () =>
        //    // {
        //    //     if (InputBroker.GetMouseButtonUp(0))
        //    //         return true;
        //    //     return false;
        //    //     
        //    //     // SHOULD WE ADD MIN/MAX DURATION HERE??
        //    // };
        //    // ActiveSelectionHandlers.Add("LeftMouseButtonClick", leftMouseButtonClick);
            
            
            
        //    //raycast hits object, button 0 down (init + update), button 0 down (init + update) and up (termination), 
            
        //    // include other mouse buttons??

        //}

    }

    public class Selection
    {
        public float? Duration, StartTime, EndTime;
        public int StartFrame, EndFrame;
        public GameObject SelectedGameObject;
        public bool WasSuccessful;
        public List<Vector3> InputLocations;

        public Selection(GameObject go)
        {
            SelectedGameObject = go;
            Duration = 0;
            StartFrame = Time.frameCount;
            StartTime = Time.time;
            CustomSelectionInit();
        }

        public void UpdateSelection(Vector3 inputLocation)
        {
            InputLocations.Add(inputLocation);
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
        private Selection OngoingSelection;
        private GameObject currentTarget;
        public float? MinDuration, MaxDuration;
        public int? MaxPixelDisplacement;
        public List<BoolDelegate> InitConditions, UpdateConditions, TerminationConditions, 
            InitErrorTriggers, UpdateErrorTriggers, TerminationErrorTriggers;
        public InputDelegate CurrentInputLocation;

        public SelectionHandler()
        {
            MinDuration = 0.25f;
            CurrentInputLocation = () => InputBroker.mousePosition;
            InitConditions = new List<BoolDelegate>(){DefaultConditions("RaycastHitsAGameObject")};
        }
        public SelectionHandler(InputDelegate inputLoc = null, float? minDuration = null, float? maxDuration = null, 
            int? maxPixelDisplacement = null)
        {
            if (inputLoc == null)
                CurrentInputLocation = () => InputBroker.mousePosition; //default to just using the mouse
            else
                CurrentInputLocation = inputLoc;
            
            MinDuration = minDuration;
            MaxDuration = maxDuration;
            MaxPixelDisplacement = maxPixelDisplacement;
        }
        
        
        private void SelectionInitErrorHandling(){}
        private void SelectionUpdateErrorHandling(){}
        private void SelectionTerminationErrorHandling(){}
        public void UpdateSelections()
        {
            if (CurrentInputLocation == null) // there is no input recorded on the screen
            {
                if (OngoingSelection != null) // the previous frame was a selection
                {
                    CheckTermination();
                }
                return;
            }
            
            //if we have reached this point we know there is input

            currentTarget = FindCurrentTarget(CurrentInputLocation());
            if (currentTarget == null) //input is not over a gameobject
            {
                if (OngoingSelection != null) // the previous frame was a selection
                {
                    CheckTermination();
                }

                return;
            }
            
            //if we have reached this point we know there is a target
            if (OngoingSelection == null || currentTarget != OngoingSelection.SelectedGameObject) //input is over a new gameObject
                CheckInit();
            
            
            if (OngoingSelection == null && currentTarget != null) // there is no ongoing selection
            {
                
            }
            else if (currentTarget != null)
            {
                bool selectionOngoing = true;
                if (currentTarget == OngoingSelection.SelectedGameObject)
                {
                    if (CheckAllConditions(UpdateConditions))
                    {
                        // update condition is true (e.g. mouse button is being held down)
                        if (!CheckAllConditions(UpdateErrorTriggers))
                            OngoingSelection.UpdateSelection(CurrentInputLocation()); // will track duration and other custom functions while selecting
                    }

                    if (CheckAllConditions(UpdateErrorTriggers))
                        SelectionUpdateErrorHandling();

                    if (CheckAllConditions(TerminationConditions))
                    {
                        if (!CheckAllConditions(TerminationErrorTriggers))
                            OngoingSelection.CompleteSelection();
                        else
                            SelectionTerminationErrorHandling();
                    }
                }
            }
        }

        private bool CheckInit()
        {
            bool init = CheckAllConditions(InitConditions);
            bool initErrors = CheckAllConditions(InitErrorTriggers);
            if (init) // intialization condition is true (e.g. mouse button is down)
            if (!initErrors)
                OngoingSelection = new Selection(currentTarget); // start a new ongoing selection
            else
                SelectionInitErrorHandling();
            return init & !initErrors;
        }

        private void CheckUpdate()
        {
            
        }
        
        private void CheckTermination(){}

        private GameObject FindCurrentTarget(Vector3? inputLocation)
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

        private BoolDelegate DefaultConditions(string ConditionName)
        {
            Dictionary<string, BoolDelegate> DefaultConditions = new Dictionary<string, BoolDelegate>();
            DefaultConditions.Add("RaycastHitsAGameObject", ()=> currentTarget != null);
            DefaultConditions.Add("RaycastHitsSameObjectAsPreviousFrame", ()=> DefaultConditions["RaycastHitsAGameObject"]() && 
                                                                               currentTarget == OngoingSelection.SelectedGameObject);
            DefaultConditions.Add("DurationTooLong", ()=> OngoingSelection.Duration > MaxDuration);
            DefaultConditions.Add("DurationTooShort", ()=> OngoingSelection.Duration < MinDuration);
            DefaultConditions.Add("MovedTooFar", ()=>
            {
                return Vector3.Distance(CurrentInputLocation(), OngoingSelection.InputLocations[0]) < MaxPixelDisplacement;
            });
            
            
            if (DefaultConditions.ContainsKey(ConditionName))
                return DefaultConditions[ConditionName];
            else
            {
                Debug.LogError("Attempted to load a selection handler condition called " + ConditionName + 
                               "but there is no such condition in the default dictionary.");
                return null;
            }
            
            
            // raycastOnSelectionHandler.InitConditions.Add () =>
            // {
            //     // when would a selection begin in a raycast selection
            //     if (raycastOnSelectionHandler.FindCurrentTarget(InputBroker.mousePosition) != null)
            //         return true;
            //     return false;
            // };
            // // raycastOnSelectionHandler.UpdateConditions.Add null;
            // raycastOnSelectionHandler.TerminationConditions.Add () =>
            // {
            //     if (raycastOnSelectionHandler.OngoingSelection.Duration >= raycastOnSelectionHandler.MinDuration &&
            //         raycastOnSelectionHandler.OngoingSelection.Duration <= raycastOnSelectionHandler.MaxDuration)
            //         return true;
            //     return false;
            // };
            // ActiveSelectionHandlers.Add("RaycastOnSelection", raycastOnSelectionHandler);
            //
            // // DEFINE RAYCAST ON OFF SELECTION HANDLING ------------------------------------
            // SelectionHandler raycastOnOffSelectionHandler = new SelectionHandler();
            // raycastOnOffSelectionHandler.Add () =>
            // {
            //     // when would a selection begin in a raycast selection
            //     if (raycastOnOffSelectionHandler.FindCurrentTarget(InputBroker.mousePosition) != null)
            //         return true;
            //     return false;
            // };
            // raycastOnOffSelectionHandler.UpdateConditions.Add null;
            // raycastOnOffSelectionHandler.TerminationConditions.Add () =>
            // {
            //     if (raycastOnOffSelectionHandler.OngoingSelection.Duration >= raycastOnOffSelectionHandler.MinDuration &&
            //         raycastOnOffSelectionHandler.OngoingSelection.Duration <= raycastOnOffSelectionHandler.MaxDuration && 
            //         raycastOnOffSelectionHandler.FindCurrentTarget(InputBroker.mousePosition) == null)
            //         return true;
            //     return false;
            // };
            // ActiveSelectionHandlers.Add("RaycastOnSelection", raycastOnSelectionHandler);
            //
            // // DEFINE LEFT MOUSE BUTTON DOWN SELECTION HANDLING ------------------------------------
            // SelectionHandler leftMouseButtonDown = new SelectionHandler();
            // leftMouseButtonDown.InitConditions.Add () =>
            // {
            //     if (InputBroker.GetMouseButtonDown(0))
            //         return true;
            //     return false;
            // };
            // leftMouseButtonDown.UpdateCondition = null; // same as the init condition
            // ActiveSelectionHandlers.Add("LeftMouseButtonDown", leftMouseButtonDown);
            //
            // // DEFINE LEFT MOUSE BUTTON CLICK SELECTION HANDLING ------------------------------------
            // SelectionHandler leftMouseButtonClick = new SelectionHandler();
            // leftMouseButtonClick.InitCondition = () =>
            // {
            //     if (InputBroker.GetMouseButtonDown(0))
            //         return true;
            //     return false;
            // };
            // leftMouseButtonClick.UpdateCondition = null;
            // leftMouseButtonClick.TerminationCondition = () =>
            // {
            //     if (InputBroker.GetMouseButtonUp(0))
            //         return true;
            //     return false;
            //     
            //     // SHOULD WE ADD MIN/MAX DURATION HERE??
            // };
            // ActiveSelectionHandlers.Add("LeftMouseButtonClick", leftMouseButtonClick);
        }
    }
    

}
