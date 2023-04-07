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
    public Canvas TaskCanvas;
    //public GameObject TouchFeedback_CanvasGO;
    //public Canvas TouchFeedback_Canvas;
    public AudioFBController audioFBController;
    public GameObject HeldTooLong_Prefab;
    public GameObject HeldTooShort_Prefab;
    public GameObject MovedTooFar_Prefab;
    public int FeedbackSize;
    public float FeedbackDuration = .3f;
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
        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);
        InstantiatedGO = null;
        HeldTooLong_Prefab = null; //making 1 prefab null so we can save a "PrefabsCreated" boolean
    }

    public void EnableTouchFeedback(SelectionTracker.SelectionHandler handler, float fbDuration, int fbSize, GameObject taskCanvasGO)
    {        
        Handler = handler;
        FeedbackDuration = fbDuration;
        FeedbackSize = fbSize;
        TaskCanvasGO = taskCanvasGO;
        TaskCanvas = TaskCanvasGO.GetComponent<Canvas>();

        if (HeldTooLong_Prefab == null)
            CreatePrefabs();
       
        Handler.TouchErrorFeedback += OnTouchErrorFeedback; //Subscribe to event
    }

    private void OnTouchErrorFeedback(object sender, TouchFeedbackArgs e) //e passes us errorType and the gameobject
    {
        if(!FeedbackOn)
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
    }

    private void ShowTouchFeedback(TouchFeedback touchFb)
    {
        FeedbackOn = true;
        audioFBController.Play("Negative");

        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);

        InstantiatedGO = Instantiate(touchFb.Prefab, TaskCanvasGO.transform);
        InstantiatedGO.name = "TouchFeedback_GO";
        InstantiatedGO.GetComponent<RectTransform>().anchoredPosition = touchFb.PosOnCanvas;

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
        HeldTooLong_Prefab = CreatePrefab("HeldTooLongGO", HeldTooLong_Texture);
        HeldTooShort_Prefab = CreatePrefab("HeldTooShortGO", HeldTooShort_Texture);
        MovedTooFar_Prefab = CreatePrefab("MovedTooFarGO", MovedTooFar_Texture);
    }

    public GameObject CreatePrefab(string name, Texture2D texture)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
        image.rectTransform.sizeDelta = new Vector2(FeedbackSize, FeedbackSize);
        return go;
    }

    //public void CreateFbCanvas()
    //{
    //    TouchFeedback_CanvasGO = new GameObject("TouchFeedback_CanvasGO");
    //    TouchFeedback_Canvas = TouchFeedback_CanvasGO.AddComponent<Canvas>();
    //    TouchFeedback_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    //    TouchFeedback_CanvasGO.AddComponent<CanvasScaler>();
    //    TouchFeedback_CanvasGO.AddComponent<GraphicRaycaster>();
    //    TouchFeedback_CanvasGO.GetComponent<RectTransform>().position = new Vector3(0, 0, 0);
    //    TouchFeedback_Canvas.sortingOrder = 3000;
    //}


    public class TouchFeedback
    {
        public GameObject Prefab;
        public GameObject SelectedGO;
        public string FeedbackType; //"HeldTooLong" , "HeldTooShort", "MovedTooFar";
        public Vector2 PosOnCanvas;
        public TouchFBController TouchFeedbackController;

        public TouchFeedback(GameObject selectedGO, string fbType, GameObject prefab, TouchFBController touchFbController)
        {            
            SelectedGO = selectedGO;
            FeedbackType = fbType;
            Prefab = prefab;
            TouchFeedbackController = touchFbController;
            PosOnCanvas = GetPosOnCanvas();
        }

        public Vector2 GetPosOnCanvas()
        {
            RectTransform canvasRect = TouchFeedbackController.TaskCanvas.GetComponent<RectTransform>();
            Vector3 screenPos = Camera.main.WorldToScreenPoint(SelectedGO.transform.position);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, Camera.main, out localPoint);
            return localPoint;
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
