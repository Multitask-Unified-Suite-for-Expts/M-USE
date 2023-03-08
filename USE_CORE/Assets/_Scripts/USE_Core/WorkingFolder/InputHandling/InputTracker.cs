using UnityEngine;
using USE_Data;
using USE_States;
using USE_StimulusManagement;
using System;

public abstract class InputTracker : MonoBehaviour
{
    private DataController FrameData;
    public GameObject TargetedGameObject; //Is there an Object where they're looking/touching/click?
    public StimDef TargetedStimDef;
    protected int AllowedDisplay = -1;
    public Vector3? CurrentInputScreenPosition; //Where is my Gaze?
    private event EventHandler<EventArgs> SelectionHandler_UpdateTarget;

    public delegate bool IsSelectionPossible();

    //Adds a selection handler (automatically checks for selected objects) to this instance of an InputTracker
    public void AddSelectionHandler<T>(SelectionHandler<T> selectionHandler, State startState, State endState = null) where T : StimDef
    {
        if (endState == null)
            endState = startState;
        
        void CheckForSelection_Handler(object sender, EventArgs e)
        {
            selectionHandler.CheckForSelection(TargetedGameObject, CurrentInputScreenPosition);
        }

        startState.StateInitializationFinished += (object sender, EventArgs e) =>
        {
            SelectionHandler_UpdateTarget += CheckForSelection_Handler; //just calls selectionHandler's UpdateTarget method
            selectionHandler.Start();
        };
        endState.StateTerminationFinished += (object sender, EventArgs e) =>
        {
            SelectionHandler_UpdateTarget -= CheckForSelection_Handler;
            selectionHandler.Stop();
        };
    }

    public void Init(DataController frameData, int allowedDisplay)
    {
        AddFieldsToFrameData(frameData);
        AllowedDisplay = allowedDisplay;
    }

    private void Update()
    {
        CustomUpdate();
        TargetedGameObject = FindCurrentTarget();
        SelectionHandler_UpdateTarget?.Invoke(this, EventArgs.Empty); //if TargetUpdated has any content, run SelectionHandler UpdateTarget method
    }

    public abstract void AddFieldsToFrameData(DataController frameData);

    public abstract GameObject FindCurrentTarget();

    public virtual void CustomUpdate() //Anything a particular tracker needs to track that isn't a target neccessarily (ex: click count). 
    {
    }

}
