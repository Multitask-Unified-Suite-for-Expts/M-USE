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
    public GameObject HeldTooLong_Prefab;
    public GameObject HeldTooShort_Prefab;
    public GameObject MovedTooFar_Prefab;
    public float FeedbackDuration;
    public bool FeedbackOn;
    //Textures are currently set in The Trial Template "LoadTextures" method:
    public Texture2D HeldTooLong_Texture;
    public Texture2D HeldTooShort_Texture;
    public Texture2D MovedTooFar_Texture;
    [HideInInspector] public EventCodeManager EventCodeManager;
    [HideInInspector] public Dictionary<string, EventCode> SessionEventCodes;


    public void Init(DataController frameData)
    {
        frameData.AddDatum("FeedbackOn", () => FeedbackOn.ToString());
        EventCodeManager = new EventCodeManager();
        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);
        InstantiatedGO = null;
        HeldTooLong_Prefab = null; //making 1 prefab null so we can save a "PrefabsCreated" boolean
    }

    public void EnableTouchFeedback(SelectionTracker.SelectionHandler handler, float fbDuration, GameObject taskCanvasGO)
    {
        if(HeldTooLong_Prefab == null)
            CreatePrefabs();
        
        Handler = handler;
        FeedbackDuration = fbDuration;
        TaskCanvasGO = taskCanvasGO;
        Handler.TouchErrorFeedback += OnTouchErrorFeedback; //Subscribe to event
    }

    private void OnTouchErrorFeedback(object sender, TouchFeedbackArgs e) //e passes us errorType and the gameobject
    {
        switch (e.errorTypeString)
        {
            case "DurationTooLong":
                Debug.Log("Touch Duration too long.....");
                ShowTouchFeedback(new TouchFeedback(e.gameObject, e.errorTypeString, HeldTooLong_Prefab, this));
                break;
            case "DurationTooShort":
                Debug.Log("Touch Duration too short.....");
                ShowTouchFeedback(new TouchFeedback(e.gameObject, e.errorTypeString, HeldTooShort_Prefab, this));
                break;
            case "MovedTooFar":
                Debug.Log("Touch Moved too far.....");
                ShowTouchFeedback(new TouchFeedback(e.gameObject, e.errorTypeString, MovedTooFar_Prefab, this));
                break;
            default:
                break;
        }
    }

    private void ShowTouchFeedback(TouchFeedback touchFb)
    {
        FeedbackOn = true;
        audioFBController.Play("Negative");

        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);

        InstantiatedGO = Instantiate(touchFb.Prefab, TaskCanvasGO.transform);
        InstantiatedGO.name = "TouchFeedback_GO";
        InstantiatedGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(.5f, .5f);
        //InstantiatedGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(touchFb.SelectedGO_ScreenPos.x, touchFb.SelectedGO_ScreenPos.y); //Doesn't work. 

        //InstantiatedGO.SetActive(true);
        ////EventCodeManager.SendCodeImmediate(SessionEventCodes["TouchFBController_FeedbackOn"]);

        Invoke("DestroyTouchFeedback", FeedbackDuration);
    }

    public void DestroyTouchFeedback()
    {
        if (InstantiatedGO != null)
        {
            Destroy(InstantiatedGO);
            FeedbackOn = false;
        }
    }


    public void CreatePrefabs()
    {
        HeldTooLong_Prefab = CreatePrefab("HeldTooLongGO", HeldTooLong_Texture, 200f);
        HeldTooShort_Prefab = CreatePrefab("HeldTooShortGO", HeldTooShort_Texture, 200f);
        MovedTooFar_Prefab = CreatePrefab("MovedTooFarGO", MovedTooFar_Texture, 200f);
    }

    public GameObject CreatePrefab(string name, Texture2D texture, float size)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
        image.rectTransform.sizeDelta = new Vector2(size, size);
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


    public class TouchFeedback
    {
        public GameObject Prefab;
        public GameObject SelectedGO;
        public string FeedbackType; //"HeldTooLong" , "HeldTooShort", "MovedTooFar";
        public Vector3 SelectedGO_ScreenPos;

        public TouchFeedback(GameObject selectedGO, string fbType, GameObject prefab, TouchFBController touchFbController)
        {
            SelectedGO = selectedGO;
            FeedbackType = fbType;
            SelectedGO_ScreenPos = Camera.main.WorldToScreenPoint(selectedGO.transform.position);
            Prefab = prefab;
          
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









//public GameObject CreateTouchFbPrefab(string fbType)
//{
//    GameObject go = new GameObject(fbType);
//    go.AddComponent<RectTransform>();
//    Image image = go.AddComponent<Image>();
//    image.rectTransform.sizeDelta = new Vector2(150f, 150f);

//    switch (fbType)
//    {
//        case "DurationTooLong":
//            image.sprite = Sprite.Create(HeldTooLong_Texture, new Rect(0, 0, HeldTooLong_Texture.width, HeldTooLong_Texture.height), new Vector2(.5f, .5f));
//            break;
//        case "DurationTooShort":
//            image.sprite = Sprite.Create(HeldTooShort_Texture, new Rect(0, 0, HeldTooShort_Texture.width, HeldTooShort_Texture.height), new Vector2(.5f, .5f));
//            break;
//        case "MovedTooFar":
//            image.sprite = Sprite.Create(MovedTooFar_Texture, new Rect(0, 0, MovedTooFar_Texture.width, MovedTooFar_Texture.height), new Vector2(.5f, .5f));
//            break;
//        default:
//            break;
//    }
//    return go;
//}