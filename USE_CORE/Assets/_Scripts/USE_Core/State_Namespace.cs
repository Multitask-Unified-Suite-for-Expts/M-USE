using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace State_Namespace
{

    public class State
    {

        //this class governs the immediate activity on a frame

        //State references
        public string StateName;
        public StateMachine Parent;
        public StateMachine Child;

        //Internal logic
        public VoidDelegate StateInitialization { get; set; }
        public VoidDelegate StateFixedUpdate { get; set; }
        public VoidDelegate StateUpdate { get; set; }
        public VoidDelegate StateLateUpdate { get; set; }
        public List<BoolDelegate> StateTerminationCriteria { get; set; }
        public List<VoidDelegate> StateTerminations { get; set; }
        public List<State> SuccessorStates { get; set; }
        public State Successor;
        private bool initialized;
        public bool Terminated;
        public bool Paused { get; set; }
        public bool DebugActive { get; set; }

        //Timekeeping
        private int StartFrame { get; set; }
        private int EndFrame { get; set; }
        private float StartTimeAbsolute { get; set; }
        private float StartTimeRelative { get; set; }
        private float Duration { get; set; }


        public State(string name){
            StateName = name;
            initialized = false;
            Terminated = false;
            Paused = false;
        }


        //Update cycle - run self and child FixedUpdate, Update, and LateUpdate,

        public void RunStateFixedUpdate()
        {
            if (!Paused)
            {
                //Initialization only happens during FixedUpdate
                CheckInitialization();
                if (StateFixedUpdate != null)
                {
                    StateFixedUpdate();
                }
                if (Child != null)
                {
                    Child.RunStateMachineFixedUpdate();
                }
            }
        }


        public void RunStateUpdate()
        {
            if (!Paused)
            {
                if (StateUpdate != null)
                {
                    StateUpdate();
                }
                if (Child != null)
                {
                    Child.RunStateMachineUpdate();
                }
            }
        }


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
                    Child.RunStateMachineLateUpdate();
                }
                CheckTermination();
                //Termination only happens after LateUpdate
            }
        }


        void CheckInitialization()
        {
            if (!initialized)
            {
                if (DebugActive)
                {
                    Debug.Log("State " + StateName + " initialization on Frame " + Time.frameCount + ".");
                }
                if (StateInitialization != null)
                {
                    StateInitialization();
                }
                initialized = true;
                Terminated = false;
                Successor = null;
                StartFrame = Time.frameCount;
                StartTimeAbsolute = Time.time;
                Duration = -1;
            }
        }


        void CheckTermination()
        {
            if (StateTerminationCriteria.Count > 0)
            {
                for (int i = 0; i < StateTerminationCriteria.Count; i++)
                {
                    Terminated = StateTerminationCriteria[i]();
                    if (Terminated)
                    {
                        if (DebugActive)
                        {
                            Debug.Log("State " + StateName + " termination on Frame " + Time.frameCount + ".");
                        }
                        if (StateTerminations[i] != null)
                        {
                            StateTerminations[i]();
                        }
                        Successor = SuccessorStates[i];
                        initialized = false;
                        Duration = Time.time - StartTimeAbsolute;
                        break;
                    }
                }
            }
        }
    }

    public class StateMachine
    {
        private List<State> States;
        private List<string> StateNames;

        public VoidDelegate StateMachineInitialization { get; set; }
        public VoidDelegate StateMachineTermination { get; set; }

        private int iCurrentState;
        private bool initialized;
        public bool Terminated;


        //CONSTRUCTORS

        //no arguments - just make a plain StateMachine to be populated later
        public StateMachine()
        {
            initialized = false;
        }

        //populated StateMachine
        public StateMachine(List<State> stateList)
        {
            for (int i = 0; i < stateList.Count; i++)
            {
                States.Add(stateList[i]);
                StateNames.Add(stateList[i].StateName);
            }
            States[iCurrentState] = null;
        }


        //POPULATE STATE MACHINE - either single state or lists
        public void Add(State state)
        {
            States.Add(state);
            StateNames.Add(state.StateName);
        }

        public void Add(List<State> stateList)
        {
            for (int i = 0; i < stateList.Count; i++)
            {
                States.Add(stateList[i]);
                StateNames.Add(stateList[i].StateName);
            }
        }


        //RUN STATEMACHINE
        //when no argument, use existing
        public void InitializeStateMachine()
        {
            iCurrentState = 0;

        }

        public void RunStateMachineFixedUpdate()
        {
            if (States[iCurrentState] != null)
            {
                States[iCurrentState].RunStateFixedUpdate();
            }
        }

        public void RunStateMachineUpdate()
        {
            if (States[iCurrentState] != null)
            {
                States[iCurrentState].RunStateFixedUpdate();
            }
        }

        public void RunStateMachineLateUpdate()
        {
            if (States[iCurrentState] != null)
            {
                States[iCurrentState].RunStateLateUpdate();
                if (States[iCurrentState].Terminated)
                {
                    iCurrentState = States.IndexOf(States[iCurrentState].Successor);
                }
            }
        }
    }


    //There are two delegate types, VoidDelegate returns void and is used for the initialization, fixedUpdate, update, and termination delegates.
    //EpochTerminationCriteria returns bool and controls the switch from update to termination.
    public delegate void VoidDelegate();
    public delegate bool BoolDelegate();

}
