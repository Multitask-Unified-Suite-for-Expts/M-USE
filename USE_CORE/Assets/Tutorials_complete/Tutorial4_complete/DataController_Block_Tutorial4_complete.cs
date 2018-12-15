using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class DataController_Block_Tutorial4_complete : DataController {
    public ControlLevel_Block_Tutorial4_complete blockLevel;
    public ControlLevel_Trial_Tutorial4_complete trialLevel;

    public override void DefineDataController()
    {
        AddDatum("Block", () => blockLevel.currentBlock);
        AddDatum("FirstTrial", () => blockLevel.firstTrial);
        AddDatum("LastTrial", () => blockLevel.lastTrial);
        AddDatum("Proportion Correct", () => trialLevel.numCorrect / trialLevel.numTrials);
        AddDatum("Proportion Reward", () => trialLevel.numReward / trialLevel.numTrials);
        AddStateTimingData(blockLevel, new string[] { "Duration", "StartFrame", "EndFrame" });
    }
}
