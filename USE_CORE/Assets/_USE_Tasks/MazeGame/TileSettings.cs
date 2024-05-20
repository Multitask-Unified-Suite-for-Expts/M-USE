using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TileSettings : ScriptableObject
{
    [Header("Default Maze Configs")]
    private Color startColor = new Color(0.94f, 0.93f, 0.48f);
    private Color finishColor = new Color(0.37f, 0.59f, 0.94f);
    private Color correctColor = new Color(0.62f, 1f, 0.5f);
    private Color prevCorrectColor = new Color(0.2f, 0.7f, 0.5f);
    private Color incorrectRuleAbidingColor = new Color(1f, 0.5f, 0.25f);
    private Color incorrectRuleBreakingColor = new Color(0f, 0f, 0f);
    private Color defaultTileColor = new Color(1, 1, 1);
    private int numBlinks = 4;

    [Header("Feedback Length in Seconds")]
    private float correctFeedbackDuration = 0.5f;
    private float prevCorrectFeedbackDuration = 0.5f;
    private float incorrectRuleAbidingDuration = 0.5f;
    private float incorrectRuleBreakingDuration = 1;
    private float tileBlinkingDuration = 2;
    private float timeoutDuration = 10.0f;

    public float GetFeedbackDuration(string feedbackType)
    {
        switch (feedbackType)
        {
            case ("correct"):
                return correctFeedbackDuration;
            
            case ("prevCorrect"):
                return prevCorrectFeedbackDuration;
            
            case ("incorrectRuleAbiding"):
                return incorrectRuleAbidingDuration;
            
            case ("incorrectRuleBreaking"):
                return incorrectRuleBreakingDuration;
            
            case ("blinking"):
                return tileBlinkingDuration;
            
            case ("timeout"):
                return timeoutDuration;
           
            default: 
                Debug.Log("INVALID FEEDBACK TYPE, FEEDBACK DURATION OF 0 RETURNED");
                return 0;
        }
    }
    public void SetFeedbackDuration(string feedbackType, float duration)
    {
        switch (feedbackType)
        {
            case ("correct"):
                correctFeedbackDuration = duration;
                break;
            case ("prevCorrect"):
                prevCorrectFeedbackDuration = duration;
                break;
            case ("incorrectRuleAbiding"):
                incorrectRuleAbidingDuration = duration;
                break;
            case ("incorrectRuleBreaking"):
                incorrectRuleBreakingDuration = duration;
                break;
            case ("blinking"):
                tileBlinkingDuration = duration;
                break;
            case ("timeout"):
                timeoutDuration = duration;
                break;
        }
    }

    public void SetTileColor(string tileType, Color color)
    {
        switch (tileType)
        {
            case ("default"):
                defaultTileColor = color;
                break;
            case ("start"):
                startColor = color;
                break;
            case ("finish"):
                finishColor = color;
                break;
            case ("correct"):
                correctColor = color;
                break;
            case ("prevCorrect"):
                prevCorrectColor = color;
                break;
            case ("incorrectRuleAbiding"):
                incorrectRuleAbidingColor = color;
                break;
            case ("incorrectRuleBreaking"):
                incorrectRuleBreakingColor = color;
                break;

        }
    }

    public Color GetTileColor(string tileType)
    {
        switch (tileType)
        {
            case ("default"):
                return defaultTileColor;
            case ("start"):
                return startColor;
            case ("finish"):
                return finishColor;
            case ("correct"):
                return correctColor;
            case ("prevCorrect"):
                return prevCorrectColor;
            case ("incorrectRuleAbiding"):
                return incorrectRuleAbidingColor;
            case ("incorrectRuleBreaking"):
                return incorrectRuleBreakingColor;
            default:
                Debug.LogWarning("INVALID TILE TYPE, RETURNING COLOR (0, 0, 0)");
                return new Color(0, 0, 0);
        }
    }
    public void SetNumBlinks(int num)
    {
        numBlinks = num;
    }
    public int GetNumBlinks()
    {
        return numBlinks;
    }
}
