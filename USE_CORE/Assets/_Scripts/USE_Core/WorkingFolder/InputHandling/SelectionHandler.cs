using UnityEngine;
using USE_StimulusManagement;
using USE_States;

// Only min duration means that selection is finished once min duration is met
// Both min and max duration means that selection is finished once its let go
public class SelectionHandler<T> where T : StimDef
{
    public float MinDuration = 0;
    public float? MaxDuration = null;

    public bool SelectionTooShort = false;
    public bool SelectionTooLong = false;

    private int NumNonStimSelection = 0;
    private int NumTouchDurationError = 0;

    public float? MaxMoveDistance = 20;
    
    // When a selection has been finalized and meets all the constraints, these will be populated
    public GameObject SelectedGameObject = null;
    public T SelectedStimDef = null;
    
    public float? CurrentTargetDuration;
    public Vector3? CurrentInputScreenPosition;
    private Vector2? CurrentInputScreenPositionPix;
    public bool MovedPastMaxDistance;
    private bool SelectionHandlerStarted;
    public Vector2? SelectionStartPosition;

    // public PossibleSelection ongoingSelection;


    private void NullSelection()
    {
        SelectionStartPosition = null;
        CurrentTargetDuration = null;
        SelectedGameObject = null;
        SelectedStimDef = null;
        SelectionTooLong = false;
        SelectionTooShort = false;
    }

    private void InitSelection()
    {
        SelectionStartPosition = CurrentInputScreenPositionPix;
        CurrentTargetDuration = 0;
        
    }

    private void CheckSelectionErrors()
    {
        if (Vector2.Distance(CurrentInputScreenPositionPix.Value, SelectionStartPosition.Value) >= MaxMoveDistance)
        {
            //error, moved too far
        }

        if (CurrentTargetDuration >= MaxDuration)
        {
            //error, too long

        }
    }

    private void UpdateSelection()
    {
        
    }
    
    private class PossibleSelection
    {
        public Vector2? SelectionStartPosition;
        public float SelectionStartTime;
        public float SelectionDuration;
        public float? maxDuration;
        public int? maxMoveDistance;
        public GameObject SelectedGameObject;
        public T SelectedStimDef;

        public PossibleSelection(Vector2? inputPos, GameObject go, float? maxDur, int? maxDist)
        {
            SelectionStartPosition = inputPos;
            SelectionStartTime = Time.time;
            SelectionDuration = 0;
            SelectedGameObject = go;
            SelectedStimDef = go?.GetComponent<StimDefPointer>()?.GetStimDef<T>();
            maxDuration = maxDur;
            maxMoveDistance = maxDist;
        }

        public void UpdatePossibleSelection(Vector2? inputPos)
        {
            SelectionDuration += Time.deltaTime;
            if (SelectionDuration >= maxDuration)
            {
                
            }

            if (inputPos != null & Vector2.Distance(inputPos.Value, SelectionStartPosition.Value) >= maxMoveDistance)
            {
                
            }
        }
    }

    public void Start()
    {
        SelectionHandlerStarted = true;
    }

    public void Stop()
    {
        // Resets all values at the end of the state, store counter values at the trial level 
        SelectionHandlerStarted = false;
        SelectedGameObject = null;
        SelectedStimDef = null;
        CurrentTargetDuration = null;
        NumNonStimSelection = 0; 
        NumTouchDurationError = 0; 
    }
// -------------------------------------Evaluate the identity of the selection -------------------------------------
    public bool SelectionMatches(GameObject gameObj) {
        return ReferenceEquals(SelectedGameObject, gameObj);
    }

    public bool SelectionMatches(T stimDef) {
        return ReferenceEquals(SelectedStimDef, stimDef);
    }
    //-------------------------------------- Get/Set touch duration variables ------------------------------------------
    

    public void SetMaxMoveDistance(float distance)
    {
        MaxMoveDistance = distance;
    }
    
    public int UpdateNumNonStimSelection()
    {
        if (InputBroker.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit))
                NumNonStimSelection++;
        }
        return NumNonStimSelection;
    }

    public void ResetNumNonStimSelection()
    {
        NumNonStimSelection = 0;
    }

    public void UpdateSelection(GameObject targetedGO, Vector3? currentLoc,
        BoolDelegate selectionIsPossible = null,
        BoolDelegate selectionCompleteIsPossible = null) //TargetedGO is what they're currently hovering over
    {
        // if (targetedGO == null)
        //     ongoingSelection = null;
        // else
        // {
        //     if (selectionIsPossible())
        //         if (ongoingSelection == null)
        //             ongoingSelection = new PossibleSelection(GetScreenPos(currentLoc), targetedGO, );
        // }
    }
    
    //---------------------------------------------UPDATING SELECTION MANAGEMENT-----------------------------------------
    //Should be checking for a selected GO:
    public void CheckForSelection(GameObject targetedGO, Vector3? currentLoc, BoolDelegate selectionIsPossible = null,
        BoolDelegate selectionCompleteIsPossible = null) //TargetedGO is what they're currently hovering over
    {
        CurrentInputScreenPosition = currentLoc;
        MovedPastMaxDistance = false;
        //SelectedGameObject = null;
        //SelectedStimDef = null;

        if (targetedGO == null)
        {
            SelectionStartPosition = null;
            MovedPastMaxDistance = false;
        }

        // Check if handler has started and if there is a current selection location
        if (!SelectionHandlerStarted || CurrentInputScreenPosition == null) 
            return;


        // Check if selectedGameObject is null and if input location hasn't moved more than max distance:
        if (SelectedGameObject == null && !MovedPastMaxDistance)
        {
            // Check if there is a targeted game object
            if (targetedGO != null)
            {                
                if (selectionIsPossible == null || selectionIsPossible())
                {
                    //Have they moved too far?
                    if (SelectionStartPosition != null && Vector2.Distance(GetScreenPos(CurrentInputScreenPosition).Value, SelectionStartPosition.Value) > MaxMoveDistance)
                    {
                        MovedPastMaxDistance = true;
                    }
                    //If selection has just started
                    if (targetedGO != SelectedGameObject && CurrentTargetDuration == null)
                    {
                        SelectionTooShort = false;
                        SelectionTooLong = false;

                        CurrentTargetDuration = 0;
                        if (CurrentInputScreenPosition != null)
                            SelectionStartPosition = GetScreenPos(CurrentInputScreenPosition.Value);
                        else
                            SelectionStartPosition = null;
                    }
                    else if (!MovedPastMaxDistance)
                        CurrentTargetDuration += Time.deltaTime;
                }

                // Check if the touch duration is within the appropriate range
                bool withinDuration = CurrentTargetDuration >= MinDuration && (MaxDuration == null || (CurrentTargetDuration <= MaxDuration));

                if (withinDuration && (selectionCompleteIsPossible == null || selectionCompleteIsPossible()))
                {
                    SelectedGameObject = targetedGO;
                    SelectedStimDef = targetedGO?.GetComponent<StimDefPointer>()?.GetStimDef<T>();
                    MovedPastMaxDistance = false;
                    SelectionStartPosition = null;
                }
                else
                {
                    NumTouchDurationError++;                    
                    if (CurrentTargetDuration <= MinDuration) 
                        SelectionTooShort = true;
                    else if (CurrentTargetDuration >= MaxDuration) 
                        SelectionTooLong = true;
                }
            }
            else
            {
                CurrentTargetDuration = null;
                SelectionStartPosition = null;
                MovedPastMaxDistance = false;
            }
        }
    }

    protected Vector2? GetScreenPos(Vector3? worldPos)
    {
        if (worldPos != null)
        {
            Vector3 temp = Camera.main.WorldToScreenPoint(worldPos.Value);
            return new Vector2(temp.x, temp.y);
        }
        else
        {
            return null;
        }
    }
}