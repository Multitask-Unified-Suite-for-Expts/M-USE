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

    // Tiles are distiguished by their (x, y) coordinate 
    // This means the bottom-left-most tile is (1, 1).
    public Coords mCoord;
    public float sliderValueChange;
    private MazeGame_TrialLevel mgTL = null;
    private ChaseReactionTest_TrialLevel crtTL = null;

    // DEFAULT MAZE CONFIGS - CONFIGURABLE IN TASK DEF/ TRIAL LEVEL
    public Color START_COLOR = new Color(0.94f, 0.93f, 0.48f);
    public Color FINISH_COLOR = new Color(0.37f, 0.59f, 0.94f);
    public Color CORRECT_COLOR = new Color(0.62f, 1f, 0.5f);
    public Color PREV_CORRECT_COLOR = new Color(0.2f, 0.7f, 0.5f);
    public Color INCORRECT_RULEABIDING_COLOR = new Color(1f, 0.5f, 0.25f);
    public Color INCORRECT_RULEBREAKING_COLOR = new Color(0f, 0f, 0f);
    public Color DEFAULT_TILE_COLOR = new Color(1, 1, 1);
    public int NUM_BLINKS = 4;

    // FEEDBACK LENGTH IN SECONDS
    public float CORRECT_FEEDBACK_SECONDS = 0.5f;
    public float PREV_CORRECT_FEEDBACK_SECONDS = 0.5f;
    public float INCORRECT_RULEABIDING_SECONDS = 0.5f;
    public float INCORRECT_RULEBREAKING_SECONDS = 1;
    public float TILE_BLINKING_DURATION = 2;
    public float TIMEOUT_SECONDS = 10.0f;

    private bool tileFlash;
    private Color fbColor;
    public Color originalTileColor;
    public Color baseColor;
    private int done = 0;
    public bool isFlashing = false;
    private float flashStartTime;
    public GameObject flashingTile;
    private int numFlashes;

    public bool choiceFeedback;
    private float fbStartTime;
    private int correctnessCode;
    void Start()
    {
        gameObject.GetComponent<Renderer>().material.color = baseColor;

        if (GameObject.Find("MazeGame_Scripts") != null)
        {
            mgTL = GameObject.Find("MazeGame_Scripts").GetComponent<MazeGame_TrialLevel>();
        }
        else
        {
            crtTL = GameObject.Find("ChaseReactionTest_Scripts").GetComponent<ChaseReactionTest_TrialLevel>();
        }
    }


    public void OnMouseDown()
    {
            if (mgTL != null)
            {
                correctnessCode = mgTL.ManageTileTouch(this);
            }
            else
            {
                correctnessCode = crtTL.ManageTileTouch(this);
            }
            ColorFeedback(correctnessCode);
        
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
        
        gameObject.GetComponent<Renderer>().material.color = fbColor;
        fbStartTime = Time.unscaledTime;
        choiceFeedback = true;
    }

    public IEnumerator LastCorrectFlashingFeedback()
    {
        // FAILS TO SELECT LAST CORRECT AFTER ERROR
        fbColor = PREV_CORRECT_COLOR;
        GameObject flashingTile;
        isFlashing = true;
        if (mgTL.pathProgressGO.Count == 0) // haven't selected the start yet
            flashingTile = mgTL.startTile;
        else // somewhere along the path, can now index through pathProgress
            flashingTile = mgTL.pathProgressGO[mgTL.pathProgressGO.Count - 1];

        float increment = TILE_BLINKING_DURATION / NUM_BLINKS;
        float flashingTime = 0f;
        while (flashingTime < TILE_BLINKING_DURATION)
        {
            flashingTile.GetComponent<Renderer>().material.color = fbColor;
            yield return new WaitForSeconds(increment / 2);
            flashingTile.GetComponent<Renderer>().material.color = flashingTile.GetComponent<Tile>().originalTileColor;
            yield return new WaitForSeconds(increment / 2);
            flashingTime += increment;
        }

        isFlashing = false;
    }
    
    public void NextCorrectFlashingFeedback()
    {
        // ... code to set flashingTile, originalColor, and flashColor ...
        if (crtTL.pathProgressGO.Count == 0) // haven't selected the start yet
            flashingTile = crtTL.startTile;
        else
            flashingTile = GameObject.Find(crtTL.currentMaze_InTrial.mNextStep);

        isFlashing = true;
        flashStartTime = Time.unscaledTime;

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
                    flashingTile.GetComponent<Renderer>().material.color = flashingTile.GetComponent<Tile>().originalTileColor;
            
                numFlashes++;
            }
        
            if (numFlashes >= 2 * NUM_BLINKS)
            { 
                flashingTile.GetComponent<Renderer>().material.color = flashingTile.GetComponent<Tile>().originalTileColor;
                isFlashing = false;
            }

        }

        if (choiceFeedback)
        {

            float elapsed = Time.unscaledTime - fbStartTime;
            float interval = mgTL != null ? mgTL.tileFbDuration : crtTL.tileFbDuration;
        
            if (elapsed >=  interval)
            {
                if (mgTL != null? !mgTL.viewPath:!crtTL.viewPath || correctnessCode != 1)
                    gameObject.GetComponent<Renderer>().material.color = gameObject.GetComponent<Tile>().originalTileColor;
                choiceFeedback = false;
            }
        }

    }

}