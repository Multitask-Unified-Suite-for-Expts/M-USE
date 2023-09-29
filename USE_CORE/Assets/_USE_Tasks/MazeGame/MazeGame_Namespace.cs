using System.Collections.Generic;
using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;


namespace MazeGame_Namespace
{
    public class MazeGame_TaskDef : TaskDef
    {
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
    }

    public class MazeGame_BlockDef : BlockDef
    {
        public string MazeName;
        public Vector2 MazeDims;
        public string MazeStart;
        public string MazeFinish;
        public int MazeNumSquares;
        public int MazeNumTurns;
        public bool ViewPath;
        public bool ErrorPenalty;
        public int RewardRatio;
        public bool GuidedMazeSelection;
        public bool DarkenNonPathTiles;
        public int TileFlashingRatio = 1;
        public float MaxMazeDuration;
        public float MaxChoiceDuration;

        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            if (RandomMinMaxTrials != null)
            {
                MaxTrials = RandomNumGenerator.Next(RandomMinMaxTrials[0], RandomMinMaxTrials[1]);
                MinTrials = RandomMinMaxTrials[0];
            }
            else
            {
                MaxTrials = MinMaxTrials[1];
                MinTrials = MinMaxTrials[0];
            }

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
                td.SliderInitialValue = SliderInitialValue;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndType = BlockEndType;
                td.RandomMinMaxTrials = RandomMinMaxTrials;
                td.ErrorPenalty = ErrorPenalty;
                td.MaxTrials = MaxTrials;
                td.GuidedMazeSelection = GuidedMazeSelection;
                td.DarkenNonPathTiles = DarkenNonPathTiles;
                td.TileFlashingRatio = TileFlashingRatio;
                td.MaxMazeDuration = MaxMazeDuration;
                td.MaxChoiceDuration = MaxChoiceDuration;
                TrialDefs.Add(td);
            }
        }
    }

    public class MazeGame_TrialDef : TrialDef
    {
        public string MazeName;
        public Vector2 MazeDims;
        public string MazeStart;
        public string MazeFinish;
        public int MazeNumSquares;
        public int MazeNumTurns;
        public bool ViewPath;
        public bool ErrorPenalty;
        public int RewardRatio;
        public bool GuidedMazeSelection;
        public bool DarkenNonPathTiles;
        public int TileFlashingRatio;
        public float MaxMazeDuration;
        public float MaxChoiceDuration;
    }
    public class MazeGame_StimDef : StimDef
    {
        public bool IsTarget;
        public int TokenUpdate;
    }
    public class MazeDef
    {
        public string mName;
        public Vector2 mDims;
        public string mStart;
        public string mFinish;
        public int mNumSquares;
        public int mNumTurns;
        public string mString;
        
    }
}