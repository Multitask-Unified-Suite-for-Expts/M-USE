using USE_ExperimentTemplate;
using VisualSearch_Namespace;
using System;

public class VisualSearch_TaskLevel : ControlLevel_Task_Template
{
    VisualSearch_BlockDef bd => GetCurrentBlockDef<VisualSearch_BlockDef>();
    public override void DefineControlLevel()
    {
        VisualSearch_TrialLevel vsTL = (VisualSearch_TrialLevel)TrialLevel;
        RunBlock.AddInitializationMethod(() =>
        {
            /*
            vsTL.totalErrors_InBlock = 0;
            vsTL.errorType_InBlockString = "";
            vsTL.errorType_InBlock.Clear();
            Array.Clear(vsTL.numTotal_InBlock, 0, vsTL.numTotal_InBlock.Length);
            Array.Clear(vsTL.numCorrect_InBlock, 0, vsTL.numCorrect_InBlock.Length);
            Array.Clear(vsTL.numErrors_InBlock, 0, vsTL.numErrors_InBlock.Length);
            vsTL.accuracyLog_InBlock = "";*/
            vsTL.MaterialFilePath = bd.ContextExternalFilePath;
        });

    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }


}