using System;
using System.Collections;
using System.Collections.Generic;
using MazeGame_Namespace;
using UnityEngine;
using UnityEngine.UIElements;
using USE_StimulusManagement;
using USE_States;

// Only min duration means that selection is finished once min duration is met
// Both min and max duration means that selection is finished once its let go
public class SelectionHandler<T> where T : StimDef
{
    private float MinDuration = 0;
    private float? MaxDuration = null;

    private bool SelectionTooShort = false;
    private bool SelectionTooLong = false;

    private int NumNonStimSelection = 0;
    private int NumTouchDurationError = 0;

    public float? MaxMoveDistance = 20;
    
    // When a selection has been finalized and meets all the constraints, these will be populated
    public GameObject SelectedGameObject = null;
    public T SelectedStimDef = null;
    
    public float? CurrentTargetDuration;
    public Vector3? CurrentInputScreenPosition;
    private Vector2 CurrentInputScreenPositionPix;
    public bool MovedPastMaxDistance;
    private bool SelectionHandlerStarted;
    public Vector2? SelectionStartPosition;


    public void Start()
    {
        SelectionHandlerStarted = true;
    }

    public void Stop()
    {
        // Resets all values at the end of the state, store counter values at the trial level 
        SelectionHandlerStarted = false;
        SelectedGameObject = null;
        SelectedStimDef = null;
        CurrentTargetDuration = null;
        NumNonStimSelection = 0; 
        NumTouchDurationError = 0; 
    }
// -------------------------------------Evaluate the identity of the selection -------------------------------------
    public bool SelectionMatches(GameObject gameObj) {
        return ReferenceEquals(SelectedGameObject, gameObj);
    }

    public bool SelectionMatches(T stimDef) {
        return ReferenceEquals(SelectedStimDef, stimDef);
    }
    //-------------------------------------- Get/Set touch duration variables ------------------------------------------
    public void SetMinTouchDuration(float minDuration)
    {
        MinDuration = minDuration;
    }

    public float GetMinTouchDuration()
    {
        return MinDuration;
    }

    public void SetMaxTouchDuration(float maxDuration)
    {
        MaxDuration = maxDuration;
    }

    public float? GetMaxTouchDuration()
    {
        return MaxDuration;
    }

    public float? GetTargetTouchDuration()
    {
        return CurrentTargetDuration;
    }

    public bool GetSelectionTooLong()
    {
        return SelectionTooLong;
    }
    public bool GetSelectionTooShort()
    {
        return SelectionTooShort;
    }
    public void SetSelectionTooLong(bool heldTooLong)
    {
        SelectionTooLong = heldTooLong;
    }
    public void SetSelectionTooShort(bool heldTooShort)
    {
        SelectionTooShort = heldTooShort;
    }

    public void SetMaxMoveDistance(float distance)
    {
        MaxMoveDistance = distance;
    }
    //-------------------------------------- Get/Set Data tracking variables------------------------------------------
    public int GetNumTouchDurationError()
    {
        return NumTouchDurationError;
    }

    public void SetNumTouchDurationError(int val)
    {
        NumTouchDurationError = val;
    }
    public int UpdateNumNonStimSelection()
    {
        if (InputBroker.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit))
                NumNonStimSelection++;
        }
        return NumNonStimSelection;
    }

    public void ResetNumNonStimSelection()
    {
        NumNonStimSelection = 0;
    }
    
    //---------------------------------------------UPDATING SELECTION MANAGEMENT-----------------------------------------
    //Should be checking for a selected GO:
    public void CheckForSelection(GameObject targetedGO, Vector3? currentLoc, BoolDelegate selectionIsPossible = null,
        BoolDelegate selectionCompleteIsPossible = null) //TargetedGO is what they're currently hovering over
    {
        CurrentInputScreenPosition = currentLoc;
        MovedPastMaxDistance = false;
        //SelectedGameObject = null;
        //SelectedStimDef = null;

        if (targetedGO == null)
        {
            SelectionStartPosition = null;
            MovedPastMaxDistance = false;
        }

        // Check if handler has started and if there is a current selection location
        if (!SelectionHandlerStarted || CurrentInputScreenPosition == null) 
            return;


        // Check if selectedGameObject is null and if input location hasn't moved more than max distance:
        if (SelectedGameObject == null && !MovedPastMaxDistance)
        {
            // Check if there is a targeted game object
            if (targetedGO != null)
            {
                SelectionTooShort = false;
                SelectionTooLong = false;
                
                if (selectionIsPossible == null || selectionIsPossible())
                {
                    //Have they moved too far?
                    if (SelectionStartPosition != null && Vector2.Distance(GetScreenPos(CurrentInputScreenPosition.Value), SelectionStartPosition.Value) > MaxMoveDistance)
                    {
                        Debug.Log(Time.frameCount + ": moved too far");
                        MovedPastMaxDistance = true;
                    }
                    //If selection has just started
                    if (targetedGO != SelectedGameObject && CurrentTargetDuration == null)
                    {
                        CurrentTargetDuration = 0;
                        if (CurrentInputScreenPosition != null)
                            SelectionStartPosition = GetScreenPos(CurrentInputScreenPosition.Value);
                        else
                            SelectionStartPosition = null;
                    }
                    else if (!MovedPastMaxDistance)
                        CurrentTargetDuration += Time.deltaTime;
                }

                // Check if the touch duration is within the appropriate range
                bool withinDuration = CurrentTargetDuration >= MinDuration && ((CurrentTargetDuration <= MaxDuration) || MaxDuration == null);

                if (withinDuration && (selectionCompleteIsPossible == null || selectionCompleteIsPossible()))
                {
                    SelectedGameObject = targetedGO;
                    SelectedStimDef = targetedGO?.GetComponent<StimDefPointer>()?.GetStimDef<T>();
                    MovedPastMaxDistance = false;
                    SelectionStartPosition = null;
                }
                else
                {
                    NumTouchDurationError++;                    
                    if (CurrentTargetDuration <= MinDuration) 
                        SelectionTooShort = true;
                    else if (CurrentTargetDuration >= MaxDuration) 
                        SelectionTooLong = true;
                }
            }
            else
            {
                CurrentTargetDuration = null;
                SelectionStartPosition = null;
                MovedPastMaxDistance = false;
            }
        }
    }

    protected Vector2 GetScreenPos(Vector3 worldPos)
    {
        Vector3 temp = Camera.main.WorldToScreenPoint(worldPos);
        return new Vector2(temp.x, temp.y);
    }
}