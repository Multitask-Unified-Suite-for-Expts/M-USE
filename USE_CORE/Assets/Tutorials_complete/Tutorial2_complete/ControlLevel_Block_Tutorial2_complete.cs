using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_Data;

public class ControlLevel_Block_Tutorial2_complete : ControlLevel
{
    public GameObject stim1;
    public GameObject stim2;
    public GameObject fbText;
    public GameObject fbPanel;

    public int numBlocks = 3;
    public int numTrials = 20;
    public int currentBlock = 1;

    public override void DefineControlLevel()
    {

        //define States within this Control Level
        State runTrials = new State("RunTrials");
        State blockFb = new State("BlockFB");

        AddActiveStates(new List<State> { runTrials, blockFb });

        ControlLevel_Trial_Tutorial2_complete trialLevel = transform.GetComponent<ControlLevel_Trial_Tutorial2_complete>();
        runTrials.AddChildLevel(trialLevel);

        runTrials.AddStateInitializationMethod(() =>
        {
            trialLevel.numTrials = numTrials;
            trialLevel.trialCount = 1;
            trialLevel.numCorrect = 0;

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
        runTrials.SpecifyStateTermination(()=> trialLevel.Terminated == true, blockFb);

        blockFb.AddStateInitializationMethod(() =>
        {
            fbText.SetActive(true);
            fbPanel.SetActive(true);
            float acc = (float)trialLevel.numCorrect / (float)(trialLevel.trialCount - 1);

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
        blockFb.SpecifyStateTermination(() => InputBroker.GetKeyDown(KeyCode.Space), runTrials, ()=> EndBlock());

        AddControlLevelTerminationSpecification(() => currentBlock > numBlocks);
    }

    private void EndBlock()
    {
        fbText.SetActive(false);
        fbPanel.SetActive(false);
        currentBlock++;
    }
}
