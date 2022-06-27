using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using USE_Data;
using USE_ExperimentTemplate;
using USE_Settings;

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

    //WhatWhenWhere_TrialLevel trialLevel;

    // Start is called before the first frame update
    /*
    public void InitiializeSyncBox(string filePrefix, List<string> syncBoxInitCommands, bool runFlashPatches = false)
    {
        if (eventCodeManager.codesActive)
        {
            serialPortController.Initialize();
            
            //do something clever with data files
            // serialSentData.fileName = filePrefix + "__SerialSent_PreTrial.txt";
            // serialSentData.CreateFile();
            // serialRecvData.fileName = filePrefix + "__SerialRecv_PreTrial.txt";
            // serialRecvData.CreateFile();
        }

        serialPortController.AddToSend(syncBoxInitCommands);
        
        eyeTrackType = (int)SessionSettings.Get("sessionConfig", "eyeTrackType");
        if (eventCodeManager.codesActive)
        {
            serialPortController.ClosePort();
        }
        
        
        if (runFlashPatches)
            serialPortController.AddToSend("CAF 40000");
    }
*/
    public void SendCommand(string command)
    {
        serialPortController.AddToSend(command);
    }
    public void SendCommand(List<string> command)
    {
        serialPortController.AddToSend(command);
    }
    private void SendRewardPulses(int numPulses, int pulseSize)
    {
        if (usingSonication)
        {
            SendSonication();
        }
        for (int i = 0; i < numPulses; i++)
        {
            serialPortController.AddToSend("RWD " + pulseSize);//values less than 20 don't consistently work so use between 20-100 (# in 0.1 ms increments)
            //eventCodeManager.SendCodeImmediate(eventCodes.Fluid1Onset.Value);
            Thread.Sleep(MsBetweenRewardPulses);
        }
        rewardFinished = true;
        Thread.CurrentThread.Abort();
    }
    void SendSonication()
    {
        if ((maxConsecutiveSonicationTrials == null ||
            numConsecutiveSonicationTrials < maxConsecutiveSonicationTrials && numTrialsUntilNextSonication == 0) &&
            sonicationSentThisTrial == false)
        {
            serialPortController.AddToSend("RWB " + ultrasoundTriggerDurationTicks);
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
