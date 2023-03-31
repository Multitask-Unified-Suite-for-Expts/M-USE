using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using HiddenMaze;
namespace ChaseReactionTest_Namespace
{
    public class ChaseReactionTest_TaskDef : TaskDef
    {
    }

    public class ChaseReactionTest_BlockDef : BlockDef
    {
        public string BlockName;
        public int[] MinMaxTrials;
        public int RewardRatio;

        public Vector2 MazeDims;
        public string MazeStart, MazeFinish;
        public int MazeNumSquares;
        public int MazeNumTurns;
        public string MazeName;
        
        public int PulseSize;
        public int NumPulses;
        public bool ViewPath;
        public string ContextName;
        public int SliderInitial;
        
        public string BlockEndType;
        public float BlockEndThreshold;
        public bool ErrorPenalty;
        
        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            
            System.Random rnd = new System.Random();
            int num = rnd.Next(MinMaxTrials[0], MinMaxTrials[1]+1);
            
            TrialDefs = new List<ChaseReactionTest_TrialDef>().ConvertAll(x => (TrialDef)x);
            
            for (int iTrial = 0; iTrial < num; iTrial++)
            {
                ChaseReactionTest_TrialDef td = new ChaseReactionTest_TrialDef();
                td.BlockName = BlockName;
                td.RewardRatio = RewardRatio;
                td.NumPulses = NumPulses;
                td.PulseSize = PulseSize;
                td.ViewPath = ViewPath;
                td.ContextName = ContextName;
                td.MazeName = MazeName;
                td.SliderInitial = SliderInitial;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndType = BlockEndType;
                td.MinMaxTrials = MinMaxTrials;
                td.ErrorPenalty = ErrorPenalty;
                td.MaxTrials = num;
                TrialDefs.Add(td);
            }
        }
    }

    public class ChaseReactionTest_TrialDef : TrialDef
    {
        public string BlockName;
        public int PulseSize;
        public int NumPulses;
        public int RewardRatio;
        public bool ViewPath;
        public bool ErrorPenalty;
        public string ContextName;
        public string MazeName;
        public int SliderInitial;
        public int MaxTrials;
        public string BlockEndType;
        public float BlockEndThreshold;
        public int[] MinMaxTrials;
    }

    public class ChaseReactionTest_StimDef : StimDef
    {
    }
    public class MazeDef
    {
        public Vector2 mDims;
        public int mNumTurns;
        public int mNumSquares;
        public string mStart;
        public string mFinish;
        public string mName;
        
    }
}