using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_States;
using StimHandling_Namespace;
using UnityEngine.UI;
using USE_StimulusManagement;
using USE_Settings;

public class StimHandling_TrialLevel : ControlLevel_Trial_Template
{

    private StimGroup externalStimsA, externalStimsB, prefabStimsA, prefabStimsB, preloadedStimsA, preloadedStimsB;
    public StimHandling_TrialDef CurrentTrialDef => GetCurrentTrialDef<StimHandling_TrialDef>();

    public override void DefineControlLevel()
    {
        State startScreen = new State("StartScreen");
        State loadObjects = new State("LoadObjects");
        State makeGroupAVisible = new State("MakeGroupAVisible");
        State makeGroupAInvisible = new State("MakeGroupAInvisible");
        State makeGroupBVisible = new State("MakeGroupBVisible");
        State destroyBothGroups = new State("DestroyBothGroups");

        AddActiveStates(new List<State>
            {startScreen, loadObjects, makeGroupAVisible, makeGroupBVisible, makeGroupAInvisible, destroyBothGroups});

        Text commandText = null;
        SetupTrial.SpecifyTermination(() => true, startScreen);


        startScreen.AddDefaultInitializationMethod(() =>
        {
            commandText = GameObject.Find("CommandText").GetComponent<Text>();
            commandText.text = "Press the mouse button to load all stimuli invisibly.";
        });
        startScreen.SpecifyTermination(() => InputBroker.GetMouseButtonUp(0), loadObjects);

        loadObjects.AddDefaultInitializationMethod(() =>
        {
            externalStimsA = CreateStimGroup("GroupA", ExternalStims, CurrentTrialDef.GroupAIndices);
            externalStimsA.LoadExternalStims();
            externalStimsA.ToggleVisibility(false);
            externalStimsB = CreateStimGroup("GroupB", ExternalStims, CurrentTrialDef.GroupBIndices);
            externalStimsB.LoadExternalStims();
            externalStimsB.ToggleVisibility(false);
            commandText.text = "Press the mouse button to make all Group A stimuli visible.";
        });
        loadObjects.SpecifyTermination(() => InputBroker.GetMouseButtonUp(0), makeGroupAVisible);

        makeGroupAVisible.AddDefaultInitializationMethod(() =>
        {
            externalStimsA.ToggleVisibility(true); 
            commandText.text = "Press the mouse button to make all Group B stimuli visible.";
        });
        makeGroupAVisible.SpecifyTermination(() => InputBroker.GetMouseButtonUp(0), makeGroupBVisible);

        makeGroupBVisible.AddDefaultInitializationMethod(() =>
        {
            externalStimsB.ToggleVisibility(true);
            commandText.text = "Press the mouse button to make all Group A stimuli invisible.";
        });
        makeGroupBVisible.SpecifyTermination(() => InputBroker.GetMouseButtonUp(0), makeGroupAInvisible);

        makeGroupAInvisible.AddDefaultInitializationMethod(() => { externalStimsA.ToggleVisibility(false); 
            commandText.text = "Press the mouse button to destroy both Group A and B.";});
        makeGroupAInvisible.SpecifyTermination(() => InputBroker.GetMouseButtonUp(0), destroyBothGroups);

        destroyBothGroups.AddDefaultInitializationMethod(() =>
        {
            DestroyStimGroup(externalStimsA);
            DestroyStimGroup(externalStimsB);
        });
        destroyBothGroups.SpecifyTermination(() => InputBroker.GetMouseButtonUp(0), FinishTrial);

    }
}
