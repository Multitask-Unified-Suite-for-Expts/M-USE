using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class DataController_Trial_Tutorial4_complete : DataController {
    public ControlLevel_Block_Tutorial4_complete blockLevel;
    public ControlLevel_Trial_Tutorial4_complete trialLevel;

    public override void DefineDataController()
    {
        blockLevel = GameObject.Find("ControlLevels").GetComponent<ControlLevel_Block_Tutorial4_complete>();
        trialLevel = GameObject.Find("ControlLevels").GetComponent<ControlLevel_Trial_Tutorial4_complete>();

        AddDatum("Block", () => blockLevel.currentBlock);
        AddDatum("TrialInBlock", () => trialLevel.trialCount);
        AddDatum("Response", ()=> trialLevel.response);
        AddDatum("Reward", () => trialLevel.reward);
        AddStimData("Stim1", trialLevel.stim1);
        AddStimData("Stim2", trialLevel.stim2);
        AddStateTimingData(trialLevel);
    }

    void AddStimData(string name, GameObject stim)
    {
        AddDatum(name + "_name", () => stim.name);
        AddDatum(name + "_targetStatus", () => stim.tag == "Target" ? 1 : 0);
        AddDatum(name + "_worldX", () => stim.transform.position.x);
        AddDatum(name + "_worldY", () => stim.transform.position.y);
        AddDatum(name + "_worldZ", () => stim.transform.position.z);
        AddDatum(name + "_screenX", () => Camera.main.WorldToScreenPoint(stim.transform.position).x);
        AddDatum(name + "_screenY", () => Camera.main.WorldToScreenPoint(stim.transform.position).y);
    }
}
