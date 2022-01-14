using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_States;
using StimHandling_Namespace;
public class StimHandling_TrialLevel : ControlLevel_Trial_Template
{
    //add state where individual stimuli are togglevisibility/destroyed
    
    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public StimHandling_TrialDef CurrentTrialDef => GetCurrentTrialDef<StimHandling_TrialDef>();
    public override void DefineControlLevel()
    {
        State startScreen = new State("StartScreen");
        State loadObjects = new State("LoadObjects");
        State makeGroupAVisible = new State("MakeGroupAVisible");
        State makeGroupAInvisible = new State("MakeGroupAInvisible");
        State makeGroupBVisible = new State("MakeGroupBVisible");
        State destroyBothGroups = new State("DestroyBothGroups");
        
        AddActiveStates(new List<State>{startScreen, loadObjects,makeGroupAVisible, makeGroupBVisible, makeGroupAInvisible, destroyBothGroups});
        
        startScreen.SpecifyTermination(()=>InputBroker.GetMouseButtonUp(0), loadObjects);
        
        loadObjects.AddDefaultInitializationMethod(() =>
        {
            TaskStims.CreateStimGroup("GroupA", TaskStims.AllTaskStimGroups["ExternalStimDefs"], CurrentTrialDef.GroupAIndices);
            TaskStims.AllTaskStimGroups["GroupA"].LoadStims();
            TaskStims.AllTaskStimGroups["GroupA"].ToggleVisibility(false);
            TaskStims.CreateStimGroup("GroupB", TaskStims.AllTaskStimGroups["ExternalStimDefs"], CurrentTrialDef.GroupBIndices);
            TaskStims.AllTaskStimGroups["GroupB"].LoadStims();
            TaskStims.AllTaskStimGroups["GroupB"].ToggleVisibility(false);
        });
        loadObjects.SpecifyTermination(()=>InputBroker.GetMouseButtonUp(0), makeGroupAVisible);

        makeGroupAVisible.AddDefaultInitializationMethod(() =>
        {
            TaskStims.AllTaskStimGroups["GroupA"].ToggleVisibility(true);
        });
        makeGroupAVisible.SpecifyTermination(()=>InputBroker.GetMouseButtonUp(0), makeGroupBVisible);
        
        makeGroupBVisible.AddDefaultInitializationMethod(() =>
        {
            TaskStims.AllTaskStimGroups["GroupB"].ToggleVisibility(true);
        });
        makeGroupBVisible.SpecifyTermination(()=>InputBroker.GetMouseButtonUp(0), makeGroupAInvisible);
        
        makeGroupAInvisible.AddDefaultInitializationMethod(() =>
        {
            TaskStims.AllTaskStimGroups["GroupA"].ToggleVisibility(false);
        });
        makeGroupAInvisible.SpecifyTermination(()=>InputBroker.GetMouseButtonUp(0), destroyBothGroups);
        
        destroyBothGroups.AddDefaultInitializationMethod(() =>
        {
            TaskStims.AllTaskStimGroups["GroupA"].DestroyStimGroup();
            TaskStims.AllTaskStimGroups["GroupB"].DestroyStimGroup();
        });

    }
}
