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
        public int NumGroups;
        public Reward[][] ProbablisticTrialStimTokenReward;
        public int[] TrialStimIndices;
        public string[] TrialStimTypes; //Positive, Negative, Neutral
        public string[] TrialStimGeneralPositions; //Left, Middle, Right

    }

    public class FruitRunner_StimDef : StimDef
    {
        public string QuaddleType;
        public string QuaddleGeneralPosition;
    }
}