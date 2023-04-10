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
    public List<GameObject> PrefabList;
    public float FeedbackSize = 150f; //Default is 150;
    public float FeedbackDuration = .3f; //Default is .3
    public bool FeedbackOn;
    //Textures are currently set in The Trial Template "LoadTextures" method:
    public Texture2D HeldTooLong_Texture;
    public Texture2D HeldTooShort_Texture;
    public Texture2D MovedTooFar_Texture;
    [HideInInspector] public EventCodeManager EventCodeManager;
    [HideInInspector] public Dictionary<string, EventCode> SessionEventCodes;

    private int Num_HeldTooLong = 0;
    private int Num_HeldTooShort = 0;
    private int Num_MovedTooFar = 0;

    public int ErrorCount
    {
        get
        {
            return Num_HeldTooLong + Num_HeldTooShort + Num_MovedTooFar;
        }
    }


    public void Init(DataController trialData, DataController frameData)
    {
        trialData.AddDatum("Num_HeldTooLong", () => Num_HeldTooLong);
        trialData.AddDatum("Num_HeldTooShort", () => Num_HeldTooShort);
        trialData.AddDatum("Num_MovedTooFar", () => Num_MovedTooFar);

        frameData.AddDatum("FeedbackOn", () => FeedbackOn.ToString());
        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);
        InstantiatedGO = null;
        HeldTooLong_Prefab = null; //making 1 prefab null so we can save a "PrefabsCreated" boolean
    }

    public void EnableTouchFeedback(SelectionTracker.SelectionHandler handler, float fbDuration, float fbSize, GameObject taskCanvasGO)
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
    public void EnableTouchFeedback(SelectionTracker.SelectionHandler handler, GameObject taskCanvasGO)
    {
        Handler = handler;
        TaskCanvasGO = taskCanvasGO;
        TaskCanvas = TaskCanvasGO.GetComponent<Canvas>();

        if (HeldTooLong_Prefab == null)
            CreatePrefabs();

        Handler.TouchErrorFeedback += OnTouchErrorFeedback; //Subscribe to event
    }

    private void OnTouchErrorFeedback(object sender, TouchFeedbackArgs e)
    {
        if (e.Selection.ParentName == "ExperimenterDisplay")
            return;

        if(!FeedbackOn)
        {
            switch (e.Selection.ErrorType)
            {
                case "DurationTooLong":
                    Num_HeldTooLong++;
                    ShowTouchFeedback(new TouchFeedback(e.Selection, HeldTooLong_Prefab, this));
                    break;
                case "DurationTooShort":
                    Num_HeldTooShort++;
                    ShowTouchFeedback(new TouchFeedback(e.Selection, HeldTooShort_Prefab, this));
                    break;
                case "MovedTooFar":
                    Num_MovedTooFar++;
                    ShowTouchFeedback(new TouchFeedback(e.Selection, MovedTooFar_Prefab, this));
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
        if (InstantiatedGO != null) Destroy(InstantiatedGO);
        InstantiatedGO = Instantiate(touchFb.Prefab, TaskCanvasGO.transform);
        InstantiatedGO.name = "TouchFeedback_GO";
        InstantiatedGO.GetComponent<RectTransform>().anchoredPosition = touchFb.PosOnCanvas;
        EventCodeManager.SendCodeImmediate(SessionEventCodes["TouchFBController_FeedbackOn"]);

        Invoke("DestroyTouchFeedback", FeedbackDuration);
    }

    public void DestroyTouchFeedback()
    {
        if (InstantiatedGO != null)
        {
            Destroy(InstantiatedGO);
            EventCodeManager.SendCodeImmediate(SessionEventCodes["TouchFBController_FeedbackOn"]);
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
        PrefabList.Add(go); //add to prefab list. 
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

    public void IncrementErrorCount(string error)
    {
        switch (error)
        {
            case "DurationTooLong":
                Num_HeldTooLong++;
                break;
            case "DurationTooShort":
                Num_HeldTooShort++;
                break;
            case "MovedTooFar":
                Num_MovedTooFar++;
                break;
            default:
                break;
        }
    }

    public int GetErrorCount()
    {
        return Num_HeldTooLong + Num_HeldTooShort + Num_MovedTooFar;
    }

    public void ClearErrorCounts()
    {
        Num_HeldTooLong = 0;
        Num_HeldTooShort = 0;
        Num_MovedTooFar = 0;
    }

    public void SetPrefabSizes(float size)
    {
        if (PrefabList.Count > 0)
        {
            foreach (GameObject prefab in PrefabList)
                prefab.GetComponent<Image>().rectTransform.sizeDelta = new Vector2(size, size);
        }
        else
            Debug.Log("Trying to change the prefab sizes, but the prefablist only has " + PrefabList.Count + " items!");
    }


    public class TouchFeedback
    {
        public SelectionTracker.USE_Selection Selection;
        public GameObject Prefab;
        public Vector2 PosOnCanvas;
        public TouchFBController TouchFeedbackController;

        public TouchFeedback(SelectionTracker.USE_Selection selection, GameObject prefab, TouchFBController touchFbController)
        {
            Selection = selection;
            Prefab = prefab;
            TouchFeedbackController = touchFbController;
            PosOnCanvas = GetPosOnCanvas();
        }

        public Vector2 GetPosOnCanvas()
        {
            RectTransform canvasRect = TouchFeedbackController.TaskCanvas.GetComponent<RectTransform>();
            Vector3 screenPos = Camera.main.WorldToScreenPoint(Selection.SelectedGameObject.transform.position);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, Camera.main, out localPoint);
            return localPoint;
        }
    }


    public class TouchFeedbackArgs : EventArgs
    {
        public SelectionTracker.USE_Selection Selection { get; }

        public TouchFeedbackArgs(SelectionTracker.USE_Selection selection)
        {
            Selection = selection;
        }
    }
}
