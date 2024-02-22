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
        public bool DarkenNonPathTiles;
        public int TileFlashingRatio = 1;
        public float MaxMazeDuration;
        public float MaxChoiceDuration;
        public float[] DefaultTileColor;
        public string MazeDef;
        public Dictionary<string, string> Landmarks;
        public List<string> Blockades;



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
                td.DarkenNonPathTiles = DarkenNonPathTiles;
                td.TileFlashingRatio = TileFlashingRatio;
                td.MaxMazeDuration = MaxMazeDuration;
                td.MaxChoiceDuration = MaxChoiceDuration;
                td.DefaultTileColor = DefaultTileColor;
                td.MazeDef = MazeDef;
                td.Landmarks = Landmarks;
                td.Blockades = Blockades;
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
        public bool DarkenNonPathTiles;
        public int TileFlashingRatio;
        public float MaxMazeDuration;
        public float MaxChoiceDuration;
        public float[] DefaultTileColor;
        public string MazeDef;
        public Dictionary<string, string> Landmarks;
        public List<string> Blockades;

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
    public class MazeGame_BlockDataSummary
    {
        public int BlockNum;
        public int TotalTouches;
        public int IncompleteTouches;
        public int CorrectTouches;
        public int RetouchCorrect;
        public int IncorrectTouches;
        public int RuleAbidingErrors;
        public int PerseverativeRuleAbidingErrors;
        public int RuleBreakingErrors;
        public int PerseverativeRuleBreakingErrors;
        public int BacktrackErrors;
        public int PerseverativeBackTrackErrors;
        public int RetouchError;
        public int PerseverativeRetouchErrors;
        public string AvgMazeDuration;
        public string AvgChoiceDuration;
        public float NumCompletedTrials;
        public int TrialsToCriterion;
    }


}