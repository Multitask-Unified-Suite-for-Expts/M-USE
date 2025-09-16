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

    private readonly float SecBetweenRewardPulses = 0.2f;


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

        Session.SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses", numPulses));

        // Convert pulseSize (in 0.1ms units) to seconds
        float pulseDurationSeconds = pulseSize / 10000f; // 250 = 0.025s

        if(pulseDurationSeconds < .025)
            Debug.LogError("PULSE SIZE MUST BE LESS THAN 250! THIS COULD CAUSE TIMING TO BE OFF AND PULSES TO BE SKIPPED");

        for (int i = 0; i < numPulses; i++)
        {
            serialPortController.AddToSend("RWD " + pulseSize);

            yield return new WaitForSeconds(pulseDurationSeconds);

            if (i < numPulses - 1) // don't delay after final pulse
                yield return new WaitForSeconds(SecBetweenRewardPulses);
        }


    }

    public IEnumerator SendSonication()
    {
        Debug.LogWarning("ABOUT TO START SONICATION");

        Session.EventCodeManager.SendCodeThisFrame(Session.EventCodeManager.SessionEventCodes["SyncBoxController_SonicationPulseSent"]);

        Session.SessionInfoPanel.UpdateSessionSummaryValues(("totalStimulationPulses", Session.SessionDef.StimulationNumPulses));

        // Convert pulseSize (in 0.1ms units) to seconds
        float pulseDurationSeconds = Session.SessionDef.StimulationPulseSize / 10000f; // 250 = 0.025s

        if(pulseDurationSeconds < .025)
            Debug.LogError("SESSION CONFIG PULSE SIZE MUST BE LESS THAN 250! THIS COULD CAUSE TIMING TO BE OFF AND PULSES TO BE SKIPPED");

        for (int i = 0; i < Session.SessionDef.StimulationNumPulses; i++)
        {
            serialPortController.AddToSend("RWB " + Session.SessionDef.StimulationPulseSize);

            yield return new WaitForSeconds(pulseDurationSeconds);

            if (i < Session.SessionDef.StimulationNumPulses - 1) // don't delay after final pulse
                yield return new WaitForSeconds(SecBetweenRewardPulses);
        }

    }


}
