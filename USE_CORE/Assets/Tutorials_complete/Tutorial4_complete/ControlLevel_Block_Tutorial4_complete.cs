using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;

public class ControlLevel_Block_Tutorial4_complete: ControlLevel
{
    public GameObject stim1, stim2, fbText, fbPanel;

    public int currentBlock = 1;

    [HideInInspector]
    public int numBlocks, numTrials, firstTrial, lastTrial;

    public DataController_Block_Tutorial4_complete blockData;

    public override void DefineControlLevel()
    {

        //define States within this Control Level
        State runTrials = new State("RunTrials");
        State blockFb = new State("BlockFB");

        AddActiveStates(new List<State> { runTrials, blockFb });

        ControlLevel_Trial_Tutorial4_complete trialLevel = transform.GetComponent<ControlLevel_Trial_Tutorial4_complete>();
        runTrials.AddChildLevel(trialLevel);

        runTrials.AddInitializationMethod(() =>
        {
            trialLevel.numTrials = numTrials;
            trialLevel.trialInBlock = 1;
            trialLevel.numCorrect = 0;
            trialLevel.numReward = 0;
            firstTrial = trialLevel.trialInExperiment;

            if (Random.Range(0, 2) == 1)
            {
                stim1.tag = "Target";
                stim2.tag = "NotTarget";
            }else
            {
                stim1.tag = "NotTarget";
                stim2.tag = "Target";
            }
        });
        runTrials.SpecifyTermination(()=> trialLevel.Terminated == true, blockFb);

        blockFb.AddInitializationMethod(() =>
        {
            fbText.SetActive(true);
            fbPanel.SetActive(true);
            lastTrial = trialLevel.trialInExperiment;
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
        });
        blockFb.SpecifyTermination(() => InputBroker.GetKeyDown(KeyCode.Space), runTrials, ()=> EndBlock());

        this.AddTerminationSpecification(() => currentBlock > numBlocks);
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
