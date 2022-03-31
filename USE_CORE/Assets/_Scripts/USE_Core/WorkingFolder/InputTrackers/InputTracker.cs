using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;
using USE_StimulusManagement;

public abstract class InputTracker : MonoBehaviour
{
    private DataController FrameData;
    public GameObject CurrentTargetGameObject;
    public StimDef CurrentTargetStimDef;
    
    private void Init()
    {
        // AddFieldsToFrameData();
    }
    private void Update()
    {
        CurrentTargetGameObject = FindCurrentTarget();
        CurrentTargetStimDef = CurrentTargetGameObject.GetComponent<StimDef>();
    }

    public abstract void AddFieldsToFrameData(DataController frameData);
    public abstract GameObject FindCurrentTarget();
    // public abstract void UpdateFields();
}
