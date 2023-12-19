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
    [HideInInspector] public MazeManager MazeManager;
    public TileSettings TileSettings;

    public List<GameObject> AdjacentTiles = new List<GameObject>();

    public Coords mCoord;

    public float sliderValueChange;
// Reference to the ScriptableObject holding the settings

    // Access settings through this instance
    public Color startColor => TileSettings.startColor;
    public Color finishColor => TileSettings.finishColor;
    public Color correctColor => TileSettings.correctColor;
    public Color prevCorrectColor => TileSettings.prevCorrectColor;
    public Color incorrectRuleAbidingColor => TileSettings.incorrectRuleAbidingColor;
    public Color incorrectRuleBreakingColor => TileSettings.incorrectRuleBreakingColor;
    public Color defaultTileColor => TileSettings.defaultTileColor;
    public int numBlinks => TileSettings.numBlinks;

    // Access feedback length settings
    public float correctFeedbackSeconds => TileSettings.correctFeedbackSeconds;
    public float prevCorrectFeedbackSeconds => TileSettings.prevCorrectFeedbackSeconds;
    public float incorrectRuleAbidingSeconds => TileSettings.incorrectRuleAbidingSeconds;
    public float incorrectRuleBreakingSeconds => TileSettings.incorrectRuleBreakingSeconds;
    public float tileBlinkingDuration => TileSettings.tileBlinkingDuration;
    public float timeoutSeconds => TileSettings.timeoutSeconds;

    private Color FBColor;
    private float flashStartTime;
    private float FBStartTime;
    private int CorrectnessCode;
    private int iFlashes;
    

    [HideInInspector] public Color initialTileColor;
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

    public void Initialize(TileSettings tileSettings, MazeManager mazeManager)
    {
        TileSettings = tileSettings;
        MazeManager = mazeManager;
    }
    public void SelectionFeedback()
    {
        if (!isFlashing)
        {
            CorrectnessCode = MazeManager.freePlay ? MazeManager.ManageFreePlayTileTouch(this) : MazeManager.ManageHiddenPathTileTouch(this);
           
            ColorFeedback(CorrectnessCode);
        }
    }
    public void setColor(Color c)
    {
        GetComponent<Image>().color = c;
    } 

    public void ColorFeedback(int code)
    {
        switch (code)
        {
            case 1:
                // CORRECT
                FBColor =  correctColor;
                break;
            case 2:
                // PREVIOUSLY CORRECT
                FBColor =  prevCorrectColor;
                break;
            case 10:
                // RULE-ABIDING INCORRECT
                FBColor =  incorrectRuleAbidingColor;
                break;
            case 20:
                // RULE-BREAKING INCORRECT
                FBColor = incorrectRuleBreakingColor;
                break;
        }

        initialTileColor = gameObject.GetComponent<Image>().color;
        gameObject.GetComponent<Image>().color = FBColor;
        FBStartTime = Time.unscaledTime;
        choiceFeedback = true;
    }

    public void FlashTile()
     {
    //     if (!mgTrialLevel.MazeManager.startedMaze) // haven't selected the start yet
    //         flashingTileGO = GameObject.Find(mgTrialLevel.MazeManager.currentMaze.mStart);
    //     else
    //         flashingTileGO = GameObject.Find(mgTrialLevel.MazeManager.currentMaze.mNextStep);

        iFlashes = 0;

        Tile flashingTile = this;
        isFlashing = true;
        flashStartTime = Time.unscaledTime;
        if (flashingTile.isStartTile)
            initialTileColor = startColor;
        else if (flashingTile.isFinishTile)
            initialTileColor = finishColor;
        else
            initialTileColor = defaultTileColor;// before it starts flashing set color
     }

    void Update()
    {
        if (isFlashing)
        {
            FBColor = prevCorrectColor;
            
            float elapsed = Time.unscaledTime - flashStartTime;
            float interval = tileBlinkingDuration / (2 * numBlinks);


            if (elapsed >= iFlashes * interval)
            {
                if (iFlashes % 2 == 0)
                    flashingTileGO.GetComponent<Image>().color = FBColor;
                else
                    flashingTileGO.GetComponent<Image>().color = initialTileColor;

                iFlashes++;
            }
        
            if (iFlashes >= 2 * numBlinks)
            { 
                flashingTileGO.GetComponent<Image>().color = initialTileColor; // confirm it stops on original tile color
                isFlashing = false;
            }
        }

        if (choiceFeedback && !isFlashing)
        {

            float elapsed = Time.unscaledTime - FBStartTime;
            float interval = MazeManager.mgTrialLevel.tileFbDuration;
        
            if (elapsed >=  interval)
            {
                if (!MazeManager.viewPath || CorrectnessCode != 1 && CorrectnessCode != 2)
                {
                    gameObject.GetComponent<Image>().color = initialTileColor;
                }
                else if(MazeManager.viewPath && CorrectnessCode == 2)
                    gameObject.GetComponent<Image>().color= correctColor;
               

                choiceFeedback = false;
            }
        }

    }

}