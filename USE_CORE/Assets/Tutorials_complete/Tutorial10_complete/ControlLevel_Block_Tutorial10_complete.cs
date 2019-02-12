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
using UnityEngine.UI;
using USE_States;

public class ControlLevel_Block_Tutorial10_complete: ControlLevel
{
    public GameObject fbText, fbPanel;

    public int numBlocks, currentBlock, numTrials, firstTrial, lastTrial;
    [HideInInspector]
    public DataController_Block_Tutorial10_complete blockData;

    public System.Action<bool> OnBlockEnd, OnAllBlocksEnd;

    public bool skipTexts;

    void CallbackTrialEnd(bool ranAllTrials){
        if(OnBlockEnd != null){
            // Debug.Log("Calling OnBlockEnd");
            OnBlockEnd.Invoke(ranAllTrials);
        }
        if(OnAllBlocksEnd != null)
            OnAllBlocksEnd.Invoke(ranAllTrials && currentBlock + 1 >= numBlocks);
    }

    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State runTrials = new State("RunTrials");
        State blockFb = new State("BlockFB");

        AddActiveStates(new List<State> { runTrials, blockFb });

        ControlLevel_Trial_Tutorial10_complete trialLevel = transform.GetComponent<ControlLevel_Trial_Tutorial10_complete>();
        trialLevel.OnTrialEnd += CallbackTrialEnd;

        runTrials.AddChildLevel(trialLevel);
        runTrials.AddInitializationMethod(() =>
        {
            trialLevel.numTrials = numTrials;
            trialLevel.trialInBlock = 1;
            trialLevel.numCorrect = 0;
            trialLevel.numReward = 0;
            firstTrial = trialLevel.trialInExperiment;

            trialLevel.SetTargetFeature();

            ResetRelativeStartTime();
        });
        runTrials.SpecifyTermination(() => trialLevel.Terminated == true, blockFb, () => lastTrial = trialLevel.trialInExperiment);

        blockFb.AddInitializationMethod(() =>
        {
            if(!skipTexts){
                fbText.SetActive(true);
                fbPanel.SetActive(true);
                float acc = (float)trialLevel.numCorrect / (float)(trialLevel.trialInBlock - 1);

                string fbString = "You chose correctly on " + (acc * 100).ToString("F0") + "% of trials.\n";
                if (acc >= 0.7)
                {
                    fbString += "Nice Work!\n\nPress the space bar to ";
                }
                else
                {
                    fbString += "You could probably get even higher! \n\nPress the space bar to ";
                }
                if (currentBlock < numBlocks)
                {
                    fbString += "start the next block.";
                }
                else
                {
                    fbString += "finish the experiment.";
                }

                fbText.GetComponent<Text>().text = fbString;
            }
        });
        if(!skipTexts){
            blockFb.SpecifyTermination(() => InputBroker.GetKeyDown(KeyCode.Space), runTrials, ()=> EndBlock());
        }else{
            blockFb.SpecifyTermination(()=>true, runTrials, ()=> EndBlock());
        }

        this.AddTerminationSpecification(() => currentBlock >= numBlocks);
    }

    private void EndBlock()
    {
        fbText.SetActive(false);
        fbPanel.SetActive(false);
        blockData.AppendData();
        blockData.WriteData();
        currentBlock++;
    }
}
