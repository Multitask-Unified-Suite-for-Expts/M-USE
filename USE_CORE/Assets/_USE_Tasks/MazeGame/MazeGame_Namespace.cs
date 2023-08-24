using System.Collections.Generic;
using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;


namespace MazeGame_Namespace
{
    public class MazeGame_TaskDef : TaskDef
    {
        // public string MazeKeyFilePath;
        // public string MazeFilePath;

        public Vector3 MazePosition;

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
        public string MazeBackgroundTexture;
        public float SpaceBetweenTiles;
        public bool GuidedMazeSelection;
    }

    public class MazeGame_BlockDef : BlockDef
    {
        public int RewardRatio;

        public Vector2 MazeDims;
        public string MazeStart, MazeFinish;
        public int MazeNumSquares;
        public int MazeNumTurns;
        public string MazeName;
        public bool ViewPath;
        public int SliderInitial;
        public bool ErrorPenalty;
        
        
        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            MaxTrials = RandomNumGenerator.Next(MinMaxTrials[0], MinMaxTrials[1]+1);
            
            TrialDefs = new List<MazeGame_TrialDef>().ConvertAll(x => (TrialDef)x);
            
            for (int iTrial = 0; iTrial < MaxTrials; iTrial++)
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
                td.MaxTrials = MaxTrials;
                TrialDefs.Add(td);
            }
        }
    }

    public class MazeGame_TrialDef : TrialDef
    {
        public int RewardRatio;
        public bool ViewPath;
        public bool ErrorPenalty;
        public string MazeName;
        public int SliderInitial;
        public int[] MinMaxTrials;
    }

    public class MazeGame_StimDef : StimDef
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
        public string mString;
        
    }
}