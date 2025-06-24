/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

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
using USE_DisplayManagement;
using USE_ExperimentTemplate_Trial;


namespace USE_Def_Namespace
{
    public class SessionDef
    {
        /// <summary>
        /// Represents a dictionary of task mappings where the key is the config folder name and the value is the task name.
        /// </summary>
        public OrderedDictionary TaskMappings;

        /// <summary>
        /// Path to the external folder containing the contexts for the session.
        /// </summary>
        public string ContextExternalFilePath;

        /// <summary>
        /// Directory path where task icons are stored.
        /// </summary>
        public string TaskIconsFolderPath;

        /// <summary>
        /// Duration of timeout for task selection. Default is 20f.
        /// </summary>
        public float TaskSelectionTimeout = 20f;

        /// <summary>
        /// Indicates if the build is for Mac's main display.
        /// </summary>
        public bool MacMainDisplayBuild = false;

        /// <summary>
        /// Indicates if the subject is human.
        /// </summary>
        public bool IsHuman;

        /// <summary>
        /// Indicates if event codes are active.
        /// </summary>
        public bool EventCodesActive;

        /// <summary>
        /// Indicates if the serial port is active.
        /// </summary>
        public bool SerialPortActive;

        /// <summary>
        /// Address of the serial port.
        /// </summary>
        public string SerialPortAddress;

        /// <summary>
        /// Speed of the serial port connection.
        /// </summary>
        public int SerialPortSpeed;

        /// <summary>
        /// Indicates if the sync box is active.
        /// </summary>
        public bool SyncBoxActive;

        /// <summary>
        /// List of commands to initialize the sync box.
        /// </summary>
        public List<string> SyncBoxInitCommands;

        /// <summary>
        /// Number of bytes to be split. Default value of 2.
        /// </summary>
        public int SplitBytes = 2;

        /// <summary>
        /// Indicates if the eye tracker is active.
        /// </summary>
        public bool EyeTrackerActive;

        /// <summary>
        /// Type of selection method being used. Default is "MouseButton0Click".
        /// </summary>
        public string SelectionType = "MouseButton0Click";
        
        /// <summary>
        /// Details of the monitor being used.
        /// </summary>
        public MonitorDetails MonitorDetails;

        /// <summary>
        /// Details of the screen being used.
        /// </summary>
        public ScreenDetails ScreenDetails;

        /// <summary>
        /// Number of pulses for the reward hotkey. Default is 1.
        /// </summary>
        public int RewardHotKeyNumPulses = 1;

        /// <summary>
        /// Size of each pulse for the reward hotkey in milliseconds. Default is 250.
        /// </summary>
        public int RewardHotKeyPulseSize = 250;

        /// <summary>
        /// Duration of the task results, specific for human session configurations.
        /// </summary>
        public float TaskResultsDuration;

        /// <summary>
        /// Indicates if background music should be played.
        /// </summary>
        public bool PlayBackgroundMusic;

        /// <summary>
        /// Indicates if the task selection is guided.
        /// </summary>
        public bool GuidedTaskSelection;

        /// <summary>
        /// Indicates if flash panels are active.
        /// </summary>
        public bool FlashPanelsActive;

        /// <summary>
        /// Total number of grid spots. Default is 20, but can be adjusted if needed.
        /// </summary>
        public int NumGridSpots = 20;

        /// <summary>
        /// List of positions for task button grid. To be set in session config.
        /// </summary>
        public List<int> TaskButtonGridSpots;

        /// <summary>
        /// Maximum number of task buttons per row. Default is 5.
        /// </summary>
        public int TaskButtonGridMaxPerRow = 5;

        /// <summary>
        /// Size of each task button. Default is 225.
        /// </summary>
        public int TaskButtonSize = 225;

        /// <summary>
        /// Spacing between task buttons. Default is 25.
        /// </summary>
        public int TaskButtonSpacing = 25;

        /// <summary>
        /// Private backing field used to allow MaxStimLoadingDuration to have default value of 2, but also never be less than 1
        /// </summary>
        private float maxStimLoadingDuration = 2f;

        /// <summary>
        /// Max amount of time for Stim to be loaded.
        /// </summary>
        public float MaxStimLoadingDuration
        {
            get { return maxStimLoadingDuration; }
            set { maxStimLoadingDuration = Mathf.Max(value, 1f); }
        }

        /// <summary>
        /// Duration of the SessionSummary screen before the session finishes.
        /// </summary>
        public float SessionSummaryDuration = 120f;

        /// <summary>
        /// Duration for Start Button Selection
        /// </summary>
        public float StartButtonSelectionDuration = 0.3f;

        /// <summary>
        /// Time between sending event code and sending command for sonication unit testing
        /// </summary>
        public int SonicationTest_TimeBetween = 2;

        /// <summary>
        /// Number of times to send event code and sonication for each stimulation condition
        /// </summary>
        public int SonicationTest_NumPerCondition = 10;

        /// <summary>
        /// Num of Stimulation Pulses
        /// </summary>
        public int StimulationNumPulses = 1;

        /// <summary>
        /// Size of Stimulation Pulses
        /// </summary>
        public int StimulationPulseSize = 250;


    }

    public class TaskDef
    {
        /// <summary>
        /// Represents the name of the task.
        /// </summary>
        public string TaskName;
 
        /// <summary>
        /// Path to the external file associated with the context.
        /// </summary>
        public string ContextExternalFilePath;

        /// <summary>
        /// Directory path where audio clips are stored.
        /// </summary>
        public string AudioClipsFolderPath;

        /// <summary>
        /// Directory path where external stimuli are stored.
        /// </summary>
        public string ExternalStimFolderPath;

        /// <summary>
        /// File extension used for the external stimuli.
        /// </summary>
        public string ExternalStimExtension;

        /// <summary>
        /// A list of feature names associated with the task.
        /// </summary>
        public List<string[]> FeatureNames;

        /// <summary>
        /// Name of the neutral patterned color.
        /// </summary>
        public string NeutralPatternedColorName;

        /// <summary>
        /// Scale applied to the external stimuli.
        /// </summary>
        public float? ExternalStimScale;

        /// <summary>
        /// List of controllers used for feedback (ie. Audio, Halo, Slider, Token).
        /// </summary>
        public List<string> FeedbackControllers;

        /// <summary>
        /// Duration of selection error touch feedback. Default is 0.3f.
        /// </summary>
        public float TouchFeedbackDuration = 0.3f;

        /// <summary>
        /// Total number of tokens in the token bar. Default is 5.
        /// </summary>
        public int TotalTokensNum = 5;

        /// <summary>
        /// Indicates if the reward pulses are active.
        /// </summary>
        public bool RewardPulsesActive;

        /// <summary>
        /// Represents the type of selection method being used.
        /// </summary>
        public string SelectionType;

        /// <summary>
        /// Custom settings defined as key-value pairs.
        /// </summary>
        public Dictionary<string, string> CustomSettings;

        /// <summary>
        /// Position of the start button. Default is (0,0,0).
        /// </summary>
        public Vector3 StartButtonPosition = Vector3.zero;

        /// <summary>
        /// Scale applied to the start button. Default is 1.2f.
        /// </summary>
        public float StartButtonScale = 1.2f;

        /// <summary>
        /// TouchFeedback size. Default is 20.
        /// </summary>
        public float TouchFeedbackSize = 20f;

        /// <summary>
        /// Indicates whether the stimulus is facing the camera.
        /// </summary>
        public bool StimFacingCamera;

        /// <summary>
        /// Specifies the type of shadow being used or its configuration.
        /// </summary>
        public string ShadowType = "None";

        /// <summary>
        /// Indicates whether the Inter-Trial Interval (ITI) is set to a neutral state or mode.
        /// </summary>
        public bool NeutralITI;
        
        public string TrialDefSelectionStyle;

        /// <summary>
        /// The Task's Directional Light Intensity.
        /// </summary>
        public virtual float TaskDirectionalLightIntensity { get; set; } = 1.35f; //1.35 for new quaddles, 2 for old quaddles

        /// <summary>
        /// Whether or not to run a stimulation
        /// </summary>
        public bool RunSimulation;


        /// <summary>
        /// 3D Shotgun Radius in Pixels
        /// </summary>
        public int ShotgunRadius_Pixels = 50;



    }

    public class BlockDef
    {
        /// <summary>
        /// Represents the block number of the specified block.
        /// </summary>
        public int BlockCount;
        
        /// <summary>
        /// A unique string used to label different blocks.
        /// </summary>
        public string BlockName;
        
        /// <summary>
        /// Refers to the filename of the PNG texture in the resources folder used during the block.
        /// </summary>
        public string ContextName;
        
        /// <summary>
        /// Integer value indicating the minimum number of trials in the block.
        /// </summary>
        public int MinTrials;

        /// <summary>
        /// Integer value indicating the maximum number of trials in the block.
        /// </summary>
        public int MaxTrials;
        
        /// <summary>
        /// Integer value indicating the exact number of trials in the block.
        /// </summary>
        public int NumTrials;

        /// <summary>
        /// Array containing the minimum and maximum number of trials.
        /// </summary>
        public int[] RandomMinMaxTrials;
        
        /// <summary>
        /// Array containing the minimum and maximum number of trials.
        /// </summary>
        public int[] MinMaxTrials;
        
        /// <summary>
        /// A strategy defining when to end a block. Options include: CurrentTrialPerformance, SimpleThreshold, ThresholdAndPeak, or ThresholdOrAsymptote.
        /// </summary>
        public string BlockEndType;

        /// <summary>
        /// A specified value used in conjunction with the BlockEndType to determine when to conclude a block.
        /// </summary>
        public float BlockEndThreshold;

        /// <summary>
        /// The number of most recent trials evaluated against the block end threshold. 
        /// </summary>
        public int BlockEndWindow;
        
        /// <summary>
        /// The number of pulses transmitted to the SyncBox when a pulse reward is given.
        /// </summary>
        public int NumPulses;

        /// <summary>
        /// The magnitude of each pulse sent from the SyncBox for reward.
        /// </summary>
        public int PulseSize;
        
        /// <summary>
        /// The initial amount of progress displayed in the slider at the start of the block.
        /// </summary>
        public int SliderInitialValue;

        /// <summary>
        /// The number of tokens earned per correct response.
        /// </summary>
        public int TokenGain;

        /// <summary>
        /// The number of tokens removed per incorrect response.
        /// </summary>
        public int TokenLoss;

        /// <summary>
        /// The number of tokens displayed in the token bar at the start of the block.
        /// </summary>
        public int NumInitialTokens = 0;
       
        /// <summary>
        /// The number of tokens that the token bar can hold.
        /// </summary>
        public int TokenBarCapacity;

        /// <summary>
        /// A list of trial definitions for the block.
        /// </summary>
        public List<TrialDef> TrialDefs;

        /// <summary>
        /// Random number generator, used to select random number of max trials in the RandomMinMaxTrials range.
        /// </summary>
        public System.Random RandomNumGenerator;
        
        public int DifficultyLevel;


        /// <summary>
        /// The perceptual similarity score (1-5) of the quaddles for that trial.
        /// </summary>
        public int PerceptualSimilarity;

        /// <summary>
        /// Whether or not the particle halo will be created after a selection is made
        /// </summary>
        public bool ParticleHaloActive;

        /// <summary>
        /// Whether or not the circle halo will be created after a selection is made
        /// </summary>
        public bool CircleHaloActive = true;

        /// <summary>
        /// MaskValues [TrialNum, MaskNum, Transparency]
        /// </summary>
        public Vector3[] MaskValues;

        /// <summary>
        /// MaskColor
        /// </summary>
        public Vector3 MaskColor;

        /// <summary>
        /// Mask Fade In Duration. Default is .75 seconds.
        /// </summary>
        public float MaskFadeInDuration = .75f;

        /// <summary>
        /// Stimulation Type. [FixationChoice_Target, FixationChoice_Distractor)
        /// </summary>
        public string StimulationType;

        /// <summary>
        /// Fixation time before sending the FixationOnsetPassed EventCode. 
        /// </summary>
        public float InitialFixationDuration = 0.3f;

        /// <summary>
        /// Delay before Stimulation
        /// </summary>
        public float StimulationDelayDuration = 0f;

        /// <summary>
        /// Stimulation conditions to be randomized and sent to external python script at start of stimulation trial. 
        /// </summary>
        public int[] StimulationConditionCodes;

        /// <summary>
        /// Trials to Stimulate on for the Block
        /// </summary>
        public int[] TrialsToStimulateOn;

        /// <summary>
        /// Object Stimulation Order Number
        /// </summary>
        public int[][] ObjectsToStimulateOn;





        /// <summary>
        /// Generates trial definitions based on block definitions.
        /// </summary>
        public virtual void GenerateTrialDefsFromBlockDef()
        {
        }

        /// <summary>
        /// Adds to the trial definitions based on block definitions.
        /// </summary>
        public virtual void AddToTrialDefsFromBlockDef()
        {
        }

        /// <summary>
        /// Method for initializing the block.
        /// </summary>
        public virtual void BlockInitializationMethod()
        {
        }
    }
    
    public abstract class TrialDef
    {
        /// <summary>
        /// Integer value indicating the minimum number of trials.
        /// </summary>
        public int MinTrials;

        /// <summary>
        /// Integer value indicating the maximum number of trials.
        /// </summary>
        public int MaxTrials;
        
        /// <summary>
        /// Array containing the minimum number of trials, and the maximum is randomly selected within that range.
        /// </summary>
        public int[] RandomMinMaxTrials;

        /// <summary>
        /// Array containing the minimum and maximum number of trials.
        /// </summary>
        public int[] MinMaxTrials;

        
        /// <summary>
        /// Represents the block count of the trial, corresponding to the BlockCount of BlockDef
        /// </summary>
        public int BlockCount;

        /// <summary>
        /// Represents the count of trials within a block.
        /// </summary>
        public int TrialCountInBlock;

        /// <summary>
        /// Represents the count of trials within a task.
        /// </summary>
        public int TrialCountInTask;

        /// <summary>
        /// Unique identifier for a trial.
        /// </summary>
        public string TrialID;

        /// <summary>
        /// Object representing the stimuli associated with the trial.
        /// </summary>
        public TrialStims TrialStims;

        /// <summary>
        /// Indicates the type of block end condition.
        /// </summary>
        public string BlockEndType;

        /// <summary>
        /// Threshold value used to determine block end condition.
        /// </summary>
        public float BlockEndThreshold;

        /// <summary>
        /// Window value used in conjunction with the block end condition.
        /// </summary>
        public int BlockEndWindow;

        /// <summary>
        /// Represents the name of the block.
        /// </summary>
        public string BlockName;

        /// <summary>
        /// Represents the name of the context.
        /// </summary>
        public string ContextName;
        
        /// <summary>
        /// Number of pulses.
        /// </summary>
        public int NumPulses;

        /// <summary>
        /// Size of each pulse.
        /// </summary>
        public int PulseSize;
        
        /// <summary>
        /// The initial amount of progress displayed in the slider at the start of the block.
        /// </summary>
        public int SliderInitialValue;

        /// <summary>
        /// The slider gains
        /// </summary>
        public int[] SliderGain;

        /// <summary>
        /// The slider losses
        /// </summary>
        public int[] SliderLoss;

        /// <summary>
        /// The number of tokens earned per correct response.
        /// </summary>
        public int TokenGain;

        /// <summary>
        /// The number of tokens removed per incorrect response.
        /// </summary>
        public int TokenLoss;

        /// <summary>
        /// The number of tokens displayed in the token bar at the start of the trial.
        /// </summary>
        public int NumInitialTokens;

        /// <summary>
        /// The number of tokens that the token bar can hold.
        /// </summary>
        public int TokenBarCapacity;

        /// <summary>
        /// Whether or not the particle halo will be created after a selection is made
        /// </summary>
        public bool ParticleHaloActive;
        
        /// <summary>
        /// Whether or not the circle halo will be created after a selection is made
        /// </summary>
        public bool CircleHaloActive = true;

        /// <summary>
        /// MaskValues [TrialNum, MaskNum, Transparency]
        /// </summary>
        public Vector3[] MaskValues;

        /// <summary>
        /// MaskColor
        /// </summary>
        public Vector3 MaskColor;

        /// <summary>
        /// Mask Fade In Duration. Default is .75 seconds.
        /// </summary>
        public float MaskFadeInDuration = .75f;

        /// <summary>
        /// Stimulation Type. [FixationChoice_Target, FixationChoice_Distractor)
        /// </summary>
        public string StimulationType;

        /// <summary>
        /// Fixation time before sending the FixationOnsetPassed EventCode. 
        /// </summary>
        public float InitialFixationDuration = 0.3f;

        /// <summary>
        /// Delay before Stimulation
        /// </summary>
        public float StimulationDelayDuration = 0f;

        /// <summary>
        /// Stimulation Conditions Event Code list
        /// </summary>
        public int[] StimulationConditionCodes;

        /// <summary>
        /// Trials to Stimulate on for the Block
        /// </summary>
        public int[] TrialsToStimulateOn;

        /// <summary>
        /// Object Stimulation Order Number
        /// </summary>
        public int[][] ObjectsToStimulateOn;



        public int DifficultyLevel;
        public int posStep;
        public int numTrialsBeforePosStep;
        public int negStep;
        public int numTrialsBeforeNegStep;
    }

}
