using GazeCalibration_Namespace;
using UnityEngine;
using USE_ExperimentTemplate_Task;


public class GazeCalibration_TaskLevel : ControlLevel_Task_Template
{
    GazeCalibration_BlockDef gcBD => GetCurrentBlockDef<GazeCalibration_BlockDef>();
    GazeCalibration_TrialLevel gcTL;
    public override void DefineControlLevel()
    {
        gcTL = (GazeCalibration_TrialLevel)TrialLevel;
        SetSettings();

        RunBlock.AddInitializationMethod (() =>
        {
            // Set the Background image as defined in the BlockDef
            RenderSettings.skybox = CreateSkybox(gcTL.GetContextNestedFilePath(ContextExternalFilePath, "Gray_1", "LinearDark"));
        });
    }
    private void SetSettings()
    {   
        gcTL.ContextExternalFilePath = ContextExternalFilePath;
        gcTL.SpoofGazeWithMouse = false;
        gcTL.CalibPointsInset = new float[] {0.15f, 0.15f};
        gcTL.MaxCircleScale = 0.75f;
        gcTL.MinCircleScale = 0.15f;
        gcTL.ShrinkDuration = 1.5f;
    }

}
