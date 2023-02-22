using UnityEngine;
using USE_Data;
using USE_States;
using USE_StimulusManagement;
using System;

public abstract class InputTracker : MonoBehaviour
{
    private DataController FrameData;
    public GameObject CurrentTargetGameObject;
    public StimDef CurrentTargetStimDef;
    protected int AllowedDisplay = -1;

    private event EventHandler<EventArgs> SelectionHandler_UpdateTarget;

    public void AddSelectionHandler<T>(SelectionHandler<T> selectionHandler, State startState, State endState = null) where T : StimDef
    {
        //adds a selection handler (automatically checks for hover and selected objects) to this instance of an InputTracker
        //(mousetracker etc) with a given start and end state
        
        //if it's just one state, don't need to specify end state
        if (endState == null)
            endState = startState;
        
        //assign the selectionhandler's UpdateTarget method to this tracker
        void targetUpdatedHandler(object sender, EventArgs e)
        {
            selectionHandler.UpdateTarget(CurrentTargetGameObject);
        }

        startState.StateInitializationFinished += (object sender, EventArgs e) =>
        {
            SelectionHandler_UpdateTarget += targetUpdatedHandler; //just calls selectionHandler's UpdateTarget method
            selectionHandler.Start();
        };
        endState.StateTerminationFinished += (object sender, EventArgs e) =>
        {
            SelectionHandler_UpdateTarget -= targetUpdatedHandler;
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
        CurrentTargetGameObject = FindCurrentTarget();
        SelectionHandler_UpdateTarget?.Invoke(this, EventArgs.Empty); //if TargetUpdated has any content, run it
                                                      //(run SelectionHandler UpdateTarget method
    }

    public abstract void AddFieldsToFrameData(DataController frameData);
    public abstract GameObject FindCurrentTarget();
    // public abstract void UpdateFields();
    public virtual void CustomUpdate()
    {
    }
}
