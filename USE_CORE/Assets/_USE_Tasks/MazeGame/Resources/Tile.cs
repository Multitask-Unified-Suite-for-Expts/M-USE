using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine.Serialization;
using UnityEngine.UI;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;


public class Tile : MonoBehaviour
{
    // SET IN THE INSPECTOR
    public MazeGame_TrialLevel mgTL;
    
    [HideInInspector] public Coords mCoord;
    [HideInInspector] public float sliderValueChange;

    public List<GameObject> AdjacentTiles = new List<GameObject>();
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

    private Color FBColor;
    private float FlashStartTime;
    private float FBStartTime;
    private int CorrectnessCode;
    private int NumFlashes;
    

    [HideInInspector] public Color InitialTileColor;
    [HideInInspector] public Color baseColor;
    [HideInInspector] public bool isFlashing = false;
    [HideInInspector] public bool assignedTileFlash;
    [HideInInspector] public bool choiceFeedback;
    [HideInInspector] public bool isStartTile;
    [HideInInspector] public bool isFinishTile;

    [FormerlySerializedAs("flashingTile")] [HideInInspector] public GameObject flashingTileGO;

    void Start()
    {
        //gameObject.GetComponent<Image>().color = baseColor;
    }


    public void SelectionFeedback()
    {
        if (!isFlashing)
        {
            CorrectnessCode = mgTL.mazeManager.ManageTileTouch(this);
            ColorFeedback(CorrectnessCode);
        }
    }
    public void setColor(Color c)
    {
        gameObject.GetComponent<Image>().color = c;
    } 

    public void ColorFeedback(int code)
    {
        switch (code)
        {
            case 1:
                // CORRECT
                FBColor =  CORRECT_COLOR;
                break;
            case 2:
                // PREVIOUSLY CORRECT
                FBColor =  PREV_CORRECT_COLOR;
                break;
            case 10:
                // RULE-ABIDING INCORRECT
                FBColor =  INCORRECT_RULEABIDING_COLOR;
                break;
            case 20:
                // RULE-BREAKING INCORRECT
                FBColor = INCORRECT_RULEBREAKING_COLOR;
                break;
        }

        InitialTileColor = gameObject.GetComponent<Image>().color;
        gameObject.GetComponent<Image>().color = FBColor;
        FBStartTime = Time.unscaledTime;
        choiceFeedback = true;
    }

    public void NextCorrectFlashingFeedback()
    {
        if (!mgTL.mazeManager.startedMaze) // haven't selected the start yet
            flashingTileGO = GameObject.Find(mgTL.mazeManager.currentMaze.mStart);
        else
            flashingTileGO = GameObject.Find(mgTL.mazeManager.currentMaze.mNextStep);

        Tile flashingTile = flashingTileGO.GetComponent<Tile>();
        isFlashing = true;
        FlashStartTime = Time.unscaledTime;
        if (flashingTile.isStartTile)
            InitialTileColor = START_COLOR;
        else if (flashingTile.isFinishTile)
            InitialTileColor = FINISH_COLOR;
        else
            InitialTileColor = DEFAULT_TILE_COLOR;// before it starts flashing set color
        NumFlashes = 0;
    }

    void Update()
    {
        if (isFlashing)
        {
            FBColor = PREV_CORRECT_COLOR;
            
            float elapsed = Time.unscaledTime - FlashStartTime;
            float interval = TILE_BLINKING_DURATION / (2 * NUM_BLINKS);


            if (elapsed >= NumFlashes * interval)
            {
                if (NumFlashes % 2 == 0)
                    flashingTileGO.GetComponent<Image>().color = FBColor;
                else
                    flashingTileGO.GetComponent<Image>().color = InitialTileColor;

                NumFlashes++;
            }
        
            if (NumFlashes >= 2 * NUM_BLINKS)
            { 
                flashingTileGO.GetComponent<Image>().color = InitialTileColor; // confirm it stops on original tile color
                isFlashing = false;
            }
        }

        if (choiceFeedback && !isFlashing)
        {

            float elapsed = Time.unscaledTime - FBStartTime;
            float interval = mgTL.tileFbDuration;
        
            if (elapsed >=  interval)
            {
                if (!mgTL.viewPath || CorrectnessCode != 1 && CorrectnessCode != 2)
                {
                    gameObject.GetComponent<Image>().color = InitialTileColor;
                }
                 else if(mgTL.viewPath && CorrectnessCode == 2)
                     gameObject.GetComponent<Image>().color= CORRECT_COLOR;
               

                choiceFeedback = false;
            }
        }

    }

}