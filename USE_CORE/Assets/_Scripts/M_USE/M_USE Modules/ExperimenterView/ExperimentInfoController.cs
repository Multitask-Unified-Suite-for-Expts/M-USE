// to have an experimenter view, you have to 
// (1) set the SUBJECT DISPLAY as the default in Mac's display settings (drag white bar)
// (2) start the game on the subject display


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using USE_Utilities;
// using USE_Common_Namespace;

public class ExperimentInfoController : MonoBehaviour
{

    public bool experimenterViewOn;

    private Camera exptViewCam;
    private Canvas exptViewCanv;
    private GameObject playerViewCanv;
    // private ExternalDataManager externalDataManager;

    private Display dispSubj;
    private Display dispExpt;
    private GameObject mainCamObj;
    private Camera mainCamCopy;
    private Camera mainCam;

    private GameObject gazeCircle;
    private GameObject gazeParticle;

    public GameObject prefabFixwin;
    private GameObject circle;
    private GameObject fixwin;

    private EyePanelController eyePanelController;

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
                mainCam = GameObject.Find("Main Camera").GetComponent<Camera>();
                exptViewCam = GameObject.Find("CameraExperimenterView").GetComponent<Camera>();
                exptViewCanv = UnityExtension.GetChildByName(gameObject, "ExperimenterCanvas").GetComponent<Canvas>();
                eyePanelController = GameObject.Find("EyePanel").GetComponent<EyePanelController>();
                // externalDataManager = GameObject.Find("ScriptManager").GetComponent<ExternalDataManager>();

                fixwin = (GameObject)Instantiate(prefabFixwin);
                fixwin.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
                fixwin.SetActive(false);

                exptViewRes = new Vector2(Screen.width, Screen.height);
                mainDisplayRes = new Vector2(Screen.width, Screen.height);

                exptViewCanv.worldCamera = exptViewCam;

                //find the MainCameraCopy and set some values for it
                mainCamObj = UnityExtension.GetChildByName(gameObject, "MainCameraCopy");
                mainCamCopy = mainCamObj.GetComponent<Camera>();
                mainCamObj.transform.position = Camera.main.transform.position;
                mainCamObj.transform.rotation = Camera.main.transform.rotation;

                playerViewCanv = GameObject.Find("PlayerViewCanvas");
                gazeCircle = GameObject.Find("GazeCircle");
                gazeParticle = GameObject.Find("EyeParticle");
                gazeCircle.transform.SetParent(exptViewCanv.transform);
                gazeParticle.transform.SetParent(exptViewCanv.transform);

                fixwin.transform.SetParent(exptViewCanv.transform);

                playerViewCanv.GetComponent<RectTransform>().anchoredPosition = ProportionToPixel(mainCamCopy.rect.position, exptViewRes);
                playerViewRes = new Vector2(mainCamCopy.rect.size.x * exptViewRes.x, mainCamCopy.rect.size.y * exptViewRes.y);
                playerViewCanv.GetComponent<RectTransform>().sizeDelta = playerViewRes;
                playerViewCanv.GetComponent<Image>().enabled = false;
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
            //UpdateEyePosition
            // float rx = externalDataManager.eyePosition.x;
            // float ry = externalDataManager.eyePosition.y;
            float rx = Input.mousePosition.x;
            float ry = Input.mousePosition.y;
            float lx = rx;
            float ly = ry;

            // Vector2 playerViewGaze = ScreenToPlayerViewPoint(externalDataManager.eyePosition);
            Vector2 playerViewGaze = ScreenToPlayerViewPoint(Input.mousePosition);

            // if (ConfigReader.sessionSettings.Int["eyeTrackType"] == 1) {
                if (playerViewGaze[1] < 0 || playerViewGaze[0] < 0) //if gaze and eye particle is off screen turn off particle
                {
                    gazeCircle.SetActive(false);
                    gazeParticle.SetActive(false);
                } else
                {
                    gazeCircle.SetActive(true);
                    gazeParticle.SetActive(true);
                }
            // }
            gazeCircle.GetComponent<RectTransform>().anchoredPosition = playerViewGaze + playerViewCanv.GetComponent<RectTransform>().anchoredPosition;
            gazeParticle.GetComponent<RectTransform>().anchoredPosition = playerViewGaze + playerViewCanv.GetComponent<RectTransform>().anchoredPosition;

            //eyePanelController.UpdateEyePosition (rx, ry, ry != -1, lx, ly, ly != -1);
            eyePanelController.UpdateEyePosition(rx, ry, ry < 0.5f, lx, ly, ly > 0.5f);
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

    public void TurnFixWinOnOff(bool flag, float fixwinsize, Vector2 loc)
    {
        if (flag)
        {
            fixwin.transform.localScale = new Vector3(fixwinsize / 100f, fixwinsize / 100f, 1f);//default size is 100 pixels wide/tall then divide by 2 since scale is half
            if (!Application.isEditor)
            {
                fixwin.transform.position = playerViewCanv.GetComponent<RectTransform>().anchoredPosition + ScreenToPlayerViewPoint(loc) + new Vector2(playerViewRes[0],0f);
            }
            else
            {
                fixwin.transform.position = playerViewCanv.GetComponent<RectTransform>().anchoredPosition + ScreenToPlayerViewPoint(loc);
            }
            fixwin.SetActive(true);
        }
        else
        {
            fixwin.SetActive(false);
        }
    }

}