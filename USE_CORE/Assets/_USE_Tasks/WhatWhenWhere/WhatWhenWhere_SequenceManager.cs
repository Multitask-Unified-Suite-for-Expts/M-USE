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
    private List<WhatWhenWhere_StimDef> selectedSDs_InSequence = new List<WhatWhenWhere_StimDef>();
    private List<WhatWhenWhere_StimDef> selectedSDs_All = new List<WhatWhenWhere_StimDef>();
    private List<GameObject> selectedGOs_All = new List<GameObject>();
    private List<string> selectionClassifications_All = new List<string>();


    // Stim Evaluation Variables
    private WhatWhenWhere_StimDef lastCorrectStimSD;
    private GameObject lastCorrectStimGO;
    private GameObject lastErrorStimGO;
    private GameObject targetStimGO;
    private int sequenceIdx;
    private int totalStimInSequence;
    private float sequenceStartTime;
    private float sequenceDuration;
    private int consecutiveErrors;

    private bool selectedFirstStimInSequence;
    private bool correctSelection;
    private bool retouchCorrect;
    private bool retouchError;
    private bool backTrackError;
    private bool distractorRuleAbidingError;
    private bool ruleAbidingError;
    private bool ruleBreakingError;
    private bool startedSequence;
    private bool finishedSequence;

    private string selectionClassification;

    public void ManageSelection()
    {
        correctSelection = selectedSD.IsCurrentTarget;
        if (correctSelection)
        {
            // could be selection of the next correct object in the sequence OR return to last correctly selected object after an error
            if (!selectedFirstStimInSequence)
                selectedFirstStimInSequence = true;

            if (selectedSD == lastCorrectStimSD)
                retouchCorrect = true;

            consecutiveErrors = 0;
            sequenceIdx++;

            lastCorrectStimSD = selectedSD;
            lastCorrectStimGO = selectedGO;

            if (sequenceIdx == totalStimInSequence)
            {
                finishedSequence = true;
                sequenceDuration = Time.time - sequenceStartTime;
            }

            if(!retouchCorrect)
                selectedSDs_InSequence.Add(selectedSD);

        }
        else
        {
            if (selectedSDs_InSequence.Any(sd => sd.StimIndex.Equals(selectedSD.StimIndex)))
            {
                if (selectedSD.StimIndex == lastCorrectStimSD.StimIndex)
                    retouchError = true;
                else
                    backTrackError = true;
            }
            else if (consecutiveErrors > 0)
                ruleBreakingError = true;
            else
            {
                if (selectedSD.IsDistractor)
                    distractorRuleAbidingError = true;
                else
                    ruleAbidingError = true;
            }
            
            if(!retouchError)
                consecutiveErrors++;
            if(consecutiveErrors == 1 && sequenceIdx != 0)
                sequenceIdx--;


        }
        selectedSDs_All.Add(selectedSD);
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
            else if (ruleBreakingError)
                selectionType = "perseverativeRuleBreakingError";
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
            else if (ruleBreakingError)
                selectionType = "ruleBreakingError";
            else if (ruleAbidingError)
                selectionType = "ruleAbidingError";
            else if (distractorRuleAbidingError)
                selectionType = "distractorRuleAbidingError";
        }

        if (distractorRuleAbidingError || ruleAbidingError || backTrackError || retouchError || ruleBreakingError)
            lastErrorStimGO = selectedGO;

        selectionClassifications_All.Add(selectionType);
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
    public WhatWhenWhere_StimDef GetLastCorrectStimSD()
    {
        return lastCorrectStimSD;
    }    
    
    public GameObject GetLastCorrectStimGO()
    {
        return lastCorrectStimGO;
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
    public bool GetSelectedFirstStimInSequence()
    {
        return selectedFirstStimInSequence;
    }
    public void ResetSelectionClassifications()
    {
        correctSelection = false;
        retouchCorrect = false;
        retouchError = false;
        backTrackError = false;
        distractorRuleAbidingError = false;
        ruleAbidingError = false;
        ruleBreakingError = false;

        targetStimGO = null;
        selectedGO = null;
        selectedSD = null;
    }
    public void ResetSequenceManagerVariables()
    {
        // Stim Evaluation Variables
        lastCorrectStimSD = null;
        lastCorrectStimGO = null;
        lastErrorStimGO = null;
        targetStimGO = null;
        selectedGO = null;
        selectedSD = null;

        selectedSDs_InSequence.Clear();
        selectedSDs_All.Clear();
        selectedGOs_All.Clear();
        selectionClassifications_All.Clear();

        sequenceIdx = 0;
        totalStimInSequence = 0;
        sequenceStartTime = 0;
        sequenceDuration = 0;

        startedSequence = false;
        finishedSequence = false;
        selectedFirstStimInSequence = false;

        ResetSelectionClassifications();
    }

    public void AssignStimClassifiers(int[] correctObjectTouchOrder, StimGroup searchStims, StimGroup distractorStims)
    {
        targetStimGO = null;

        int correctIndex = correctObjectTouchOrder[sequenceIdx];

        foreach(WhatWhenWhere_StimDef stim in searchStims.stimDefs.Cast<WhatWhenWhere_StimDef>())
        {
            if (stim.StimIndex == correctIndex)
            {
                stim.IsCurrentTarget = true;
                targetStimGO = stim.StimGameObject;
            }
            else
                stim.IsCurrentTarget = false;
        }

        foreach (WhatWhenWhere_StimDef stim in distractorStims.stimDefs.Cast<WhatWhenWhere_StimDef>())
            stim.IsDistractor = true;

        totalStimInSequence = correctObjectTouchOrder.Length;

    }

    public GameObject GetTargetStimGO()
    {
        return targetStimGO;
    }

    public List<WhatWhenWhere_StimDef> GetAllSelectedSDs()
    {
        return selectedSDs_All;
    }    
    public List<GameObject> GetAllSelectedGOs()
    {
        return selectedGOs_All;
    } 
    public List<string> GetAllSelectionClassifications()
    {
        return selectionClassifications_All;
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
