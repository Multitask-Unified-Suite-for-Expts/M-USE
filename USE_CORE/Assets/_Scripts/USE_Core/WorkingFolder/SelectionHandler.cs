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

    private GameObject targetedGameObject;
    private float currentTargetDuration;
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
            if (targetedGameObject != null)
            {
                // Released the selected object
                bool withinDuration = currentTargetDuration >= MinDuration && (currentTargetDuration <= (MaxDuration ?? float.PositiveInfinity));
                if (withinDuration) SelectedGameObject = targetedGameObject;
            }
            targetedGameObject = null;
            currentTargetDuration = 0;
        }
        else
        {
            // Do we allow them to change their selection?
            if (go != targetedGameObject)
                currentTargetDuration = 0;
            else
                currentTargetDuration += Time.deltaTime;

            targetedGameObject = go;
            // if (go.GetComponent<StimDefPointer>())
            //     CurrentlySelectedStimDef = go.GetComponent<StimDefPointer>().GetStimDef<T>();
            if (MaxDuration == null && currentTargetDuration >= MinDuration)
                SelectedGameObject = go;
        }
    }

    public abstract GameObject CheckSelection();
}
