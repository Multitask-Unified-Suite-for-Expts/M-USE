using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;

public abstract class SelectionHandler<T> : MonoBehaviour where T:StimDef
{
    public string SelectionType;
    public float MinDuration;
    public float? MaxDuration;
    public T CurrentlySelectedStimDef;
    public GameObject CurrentlySelectedGameObject;
    public Event SelectionFinished;
    public bool ObjectCurrentlySelected, ObjectSelectionFinished;
    private float CurrentSelectionDuration;
    

    private void Update()
    {
        GameObject go = CheckSelection();
        if (go == null)
        {
            if (CurrentlySelectedGameObject != null && MaxDuration != null && CurrentSelectionDuration < MaxDuration.Value && CurrentSelectionDuration > MinDuration)
            {
                ObjectSelectionFinished = true;
                //SelectionFinished. call the event 
            }
            CurrentlySelectedGameObject = null;
            CurrentlySelectedStimDef = null;
            ObjectCurrentlySelected = false;
            ObjectSelectionFinished = false;
            CurrentSelectionDuration = 0;
        }
        else
        {
            if (go != CurrentlySelectedGameObject)
                CurrentSelectionDuration = 0;
            else
                CurrentSelectionDuration += Time.deltaTime;

            CurrentlySelectedGameObject = go;
            if(go.GetComponent<StimDefPointer>())
                CurrentlySelectedStimDef = go.GetComponent<StimDefPointer>().GetStimDef<T>();
            ObjectCurrentlySelected = true;
            if (MaxDuration == null && CurrentSelectionDuration > MinDuration)
            {
                ObjectSelectionFinished = true;
                //SelectionFinished. call the event 
            }

        }
    }
    
    public abstract GameObject CheckSelection();
}
