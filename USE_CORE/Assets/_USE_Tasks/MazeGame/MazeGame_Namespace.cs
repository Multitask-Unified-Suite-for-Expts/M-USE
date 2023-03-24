using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using HiddenMaze;
namespace MazeGame_Namespace
{
    public class MazeGame_TaskDef : TaskDef
    {


    }

    public class MazeGame_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
        //public int BlockCount;
        //public TrialDef[] TrialDefs;

       // public string TrialID;
       // public int Context;
        public int Trial;
        public string TrialID;
        public string BlockName;
        public int[] MinMaxTrials;
        public int RewardRatio;
        public string MazeInfo;

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
            //pick # of trials from minmaxokay 
            
            System.Random rnd = new System.Random();
            int num = rnd.Next(MinMaxTrials[0], MinMaxTrials[1]);

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
        //Already-existing fields (inherited from TrialDef)
        //public int BlockCount, TrialCountInBlock, TrialCountInTask;
        //public TrialStims TrialStims;
        public string TrialID;
        public string BlockName;
        public float[] TileColor;
        public float TileSize;
        public int PulseSize;
        public int NumPulses;
        public int RewardRatio;
        /*public int Texture;
        public Vector2 MazeDims;
        public Vector2 MazeStart;
        public Vector2 MazeFinish;
        public int MazeNumSquares;
        public int MazeNumTurns;*/
        public bool ViewPath;
        public bool ErrorPenalty;
        public string ContextName;
        public string MazeName;
        public int SliderInitial;
        public MazeDef MazeDef;
        public int MaxTrials;
        
        public string BlockEndType;
        public float BlockEndThreshold;
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
        
    }
}