using UnityEngine;
using System.Collections;
using HiddenMaze;
using CognitiveMaze_Namespace;


public class Tile : MonoBehaviour
{

    // Tiles are distiguished by their (x, y) coordinate using standard C1 coordinate system, zero-indexed
    // This means the bottom-left-most tile is (0, 0).
    public Coords mCoord;

    // gameConfigs is where adjustable constants about the game can be set
    public GameConfigs gameConfigs;

    public GameObject cBeep;
    public GameObject eBeep;
    public AudioSource cAudio;
    public AudioSource eAudio;

    void Start()
    {
        GameObject gameConfigsObj = GameObject.FindWithTag("Game Configs");
        gameConfigs = gameConfigsObj.GetComponent<GameConfigs>();
        cBeep = GameObject.Find("CorrectBeep");
        eBeep = GameObject.Find("ErrorBeep");
        cAudio = cBeep.GetComponent<AudioSource>();
        eAudio = eBeep.GetComponent<AudioSource>();



        // cBeep.SetActive(false);
        //  eBeep.SetActive(false);
    }

    void OnMouseDown()
    {
        int correctnessCode;
        correctnessCode = MazeGame_TrialLevel.ManageTileTouch(this);
 
            MazeGame_TrialLevel.setEnd(correctnessCode);
        
        StartCoroutine(ColorFeedback(correctnessCode));
    }

   // public void Reset()
    //{
    //    this.gameObject.SetActive(false);
    //}

    public IEnumerator ColorFeedback(int code)
    {
        switch (code)
        {
            case 0:
                // CORRECT DEFAULT
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.CORRECT_COLOR);

                 cAudio.Play(0);
                yield return new WaitForSeconds(gameConfigs.CORRECT_FEEDBACK_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.DEFAULT_TILE_COLOR);


                break;
            case 1:
                // CORRECT and START
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.CORRECT_COLOR);
                cAudio.Play(0);

                yield return new WaitForSeconds(gameConfigs.CORRECT_FEEDBACK_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.START_COLOR);


                break;
            case 99:
                // CORRECT and FINISH
                cAudio.Play(0);

                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.CORRECT_COLOR);
                yield return new WaitForSeconds(gameConfigs.CORRECT_FEEDBACK_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.FINISH_COLOR);


                break;
            case 30:
                // LAST CORRECT STEP DEFAULT
                cAudio.Play(0);

                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.LAST_CORRECT_COLOR);
                yield return new WaitForSeconds(gameConfigs.PREV_CORRECT_FEEDBACK_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.DEFAULT_TILE_COLOR);
                break;
            case 31:
                // LAST CORRECT STEP and START
                cAudio.Play(0);

                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.LAST_CORRECT_COLOR);
                yield return new WaitForSeconds(gameConfigs.PREV_CORRECT_FEEDBACK_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.START_COLOR);


                break;
            case 10:
                // RULE-ABIDING INCORRECT DEFAULT
                eAudio.Play(0);

                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEABIDING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.DEFAULT_TILE_COLOR);


                break;
            case 11:
                // RULE-ABIDING INCORRECT and START
                eAudio.Play(0);

                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEABIDING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.START_COLOR);
                break;
            case 12:
                // RULE-ABIDING INCORRECT and FINISH
                eAudio.Play(0);

                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEABIDING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.FINISH_COLOR);

                break;
            case 20:
                // RULE-BREAKING INCORRECT DEFAULT
                eAudio.Play(0);

                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEBREAKING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEBREAKING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.DEFAULT_TILE_COLOR);
                break;
            case 21:
                // RULE-BREAKING INCORRECT and START
                eAudio.Play(0);

                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEBREAKING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEBREAKING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.START_COLOR);

                break;
            case 22:
                // RULE-BREAKING INCORRECT and FINISH
                eAudio.Play(0);

                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEBREAKING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEBREAKING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.FINISH_COLOR);

                break;
        }

    }
}



/*
public class Tile : MonoBehaviour
{

    // Tiles are distiguished by their (x, y) coordinate using standard C1 coordinate system, zero-indexed
    // This means the bottom-left-most tile is (0, 0).
    public Coords mCoord;

    // gameConfigs is where adjustable constants about the game can be set
    public GameConfigs gameConfigs;


    void Start()
    {
        GameObject gameConfigsObj = GameObject.FindWithTag("Game Configs");
        gameConfigs = gameConfigsObj.GetComponent<GameConfigs>();
    }

    void OnMouseDown() 
    {
        int correctnessCode;
        correctnessCode = gameObject.transform.parent.gameObject.GetComponent<MazeVisible>().ManageTileTouch(this);

        StartCoroutine(ColorFeedback(correctnessCode));
    }

    public IEnumerator ColorFeedback(int code)
    {
        switch (code)
        {
            case 0:
                // CORRECT DEFAULT
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.CORRECT_COLOR);
                yield return new WaitForSeconds(gameConfigs.CORRECT_FEEDBACK_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.DEFAULT_TILE_COLOR);

                break;
            case 1:
                // CORRECT and START
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.CORRECT_COLOR);
                yield return new WaitForSeconds(gameConfigs.CORRECT_FEEDBACK_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.START_COLOR);

                break;
            case 99:
                // CORRECT and FINISH
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.CORRECT_COLOR);
                yield return new WaitForSeconds(gameConfigs.CORRECT_FEEDBACK_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.FINISH_COLOR);

                break;
            case 30:
                // LAST CORRECT STEP DEFAULT
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.LAST_CORRECT_COLOR);
                yield return new WaitForSeconds(gameConfigs.PREV_CORRECT_FEEDBACK_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.DEFAULT_TILE_COLOR);

                break;
            case 31:
                // LAST CORRECT STEP and START
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.LAST_CORRECT_COLOR);
                yield return new WaitForSeconds(gameConfigs.PREV_CORRECT_FEEDBACK_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.START_COLOR);

                break;
            case 10:
                // RULE-ABIDING INCORRECT DEFAULT
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEABIDING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.DEFAULT_TILE_COLOR);

                break;
            case 11:
                // RULE-ABIDING INCORRECT and START
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEABIDING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.START_COLOR);
                break;
            case 12:
                // RULE-ABIDING INCORRECT and FINISH
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEABIDING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.FINISH_COLOR);

                break;
            case 20:
                // RULE-BREAKING INCORRECT DEFAULT
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEBREAKING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEBREAKING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.DEFAULT_TILE_COLOR);

                break;
            case 21:
                // RULE-BREAKING INCORRECT and START
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEBREAKING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEBREAKING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.START_COLOR);

                break;
            case 22:
                // RULE-BREAKING INCORRECT and FINISH
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.INCORRECT_RULEBREAKING_COLOR);
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEBREAKING_SECONDS);
                gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", gameConfigs.FINISH_COLOR);

                break;
        }

    }
}

    */
