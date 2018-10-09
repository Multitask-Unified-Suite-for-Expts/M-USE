// to have an experimenter view, you have to 
// (1) set the SUBJECT DISPLAY as the default in Mac's display settings (drag white bar)
// (2) start the game on the subject display


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using USE_Utilities;
using USE_Common_Namespace;

public class ExperimentInfoController : MonoBehaviour
{

    public bool experimenterViewOn;

    private Camera exptViewCam;
    private Text text;
    private Canvas exptViewCanv;
    private GameObject playerViewCanv;
    private ExternalDataManager externalDataManager;

    private int iexpt;
    private int isubj;
    private Display dispSubj;
    private Display dispExpt;
    private GameObject userPanels;
    private GameObject commandWindow;
    private GameObject mainCamObj;
    private Camera mainCamCopy;
    private Camera mainCam;

    private GameObject gazeCircle;
    private GameObject gazeParticle;
    private GameObject circle;

    private Camera camScene;

    private List<string> stringHistory = new List<string>();

    private EyePanelController eyePanelController;

    //panels
    private ViewWindow playerViewWindow;

    private Vector2 exptViewRes;
    private Vector2 playerViewRes;
    private Vector2 mainDisplayRes;

    // Use this for initialization
    void Awake()
    {
        try
        {
            if (experimenterViewOn)
            {
                mainCam = GameObject.Find("MainCamera").GetComponent<Camera>();
                exptViewCam = GameObject.Find("BackgroundCamera").GetComponent<Camera>();
                exptViewCanv = UnityExtension.GetChildByName(gameObject, "ExperimenterCanvas").GetComponent<Canvas>();
                commandWindow = UnityExtension.GetChildByName(exptViewCanv.gameObject, "CommandWindow");
                text = UnityExtension.GetChildByName(commandWindow, "Text").GetComponent<Text>();
                userPanels = UnityExtension.GetChildByName(exptViewCanv.gameObject, "UserDefinedPanels");
                eyePanelController = GameObject.Find("EyePanel").GetComponent<EyePanelController>();
                externalDataManager = GameObject.Find("ScriptManager").GetComponent<ExternalDataManager>();

                if (ConfigReader.sessionSettings.Bool["usingtouchscreen"])
                {
                    circle = (GameObject)Instantiate(Resources.Load("Prefabs/SquareTouch"));
                    circle.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.4f);
                }
                else
                {
                    circle = (GameObject)Instantiate(Resources.Load("Prefabs/EyeCircle"));
                    circle.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);
                }
                circle.SetActive(false);


                text.text = "";
                //camScene = GameObject.Find ("ExperimenterCamera").GetComponent<Camera> ();


                //exptViewCanv.gameObject.SetActive (false);

                // isubj = 0;
                // iexpt = 1;
                // dispSubj = Display.displays[isubj];

                // if (Display.displays.Length > 1)
                // {

                //     //activate displays
                //     //dispSubj.Activate ();

                //     dispExpt = Display.displays[iexpt];
                //     dispExpt.Activate();
                // }



                //Asif changed how displays work so just set to screen width
                exptViewRes = new Vector2(Screen.width, Screen.height);
                /*
                if (Application.isEditor)
                {
                    exptViewRes = new Vector2(Screen.width, Screen.height);
                }
                else
                {
                    //exptViewRes = new Vector2(dispExpt.renderingWidth, dispExpt.renderingHeight);
                }
                */
                mainDisplayRes = new Vector2(Screen.width, Screen.height);


                // set the background and cam
                // exptViewCam.targetDisplay = iexpt;
                // exptViewCanv.targetDisplay = iexpt;
                //				exptViewCanv.renderMode = RenderMode.ScreenSpaceOverlay;
                exptViewCanv.worldCamera = exptViewCam;

                // Initlize new, user defined panels
                // **NB: add thsi to the "userPanels" gameobject to keep things clean

                //initalize new camera for player view
                //ActivatePlayersView ();
                //GameObject g = GameObject.Find("TestCamera");

                //				GameObject g = new GameObject();
                //				//g.transform.parent = userPanels.transform;
                //				g.name = "CameraCopy";
                //				Camera c = g.AddComponent<Camera> ();
                //				c.targetDisplay = iexpt;
                //				c.rect = new Rect(0.6f,0.6f,0.4f,0.4f);
                //				g.transform.parent = GameObject.Find ("Player").transform;
                //				g.transform.position = Camera.main.transform.position;
                //				g.transform.rotation = Camera.main.transform.rotation;

                //				GameObject g2 = new GameObject();
                //				g2.name = "GazeCanvas";
                //				g2.transform.parent = gameObject.transform;
                //				playerViewCanv = g2.AddComponent<Canvas>();
                //				playerViewCanv.renderMode = RenderMode.ScreenSpaceCamera;
                //				playerViewCanv.targetDisplay = iexpt;
                //				playerViewCanv.worldCamera = c;
                //				gazeCircle = (GameObject)Instantiate (Resources.Load ("Prefabs/EyeCircle"));
                //				gazeCircle.transform.SetParent (playerViewCanv.transform, false);

                //find the MainCameraCopy and set some values for it
                mainCamObj = UnityExtension.GetChildByName(gameObject, "MainCameraCopy");
                mainCamCopy = mainCamObj.GetComponent<Camera>();
                // mainCamCopy.targetDisplay = iexpt;
                //c.rect = new Rect(0.6f,0.6f,0.4f,0.4f);
                mainCamObj.transform.parent = GameObject.Find("Player").transform;
                mainCamObj.transform.position = Camera.main.transform.position;
                mainCamObj.transform.rotation = Camera.main.transform.rotation;

                playerViewCanv = GameObject.Find("PlayerViewCanvas");
                gazeCircle = GameObject.Find("GazeCircle");
                gazeParticle = GameObject.Find("EyeParticle");
                gazeCircle.transform.SetParent(exptViewCanv.transform);
                gazeParticle.transform.SetParent(exptViewCanv.transform);
                circle.transform.SetParent(exptViewCanv.transform);
                //toggle the gaze point on and off
                //				if (Application.isEditor){
                //					//necessary because reference resolution of screen overlay doesn't account for window surround of editor
                //					playerViewCanv.GetComponent<RectTransform>().anchoredPosition =  new Vector2(mainCamCopy.rect.position.x * exptViewRes.x, mainCamCopy.rect.position.y * Screen.height);
                //				}else{
                playerViewCanv.GetComponent<RectTransform>().anchoredPosition = ProportionToPixel(mainCamCopy.rect.position, exptViewRes);
                //				}
                playerViewRes = new Vector2(mainCamCopy.rect.size.x * exptViewRes.x, mainCamCopy.rect.size.y * exptViewRes.y);
                playerViewCanv.GetComponent<RectTransform>().sizeDelta = playerViewRes;
                playerViewCanv.GetComponent<Image>().enabled = false;
                //				if (true){
                //					//g2.transform.parent = gameObject.transform;
                ////					playerViewCanv = playerViewCanv.GetComponent<Canvas>();
                ////					playerViewCanv.worldCamera = mainCamCopy;
                ////					playerViewCanv.GetComponent<RectTransform>().anchoredPosition = new Vector2 (mainCamCopy.rect.x, mainCamCopy.rect.y);
                ////					playerViewCanv.GetComponent<RectTransform>().sizeDelta = new Vector2(mainCamCopy.rect.width, mainCamCopy.rect.height);
                //				} else {
                //					playerViewCanv.gameObject.SetActive(false);
                //				}

                //				playerViewCanv = g.GetComponent<Canvas>();
                //				playerViewCanv.targetDisplay = iexpt;
                //				gazeCircle = UnityExtension.GetChildByName(g,"EyeCircle");
                //				gazeCircle.transform.SetParent (g.transform, false);


                //				GameObject myCircle = DrawCalibResult(33, 1, new CalibrationPoint(new Vector2(0.5f,0.5f), new Vector2[]{new Vector2(0.6f,0.4f), new Vector2(0.3f,0.4f)}, new Vector2[]{new Vector2(0.7f,0.6f)}));
                //				GameObject myCircle1 = DrawCalibResult(33, 1, new CalibrationPoint(new Vector2(0f,0f), new Vector2[]{new Vector2(0.05f,0.05f), new Vector2(0.1f,-0.05f)}, new Vector2[]{new Vector2(0f,0f)}));
                //				GameObject myCircle2 = DrawCalibResult(33, 1, new CalibrationPoint(new Vector2(1f,1f), new Vector2[]{new Vector2(0.9f,0.95f), new Vector2(0.94f,1.02f)}, new Vector2[]{new Vector2(1f,1f)}));


                if (ConfigReader.sessionSettings.Bool["2Dview"]) //change arena size and camera angles but leave everything else the same to create a 2D looking effect
                {

                    Vector3 new_camera_position = new Vector3(0f, 100f, 0f);
                    Vector3 new_camera_rotation = new Vector3(90f, 0f, 0f);

                    //Change position, rotation, & Field of view
                    mainCamObj.transform.position = new_camera_position;
                    mainCamObj.transform.eulerAngles = new_camera_rotation;
                    mainCamCopy.fieldOfView = 7.5f;
                }

            }
        }
        catch (Exception e)
        {
            string err = e.Message + "\t" + e.StackTrace;
            //Debug.Log (err);
            throw new System.ArgumentException(err);
        }


    }

    void Update()
    {
        if (experimenterViewOn)
        {
            if (false && Time.realtimeSinceStartup % 2f > 0)
            {
                UpdateCommandText("test");
            }

            //UpdateEyePosition
            float rx = externalDataManager.eyePosition.x;
            float ry = externalDataManager.eyePosition.y;
            float lx = rx;
            float ly = ry;

            Vector2 playerViewGaze = ScreenToPlayerViewPoint(externalDataManager.eyePosition);

            if (ConfigReader.sessionSettings.Int["eyeTrackType"] == 1) {
                if (playerViewGaze[1] < 0 || playerViewGaze[0] < 0) //if gaze and eye particle is off screen turn off particle
                {
                    gazeCircle.SetActive(false);
                    gazeParticle.SetActive(false);
                } else
                {
                    gazeCircle.SetActive(true);
                    gazeParticle.SetActive(true);
                }
            }
            gazeCircle.GetComponent<RectTransform>().anchoredPosition = playerViewGaze + playerViewCanv.GetComponent<RectTransform>().anchoredPosition;
            gazeParticle.GetComponent<RectTransform>().anchoredPosition = playerViewGaze + playerViewCanv.GetComponent<RectTransform>().anchoredPosition;

            //			if (Display.displays.Length > 1) {
            //				gazeCircle.GetComponent<RectTransform> ().anchoredPosition = ScreenRatioMultiplier (playerViewGaze); 
            //			} else {
            //				gazeCircle.GetComponent<RectTransform> ().anchoredPosition = playerViewGaze;
            //			}


            //eyePanelController.UpdateEyePosition (rx, ry, ry != -1, lx, ly, ly != -1);
            eyePanelController.UpdateEyePosition(rx, ry, ry < 0.5f, lx, ly, ly > 0.5f);
        }
    }


    public void UpdateCommandText(string str)
    {
        if (experimenterViewOn)
        {
            //stringHistory.Add (System.DateTime.Now.ToShortTimeString + ": " + str);
            str = "\n > " + System.DateTime.Now.ToLongTimeString() + ": " + str;
            stringHistory.Add(str);

            text.text = text.text + str;

            //deal with overflow
            if (stringHistory.Count > 12)
            {
                text.text = text.text.Substring(stringHistory[0].Length);
                stringHistory.RemoveAt(0);
            }
        }
    }

    private Vector2 ProportionToPixel(Vector2 posProp, Vector2 resolution)
    {
        return new Vector2(posProp.x * resolution.x, posProp.y * resolution.y);
    }

    private Vector2 PixelToProportion(Vector2 posPix, Vector2 resolution)
    {
        return new Vector2(posPix.x / resolution.x, posPix.y / resolution.y);
    }

    private Vector2 ScreenToPlayerViewPoint(Vector2 screenPoint)
    {
        if (!Application.isEditor)
        {
            screenPoint = screenPoint - new Vector2(Screen.width, 0f);
        }
        Vector2 posProp = PixelToProportion(screenPoint, mainDisplayRes);
        return ProportionToPixel(posProp, playerViewRes);
    }

    private Vector2 ScreenRatioMultiplier(Vector2 screenpointA)
    {
        return new Vector2(screenpointA.x * dispSubj.renderingWidth / dispExpt.renderingWidth, screenpointA.y * dispSubj.renderingHeight / dispExpt.renderingHeight);
    }

    public GameObject DrawCalibResult(string name, float monitorWidth, float visualAngle, CalibrationPointResult calibPoint)
    {// Vector2 calibPointProportional, Vector2[] leftSamplesProportional, Vector2[] rightSamplesProportional){
     //find cm and pixel size of radius visualAngle at surface of screen
        float distanceToScreen = 50;
        if (ConfigReader.sessionSettings.Int["eyeTrackType"] == 1)
        {
            distanceToScreen = 50; // dummy value
        }
        else
        {
            //calculate distance using eyetracker info
        }

        float radCm = 2 * distanceToScreen * (Mathf.Tan((Mathf.PI * visualAngle / 180f) / 2));
        float radPix = radCm * playerViewRes.x / monitorWidth;

        Vector2 calibPointPixel = ProportionToPixel(calibPoint.testPoint, playerViewRes);
        calibPointPixel = calibPointPixel + playerViewCanv.GetComponent<RectTransform>().anchoredPosition; //because making parent playerViewCanv doesn't work

        GameObject calibResult = new GameObject(name, typeof(RectTransform));
        //		calibResult.transform.SetParent (playerViewCanv.transform);
        calibResult.transform.SetParent(exptViewCanv.transform);
        calibResult.GetComponent<RectTransform>().sizeDelta = playerViewRes;
        calibResult.GetComponent<RectTransform>().anchorMax = Vector2.zero;
        calibResult.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        calibResult.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;// ProportionToPixel (calibPointProportional, playerViewRes);

        GameObject degreeCircle = new GameObject("DegreeCircle", typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UICircle));
        degreeCircle.transform.SetParent(calibResult.transform);
        degreeCircle.GetComponent<UnityEngine.UI.Extensions.UICircle>().fill = false;
        degreeCircle.GetComponent<UnityEngine.UI.Extensions.UICircle>().thickness = 2f;
        degreeCircle.GetComponent<RectTransform>().sizeDelta = new Vector2(radPix * 2, radPix * 2);
        degreeCircle.GetComponent<RectTransform>().anchoredPosition = calibPointPixel;// new Vector3(calibPointPixel.x, calibPointPixel.y, exptViewCam.nearClipPlane);

        DrawSampleLines("LeftSampleLines", calibResult, Color.green, calibPoint.leftSamples, calibPoint.testPoint);
        DrawSampleLines("RightSampleLines", calibResult, Color.red, calibPoint.rightSamples, calibPoint.testPoint);

        WriteProportionValid(calibResult, Color.green, calibPoint, "Left");
        WriteProportionValid(calibResult, Color.red, calibPoint, "Right");

        return calibResult;
    }

    private GameObject DrawSampleLines(string name, GameObject parent, Color col, List<CalibrationSample> samples, Vector2 calibPoint)
    {
        GameObject sampleLines = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UILineRenderer));
        sampleLines.transform.SetParent(parent.transform);
        sampleLines.GetComponent<RectTransform>().anchorMax = Vector2.zero;
        sampleLines.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        sampleLines.GetComponent<RectTransform>().sizeDelta = playerViewRes;
        sampleLines.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        UnityEngine.UI.Extensions.UILineRenderer lineComp = sampleLines.GetComponent<UnityEngine.UI.Extensions.UILineRenderer>();
        //		lineComp.Points = new Vector2[samples.Count * 2];
        List<Vector2> pointList = new List<Vector2>();
        lineComp.color = col;
        lineComp.relativeSize = false;
        int validSamples = 0;
        for (int i = 0; i < samples.Count * 2; i++)
        {
            CalibrationSample thisSample = samples[(int)Mathf.Floor(i / 2)];
            if (thisSample.validity == true)
            {
                //add the calibration point and the measured sample point to the list of points on the line
                //(for each sample point, we start at the measured sample, next point is the calibration point, then out to the next measured sample, back to the calibration point, etc.
                //this way there are actually two identical lines drawn for most samples, but it allows only a single line object per calibration point)
                pointList.Add(ProportionToPixel(thisSample.sample, playerViewRes) + playerViewCanv.GetComponent<RectTransform>().anchoredPosition + playerViewCanv.GetComponent<RectTransform>().sizeDelta);
                pointList.Add(ProportionToPixel(calibPoint, playerViewRes) + playerViewCanv.GetComponent<RectTransform>().anchoredPosition + playerViewCanv.GetComponent<RectTransform>().sizeDelta);
                validSamples++;
            }
            lineComp.Points = pointList.ToArray();
        }

        //display invalid samples somehow

        return sampleLines;
    }

    private void WriteProportionValid(GameObject parent, Color col, CalibrationPointResult calibPoint, string eye)
    {
        GameObject propValid = new GameObject(eye + "Validity", typeof(RectTransform), typeof(Text));
        propValid.transform.SetParent(parent.transform);
        string validityString = "";
        Vector2 offset = Vector2.zero;
        if (eye.Equals("Left"))
        {
            validityString = calibPoint.leftValid + "/" + calibPoint.leftSamples.Count;
            offset = new Vector2(-20, -20);
        }
        else if (eye.Equals("Right"))
        {
            validityString = calibPoint.rightValid + "/" + calibPoint.rightSamples.Count;
            offset = new Vector2(20, -20);
        }

        RectTransform rt = propValid.GetComponent<RectTransform>();
        Text txt = propValid.GetComponent<Text>();

        Vector2 calibPointPixel = ProportionToPixel(calibPoint.testPoint, playerViewRes) + playerViewCanv.GetComponent<RectTransform>().anchoredPosition;
        rt.anchoredPosition = calibPointPixel + offset +
            new Vector2(playerViewCanv.GetComponent<RectTransform>().sizeDelta.x / 2, playerViewCanv.GetComponent<RectTransform>().sizeDelta.y / 2);
        //still don't know why I have to add half the resolution to this but it works...
        rt.anchorMax = Vector2.zero;
        rt.anchorMin = Vector2.zero;
        txt.text = validityString;
        txt.color = col;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.alignment = TextAnchor.MiddleCenter;
    }


    // ==============================================================================
    //ViewWindow class, for making new panels 
    // ***NOT FINISHED****
    public class ViewWindow
    {
        public string name;
        public Camera camera;
        public Canvas canvas;
        public Text text;
        public GameObject background;
        public int targetDisplay;

        //constructor
        public ViewWindow(int targetDisplay)
        {
            this.name = null;
            this.camera = null;
            this.canvas = null;
            this.text = null;
            this.targetDisplay = targetDisplay;
        }

        //methods
        public void NewCamera()
        {
            //should only have on camera per window
            if (camera.Equals(null))
            {
                GameObject g = new GameObject();
                g.name = "Camera";
                Camera c = g.AddComponent<Camera>();
                c.targetDisplay = targetDisplay;
                //c.audio

                this.camera = c;
            }
        }

        public void NewTextCanvas()
        {

        }

    }

    public void TurnCircleOnOff(bool flag, float cuesize)
    {
        if (flag)
        {
            circle.transform.localScale = new Vector3(mainCamCopy.rect.size.x * cuesize / 10f, mainCamCopy.rect.size.y * cuesize / 10f, 1f);//default size is 10 pixels wide/tall then divide by 2 since scale is half
            circle.transform.position = playerViewCanv.GetComponent<RectTransform>().anchoredPosition + playerViewRes/2;
            circle.SetActive(true);
        }
        else
        {
            circle.SetActive(false);
        }
    }

}

//try adjusting it again???
//		if (Display.displays.Length > 1) {
//			int iexpt = 1;
//			dispSubj = Display.displays [0];
//			dispExpt = Display.displays [iexpt];
//
//			PlayerPrefs.SetInt ("UnitySelectMonitor", iexpt);
//			string str = "got here.\nsize=" + dispExpt.systemWidth + "," + dispExpt.systemHeight + ".\nres=" + Screen.width + "," + Screen.height;
//			if (Screen.width == dispExpt.systemWidth) {
//				text.text = text.text + "\n\ntried changing resolution again...";
//				int w = dispExpt.systemWidth / 4;
//				int h = dispExpt.systemHeight / 4;
//				Screen.SetResolution (w, h, false);
//			}
//		}