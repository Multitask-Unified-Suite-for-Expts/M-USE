using UnityEngine;
using USE_Data;
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


    public delegate bool IsSelectionPossible();


    public void Awake()
    {
        ShotgunRaycast = GameObject.Find("MiscScripts").GetComponent<ShotgunRaycast>();
    }
    public void Init(DataController frameData, int allowedDisplay)
    {
        AddFieldsToFrameData(frameData);
        AllowedDisplay = allowedDisplay;
    }

    private void Update()
    {
        CustomUpdate();
        FindCurrentTarget();
    }

    public abstract void AddFieldsToFrameData(DataController frameData);

    public abstract void FindCurrentTarget();

    public virtual void CustomUpdate() //Anything a particular tracker needs to track that isn't a target neccessarily (ex: click count). 
    {
    }
    

}
