using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class EffortControl_TrialDataController : DataController
{
	public EffortControl_TrialLevel trialLevel;
	public override void DefineDataController()
	{
		AddDatum("TrialCount", () => trialLevel.trialCount);
		AddStateTimingData(trialLevel);
	}
}
