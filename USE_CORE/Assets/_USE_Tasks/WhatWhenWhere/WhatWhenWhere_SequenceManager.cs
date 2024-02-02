using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USE_StimulusManagement;
using WhatWhenWhere_Namespace;

public class WhatWhenWhere_SequenceManager : MonoBehaviour
{
    private GameObject selectedGO;
    private WhatWhenWhere_StimDef selectedSD;
    private List<GameObject> selectedGOs_InSequence = new List<GameObject>();
    private List<GameObject> selectedGOs_All = new List<GameObject>();


    // Stim Evaluation Variables
    private GameObject lastCorrectStimGO;
    private GameObject lastErrorStimGO;
    private GameObject targetStimGO;
    private int sequenceIdx = -1;
    private int totalStimInSequence;
    private float sequenceStartTime;
    private float sequenceDuration;

    private int consecutiveErrors;
    private bool correctSelection;
    private bool retouchCorrect;
    private bool retouchError;
    private bool backTrackError;
    private bool distractorRuleAbidingError;
    private bool ruleAbidingError;
    private bool startedSequence;
    private bool finishedSequence;

    private string selectionClassification;

    public void ManageSelection()
    {
        correctSelection = selectedSD.IsCurrentTarget;

        if (correctSelection)
        {
            // could be selection of the next correct object in the sequence OR return to last correctly selected object after an error

            if (selectedGO == lastCorrectStimGO)
                retouchCorrect = true;

            consecutiveErrors = 0;
            sequenceIdx++;
            lastCorrectStimGO = selectedGO;

            if (sequenceIdx == (totalStimInSequence - 1))
            {
                finishedSequence = true;
                sequenceDuration = Time.time - sequenceStartTime;
            }

            if(!retouchCorrect)
                selectedGOs_InSequence.Add(selectedGO);

        }
        else
        {
            if (selectedGOs_InSequence.Any(go => go.name.Equals(selectedSD.StimName)))
            {
                backTrackError = true;
                if (selectedGO == lastCorrectStimGO && consecutiveErrors == 0)
                    retouchError = true;
            }

            else
            {
                if (selectedSD.IsDistractor)
                    distractorRuleAbidingError = true;
                else
                    ruleAbidingError = true;
            }
            
            consecutiveErrors++;
            if(consecutiveErrors == 1)
                sequenceIdx--;


        }
        selectedGOs_All.Add(selectedGO);
    }

    public string DetermineErrorType()
    {
        string selectionType = "";
        if (lastErrorStimGO != null && lastErrorStimGO == selectedGO && !correctSelection) // Checks for Perseverative Error
        {
            if (backTrackError)
                selectionType = "perseverativeBackTrackError";
            else if (retouchError)
                selectionType = "perseverativeRetouchError";
            else if (ruleAbidingError)
                selectionType = "perseverativeRuleAbidingError";
            else if (distractorRuleAbidingError)
                selectionType = "perseverativeDistractorRuleAbidingError";
        
        }
        else
        {
            if (retouchCorrect)
                selectionType = "retouchCorrect";
            else if (correctSelection)
                selectionType = "correctSelection";
            else if (backTrackError)
                selectionType = "backTrackError";
            else if (retouchError)
                selectionType = "retouchError";
            else if (ruleAbidingError)
                selectionType = "ruleAbidingError";
            else if (distractorRuleAbidingError)
                selectionType = "distractorRuleAbidingError";
        }

        if (distractorRuleAbidingError || ruleAbidingError || backTrackError || retouchError)
            lastErrorStimGO = selectedGO;

        return selectionType;
    }
    public void SetSelectedGO(GameObject go)
    {
        selectedGO = go;
    }
    public GameObject GetSelectedGO()
    {
        return selectedGO;
    }
    public void SetSelectedSD(WhatWhenWhere_StimDef sd)
    {
        selectedSD = sd;
    }
    public WhatWhenWhere_StimDef GetSelectedSD()
    {
        return selectedSD;
    }
 
    public void SetStartedSequence(bool isSequenceStarted)
    {
        startedSequence = isSequenceStarted;
    }
    public bool GetStartedSequence()
    {
        return startedSequence;
    }
    public bool GetFinishedSequence()
    {
        return finishedSequence;
    }
    public void SetSequenceStartTime(float time)
    {
        sequenceStartTime = time;
    }
    public int GetConsecutiveErrorCount()
    {
        return consecutiveErrors;
    }
    public void ResetSelectionClassifications()
    {
        correctSelection = false;
        retouchCorrect = false;
        retouchError = false;
        backTrackError = false;
        distractorRuleAbidingError = false;
        ruleAbidingError = false;
    }
    public void ResetSequenceManagerVariables()
    {
        // Stim Evaluation Variables
        lastCorrectStimGO = null;
        lastErrorStimGO = null;
        targetStimGO = null;
        
        sequenceIdx = -1;
        totalStimInSequence = 0;
        sequenceStartTime = 0;
        sequenceDuration = 0;

        startedSequence = false;
        finishedSequence = false;

        ResetSelectionClassifications();
    }

    public void AssignStimClassifiers(int[] correctObjectTouchOrder, StimGroup searchStims, StimGroup distractorStims)
    {
        targetStimGO = null;

            //find which stimulus is currently target
            int correctIndex = correctObjectTouchOrder[sequenceIdx] - 1;

            for (int iStim = 0; iStim < searchStims.stimDefs.Count; iStim++)
            {
                WhatWhenWhere_StimDef sd = (WhatWhenWhere_StimDef)searchStims.stimDefs[iStim];
                if (iStim == correctIndex)
                {
                    sd.IsCurrentTarget = true;
                    targetStimGO = sd.StimGameObject;
                }
                else
                    sd.IsCurrentTarget = false;
            }

            for (int iDist = 0; iDist < distractorStims.stimDefs.Count; ++iDist)
            {
                WhatWhenWhere_StimDef sd = (WhatWhenWhere_StimDef)distractorStims.stimDefs[iDist];
                sd.IsDistractor = true;
            }

        totalStimInSequence = correctObjectTouchOrder.Length;
        
    }

    public GameObject GetTargetStimGO()
    {
        return targetStimGO;
    }

    public List<GameObject> GetAllSelectedGOs()
    {
        return selectedGOs_All;
    }
    public int GetSeqIdx()
    {
        return sequenceIdx;
    }   
    public void SetSeqIdx(int seqIdx)
    {
        sequenceIdx = seqIdx;
    }
    public void SetConsecutiveErrorCount(int count)
    {
        consecutiveErrors = count;
    }
}
