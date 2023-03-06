using System.Collections;
using System.Collections.Generic;
using MazeGame_Namespace;
using UnityEngine;
using UnityEngine.UIElements;
using USE_StimulusManagement;

// Only min duration means that selection is finished once min duration is met
// Both min and max duration means that selection is finished once its let go
public class SelectionHandler<T> where T : StimDef
{

    private float MinDuration = 0;
    private float? MaxDuration = null;

    private bool HeldTooShort = false;
    private bool HeldTooLong = false;

    private int NumNonStimSelection = 0;
    private int NumTouchDurationError = 0;

    private float? MaxMoveDistance = 20;
    
    
    // When a selection has been finalized and meets all the constraints, these will be populated
    public GameObject SelectedGameObject = null;
    public T SelectedStimDef = null;
    

    public GameObject targetedGameObject;
    public float? currentTargetDuration;
    public float currentHoldDuration;
    public Vector3? CurrentSelectionLocation;
    private Vector2 currentSelectionLocationPix;
    private Vector2 startingPosition;
    private bool isMouseDragging;
    private bool started;
    

    public void Start()
    {
        started = true;
    }

    public void Stop()
    {
        // Resets all values at the end of the state, store counter values at the trial level 
        started = false;
        SelectedGameObject = null;
        SelectedStimDef = null;
        targetedGameObject = null;
        currentTargetDuration = null;
        currentHoldDuration = 0;
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
        return currentTargetDuration;
    }

    public bool GetHeldTooLong()
    {
        return HeldTooLong;
    }
    public bool GetHeldTooShort()
    {
        return HeldTooShort;
    }
    public void SetHeldTooLong(bool heldTooLong)
    {
        HeldTooLong = heldTooLong;
    }
    public void SetHeldTooShort(bool heldTooShort)
    {
        HeldTooShort = heldTooShort;
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
    public void UpdateTarget(GameObject selectedGameObject)
    {
        // Check if input has started and if there is a current selection location
        if (!started || CurrentSelectionLocation == null) 
        {
            return;
        }
       
        // Check if the left mouse button is being pressed 
        if (InputBroker.GetMouseButtonDown(0))
        {
            // Set the starting position of the touch, regardless of if on stim or not
            startingPosition = GetScreenPos(CurrentSelectionLocation.Value);
            currentHoldDuration = 0;
        }

        if (InputBroker.GetMouseButton(0))
        { //if the mouse button is still being held 
            currentHoldDuration += Time.deltaTime;
        }
        
        // Check if selectedGameObject is null and if the player is not dragging the mouse
        if (selectedGameObject == null && !isMouseDragging)
        {
            // Check if there is a targeted game object
            if (targetedGameObject != null)
            {
                // Check if the touch duration is within the appropriate range
                bool withinDuration = currentTargetDuration >= MinDuration && 
                                      ((currentTargetDuration <= MaxDuration) || MaxDuration == null);

                if (withinDuration)
                {
                    // Set the selected game object and check if it has stimDefPointer to set
                    SelectedGameObject = targetedGameObject;
                    SelectedStimDef = targetedGameObject?.GetComponent<StimDefPointer>()?.GetStimDef<T>();
                }
                else
                {
                    // Increment the number of touch duration errors and set flags accordingly
                    NumTouchDurationError++;
                    
                    if (currentTargetDuration <= MinDuration) 
                        HeldTooShort = true;
                    else if (currentTargetDuration >= MaxDuration) 
                        HeldTooLong = true;

                    Debug.Log("Did not select for the appropriate duration"); 
                }

                targetedGameObject = null;
                currentTargetDuration = null;
            }
            return;
        }

        HeldTooShort = false;
        HeldTooLong = false;

        
        if (Vector2.Distance(GetScreenPos(CurrentSelectionLocation.Value), startingPosition) > MaxMoveDistance)
        {
            // Set the isMouseDragging flag and check if the left mouse button is released
            isMouseDragging = true;
            
            if(InputBroker.GetMouseButtonUp(0))
            {
                // Reset the isMouseDragging flag and targeted game object
                isMouseDragging = false;
                targetedGameObject = null;
            }
        }
        // Check if selectedGameObject is not the same as the targeted game object and the mouse is not being dragged
        if (selectedGameObject != targetedGameObject && !isMouseDragging)
        {
            currentTargetDuration = 0;
            startingPosition = GetScreenPos(CurrentSelectionLocation.Value);
        }
        else if (!isMouseDragging)
        {
            // Increment the touch duration if the mouse is not being dragged
            currentTargetDuration += Time.deltaTime;
        }
        
        // Set targeted game object to selectedGameObject
        targetedGameObject = selectedGameObject;
    }

    private Vector2 GetScreenPos(Vector3 worldPos)
    {
        Vector3 temp = Camera.main.WorldToScreenPoint(worldPos);
        return new Vector2(temp.x, temp.y);
    }
    
}