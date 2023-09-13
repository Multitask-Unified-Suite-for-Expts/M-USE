using USE_ExperimentTemplate_Task;
using AntiSaccade_Namespace;


public class AntiSaccade_TaskLevel : ControlLevel_Task_Template
{
    AntiSaccade_BlockDef CurrentBlock => GetCurrentBlockDef<AntiSaccade_BlockDef>();
    AntiSaccade_TrialLevel trialLevel;

    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(AntiSaccade_TaskLevel);
        TrialLevelType = typeof(AntiSaccade_TrialLevel);
        TaskDefType = typeof(AntiSaccade_TaskDef);
        BlockDefType = typeof(AntiSaccade_BlockDef);
        TrialDefType = typeof(AntiSaccade_TrialDef);
        StimDefType = typeof(AntiSaccade_StimDef);
    }

    public override void DefineControlLevel()
    {
        trialLevel = (AntiSaccade_TrialLevel)TrialLevel;

        DefineBlockData();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            //trialLevel.ResetBlockVariables();
            SetSkyBox(CurrentBlock.ContextName);
            trialLevel.TokenFBController.SetTotalTokensNum(CurrentBlock.TokenBarCapacity);
            trialLevel.TokenFBController.SetTokenBarValue(CurrentBlock.NumInitialTokens);
        });

    }

    void DefineBlockData()
    {
        //NEED TO FILL THIS OUT!

    }


}