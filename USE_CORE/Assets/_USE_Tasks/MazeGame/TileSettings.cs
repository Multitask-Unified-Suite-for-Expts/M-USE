using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TileSettings : ScriptableObject
{
    [Header("Default Maze Configs")]
    public Color startColor = new Color(0.94f, 0.93f, 0.48f);
    public Color finishColor = new Color(0.37f, 0.59f, 0.94f);
    public Color correctColor = new Color(0.62f, 1f, 0.5f);
    public Color prevCorrectColor = new Color(0.2f, 0.7f, 0.5f);
    public Color incorrectRuleAbidingColor = new Color(1f, 0.5f, 0.25f);
    public Color incorrectRuleBreakingColor = new Color(0f, 0f, 0f);
    public Color defaultTileColor = new Color(1, 1, 1);
    public int numBlinks = 4;

    [Header("Feedback Length in Seconds")]
    public float correctFeedbackSeconds = 0.5f;
    public float prevCorrectFeedbackSeconds = 0.5f;
    public float incorrectRuleAbidingSeconds = 0.5f;
    public float incorrectRuleBreakingSeconds = 1;
    public float tileBlinkingDuration = 2;
    public float timeoutSeconds = 10.0f;
    public float tileFbDuration;

}
