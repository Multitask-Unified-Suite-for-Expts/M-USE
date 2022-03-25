using UnityEngine;
using USE_StimulusManagement;

// Only min duration means that selection is finished once min duration is met
// Both min and max duration means that selection is finished once its let go
public abstract class SelectionHandler<T> : MonoBehaviour where T : StimDef
{
    public string SelectionType;
    public float MinDuration;
    public float? MaxDuration;

    // When a selection has been finalized and meets all the constraints, these will be populated
    public GameObject SelectedGameObject = null;
    public T SelectedStimDef = null;

    private GameObject currentlySelectedGameObject;
    private float currentSelectionDuration;
    private bool started;

    public void Start()
    {
        started = true;
    }

    public void Stop()
    {
        started = false;
    }

    private void Update()
    {
        if (!started) return;

        GameObject go = CheckSelection();
        if (go == null)
        {
            if (currentlySelectedGameObject != null)
            {
                // Released the selected object
                bool withinDuration = currentSelectionDuration >= MinDuration && (currentSelectionDuration <= (MaxDuration ?? float.PositiveInfinity));
                if (withinDuration) SelectedGameObject = currentlySelectedGameObject;
            }
            currentlySelectedGameObject = null;
            currentSelectionDuration = 0;
        }
        else
        {
            // Do we allow them to change their selection?
            if (go != currentlySelectedGameObject)
                currentSelectionDuration = 0;
            else
                currentSelectionDuration += Time.deltaTime;

            currentlySelectedGameObject = go;
            // if (go.GetComponent<StimDefPointer>())
            //     CurrentlySelectedStimDef = go.GetComponent<StimDefPointer>().GetStimDef<T>();
            if (MaxDuration == null && currentSelectionDuration >= MinDuration)
                SelectedGameObject = go;
        }
    }

    public abstract GameObject CheckSelection();
}
