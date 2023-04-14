using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using GazeCalibration_Namespace;
using EyeTrackerData_Namespace;
using System;
using USE_UI;
using USE_Common_Namespace;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using System.Globalization;

public class GazeCalibration_TrialLevel : ControlLevel_Trial_Template
{
    public GazeCalibration_TrialDef CurrentTrialDef => GetCurrentTrialDef<GazeCalibration_TrialDef>();
    public GameObject GC_CanvasGO;
    public USE_Circle USE_Circle;

    // Task Def Variables
    [HideInInspector] public String ContextExternalFilePath;
    [HideInInspector] public bool SpoofGazeWithMouse;
   
    [HideInInspector] public Vector3 SmallCirclePosition;
    [HideInInspector] public float SmallCircleSize;
    [HideInInspector] public Vector3 BigCirclePosition;
    [HideInInspector] public float BigCircleSize;
    
    [HideInInspector] public int RedSpriteSize;
    [HideInInspector] public int BlackSpriteSize;
    [HideInInspector] public int BlueSpriteSize;





    //for calibration point definition
    [HideInInspector]
    public Vector2[] ninePoints, sixPoints;
    [HideInInspector]
    public int numCalibPoints;
    [HideInInspector]
    public float[] calibPointsInset = new float[2] { .1f, .15f };
    [HideInInspector]
    public Vector2[] calibPointsADCS;

    //Other Calibration Point Variables....?
    private float acceptableCalibrationDistance;
    [HideInInspector]
    public bool currentCalibrationPointFinished, calibrationUnfinished, calibrationFinished;
    private int recalibratePoint = 0;
    //private bool calibAssessment;
    private Vector3 currentCalibTargetScreen;
    private Vector2 currentCalibTargetADCS;
    private Vector3 moveVector;
    private Vector3 calibCircleStartPos;
    private int calibCount;
    private EyeTrackerData_Namespace.CalibrationResult calibResult;

    //Calibration Timing Variables
    private float epochStartTime;
    private float proportionOfMoveTime;
    private float proportionOfShrinkTime;
    private float calibCircleMoveTime = .75f;
    private float assessTime = 4.0f; //0.5f;
    private float calibCircleShrinkTime = 0.6f; //0.5f;//0.3f;
    private float calibTime = 0.3f;
    private float rewardTime = 0.5f;
    private float blinkOnDuration = 0.2f;
    private float blinkOffDuration = 0.1f;

    //Calibration Sizing Variables
    private Vector3 bigCircleMaxScale = new Vector3(0.6f, 0.6f, 0.6f);
    private float bigCircleShrinkTargetSize = .1f;
    private float smallCircleSize = 0.065f;

    // Game Objects
    private USE_Circle calibSmallCircle;
    private USE_Circle calibBigCircle;
    //private GameObject calibCanvas;
    private Sprite redCircle;
    private Sprite blackCircle;
    private Sprite blueCircle;

    //public float widthCm;

    public override void DefineControlLevel()
    {
        //Define Calibration Points
        ninePoints = new Vector2[9]
        {new Vector2(calibPointsInset[0], calibPointsInset[1]),
            new Vector2(0.5f, calibPointsInset[1]),
            new Vector2(1f - calibPointsInset[0], calibPointsInset[1]),
            new Vector2(calibPointsInset[0], 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1f - calibPointsInset[0], 0.5f),
            new Vector2(calibPointsInset[0], 1f - calibPointsInset[1]),
            new Vector2(0.5f, 1f - calibPointsInset[1]),
            new Vector2(1f - calibPointsInset[0], 1f - calibPointsInset[1])};
        sixPoints = new Vector2[6]
        {new Vector2(calibPointsInset[0], calibPointsInset[1]),
            new Vector2(0.5f, calibPointsInset[1]),
            new Vector2(1f - calibPointsInset[0], calibPointsInset[1]),
            new Vector2(calibPointsInset[0], 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1f - calibPointsInset[0], 0.5f)};

        

        State Init = new State("Init");
        State Blink = new State("Blink");
        State Shrink = new State("Shrink");
        State Check = new State("Check");
        State Calibrate = new State("Calibrate");
        State Confirm = new State("Confirm");

        AddActiveStates(new List<State> { Init, Blink, Shrink, Check, Calibrate, Confirm });

        Add_ControlLevel_InitializationMethod(() =>
        {
            calibrationUnfinished = true;
           // mainLevel.udpManager.SendString("ET###enter_calibration");
            calibCount = 0;
           // calibCanvas.SetActive(true);
            calibrationFinished = false;
            calibResult = new EyeTrackerData_Namespace.CalibrationResult();

            // Assign Game Objects
            calibSmallCircle = new USE_Circle(GC_CanvasGO.GetComponent<Canvas>(), SmallCirclePosition, SmallCircleSize, "CalibrationSmallCircle");
            calibBigCircle = new USE_Circle(GC_CanvasGO.GetComponent<Canvas>(), BigCirclePosition, BigCircleSize, "CalibrationBigCircle");
            redCircle = USE_Circle.CreateCircleSprite(Color.red, RedSpriteSize);
            blackCircle = USE_Circle.CreateCircleSprite(Color.red, BlackSpriteSize);
            blueCircle = USE_Circle.CreateCircleSprite(Color.red, BlueSpriteSize);
            calibBigCircle.SetSprite(blackCircle);
        });

        var SelectionHandler = SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Init, Calibrate);
        
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Space), Blink, () => {
            numCalibPoints = 9;
            DefineCalibPoints(numCalibPoints);
        });
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Alpha9), Blink, () => {
            numCalibPoints = 9;
            DefineCalibPoints(numCalibPoints);
        });
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Alpha6), Blink, () => {
            numCalibPoints = 6;
            DefineCalibPoints(numCalibPoints);
        });
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Alpha5), Blink, () => {
            numCalibPoints = 5;
            DefineCalibPoints(numCalibPoints);
        });
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Alpha3), Blink, () => {
            numCalibPoints = 3;
            DefineCalibPoints(numCalibPoints);
        });
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Alpha1), Blink, () => {
            numCalibPoints = 1;
            DefineCalibPoints(numCalibPoints);
        });

        //EPOCH 0 = blink calibration circle
        float blinkStartTime = 0;
        bool keyboardOverride = false;
        Blink.AddInitializationMethod(() =>
        {
            //calibAssessment = false;
            calibBigCircle.transform.localScale = bigCircleMaxScale;
            //              if(currentCalibrationPointFinished){
            currentCalibTargetADCS = calibPointsADCS[calibCount];
            //              }
            currentCalibrationPointFinished = false;
            currentCalibTargetScreen = ScreenTransformations.AdcsToScreenPoint(currentCalibTargetADCS);
            calibBigCircle.transform.position = currentCalibTargetScreen;
            calibBigCircle.gameObject.SetActive(true);
            calibSmallCircle.gameObject.SetActive(false);
            keyboardOverride = false;
        });
        Blink.AddUpdateMethod(() =>
        {
            blinkStartTime = CheckBlink(blinkStartTime, calibBigCircle.gameObject);
            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);
        });
        Blink.SpecifyTermination(() =>
            keyboardOverride || Vector3.Distance(SelectionHandler.CurrentInputLocation(), currentCalibTargetScreen) < acceptableCalibrationDistance,
            Shrink, () => calibBigCircle.gameObject.SetActive(true));

        //EPOCH 1 - Shrink calibration circle
        Shrink.AddInitializationMethod(
            () =>
            {
                calibSmallCircle.transform.localScale = new Vector3(smallCircleSize, smallCircleSize, smallCircleSize);
                calibSmallCircle.transform.position = currentCalibTargetScreen;
                calibSmallCircle.SetSprite(redCircle);
                calibSmallCircle.gameObject.SetActive(true);
                proportionOfShrinkTime = 0;
            });
        Shrink.AddUpdateMethod(() => ShrinkCalibCircle(Shrink.TimingInfo.StartTimeAbsolute));
        Shrink.SpecifyTermination(() => proportionOfShrinkTime == 1, Check);
        Shrink.SpecifyTermination(() => !keyboardOverride & Vector3.Distance(SelectionHandler.CurrentInputLocation(), currentCalibTargetScreen) > acceptableCalibrationDistance, Blink);
        //

        //EPOCH 3 - Check readiness to calibrate
        Check.AddInitializationMethod(() => keyboardOverride = false);
        Check.AddUpdateMethod(() => keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space));
        Check.SpecifyTermination(() => keyboardOverride ||
            Vector3.Distance(SelectionHandler.CurrentInputLocation(), currentCalibTargetScreen) < acceptableCalibrationDistance, Calibrate);



        //EPOCH 4 - Calibrate!
        Calibrate.AddInitializationMethod(
            () =>
            {
               // mainLevel.udpManager.SendString("ET###collect_calibration_at_point###float " + currentCalibTargetADCS.x.ToString() + "###float " + currentCalibTargetADCS.y.ToString());
            });
        Calibrate.AddUpdateMethod(() =>
        {
            TobiiReadCalibrationMsg();
            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);
        });
        Calibrate.SpecifyTermination(() => currentCalibrationPointFinished | keyboardOverride, Confirm, () => {
            if (calibCount == calibPointsADCS.Length - 1)// & !calibAssessment)
            {
                //calibAssessment = true;
             //   mainLevel.udpManager.SendString("ET###compute_and_apply_calibration");
            }
        });

        bool recalibpoint = false;
        bool resultAdded = false;
        bool resultsDisplayed = false;
        bool pointFinished = false;
        Confirm.AddInitializationMethod(() =>
        {
            recalibpoint = false;
            resultAdded = false;
            resultsDisplayed = false;
            pointFinished = false;
            calibSmallCircle.SetSprite(blueCircle);
            if (SyncBoxController != null)
            {
               // SyncBoxController.AddToSend("RWD " + rewardTime * 10000);
                resultsDisplayed = DisplayCalibrationResults(); // just added
            }
        });
        Confirm.AddUpdateMethod(() =>
        {/*
            if (mainLevel.externalDataManager.calibMsgResult.Length > 0 && !resultAdded)
            {
                resultAdded = RecordCalibrationResult(mainLevel.externalDataManager.calibMsgResult);
            }*/
            if (calibCount == calibPointsADCS.Length - 1 && !resultsDisplayed) //put this here instead of init in case calib message takes more than a frame to send
            {
                ClearCalibVisuals();
                resultsDisplayed = DisplayCalibrationResults();
            }
            if (InputBroker.anyKey)
            {
                //string commandString = Input.inputString;
                if (InputBroker.GetKeyDown(KeyCode.Space) && calibCount == calibPointsADCS.Length - 1)
                {
                    //calibSuccess = true;
                    calibrationFinished = true;
                    //ClearCalibVisuals();
                }
                else if (InputBroker.GetKeyDown(KeyCode.Equals))
                {
                    pointFinished = true;
                }
                else if (InputBroker.GetKeyDown(KeyCode.Minus))
                {
                    DiscardCalibrationPoint(calibCount);
                    recalibpoint = true;
                    ////calibSuccess = true;
                    //calibCount = 0; //set to -1 because the termination includes calibCount++
                    //ClearCalibResults();
                    //for (int i = 0; i < numCalibPoints; i++)
                    //{
                    //	DiscardCalibrationPoint(i);
                    //}
                    //DefineCalibPoints(numCalibPoints);
                }
              /*  REIMPLEMENT ************* I DIDN'T KNOW HOW TO GET GENERIC INPUT KEY 
               *  
               *  else if (int.TryParse(InputBroker.GetKey(), out recalibratePoint))
                {
                    if (recalibratePoint > 0 & recalibratePoint < 10)
                    {
                        DiscardCalibrationPoint(recalibratePoint - 1);
                        calibCount = -1; //set to -1 because the termination includes calibCount++
                        ClearCalibResults();
                        DefineCalibPoints(1);
                        //calibSuccess = true;
                    }
                }*/
            }
            if (Time.time - Confirm.TimingInfo.StartTimeAbsolute > assessTime)
            {
                pointFinished = true;
                calibBigCircle.gameObject.SetActive(false);
                calibSmallCircle.gameObject.SetActive(false);
            }
        });
        Confirm.SpecifyTermination(() => calibCount < calibPointsADCS.Length - 1 && pointFinished, Blink, () => {//!calibrationFinished, blink, ()=> {
            calibCount++;
            calibBigCircle.gameObject.SetActive(false);
            calibSmallCircle.gameObject.SetActive(false);
        });
        Confirm.SpecifyTermination(() => recalibpoint, Blink, () => {
            calibBigCircle.gameObject.SetActive(false);
            calibSmallCircle.gameObject.SetActive(false);
        });
        Confirm.SpecifyTermination(() => calibrationFinished, FinishTrial, () =>
        {
            calibrationUnfinished = false;
          //  mainLevel.udpManager.SendString("ET###leave_calibration");
            ClearCalibResults();
            GameObject.Find("CalibrationCanvas").SetActive(false);
            /*if (mainLevel.usingSyncbox)
            {
                mainLevel.eventCodeManager.SendCodeImmediate(103);
            }
            if (mainLevel.storeData)
            {
                mainLevel.udpManager.SendString("ET###save_calibration_textfile");
                mainLevel.udpManager.SendString("ET###save_calibration_binfile");
            }
            mainLevel.WriteFrameByFrameData();
            mainLevel.TimesRunCalibration++;*/
        });

    }

    private void DiscardCalibrationPoint(int point)
    {
        // Reimplement without UDP Manager

        //mainLevel.udpManager.SendString("ET###discard_calibration_at_point\tfloat " + ninePoints[point].x.ToString() + "\tfloat " + ninePoints[point].y.ToString());
        //mainLevel.udpManager.SendString("ET###discard_calibration_at_point###float " + currentCalibTargetADCS.x.ToString() + "###float " + currentCalibTargetADCS.y.ToString());
    }

    void DefineCalibPoints(int nPoints)
    {
        switch (nPoints)
        {
            case 9:
                calibPointsADCS = ninePoints;
                acceptableCalibrationDistance = Vector2.Distance(ScreenTransformations.AdcsToScreenPoint(ninePoints[0]), ScreenTransformations.AdcsToScreenPoint(ninePoints[1])) / 2;
                break;
            case 6:
                calibPointsADCS = sixPoints;
                acceptableCalibrationDistance = Vector2.Distance(ScreenTransformations.AdcsToScreenPoint(sixPoints[0]), ScreenTransformations.AdcsToScreenPoint(sixPoints[1])) / 2;
                break;
            case 5:
                calibPointsADCS = new Vector2[5] {
                ninePoints [0],
                ninePoints [2],
                ninePoints [4],
                ninePoints [6],
                ninePoints [8]};
                acceptableCalibrationDistance = Vector2.Distance(ScreenTransformations.AdcsToScreenPoint(ninePoints[0]), ScreenTransformations.AdcsToScreenPoint(ninePoints[4])) / 2;
                break;
            case 3:
                calibPointsADCS = new Vector2[3]{
                ninePoints [3],
                ninePoints [4],
                ninePoints [5] };
                acceptableCalibrationDistance = Vector2.Distance(ScreenTransformations.AdcsToScreenPoint(ninePoints[0]), ScreenTransformations.AdcsToScreenPoint(ninePoints[1])) / 2;
                break;
            case 1:
                Vector2[] originalPoints = new Vector2[numCalibPoints];
                switch (numCalibPoints)
                {
                    case 9:
                        originalPoints = ninePoints;
                        break;
                    case 5:
                        originalPoints = new Vector2[5] {
                    ninePoints [0],
                    ninePoints [2],
                    ninePoints [4],
                    ninePoints [6],
                    ninePoints [8]
                };
                        break;
                    case 3:
                        originalPoints = new Vector2[3] {
                    ninePoints [3],
                    ninePoints [4],
                    ninePoints [5]
                };
                        break;
                }
                calibPointsADCS = new Vector2[1] { originalPoints[recalibratePoint - 1] };
                break;
        }
    }
    private void ClearCalibVisuals()
    {
        for (int i = 0; i < calibResult.results.Count; i++)
        {
            Destroy(calibResult.results[i].resultDisplay);
        }
    }
    private void ClearCalibResults()
    {
        ClearCalibVisuals();
        calibResult = new EyeTrackerData_Namespace.CalibrationResult();
    }
    void ShrinkCalibCircle(float startTime)
    {
        proportionOfShrinkTime = (Time.time - startTime) / calibCircleShrinkTime;
        if (proportionOfShrinkTime > 1)
        {
            proportionOfShrinkTime = 1;
            calibBigCircle.transform.localScale = new Vector3(bigCircleShrinkTargetSize, bigCircleShrinkTargetSize, bigCircleShrinkTargetSize);
        }
        else
        {
            float newScale = bigCircleMaxScale[0] * (1 - ((1 - bigCircleShrinkTargetSize) * proportionOfShrinkTime));
            calibBigCircle.transform.localScale = new Vector3(newScale, newScale, newScale);
        }
    }
    private float CheckBlink(float blinkStartTime, GameObject circle)
    {
        if (circle.activeInHierarchy && Time.time - blinkStartTime > blinkOnDuration)
        {
            circle.SetActive(false);
            blinkStartTime = Time.time;
        }
        else if (!circle.activeInHierarchy && Time.time - blinkStartTime > blinkOffDuration)
        {
            circle.SetActive(true);
            blinkStartTime = Time.time;
        }
        return blinkStartTime;
    }
public bool DisplayCalibrationResults()
{
    for (int i = 0; i < calibResult.results.Count; i++)
    {
        for (int j = 0; j < calibResult.results[i].leftSamples.Count; j++)
        {
            Vector2 sampleL = calibResult.results[i].leftSamples[j].sample;
        }
        for (int j = 0; j < calibResult.results[i].rightSamples.Count; j++)
        {
            Vector2 sampleL = calibResult.results[i].rightSamples[j].sample;
        }
      //  calibResult.results[i].resultDisplay = exptInfo.DrawCalibResult("CalibResult " + (i + 1), 1, calibResult.results[i]);
    }
    if (calibResult.results.Count == calibPointsADCS.Length)
        return true;
    else
        return false;
}

    public void TobiiReadCalibrationMsg()
    {
        // NO LONGER NEEDED IF SDK DIRECTLY COMMUNICATES

        /*if (mainLevel.externalDataManager.calibMsgPt.Length > 0)
        {
            if (String.Equals(mainLevel.externalDataManager.calibMsgPt[2], "calibration_status_success"))
            {
                currentCalibrationPointFinished = true;
            }
            else if (String.Equals(mainLevel.externalDataManager.calibMsgPt[2], "calibration_status_failure"))
            {
                currentCalibrationPointFinished = false;
            }
            else //unkown message type
            {
                currentCalibrationPointFinished = false;
            }
        }*/


    }
}
