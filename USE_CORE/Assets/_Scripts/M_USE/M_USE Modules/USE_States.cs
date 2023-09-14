/*
This software is part of the Unified Suite for Experiments (USE).
Information on USE is available at
http://accl.psy.vanderbilt.edu/resources/analysis-tools/unifiedsuiteforexperiments/

Copyright (c) <2018> <Marcus Watson>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

1) The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
2) If this software is used as a component of a project that leads to publication
(e.g. a paper in a scientific journal or a student thesis), the published work
will give appropriate attribution (e.g. citation) to the following paper:
Watson, M.R., Voloh, B., Thomas, C., Hasan, A., Womelsdorf, T. (2018). USE: An
integrative suite for temporally-precise psychophysical experiments in virtual
environments for human, nonhuman, and artificially intelligent agents. BioRxiv:
DOI https://doi.org/10.1101/434944

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using USE_StimulusManagement;
using System.IO;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;

namespace USE_States
{
    // ############################################################################################################
    // ############################################################################################################
    // ############################################################################################################

    /// <summary>Represents a single State of a ControlLevel, governing activity on each frame during this state.</summary>
	public class State
	{
		//STATE AND CONTROLLEVEL REFERENCES
		/// <summary>The name of this <see cref="T:State_Namespace.State"/>.</summary>
		public string StateName;
		/// <summary>The parent <see cref="T:State_Namespace.ControlLevel"/> of this <see cref="T:State_Namespace.State"/>.</summary>
		public ControlLevel ParentLevel;
		/// <summary>The child <see cref="T:State_Namespace.ControlLevel"/> of this <see cref="T:State_Namespace.State"/> (optional).</summary>
		public ControlLevel ChildLevel;

		//STATE UPDATE, INITIALIZATION AND TERMINATION CONTROL
		public List<StateInitialization> StateInitializations;
		public StateInitialization StateDefaultInitialization;
		public StateInitialization StateActiveInitialization;
		private VoidDelegate StateUniversalInitialization;
		/// <summary>The group of methods associated with this <see cref="T:State_Namespace.State"/>'s fixed update.</summary>
		private VoidDelegate StateFixedUpdate;
		/// <summary>The group of methods associated with this <see cref="T:State_Namespace.State"/>'s update.</summary>
		private VoidDelegate StateUpdate;
		/// <summary>The group of methods associated with this <see cref="T:State_Namespace.State"/>'s late update.</summary>
		private VoidDelegate StateLateUpdate;
		/// <summary>The list of <see cref="T:State_Namespace.StateTerminationSpecification"/>s
		///  that govern this <see cref="T:State_Namespace.State"/>'s termination and transition
		///  to the next <see cref="T:State_Namespace.State"/>.</summary>
		private List<StateTerminationSpecification> StateTerminationSpecifications;
		/// <summary>The group of methods associated by default with this <see cref="T:State_Namespace.State"/>'s
		///  termination (other terminations can be specified with <see cref="T:State_Namespace.StateTerminationSpecification"/>s).</summary>
		public VoidDelegate StateDefaultTermination;
		private VoidDelegate StateUniversalTermination;
		private VoidDelegate StateUniversalLateTermination;
		/// <summary>The State that will follow this one, only given a value after StateTerminationCheck returns true.</summary>
		public State Successor;

		//STATUS FLAGS
		/// <summary>The initialization status of this <see cref="T:State_Namespace.State"/>.</summary>
		/// <value><c>true</c> if initialized; otherwise, <c>false</c>.</value>
		private bool initialized;
		/// <summary>The termination status of this <see cref="T:State_Namespace.State"/>.</summary>
		/// <value><c>true</c> if terminated; otherwise, <c>false</c>.</value>
		public bool Terminated;
		/// <summary>Gets or sets a value indicating whether this <see cref="T:State_Namespace.State"/> is paused.</summary>
		/// <value><c>true</c> if paused; otherwise, <c>false</c>.</value>
		public bool Paused { get; set; }
		/// <summary>Gets or sets a value indicating whether this <see cref="T:State_Namespace.State"/>
		///  will log initialization and termination messages.</summary>
		/// <value><c>true</c> if debug active; otherwise, <c>false</c>.</value>
		public bool DebugActive { get; set; }
		public bool InitializationDelayed, TerminationDelayed;

		public EventHandler StateInitializationFinished, StateTerminationFinished;

		//TIMEKEEPING
		public StateTimingInfo TimingInfo;
		///// <summary>The Time.FrameCount of the first frame in which this <see cref="T:State_Namespace.State"/> was active.</summary>
		//public int StartFrame;
		///// <summary>The Time.FrameCount of the last frame in which this <see cref="T:State_Namespace.State"/> was active.</summary>
		//public int EndFrame;
		///// <summary>The Time.Time of the first frame in which this <see cref="T:State_Namespace.State"/> was active.</summary>
		//public float StartTimeAbsolute;
		///// <summary>
		///// The Time.Time of the first frame in which this <see cref="T:State_Namespace.State"/> was active, minus the StartTimeRelative of the parent Control Level.
		///// </summary>
		//public float StartTimeRelative;
		///// <summary>The Time.Time of the last frame minus the Time.Time of the first
		/////  frame in which this <see cref="T:State_Namespace.State"/> was active.</summary>
		///// <remarks>This does not include the duration of the last frame.</remarks>
		//public float Duration;

		/// <summary>Initializes a new instance of the <see cref="T:State_Namespace.State"/> class.</summary>
		/// <param name="name">The name of the State.</param>
		/// <remarks>It is recommended that the name parameter be identical to the name of the
		///  <see cref="T:State_Namespace.State"/>'s variable.</remarks>
		public State(string name, ControlLevel parent = null)
		{
			StateName = name;
			initialized = false;
			Terminated = false;
			Paused = false;
			TimingInfo = new StateTimingInfo();
			TimingInfo.StartFrame = -1;
			TimingInfo.EndFrame = -1;
			TimingInfo.StartTimeAbsolute = -1;
			TimingInfo.StartTimeRelative = -1;
			TimingInfo.EndTimeAbsolute = -1;
			TimingInfo.EndTimeRelative = -1;
			TimingInfo.Duration = -1;
			StateInitializations = new List<StateInitialization>();
			StateTerminationSpecifications = new List<StateTerminationSpecification>();
			this.ParentLevel = parent;
			if (this.ParentLevel != null)
				this.ParentLevel.AddAvailableStates(this);
		}

		//UPDATE, INITIALIZATION, AND DEFAULT TERMINATION METHODS
		public void AddSpecificInitializationMethod(VoidDelegate method, string name, float? initDelay = null)
		{
			StateInitialization init = new StateInitialization(method, name, initDelay);
			StateInitializations.Add(init);
			if (StateDefaultInitialization == null)
			{
				StateDefaultInitialization = init;
			}
		}
		public void AddSpecificInitializationMethod(VoidDelegate method, float? initDelay = null)
		{
			string name = StateName + "Initialization_" + StateInitializations.Count;
			AddSpecificInitializationMethod(method, name, initDelay);
		}
		public void AddDefaultInitializationMethod(VoidDelegate method, string name, float? initDelay = null)
		{
			StateInitialization init = new StateInitialization(method, name, initDelay);
			StateInitializations.Add(init);
			StateDefaultInitialization = init;
		}
		public void AddDefaultInitializationMethod(VoidDelegate method, float? initDelay = null)
		{
			string name = StateName + "Initialization_" + StateInitializations.Count + "_(Default)";
			AddDefaultInitializationMethod(method, name, initDelay);
		}
		public void AddUniversalInitializationMethod(VoidDelegate method)
		{
			StateUniversalInitialization += method;
		}


		/// <summary>Adds state fixed update methods to <see cref="T:State_Namespace.State"/>.</summary>
		/// <param name="method">The method to be added.</param>
		/// <remarks>StateFixedUpdate methods are run during the Fixed Update of every
		///  frame in which this <see cref="T:State_Namespace.State"/> is active.</remarks>
		public void AddFixedUpdateMethod(VoidDelegate method)
		{
			StateFixedUpdate += method;
		}
		/// <summary>Adds state update methods to <see cref="T:State_Namespace.State"/>.</summary>
		/// <param name="method">The method to be added.</param>
		/// <remarks>StateUpdate methods are run during the Update of every frame in
		///  which this <see cref="T:State_Namespace.State"/> is active.</remarks>
		public void AddUpdateMethod(VoidDelegate method)
		{
			StateUpdate += method;
		}
		/// <summary>Adds state late update methods to <see cref="T:State_Namespace.State"/>.</summary>
		/// <param name="method">The method to be added.</param>
		/// <remarks>StateLateUpdate methods are run during the Late Update of every
		///  frame in which this <see cref="T:State_Namespace.State"/> is active.</remarks>
		public void AddLateUpdateMethod(VoidDelegate method)
		{
			StateLateUpdate += method;
		}
		/// <summary>Adds default state termination methods to <see cref="T:State_Namespace.State"/>.</summary>
		/// <param name="method">The method to be added.</param>
		/// <remarks>StateTermination methods are run at the end of the last frame in
		///  which this <see cref="T:State_Namespace.State"/> is active. The default
		/// StateTermination will run if no other StateTermination is specified.</remarks>
		public void AddDefaultTerminationMethod(VoidDelegate method)
		{
			StateDefaultTermination += method;
		}
		public void AddUniversalTerminationMethod(VoidDelegate method)
		{
			StateUniversalTermination += method;
		}
		public void AddUniversalLateTerminationMethod(VoidDelegate method)
		{
			StateUniversalLateTermination += method;
		}


		public void SpecifyTermination(BoolDelegate terminationCriterion, State successorState)
		{
			string tmp = null;
			SpecifyTermination(terminationCriterion, successorState, tmp);
		}
		
		public void SpecifyTermination(BoolDelegate terminationCriterion, Func<State> successorState)
		{
			string tmp = null;
			SpecifyTermination(terminationCriterion, successorState, tmp);
		}

		public void SpecifyTermination(BoolDelegate terminationCriterion, State successorState, string successorInitName, VoidDelegate terminationMethod = null)
		{
			if (ParentLevel.CheckForAvailableState(successorState) || successorState == null)
			{
				StateInitialization init = null;
				if (successorInitName != null)
				{
					foreach (StateInitialization iinit in StateInitializations)
					{
						if (iinit.Name == successorInitName)
						{
							init = iinit;
						}
					}
				}

				StateTerminationSpecifications.Add(new StateTerminationSpecification(terminationCriterion, successorState, init, terminationMethod));
				//if (StateDefaultTermination == null && terminationMethod != null)
				//{
				//    StateDefaultTermination = terminationMethod;
				//}
			}
			else
			{
				Debug.LogError(ParentLevel.ControlLevelName + ": Attempted to add successor state " + successorState.StateName + " to state " + StateName + " but this state is not found in control level " + ParentLevel.ControlLevelName);
			}

		}
		public void SpecifyTermination(BoolDelegate terminationCriterion, Func<State> successorState, string successorInitName, VoidDelegate terminationMethod = null)
		{
			if (ParentLevel.CheckForAvailableState(successorState()) || successorState() == null)
			{
				StateInitialization init = null;
				if (successorInitName != null)
				{
					foreach (StateInitialization iinit in StateInitializations)
					{
						if (iinit.Name == successorInitName)
						{
							init = iinit;
						}
					}
				}

				StateTerminationSpecifications.Add(new StateTerminationSpecification(terminationCriterion, successorState, init, terminationMethod));
				//if (StateDefaultTermination == null && terminationMethod != null)
				//{
				//    StateDefaultTermination = terminationMethod;
				//}
			}
			else
			{
				Debug.LogError(ParentLevel.ControlLevelName + ": Attempted to add successor state " + successorState().StateName + " to state " + StateName + " but this state is not found in control level " + ParentLevel.ControlLevelName);
			}

		}
		public void SpecifyTermination(BoolDelegate terminationCriterion, State successorState, VoidDelegate termination, string successorInitName = null)
		{
			SpecifyTermination(terminationCriterion, successorState, successorInitName, termination);
		}
		public void SpecifyTermination(BoolDelegate terminationCriterion, Func<State> successorState, VoidDelegate termination, string successorInitName = null)
		{
			SpecifyTermination(terminationCriterion, successorState, successorInitName, termination);
		}

		public void AddTimer(float time, State successorState, VoidDelegate termination = null)
		{
			SpecifyTermination(() => {
				//Debug.Log(Time.time + " " + TimingInfo.StartTimeAbsolute  + " " + time);
				return Time.time - TimingInfo.StartTimeAbsolute >= time;
			}, successorState, termination);
		}

		public void AddTimer(Func<float> time, State successorState, VoidDelegate termination = null)
		{
			SpecifyTermination(() => {
				//Debug.Log(Time.time + " " + TimingInfo.StartTimeAbsolute  + " " + time);
				return Time.time - TimingInfo.StartTimeAbsolute >= time();
			}, successorState, termination);
		}
		
		
		public void AddTimer(Func<float> time, Func<State> successorState, VoidDelegate termination = null)
		{
			SpecifyTermination(() => {
				//Debug.Log(Time.time + " " + TimingInfo.StartTimeAbsolute  + " " + time);
				return Time.time - TimingInfo.StartTimeAbsolute >= time();
			}, successorState, termination);
		}

		public void AddChildLevel(ControlLevel child)
		{
			child.ParentState = this;
			ChildLevel = child;
		}



		//Update cycle - run self and child FixedUpdate, Update, and LateUpdate,
		/// <summary>
		/// Runs the state fixed update. Should not be called directly by users.
		/// </summary>
		public void RunStateFixedUpdate()
		{
			if (!Paused)
			{
				CheckInitialization();
				if (!InitializationDelayed)
				{
					if (StateFixedUpdate != null)
					{
						StateFixedUpdate();
					}
					if (ChildLevel != null)
					{
						ChildLevel.RunControlLevelFixedUpdate();
					}
				}
				else
				{
					float timeSinceInit = Time.time - TimingInfo.StartTimeAbsolute;
					if (StateActiveInitialization != null)
					{
						if (timeSinceInit >= StateActiveInitialization.InitializationDelay)
							InitializationDelayed = false;
					}
					else if (StateDefaultInitialization != null && timeSinceInit >= StateDefaultInitialization.InitializationDelay)
						InitializationDelayed = false;

					if (!InitializationDelayed)
					{
						RunInitializationMethods();
						if (StateFixedUpdate != null)
						{
							StateFixedUpdate();
						}
						if (ChildLevel != null)
						{
							ChildLevel.RunControlLevelFixedUpdate();
						}
					}
				}
			}
		}

		/// <summary>
		/// Runs the state update. Should not be called directly by users.
		/// </summary>
		public void RunStateUpdate()
		{
			if (!Paused)
			{
				CheckInitialization();
				if (!InitializationDelayed)
				{
					if (StateUpdate != null)
					{
						StateUpdate();
					}
					if (ChildLevel != null)
					{
						ChildLevel.RunControlLevelUpdate();
					}
				}
				else
				{
					float timeSinceInit = Time.time - TimingInfo.StartTimeAbsolute;
					if (StateActiveInitialization != null)
					{
						if (timeSinceInit >= StateActiveInitialization.InitializationDelay)
							InitializationDelayed = false;
					}
					else if (StateDefaultInitialization != null && timeSinceInit >= StateDefaultInitialization.InitializationDelay)
						InitializationDelayed = false;

					if (!InitializationDelayed)
					{
						RunInitializationMethods();
						if (StateFixedUpdate != null)
						{
							StateUpdate();
						}
						if (ChildLevel != null)
						{
							ChildLevel.RunControlLevelUpdate();
						}
					}
				}
			}
		}

		/// <summary>
		///  Runs the state late update. Should not be called directly by users.
		/// </summary>
		public void RunStateLateUpdate()
		{
			if (!Paused)
			{
				if (StateLateUpdate != null)
				{
					StateLateUpdate();
				}
				if (ChildLevel != null)
				{
					ChildLevel.RunControlLevelLateUpdate();
				}
				CheckTermination();
				//Termination only happens after LateUpdate
			}
		}

		/// <summary>
		/// Checks if initialization is needed, and if so runs <see cref="T:State_Namespace.StateInitialization"/> methods. Should not be called directly by users.
		/// </summary>
		void CheckInitialization()
		{
			if (!initialized)
			{
				if (DebugActive)
				{
					Debug.Log("Control Level " + ParentLevel.ControlLevelName + ": State " + StateName + " initialization on Frame " + Time.frameCount + ".");
				}
				//reset default State characteristics
				StateActiveInitialization = null;
				initialized = true;
				Terminated = false;
				Successor = null;

				//setup State timing
				TimingInfo.StartFrame = Time.frameCount;
				TimingInfo.StartTimeAbsolute = Time.time;
				TimingInfo.StartTimeRelative = TimingInfo.StartTimeAbsolute - ParentLevel.StartTimeRelative;
				TimingInfo.EndFrame = -1;
				TimingInfo.EndTimeAbsolute = -1;
				TimingInfo.EndTimeRelative = -1;
				TimingInfo.Duration = -1;
				if (ParentLevel.PreviousState != null)
				{
					// the duration of a State should include its last frame, so needs to be measured at the start of the following State
					ParentLevel.PreviousState.TimingInfo.EndTimeAbsolute = Time.time;
					ParentLevel.PreviousState.TimingInfo.EndTimeRelative = Time.time - ParentLevel.StartTimeRelative;
					ParentLevel.PreviousState.TimingInfo.Duration = Time.time - ParentLevel.PreviousState.TimingInfo.StartTimeAbsolute;
				}
				if (StateActiveInitialization != null)
				{
					if (StateActiveInitialization.InitializationDelay != null)
						InitializationDelayed = true;
				}
				else if (StateDefaultInitialization != null && StateDefaultInitialization.InitializationDelay != null)
					InitializationDelayed = true;
				else
				{
					RunInitializationMethods();
					StateInitializationFinished?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		void RunInitializationMethods()
		{
			//if there is a universal initialization, run it
			if (StateUniversalInitialization != null)
				StateUniversalInitialization();

			//If previous state specified this state's initialization, run it
			if (StateActiveInitialization != null)
			{
				StateActiveInitialization.InitializationMethod();
			}
			//Otherwise run default initialization
			else if (StateDefaultInitialization != null)
			{
				StateDefaultInitialization.InitializationMethod();
			}
		}


		/// <summary>
		/// Checks it termination is needed, and if so runs <see cref="T:State_Namespace.StateTerminationSpecification"/> methods.
		/// </summary>
		void CheckTermination()
		{
			if (!initialized)
				return;
			//if no State termination has been specified, State will run forever!
			if (StateTerminationSpecifications.Count > 0)
			{
				//go through each termination in order - the first one where TerminationCriterion returns true will be triggered and end State
				for (int i = 0; i < StateTerminationSpecifications.Count; i++)
				{
					StateTerminationSpecification termSpec = StateTerminationSpecifications[i];
					Terminated = termSpec.TerminationCriterion();
					if (Terminated) //this TerminationCriterion returned true
					{
						if (termSpec.successorIsDynamic)
							termSpec.SuccessorState = termSpec.DynamicSuccessorState();
						TerminateState(termSpec);
						StateTerminationFinished?.Invoke(this, EventArgs.Empty);
						break;
					}
				}
			}
		}

		public void TerminateState(State successorState, VoidDelegate termination = null)
		{
			if (termination == null)
				termination = () => { };
			TerminateState(new StateTerminationSpecification(null, successorState, null, termination));
		}
		void TerminateState(StateTerminationSpecification termSpec)
		{
			//if (termSpec.SuccessorInitialization != null)
			//		initialized = false;

			initialized = false;

			//Time management
			ParentLevel.PreviousState = this;
			TimingInfo.EndFrame = Time.frameCount;

			if (DebugActive)
			{
				if (termSpec.SuccessorState != null && ParentLevel.CheckForActiveState(termSpec.SuccessorState))
				{
					Debug.Log("Control Level " + ParentLevel.ControlLevelName + ": State " + StateName + " termination on Frame " + Time.frameCount + ", successor specified as " + termSpec.SuccessorState.StateName + ".");
				}
				else
				{
					//if (!Parent.CheckForActiveState(termSpec.SuccessorState)) {
					//Debug.Log("Control Level " + Parent.ControlLevelName + ": State " + StateName +
					//    " attempted to move to successor state " + termSpec.SuccessorState.StateName +
					//    " but this state is not an active state in this ControlLevel.");
					//}
					Debug.Log("Control Level " + ParentLevel.ControlLevelName + ": State " + StateName + " termination on Frame " + Time.frameCount + ", no successor.");
				}
			}
			
			//if there is a universal termination, run it
			if (StateUniversalTermination != null)
				StateUniversalTermination();
			
			//if TerminationSpecification includes a termination method, run it
			if (termSpec.Termination != null)
			{
				termSpec.Termination();
			}
			//if not, and there is a default State termination method, run it
			else if (StateDefaultTermination != null)
			{
				StateDefaultTermination();
			}
			//if there is a universal termination, run it
			if (StateUniversalLateTermination != null)
				StateUniversalLateTermination();

			//setup Successor State
			if (termSpec.SuccessorState != null)
			{
				Successor = termSpec.SuccessorState;
				Successor.initialized = false;
				ParentLevel.SpecifyCurrentState(Successor);
				if (termSpec.SuccessorInitialization != null)
				{
					Successor.StateActiveInitialization = termSpec.SuccessorInitialization;
				}
			}
			else // if successor state is specified as null, the control level will terminate
			{
				ParentLevel.Terminated = true;
				ParentLevel.ControlLevelTermination(null);
			}
		}
	}

	// ############################################################################################################
	// ############################################################################################################
	// ############################################################################################################

	/// <summary>Represents a Control Level of the experimental hierarchy.</summary>
	/// <remarks>Each ControlLevel contains a number of <see cref="T:State_Namespace.State"/>s, only one of which can run at a time.</remarks>
	public abstract class ControlLevel : MonoBehaviour
	{
		/// <summary>
		/// The name of the control level.
		/// </summary>
		public string ControlLevelName;
		/// <summary>
		/// States that can run in this control level.
		/// </summary>
		public List<State> ActiveStates;
		/// <summary>
		/// Names of the states that can run in this control level.
		/// </summary>
		private List<string> ActiveStateNames;
		/// <summary>
		/// The control level's current state.
		/// </summary>
		public State CurrentState;
		public State PreviousState;
		public State DefaultInitState;
		public State ParentState;

		//Available states can optionally be added, to allow the easy runtime specification of different states in the Control Level via string names
		/// <summary>
		/// States that are available to be added to the control level at runtime.
		/// </summary>
		/// <remarks>Available states enable the runtime specification of different Active States, thus creating different Control Levels.</remarks>
		[System.NonSerialized]
		public List<State> AvailableStates;
		/// <summary>
		/// Names of the states that are available to be added to the control level at runtime.
		/// </summary>
		[System.NonSerialized]
		public List<string> AvailableStateNames;

		private VoidDelegate controlLevelInitialization;
		private VoidDelegate controlLevelDefaultTermination;
		private List<ControlLevelTerminationSpecification> controlLevelTerminationSpecifications;

		private StimGroup ControlLevelAllStims;
		private Dictionary<string, StimGroup> ControlLevelAllStimGroups;

		private bool initialized;
		/// <summary>
		/// Whether this Control Level is terminated.
		/// </summary>
		public bool Terminated;

		/// <summary>
		/// The first frame in which the Control Level was active.
		/// </summary>
		///
		[System.NonSerialized]
		public int StartFrame;
		/// <summary>
		/// The start time of the first frame in which the Control Level was active.
		/// </summary>
		[System.NonSerialized]
		public float StartTimeAbsolute;
		/// <summary>
		/// Gets or sets a start time used to compute the StartTimeRelative of States.
		/// </summary>
		/// <value>The start time relative.</value>
		[System.NonSerialized]
		public float StartTimeRelative;
		/// <summary>
		/// The last frame in which the Control Level was active.
		/// </summary>
		[System.NonSerialized]
		public int EndFrame;
		/// <summary>
		/// Time from the start of the first frame to the start of the last frame in which the Control Level was active.
		/// </summary>
		[System.NonSerialized]
		public float Duration;


		public static bool mainLevelSpecified;
		/// <summary>
		/// Whether this Control Level is the Main Level of the experiment.
		/// </summary>
		public bool isMainLevel;
		public bool CallDefineLevelAutomatically = true;
		public bool quitApplicationAtEnd = false;


		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:State_Namespace.ControlLevel"/> prints termination and initialization messages to the log.
		/// </summary>
		/// <value><c>true</c> if debug active; otherwise, <c>false</c>.</value>
		public bool DebugActive;

		public bool Paused = true;

		public abstract void DefineControlLevel();
		public virtual void LoadSettings()
		{

		}

		public void InitializeControlLevel()
		{
			if (string.IsNullOrEmpty(ControlLevelName))
				Debug.LogError("A Control Level requires a name.");
			
			Debug.Log("Initialize " + ControlLevelName);
			initialized = false;
			Terminated = false;
			CurrentState = null;
			ActiveStates = new List<State>();
			ActiveStateNames = new List<string>();
			AvailableStates = new List<State>();
			AvailableStateNames = new List<string>();
			StartFrame = -1;
			StartTimeAbsolute = -1;
			EndFrame = -1;
			Duration = -1;
			controlLevelTerminationSpecifications = new List<ControlLevelTerminationSpecification>();

            LoadSettings();
            if (CallDefineLevelAutomatically)
                DefineControlLevel();

            Paused = false;
		}
		public void InitializeControlLevel(State state)
		{
			InitializeControlLevel();
			AddActiveStates(state);
		}
		public void InitializeControlLevel(IEnumerable<State> states)
		{
			InitializeControlLevel();
			AddActiveStates(states);
		}


		//DEFINE INITIALIZATION AND TERMINATION
		/// <summary>
		/// Adds the control level initialization method.
		/// </summary>
		/// <param name="method">Method.</param>
		public void Add_ControlLevel_InitializationMethod(VoidDelegate method)
		{
			controlLevelInitialization += method;
		}
		/// <summary>
		/// Adds the control level default termination method.
		/// </summary>
		/// <param name="method">Method.</param>
		public void AddDefaultControlLevelTerminationMethod(VoidDelegate method)
		{
			controlLevelDefaultTermination += method;
		}
		/// <summary>
		/// Adds a single control level termination specification.
		/// </summary>
		/// <param name="criterion">Criterion.</param>
		/// <param name="method">Method.</param>
		/// <remarks>Parameters consist of a single criterion for termination, and an optional termination method. If a termination method is not specified, the DefaultTerminationMethod will run after the TerminationCriterion returns true.</remarks>
		/// <overloads>There are two overloads for this method.</overloads>
		public void AddTerminationSpecification(BoolDelegate criterion, VoidDelegate method = null)
		{
			if (method == null)
			{
				controlLevelTerminationSpecifications.Add(new ControlLevelTerminationSpecification(criterion, controlLevelDefaultTermination));
			}
			else
			{
				controlLevelTerminationSpecifications.Add(new ControlLevelTerminationSpecification(criterion, method));
			}
		}


		//POPULATE CONTROL LEVEL - either single or multiple states, or strings of state names
		/// <summary>
		/// Adds an active state to the control level.
		/// </summary>
		/// <param name="state">State.</param>
		/// <overloads>There are four overloads to this method.</overloads>
		public void AddActiveStates(State state)
		{
			ActiveStates.Add(state);
			ActiveStateNames.Add(state.StateName);
			state.ParentLevel = this;
			state.DebugActive = DebugActive;
			if (!AvailableStates.Contains(state))
			{
				AvailableStates.Add(state);
				AvailableStateNames.Add(state.StateName);
			}
			if (CurrentState == null)
			{
				CurrentState = state;
			}
		}

		/// <summary>
		/// Adds active states to the control level.
		/// </summary>
		/// <param name="states">A list or array of States.</param>
		public void AddActiveStates(IEnumerable<State> states)
		{
			foreach (State state in states)
			{
				AddActiveStates(state);
			}
		}
		/// <summary>
		/// Adds active states to the control level.
		/// </summary>
		/// <param name="name">The name of the State to be added, which must be have been added to AvailableStates.</param>
		public void AddActiveStates(string name)
		{
			int stateIndex = AvailableStateNames.IndexOf(name);
			if (stateIndex >= 0)
			{
				AddActiveStates(AvailableStates[stateIndex]);
			}
			else
			{
				Debug.LogError("Attempted to add state named " + name + " to ControlLevel "
							   + ControlLevelName + ", but no state of this name has been added to AvailableStates");
			}

		}
		/// <summary>
		/// Adds active states to the control level.
		/// </summary>
		/// <param name="names">A list or array of names of States to be added, which must have been added to AvailableStates.</param>
		public void AddActiveStates(IEnumerable<string> names)
		{
			foreach (string n in names)
			{
				AddActiveStates(n);
			}
		}
		/// <summary>
		/// Adds States to the list of available States for this control level.
		/// </summary>
		/// <param name="state">The state to add.</param>
		/// <overloads>There are two overloads to this method.</overloads>
		/// <remarks>Note that available States are different from ActiveStates. Available States will not be added </remarks>
		public void AddAvailableStates(State state)
		{
			if (!AvailableStates.Contains(state))
			{
				AvailableStates.Add(state);
				AvailableStateNames.Add(state.StateName);
			}
		}
		/// <summary>
		/// Adds available States to this control level.
		/// </summary>
		/// <param name="states">A list or array of states to add.</param>
		public void AddAvailableStates(IEnumerable<State> states)
		{
			foreach (State state in states)
			{
				AddAvailableStates(state);
			}
		}

		public State GetStateFromName(string name)
		{
			int iS = ActiveStateNames.IndexOf(name);
			if (iS == null)
			{
				Debug.LogError("Attempted to retrieve state with name " + name + " from ControlLevel " +
				               ControlLevelName + " but no State with this name exists in this ControlLevel.");
				return null;
			}

			if (ActiveStates[iS].StateName != name)
			{
				Debug.LogError("Attempted to retrieve state with name " + name + " from ControlLevel " +
				               ControlLevelName + " but there has been an error with state name assignment.");
				return null;
			}
			else
				return ActiveStates[iS];
		}

		public bool CheckForActiveState(State state)
		{
			return ActiveStates.Contains(state) ? true : false;
		}
		public bool CheckForActiveState(string stateName)
		{
			return ActiveStateNames.Contains(stateName) ? true : false;
		}


		public bool CheckForAvailableState(State state)
		{
			return AvailableStates.Contains(state) ? true : false;
		}
		public bool CheckForAvailableState(string stateName)
		{
			return AvailableStateNames.Contains(stateName) ? true : false;
		}

		public void TerminateState(State successorState, VoidDelegate termination = null)
		{
			CurrentState.TerminateState(successorState, termination);
		}



		//RUN STATEMACHINE
		void Awake()
		{
            Debug.Log("Awake " + ControlLevelName);
			Paused = true;
			try
			{
				InitializeControlLevel();
                if (isMainLevel)
                {
                    if (!mainLevelSpecified)
                    {
                        mainLevelSpecified = true;
                    }
                    else
                    {
                        Debug.LogError("Attempted to specify more than one main ControlLevel. Only one per experiment!");
                    }
                }
			}
			catch (Exception e)
			{
				string errorMessage = "###############################################################################################################" + Environment.NewLine;
				errorMessage += "[ERROR] An error occurred: " + e.GetBaseException() + Environment.NewLine;
				errorMessage += "###############################################################################################################";

				Debug.LogError(errorMessage);

			}

		}
		void FixedUpdate()
		{
			try
			{
				if (isMainLevel & !Paused)
                {
                    RunControlLevelFixedUpdate();
                }
			}
			catch (Exception e)
			{
				string errorMessage = "###############################################################################################################" + Environment.NewLine;
				errorMessage += "[ERROR] An error occurred: " + e.GetBaseException() + Environment.NewLine;
				errorMessage += "###############################################################################################################";

				Debug.LogError(errorMessage);

			}

		}
		public virtual void Update()
		{
			try
			{
				if (isMainLevel & !Paused)
                {
                    RunControlLevelUpdate();
                }
			}
			catch (Exception e)
			{
				string errorMessage = "###############################################################################################################" + Environment.NewLine;
				errorMessage += "[ERROR] An error occurred: " + e.GetBaseException() + Environment.NewLine;
				errorMessage += "###############################################################################################################";

				Debug.LogError(errorMessage);

			}
		}
		void LateUpdate()
		{
			try
			{
				if (isMainLevel & !Paused)
				{
					RunControlLevelLateUpdate();
				}
			}
			catch (Exception e)
			{
				string errorMessage = "###############################################################################################################" + Environment.NewLine;
				errorMessage += "[ERROR] An error occurred: " + e.GetBaseException() + Environment.NewLine;
				errorMessage += "###############################################################################################################";

				Debug.LogError(errorMessage);
			}

		}

		public void SpecifyCurrentState(State state)
		{
			CurrentState = state;
		}

		public void RunControlLevelFixedUpdate()
		{
			if (!Paused)
			{
				CheckInitialization();
				if (CurrentState != null)
				{
					CurrentState.RunStateFixedUpdate();
				}
			}
		}

		public void RunControlLevelUpdate()
		{
			if (!Paused)
			{
				CheckInitialization(); /// add here because sometimes fixedupdate does not run in a frame
				if (CurrentState != null)
				{
					CurrentState.RunStateUpdate();
				}
			}
		}

		public void RunControlLevelLateUpdate()
		{
			if (!Paused)
			{
				if (CurrentState != null)
				{
					CurrentState.RunStateLateUpdate();
				}

				CheckTermination();
			}
		}


		void CheckInitialization()
		{
			if (!initialized)
			{
				if (DebugActive)
				{
					Debug.Log("ControlLevel " + ControlLevelName + " initialization on Frame " + Time.frameCount + ".");
				}
				if (controlLevelInitialization != null)
				{
					controlLevelInitialization();
				}
				initialized = true;
				Terminated = false;
				StartFrame = Time.frameCount;
				StartTimeAbsolute = Time.time;
				EndFrame = -1;
				Duration = -1;
				if (DefaultInitState != null)
					CurrentState = DefaultInitState;
				else if (ActiveStates != null & ActiveStates.Count > 0)
					CurrentState = ActiveStates[0];
			}
		}


		void CheckTermination()
		{
			if (controlLevelTerminationSpecifications != null && controlLevelTerminationSpecifications.Count > 0)
			{
				for (int i = 0; i < controlLevelTerminationSpecifications.Count; i++)
				{
					Terminated = controlLevelTerminationSpecifications[i].TerminationCriterion();
					if (Terminated)
					{
						ControlLevelTermination(controlLevelTerminationSpecifications[i].Termination);
						break;
					}
				}
			}
		}

		public void ControlLevelTermination(VoidDelegate termination)
		{
			if (DebugActive)
			{
				Debug.Log("ControlLevel " + ControlLevelName + " termination on Frame " + Time.frameCount + ".");
			}

			if (termination != null)
			{
				termination();
			}
			else if (controlLevelDefaultTermination != null)
			{
				controlLevelDefaultTermination();
			}
			CurrentState = null;
			initialized = false;
			EndFrame = Time.frameCount;
			Duration = Time.time - StartTimeAbsolute;
			if (isMainLevel)
			{
				if (quitApplicationAtEnd)
				{
#if UNITY_EDITOR
					{
						UnityEditor.EditorApplication.isPlaying = false;
					}
#endif
					// if (Application.isEditor)
					// {
					//     UnityEditor.EditorApplication.isPlaying = false;
					// }
					// else
					{
						Application.Quit();
					}
				}
				//else
				//{
				//    currentState = null;
				//}
			}
		}

		public void ResetRelativeStartTime()
		{
			StartTimeRelative = Time.time;
		}



        public Color32 GetRandomColor()
        {
            return new Color32((byte)UnityEngine.Random.Range(0, 256), (byte)UnityEngine.Random.Range(0, 256), (byte)UnityEngine.Random.Range(0, 256), 255);
        }

        public string TurnIntArrayIntoString(int[] array)
        {
            string s = "[";
            foreach (var num in array)
                s += num + ", ";
            s = s.Substring(0, s.Length - 2);
            s += "]";
            return s;
        }

        public string TurnVectorArrayIntoString(Vector3[] array)
        {
            string s = "[";
            foreach (var num in array)
                s += num + ", ";
            s = s.Substring(0, s.Length - 2);
            s += "]";
            return s;
        }


        public static Texture2D LoadPNG(string filePath)
        {
            Texture2D tex = null;
            byte[] fileData;

			if (File.Exists(filePath))
			{
				fileData = File.ReadAllBytes(filePath);
				tex = new Texture2D(2, 2);
				tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
			}
            return tex;
        }

        public IEnumerator HandleSkybox(string filePath)
        {
            Texture2D tex = null;

            yield return LoadTexture(filePath, result =>
            {
                if (result != null)
					tex = result;
                else
                    Debug.Log("TEX RESULT IS NULL!");
            });

			if (tex != null)
			{
				RenderSettings.skybox = CreateSkybox(tex);
				SessionValues.EventCodeManager.SendCodeNextFrame("ContextOn");
			}
			else
				Debug.Log("NOT SETTING SKYBOX BECAUSE TEX IS NULL!");
        }

        public static IEnumerator LoadTexture(string filePath, Action<Texture2D> callback)
		{
            filePath = filePath.Trim();
			Texture2D tex = null;

            if (SessionValues.WebBuild)
			{
				if(SessionValues.UsingDefaultConfigs)
					tex = Resources.Load<Texture2D>(filePath);
                else
				{
					yield return CoroutineHelper.StartCoroutine(ServerManager.LoadTextureFromServer(filePath, result =>
                    {
                        if (result != null)
							tex = result;
                        else
                            Debug.Log("TRIED TO GET TEXTURE FROM SERVER BUT THE RESULT IS NULL!");
                    }));
                }
			}
			else
				tex = LoadPNG(filePath);

			callback?.Invoke(tex);
        }

        public static Material CreateSkybox(Texture2D tex)
        {
			if (tex == null)
				Debug.Log("TEX IS NULL WHEN TRYING TO CREATE SKYBOX!");

			Material materialSkybox = new Material(Shader.Find("Skybox/6 Sided"));

			materialSkybox.SetTexture("_FrontTex", tex);
            materialSkybox.SetTexture("_BackTex", tex);
            materialSkybox.SetTexture("_LeftTex", tex);
            materialSkybox.SetTexture("_RightTex", tex);
            materialSkybox.SetTexture("_UpTex", tex);
            materialSkybox.SetTexture("_DownTex", tex);

            return materialSkybox;
        }

        public GameObject FindInactiveGameObjectByName(string name)
        {
            Transform[] objs = Resources.FindObjectsOfTypeAll<Transform>() as Transform[];
            foreach (Transform obj in objs)
            {
                if (obj.hideFlags == HideFlags.None && obj.name == name)
                {
                    return obj.gameObject;
                }
            }
            return null;
        }
    }


	// ############################################################################################################
	// ############################################################################################################
	// ############################################################################################################

	//There are two delegate types, VoidDelegate returns void and is used for the initialization, fixedUpdate, update, and termination delegates.
	//BoolDelegate returns bool and is used for termination criteria.
	public delegate void VoidDelegate();
	public delegate bool BoolDelegate();

	public class StateInitialization
	{
		public VoidDelegate InitializationMethod;
		public string Name;
		public float? InitializationDelay;

		public StateInitialization(VoidDelegate method, string name, float? initDelay = null)
		{
			InitializationMethod = method;
			Name = name;
			InitializationDelay = initDelay;
		}
	}

	public class StateTerminationSpecification
	{
		public BoolDelegate TerminationCriterion;
		public VoidDelegate Termination;
		public State SuccessorState;
		public Func<State> DynamicSuccessorState;
		public StateInitialization SuccessorInitialization;
		public string Name;
		public float? TerminationDelay;
		public bool successorIsDynamic;


		public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState, float? termDelay = null)
		{
			DefineTermination(terminationCriterion, successorState, termDelay);
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState, StateInitialization successorInit, float? termDelay = null, VoidDelegate termination = null)
		{
			DefineTermination(terminationCriterion, successorState, termDelay, successorInit, termination);
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState, VoidDelegate termination, float? termDelay = null, StateInitialization successorInit = null)
		{
			DefineTermination(terminationCriterion, successorState, termDelay, successorInit, termination);
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState, StateInitialization successorInit, VoidDelegate termination = null, float? termDelay = null)
		{
			DefineTermination(terminationCriterion, successorState, termDelay, successorInit, termination);
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState, VoidDelegate termination, StateInitialization successorInit = null, float? termDelay = null)
		{
			DefineTermination(terminationCriterion, successorState, termDelay, successorInit, termination);
		}
		private void DefineTermination(BoolDelegate terminationCriterion, State successorState, float? termDelay = null, StateInitialization successorInit = null, VoidDelegate termination = null)
		{
			TerminationCriterion = terminationCriterion;
			TerminationDelay = termDelay;
			if (termination != null)
			{
				Termination = termination;
			}
			SuccessorState = successorState;
			if (successorInit != null)
			{
				successorState.StateActiveInitialization = successorInit;
			}
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, Func<State> successorState, float? termDelay = null)
		{
			DefineTermination(terminationCriterion, successorState, termDelay);
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, Func<State> successorState, StateInitialization successorInit, float? termDelay = null, VoidDelegate termination = null)
		{
			DefineTermination(terminationCriterion, successorState, termDelay, successorInit, termination);
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, Func<State> successorState, VoidDelegate termination, float? termDelay = null, StateInitialization successorInit = null)
		{
			DefineTermination(terminationCriterion, successorState, termDelay, successorInit, termination);
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, Func<State> successorState, StateInitialization successorInit, VoidDelegate termination = null, float? termDelay = null)
		{
			DefineTermination(terminationCriterion, successorState, termDelay, successorInit, termination);
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, Func<State> successorState, VoidDelegate termination, StateInitialization successorInit = null, float? termDelay = null)
		{
			DefineTermination(terminationCriterion, successorState, termDelay, successorInit, termination);
		}
		private void DefineTermination(BoolDelegate terminationCriterion, Func<State> successorState, float? termDelay = null, StateInitialization successorInit = null, VoidDelegate termination = null)
		{
			TerminationCriterion = terminationCriterion;
			TerminationDelay = termDelay;
			if (termination != null)
			{
				Termination = termination;
			}

			DynamicSuccessorState = successorState;
			successorIsDynamic = true;
			// SuccessorState = successorState;
			// if (successorInit != null)
			// {
			// 	successorState.StateActiveInitialization = successorInit;
			// }
		}
	}

	public class ControlLevelTerminationSpecification
	{
		public BoolDelegate TerminationCriterion;
		public VoidDelegate Termination;
		public ControlLevelTerminationSpecification(BoolDelegate terminationCriterion, VoidDelegate termination)
		{
			TerminationCriterion = terminationCriterion;
			Termination = termination;
		}
	}


	public class StateTimingInfo
	{
		public int StartFrame { get; set; }
		public int EndFrame { get; set; }
		public float StartTimeAbsolute { get; set; }
		public float StartTimeRelative { get; set; }
		public float EndTimeAbsolute { get; set; }
		public float EndTimeRelative { get; set; }
		public float Duration { get; set; }
	}
}
