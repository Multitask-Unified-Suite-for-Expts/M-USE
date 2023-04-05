using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Classes;
using USE_Data;
using SelectionTracking;
using System;
using UnityEngine.UI;

public class TouchFBController : MonoBehaviour
{
    SelectionTracker.SelectionHandler Handler;

    public GameObject InstantiatedGO;

    public GameObject TaskCanvasGO;

    //public GameObject TouchFeedback_CanvasGO;
    //public Canvas TouchFeedback_Canvas;

    public AudioFBController audioFBController;

    //List<TouchFeedback> TouchFeedback_List;

    //public GameObject HeldTooLong_Prefab;
    //public GameObject HeldTooShort_Prefab;
    //public GameObject MovedTooFar_Prefab;

    public float FeedbackDuration;

    [HideInInspector] public EventCodeManager EventCodeManager;
    [HideInInspector] public Dictionary<string, EventCode> SessionEventCodes;

    //Textures are set in The Trial Template "LoadTextures" method:
    public Texture2D HeldTooLong_Texture;
    public Texture2D HeldTooShort_Texture;
    public Texture2D MovedTooFar_Texture;

    public bool FeedbackOn;


    public void Init(DataController frameData)
    {
        frameData.AddDatum("FeedbackOn", () => FeedbackOn.ToString());
        EventCodeManager = new EventCodeManager();
        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);
        InstantiatedGO = null;

        //CreatePrefabs();
        //CreateTouchFeedbackCanvas();
    }

    public void EnableTouchFeedback(SelectionTracker.SelectionHandler handler, float fbDuration, GameObject taskCanvasGO)
    {
        Handler = handler;
        FeedbackDuration = fbDuration;
        TaskCanvasGO = taskCanvasGO;
        Handler.TouchErrorFeedback += OnTouchErrorFeedback;
    }

    private void OnTouchErrorFeedback(object sender, TouchFeedbackArgs e) //e passes us errorType and the gameobject
    {
        switch (e.errorTypeString)
        {
            case "DurationTooLong":
                Debug.Log("Duration too long.....");
                StartCoroutine(ShowTouchFeedback(new TouchFeedback(e.gameObject, e.errorTypeString, this))); //may not even need errorTypestring
                break;
            case "DurationTooShort":
                Debug.Log("Duration too short.....");
                StartCoroutine(ShowTouchFeedback(new TouchFeedback(e.gameObject, e.errorTypeString, this)));
                break;
            case "MovedTooFar":
                Debug.Log("Moved too far.....");
                StartCoroutine(ShowTouchFeedback(new TouchFeedback(e.gameObject, e.errorTypeString, this)));
                break;
            default:
                Debug.Log("Default case for error type string.");
                break;
        }
    }

    private IEnumerator ShowTouchFeedback(TouchFeedback touchFb)
    {
        FeedbackOn = true;

        audioFBController.Play("Negative");

        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);

        InstantiatedGO = Instantiate(touchFb.Prefab, TaskCanvasGO.transform);
        //InstantiatedGO = Instantiate(touchFb.Prefab, TouchFeedback_CanvasGO.transform);
        InstantiatedGO.name = "TouchFeedback_GO";
        InstantiatedGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(.5f, .5f);
        //InstantiatedGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(touchFb.SelectedGO_ScreenPos.x, touchFb.SelectedGO_ScreenPos.y); //Doesn't work. 

        InstantiatedGO.SetActive(true);
        ////EventCodeManager.SendCodeImmediate(SessionEventCodes["TouchFBController_FeedbackOn"]);

        yield return new WaitForSeconds(FeedbackDuration);

        Destroy(InstantiatedGO);

        FeedbackOn = false;
    }

    public GameObject CreateTouchFbPrefab(string fbType)
    {
        GameObject go = new GameObject(fbType);
        go.AddComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.rectTransform.sizeDelta = new Vector2(150f, 150f);

        switch (fbType)
        {
            case "DurationTooLong":
                image.sprite = Sprite.Create(HeldTooLong_Texture, new Rect(0, 0, HeldTooLong_Texture.width, HeldTooLong_Texture.height), new Vector2(.5f, .5f));
                break;
            case "DurationTooShort":
                image.sprite = Sprite.Create(HeldTooShort_Texture, new Rect(0, 0, HeldTooShort_Texture.width, HeldTooShort_Texture.height), new Vector2(.5f, .5f));
                break;
            case "MovedTooFar":
                image.sprite = Sprite.Create(MovedTooFar_Texture, new Rect(0, 0, MovedTooFar_Texture.width, MovedTooFar_Texture.height), new Vector2(.5f, .5f));
                break;
            default:
                break;
        }
        return go;
    }

    //public void CreateTouchFeedbackCanvas()
    //{
    //    TouchFeedback_CanvasGO = new GameObject("TouchFeedback_CanvasGO");
    //    TouchFeedback_Canvas = TouchFeedback_CanvasGO.AddComponent<Canvas>();
    //    TouchFeedback_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    //    TouchFeedback_CanvasGO.AddComponent<CanvasScaler>();
    //    TouchFeedback_CanvasGO.AddComponent<GraphicRaycaster>();
    //    TouchFeedback_Canvas.sortingOrder = 32767;
    //}

    //public void CreatePrefabs()
    //{
    //    //Create HeldTooLongPrefab:
    //    GameObject htl_GO = new GameObject("HeldTooLong");
    //    htl_GO.AddComponent<RectTransform>();
    //    Image htl_Image = htl_GO.AddComponent<Image>();
    //    htl_Image.sprite = Sprite.Create(HeldTooLong_Texture, new Rect(0, 0, HeldTooLong_Texture.width, HeldTooLong_Texture.height), new Vector2(.5f, .5f));
    //    htl_Image.rectTransform.sizeDelta = new Vector2(200f, 200f);
    //    //HeldTooLong_Prefab = htl_GO;

    //    //Create HeldTooShortPrefab:
    //    GameObject hts_GO = new GameObject("HeldTooShort");
    //    hts_GO.AddComponent<RectTransform>();
    //    Image hts_Image = hts_GO.AddComponent<Image>();
    //    hts_Image.sprite = Sprite.Create(HeldTooShort_Texture, new Rect(0, 0, HeldTooShort_Texture.width, HeldTooShort_Texture.height), new Vector2(.5f, .5f));
    //    hts_Image.rectTransform.sizeDelta = new Vector2(200f, 200f);
    //    //HeldTooShort_Prefab = hts_GO;

    //    //Create MovedTooFarPrefab:
    //    GameObject mtf_GO = new GameObject("MovedTooFar");
    //    mtf_GO.AddComponent<RectTransform>();
    //    Image mtf_Image = mtf_GO.AddComponent<Image>();
    //    mtf_Image.sprite = Sprite.Create(MovedTooFar_Texture, new Rect(0, 0, MovedTooFar_Texture.width, MovedTooFar_Texture.height), new Vector2(.5f, .5f));
    //    mtf_Image.rectTransform.sizeDelta = new Vector2(200f, 200f);
    //    //MovedTooFar_Prefab = mtf_GO;
    //}




    public class TouchFeedback
    {
        public GameObject Prefab;
        public GameObject SelectedGO;
        public string FeedbackType; //"HeldTooLong" , "HeldTooShort", "MovedTooFar";
        public Vector3 SelectedGO_ScreenPos;

        public TouchFeedback(GameObject selectedGO, string fbType, TouchFBController touchFbController)
        {
            SelectedGO = selectedGO;
            FeedbackType = fbType;
            SelectedGO_ScreenPos = Camera.main.WorldToScreenPoint(selectedGO.transform.position);
            Prefab = touchFbController.CreateTouchFbPrefab(fbType);
          
        }
    }



    public class TouchFeedbackArgs : EventArgs
    {
        public GameObject gameObject { get; }
        public string errorTypeString { get; }

        public TouchFeedbackArgs(GameObject go, string errorType)
        {
            gameObject = go;
            errorTypeString = errorType;
        }
    }
}
