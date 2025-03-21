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
using UnityEngine;
using USE_StimulusManagement;
using USE_Def_Namespace;


namespace GazeCalibration_Namespace
{
    public class GazeCalibration_TaskDef : TaskDef
    {
        public string RewardStructure = "PerPoint";
    }

    public class GazeCalibration_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
        //public int BlockCount;
        //public TrialDef[] TrialDefs;
        public float[] CalibPointsInset;
        public float MaxCircleScale;
        public float MinCircleScale;
        public float ShrinkDuration;
        public float ConfirmDuration;

        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<GazeCalibration_TrialDef>().ConvertAll(x => (TrialDef)x);

            //for (int iTrial = 0; iTrial < NumTrials; iTrial++)
            //{
            GazeCalibration_TrialDef td = new GazeCalibration_TrialDef();
            td.NumPulses = NumPulses;
            td.PulseSize = PulseSize;
            td.CalibPointsInset = CalibPointsInset;
            td.MaxCircleScale = MaxCircleScale;
            td.MinCircleScale = MinCircleScale;
            td.ShrinkDuration = ShrinkDuration;
            td.ConfirmDuration = ConfirmDuration;

            TrialDefs.Add(td);
            //}
        }
    }

    public class GazeCalibration_TrialDef : TrialDef
    {
        //Already-existing fields (inherited from TrialDef)
        //public int BlockCount, TrialCountInBlock, TrialCountInTask;
        //public TrialStims TrialStims;
       // public int BlockID;
       // public int NumTrials;
        public float[] CalibPointsInset = new float[] {0.15f, 0.15f};
        public float MaxCircleScale = 0.075f;
        public float MinCircleScale = 0.015f;
        public float ShrinkDuration = 1.5f;
        public float ConfirmDuration = 1.5f;
    }

    public class GazeCalibration_StimDef : StimDef
    {

    }

}