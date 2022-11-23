using UnityEngine;
using System.Collections;
using System.IO;
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
    public Color baseColor;
    private int done = 0;


    void Start()
    {
        GameObject gameConfigsObj = GameObject.FindWithTag("Game Configs");
        gameConfigs = gameConfigsObj.GetComponent<GameConfigs>();
        // gameConfigs = MazeGame_TrialLevel.gameConfigs;
        cBeep = GameObject.Find("CorrectBeep");
        eBeep = GameObject.Find("ErrorBeep");
        cAudio = cBeep.GetComponent<AudioSource>();
        eAudio = eBeep.GetComponent<AudioSource>();
        Material newMat = Resources.Load("TestMat", typeof(Material)) as Material;
        //System.Random rnd = new System.Random();
        //int num = rnd.Next(0, 9);
        gameObject.GetComponent<MeshRenderer>().material = newMat;
        //string textStr = "Textures/Picture" + num.ToString();
        string textStr = "Textures/Picture5";
        Texture newTxt = Resources.Load(textStr, typeof(Texture)) as Texture;
        newMat.SetTexture("_MainTex", newTxt);



        // gameObject.GetComponent<Renderer>().material.color = MazeGame_TrialLevel.tileColor;

        gameObject.GetComponent<Renderer>().material.color = baseColor;
        

        //   gameObject.GetComponent<Renderer>().material("_BaseColor", gameConfigs.CORRECT_COLOR);




        // cBeep.SetActive(false);
        //  eBeep.SetActive(false);
        // gameObject.GetComponent<Renderer>().material.color = gameConfigs.FINISH_COLOR;

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
        // baseColor = MazeGame_TrialLevel.tileColor;
        Debug.Log("Change Color");
        //  c = gameConfigs.CORRECT_COLOR;
        //gameObject.GetComponent<Renderer>().sharedMaterial.color = new Color(5f, 1f, 1f);
        baseColor = c;
    } 

    public IEnumerator ColorFeedback(int code)
    {
        switch (code)
        {
            case 0:
                // CORRECT DEFAULT
                //gameObject.GetComponent<Renderer>().material.color = gameConfigs.CORRECT_COLOR);
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.CORRECT_COLOR;

                //   cAudio.Play(0);
              //  MazeGame_TrialLevel.c = true;
                yield return new WaitForSeconds(gameConfigs.CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = MazeGame_TrialLevel.tileColor;
                }


                break;
            case 1:
                // CORRECT and START
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.CORRECT_COLOR;
                yield return new WaitForSeconds(gameConfigs.CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = gameConfigs.START_COLOR;
                }

                break;
            case 99:
                // CORRECT and FINISH
                //  cAudio.Play(0);
              //  MazeGame_TrialLevel.c = true;

                gameObject.GetComponent<Renderer>().material.color = gameConfigs.CORRECT_COLOR;
                yield return new WaitForSeconds(gameConfigs.CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = gameConfigs.FINISH_COLOR;
                }

                break;
            case 30:
                // LAST CORRECT STEP DEFAULT
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.LAST_CORRECT_COLOR;
                yield return new WaitForSeconds(gameConfigs.PREV_CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = MazeGame_TrialLevel.tileColor;
                }
                break;
            case 31:
                // LAST CORRECT STEP and START
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.LAST_CORRECT_COLOR;
                yield return new WaitForSeconds(gameConfigs.PREV_CORRECT_FEEDBACK_SECONDS);
                if (!MazeGame_TrialLevel.viewPath)
                {
                    gameObject.GetComponent<Renderer>().material.color = gameConfigs.START_COLOR;
                }
                break;
            case 10:
                // RULE-ABIDING INCORRECT DEFAULT
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.INCORRECT_RULEABIDING_COLOR;
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.color = MazeGame_TrialLevel.tileColor;
                break;
            case 11:
                // RULE-ABIDING INCORRECT and START
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.INCORRECT_RULEABIDING_COLOR;
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.START_COLOR;
                break;
            case 12:
                // RULE-ABIDING INCORRECT and FINISH
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.INCORRECT_RULEABIDING_COLOR;
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEABIDING_SECONDS);
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.FINISH_COLOR;
                break;
            case 20:
                // RULE-BREAKING INCORRECT DEFAULT
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.INCORRECT_RULEBREAKING_COLOR;
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEBREAKING_SECONDS);
                gameObject.GetComponent<Renderer>().material.color = MazeGame_TrialLevel.tileColor;
                break;
            case 21:
                // RULE-BREAKING INCORRECT and START
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.INCORRECT_RULEBREAKING_COLOR;
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEBREAKING_SECONDS);
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.START_COLOR;
                break;
            case 22:
                // RULE-BREAKING INCORRECT and FINISH
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.INCORRECT_RULEBREAKING_COLOR;
                yield return new WaitForSeconds(gameConfigs.INCORRECT_RULEBREAKING_SECONDS); 
                gameObject.GetComponent<Renderer>().material.color = gameConfigs.FINISH_COLOR;
                break;
        }

    }
    public static Texture2D LoadPNG(string filePath)
    {

        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }
}
