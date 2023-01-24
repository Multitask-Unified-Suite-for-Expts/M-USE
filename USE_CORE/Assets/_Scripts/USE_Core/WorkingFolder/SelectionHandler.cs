using System.Collections;
using System.Collections.Generic;
using MazeGame_Namespace;
using UnityEngine;
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
    
    // When a selection has been finalized and meets all the constraints, these will be populated
    public GameObject SelectedGameObject = null;
    public T SelectedStimDef = null;

    public GameObject targetedGameObject;
    public float currentTargetDuration;
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
        currentTargetDuration = 0;
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

    public float GetTargetTouchDuration()
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
    //-------------------------------------- Get/Set Data tracking variables------------------------------------------
    public int GetNumTouchDurationError()
    {
        return NumTouchDurationError;
    }

    public void SetNumTouchDurationError(int val)
    {
        NumTouchDurationError = val;
    }
    public int GetNumNonStimSelection()
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

    public void SetNumNonStimSelection(int val)
    {
        NumNonStimSelection = val;
    }
    
    public void UpdateTarget(GameObject go)
    {
        if (!started) return;
        if (go == null) // Evaluates when the player is not selecting anything
        {
            GetNumNonStimSelection();
            if (targetedGameObject != null) // Evaluates when the player releases the selected object
            {
                bool withinDuration = currentTargetDuration >= MinDuration && 
                                      ((currentTargetDuration <= MaxDuration) || MaxDuration == null);
                if (withinDuration)
                {
                    SelectedGameObject = targetedGameObject;
                    SelectedStimDef = null;
                    if (SelectedGameObject.TryGetComponent(typeof(StimDefPointer), out Component sdPointer))
                        SelectedStimDef = (sdPointer as StimDefPointer).GetStimDef<T>();
                }
                else
                {
                    NumTouchDurationError++;
                    if (currentTargetDuration <= MinDuration) HeldTooShort = true;
                    else if (currentTargetDuration >= MaxDuration) HeldTooLong = true;
                    Debug.Log("Did not select for the appropriate duration"); //ADD FURTHER ERROR FEEDBACK HERE
                }
            }
            targetedGameObject = null;
            currentTargetDuration = 0;
        }
        else
        {
            HeldTooShort = false;
            HeldTooLong = false;
            // Continuously checking the Selected GameObject and resets the currentTargetDuration when the selection changes
            if (go != targetedGameObject) currentTargetDuration = 0;
            else currentTargetDuration += Time.deltaTime;
            targetedGameObject = go;
        }
    }
}
