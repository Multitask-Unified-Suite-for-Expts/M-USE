/*
This software is part of the Unified Suite for Experiments (USE).
Information on USE is available at
http://accl.psy.vanderbilt.edu/resources/analysis-tools/unifiedsuiteforexperiments/

Copyright (c) <2018> <Marcus Watson>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

1) The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
2) If this software is used as a component of a project that leads to publication
(e.g. a paper in a scientific journal or a student thesis), the published work
will give appropriate attribution (e.g. citation) to the following paper:
Watson, M.R., Voloh, B., Thomas, C., Hasan, A., Womelsdorf, T. (2018). USE: An
integrative suite for temporally-precise psychophysical experiments in virtual
environments for human, nonhuman, and artificially intelligent agents. BioRxiv:
http://dx.doi.org/10.1101/434944

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using USE_Data;

public class DataController_Trial_Tutorial10_complete : DataController {
    public ControlLevel_Block_Tutorial10_complete blockLevel;
    public ControlLevel_Trial_Tutorial10_complete trialLevel;

    //public override void Update(){
    //    base.Update();
    //}

    public override void DefineDataController()
    {
        blockLevel = GameObject.Find("ControlLevels").GetComponent<ControlLevel_Block_Tutorial10_complete>();
        trialLevel = GameObject.Find("ControlLevels").GetComponent<ControlLevel_Trial_Tutorial10_complete>();
        AddDatum("Block", () => blockLevel.currentBlock);
        AddDatum("TrialInBlock", () => trialLevel.trialInBlock);
        AddDatum("TrialInExperiment", () => trialLevel.trialInExperiment);
        AddDatum("Response", ()=> trialLevel.response);
        AddDatum("Reward", () => trialLevel.reward);
        AddDatum("Stim1_name", () => trialLevel.stims[0].gameObject.name);
        AddDatum("Stim1_targetStatus", () => trialLevel.stims[0].gameObject.tag == "Target" ? 1 : 0);
        AddDatum("Stim1_shape_id", () => trialLevel.stims[0].featureValues[0]);
        AddDatum("Stim1_color_id", () => trialLevel.stims[0].featureValues[1]);
        AddDatum("Stim1_location_id", () => trialLevel.stims[0].featureValues[2]);
        AddDatum("Stim1_worldX", () => trialLevel.stims[0].gameObject.transform.position.x);
        AddDatum("Stim1_worldY", () => trialLevel.stims[0].gameObject.transform.position.y);
        AddDatum("Stim1_worldZ", () => trialLevel.stims[0].gameObject.transform.position.z);
        AddDatum("Stim1_screenX", () => Camera.main.WorldToScreenPoint(trialLevel.stims[0].gameObject.transform.position).x);
        AddDatum("Stim1_screenY", () => Camera.main.WorldToScreenPoint(trialLevel.stims[0].gameObject.transform.position).y);
        AddDatum("Stim2_name", () => trialLevel.stims[1].gameObject.name);
        AddDatum("Stim2_targetStatus", () => trialLevel.stims[1].gameObject.tag == "Target" ? 1 : 0);
        AddDatum("Stim2_shape_id", () => trialLevel.stims[1].featureValues[0]);
        AddDatum("Stim2_color_id", () => trialLevel.stims[1].featureValues[1]);
        AddDatum("Stim2_location_id", () => trialLevel.stims[1].featureValues[2]);
        AddDatum("Stim2_worldX", () => trialLevel.stims[1].gameObject.transform.position.x);
        AddDatum("Stim2_worldY", () => trialLevel.stims[1].gameObject.transform.position.y);
        AddDatum("Stim2_worldZ", () => trialLevel.stims[1].gameObject.transform.position.z);
        AddDatum("Stim2_screenX", () => Camera.main.WorldToScreenPoint(trialLevel.stims[1].gameObject.transform.position).x);
        AddDatum("Stim2_screenY", () => Camera.main.WorldToScreenPoint(trialLevel.stims[1].gameObject.transform.position).y);
        AddStateTimingData(trialLevel, new string[] { "Duration", "StartFrame", "EndFrame" });
    }
}
