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


using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SyncBoxController
{
    [HideInInspector] public SerialPortThreaded serialPortController;

    private readonly int MsBetweenRewardPulses = 200;


    public void SendCommand(string command)
    {
        serialPortController.AddToSend(command);
    }
    public void SendCommand(List<string> commands)
    {
        serialPortController.AddToSend(commands);
    }

    public void SendCommand(string command, List<string> codesToCheck)
    {
        serialPortController.AddToSend(command, codesToCheck);
    }

    public IEnumerator SendRewardPulses(int numPulses, int pulseSize)
    {
        Session.EventCodeManager.SendRangeCodeThisFrame("SyncBoxController_RewardPulseSent", numPulses);

        // Convert pulseSize (in 0.1ms units) to seconds
        float pulseDurationSeconds = pulseSize / 10000f; // 250 = 0.025s

        // Convert MsBetweenRewardPulses to seconds
        float gapDurationSeconds = Mathf.Max(MsBetweenRewardPulses / 1000f, 0.001f); // enforce minimum wait

        for (int i = 0; i < numPulses; i++)
        {
            serialPortController.AddToSend("RWD " + pulseSize);

            yield return new WaitForSeconds(pulseDurationSeconds);

            if (i < numPulses - 1) // don't delay after final pulse
                yield return new WaitForSeconds(gapDurationSeconds);
        }

        Session.SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses", numPulses));
    }


    //public IEnumerator SendRewardPulses(int numPulses, int pulseSize)
    //{
    //    Session.EventCodeManager.SendRangeCodeThisFrame("SyncBoxController_RewardPulseSent", numPulses); //moved out of for loop and changed to range

    //    for (int i = 0; i < numPulses; i++)
    //    {
    //        serialPortController.AddToSend("RWD " + pulseSize);//values less than 250 don't consistently work so use between 250-500 (# in 0.1 ms increments)
    //        float waitTime = (float)(MsBetweenRewardPulses + pulseSize / 10) / 1000;
            
    //        if(waitTime < .2)
    //            Debug.LogWarning("WAIT TIME IS LESS THAN .2s WHICH MEANS IT MAY SKIP PULSES | WAIT TIME = " + waitTime);

    //        yield return new WaitForSeconds(waitTime);
    //    }

    //    Session.SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses", numPulses));
    //}

    public IEnumerator SendSonication()
    {
        Session.EventCodeManager.SendCodeThisFrame(Session.EventCodeManager.SessionEventCodes["SyncBoxController_SonicationPulseSent"]);

        for (int i = 0; i < Session.SessionDef.StimulationNumPulses; i++)
        {
            serialPortController.AddToSend("RWB " + Session.SessionDef.StimulationPulseSize);
            float waitTime = (MsBetweenRewardPulses + Session.SessionDef.StimulationPulseSize / 10) / 1000;
            yield return new WaitForSeconds(waitTime);
        }

        Session.SessionInfoPanel.UpdateSessionSummaryValues(("totalStimulationPulses", Session.SessionDef.StimulationNumPulses));
    }


    public IEnumerator SendCameraSyncPulses()
    {
        Session.EventCodeManager.SendCodeThisFrame(Session.EventCodeManager.SessionEventCodes["SyncBoxController_SonicationPulseSent"]);

        for (int i = 0; i < Session.SessionDef.Camera_NumPulses; i++)
        {
            serialPortController.AddToSend("RWB " + Session.SessionDef.Camera_PulseSize_Ticks);
            float waitTime = (MsBetweenRewardPulses + Session.SessionDef.Camera_PulseSize_Ticks / 10) / 1000;
            yield return new WaitForSeconds(waitTime);
        }
    }

}
