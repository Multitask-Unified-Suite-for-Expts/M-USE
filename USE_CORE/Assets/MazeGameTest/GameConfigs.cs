using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConfigs : MonoBehaviour
{
    
    public float SCREEN_WIDTH;
    public float TILE_WIDTH;

    public Color START_COLOR;
    public Color FINISH_COLOR;
    public Color CORRECT_COLOR;
    public Color LAST_CORRECT_COLOR;
    public Color INCORRECT_RULEABIDING_COLOR;
    public Color INCORRECT_RULEBREAKING_COLOR;
    public Color DEFAULT_TILE_COLOR;

    public float CORRECT_FEEDBACK_SECONDS;
    public float PREV_CORRECT_FEEDBACK_SECONDS;
    public float INCORRECT_RULEABIDING_SECONDS;
    public float INCORRECT_RULEBREAKING_SECONDS;
    
    public float TIMEOUT_SECONDS;

    void Start()
    {
        // MAZE GAME WIDTHS

        // TODO: Not implemented, but this should be the maximum screen width that tiles can take up without overfilling the screen
        SCREEN_WIDTH = 4;

        // Default tile width
        TILE_WIDTH = 0.5f;

        //---------------------------------------------------------

        // TILE COLORS

        // Start - Light yellow
        START_COLOR = new Color(0.94f, 0.93f, 0.48f);

        // Finish - Light blue
        FINISH_COLOR = new Color(0.37f, 0.59f, 0.94f);

        // Correct - Light green
        CORRECT_COLOR = new Color(0.62f, 1f, 0.5f);

        // Prev correct - Darker green
        LAST_CORRECT_COLOR = new Color(0.2f, 0.7f, 0.5f);

        // Incorrect rule-abiding - Orange
        INCORRECT_RULEABIDING_COLOR = new Color(1f, 0.5f, 0.25f);

        // Incorrect rule-breaking - Black
        INCORRECT_RULEBREAKING_COLOR = new Color(0f, 0f, 0f);

        // Default - Off-white
        // DEFAULT_TILE_COLOR = new Color(0.95f, 0.95f, 0.95f);
        DEFAULT_TILE_COLOR = new Color(1, 1, 1, 1);


        //---------------------------------------------------------

        // FEEDBACK LENGTH IN SECONDS

        // Correct - 0.5 seconds
        CORRECT_FEEDBACK_SECONDS = 0.5f;

        // Prev correct - 0.5 seconds
        PREV_CORRECT_FEEDBACK_SECONDS = 0.5f;

        // Incorrect rule-abiding - 0.5 seconds
        INCORRECT_RULEABIDING_SECONDS = 0.5f;

        // Incorrect rule-breaking - 1.0 seconds
        INCORRECT_RULEBREAKING_SECONDS = 1.0f;

        //---------------------------------------------------------

        // TIMEOUT

        TIMEOUT_SECONDS = 10.0f;

    }
}
