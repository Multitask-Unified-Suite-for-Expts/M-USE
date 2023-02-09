using UnityEngine;
using System.Collections;
using System.IO;
using HiddenMaze;
using MazeGame_Namespace;
using USE_ExperimentTemplate_Trial;


public class Tile : MonoBehaviour
{

    // Tiles are distiguished by their (x, y) coordinate using standard C1 coordinate system, zero-indexed
    // This means the bottom-left-most tile is (0, 0).
    public Coords mCoord;
   
    // DEFAULT MAZE CONFIGS - CONFIGURABLE IN TASK DEF/ TRIAL LEVEL
    public Color START_COLOR = new Color(0.94f, 0.93f, 0.48f);
    public Color FINISH_COLOR = new Color(0.37f, 0.59f, 0.94f);
    public Color CORRECT_COLOR = new Color(0.62f, 1f, 0.5f);
    public Color LAST_CORRECT_COLOR = new Color(0.2f, 0.7f, 0.5f);
    public Color INCORRECT_RULEABIDING_COLOR = new Color(1f, 0.5f, 0.25f);
    public Color INCORRECT_RULEBREAKING_COLOR = new Color(0f, 0f, 0f);
    public Color DEFAULT_TILE_COLOR = new Color(1, 1, 1); 

    // FEEDBACK LENGTH IN SECONDS
    public float CORRECT_FEEDBACK_SECONDS = 0.5f;
    public float PREV_CORRECT_FEEDBACK_SECONDS = 0.5f;
    public float INCORRECT_RULEABIDING_SECONDS = 0.5f;
    public float INCORRECT_RULEBREAKING_SECONDS = 1;
    public float TIMEOUT_SECONDS = 10.0f;
    
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
            case 0:
                // CORRECT AND FINISH
                gameObject.GetComponent<Renderer>().material.color = CORRECT_COLOR;
                yield return new WaitForSeconds(CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = FINISH_COLOR;
                }
                break;
            
            case 1:
                // CORRECT and START
                gameObject.GetComponent<Renderer>().material.color = CORRECT_COLOR;
                yield return new WaitForSeconds(CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = START_COLOR;
                }
                break;
            
            case 2:
                // DEFAULT CORRECT
                gameObject.GetComponent<Renderer>().material.color = CORRECT_COLOR;
                yield return new WaitForSeconds(CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = DEFAULT_TILE_COLOR;
                }
                break;
            
            case 3:
                // RULE-BREAKING BACKTRACK
                gameObject.GetComponent<Renderer>().material.color = INCORRECT_RULEBREAKING_COLOR;
                yield return new WaitForSeconds(INCORRECT_RULEBREAKING_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = DEFAULT_TILE_COLOR;
                }
                else
                {
                    gameObject.GetComponent<Renderer>().material.color = CORRECT_COLOR;
                }
                
                break;
            case 4:
                // RULE-BREAKING INCORRECT DEFAULT or NOT START
                gameObject.GetComponent<Renderer>().material.color = INCORRECT_RULEBREAKING_COLOR;
                yield return new WaitForSeconds(INCORRECT_RULEBREAKING_SECONDS);
                gameObject.GetComponent<Renderer>().material.color = DEFAULT_TILE_COLOR;
                break;
            
            case 99:
                // CORRECT and FINISH
                //  cAudio.Play(0);
              //  MazeGame_TrialLevel.c = true;

                gameObject.GetComponent<Renderer>().material.color = CORRECT_COLOR;
                yield return new WaitForSeconds(CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = FINISH_COLOR;
                }

                break;
            case 30:
                // LAST CORRECT STEP DEFAULT
                gameObject.GetComponent<Renderer>().material.color = LAST_CORRECT_COLOR;
                yield return new WaitForSeconds(PREV_CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = DEFAULT_TILE_COLOR;
                }
                else
                {
                    gameObject.GetComponent<Renderer>().material.color = CORRECT_COLOR;
                }
                break;
            case 31:
                // LAST CORRECT STEP and START
                gameObject.GetComponent<Renderer>().material.color = LAST_CORRECT_COLOR;
                yield return new WaitForSeconds(PREV_CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = START_COLOR;
                }
                else
                {
                    gameObject.GetComponent<Renderer>().material.color = CORRECT_COLOR;
                }
                break;
            case 10:
                // RULE-ABIDING INCORRECT DEFAULT
                gameObject.GetComponent<Renderer>().material.color = INCORRECT_RULEABIDING_COLOR;
                yield return new WaitForSeconds(INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.color = DEFAULT_TILE_COLOR;
                break;
            case 11:
                // RULE-ABIDING INCORRECT and START
                gameObject.GetComponent<Renderer>().material.color = INCORRECT_RULEABIDING_COLOR;
                yield return new WaitForSeconds(INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.color = START_COLOR;
                break;
            case 12:
                // RULE-ABIDING INCORRECT and FINISH
                gameObject.GetComponent<Renderer>().material.color = INCORRECT_RULEABIDING_COLOR;
                yield return new WaitForSeconds(INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.color = FINISH_COLOR;
                break;
            case 21:
                // RULE-BREAKING INCORRECT and START
                gameObject.GetComponent<Renderer>().material.color = INCORRECT_RULEBREAKING_COLOR;
                yield return new WaitForSeconds(INCORRECT_RULEBREAKING_SECONDS);
                gameObject.GetComponent<Renderer>().material.color = START_COLOR;
                break;
            case 22:
                // RULE-BREAKING INCORRECT and FINISH
                gameObject.GetComponent<Renderer>().material.color = INCORRECT_RULEBREAKING_COLOR;
                yield return new WaitForSeconds(INCORRECT_RULEBREAKING_SECONDS); 
                gameObject.GetComponent<Renderer>().material.color = FINISH_COLOR;
                break;
        }

    }
}
