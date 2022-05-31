using USE_ExperimentTemplate;
using WhatWhenWhere_Namespace;
using ExperimenterDisplayPanels; 

public class WhatWhenWhere_TaskLevel : ControlLevel_Task_Template
{
    

    public override void SpecifyTypes()
    {
        //note that since EffortControl_TaskDef and EffortControl_BlockDef do not add any fields or methods to their parent types, 
        //they do not actually need to be specified here, but they are included to make this script more useful for later copying.
        TaskLevelType = typeof(WhatWhenWhere_TaskLevel);
        TrialLevelType = typeof(WhatWhenWhere_TrialLevel);
        TaskDefType = typeof(WhatWhenWhere_TaskDef);
        BlockDefType = typeof(WhatWhenWhere_BlockDef);
        TrialDefType = typeof(WhatWhenWhere_TrialDef);
        StimDefType = typeof(WhatWhenWhere_StimDef);
    }

    public override void DefineControlLevel()
    {
        WhatWhenWhere_TrialLevel wwwTL = (WhatWhenWhere_TrialLevel)TrialLevel;

        RunBlock.AddInitializationMethod(() =>
        {
           wwwTL.totalErrors_InBlock = 0 ;
           wwwTL.errorType_InBlockString = "";
           wwwTL.errorType_InBlock.Clear();
           Panel panel = new Panel();
           panel.initPanel(experimenterInfo);
        });
        

    }

}
