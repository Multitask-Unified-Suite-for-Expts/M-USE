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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
		public ControlLevel Parent;
		/// <summary>The child <see cref="T:State_Namespace.ControlLevel"/> of this <see cref="T:State_Namespace.State"/> (optional).</summary>
		public ControlLevel Child;

		//STATE UPDATE, INITIALIZATION AND TERMINATION CONTROL
		public List<StateInitialization> StateInitializations;
		public StateInitialization StateDefaultInitialization;
		public StateInitialization StateActiveInitialization;
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
			this.Parent = parent;
			if (this.Parent != null)
				this.Parent.AddAvailableStates(this);
		}

		//UPDATE, INITIALIZATION, AND DEFAULT TERMINATION METHODS
		public void AddInitializationMethod(VoidDelegate method, string name)
		{
			StateInitialization init = new StateInitialization(method, name);
			StateInitializations.Add(init);
			if (StateDefaultInitialization == null)
			{
				StateDefaultInitialization = init;
			}
		}
		public void AddInitializationMethod(VoidDelegate method)
		{
			string name = StateName + "Initialization_" + StateInitializations.Count;
			AddInitializationMethod(method, name);
		}
		public void AddDefaultInitializationMethod(VoidDelegate method, string name)
		{
			StateInitialization init = new StateInitialization(method, name);
			StateInitializations.Add(init);
			StateDefaultInitialization = init;
		}
		public void AddDefaultInitializationMethod(VoidDelegate method)
		{
			string name = StateName + "Initialization_" + StateInitializations.Count + "_(Default)";
			AddDefaultInitializationMethod(method, name);
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


		public void SpecifyTermination(BoolDelegate terminationCriterion, State successorState)
		{
			string tmp = null;
			SpecifyTermination(terminationCriterion, successorState, tmp);
		}

		public void SpecifyTermination(BoolDelegate terminationCriterion, State successorState, string successorInitName, VoidDelegate terminationMethod = null)
		{
			if (Parent.CheckForAvailableState(successorState) || successorState == null)
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
				Debug.LogError(Parent.ControlLevelName + ": Attempted to add successor state " + successorState.StateName + " to state " + StateName + " but this state is not found in control level " + Parent.ControlLevelName);
			}

		}
		public void SpecifyTermination(BoolDelegate terminationCriterion, State successorState, VoidDelegate termination, string successorInitName = null)
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

		public void AddChildLevel(ControlLevel child)
		{
			Child = child;
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
				if (StateFixedUpdate != null)
				{
					StateFixedUpdate();
				}
				if (Child != null)
				{
					Child.RunControlLevelFixedUpdate();
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
				if (StateUpdate != null)
				{
					StateUpdate();
				}
				if (Child != null)
				{
					Child.RunControlLevelUpdate();
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
				if (Child != null)
				{
					Child.RunControlLevelLateUpdate();
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
					Debug.Log("Control Level " + Parent.ControlLevelName + ": State " + StateName + " initialization on Frame " + Time.frameCount + ".");
				}
				//reset default State characteristics
				StateActiveInitialization = null;
				initialized = true;
				Terminated = false;
				Successor = null;

				//setup State timing
				TimingInfo.StartFrame = Time.frameCount;
				TimingInfo.StartTimeAbsolute = Time.time;
				TimingInfo.StartTimeRelative = TimingInfo.StartTimeAbsolute - Parent.StartTimeRelative;
				TimingInfo.EndFrame = -1;
				TimingInfo.EndTimeAbsolute = -1;
				TimingInfo.EndTimeRelative = -1;
				TimingInfo.Duration = -1;
				if (Parent.PreviousState != null)
				{
					// the duration of a State should include its last frame, so needs to be measured at the start of the following State
					Parent.PreviousState.TimingInfo.EndTimeAbsolute = Time.time;
					Parent.PreviousState.TimingInfo.EndTimeRelative = Time.time - Parent.StartTimeRelative;
					Parent.PreviousState.TimingInfo.Duration = Time.time - Parent.PreviousState.TimingInfo.StartTimeAbsolute;
				}
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
			Parent.PreviousState = this;
			TimingInfo.EndFrame = Time.frameCount;

			if (DebugActive)
			{
				if (termSpec.SuccessorState != null && Parent.CheckForActiveState(termSpec.SuccessorState))
				{
					Debug.Log("Control Level " + Parent.ControlLevelName + ": State " + StateName + " termination on Frame " + Time.frameCount + ", successor specified as " + termSpec.SuccessorState.StateName + ".");
				}
				else
				{
					//if (!Parent.CheckForActiveState(termSpec.SuccessorState)) {
					//Debug.Log("Control Level " + Parent.ControlLevelName + ": State " + StateName +
					//    " attempted to move to successor state " + termSpec.SuccessorState.StateName +
					//    " but this state is not an active state in this ControlLevel.");
					//}
					Debug.Log("Control Level " + Parent.ControlLevelName + ": State " + StateName + " termination on Frame " + Time.frameCount + ", no successor.");
				}
			}
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

			//setup Successor State
			if (termSpec.SuccessorState != null)
			{
				Successor = termSpec.SuccessorState;
				Successor.initialized = false;
				Parent.SpecifyCurrentState(Successor);
				if (termSpec.SuccessorInitialization != null)
				{
					Successor.StateActiveInitialization = termSpec.SuccessorInitialization;
				}
			}
			else // if successor state is specified as null, the control level will terminate
			{
				Parent.Terminated = true;
				Parent.ControlLevelTermination(null);
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
						TerminateState(termSpec);
						break;
					}
				}
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

		public InitScreen initScreen;

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

		public bool Paused { get; set; }

		public abstract void DefineControlLevel();
		public virtual void LoadSettings()
		{

		}

		public void InitializeControlLevel()
		{
			if (string.IsNullOrEmpty(ControlLevelName))
			{
				Debug.LogError("A Control Level requires a name.");
			}
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
			Paused = false;
			controlLevelTerminationSpecifications = new List<ControlLevelTerminationSpecification>();

			//should be called after Init screen confirm button press
			//var initScreen = FindObjectOfType<InitScreen>();
			if (initScreen != null)
			{
				initScreen.OnLoadSettings += LoadSettings;
				initScreen.OnConfirm += () => {
					Debug.Log("initScreen confirmed fired for ControlLevel " + ControlLevelName + ", CallDefineLevelAutomatically" + CallDefineLevelAutomatically);
					if (CallDefineLevelAutomatically)
						DefineControlLevel();
				};
			}
			else
			{
				// initScreen.OnLoadConfigs += LoadConfigs;
				LoadSettings();
				if (CallDefineLevelAutomatically)
					DefineControlLevel();
			}
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
		public void AddInitializationMethod(VoidDelegate method)
		{
			controlLevelInitialization += method;
		}
		/// <summary>
		/// Adds the control level default termination method.
		/// </summary>
		/// <param name="method">Method.</param>
		public void AddDefaultTerminationMethod(VoidDelegate method)
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
			state.Parent = this;
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

		//Populate State method groups
		public void AddStateInitializationMethod(VoidDelegate method, State state = null)
		{
			List<State> stateList = new List<State>();
			if (state == null)
			{
				stateList = AvailableStates;
			}
			else
			{
				stateList.Add(state);
			}
			foreach (State s in stateList)
			{
				if (CheckForAvailableState(s))
				{
					s.AddInitializationMethod(method);
				}
				else
				{
					Debug.LogError("Attempted to add Initialization method to state named " + state.StateName
								   + " via ControlLevel " + ControlLevelName + ", but this ControlLevel does" +
								   " not contain this state. Perhaps you need to add it using the ControlLevel.AddActiveStates method.");
				}
			}
		}
		public void AddStateInitializationMethod(VoidDelegate method, IEnumerable<State> states)
		{
			foreach (State s in states)
			{
				AddStateInitializationMethod(method, s);
			}
		}
		public void AddStateInitializationMethod(VoidDelegate method, string stateName)
		{
			if (ActiveStateNames.Contains(stateName))
			{
				AddStateInitializationMethod(method, ActiveStates[ActiveStateNames.IndexOf(stateName)]);
			}
			else
			{
				Debug.LogError("Attempted Initialization method to state named " + stateName +
							   "via ControlLevel " + ControlLevelName + ", but this ControlLevel" +
							   "does not contain a state with this name. Perhaps you need to add it uing the ControlLevel.AddActiveStates method.");
			}
		}
		public void AddStateInitializationMethod(VoidDelegate method, IEnumerable<string> stateNames)
		{
			foreach (string s in stateNames)
			{
				AddStateInitializationMethod(method, s);
			}
		}

		public void AddStateFixedUpdateMethod(VoidDelegate method, State state = null)
		{
			List<State> stateList = new List<State>();
			if (state == null)
			{
				stateList = AvailableStates;
			}
			else
			{
				stateList.Add(state);
			}
			foreach (State s in stateList)
			{
				if (CheckForAvailableState(s))
				{
					s.AddFixedUpdateMethod(method);
				}
				else
				{
					Debug.LogError("Attempted to add FixedUpdate method to state named " + state.StateName
								   + " via ControlLevel " + ControlLevelName + ", but this ControlLevel does" +
								   " not contain this state. Perhaps you need to add it using the ControlLevel.AddActiveStates method.");
				}
			}
		}
		public void AddStateFixedUpdateMethod(VoidDelegate method, IEnumerable<State> states)
		{
			foreach (State s in states)
			{
				AddStateFixedUpdateMethod(method, s);
			}
		}
		public void AddStateFixedUpdateMethod(VoidDelegate method, string stateName)
		{
			if (ActiveStateNames.Contains(stateName))
			{
				AddStateFixedUpdateMethod(method, ActiveStates[ActiveStateNames.IndexOf(stateName)]);
			}
			else
			{
				Debug.LogError("Attempted FixedUpdate method to state named " + stateName +
							   "via ControlLevel " + ControlLevelName + ", but this ControlLevel" +
							   "does not contain a state with this name. Perhaps you need to add it uing the ControlLevel.AddActiveStates method.");
			}
		}
		public void AddStateFixedUpdateMethod(VoidDelegate method, IEnumerable<string> stateNames)
		{
			foreach (string s in stateNames)
			{
				AddStateFixedUpdateMethod(method, s);
			}
		}

		public void AddStateUpdateMethod(VoidDelegate method, State state = null)
		{
			List<State> stateList = new List<State>();
			if (state == null)
			{
				stateList = AvailableStates;
			}
			else
			{
				stateList.Add(state);
			}
			foreach (State s in stateList)
			{
				if (CheckForAvailableState(s))
				{
					s.AddUpdateMethod(method);
				}
				else
				{
					Debug.LogError("Attempted to add Update method to state named " + s.StateName
								   + " via ControlLevel " + ControlLevelName + ", but this ControlLevel does" +
								   " not contain this state. Perhaps you need to add it using the ControlLevel.AddActiveStates method.");
				}
			}
		}
		public void AddStateUpdateMethod(VoidDelegate method, IEnumerable<State> states)
		{
			foreach (State s in states)
			{
				AddStateUpdateMethod(method, s);
			}
		}
		public void AddStateUpdateMethod(VoidDelegate method, string stateName)
		{
			if (ActiveStateNames.Contains(stateName))
			{
				AddStateUpdateMethod(method, ActiveStates[ActiveStateNames.IndexOf(stateName)]);
			}
			else
			{
				Debug.LogError("Attempted to add Update method to state named " + stateName +
							   "via ControlLevel " + ControlLevelName + ", but this ControlLevel" +
							   "does not contain a state with this name. Perhaps you need to add it uing the ControlLevel.AddActiveStates method.");
			}
		}
		public void AddStateUpdateMethod(VoidDelegate method, IEnumerable<string> stateNames)
		{
			foreach (string s in stateNames)
			{
				AddStateUpdateMethod(method, s);
			}
		}

		public void AddStateLateUpdateMethod(VoidDelegate method, State state = null)
		{
			List<State> stateList = new List<State>();
			if (state == null)
			{
				stateList = AvailableStates;
			}
			else
			{
				stateList.Add(state);
			}
			foreach (State s in stateList)
			{
				if (CheckForAvailableState(s))
				{
					s.AddLateUpdateMethod(method);
				}
				else
				{
					Debug.LogError("Attempted to add LateUpdate method to state named " + state.StateName
								   + " via ControlLevel " + ControlLevelName + ", but this ControlLevel does" +
								   " not contain this state. Perhaps you need to add it using the ControlLevel.AddActiveStates method.");
				}
			}
		}
		public void AddStateLateUpdateMethod(VoidDelegate method, IEnumerable<State> states)
		{
			foreach (State s in states)
			{
				AddStateLateUpdateMethod(method, s);
			}
		}
		public void AddStateLateUpdateMethod(VoidDelegate method, string stateName)
		{
			if (ActiveStateNames.Contains(stateName))
			{
				AddStateLateUpdateMethod(method, ActiveStates[ActiveStateNames.IndexOf(stateName)]);
			}
			else
			{
				Debug.LogError("Attempted to add LateUpdate method to state named " + stateName +
							   "via ControlLevel " + ControlLevelName + ", but this ControlLevel" +
							   "does not contain a state with this name. Perhaps you need to add it uing the ControlLevel.AddActiveStates method.");
			}
		}
		public void AddStateLateUpdateMethod(VoidDelegate method, IEnumerable<string> stateNames)
		{
			foreach (string s in stateNames)
			{
				AddStateLateUpdateMethod(method, s);
			}
		}

		// adding termination specifications to multiple states is currently not properly supported, too many possible overloads
		public void SpecifyStateTermination(BoolDelegate terminationCriterion, State successorState, State state = null)
		{
			List<State> stateList = new List<State>();
			if (state == null)
			{
				stateList = AvailableStates;
			}
			else
			{
				stateList.Add(state);
			}
			foreach (State s in stateList)
			{
				if (CheckForAvailableState(s))
				{
					s.SpecifyTermination(terminationCriterion, successorState);
				}
				else
				{
					Debug.LogError("Attempted to specify StateTerminationSpecification for state named " + s.StateName
								   + " via ControlLevel " + ControlLevelName + ", but this ControlLevel does" +
								   " not contain this state. Perhaps you need to add it using the ControlLevel.AddActiveStates method.");
				}
			}
		}

		public void SpecifyStateTermination(BoolDelegate terminationCriterion, State successorState, VoidDelegate termination, State state = null)
		{
			List<State> stateList = new List<State>();
			if (state == null)
			{
				stateList = AvailableStates;
			}
			else
			{
				stateList.Add(state);
			}
			foreach (State s in stateList)
			{
				if (CheckForAvailableState(s))
				{
					s.SpecifyTermination(terminationCriterion, successorState, termination);
				}
				else
				{
					Debug.LogError("Attempted to specify StateTerminationSpecification for state named " + s.StateName
								   + " via ControlLevel " + ControlLevelName + ", but this ControlLevel does" +
								   " not contain this state. Perhaps you need to add it using the ControlLevel.AddActiveStates method.");
				}
			}
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
		void FixedUpdate()
		{
			if (isMainLevel & !Paused)
			{
				RunControlLevelFixedUpdate();
			}
		}
		public virtual void Update()
		{
			if (isMainLevel & !Paused)
			{
				RunControlLevelUpdate();
			}
		}
		void LateUpdate()
		{
			if (isMainLevel & !Paused)
			{
				RunControlLevelLateUpdate();
			}
		}

		public void SpecifyCurrentState(State state)
		{
			CurrentState = state;
		}

		public void RunControlLevelFixedUpdate()
		{
			CheckInitialization();
			if (CurrentState != null)
			{
				CurrentState.RunStateFixedUpdate();
			}
		}

		public void RunControlLevelUpdate()
		{
			CheckInitialization(); /// add here because sometimes fixedupdate does not run in a frame
			if (CurrentState != null)
			{
				CurrentState.RunStateUpdate();
			}
		}

		public void RunControlLevelLateUpdate()
		{
			if (CurrentState != null)
			{
				CurrentState.RunStateLateUpdate();
			}
			CheckTermination();
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

		public StateInitialization(VoidDelegate method, string name)
		{
			InitializationMethod = method;
			Name = name;
		}
	}

	public class StateTerminationSpecification
	{
		public BoolDelegate TerminationCriterion;
		public VoidDelegate Termination;
		public State SuccessorState;
		public StateInitialization SuccessorInitialization;
		public string Name;


		public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState)
		{
			DefineTermination(terminationCriterion, successorState);
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState, StateInitialization successorInit, VoidDelegate termination = null)
		{
			DefineTermination(terminationCriterion, successorState, successorInit, termination);
		}
		public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState, VoidDelegate termination, StateInitialization successorInit = null)
		{
			DefineTermination(terminationCriterion, successorState, successorInit, termination);
		}

		private void DefineTermination(BoolDelegate terminationCriterion, State successorState, StateInitialization successorInit = null, VoidDelegate termination = null)
		{
			TerminationCriterion = terminationCriterion;
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
