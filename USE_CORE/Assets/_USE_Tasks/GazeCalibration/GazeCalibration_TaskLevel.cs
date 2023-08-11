using GazeCalibration_Namespace;
using USE_ExperimentTemplate_Task;


public class GazeCalibration_TaskLevel : ControlLevel_Task_Template
{
    GazeCalibration_BlockDef gcBD => GetCurrentBlockDef<GazeCalibration_BlockDef>();
    GazeCalibration_TrialLevel gcTL;
    public override void DefineControlLevel()
    {
        gcTL = (GazeCalibration_TrialLevel)TrialLevel;
        SetSettings();

        RunBlock.AddSpecificInitializationMethod (() =>
        {
            // Set the Background image as defined in the BlockDef
            StartCoroutine(HandleSkybox(gcTL.GetContextNestedFilePath(SessionValues.SessionDef.ContextExternalFilePath, "Gray_1", "LinearDark")));
        });
    }
    private void SetSettings()
    {   
        gcTL.ContextExternalFilePath = SessionValues.SessionDef.ContextExternalFilePath;
        gcTL.SpoofGazeWithMouse = false;
        gcTL.CalibPointsInset = new float[] {0.15f, 0.15f};
        gcTL.MaxCircleScale = 0.75f;
        gcTL.MinCircleScale = 0.15f;
        gcTL.ShrinkDuration = 1.5f;
    }

}
