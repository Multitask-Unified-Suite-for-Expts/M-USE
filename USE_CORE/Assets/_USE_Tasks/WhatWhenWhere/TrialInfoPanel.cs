using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using USE_Utilities;

public class TrialInfoPanel : MonoBehaviour
{
    public TrialInfoList tiList;
    public GameObject trialInfoText;

    //Variables for the drawCircle
    //Cameras and Associated Canvases
    private Camera exptViewCam;
    private GameObject mainCamObj;
    private Camera mainCamCopy;
    private Camera mainCam;
    private Canvas exptViewCanv;
    private GameObject playerViewCanv;
    //Screen and Panel/Canvase Resolutions
    public Vector2 exptViewRes;
    private Vector2 playerViewRes;
    private Vector2 playerViewResAnchor;
    private Vector2 mainDisplayRes;
    private float distanceToScreen;
    private Vector3 circleLocation;
    private float visualAngle;

    private GameObject circle; 


    // Start is called before the first frame update
    void Start()
    {
        //----------------------------------------------------------DRAW CIRCLE-------------------------------------------
        /*exptViewRes = new Vector2(Screen.width, Screen.height);
        mainDisplayRes = new Vector2(Screen.width, Screen.height);
        Canvas exptViewCanv = UnityExtension.GetChildByName(gameObject, "ExperimenterCanvas").GetComponent<Canvas>();
        exptViewCanv.worldCamera = exptViewCam;

        mainCamCopy = mainCamObj.GetComponent<Camera>();
        mainCamObj = UnityExtension.GetChildByName(gameObject, "MainCameraCopy");
        playerViewRes = new Vector2(mainCamCopy.rect.size.x * exptViewRes.x, mainCamCopy.rect.size.y * exptViewRes.y);
        
        */
        visualAngle = 2;
        circleLocation = new Vector3(220f, -247.5f, 0f);
        distanceToScreen = 50; // dummy value
        drawCircle(circleLocation, distanceToScreen, visualAngle);
        
        
        tiList = new TrialInfoList();
        tiList.Initialize();
        trialInfoText = transform.Find("TrialInfoPanelText").gameObject;
        trialInfoText.GetComponent<Text>().supportRichText = true;
        trialInfoText.GetComponent<Text>().text = "<size=35><b><color=#2962486>Trial Info</color></b></size>" + "\n<size=20>" + tiList.GenerateTrialInfo() + "</size>";
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public class TrialInfo
    {
        public string dataDescription;
        //public string dataValue;
        public string GenerateTextDescription()
        {
            return dataDescription; // add "+ dataValue" eventually
        }
        
    }
    public class TrialInfoList
    {
        List<TrialInfo> TrialInfos = new List<TrialInfo>();
        public string GenerateTrialInfo()
        {
            string completeString = "";
            foreach (TrialInfo ti in TrialInfos)
            {
                completeString = completeString + ti.GenerateTextDescription() + "\n";
            }

            Debug.Log("TrialInfo: " + completeString);

            return completeString;
        }
        public void Initialize(Func<List<TrialInfo>> CustomTrialInfoList = null)
        {
            if (CustomTrialInfoList == null)
                TrialInfos = DefaultTrialInfoList(); //this is your default function
            else
                TrialInfos = CustomTrialInfoList(); //allows users to specify task-specific lists - this will end up looking something like the various task-specific classes like WWW_TaskDef or whatever

            //GenerateTextForPanel(); //method that loops through each hotkey and creates the string to show the hotkey options, using the GenerateTextDescription function of each on
        }
        public List<TrialInfo> DefaultTrialInfoList()
        {
            List<TrialInfo> TrialInfoList = new List<TrialInfo>();
            TrialInfo trialNumber = new TrialInfo
            {
                dataDescription = "Trial: "
            };
            TrialInfoList.Add(trialNumber);

            TrialInfo trialPerformance = new TrialInfo
            {
                dataDescription = "Trial Performance: "
            };
            TrialInfoList.Add(trialPerformance);

            return TrialInfoList;
        }
    }
    public GameObject drawCircle(Vector3 circleLocation, float distanceToScreen, float visualAngle)
    {
        float radCm = 2 * distanceToScreen * (Mathf.Tan((Mathf.PI * visualAngle / 180f) / 2));
        float radPix = radCm * playerViewRes.x / 1920; // dummy value 1920 used, ((MonitorDetails)SessionSettings.Get("sessionConfig", "monitorDetails")).CmSize[0]

        GameObject degreeCircle = new GameObject("DegreeCircle", typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UICircle));
        degreeCircle.AddComponent<CanvasRenderer>();
        degreeCircle.transform.SetParent(transform);
        degreeCircle.GetComponent<UnityEngine.UI.Extensions.UICircle>().fill = false;
        degreeCircle.GetComponent<UnityEngine.UI.Extensions.UICircle>().thickness = 2f;
        degreeCircle.GetComponent<RectTransform>().sizeDelta = new Vector2(radPix * 2, radPix * 2);
        degreeCircle.GetComponent<RectTransform>().anchoredPosition = circleLocation;// new Vector3(calibPointPixel.x, calibPointPixel.y, exptViewCam.nearClipPlane);
        return degreeCircle;
        
    }
}
