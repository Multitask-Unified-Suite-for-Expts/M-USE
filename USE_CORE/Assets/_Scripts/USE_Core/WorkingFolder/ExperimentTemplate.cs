using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_States;
using USE_Data;

namespace ExperimentTemplate
{
	public class ControlLevel_Session : ControlLevel
	{

		public override void DefineControlLevel()
		{
			State selectTask = new State("SelectTask");
			State runTask = new State("RunTask");
			AddActiveStates(new List<State> { selectTask, runTask });
		}

		//public virtual DefineSession();
	}

	public class ControlLevel_Task : ControlLevel
	{
		public string taskName;
		public int blockNum;


		public override void DefineControlLevel()
		{
			State runBlock = new State("RunBlock");
			State blockFB = new State("BlockFB");

			AddInitializationMethod(() => {
				//prepare blockDef[]
			});
		}
	}


	public class ControlLevel_Trial : ControlLevel
	{
		protected TrialData trialData;
		public override void DefineControlLevel()
		{
			//DefineTrial();
		}
		//public abstract void DefineTrial();
	}

	public class SessionDef
	{
		public string Subject;
		public DateTime SessionStart_DateTime;
		public int SessionStart_Frame;
		public float SessionStart_UnityTime;
		public string SessionID;
	}
	public class TaskDef
	{
		public DateTime TaskStart_DateTime;
		public int TaskStart_Frame;
		public float TaskStart_UnityTime;

		public string TaskName;
	}
	public class BlockDef { }
	public class TrialDef { }

	public class BlockData : DataController
	{
		public override void DefineDataController()
		{
		}
	}
	public class TrialData : DataController
	{
		public override void DefineDataController() { 
		
		}
	}
	public class FrameData : DataController
	{
		public override void DefineDataController() { }
	}
}
