using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_States;
using USE_StimulusManagement;

public class StimHandlingExamples : ControlLevel
{

	public override void DefineControlLevel()
	{
		State startScreen = new State("StartScreen");
		State loadObjects = new State("LoadObjects");
		State makeGroupAVisible = new State("MakeGroupAVisible");
		State makeGroupAInvisible = new State("MakeGroupAInvisible");
		State makeGroupBVisible = new State("MakeGroupBVisible");
		State destroyAndClose = new State("DestroyAndClose");
	}
}