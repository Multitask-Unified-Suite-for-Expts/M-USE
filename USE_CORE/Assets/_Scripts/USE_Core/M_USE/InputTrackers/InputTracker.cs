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

    private event EventHandler<EventArgs> TargetUpdated;

    public void AddSelectionHandler<T>(SelectionHandler<T> selectionHandler, State startState, State endState = null) where T : StimDef
    {
        if (endState == null) {
            endState = startState;
        }
        void targetUpdatedHandler(object sender, EventArgs e) {
            selectionHandler.UpdateTarget(CurrentTargetGameObject);
        }

        startState.StateInitializationFinished += (object sender, EventArgs e) =>
        {
            TargetUpdated += targetUpdatedHandler;
            selectionHandler.Start();
        };
        endState.StateTerminationFinished += (object sender, EventArgs e) =>
        {
            TargetUpdated -= targetUpdatedHandler;
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
        TargetUpdated?.Invoke(this, EventArgs.Empty);
    }

    public abstract void AddFieldsToFrameData(DataController frameData);
    public abstract GameObject FindCurrentTarget();
    // public abstract void UpdateFields();
    public virtual void CustomUpdate()
    {
    }
}
