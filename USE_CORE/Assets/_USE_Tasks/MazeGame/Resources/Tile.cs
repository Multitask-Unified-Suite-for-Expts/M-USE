using System;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;
using System.Timers;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine.Serialization;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;


public class Tile : MonoBehaviour
{
    // SET IN THE INSPECTOR
    public MazeGame_TrialLevel mgTL;
    
    [HideInInspector] public Coords mCoord;
    [HideInInspector] public float sliderValueChange;
    
    // DEFAULT MAZE CONFIGS - CONFIGURABLE IN TASK DEF/ TRIAL LEVEL
    [HideInInspector]public Color START_COLOR = new Color(0.94f, 0.93f, 0.48f);
    [HideInInspector] public Color FINISH_COLOR = new Color(0.37f, 0.59f, 0.94f);
    [HideInInspector] public Color CORRECT_COLOR = new Color(0.62f, 1f, 0.5f);
    [HideInInspector] public Color PREV_CORRECT_COLOR = new Color(0.2f, 0.7f, 0.5f);
    [HideInInspector] public Color INCORRECT_RULEABIDING_COLOR = new Color(1f, 0.5f, 0.25f);
    [HideInInspector] public Color INCORRECT_RULEBREAKING_COLOR = new Color(0f, 0f, 0f);
    [HideInInspector] public Color DEFAULT_TILE_COLOR = new Color(1, 1, 1);
    [HideInInspector] public int NUM_BLINKS = 4;

    // FEEDBACK LENGTH IN SECONDS
    [HideInInspector] public float CORRECT_FEEDBACK_SECONDS = 0.5f;
    [HideInInspector] public float PREV_CORRECT_FEEDBACK_SECONDS = 0.5f;
    [HideInInspector] public float INCORRECT_RULEABIDING_SECONDS = 0.5f;
    [HideInInspector] public float INCORRECT_RULEBREAKING_SECONDS = 1;
    [HideInInspector] public float TILE_BLINKING_DURATION = 2;
    [HideInInspector] public float TIMEOUT_SECONDS = 10.0f;

    private Color fbColor;
    private float flashStartTime;
    private float fbStartTime;
    private int correctnessCode;
    private int numFlashes;

    [HideInInspector] public Color originalTileColor;
    [HideInInspector] public Color baseColor;
    [HideInInspector] public bool isFlashing = false;
    [HideInInspector] public bool assignedTileFlash;
    [HideInInspector] public bool choiceFeedback;
    [HideInInspector] public GameObject flashingTile;


    void Start()
    {
        gameObject.GetComponent<Renderer>().material.color = baseColor;
    }


    public void SelectionFeedback()
    {
        if (!isFlashing)
        {
            correctnessCode = mgTL.ManageTileTouch(this);
            ColorFeedback(correctnessCode);
        }
    }
    public void setColor(Color c)
    {
        baseColor = c;
    } 

    public void ColorFeedback(int code)
    {
        switch (code)
        {
            case 1:
                // CORRECT
                fbColor =  CORRECT_COLOR;
                break;
            case 2:
                // PREVIOUSLY CORRECT
                fbColor =  PREV_CORRECT_COLOR;
                break;
            case 10:
                // RULE-ABIDING INCORRECT
                fbColor =  INCORRECT_RULEABIDING_COLOR;
                break;
            case 20:
                // RULE-BREAKING INCORRECT
                fbColor = INCORRECT_RULEBREAKING_COLOR;
                break;
        }

        originalTileColor = gameObject.GetComponent<Renderer>().material.color;
        gameObject.GetComponent<Renderer>().material.color = fbColor;
        fbStartTime = Time.unscaledTime;
        choiceFeedback = true;
    }

    // public void LastCorrectFlashingFeedback()
    // {
    //     // FAILS TO SELECT LAST CORRECT AFTER ERROR
    //     fbColor = PREV_CORRECT_COLOR;
    //     if (mgTL.pathProgressGO.Count == 0) // haven't selected the start yet
    //         flashingTile = mgTL.startTile;
    //     else // somewhere along the path, can now index through pathProgress
    //         flashingTile = mgTL.pathProgressGO[mgTL.pathProgressGO.Count - 1];
    //
    //     isFlashing = true;
    //     flashStartTime = Time.unscaledTime;
    //     if 
    //     originalTileColor = flashingTile.GetComponent<Renderer>().material.color; // before it starts flashing set color
    //     numFlashes = 0;
    // }
    
    public void NextCorrectFlashingFeedback()
    {
        if (mgTL.pathProgressGO.Count == 0) // haven't selected the start yet
            flashingTile = mgTL.startTile;
        else
            flashingTile = GameObject.Find(mgTL.CurrentTaskLevel.currMaze.mNextStep);

        isFlashing = true;
        flashStartTime = Time.unscaledTime;
        if (flashingTile == mgTL.startTile)
            originalTileColor = START_COLOR;
        else if (flashingTile == mgTL.finishTile)
            originalTileColor = FINISH_COLOR;
        else
            originalTileColor = DEFAULT_TILE_COLOR;// before it starts flashing set color
        numFlashes = 0;
    }

    void Update()
    {
        if (isFlashing)
        {
            fbColor = PREV_CORRECT_COLOR;
            
            float elapsed = Time.unscaledTime - flashStartTime;
            float interval = TILE_BLINKING_DURATION / (2 * NUM_BLINKS);
        
            if (elapsed >= numFlashes * interval)
            {
                if (numFlashes % 2 == 0)
                    flashingTile.GetComponent<Renderer>().material.color = fbColor;
                else
                    flashingTile.GetComponent<Renderer>().material.color = originalTileColor;

                numFlashes++;
            }
        
            if (numFlashes >= 2 * NUM_BLINKS)
            { 
                flashingTile.GetComponent<Renderer>().material.color = originalTileColor; // confirm it stops on original tile color
                isFlashing = false;
            }
        }

        if (choiceFeedback && !isFlashing)
        {

            float elapsed = Time.unscaledTime - fbStartTime;
            float interval = mgTL.tileFbDuration;
        
            if (elapsed >=  interval)
            {
                if (!mgTL.viewPath || correctnessCode != 1 && correctnessCode != 2)
                {
                    gameObject.GetComponent<Renderer>().material.color = originalTileColor;
                }
                 else if(mgTL.viewPath && correctnessCode == 2)
                     gameObject.GetComponent<Renderer>().material.color = CORRECT_COLOR;
               

                choiceFeedback = false;
            }
        }

    }

}