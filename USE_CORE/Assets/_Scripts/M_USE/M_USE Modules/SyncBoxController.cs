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
using System.Threading;
using UnityEngine;


public class SyncBoxController
{
    [HideInInspector] public SerialPortThreaded serialPortController;

    private int MsBetweenRewardPulses = 200; //MAKE THIS A CONFIGURABLE VARIABLE!


    public void SendCommand(string command)
    {
        serialPortController.AddToSend(command);
    }
    public void SendCommand(List<string> command)
    {
        serialPortController.AddToSend(command);
    }

    public void SendCommand(string command, List<string> codesToCheck)
    {
        serialPortController.AddToSend(command, codesToCheck);
    }
    
    public void SendRewardPulses(int numPulses, int pulseSize)
    {
        Session.EventCodeManager.SendRangeCode("SyncBoxController_RewardPulseSent", numPulses); //moved out of for loop and changed to range

        for (int i = 0; i < numPulses; i++)
        {
            serialPortController.AddToSend("RWD " + pulseSize);//values less than 250 don't consistently work so use between 250-500 (# in 0.1 ms increments)
            Thread.Sleep(MsBetweenRewardPulses + pulseSize/10);
        }
        Session.SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses", numPulses));
    }

    public void SendCameraSyncPulses(int numPulses, int pulseSize)
    {
        for (int i = 0; i < numPulses; i++)
        {
            serialPortController.AddToSend("RWB " + pulseSize);
            Thread.Sleep(MsBetweenRewardPulses + pulseSize / 10);
        }
    }

    public void SendSonication()
    {
        int numPulses = 2;
        int pulseSize = 250;

        for (int i = 0; i < numPulses; i++)
        {
            serialPortController.AddToSend("RWB " + pulseSize);
            Thread.Sleep(MsBetweenRewardPulses + pulseSize / 10);
        }

        Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["SyncBoxController_SonicationPulseSent"]);
    }


}
