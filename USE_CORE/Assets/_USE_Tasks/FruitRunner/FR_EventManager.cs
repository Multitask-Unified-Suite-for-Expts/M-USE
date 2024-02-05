using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class FR_EventManager
{
    public static event Action<int> OnScoreChanged;


    public static void TriggerScoreChanged(int scoreChange)
    {
        OnScoreChanged?.Invoke(scoreChange);
    }
}
