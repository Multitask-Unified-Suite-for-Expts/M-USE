using USE_ExperimentTemplate;
using WhatWhenWhere_Namespace;

public class WhatWhenWhere_TaskLevel : ControlLevel_Task_Template
{
    public override void SpecifyTypes()
    {
        //note that since EffortControl_TaskDef and EffortControl_BlockDef do not add any fields or methods to their parent types, 
        //they do not actually need to be specified here, but they are included to make this script more useful for later copying.
        TaskDefType = typeof(WhatWhenWhere_TaskDef);
        BlockDefType = typeof(WhatWhenWhere_BlockDef);
        TrialDefType = typeof(WhatWhenWhere_TrialDef);
    }

    public override void DefineControlLevel()
    {
    }

}
