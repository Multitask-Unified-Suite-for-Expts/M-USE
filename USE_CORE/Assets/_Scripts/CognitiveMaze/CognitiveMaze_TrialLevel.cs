using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_ExperimentTemplate;
using CognitiveMaze_Namespace;

public class CognitiveMaze_TrialLevel : ControlLevel_Trial_Template
{
	//This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
	public CognitiveMaze_TrialDef CurrentTrialDef => GetCurrentTrialDef<CognitiveMaze_TrialDef>();

	public override void DefineControlLevel()
	{

	}

}






