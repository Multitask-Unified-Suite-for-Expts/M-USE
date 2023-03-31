using System;
using System.Collections.Generic;
using SelectionTracking;
using UnityEngine;
using USE_States;


namespace SelectionTracking
{
    public class SelectionTracker
    {
        // Start is called before the first frame update
        public Dictionary<string, SelectionHandler> ActiveSelectionHandlers = new Dictionary<string, SelectionHandler>();

        public SelectionHandler SetupSelectionHandler(string handlerName, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            SelectionHandler newHandler = GetDefaultSelectionHandler(handlerName);
            newHandler.HandlerName = handlerName;
            newHandler.selectionTracker = this;
            if (setActiveOnInit != null)
                setActiveOnInit.StateInitializationFinished += newHandler.AddToActiveHandlers;

            if (setInactiveOnTerm == null)
                setInactiveOnTerm = setActiveOnInit;

            if (setInactiveOnTerm != null)
                setInactiveOnTerm.StateTerminationFinished += newHandler.RemoveFromActiveHandlers;

            return newHandler;
        }

        public void UpdateActiveSelections()
        {
            foreach (string key in ActiveSelectionHandlers.Keys)
            {
                if (ActiveSelectionHandlers[key].HandlerActive)
                    ActiveSelectionHandlers[key].UpdateSelections();
            }
        }


        private SelectionHandler GetDefaultSelectionHandler(string hName)
        {
            Dictionary<string, SelectionHandler> DefaultSelectionHandlers =
                new Dictionary<string, SelectionHandler>();

            SelectionHandler mouseClick = new SelectionHandler();
            mouseClick.InitConditions.Add(mouseClick.DefaultConditions("RaycastHitsAGameObject"));
            mouseClick.InitConditions.Add(mouseClick.DefaultConditions("MouseButton0Down"));
            
            mouseClick.UpdateConditions.Add(mouseClick.DefaultConditions("MouseButton0"));
            mouseClick.UpdateConditions.Add(mouseClick.DefaultConditions("RaycastHitsSameObjectAsPreviousFrame"));
            
            mouseClick.UpdateErrorTriggers.Add(mouseClick.DefaultConditions("MovedTooFar"));
            mouseClick.UpdateErrorTriggers.Add(mouseClick.DefaultConditions("DurationTooLong"));
            
            mouseClick.TerminationConditions.Add(mouseClick.DefaultConditions("MouseButton0Up"));
            
            mouseClick.TerminationErrorTriggers.Add(mouseClick.DefaultConditions("DurationTooShort"));

            mouseClick.CurrentInputLocation = () => InputBroker.mousePosition;
            
            DefaultSelectionHandlers.Add("MouseButton0Click", mouseClick);
            
            
            SelectionHandler gazeSelection = new SelectionHandler();
            gazeSelection.InitConditions.Add(gazeSelection.DefaultConditions("RaycastHitsAGameObject"));
            
            gazeSelection.UpdateConditions.Add(gazeSelection.DefaultConditions("RaycastHitsSameObjectAsPreviousFrame"));
            
            gazeSelection.UpdateErrorTriggers.Add(gazeSelection.DefaultConditions("MovedTooFar"));
            gazeSelection.UpdateErrorTriggers.Add(gazeSelection.DefaultConditions("DurationTooLong"));

            gazeSelection.TerminationErrorTriggers.Add(gazeSelection.DefaultConditions("DurationTooShort"));
            DefaultSelectionHandlers.Add("GazeSelection", gazeSelection);
                
                

            return DefaultSelectionHandlers[hName];
        }
    }
    
    }

    public class USE_Selection
    {
        public float? Duration, StartTime, EndTime;
        public int StartFrame, EndFrame;
        public GameObject SelectedGameObject;
        public bool WasSuccessful;
        public List<Vector3> InputLocations;

        public USE_Selection(GameObject go)
        {
            SelectedGameObject = go;
            Duration = 0;
            StartFrame = Time.frameCount;
            StartTime = Time.time;
            InputLocations = new List<Vector3>();
        }

        public void UpdateSelection(Vector3 inputLocation)
        {
            InputLocations.Add(inputLocation);
            Duration = Time.time - StartTime;
        }

        public void CompleteSelection(bool success = true)
        {
            EndTime = Time.time;
            Duration = EndTime - StartTime;
            WasSuccessful = success;
            //error handling?
        }
    }

    public class SelectionHandler
    {
        public USE_Selection? LastSelection;
        public USE_Selection? LastSuccessfulSelection;
        public USE_Selection? LastUnsuccessfulSelection;
        public List<USE_Selection> AllSelections, SuccessfulSelections, UnsuccessfulSelections;
        public USE_Selection OngoingSelection;
        private GameObject currentTarget;
        public float? MinDuration, MaxDuration;
        public int? MaxPixelDisplacement;
        public List<BoolDelegate> InitConditions, UpdateConditions, TerminationConditions, 
            InitErrorTriggers, UpdateErrorTriggers, TerminationErrorTriggers;
        public InputDelegate CurrentInputLocation;
        public SelectionTracker selectionTracker;
        public string HandlerName;

        public bool HandlerActive;

        public SelectionHandler()
        {
            HandlerActive = true;

            InitConditions = new List<BoolDelegate>();
            UpdateConditions = new List<BoolDelegate>();
            TerminationConditions = new List<BoolDelegate>();
            InitErrorTriggers = new List<BoolDelegate>();
            UpdateErrorTriggers = new List<BoolDelegate>();
            TerminationErrorTriggers = new List<BoolDelegate>();

            AllSelections = new List<USE_Selection>();
            SuccessfulSelections = new List<USE_Selection>();
            UnsuccessfulSelections = new List<USE_Selection>();

            LastSelection = null;
            LastSuccessfulSelection = null;
            LastUnsuccessfulSelection = null;
        }

        public SelectionHandler(InputDelegate inputLoc = null, float? minDuration = null, float? maxDuration = null, 
                                int? maxPixelDisplacement = null)
        {

            if (inputLoc == null)
                CurrentInputLocation = () => InputBroker.mousePosition; //default to just using the mouse
            
            MinDuration = minDuration;
            MaxDuration = maxDuration;
            MaxPixelDisplacement = maxPixelDisplacement;
        }
        
        public void AddToActiveHandlers(object sender, EventArgs e)
        {
            selectionTracker.ActiveSelectionHandlers.Add(HandlerName, this);
        }

        public void RemoveFromActiveHandlers(object sender, EventArgs e)
        {
            selectionTracker.ActiveSelectionHandlers.Remove(HandlerName);
        }

        public void ClearSuccessfulSelections() //Used in EC.
        {
            if (SuccessfulSelections.Count > 0)
                SuccessfulSelections.Clear();
        }

        public void ClearSelections() //Not yet used.
        {
            if (SuccessfulSelections.Count > 0)
                SuccessfulSelections.Clear();

            if (UnsuccessfulSelections.Count > 0)
                UnsuccessfulSelections.Clear();

            if (AllSelections.Count > 0)
                AllSelections.Clear();
        }


        private void SelectionInitErrorHandling(){}
        private void SelectionUpdateErrorHandling(){}
        private void SelectionTerminationErrorHandling(){}

        public void UpdateSelections()
        {

            if (CurrentInputLocation == null) // there is no input recorded on the screen
            {
                //Debug.Log(" currentInputLocation == null");
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
                //Debug.Log(" currentTarget == null");
                if (OngoingSelection != null) // the previous frame was a selection
                {
                    CheckTermination();
                }
                return;
            }
            
            //if we have reached this point we know there is a target
            if (OngoingSelection == null) //no previous selection
            {
                //Debug.Log(" OngoingSelection == null");
                CheckInit(); 
                return;
            }
            
            //if we have reached this point we know there is a target, there was a previous selection,
            //and this is not the first frame of new selection
            
            
            if (currentTarget != OngoingSelection.SelectedGameObject) //previous selection was on different game object
            {
                //Debug.Log(" currentTarget != OngoingSelection.SelectedGameObject");
                CheckTermination(); //check termination of previous selection
                CheckInit(); //check init of current selection
                return;
            }

            //if we have reached this point we know we have an ongoing selection
            bool updateConditionsMet = CheckUpdate();
            CheckTermination();
            if (!updateConditionsMet && OngoingSelection != null) 
                //the selection fails because update conditions are not met (but termination condition was not met either)
            {
                OngoingSelection.CompleteSelection(false);
                AllSelections.Add(OngoingSelection);
                //LastUnsuccessfulSelection = OngoingSelection; //Not sure if this should go here, or just down below
                UnsuccessfulSelections.Add(OngoingSelection);
                OngoingSelection = null;
            }
            
        }

        private void CheckInit()
        {
            bool? init = CheckAllConditions(InitConditions);
            bool? initErrors = CheckAllConditions(InitErrorTriggers);
            //Debug.Log("####################init: " + init + ", initerrors: " + initErrors);
            // Debug.Log(Time.frameCount + "Condition 1: " + InitConditions[0]() + "Condition 2: " + InitConditions[1]());
            if (init != null && init.Value) // intialization condition is true (e.g. mouse button is down)
                if (initErrors == null || !initErrors.Value)
                    OngoingSelection = new USE_Selection(currentTarget); // start a new ongoing selection
                else
                    SelectionInitErrorHandling();
        }

        private bool CheckUpdate()
        {
            // Debug.Log(Time.frameCount + "Condition 1: " + UpdateConditions[0]() + "Condition 2: " + UpdateConditions[1]());
            bool? update = CheckAllConditions(UpdateConditions);
            bool? updateErrors = CheckAllConditions(UpdateErrorTriggers);
            // Debug.Log(Time.frameCount + "####################selectionupdate: " + update + ", selectionupdateerrors: " + updateErrors);
            if (update == null || update.Value)
            {
                // update condition is true (e.g. mouse button is being held down)
                if (updateErrors == null || !updateErrors.Value)
                {
                    // Debug.Log(Time.frameCount + "updating selection");
                    OngoingSelection.UpdateSelection(
                        CurrentInputLocation()); // will track duration and other custom functions while selecting
                    return true;
                }
                else
                {
                    SelectionUpdateErrorHandling();
                    return false;
                }
            }
            else
            { 
                // Debug.Log(Time.frameCount + "update conditions not met %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
                SelectionUpdateErrorHandling();
                return false;
            }
            //what happens if update is false?
        }

        private void CheckTermination()
        {
            bool? term = CheckAllConditions(TerminationConditions);
            bool? termErrors = CheckAllConditions(TerminationErrorTriggers);
            if (term == null || term.Value)
            {
                // update condition is true (e.g. mouse button is being held down)
                if (termErrors == null || !termErrors.Value)
                {
                    OngoingSelection.CompleteSelection(true);
                    OngoingSelection.WasSuccessful = true;
                    LastSelection = OngoingSelection;
                    LastSuccessfulSelection = OngoingSelection;
                    SuccessfulSelections.Add(OngoingSelection);
                }
                else
                {
                    OngoingSelection.CompleteSelection(false);
                    OngoingSelection.WasSuccessful = false;
                    LastSelection = OngoingSelection;
                    LastUnsuccessfulSelection = OngoingSelection;
                    UnsuccessfulSelections.Add(OngoingSelection);
                    SelectionTerminationErrorHandling();
                }
                AllSelections.Add(OngoingSelection);
                OngoingSelection = null;
            }
        }

        private GameObject FindCurrentTarget(Vector3? inputLocation)
        {
        if (inputLocation.Value.x < 0 || inputLocation.Value.y < 0 || inputLocation.Value.x > Screen.width || inputLocation.Value.y > Screen.height)
                inputLocation = null;

            if (inputLocation != null)
            {
                Vector3 direction = inputLocation.Value - Camera.main.transform.position;
                GameObject hitObject = InputBroker.RaycastBoth(inputLocation.Value, direction);

                if (hitObject != null)
                {
                    return hitObject;
                }
            }
            return null;
        }
        
        
        public delegate GameObject GoDelegate();

        public delegate Vector3 InputDelegate();
        public bool? CheckAllConditions(List<BoolDelegate> boolList)
        {
            if (boolList != null && boolList.Count > 0)
            {
                foreach (BoolDelegate bd in boolList)
                {
                    if (!bd())
                        return false;
                }
                return true;
            }
            else
                return null;
        }

        public BoolDelegate DefaultConditions(string ConditionName)
        {
            Dictionary<string, BoolDelegate> DefaultConditions = new Dictionary<string, BoolDelegate>();
            DefaultConditions.Add("RaycastHitsAGameObject", ()=> currentTarget != null);
            DefaultConditions.Add("RaycastHitsSameObjectAsPreviousFrame", ()=> DefaultConditions["RaycastHitsAGameObject"]() && 
                                                                               OngoingSelection != null && 
                                                                               currentTarget == OngoingSelection.SelectedGameObject);
            DefaultConditions.Add("DurationTooLong", ()=> OngoingSelection.Duration > MaxDuration);
            DefaultConditions.Add("DurationTooShort", ()=> OngoingSelection.Duration < MinDuration);
            // DefaultConditions.Add("DurationTooLong", ()=> MaxDuration != null && OngoingSelection.Duration > MaxDuration);
            // DefaultConditions.Add("DurationTooShort", ()=> MinDuration != null && OngoingSelection.Duration < MinDuration);
            DefaultConditions.Add("MovedTooFar", ()=>
            {
                return MaxPixelDisplacement == null || 
                       Vector3.Distance(CurrentInputLocation(), OngoingSelection.InputLocations[0]) < MaxPixelDisplacement;
            });
            DefaultConditions.Add("MouseButton0", ()=> InputBroker.GetMouseButton(0));
            DefaultConditions.Add("MouseButton0Down", ()=> InputBroker.GetMouseButtonDown(0));
            DefaultConditions.Add("MouseButton0Up", ()=> InputBroker.GetMouseButtonUp(0));
            DefaultConditions.Add("MouseButton1", ()=> InputBroker.GetMouseButton(1));
            DefaultConditions.Add("MouseButton1Down", ()=> InputBroker.GetMouseButtonDown(1));
            DefaultConditions.Add("MouseButton1Up", ()=> InputBroker.GetMouseButtonUp(1));
            DefaultConditions.Add("MouseButton2", ()=> InputBroker.GetMouseButton(2));
            DefaultConditions.Add("MouseButton2Down", ()=> InputBroker.GetMouseButtonDown(2));
            DefaultConditions.Add("MouseButton2Up", ()=> InputBroker.GetMouseButtonUp(2));
            
            
            if (DefaultConditions.ContainsKey(ConditionName))
                return DefaultConditions[ConditionName];
            else
            {
                Debug.LogError("Attempted to load a selection handler condition called " + ConditionName + 
                               "but there is no such condition in the default dictionary.");
                return null;
            }
            
            
        }
    }
