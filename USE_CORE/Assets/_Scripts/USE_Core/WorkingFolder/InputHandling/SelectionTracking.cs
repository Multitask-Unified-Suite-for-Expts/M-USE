using System;
using System.Collections;
using System.Collections.Generic;
using SelectionTracking;
using UnityEngine;
using USE_StimulusManagement;
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
            
            mouseClick.UpdateConditions.Add(mouseClick.DefaultConditions("MouseButton0Down"));
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
        }
    }

    public class SelectionHandler
    {
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

        public SelectionHandler()
        {
            InitConditions = new List<BoolDelegate>();
            UpdateConditions = new List<BoolDelegate>();
            TerminationConditions = new List<BoolDelegate>();
            InitErrorTriggers = new List<BoolDelegate>();
            UpdateErrorTriggers = new List<BoolDelegate>();
            TerminationErrorTriggers = new List<BoolDelegate>();

            AllSelections = new List<USE_Selection>();
            SuccessfulSelections = new List<USE_Selection>();
            UnsuccessfulSelections = new List<USE_Selection>();
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
        
        public void AddToActiveHandlers(object sender, EventArgs e)
        {
            selectionTracker.ActiveSelectionHandlers.Add(HandlerName, this);
        }

        public void RemoveFromActiveHandlers(object sender, EventArgs e)
        {
            selectionTracker.ActiveSelectionHandlers.Remove(HandlerName);
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
            if (OngoingSelection == null) //no previous selection
            {
                CheckInit(); 
                return;
            }else
                Debug.Log("laskhdgkjahsgkjhaskdghkasdhgkahghashgahgohsadghsidhgshglsalighlisadhgliasdglih");
            
            //if we have reached this point we know there is a target, there was a previous selection,
            //and this is not the first frame of new selection
            Debug.Log(currentTarget);
            Debug.Log(OngoingSelection);
            Debug.Log(OngoingSelection.SelectedGameObject);
            if (currentTarget != OngoingSelection.SelectedGameObject) //previous selection on different game object
            {
                CheckTermination(); //check termination of previous selection
                CheckInit(); //check init of current selection
                return;
            }

                //if we have reached this point we know we have an ongoing selection
            CheckUpdate();
            CheckTermination();
            
        }

        private bool CheckInit()
        {
            Debug.Log("inititiitititititititi");
            bool init = CheckAllConditions(InitConditions);
            bool initErrors = CheckAllConditions(InitErrorTriggers);
            if (init) // intialization condition is true (e.g. mouse button is down)
                if (!initErrors)
                    OngoingSelection = new USE_Selection(currentTarget); // start a new ongoing selection
                else
                    SelectionInitErrorHandling();
            Debug.Log("ITI INIT " + init);
            Debug.Log("ITI INITerrror " + initErrors);
            return init & !initErrors;
        }

        private bool CheckUpdate()
        {
            bool update = CheckAllConditions(UpdateConditions);
            bool updateErrors = CheckAllConditions(UpdateErrorTriggers);
            Debug.Log("selectionupdate: " + update);
            Debug.Log("selectionupdateerrors: " + updateErrors);
            if (update)
            {
                // update condition is true (e.g. mouse button is being held down)
                if (!updateErrors)
                    OngoingSelection.UpdateSelection(CurrentInputLocation()); // will track duration and other custom functions while selecting
                else
                    SelectionUpdateErrorHandling();
            }

            return update & !updateErrors;
        }

        private bool CheckTermination()
        {
            bool term = CheckAllConditions(TerminationConditions);
            bool termErrors = CheckAllConditions(TerminationErrorTriggers);
            if (term)
            {
                // update condition is true (e.g. mouse button is being held down)
                if (!termErrors)
                {
                    OngoingSelection.CompleteSelection(true);
                    OngoingSelection.WasSuccessful = true;
                    SuccessfulSelections.Add(OngoingSelection);
                }
                else
                {
                    OngoingSelection.CompleteSelection(false);
                    OngoingSelection.WasSuccessful = false;
                    UnsuccessfulSelections.Add(OngoingSelection);
                    SelectionTerminationErrorHandling();
                }
                AllSelections.Add(OngoingSelection);
                OngoingSelection = null;
            }
            return term & !termErrors;
        }

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
                foreach (BoolDelegate bd in boolList)
                {
                    Debug.Log("##################################################################################");
                    if (!bd())
                        return false;
                }
                return true;
            }
            else
                return false;
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
            DefaultConditions.Add("MovedTooFar", ()=>
            {
                return Vector3.Distance(CurrentInputLocation(), OngoingSelection.InputLocations[0]) < MaxPixelDisplacement;
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
