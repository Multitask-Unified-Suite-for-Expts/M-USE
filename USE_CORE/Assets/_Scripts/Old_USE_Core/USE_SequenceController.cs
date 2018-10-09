//DO NOT EDIT THIS SCRIPT.
//Seriously, don't touch it.
//This creates the sequenceObject object that will have its properties set by the SequenceDefintion script attached to 
//the same SequenceManager gameObject as this script.
//It also controls the flow of epochs within a sequence. See manual (which doesn't yet exist) for details.

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public abstract class USE_SequenceController : MonoBehaviour {

    public bool debugEpochs = false;

	//Variables to control the flow of the sequence.
	public bool pause = false;
	public bool sequenceTerminated = false;
	public bool sequenceInitialized = false;
	public bool loopInitialized = false;
	private bool standaloneEpochRunning	= false;
	private bool newEpoch = true;
	public int iCurrentEpoch = -1;
	public int nLoops = 0;
	private Epoch epoch;

	//There are two delegate types, VoidDelegate returns void and is used for the initialization, fixedUpdate, update, and termination delegates.
	//EpochTerminationCriteria returns bool and controls the switch from update to termination.
	public delegate void VoidDelegate ();
	public delegate bool BoolDelegate ();

	public VoidDelegate sequenceInitialization;
	public VoidDelegate sequenceTermination;
	public BoolDelegate sequenceTerminationCriterion;

	public VoidDelegate loopInitialization;
	public VoidDelegate loopTermination;

	public List<Epoch> sequenceEpochs = new List<Epoch>();
	private Dictionary<string,Epoch> standaloneEpochs = new Dictionary<string, Epoch> ();

	private Epoch queuedStandaloneEpoch;
	private bool queuedStandaloneEpochBool;


	public T InitSequence<T> (){
		DefineSequence ();
		return (T) ((object)this);
	}

	public abstract void DefineSequence();

	public void AddSequenceInitializationMethod(VoidDelegate method){
		sequenceInitialization += method;
	}

	public void AddSequenceTerminationMethod(VoidDelegate method){
		sequenceTermination += method;
	}

	public void AddSequenceTerminationCriterionMethod(BoolDelegate method){
		sequenceTerminationCriterion += method;
	}

	public void AddLoopInitializationMethod(VoidDelegate method){
		loopInitialization += method;
	}

	public void AddLoopTerminationMethod(VoidDelegate method){
		loopTermination += method;
	}

	public void AddEpochInitializationMethod(int epochNum, VoidDelegate method){
		CheckEpochCount (epochNum);
		sequenceEpochs [epochNum].initialization += method;
	}
	public void AddEpochFixedUpdateMethod(int epochNum, VoidDelegate method){
		CheckEpochCount (epochNum);
		sequenceEpochs [epochNum].fixedUpdate += method;
	}
	public void AddEpochUpdateMethod(int epochNum, VoidDelegate method){
		CheckEpochCount (epochNum);
		sequenceEpochs [epochNum].update += method;
	}
	public void AddEpochLateUpdateMethod(int epochNum, VoidDelegate method){
		CheckEpochCount (epochNum);
		sequenceEpochs [epochNum].lateUpdate += method;
	}
	public void AddEpochTerminationMethod(int epochNum, VoidDelegate method){
		CheckEpochCount (epochNum);
		sequenceEpochs [epochNum].termination += method;
	}
	public void AddEpochTerminationCriterionMethod(int epochNum, BoolDelegate method){
		CheckEpochCount (epochNum);
		sequenceEpochs [epochNum].terminationCriterion += method;
	}

	private void CheckEpochCount(int epochNum){
		if (epochNum == sequenceEpochs.Count) {
			sequenceEpochs.Add (new Epoch ());
		} else if (epochNum > sequenceEpochs.Count) {
			Debug.LogError ("<color=red>Attempted to add more than one epoch to a sequence at a time (sequence has " + sequenceEpochs.Count + " epochs, user attempted to add epoch #" + epochNum +".</color>");
		}
	}

	public void AddEpochInitializationMethod(string epochName, VoidDelegate method){
		CheckStandaloneEpochName (epochName);
		standaloneEpochs [epochName].initialization += method;
	}
	public void AddEpochFixedUpdateMethod(string epochName, VoidDelegate method){
		CheckStandaloneEpochName (epochName);
		standaloneEpochs [epochName].fixedUpdate += method;
	}
	public void AddEpochUpdateMethod(string epochName, VoidDelegate method){
		CheckStandaloneEpochName (epochName);
		standaloneEpochs [epochName].update += method;
	}
	public void AddEpochLateUpdateMethod(string epochName, VoidDelegate method){
		CheckStandaloneEpochName (epochName);
		standaloneEpochs [epochName].lateUpdate += method;
	}
	public void AddEpochTerminationMethod(string epochName, VoidDelegate method){
		CheckStandaloneEpochName (epochName);
		standaloneEpochs [epochName].termination += method;
	}
	public void AddEpochTerminationCriterionMethod(string epochName, BoolDelegate method){
		CheckStandaloneEpochName (epochName);
		standaloneEpochs [epochName].terminationCriterion += method;
	}
	private void CheckStandaloneEpochName(string epochName){
		if (!standaloneEpochs.ContainsKey (epochName)) {
			standaloneEpochs.Add (epochName, new Epoch());
            standaloneEpochs[epochName].name = epochName;
		}
	}

	//RunStandaloneEpoch with just the epoch name will start running the given epoch, 
	//and when it is done will return control to the sequence at exactly the point it started at
	public void RunStandaloneEpoch(string epochName){
		SetupStandaloneEpoch (epochName, iCurrentEpoch, false);
	}

	//RunStandaloneEpoch with the epoch name followed by an integer will start running the given epoch, 
	//and when it is done will return control to the sequence at the start of the epoch indicated by the integer
	public void RunStandaloneEpoch(string epochName, int nextSequenceEpoch){
		SetupStandaloneEpoch (epochName, nextSequenceEpoch, true);
	}

	//
	private void SetupStandaloneEpoch(string epochName, int nextSequenceEpoch, bool startNewEpoch){
		queuedStandaloneEpochBool = true;
		queuedStandaloneEpoch = standaloneEpochs [epochName];
		standaloneEpochRunning = true;

		queuedStandaloneEpoch.termination += () => SetStandaloneTermination (nextSequenceEpoch, startNewEpoch);
//		queuedStandaloneEpoch.termination += queuedStandaloneEpoch.termination -= () => SetStandaloneTermination (nextSequenceEpoch, startNewEpoch);
	}

	private void SetStandaloneTermination(int nextSequenceEpoch, bool startNewEpoch){
		if (nextSequenceEpoch > 0) {
			iCurrentEpoch = nextSequenceEpoch - 1;
		} else {
			iCurrentEpoch = sequenceEpochs.Count - 1;
		}
		newEpoch = startNewEpoch;
		standaloneEpochRunning = false;
//		queuedStandaloneEpoch.termination -= () => SetStandaloneTermination (nextSequenceEpoch, startNewEpoch);
	}

	public class Epoch
	{
        public string name { get; set; }
		public VoidDelegate initialization{ get; set; }
		public VoidDelegate fixedUpdate{ get; set; }
		public VoidDelegate update{ get; set; }
		public VoidDelegate lateUpdate{ get; set; }
		public BoolDelegate terminationCriterion{ get; set; }
		public VoidDelegate termination{ get; set; }
        public int startFrame { get; set; }
        public float startTimeAbsolute { get; set; }
        public float startTimeRelative { get; set; }
        public float duration { get; set; }

		public Epoch(){
            this.name = "";
			this.initialization = null;
			this.fixedUpdate = null;
			this.update = null;
			this.terminationCriterion = null;
            this.termination = null;
            this.startFrame = -1;
            this.startTimeAbsolute = -1;
            this.startTimeRelative = -1;
            this.duration = -1;
		}

	}


	public void SequenceControllerFixedUpdate(){
		//if this fixedupdate is being run, new epochs will be initialized here
        if (!pause && !sequenceTerminated) {
            CheckStandAlone();
            CheckInitialization();
			if (epoch.fixedUpdate != null) {
				epoch.fixedUpdate ();
            }
            CheckTermination();
		}
	}

	// Update is called once per frame
	public void SequenceControllerUpdate () {
        if (!pause && !sequenceTerminated) {
            CheckStandAlone();
            CheckInitialization();
			if (epoch.update != null) {
				epoch.update ();
            }
            CheckTermination();
		}
	}


	public void SequenceControllerLateUpdate(){
		//if this fixedupdate is being run, new epochs will be initialized here
		if (!pause && !sequenceTerminated) {
            CheckStandAlone();
            CheckInitialization();
			if (epoch.lateUpdate != null) {
				epoch.lateUpdate ();
            }
            CheckTermination();
		}
	}

    void CheckStandAlone(){
        if (queuedStandaloneEpochBool)
        {
            epoch = queuedStandaloneEpoch;
            queuedStandaloneEpochBool = false;
            newEpoch = true;
        }
    }

    void CheckInitialization(){
        if (newEpoch)
        {
            if(debugEpochs){
                if (!String.IsNullOrEmpty(epoch.name))
                {
                    Debug.Log("Epoch " + epoch.name + " initialized.");
                }else{
                    Debug.Log("Epoch " + iCurrentEpoch + " initialized.");
                }
            }
            newEpoch = false;
            if (!standaloneEpochRunning)
            { //sequence iteration stuff is not necessary if standalone epoch
                if (!sequenceInitialized)
                {
                    sequenceInitialized = true;
                    sequenceTerminated = false;
                    nLoops = 0;
                    if (sequenceInitialization != null)
                    {
                        sequenceInitialization();
                    }
                }
                IterateEpoch();
                epoch = sequenceEpochs[iCurrentEpoch];
                if (!loopInitialized)
                {
                    loopInitialized = true;
                    for (int i = 0; i < sequenceEpochs.Count; i++)
                    {
                        sequenceEpochs[i].startFrame = -1;
                        sequenceEpochs[i].startTimeAbsolute = -1;
                        sequenceEpochs[i].startTimeRelative = -1;
                        sequenceEpochs[i].duration = -1;
                    }
                    if (iCurrentEpoch == 0 && loopInitialization != null)
                    {
                        loopInitialization();
                    }
                }
            }
            epoch.startFrame = Time.frameCount;
            epoch.startTimeAbsolute = Time.time;
            epoch.startTimeRelative = Time.time - sequenceEpochs[0].startTimeAbsolute;
            epoch.duration = -1;
            if (epoch.initialization != null)
            {
                epoch.initialization();
            }
        }
    }

    void CheckTermination(){
        if (epoch.terminationCriterion != null)
        {
            if (epoch.terminationCriterion())
            {
                epoch.duration = Time.time - epoch.startTimeAbsolute;
                if (debugEpochs)
                {
                    if (!String.IsNullOrEmpty(epoch.name))
                    {
                        Debug.Log("Epoch " + epoch.name + " terminated.");
                    }
                    else
                    {
                        Debug.Log("Epoch " + iCurrentEpoch + " terminated.");
                    }
                }
                if (epoch.termination != null)
                {
                    epoch.termination();
                }
                if (!standaloneEpochRunning)
                {
                    if (iCurrentEpoch + 1 == sequenceEpochs.Count)
                    {
                        nLoops++;
                        loopInitialized = false;
                        if (loopTermination != null)
                        {
                            loopTermination();
                        }
                    }
                }
                newEpoch = true;
            }
        }
        if (sequenceTerminationCriterion != null)
        {
            if (sequenceTerminationCriterion())
            {
                if (sequenceTermination != null)
                {
                    sequenceTermination();
                }
                sequenceTerminated = true;

                //reset sequence control variables in case sequence is started again
                sequenceInitialized = false;
                iCurrentEpoch = -1;
                newEpoch = true;
                nLoops = 0;
            }
        }
    }

	void IterateEpoch(){
		//move to the next epoch
		if (iCurrentEpoch < sequenceEpochs.Count - 1) {
			iCurrentEpoch++;
		} else {
			iCurrentEpoch = 0;
		}
	}

	public void SwitchEpoch(int newEpochNum){
		iCurrentEpoch = newEpochNum - 1;
		newEpoch = true;
	}

}
