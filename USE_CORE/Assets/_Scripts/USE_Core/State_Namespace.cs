using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace State_Namespace
{
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
        /// <summary>The Time.FrameCount of the first frame in which this <see cref="T:State_Namespace.State"/> was active.</summary>
        public int StartFrame;
        /// <summary>The Time.FrameCount of the last frame in which this <see cref="T:State_Namespace.State"/> was active.</summary>
        public int EndFrame;
        /// <summary>The Time.Time of the first frame in which this <see cref="T:State_Namespace.State"/> was active.</summary>
        public float StartTimeAbsolute;
        /// <summary>
        /// The Time.Time of the first frame in which this <see cref="T:State_Namespace.State"/> was active, minus the StartTimeRelative of the parent Control Level.
        /// </summary>
        public float StartTimeRelative;
        /// <summary>The Time.Time of the last frame minus the Time.Time of the first
        ///  frame in which this <see cref="T:State_Namespace.State"/> was active.</summary>
        /// <remarks>This does not include the duration of the last frame.</remarks>
        public float Duration;

        /// <summary>Initializes a new instance of the <see cref="T:State_Namespace.State"/> class.</summary>
        /// <param name="name">The name of the State.</param>
        /// <remarks>It is recommended that the name parameter be identical to the name of the
        ///  <see cref="T:State_Namespace.State"/>'s variable.</remarks>
        public State(string name)
        {
            StateName = name;
            initialized = false;
            Terminated = false;
            Paused = false;
            StartFrame = -1;
            EndFrame = -1;
            StartTimeAbsolute = -1;
            Duration = -1;
            StateInitializations = new List<StateInitialization>();
            StateTerminationSpecifications = new List<StateTerminationSpecification>();
        }

        //UPDATE, INITIALIZATION, AND DEFAULT TERMINATION METHODS
        public void AddStateInitializationMethod(VoidDelegate method, string name)
        {
            StateInitialization init = new StateInitialization(method, name);
            StateInitializations.Add(init);
            if (StateDefaultInitialization == null)
            {
                StateDefaultInitialization = init;
            }
        }
        public void AddStateInitializationMethod(VoidDelegate method)
        {
            string name = StateName + "Initialization_" + StateInitializations.Count;
            AddStateInitializationMethod(method, name);
        }

        /// <summary>Adds state fixed update methods to <see cref="T:State_Namespace.State"/>.</summary>
        /// <param name="method">The method to be added.</param>
        /// <remarks>StateFixedUpdate methods are run during the Fixed Update of every
        ///  frame in which this <see cref="T:State_Namespace.State"/> is active.</remarks>
        public void AddStateFixedUpdateMethod(VoidDelegate method)
        {
            StateFixedUpdate += method;
        }
        /// <summary>Adds state update methods to <see cref="T:State_Namespace.State"/>.</summary>
        /// <param name="method">The method to be added.</param>
        /// <remarks>StateUpdate methods are run during the Update of every frame in
        ///  which this <see cref="T:State_Namespace.State"/> is active.</remarks>
        public void AddStateUpdateMethod(VoidDelegate method)
        {
            StateUpdate += method;
        }
        /// <summary>Adds state late update methods to <see cref="T:State_Namespace.State"/>.</summary>
        /// <param name="method">The method to be added.</param>
        /// <remarks>StateLateUpdate methods are run during the Late Update of every
        ///  frame in which this <see cref="T:State_Namespace.State"/> is active.</remarks>
        public void AddStateLateUpdateMethod(VoidDelegate method)
        {
            StateLateUpdate += method;
        }
        /// <summary>Adds default state termination methods to <see cref="T:State_Namespace.State"/>.</summary>
        /// <param name="method">The method to be added.</param>
        /// <remarks>StateTermination methods are run at the end of the last frame in
        ///  which this <see cref="T:State_Namespace.State"/> is active. The default 
        /// StateTermination will run if no other StateTermination is specified.</remarks>
        public void AddStateDefaultTerminationMethod(VoidDelegate method)
        {
            StateDefaultTermination += method;
        }


        //Add termination specification methods
        /// <summary>
        /// Adds <see cref="T:State_Namespace.StateTerminationSpecification"/> object to <see cref="T:State_Namespace.State"/>.
        /// </summary>
        /// <param name="terminationSpec"><see cref="T:State_Namespace.StateTerminationSpecification"/>.</param>
        /// <remarks>This overload requires a single TerminationSpecification object.</remarks>
        /// <overloads>There are three overloads for this method.</overloads>
        public void SpecifyStateTermination(StateTerminationSpecification terminationSpec)
        {
            if (Parent.CheckForState(terminationSpec.SuccessorState))
            {
                StateTerminationSpecifications.Add(terminationSpec);
                if (StateDefaultTermination == null)
                {
                    StateDefaultTermination = terminationSpec.Termination;
                }
            }
            else
            {
                Debug.LogError("Attempted to add successor state to state " + StateName + " but this state is not found in control level " + Parent.ControlLevelName);
            }
        }
        /// <summary>Adds <see cref="T:State_Namespace.StateTerminationSpecification"/> object to <see cref="T:State_Namespace.State"/>.</summary>
        /// <param name="terminationCriterion">Termination criterion.</param>
        /// <param name="successorState">Successor state.</param>
        /// <param name="termination">Termination method.</param>
        /// <remarks>Termination argument is optional, if not provided will use State's <see cref="T:State_Namespace.StateDefaultTermination"/>.</remarks>
        public void SpecifyStateTermination(BoolDelegate terminationCriterion, State successorState, VoidDelegate termination = null)
        {
            SpecifyStateTermination(new StateTerminationSpecification(terminationCriterion, successorState, termination));

        }
        /// <summary>Adds <see cref="T:State_Namespace.StateTerminationSpecification"/> object to <see cref="T:State_Namespace.State"/>.</summary>
        /// <param name="terminationCriteria">Termination criteria (array, list, or other iEnumerable).</param>
        /// <param name="successorStates">Successor states (array, list, or other iEnumerable).</param>
        /// <param name="terminations">Terminations (array, list, or other iEnumerable).</param>
        /// <remarks>Terminations argument is optional, if not provided will use State's <see cref="T:State_Namespace.StateDefaultTermination"/>. All lists/arrays must be the same length.</remarks>
        public void SpecifyStateTermination(IEnumerable<BoolDelegate> terminationCriteria, IEnumerable<State> successorStates, IEnumerable<VoidDelegate> terminations = null)
        {
            if (terminationCriteria.Count() == successorStates.Count())
            {
                if (terminations == null)
                {
                    for (int i = 0; i < terminationCriteria.Count(); i++)
                    {
                        SpecifyStateTermination(terminationCriteria.ElementAt(i), successorStates.ElementAt(i));
                    }
                }
                else if (terminationCriteria.Count() == terminations.Count())
                {
                    for (int i = 0; i < terminationCriteria.Count(); i++)
                    {
                        SpecifyStateTermination(terminationCriteria.ElementAt(i), successorStates.ElementAt(i), terminations.ElementAt(i));
                    }
                }
                else
                {
                    Debug.LogError("Attempted to add lists of termination criteria, terminations and successor states to state "
                                   + StateName + ", but these lists are not the same length.");
                }
            }
            else
            {
                Debug.LogError("Attempted to add lists of termination criteria and successor states to state "
                               + StateName + ", but these lists are not the same length.");
            }
        }

        public void AddTimer(float time, State successorState, VoidDelegate termination = null)
        {
            SpecifyStateTermination(() => Time.time - StartTimeAbsolute >= time, successorState, termination);
        }

        private void AddChild(ControlLevel child)
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
                    Debug.Log("State " + StateName + " initialization on Frame " + Time.frameCount + ".");
                }
                //If previous state specified this state's initialization, run it
                if (StateActiveInitialization != null)
                {
                    StateActiveInitialization.InitializationMethod();
                }
                //Otherwise run default initialization
                else if(StateDefaultInitialization != null)
                {
                    StateDefaultInitialization.InitializationMethod();
                }
                //reset active initialization (so it doesn't run instead of default next initialization)
                StateActiveInitialization = null;
                initialized = true;
                Terminated = false;
                Successor = null;
                StartFrame = Time.frameCount;
                StartTimeAbsolute = Time.time;
                StartTimeRelative = StartTimeAbsolute - Parent.StartTimeRelative;
                EndFrame = -1;
                Duration = -1;
            }
        }

        /// <summary>
        /// Checks it termination is needed, and if so runs <see cref="T:State_Namespace.StateTerminationSpecification"/> methods.
        /// </summary>
        void CheckTermination()
        {
            if (StateTerminationSpecifications.Count > 0)
            {
                for (int i = 0; i < StateTerminationSpecifications.Count; i++)
                {
                    StateTerminationSpecification termSpec = StateTerminationSpecifications[i];
                    Terminated = termSpec.TerminationCriterion();
                    if (Terminated)
                    {
                        if (DebugActive)
                        {
                            Debug.Log("State " + StateName + " termination on Frame " + Time.frameCount + ".");
                        }
                        if (termSpec.Termination != null)
                        {
                            termSpec.Termination();
                        }
                        else if (StateDefaultTermination != null)
                        {
                            StateDefaultTermination();
                        }
                        Successor = termSpec.SuccessorState;
                        if (termSpec.SuccessorInitialization != null)
                        {
                            Successor.StateActiveInitialization = termSpec.SuccessorInitialization;
                        }
                        initialized = false;
                        Parent.SpecifyCurrentState(Successor);
                        Duration = Time.time - StartTimeAbsolute;
                        break;
                    }
                }
            }
        }
    }

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
        private List<State> ActiveStates;
        /// <summary>
        /// Names of the states that can run in this control level.
        /// </summary>
        private List<string> ActiveStateNames;
        /// <summary>
        /// The control level's current state.
        /// </summary>
        private State currentState;

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

        private bool initialized;
        /// <summary>
        /// Whether this Control Level is terminated.
        /// </summary>
        private bool Terminated;

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

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:State_Namespace.ControlLevel"/> prints termination and initialization messages to the log.
        /// </summary>
        /// <value><c>true</c> if debug active; otherwise, <c>false</c>.</value>
        public bool DebugActive;
        //CONSTRUCTORS
        //no arguments - just make a plain ControlLevel to be populated later
        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:State_Namespace.ControlLevel"/> class with empty or default-valued fields.
        ///// </summary>
        ///// <overloads>There are three overloads for this constructor.</overloads>
        //public ControlLevel()
        //{
        //    initialized = false;
        //    Terminated = false;
        //    currentState = null;
        //    ActiveStates = new List<State>();
        //    ActiveStateNames = new List<string>();
        //    AvailableStates = new List<State>();
        //    AvailableStateNames = new List<string>();
        //    StartFrame = -1;
        //    StartTimeAbsolute = -1;
        //    EndFrame = -1;
        //    Duration = -1;
        //}

        ////populated ControlLevel - single state
        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:State_Namespace.ControlLevel"/> class containing  a single <see cref="T:State_Namespace.State"/>.
        ///// </summary>
        ///// <param name="state"><see cref="T:State_Namespace.State"/></param>
        //public ControlLevel(State state)
        //{
        //    initialized = false;
        //    Terminated = false;
        //    currentState = null;
        //    ActiveStates = new List<State>();
        //    ActiveStateNames = new List<string>();
        //    AvailableStates = new List<State>();
        //    AvailableStateNames = new List<string>();
        //    StartFrame = -1;
        //    StartTimeAbsolute = -1;
        //    EndFrame = -1;
        //    Duration = -1;
        //    AddActiveStates(state);
        //}

        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:State_Namespace.ControlLevel"/> class containing a group of <see cref="T:State_Namespace.State"/>s.
        ///// </summary>
        ///// <param name="states">States.</param>
        //public ControlLevel(IEnumerable<State> states)
        //{
        //    initialized = false;
        //    Terminated = false;
        //    currentState = null;
        //    ActiveStates = new List<State>();
        //    ActiveStateNames = new List<string>();
        //    AvailableStates = new List<State>();
        //    AvailableStateNames = new List<string>();
        //    StartFrame = -1;
        //    StartTimeAbsolute = -1;
        //    EndFrame = -1;
        //    Duration = -1;
        //    AddActiveStates(states);
        //}

        public abstract void DefineControlLevel();

        public void InitializeControlLevel(string name = "")
        {
            if (String.IsNullOrEmpty(ControlLevelName))
            {
                if (String.IsNullOrEmpty(name))
                {
                    Debug.LogError("A Control Level requires a name.");
                }
                else
                {
                    ControlLevelName = name;
                }
            }
            initialized = false;
            Terminated = false;
            currentState = null;
            ActiveStates = new List<State>();
            ActiveStateNames = new List<string>();
            AvailableStates = new List<State>();
            AvailableStateNames = new List<string>();
            StartFrame = -1;
            StartTimeAbsolute = -1;
            EndFrame = -1;
            Duration = -1;
            controlLevelTerminationSpecifications = new List<ControlLevelTerminationSpecification>();
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

        public void InitializeControlLevel(string name, State state)
        {
            InitializeControlLevel(name);
            AddActiveStates(state);
        }
        public void InitializeControlLevel(string name, IEnumerable<State> states)
        {
            InitializeControlLevel(name);
            AddActiveStates(states);
        }


        //DEFINE INITIALIZATION AND TERMINATION
        /// <summary>
        /// Adds the control level initialization method.
        /// </summary>
        /// <param name="method">Method.</param>
        public void AddControlLevelInitializationMethod(VoidDelegate method)
        {
            controlLevelInitialization += method;
        }
        /// <summary>
        /// Adds the control level default termination method.
        /// </summary>
        /// <param name="method">Method.</param>
        public void AddControlLevelDefaultTerminationMethod(VoidDelegate method)
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
        public void AddControlLevelTerminationSpecification(BoolDelegate criterion, VoidDelegate method = null)
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
        /// <summary>
        /// Adds a group of control level termination specifications.
        /// </summary>
        /// <param name="criteria">Criteria.</param>
        /// <param name="methods">Methods.</param>
        /// <remarks>Parameters consist of a list or array of criteria for termination, and an optional list or array of termination methods. If termination methods are not specified, the DefaultTerminationMethod will run after any of the TerminationCriteria return true. If they are specified, the list or array must be the same length as the list or array of TeriminationCriteria.</remarks>
        public void AddControlLevelTerminationSpecification(IEnumerable<BoolDelegate> criteria, IEnumerable<VoidDelegate> methods = null)
        {
            if (methods == null)
            {
                foreach (BoolDelegate criterion in criteria)
                {
                    controlLevelTerminationSpecifications.Add(new ControlLevelTerminationSpecification(criterion, controlLevelDefaultTermination));
                }
            }
            else if(criteria.Count() == methods.Count())
            {
                for (int i = 0; i < criteria.Count(); i++)
                {
                    controlLevelTerminationSpecifications.Add(new ControlLevelTerminationSpecification(criteria.ElementAt(i), methods.ElementAt(i)));
                }
            }
            else
            {
                Debug.LogError("Attempted to add lists of termination criteria and termination methods to control level "
                               + ControlLevelName + ", but these lists are not the same length.");
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
            if (currentState == null)
            {
                currentState = state;
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
            if (stateIndex > 0)
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
            foreach (string name in names)
            {
                AddActiveStates(name);
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
                if (CheckForState(s))
                {
                    s.AddStateInitializationMethod(method);
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
                if (CheckForState(s))
                {
                    s.AddStateFixedUpdateMethod(method);
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
                AddStateInitializationMethod(method, ActiveStates[ActiveStateNames.IndexOf(stateName)]);
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
                if (CheckForState(s))
                {
                    s.AddStateUpdateMethod(method);
                }
                else
                {
                    Debug.LogError("Attempted to add Update method to state named " + state.StateName
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
                AddStateInitializationMethod(method, ActiveStates[ActiveStateNames.IndexOf(stateName)]);
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
                if (CheckForState(s))
                {
                    s.AddStateLateUpdateMethod(method);
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
                AddStateInitializationMethod(method, ActiveStates[ActiveStateNames.IndexOf(stateName)]);
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

        public void AddStateDefaultTerminationMethod(VoidDelegate method, State state = null)
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
                if (CheckForState(s))
                {
                    s.AddStateDefaultTerminationMethod(method);
                }
                else
                {
                    Debug.LogError("Attempted to add DefaultTermination method to state named " + state.StateName
                                   + " via ControlLevel " + ControlLevelName + ", but this ControlLevel does" +
                                   " not contain this state. Perhaps you need to add it using the ControlLevel.AddActiveStates method.");
                }
            }
        }
        public void AddStateDefaultTerminationMethod(VoidDelegate method, IEnumerable<State> states)
        {
            foreach (State s in states)
            {
                AddStateDefaultTerminationMethod(method, s);
            }
        }
        public void AddStateDefaultTerminationMethod(VoidDelegate method, string stateName)
        {
            if (ActiveStateNames.Contains(stateName))
            {
                AddStateInitializationMethod(method, ActiveStates[ActiveStateNames.IndexOf(stateName)]);
            }
            else
            {
                Debug.LogError("Attempted to add DefaultTermination method to state named " + stateName +
                               "via ControlLevel " + ControlLevelName + ", but this ControlLevel" +
                               "does not contain a state with this name. Perhaps you need to add it uing the ControlLevel.AddActiveStates method.");
            }
        }
        public void AddStateDefaultTerminationMethod(VoidDelegate method, IEnumerable<string> stateNames)
        {
            foreach (string s in stateNames)
            {
                AddStateDefaultTerminationMethod(method, s);
            }
        }

        public void SpecifyStateTermination(StateTerminationSpecification terminationSpecification, State state = null)
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
                if (CheckForState(s))
                {
                    s.SpecifyStateTermination(terminationSpecification);
                }
                else
                {
                    Debug.LogError("Attempted to specify StateTerminationSpecification for state named " + state.StateName
                                   + " via ControlLevel " + ControlLevelName + ", but this ControlLevel does" +
                                   " not contain this state. Perhaps you need to add it using the ControlLevel.AddActiveStates method.");
                }
            }
        }
        public void SpecifyStateTermination(StateTerminationSpecification terminationSpecification, IEnumerable<State> states)
        {
            foreach (State s in states)
            {
                SpecifyStateTermination(terminationSpecification, s);
            }
        }
        public void SpecifyStateTermination(StateTerminationSpecification terminationSpecification, string stateName)
        {
            if (ActiveStateNames.Contains(stateName))
            {
                SpecifyStateTermination(terminationSpecification, ActiveStates[ActiveStateNames.IndexOf(stateName)]);
            }
            else
            {
                Debug.LogError("Attempted to specify StateTerminationSpecification method to state named " + stateName +
                               "via ControlLevel " + ControlLevelName + ", but this ControlLevel" +
                               "does not contain a state with this name. Perhaps you need to add it uing the ControlLevel.AddActiveStates method.");
            }
        }
        public void SpecifyStateTermination(StateTerminationSpecification terminationSpecification, IEnumerable<string> stateNames)
        {
            foreach (string s in stateNames)
            {
                SpecifyStateTermination(terminationSpecification, s);
            }
        }

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
                if (CheckForState(s))
                {
                    s.SpecifyStateTermination(terminationCriterion, successorState);
                }
                else
                {
                    Debug.LogError("Attempted to specify StateTerminationSpecification for state named " + state.StateName
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
                if (CheckForState(s))
                {
                    s.SpecifyStateTermination(terminationCriterion, successorState, termination);
                }
                else
                {
                    Debug.LogError("Attempted to specify StateTerminationSpecification for state named " + state.StateName
                                   + " via ControlLevel " + ControlLevelName + ", but this ControlLevel does" +
                                   " not contain this state. Perhaps you need to add it using the ControlLevel.AddActiveStates method.");
                }
            }
        }
        //public void SpecifyStateTermination(IEnumerable<BoolDelegate> terminationCriteria, IEnumerable<State> successorStates, IEnumerable<VoidDelegate> terminations = null)
        //{
        //    if (terminationCriteria.Count() == successorStates.Count())
        //    {
        //        if (terminations == null)
        //        {
        //            for (int i = 0; i < terminationCriteria.Count(); i++)
        //            {
        //                SpecifyStateTermination(terminationCriteria.ElementAt(i), successorStates.ElementAt(i));
        //            }
        //        }
        //        else if (terminationCriteria.Count() == terminations.Count())
        //        {
        //            for (int i = 0; i < terminationCriteria.Count(); i++)
        //            {
        //                SpecifyStateTermination(terminationCriteria.ElementAt(i), successorStates.ElementAt(i), terminations.ElementAt(i));
        //            }
        //        }
        //        else
        //        {
        //            Debug.LogError("Attempted to add lists of termination criteria, terminations and successor states to state "
        //                           + StateName + ", but these lists are not the same length.");
        //        }
        //    }
        //    else
        //    {
        //        Debug.LogError("Attempted to add lists of termination criteria and successor states to state "
        //                       + StateName + ", but these lists are not the same length.");
        //    }
        //}

        public bool CheckForState(State state)
        {
            return ActiveStates.Contains(state) ? true : false;
        }
        public bool CheckForState(string stateName)
        {
            return ActiveStateNames.Contains(stateName) ? true : false;
        }




        //RUN STATEMACHINE
        void Start()
        {
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
                this.DefineControlLevel();
            }
        }
        void FixedUpdate()
        {
            if (isMainLevel)
            {
                RunControlLevelFixedUpdate();
            }
        }
        void Update()
        {
            if (isMainLevel)
            {
                RunControlLevelUpdate();
            }
        }
        void LateUpdate()
        {
            if (isMainLevel)
            {
                RunControlLevelLateUpdate();
            }
        }

        public void SpecifyCurrentState(State state)
        {
            currentState = state;
        }

        public void RunControlLevelFixedUpdate()
        {
            CheckInitialization();
            if (currentState != null)
            {
                currentState.RunStateFixedUpdate();
            }
        }

        public void RunControlLevelUpdate()
        {
            if (currentState != null)
            {
                currentState.RunStateUpdate();
            }
        }

        public void RunControlLevelLateUpdate()
        {
            if (currentState != null)
            {
                currentState.RunStateLateUpdate();
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
            }
        }


        void CheckTermination()
        {
            if (controlLevelTerminationSpecifications.Count > 0)
            {
                for (int i = 0; i < controlLevelTerminationSpecifications.Count; i++)
                {
                    Terminated = controlLevelTerminationSpecifications[i].TerminationCriterion();
                    if (Terminated)
                    {
                        if (DebugActive)
                        {
                            Debug.Log("ControlLevel " + ControlLevelName + " termination on Frame " + Time.frameCount + ".");
                        }
                        
                        if (controlLevelTerminationSpecifications[i].Termination != null)
                        {
                            controlLevelTerminationSpecifications[i].Termination();
                        }
                        initialized = false;
                        EndFrame = Time.frameCount;
                        Duration = Time.time - StartTimeAbsolute;
                        if (isMainLevel)
                        {
                            if (Application.isEditor) {
                                UnityEditor.EditorApplication.isPlaying = false;
                            }
                            else
                            {
                                Application.Quit();
                            }
                        }
                        break;
                    }
                }
            }
        }

    }


    //There are two delegate types, VoidDelegate returns void and is used for the initialization, fixedUpdate, update, and termination delegates.
    //EpochTerminationCriteria returns bool and controls the switch from update to termination.
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

        private void InitTermination(BoolDelegate terminationCriterion, State successorState, StateInitialization init = null, VoidDelegate termination = null)
        {
            TerminationCriterion = terminationCriterion;
            if (termination != null)
            {
                Termination = termination;
            }
            SuccessorState = successorState;
            if (init != null)
            {
                successorState.StateActiveInitialization = init;
            }
        }

        public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState)
        {
            InitTermination(terminationCriterion, successorState);
        }
        public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState, StateInitialization init, VoidDelegate termination = null)
        {
            InitTermination(terminationCriterion, successorState, init, termination);
        }
        public StateTerminationSpecification(BoolDelegate terminationCriterion, State successorState, VoidDelegate termination, StateInitialization init = null)
        {
            InitTermination(terminationCriterion, successorState, init, termination);
        }

        //public StateTerminationSpecification(BoolDelegate terminationCriterion, VoidDelegate termination, State successorState, )
        //{
        //    TerminationCriterion = terminationCriterion;
        //    Termination = termination;
        //    SuccessorState = successorState;
        //    SuccessorInitialization = init;
        //}
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


}
