using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Classes;

public static class FR_EventManager
{
    public static event Action<int> OnScoreChanged;

    public static event Action<string> OnTargetHit;
    public static event Action<string> OnTargetMissed;
    public static event Action<string> OnDistractorHit;
    public static event Action<string> OnDistractorAvoided;
    public static event Action<string> OnBlockadeHit;
    public static event Action<string> OnBlockadeAvoided;



    public static void TriggerTargetHit(string generalPos)
    {
        OnTargetHit?.Invoke(generalPos);
    }
    public static void TriggerTargetMissed(string generalPos)
    {
        OnTargetMissed?.Invoke(generalPos);
    }
    public static void TriggerDistractorHit(string generalPos)
    {
        OnDistractorHit?.Invoke(generalPos);
    }
    public static void TriggerDistractorAvoided(string generalPos)
    {
        OnDistractorAvoided?.Invoke(generalPos);
    }
    public static void TriggerBlockadeHit(string generalPos)
    {
        OnBlockadeHit?.Invoke(generalPos);
    }
    public static void TriggerBlockadeAvoided(string generalPos)
    {
        OnBlockadeAvoided?.Invoke(generalPos);
    }

    public static void TriggerScoreChanged(int scoreChange)
    {
        OnScoreChanged?.Invoke(scoreChange);
    }
}