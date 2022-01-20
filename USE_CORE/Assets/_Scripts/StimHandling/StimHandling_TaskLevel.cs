using USE_ExperimentTemplate;
using StimHandling_Namespace;
using USE_Settings;

public class StimHandling_TaskLevel : ControlLevel_Task_Template
{
    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(StimHandling_TaskLevel);
        TaskDefType = typeof(StimHandling_TaskDef);
        BlockDefType = typeof(StimHandling_BlockDef);
        TrialDefType = typeof(StimHandling_TrialDef);
    }

    public override void DefineControlLevel()
    {
    }

}