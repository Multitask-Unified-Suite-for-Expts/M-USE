
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_States;
using StimHandling_Namespace;
using UnityEngine.UI;
using USE_StimulusManagement;

public class StimHandling_TrialLevel : ControlLevel_Trial_Template
{

    private StimGroup externalStimsA, externalStimsB, prefabStimsA, prefabStimsB, preloadedStimsA, preloadedStimsB;
    public StimHandling_TrialDef CurrentTrialDef => GetCurrentTrialDef<StimHandling_TrialDef>();

    public override void DefineControlLevel()
    {
        State startScreen = new State("StartScreen");
        State AStimsVisible = new State("AStimsVisible");
        State BStims = new State("BStims");
        State AStimsInvisible= new State("AStimsInvisible");

        AddActiveStates(new List<State>
            {startScreen, AStimsVisible, BStims, AStimsInvisible});

        Text commandText = null;
        SetupTrial.SpecifyTermination(() => true, startScreen);


        startScreen.AddDefaultInitializationMethod(() =>
        {
            commandText = GameObject.Find("CommandText").GetComponent<Text>();
            commandText.text = "Press the mouse button to make Group A stimuli visible.";
        });
        startScreen.SpecifyTermination(() => InputBroker.GetMouseButtonUp(0), AStimsVisible);


        AStimsVisible.AddDefaultInitializationMethod(() =>
            commandText.text = "Press the mouse button to make Group B stimuli visible.");
        AStimsVisible.SpecifyTermination(() => InputBroker.GetMouseButtonUp(0), BStims);
        
        BStims.AddDefaultInitializationMethod(() =>
            commandText.text = "Press the mouse button to make Group B stimuli invisible.");
        BStims.SpecifyTermination(() => InputBroker.GetMouseButtonUp(0), AStimsInvisible);
        
        
        AStimsInvisible.AddDefaultInitializationMethod(() =>
            commandText.text = "Press the mouse button to make Group A stimuli invisible and start the next trial.");
        AStimsInvisible.SpecifyTermination(() => InputBroker.GetMouseButtonUp(0), FinishTrial);

    }

    protected override void DefineTrialStims()
    {
        externalStimsA = new StimGroup("StimGroupA", ExternalStims, CurrentTrialDef.GroupAIndices);
        externalStimsA.SetVisibilityOnOffStates(GetStateFromName("AStimsVisible"), GetStateFromName("AStimsInvisible"));
        externalStimsA.SetLocations(CurrentTrialDef.GroupALocations);
        externalStimsB = new StimGroup("StimGroupB", ExternalStims, CurrentTrialDef.GroupBIndices);
        externalStimsB.SetLocations(CurrentTrialDef.GroupBLocations);
        externalStimsB.SetVisibilityOnOffStates(GetStateFromName("BStims"));
        TrialStims.Add(externalStimsA);
        TrialStims.Add(externalStimsB);
    }
}
