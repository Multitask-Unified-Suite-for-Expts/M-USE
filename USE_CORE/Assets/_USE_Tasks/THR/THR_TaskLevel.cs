/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using System.Text;
using THR_Namespace;
using USE_ExperimentTemplate_Task;
using System.Collections.Specialized;


public class THR_TaskLevel : ControlLevel_Task_Template
{
    public string CurrentBlockString;
    public StringBuilder PreviousBlocksString;

    public int BlockStringsAdded = 0;

    THR_BlockDef CurrentBlock => GetCurrentBlockDef<THR_BlockDef>();
    THR_TrialLevel trialLevel;

    public int TrialsCompleted_Task = 0;
    public int TrialsCorrect_Task = 0;
    public int SelectObjectTouches_Task = 0;
    public int AvoidObjectTouches_Task = 0;
    public int BackdropTouches_Task = 0;
    public int ItiTouches_Task = 0;
    public int TouchRewards_Task = 0;
    public int ReleaseRewards_Task = 0;
    public int ReleasedEarly_Task = 0;
    public int ReleasedLate_Task = 0;
    public int TouchesMovedOutside_Task = 0;



    public override void DefineControlLevel()
    {
        trialLevel = (THR_TrialLevel)TrialLevel;

        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        DefineBlockData();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            MinTrials_InBlock = CurrentBlock.MinTrials;
            MaxTrials_InBlock = CurrentBlock.MaxTrials;
            trialLevel.ResetBlockVariables();
            CalculateBlockSummaryString();
        });

        BlockFeedback.AddSpecificInitializationMethod(() =>
        {
            if(!SessionValues.WebBuild)
            {/*
                if (BlockStringsAdded > 0)
                    CurrentBlockString += "\n";
                BlockStringsAdded++;
                PreviousBlocksString.Insert(0, CurrentBlockString);*/
            }
        });
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = trialLevel.TrialCount_InBlock + 1,
            ["Trials Correct"] = trialLevel.TrialsCorrect_Block,
            ["Touches Released Early"] = trialLevel.NumReleasedEarly_Block,
            ["Touches Released Late"] = trialLevel.NumReleasedLate_Block,
            ["Touches Moved Outside"] = trialLevel.NumTouchesMovedOutside_Block
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();
        data["Trials Completed"] = TrialsCompleted_Task;
        data["Trials Correct"] = TrialsCorrect_Task;
        data["Touch Rewards"] = TouchRewards_Task;
        data["Release Rewards"] = ReleaseRewards_Task;
        data["Select Object Touches"] = SelectObjectTouches_Task;
        data["Avoid Object Touches"] = AvoidObjectTouches_Task;
        data["Backdrop Touches"] = BackdropTouches_Task;
        data["ITI Touches"] = ItiTouches_Task;
        data["Released Early"] = ReleasedEarly_Task;
        data["Released Late"] = ReleasedLate_Task;
        data["Touches Moved Outside"] = TouchesMovedOutside_Task;
       
        return data;
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = ("<b>\nMin Trials in Block: </b>" + MinTrials_InBlock +
                             "<b>\nMax Trials in Block: </b>" + MaxTrials_InBlock +
                                "<b>\n\nBlock Name: " + CurrentBlock.BlockName + "</b>" +
                        "\nTrials Correct: " + trialLevel.TrialsCorrect_Block + 
                        "\nReleased Early: " + trialLevel.NumReleasedEarly_Block +
                        "\nReleased Late: " + trialLevel.NumReleasedLate_Block +
                        "\nMoved Outside Object: " + trialLevel.NumTouchesMovedOutside_Block +
                        "\n\nAvoid Object Touches: " + trialLevel.AvoidObjectTouches_Block +
                        "\nSelect Object Touches: " + trialLevel.SelectObjectTouches_Block +
                        "\nBackdrop Touches: " + trialLevel.BackdropTouches_Block +
                        "\nNum Pulses: " + (trialLevel.NumTouchRewards_Block + trialLevel.NumReleaseRewards_Block)
                        );

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
        if (PreviousBlocksString.Length > 0)
            CurrentBlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("NumTrialsCompleted", () => trialLevel.TrialsCompleted_Block);
        BlockData.AddDatum("NumTrialsCorrect", () => trialLevel.TrialsCorrect_Block);
        BlockData.AddDatum("AvoidObjectTouches_Block", () => trialLevel.AvoidObjectTouches_Block);
        BlockData.AddDatum("SelectObjectTouches_Block", () => trialLevel.SelectObjectTouches_Block);
        BlockData.AddDatum("BackdropTouches_Block", () => trialLevel.BackdropTouches_Block);
        BlockData.AddDatum("ItiTouches_Block", () => trialLevel.NumItiTouches_Block);
        BlockData.AddDatum("NumTouchRewards", () => trialLevel.NumTouchRewards_Block);
        BlockData.AddDatum("NumReleaseRewards", () => trialLevel.NumReleaseRewards_Block);
        BlockData.AddDatum("DifficultyLevel", () => CurrentBlock.BlockName);
        BlockData.AddDatum("NumReleasedEarly", () => trialLevel.NumReleasedEarly_Block);
        BlockData.AddDatum("NumReleasedLate", () => trialLevel.NumReleasedLate_Block);
        BlockData.AddDatum("NumTouchesMovedOutside", () => trialLevel.NumTouchesMovedOutside_Block);
    }

    void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }

}