using System.Collections.Generic;
using UnityEngine;
using USE_Def_Namespace;
using USE_ExperimentTemplate_Task;
using USE_StimulusManagement;

namespace MazeGame_Namespace
{
    public class MazeGame_TaskDef : TaskDef
    {
        public bool NeutralITI;
        public float TileSize;
        public string TileTexture;
        public int NumBlinks;
        public float[] StartColor;
        public float[] FinishColor;
        public float[] CorrectColor;
        public float[] LastCorrectColor;
        public float[] IncorrectRuleAbidingColor;
        public float[] IncorrectRuleBreakingColor;
        public float[] DefaultTileColor;
        public bool UsingFixedRatioReward;
        public string MazeBackgroundTextureName;
    }

    public class MazeGame_BlockDef : BlockDef
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
            int num = RandomNumGenerator.Next(MinMaxTrials[0], MinMaxTrials[1]+1);
            
            TrialDefs = new List<MazeGame_TrialDef>().ConvertAll(x => (TrialDef)x);
            
            for (int iTrial = 0; iTrial < num; iTrial++)
            {
                MazeGame_TrialDef td = new MazeGame_TrialDef();
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

    public class MazeGame_TrialDef : TrialDef
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

    public class MazeGame_StimDef : StimDef
    {
    }

    public class MazeDef : CustomSettingsType
    {
        public Vector2 mDims;
        public int mNumTurns;
        public int mNumSquares;
        public string mStart;
        public string mFinish;
        public string mName;
        
    }
}