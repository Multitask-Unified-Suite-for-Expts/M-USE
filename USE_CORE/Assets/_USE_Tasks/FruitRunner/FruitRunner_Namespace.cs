using System;
using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;
using USE_Def_Namespace;
using USE_ExperimentTemplate_Classes;

namespace FruitRunner_Namespace
{
    public class FruitRunner_TaskDef : TaskDef
    {
    }

    public class FruitRunner_BlockDef : BlockDef
    {
    }

    public class FruitRunner_TrialDef : TrialDef
    {
        public int[] TrialStimIndices; //indices of the stim for the trial 
        public string[] TrialStimFeedback; //Positive, Negative, Neutral. Maps to TrialStimIndices.

        public int[][] TrialGroup_InSpawnOrder; //Trial stim indices in spawn order. Blockade is -1. 
        public string[][] TrialStimGeneralPositions; //Left, Middle, Right. Mapped to TrialGroup_InSpawnOrder.

        public int NumGroups;

        public Reward[][] ProbabilisticTokenReward; //Maps to trialStimIndices


        public bool RandomStimLocations;

        public float FloorMovementSpeed;
        public float FloorTileLength;
        public bool AllowItemPickupAnimations; //Happy and sad animations for when they go over a quaddle.
    }

    public class FruitRunner_StimDef : StimDef
    {
        public string QuaddleFeedbackType;
        //public string QuaddleGeneralPosition;

    }
}