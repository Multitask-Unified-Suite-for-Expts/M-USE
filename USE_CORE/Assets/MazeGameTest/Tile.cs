using UnityEngine;
using System.Collections;
using System.IO;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine.Serialization;
using USE_ExperimentTemplate_Trial;


public class Tile : MonoBehaviour
{

    // Tiles are distiguished by their (x, y) coordinate 
    // This means the bottom-left-most tile is (1, 1).
    public Coords mCoord;
   
    // DEFAULT MAZE CONFIGS - CONFIGURABLE IN TASK DEF/ TRIAL LEVEL
    public Color START_COLOR = new Color(0.94f, 0.93f, 0.48f);
    public Color FINISH_COLOR = new Color(0.37f, 0.59f, 0.94f);
    public Color CORRECT_COLOR = new Color(0.62f, 1f, 0.5f);
    public Color PREV_CORRECT_COLOR = new Color(0.2f, 0.7f, 0.5f);
    public Color INCORRECT_RULEABIDING_COLOR = new Color(1f, 0.5f, 0.25f);
    public Color INCORRECT_RULEBREAKING_COLOR = new Color(0f, 0f, 0f);
    public Color DEFAULT_TILE_COLOR = new Color(1, 1, 1);
    public int NUM_BLINKS = 10;

    // FEEDBACK LENGTH IN SECONDS
    public float CORRECT_FEEDBACK_SECONDS = 0.5f;
    public float PREV_CORRECT_FEEDBACK_SECONDS = 0.5f;
    public float INCORRECT_RULEABIDING_SECONDS = 0.5f;
    public float INCORRECT_RULEBREAKING_SECONDS = 1;
    public float TILE_BLINKING_DURATION = 2;
    public float TIMEOUT_SECONDS = 10.0f;

    private bool tileFlash;
    private Color fbColor;
    private Color originalTileColor;
    public Color baseColor;
    private int done = 0;
    void Start()
    {
        gameObject.GetComponent<Renderer>().material.color = baseColor;
    }


    public void OnMouseDown()
    {
        int correctnessCode;
        correctnessCode = MazeGame_TrialLevel.ManageTileTouch(this);
        MazeGame_TrialLevel.setEnd(correctnessCode);
        StartCoroutine(ColorFeedback(correctnessCode));
    }
   public void setColor(Color c)
    {
        baseColor = c;
    } 

    public IEnumerator ColorFeedback(int code)
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
        yield return new WaitForSeconds(MazeGame_TrialLevel.fbDuration);
        if (!MazeGame_TrialLevel.viewPath || code != 1)
            gameObject.GetComponent<Renderer>().material.color = originalTileColor;

    }

    public IEnumerator FlashingFeedback()
    {
            // FAILS TO SELECT LAST CORRECT AFTER ERROR
            fbColor = PREV_CORRECT_COLOR;
            originalTileColor = MazeGame_TrialLevel.pathProgressGO[MazeGame_TrialLevel.pathProgressGO.Count-1].
                GetComponent<Renderer>().material.color;
            float increment = TILE_BLINKING_DURATION / NUM_BLINKS;
            float flashingTime = 0f;
            while (flashingTime < TILE_BLINKING_DURATION)
            {
                MazeGame_TrialLevel.pathProgressGO[MazeGame_TrialLevel.pathProgressGO.Count-1].GetComponent<Renderer>().material.color = fbColor;
                yield return new WaitForSeconds(increment/2);
                MazeGame_TrialLevel.pathProgressGO[MazeGame_TrialLevel.pathProgressGO.Count-1].GetComponent<Renderer>().material.color = originalTileColor;
                yield return new WaitForSeconds(increment/2);
                flashingTime += increment;
            }
    }
}
