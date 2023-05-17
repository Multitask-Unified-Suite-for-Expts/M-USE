using UnityEngine;
using USE_Data;
using USE_States;
using USE_StimulusManagement;
using System;
using System.Collections.Generic;

public abstract class InputTracker : MonoBehaviour
{
    private DataController FrameData;
    public GameObject TargetedGameObject;
    public StimDef TargetedStimDef;
    protected int AllowedDisplay = -1;
    public Vector3? CurrentInputScreenPosition;

    public List<GameObject> ShotgunGoAboveThreshold;
    public GameObject ShotgunModalTarget;
    public GameObject SimpleRaycastTarget;

    public ShotgunRaycast ShotgunRaycast;
    public float ShotgunThreshold;
    // private event EventHandler<EventArgs> SelectionHandler_UpdateTarget;

    public delegate bool IsSelectionPossible();

    //Adds a selection handler (automatically checks for selected objects) to this instance of an InputTracker
    // public void AddSelectionHandler<T>(SelectionHandler<T> selectionHandler, State startState, State endState = null, 
    //     BoolDelegate selectionIsPossible = null, BoolDelegate selectionCompleteIsPossible = null) where T : StimDef
    // {
    //     selectionHandler.MovedPastMaxDistance = false;
    //     selectionHandler.SelectionStartPosition = null;
    //     selectionHandler.SelectedGameObject = null;
    //     if (endState == null)
    //         endState = startState;
    //     
    //     void CheckForSelection_Handler(object sender, EventArgs e)
    //     {
    //         selectionHandler.CheckForSelection(TargetedGameObject, CurrentInputScreenPosition, selectionIsPossible, selectionCompleteIsPossible);
    //     }
    //
    //     startState.StateInitializationFinished += (object sender, EventArgs e) =>
    //     {
    //         SelectionHandler_UpdateTarget += CheckForSelection_Handler; //just calls selectionHandler's UpdateTarget method
    //         selectionHandler.Start();
    //     };
    //     endState.StateTerminationFinished += (object sender, EventArgs e) =>
    //     {
    //         SelectionHandler_UpdateTarget -= CheckForSelection_Handler;
    //         selectionHandler.Stop();
    //     };
    // }

    public void Init(DataController frameData, int allowedDisplay)
    {
        AddFieldsToFrameData(frameData);
        AllowedDisplay = allowedDisplay;

        ShotgunRaycast = GameObject.Find("MiscScripts").GetComponent<ShotgunRaycast>();
    }

    private void Update()
    {
        CustomUpdate();
        FindCurrentTarget();
        /*if (TargetedGameObject.GetComponent<StimDefPointer>() != null)
            TargetedStimDef = TargetedGameObject.GetComponent<StimDefPointer>().StimDef;
        else
            TargetedStimDef = null;*/
        // SelectionHandler_UpdateTarget?.Invoke(this, EventArgs.Empty); //if TargetUpdated has any content, run SelectionHandler UpdateTarget method
    }

    public abstract void AddFieldsToFrameData(DataController frameData);

    public abstract void FindCurrentTarget();

    public virtual void CustomUpdate() //Anything a particular tracker needs to track that isn't a target neccessarily (ex: click count). 
    {
    }
    

}
