using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using USE_ExperimentTemplate_Classes;

public class SyncBoxController
{
    [HideInInspector] public SerialPortThreaded serialPortController;
    // private string filePrefix;

    private bool usingSonication; //idk said something about adding to the trial level
    private bool sonicationSentThisFrame;
    private bool sonicationSentThisTrial;
    private int numConsecutiveSonicationTrials;
    private bool rewardFinished;
    private int MsBetweenRewardPulses;
    public bool sonicationBlockedThisTrial;
    public int numTrialsUntilNextSonication;
    public int? maxConsecutiveSonicationTrials;
    private string ultrasoundTriggerDurationTicks;
    private int numTrialsWithoutSonicationAfterMax;

    public EventCodeManager EventCodeManager;
    public Dictionary<string, EventCode> SessionEventCodes;

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
        if (usingSonication)
        {
            SendSonication();
        }
        for (int i = 0; i < numPulses; i++)
        {
            serialPortController.AddToSend("RWD " + pulseSize);//values less than 20 don't consistently work so use between 20-100 (# in 0.1 ms increments)
            EventCodeManager.SendCodeImmediate(SessionEventCodes["SyncBoxController_RewardPulseSent"]);
            MsBetweenRewardPulses = 200;
            Thread.Sleep(MsBetweenRewardPulses + pulseSize/10);
        }
        rewardFinished = true;
    }
    void SendSonication()
    {
        if ((maxConsecutiveSonicationTrials == null ||
            numConsecutiveSonicationTrials < maxConsecutiveSonicationTrials && numTrialsUntilNextSonication == 0) &&
            sonicationSentThisTrial == false)
        {
            serialPortController.AddToSend("RWB " + ultrasoundTriggerDurationTicks);
            EventCodeManager.SendCodeImmediate(SessionEventCodes["SyncBoxController_SonicationPulseSent"]);
            sonicationSentThisFrame = true;
            sonicationSentThisTrial = true;
            numConsecutiveSonicationTrials += 1;
            if (numConsecutiveSonicationTrials == maxConsecutiveSonicationTrials)
                numTrialsUntilNextSonication = numTrialsWithoutSonicationAfterMax + 1; //+1 because subtraction happens at end of trial
        }
        else
        {
            sonicationBlockedThisTrial = true;
        }
    }
    
}