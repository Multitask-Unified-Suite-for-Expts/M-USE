/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



using System;
using System.Collections.Generic;
using UnityEngine;
using USE_States;


namespace SelectionTracking
{
    public class SelectionTracker //Available at Trial/Task/Session Level
    {
        public Dictionary<string, SelectionHandler> ActiveSelectionHandlers = new Dictionary<string, SelectionHandler>();
        public List<string> SessionHandlerNames = new List<string>();
        public List<string> TaskHandlerNames = new List<string>();
        public List<string> TrialHandlerNames = new List<string>();

        public SelectionHandler SetupSelectionHandler(string handlerLevel, string handlerName, InputTracker inputTracker, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            if (!HandlerLevelValid(handlerLevel))
                return null;
            else
            {
                SelectionHandler newHandler = GetDefaultSelectionHandler(handlerName);
                newHandler.HandlerName = handlerName;

                newHandler.HandlerLevel = handlerLevel.ToLower();
                newHandler.InputTracker = inputTracker;

                newHandler.InputTracker.UsingShotgunHandler = handlerName.ToLower().Contains("shotgun"); //Newly added to hopefully fix EventCode stuff

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
                {
                    ActiveSelectionHandlers[key].UpdateSelections();
                }
            }
        }

        public bool HandlerLevelValid(string handlerLevel)
        {
            List<string> validLevels = new List<string>() { "session", "task", "trial" };
            if (validLevels.Contains(handlerLevel.ToLower()))
                return true;
            else
            {
                Debug.LogWarning("The HandlerLevel value you provided is not valid. Valid HanderLevel's include session, task, trial.");
                return false;
            }
        }


        private SelectionHandler GetDefaultSelectionHandler(string hName)
        {
            Dictionary<string, SelectionHandler> DefaultSelectionHandlers = new Dictionary<string, SelectionHandler>();

            //----------------------------------------MOUSE CLICK HANDLER: --------------------------------------------------
            SelectionHandler mouseClick = new SelectionHandler();
            //Init Conditions: Has to click button, and has to hit a GO
            mouseClick.InitConditions.Add(mouseClick.DefaultConditions("RaycastHitsAGameObject"));
            mouseClick.InitConditions.Add(mouseClick.DefaultConditions("MouseButton0Down"));
            //Update Conditions: Mouse button must be down, raycast must hit previously hit GO
            mouseClick.UpdateConditions.Add(mouseClick.DefaultConditions("MouseButton0"));
            mouseClick.UpdateConditions.Add(mouseClick.DefaultConditions("RaycastHitsSameObjectAsPreviousFrame"));
            //Update Error Triggers: Moving too far, holding for too long
            mouseClick.UpdateErrorTriggers.Add("MovedTooFar", mouseClick.DefaultConditions("MovedTooFar"));

            mouseClick.UpdateErrorTriggers.Add("NotSelectablePeriod", mouseClick.DefaultConditions("NotSelectablePeriod"));

            mouseClick.UpdateErrorTriggers.Add("DurationTooLong", mouseClick.DefaultConditions("DurationTooLong"));
            //Termination Conditions: Releasing mouse click
            mouseClick.TerminationConditions.Add(mouseClick.DefaultConditions("MouseButton0Up"));
            //Termination Error Triggers: HoldingTooShort
            mouseClick.TerminationErrorTriggers.Add("DurationTooShort", mouseClick.DefaultConditions("DurationTooShort"));

            mouseClick.CurrentInputLocation = () => InputBroker.mousePosition;
            DefaultSelectionHandlers.Add("MouseButton0Click", mouseClick);


            //----------------------------------------MOUSE HOVER HANDLER: --------------------------------------------------
            SelectionHandler mouseHover = new SelectionHandler();
            
            //Init Conditions: has to hit a GO
            mouseHover.InitConditions.Add(mouseHover.DefaultConditions("RaycastHitsAGameObject"));
            //Update Conditions: raycast must hit previously hit GO
            mouseHover.UpdateConditions.Add(mouseHover.DefaultConditions("RaycastHitsSameObjectAsPreviousFrame"));
            //Update Error Triggers: Moving too far, holding for too long
            mouseHover.UpdateErrorTriggers.Add("MovedTooFar", mouseHover.DefaultConditions("MovedTooFar"));
            mouseHover.UpdateErrorTriggers.Add("NotSelectablePeriod", mouseHover.DefaultConditions("NotSelectablePeriod"));
            //Termination Conditions: Hovered above object for long enough
            mouseHover.TerminationConditions.Add(mouseHover.DefaultConditions("DurationSufficient"));
            //Termination Error Triggers: HoldingTooShort
            mouseHover.TerminationErrorTriggers.Add("DurationTooShort", mouseHover.DefaultConditions("DurationTooShort"));


            mouseHover.CurrentInputLocation = () => InputBroker.mousePosition;
            DefaultSelectionHandlers.Add("MouseHover", mouseHover);

            //----------------------------------------TOUCH SHOTGUN HANDLER: --------------------------------------------------
            SelectionHandler touchShotgun = new SelectionHandler();
            touchShotgun.InitConditions.Add(touchShotgun.DefaultConditions("ShotgunRaycastHitsAGameObject"));
            touchShotgun.InitConditions.Add(touchShotgun.DefaultConditions("MouseButton0Down"));

            touchShotgun.UpdateConditions.Add(touchShotgun.DefaultConditions("ShotgunRaycastHitsPreviouslyHitGO"));
            touchShotgun.UpdateConditions.Add(touchShotgun.DefaultConditions("MouseButton0"));

            touchShotgun.UpdateErrorTriggers.Add("MovedTooFar", touchShotgun.DefaultConditions("MovedTooFar"));
            touchShotgun.UpdateErrorTriggers.Add("DurationTooLong", touchShotgun.DefaultConditions("DurationTooLong"));

            touchShotgun.UpdateErrorTriggers.Add("NotSelectablePeriod", touchShotgun.DefaultConditions("NotSelectablePeriod"));


            touchShotgun.TerminationConditions.Add(touchShotgun.DefaultConditions("MouseButton0Up"));

            touchShotgun.TerminationErrorTriggers.Add("DurationTooShort", touchShotgun.DefaultConditions("DurationTooShort"));

            touchShotgun.CurrentInputLocation = () => InputBroker.mousePosition;
            DefaultSelectionHandlers.Add("TouchShotgun", touchShotgun);

            //----------------------------------------GAZE HANDLER: --------------------------------------------------
            SelectionHandler gazeSelection = new SelectionHandler();
            gazeSelection.InitConditions.Add(gazeSelection.DefaultConditions("RaycastHitsAGameObject"));

            gazeSelection.UpdateConditions.Add(gazeSelection.DefaultConditions("RaycastHitsSameObjectAsPreviousFrame"));

           // gazeSelection.UpdateErrorTriggers.Add("MovedTooFar", gazeSelection.DefaultConditions("MovedTooFar"));

            gazeSelection.UpdateErrorTriggers.Add("NotSelectablePeriod", gazeSelection.DefaultConditions("NotSelectablePeriod"));
            //gazeSelection.UpdateErrorTriggers.Add("InvalidGazePosition", gazeSelection.DefaultConditions("InvalidGazePosition"));

            gazeSelection.TerminationConditions.Add(gazeSelection.DefaultConditions("DurationSufficient"));

            gazeSelection.TerminationErrorTriggers.Add("DurationTooShort", gazeSelection.DefaultConditions("DurationTooShort"));
            gazeSelection.CurrentInputLocation = () => InputBroker.gazePosition;
            // gazeSelection.MaxPixelDisplacement = 70;
            gazeSelection.MinDuration = 0.7f;
            DefaultSelectionHandlers.Add("GazeSelection", gazeSelection);

            //----------------------------------------GAZE SHOTGUN HANDLER: --------------------------------------------------
            SelectionHandler gazeShotgun = new SelectionHandler();
            gazeShotgun.InitConditions.Add(gazeShotgun.DefaultConditions("ShotgunRaycastHitsAGameObject"));

            gazeShotgun.UpdateConditions.Add(gazeShotgun.DefaultConditions("ShotgunRaycastHitsPreviouslyHitGO"));

            gazeShotgun.UpdateErrorTriggers.Add("NotSelectablePeriod", gazeShotgun.DefaultConditions("NotSelectablePeriod"));
            //gazeShotgun.UpdateErrorTriggers.Add("InvalidGazePosition", gazeShotgun.DefaultConditions("InvalidGazePosition"));
            //gazeShotgun.UpdateErrorTriggers.Add("MovedTooFar", gazeShotgun.DefaultConditions("MovedTooFar"));

            gazeShotgun.TerminationConditions.Add(gazeShotgun.DefaultConditions("DurationSufficient"));

            gazeShotgun.TerminationErrorTriggers.Add("DurationTooShort", gazeShotgun.DefaultConditions("DurationTooShort"));

            gazeShotgun.CurrentInputLocation = () => InputBroker.gazePosition;
            //gazeShotgun.MaxPixelDisplacement = 70;
            gazeShotgun.MinDuration = 0.7f;
            DefaultSelectionHandlers.Add("GazeShotgun", gazeShotgun);
            
            
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
            public float SelectionPrecision;
            public string ErrorType;

            public bool InitialFixationDurationPassed;

            public string ParentName;


            public USE_Selection(GameObject go)
            {
                SelectedGameObject = go;
                SelectedStimDefPointer = SelectedGameObject?.GetComponent<StimDefPointer>();

                Duration = 0;
                StartFrame = Time.frameCount;
                StartTime = Time.time;
                InputLocations = new List<Vector3>();

                if(go != null)
                {
                    ParentName = GetRootParentName(go);
                }
            }

            public static string GetRootParentName(GameObject go)
            {
                string rootParentName = go.name;
                Transform parent = go.transform.parent;

                while (parent != null)
                {
                    rootParentName = parent.gameObject.name;
                    parent = parent.parent;
                }
                return rootParentName;
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
            public USE_Selection LastSelection;
            public USE_Selection LastSuccessfulSelection;
            public USE_Selection LastUnsuccessfulSelection;
            public USE_Selection OngoingSelection;
            public List<USE_Selection> AllSelections, SuccessfulSelections, UnsuccessfulSelections;
            public InputTracker InputTracker; 

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
            public bool SelectablePeriod = true; //Set to true, then trial level's can turn off for certain states to get fb for selecting prematurely

            private bool FixationOnEventCodeSent; //used so that hover event code is only sent on first frame of hovering. 

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
                if (selectionTracker.ActiveSelectionHandlers.ContainsKey(HandlerName))
                    return;
                
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

            public bool LastSelectionMatches(GameObject go)
            {
                if(go != null && LastSelection != null && LastSelection.SelectedGameObject != null)
                {
                    if (ReferenceEquals(LastSelection.SelectedGameObject, go))
                        return true;
                }

                return false;
            }

            public bool LastSuccessfulSelectionMatches(List<GameObject> gameObjects) //Used for startbutton since it has 3 children GO's
            {
                if (gameObjects != null && LastSuccessfulSelection != null && LastSuccessfulSelection.SelectedGameObject != null)
                {
                    foreach (GameObject go in gameObjects)
                    {
                        if (ReferenceEquals(LastSuccessfulSelection.SelectedGameObject, go))
                            return true;
                    }
                }
                return false;
            }


            //Main method used by tasks to check if selection matches start button:
            public bool LastSuccessfulSelectionMatchesStartButton()
            {
                List<GameObject> startButtonChildren = Session.GetStartButtonChildren();
                if (startButtonChildren != null && LastSuccessfulSelection?.SelectedGameObject != null)
                {
                    foreach (GameObject go in startButtonChildren)
                    {
                        if (ReferenceEquals(LastSuccessfulSelection.SelectedGameObject, go))
                        {
                            Session.EventCodeManager.SendCodeThisFrame("StartButtonSelected");
                            return true;
                        }
                    }
                }
                return false;
            }


            public bool LastSuccessfulSelectionMatches(GameObject go)
            {
                return ReferenceEquals(LastSuccessfulSelection.SelectedGameObject, go);
            }


            private void SelectionErrorHandling(string error)
            {
                if (OngoingSelection != null)
                {
                    OngoingSelection.ErrorType = error;
                    TouchErrorFeedback?.Invoke(this, new TouchFBController.TouchFeedbackArgs(OngoingSelection));
                }
                else
                    Debug.Log("Trying to set the ErrorType of OngoingSelection, but OngoingSelection is null!");
            }

            public void UpdateSelections()
            {
                if (CurrentInputLocation == null)
                {
                    if (OngoingSelection != null)
                    {
                        CheckTermination();
                    } 
                    return;
                }
                
                //if we have reached this point we know there is input
                if (HandlerName.ToLower().Contains("shotgun"))
                    currentTarget = InputTracker.ShotgunRaycastTarget;
                else
                    currentTarget = InputTracker.SimpleRaycastTarget;

                if (currentTarget == null) //input is not over a gameobject
                {
                    if (FixationOnEventCodeSent && OngoingSelection == null)
                    {
                        //For EventCodes:
                        Session.EventCodeManager.SendCodeThisFrame("FixationOffObject");
                        FixationOnEventCodeSent = false; //reset fixation
                    }

                    if (OngoingSelection != null) // the previous frame was a selection
                    {
                        //For EventCodes:
                        if(FixationOnEventCodeSent && OngoingSelection.SelectedGameObject != null)
                        {
                            Session.EventCodeManager.CheckForAndSendEventCode(OngoingSelection.SelectedGameObject, "FixationOff");
                            FixationOnEventCodeSent = false; //reset fixation
                        }

                        CheckTermination();
                    }
                    return;
                }

                //For EventCodes:
                if (currentTarget != null && !FixationOnEventCodeSent && LastSelection.SelectedGameObject != currentTarget) //The last AND is so that it wont send if selection is made. 
                {
                    Session.EventCodeManager.CheckForAndSendEventCode(currentTarget, "FixationOn");
                    FixationOnEventCodeSent = true;
                }

                //if we have reached this point we know there is a target
                if (OngoingSelection == null) //no previous selection
                {
                    CheckInit();
                    return;
                }

                

                //if we have reached this point we know there is a target, there was a previous selection,
                //and this is not the first frame of new selection
                if (currentTarget != OngoingSelection.SelectedGameObject) //previous selection was on different game object
                {
                    CheckTermination(); //check termination of previous selection
                    CheckInit(); //check init of current selection
                    return;
                }

                //if we have reached this point we know we have an ongoing selection
                bool updateConditionsMet = CheckUpdate();
                CheckTermination();
                //the selection fails because update conditions are not met (but termination condition was not met either)
                if (!updateConditionsMet && OngoingSelection != null)
                {
                    OngoingSelection.CompleteSelection(false);
                    AllSelections.Add(OngoingSelection);
                    UnsuccessfulSelections.Add(OngoingSelection);
                    OngoingSelection = null;
                }
            }

            private void CheckInit()
            {
                bool? init = CheckAllConditions(InitConditions); //returning TRUE
                string? initErrors = CheckAllErrorTriggers("init");

                if (init != null && init.Value) // intialization condition is true (e.g. mouse button is down)
                {
                    if (initErrors == null)
                    {
                        //Debug.LogWarning("START OF NEW SELECTION");
                        OngoingSelection = new USE_Selection(currentTarget); // start a new ongoing selection
                    }
                    else
                    {
                        SelectionErrorHandling(initErrors);
                    }
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
                    return false;
                }
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
                        LastSuccessfulSelection = OngoingSelection;
                        if(OngoingSelection.SelectedGameObject != null)
                            LastSuccessfulSelection.SelectionPrecision = Vector2.Distance(OngoingSelection.InputLocations[0], Camera.main.WorldToScreenPoint(OngoingSelection.SelectedGameObject.transform.root.position));
                        SuccessfulSelections.Add(OngoingSelection);

                        //For EventCodes:
                        Session.EventCodeManager.CheckForAndSendEventCode(OngoingSelection.SelectedGameObject, null, "Selected");
                        FixationOnEventCodeSent = false; //reset hover
                    }
                    else
                    {
                        OngoingSelection.CompleteSelection(false);
                        OngoingSelection.WasSuccessful = false;
                        LastSelection = OngoingSelection;
                        LastUnsuccessfulSelection = OngoingSelection;
                        UnsuccessfulSelections.Add(OngoingSelection);
                        SelectionErrorHandling(termErrors);
                        //For EventCodes:
                        FixationOnEventCodeSent = false; //reset fixation
                    }
                    AllSelections.Add(OngoingSelection);
                    OngoingSelection = null;
                }
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

                DefaultConditions.Add("ShotgunRaycastHitsAGameObject", () => InputTracker.ShotgunRaycastTarget != null);
                DefaultConditions.Add("ShotgunRaycastHitsPreviouslyHitGO", () => DefaultConditions["ShotgunRaycastHitsAGameObject"]() &&
                                                                            OngoingSelection != null &&
                                                                            InputTracker.ShotgunRaycastTarget == OngoingSelection.SelectedGameObject);

                DefaultConditions.Add("RaycastHitsAGameObject", () => currentTarget != null);
                DefaultConditions.Add("RaycastHitsSameObjectAsPreviousFrame", () => DefaultConditions["RaycastHitsAGameObject"]() &&
                                                                                   OngoingSelection != null &&
                                                                                   currentTarget == OngoingSelection.SelectedGameObject);
                DefaultConditions.Add("DurationSufficient", () => OngoingSelection != null && OngoingSelection.Duration > MinDuration);
                DefaultConditions.Add("DurationTooLong", () => MaxDuration != null && OngoingSelection != null && OngoingSelection.Duration > MaxDuration);
                DefaultConditions.Add("DurationTooShort", () => MinDuration != null && OngoingSelection != null && OngoingSelection.Duration < MinDuration);
                DefaultConditions.Add("MovedTooFar", () =>
                {
                    return MaxPixelDisplacement != null
                           && OngoingSelection.InputLocations.Count > 0
                           && Vector3.Distance(OngoingSelection.InputLocations[0], CurrentInputLocation()) > MaxPixelDisplacement;
                });

                DefaultConditions.Add("NotSelectablePeriod", () => OngoingSelection != null && !SelectablePeriod);

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
                                   " but there is no such condition in the default dictionary.");
                    return null;
                }


            }
        }

    }
}
