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

    private EventHandler targetUpdated;

    public void AddSelectionHandler<T>(State startState, State endState, SelectionHandler<T> selectionHandler) where T : StimDef
    {
        void targetUpdatedHandler(object sender, EventArgs e) {
            selectionHandler.UpdateTarget(CurrentTargetGameObject);
        }

        startState.StateInitializationFinished += (object sender, EventArgs e) =>
        {
            targetUpdated += targetUpdatedHandler;
            selectionHandler.Start();
        };
        endState.StateTerminationFinished += (object sender, EventArgs e) =>
        {
            targetUpdated -= targetUpdatedHandler;
            selectionHandler.Stop();
        };
    }

    private void Init()
    {
        // AddFieldsToFrameData();
    }

    private void Update()
    {
        CurrentTargetGameObject = FindCurrentTarget();
        CurrentTargetStimDef = CurrentTargetGameObject.GetComponent<StimDef>();
        targetUpdated.Invoke(this, EventArgs.Empty);
    }

    public abstract void AddFieldsToFrameData(DataController frameData);
    public abstract GameObject FindCurrentTarget();
    // public abstract void UpdateFields();
}
