using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class DataController_Block_Tutorial5_complete : DataController {
    public override void DefineDataController()
    {
        ControlLevel_Block_Tutorial5_complete blockLevel = GameObject.Find("ControlLevels").GetComponent<ControlLevel_Block_Tutorial5_complete>();
        ControlLevel_Trial_Tutorial5_complete trialLevel = GameObject.Find("ControlLevels").GetComponent<ControlLevel_Trial_Tutorial5_complete>();
        AddDatum("Block", () => blockLevel.currentBlock);
        AddDatum("FirstTrial", () => blockLevel.firstTrial);
        AddDatum("LastTrial", () => blockLevel.lastTrial);
        AddDatum("Proportion Correct", () => trialLevel.numCorrect / trialLevel.numTrials);
        AddDatum("Proportion Reward", () => trialLevel.numReward / trialLevel.numTrials);
        AddStateTimingData(blockLevel, new string[] { "Duration", "StartFrame", "EndFrame" });
    }
}
