using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_Data;

public class ControlLevel_Block_Tutorial5_complete : ControlLevel
{
    public GameObject stim1;
    public GameObject stim2;
    public GameObject fbText;
    public GameObject fbPanel;

    public Text BlockPerformanceText;

    public int numBlocks = 3;
    public int numTrials = 20;
    public int currentBlock = 1;
    public List<int?> runningHistory;
    ControlLevel_Trial_Tutorial5_complete trialLevel;
    // public void Awake(){
    //     runningHistory = new List<int?>();
    // }

    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State runTrials = new State("RunTrials");
        State blockFb = new State("BlockFB");

        AddActiveStates(new List<State> { runTrials, blockFb });

        trialLevel = GameObject.FindObjectOfType<ControlLevel_Trial_Tutorial5_complete>();
        runTrials.AddChildLevel(trialLevel);

        runTrials.AddInitializationMethod(() =>
        {
            trialLevel.numTrials = numTrials;
            trialLevel.trialCount = 0;
            trialLevel.numCorrect = 0;
            trialLevel.rewardedTrials = 0;

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
            float acc = (float)trialLevel.numCorrect / (float)(trialLevel.trialCount);

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

        trialLevel.OnTrialFinished += UpdateBlockText;
    }

    private void EndBlock()
    {
        fbText.SetActive(false);
        fbPanel.SetActive(false);
        currentBlock++;
    }


    public void UpdateBlockText()
    {
        string str = "";

        str += "Block#: " + currentBlock + "\r\n";
        str += "Trial# in Block: " + (trialLevel.trialCount)+ "\r\n";
        str += "Trial# in Exeriment: " + (trialLevel.trialInExpt) + "\r\n\n";

        if (trialLevel.trialInExpt > 0)
        {
            float correct = 0;
            if(trialLevel.trialCount > 0)
                correct = trialLevel.numCorrect/(float)(trialLevel.trialCount);
            str += "% Correct: " + (100 * correct).ToString("F2") + "%\r\n";

            // try
            // {
            //     float avg = (float)runningHistory.Average();
            //     str += "% Correct Running Average (" + runningAvgWin + "): " + (100 * avg).ToString("F2") + "%\r\n\n";
            // }
            // catch
            // {
            //     str += "% Correct Running Average (" + runningAvgWin + "): " + "\r\n\n";
            // }

            str += "#Rewarded Trials: " + (trialLevel.rewardedTrials) + "\r\n";
            float rwdd = 0;
            if(trialLevel.trialCount > 0)
                rwdd = (float)trialLevel.rewardedTrials / (float)(trialLevel.trialCount);
            str += "%Rewarded Trials: " + (100 * rwdd).ToString("F2") + "%\r\n\n";
        }
        else
        {
            str += "% Correct: " + "\r\n";
            // str += "% Correct Running Average (" + runningAvgWin + "): " + "\r\n\n";

            str += "#Rewarded Trials: " + "\r\n";
            str += "%Rewarded Trials: " + "\r\n\n";
        }

        BlockPerformanceText.text = str;
    }

    // void Update(){
    //     // if (InputBroker.GetMouseButtonDown(0))
    //     //     Debug.Log("mouse click");
    //     // UpdateBlockText();
    // }

}
