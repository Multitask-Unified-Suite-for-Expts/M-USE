using UnityEngine;
using System.Collections.Generic;
using HiddenMaze;
using UnityEngine.UI;


public class Tile : MonoBehaviour
{
    [HideInInspector] public MazeManager MazeManager;
    private TileSettings TileSettings;

    private List<GameObject> AdjacentTiles = new List<GameObject>();

    private Coords mCoord;

    private float sliderValueChange;

    private Vector3? position = null;
    // Reference to the ScriptableObject holding the settings

    // Access settings through this instance
    private Color startColor => TileSettings.GetTileColor("start");
    private Color finishColor => TileSettings.GetTileColor("finish");
    private Color correctColor => TileSettings.GetTileColor("correct");
    private Color prevCorrectColor => TileSettings.GetTileColor("prevCorrect");
    private Color incorrectRuleAbidingColor => TileSettings.GetTileColor("incorrectRuleAbiding");
    private Color incorrectRuleBreakingColor => TileSettings.GetTileColor("incorrectRuleBreaking");
    private Color defaultTileColor => TileSettings.GetTileColor("default");
    private int numBlinks => TileSettings.GetNumBlinks();

    // Access feedback length settings
    private float correctFeedbackDuration => TileSettings.GetFeedbackDuration("correct");
    private float prevCorrectFeedbackDuration => TileSettings.GetFeedbackDuration("prevCorrect");
    private float incorrectRuleAbidingFeedbackDuration => TileSettings.GetFeedbackDuration("incorrectRuleAbiding");
    private float incorrectRuleBreakingFeedbackDuration => TileSettings.GetFeedbackDuration("incorrectRuleBreaking");
    private float tileBlinkingDuration => TileSettings.GetFeedbackDuration("blinking");
    private float timeoutSeconds => TileSettings.GetFeedbackDuration("timeout");

    private Color FBColor;
    private float FBDuration;
    private float flashStartTime;
    private float FBStartTime;
    private int CorrectnessCode;
    private int iFlashes;
    

    [HideInInspector] public Color initialTileColor;
    [HideInInspector] public Color baseColor;
    [HideInInspector] public bool isFlashing = false;
    [HideInInspector] public bool assignedTileFlash;
    [HideInInspector] public bool choiceFeedback;

    [HideInInspector] public GameObject flashingTileGO;

    public void Initialize(TileSettings tileSettings, MazeManager mazeManager)
    {
        TileSettings = tileSettings;
        MazeManager = mazeManager;
    }
    public void SelectionFeedback()
    {
        if (!isFlashing)
        {
            CorrectnessCode = MazeManager.IsFreePlay() ? MazeManager.ManageFreePlayTileTouch(this) : MazeManager.ManageHiddenPathTileTouch(this);
           
            ColorFeedback(CorrectnessCode);
        }
    }
    public void SetColor(Color c)
    {
        GetComponent<Image>().color = c;
    } 
    public void SetTileFbDuration(float duration)
    {
        FBDuration = duration;
    }
    public float GetTileFbDuration()
    {
        return FBDuration;
    }
    public float GetSliderValueChange()
    {
        return sliderValueChange;
    }
    public void SetSliderValueChange(float val)
    {
        sliderValueChange = val;
    }
    public List<GameObject> GetAdjacentTiles()
    {
        return AdjacentTiles;
    } 
    public void SetAdjacentTiles(List<GameObject> tiles)
    {
        AdjacentTiles  = tiles;
    }
    public string GetChessCoord()
    {
        return mCoord.chessCoord;
    }
    public Coords GetCoord()
    {
        return mCoord;
    }
    public void SetCoord(Coords coord)
    {
        mCoord = coord;
    }
    public Vector3? GetTilePosition()
    {
        return position;
    }
    public void ColorFeedback(int code)
    {
        switch (code)
        {
            case 1:
                // CORRECT
                FBColor =  correctColor;
                FBDuration = correctFeedbackDuration;
                break;
            case 2:
                // PREVIOUSLY CORRECT
                FBColor =  prevCorrectColor;
                FBDuration = prevCorrectFeedbackDuration;
                break;
            case 10:
                // RULE-ABIDING INCORRECT
                FBColor =  incorrectRuleAbidingColor;
                FBDuration = incorrectRuleAbidingFeedbackDuration;
                break;
            case 20:
                // RULE-BREAKING INCORRECT
                FBColor = incorrectRuleBreakingColor;
                FBDuration = incorrectRuleBreakingFeedbackDuration;
                break;
        }
        gameObject.GetComponent<Image>().color = FBColor;
        FBStartTime = Time.unscaledTime;
        choiceFeedback = true;
    }
    
    public void FlashTile()
     {
        iFlashes = 0;

        Tile flashingTile = this;
        flashingTileGO = this.gameObject;
        isFlashing = true;
         flashStartTime = Time.unscaledTime;
        // if (flashingTile.gameObject == MazeManager.startTileGO)
        //     initialTileColor = startColor;
        // else if (flashingTile.gameObject == MazeManager.finishTileGO)
        //     initialTileColor = finishColor;
        // else
        //     initialTileColor = defaultTileColor;// before it starts flashing set color
     }
    public void TerminateTileFlashing()
    {
        flashingTileGO.GetComponent<Image>().color = initialTileColor; // confirm it stops on original tile color
        isFlashing = false;
    }
    void Update()
    {
        if(transform.hasChanged)
           position = transform.position;
        
        
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
                TerminateTileFlashing();
            }
        }

        if (choiceFeedback && !isFlashing)
        {

            float elapsed = Time.unscaledTime - FBStartTime;
            float interval = FBDuration;
            if (elapsed >=  interval)
            {
                if (!MazeManager.IsPathVisible() || (CorrectnessCode == 10 || (CorrectnessCode == 20 && !MazeManager.IsBacktrack())))
                    gameObject.GetComponent<Image>().color = initialTileColor;
                else if (MazeManager.IsPathVisible() && (CorrectnessCode == 2 || MazeManager.IsBacktrack()))
                    gameObject.GetComponent<Image>().color= correctColor;
                
                choiceFeedback = false;
            }
        }

    }

}