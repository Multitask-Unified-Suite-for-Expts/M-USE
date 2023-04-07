using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_UI;
using USE_StimulusManagement;
using WhatWhenWhere_Namespace;

namespace SelectionTracking
{
    public class SelectionTracker //Available at Trial/Task/Session Level
    {
        public Dictionary<string, SelectionHandler> ActiveSelectionHandlers = new Dictionary<string, SelectionHandler>();
        public List<string> SessionHandlerNames = new List<string>();
        public List<string> TaskHandlerNames = new List<string>();
        public List<string> TrialHandlerNames = new List<string>();

        public SelectionHandler SetupSelectionHandler(string handlerLevel, string handlerName, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            if (!HandlerLevelValid(handlerLevel))
                return null;
            else
            {
                SelectionHandler newHandler = GetDefaultSelectionHandler(handlerName);
                newHandler.HandlerName = handlerName;
                newHandler.HandlerLevel = handlerLevel.ToLower();
                newHandler.selectionTracker = this;
                if (setActiveOnInit != null)
                    setActiveOnInit.StateInitializationFinished += newHandler.AddToActiveHandlers;

                if (setInactiveOnTerm == null)
                    setInactiveOnTerm = setActiveOnInit;

                if (setInactiveOnTerm != null)
                    setInactiveOnTerm.StateTerminationFinished += newHandler.RemoveFromActiveHandlers;

                return newHandler;
            }
        }

        public void UpdateActiveSelections()
        {
            foreach (string key in ActiveSelectionHandlers.Keys)
            {
                if (ActiveSelectionHandlers[key].HandlerActive)
                    ActiveSelectionHandlers[key].UpdateSelections();
            }
        }

        public bool HandlerLevelValid(string handlerLevel)
        {
            List<string> validLevels = new List<string>() { "session", "task", "trial" };
            if (validLevels.Contains(handlerLevel.ToLower()))
                return true;
            else
            {
                Debug.Log("The HandlerLevel value you provided is not valid. Valid HanderLevel's include session, task, trial.");
                return false;
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

            mouseClick.UpdateErrorTriggers.Add("MovedTooFar", mouseClick.DefaultConditions("MovedTooFar"));
            mouseClick.UpdateErrorTriggers.Add("DurationTooLong", mouseClick.DefaultConditions("DurationTooLong"));

            mouseClick.TerminationConditions.Add(mouseClick.DefaultConditions("MouseButton0Up"));

            mouseClick.TerminationErrorTriggers.Add("DurationTooShort", mouseClick.DefaultConditions("DurationTooShort"));

            mouseClick.CurrentInputLocation = () => InputBroker.mousePosition;

            DefaultSelectionHandlers.Add("MouseButton0Click", mouseClick);


            SelectionHandler gazeSelection = new SelectionHandler();
            gazeSelection.InitConditions.Add(gazeSelection.DefaultConditions("RaycastHitsAGameObject"));

            gazeSelection.UpdateConditions.Add(gazeSelection.DefaultConditions("RaycastHitsSameObjectAsPreviousFrame"));

            gazeSelection.UpdateErrorTriggers.Add("MovedTooFar", gazeSelection.DefaultConditions("MovedTooFar"));
            gazeSelection.UpdateErrorTriggers.Add("DurationTooLong", gazeSelection.DefaultConditions("DurationTooLong"));

            gazeSelection.TerminationErrorTriggers.Add("DurationTooShort", gazeSelection.DefaultConditions("DurationTooShort"));
            DefaultSelectionHandlers.Add("GazeSelection", gazeSelection);


            return DefaultSelectionHandlers[hName];
        }

        public class USE_Selection
        {
            public float? Duration, StartTime, EndTime;
            public int StartFrame, EndFrame;
            public GameObject SelectedGameObject;
            public StimDefPointer SelectedStimDefPointer;
            public bool WasSuccessful;
            public List<Vector3> InputLocations;
            public string ErrorType;


            public USE_Selection(GameObject go)
            {
                SelectedGameObject = go;
                SelectedStimDefPointer = SelectedGameObject?.GetComponent<StimDefPointer>();

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
            public USE_Selection LastSelection;
            public USE_Selection LastSuccessfulSelection;
            public USE_Selection LastUnsuccessfulSelection;
            public USE_Selection OngoingSelection;
            public List<USE_Selection> AllSelections, SuccessfulSelections, UnsuccessfulSelections;

            private GameObject currentTarget;
            public float? MinDuration, MaxDuration;
            public int? MaxPixelDisplacement;
            public List<BoolDelegate> InitConditions, UpdateConditions, TerminationConditions;
            public Dictionary<string, BoolDelegate> InitErrorTriggers, UpdateErrorTriggers, TerminationErrorTriggers;
            public InputDelegate CurrentInputLocation;
            public SelectionTracker selectionTracker;
            public string HandlerName;
            public string HandlerLevel;
            public bool HandlerActive;

            public int Num_HeldTooLong;
            public int Num_HeldTooShort;
            public int Num_MovedTooFar;

            public Vector3 InitialTouchPos;

            public event EventHandler<TouchFBController.TouchFeedbackArgs> TouchErrorFeedback;

            public SelectionHandler()
            {
                HandlerActive = true;

                InitConditions = new List<BoolDelegate>();
                UpdateConditions = new List<BoolDelegate>();
                TerminationConditions = new List<BoolDelegate>();
                InitErrorTriggers = new Dictionary<string, BoolDelegate>();
                UpdateErrorTriggers = new Dictionary<string, BoolDelegate>();
                TerminationErrorTriggers = new Dictionary<string, BoolDelegate>();

                AllSelections = new List<USE_Selection>();
                SuccessfulSelections = new List<USE_Selection>();
                UnsuccessfulSelections = new List<USE_Selection>();

                LastSelection = new USE_Selection(null);
                LastSuccessfulSelection = new USE_Selection(null);
                LastUnsuccessfulSelection = new USE_Selection(null);

                InitialTouchPos = new Vector3();
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

                if (HandlerLevel == "session")
                    selectionTracker.SessionHandlerNames.Add(HandlerName);
                else if (HandlerLevel == "task")
                    selectionTracker.TaskHandlerNames.Add(HandlerName);
                else if (HandlerLevel == "trial")
                    selectionTracker.TrialHandlerNames.Add(HandlerName);
                else
                    Debug.Log("The HandlerLevel value you input does not match the possible values of: session, task, trial");
            }

            public void RemoveFromActiveHandlers(object sender, EventArgs e)
            {
                selectionTracker.ActiveSelectionHandlers.Remove(HandlerName);

                if (HandlerLevel == "session")
                    selectionTracker.SessionHandlerNames.Remove(HandlerName);
                else if (HandlerLevel == "task")
                    selectionTracker.TaskHandlerNames.Remove(HandlerName);
                else if (HandlerLevel == "trial")
                    selectionTracker.TrialHandlerNames.Remove(HandlerName);
            }

            public void ClearSelections()
            {
                if (SuccessfulSelections.Count > 0)
                    SuccessfulSelections.Clear();

                if (UnsuccessfulSelections.Count > 0)
                    UnsuccessfulSelections.Clear();

                if (AllSelections.Count > 0)
                    AllSelections.Clear();

                LastSelection = new USE_Selection(null);
                LastSuccessfulSelection = new USE_Selection(null);
                LastUnsuccessfulSelection = new USE_Selection(null);
            }

            public void ClearCounts()
            {
                Num_HeldTooLong = 0;
                Num_HeldTooShort = 0;
                Num_MovedTooFar = 0;
            }

            public bool LastSuccessfulSelectionMatches(GameObject go)
            {
                return ReferenceEquals(LastSuccessfulSelection.SelectedGameObject, go);
            }

            public void IncrementErrorCount(string error)
            {
                switch(error)
                {
                    case "DurationTooLong":
                        Num_HeldTooLong++;
                        break;
                    case "DurationTooShort":
                        Num_HeldTooShort++;
                        break;
                    case "MovedTooFar":
                        Num_MovedTooFar++;
                        break;
                    default:
                        break;
                }
            }

            private void SelectionErrorHandling(string error)
            {
                if (OngoingSelection != null)
                {
                    OngoingSelection.ErrorType = error;
                    IncrementErrorCount(error);
                    TouchErrorFeedback?.Invoke(this, new TouchFBController.TouchFeedbackArgs(OngoingSelection.SelectedGameObject, OngoingSelection.ErrorType));
                }
                else
                    Debug.Log("Trying to set the ErrorType of OngoingSelection, but OngoingSelection is null!");
            }

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
                string? initErrors = CheckAllErrorTriggers("init");

                if (init != null && init.Value) // intialization condition is true (e.g. mouse button is down)
                {
                    if (initErrors == null)
                        OngoingSelection = new USE_Selection(currentTarget); // start a new ongoing selection
                    else
                        SelectionErrorHandling(initErrors);
                }
            }

            private bool CheckUpdate()
            {
                bool? update = CheckAllConditions(UpdateConditions);
                string? updateErrors = CheckAllErrorTriggers("update");
                if (update == null || update.Value)
                {
                    if (updateErrors == null) // update condition is true (e.g. mouse button is being held down)
                    {
                        OngoingSelection.UpdateSelection(CurrentInputLocation()); // will track duration and other custom functions while selecting
                        return true;
                    }
                    else
                    {
                        SelectionErrorHandling(updateErrors);
                        return false;
                    }
                }
                else
                {
                    SelectionErrorHandling(updateErrors);
                    return false;
                }
                //what happens if update is false?
            }

            private void CheckTermination()
            {
                bool? term = CheckAllConditions(TerminationConditions);
                string? termErrors = CheckAllErrorTriggers("term");
                if (term == null || term.Value)
                {   
                    if (termErrors == null) // update condition is true (e.g. mouse button is being held down)
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
                        SelectionErrorHandling(termErrors);
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
                    GameObject hitObject = InputBroker.RaycastBoth(inputLocation.Value);
                    if (hitObject != null)
                        return hitObject;
                }
                return null;
            }


            public delegate GameObject GoDelegate();

            public delegate Vector3 InputDelegate();

            public string? CheckAllErrorTriggers(string category)
            {
                if(category == "init" && InitErrorTriggers != null)
                {
                    foreach(var pair in InitErrorTriggers)
                    {
                        if (pair.Value() == true)
                            return pair.Key;
                    }
                }
                else if (category == "update" && UpdateErrorTriggers != null)
                {
                    foreach (var pair in UpdateErrorTriggers)
                    {
                        if (pair.Value() == true)
                            return pair.Key;
                    }
                }
                else if (category == "term" && TerminationErrorTriggers != null)
                {
                    foreach (var pair in TerminationErrorTriggers)
                    {
                        if (pair.Value() == true)
                            return pair.Key;
                    }
                }
                return null;
            }

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
                DefaultConditions.Add("RaycastHitsAGameObject", () => currentTarget != null);
                DefaultConditions.Add("RaycastHitsSameObjectAsPreviousFrame", () => DefaultConditions["RaycastHitsAGameObject"]() &&
                                                                                   OngoingSelection != null &&
                                                                                   currentTarget == OngoingSelection.SelectedGameObject);
                DefaultConditions.Add("DurationTooLong", () => OngoingSelection.Duration > MaxDuration);
                DefaultConditions.Add("DurationTooShort", () => OngoingSelection.Duration < MinDuration);
                // DefaultConditions.Add("DurationTooLong", ()=> MaxDuration != null && OngoingSelection.Duration > MaxDuration);
                // DefaultConditions.Add("DurationTooShort", ()=> MinDuration != null && OngoingSelection.Duration < MinDuration);
                DefaultConditions.Add("MovedTooFar", () =>
                {
                    return MaxPixelDisplacement != null && 
                           Vector3.Distance(CurrentInputLocation(), OngoingSelection.InputLocations[0]) < MaxPixelDisplacement;
                });
                DefaultConditions.Add("MouseButton0", () => InputBroker.GetMouseButton(0));
                DefaultConditions.Add("MouseButton0Down", () => InputBroker.GetMouseButtonDown(0));
                DefaultConditions.Add("MouseButton0Up", () => InputBroker.GetMouseButtonUp(0));
                DefaultConditions.Add("MouseButton1", () => InputBroker.GetMouseButton(1));
                DefaultConditions.Add("MouseButton1Down", () => InputBroker.GetMouseButtonDown(1));
                DefaultConditions.Add("MouseButton1Up", () => InputBroker.GetMouseButtonUp(1));
                DefaultConditions.Add("MouseButton2", () => InputBroker.GetMouseButton(2));
                DefaultConditions.Add("MouseButton2Down", () => InputBroker.GetMouseButtonDown(2));
                DefaultConditions.Add("MouseButton2Up", () => InputBroker.GetMouseButtonUp(2));


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
    }
}