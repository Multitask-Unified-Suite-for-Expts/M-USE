using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Classes;
using USE_Data;
using SelectionTracking;
using System;
using UnityEngine.UI;
using USE_ExperimentTemplate_Data;
using System.Linq;

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

    private Dictionary<string, int> Error_Dict;

    public int ErrorCount
    {
        get
        {
            return Error_Dict.Values.Sum();
        }
    }


    public void Init(DataController trialData, DataController frameData)
    {
        CreateErrorDict();
        AddErrorsToTrialData(trialData);

        frameData.AddDatum("FeedbackOn", () => FeedbackOn.ToString());
        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);
        InstantiatedGO = null;
        HeldTooLong_Prefab = null; //making 1 prefab null so we can save a "PrefabsCreated" boolean
    }

    private void CreateErrorDict()
    {
        Error_Dict = new Dictionary<string, int>();
        Error_Dict.Add("HeldTooLong", 0);
        Error_Dict.Add("HeldTooShort", 0);
        Error_Dict.Add("MovedTooFar", 0);
    }

    private void AddErrorsToTrialData(DataController trialData)
    {
        trialData.AddDatum("HeldTooLong", () => Error_Dict["HeldTooLong"]);
        trialData.AddDatum("HeldTooShort", () => Error_Dict["HeldTooShort"]);
        trialData.AddDatum("MovedTooFar", () => Error_Dict["MovedTooFar"]);
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

    public void DisableTouchFeedback()
    {
        Handler.TouchErrorFeedback -= OnTouchErrorFeedback;
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
                    Error_Dict["HeldTooLong"]++;
                    ShowTouchFeedback(new TouchFeedback(e.Selection, HeldTooLong_Prefab, this));
                    break;
                case "DurationTooShort":
                    Error_Dict["HeldTooShort"]++;
                    ShowTouchFeedback(new TouchFeedback(e.Selection, HeldTooShort_Prefab, this));
                    break;
                case "MovedTooFar":
                    Error_Dict["MovedTooFar"]++;
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
        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);
        InstantiatedGO = Instantiate(touchFb.Prefab, TaskCanvasGO.transform);
        InstantiatedGO.name = "TouchFeedback_GO";
        InstantiatedGO.GetComponent<RectTransform>().anchoredPosition = touchFb.PosOnCanvas;
        EventCodeManager.SendCodeImmediate(SessionEventCodes["TouchFBController_FeedbackOn"]);

        Invoke("DestroyTouchFeedback", FeedbackDuration);
    }

    public void DestroyTouchFeedback() //Called in the Invoke("DestroyTouchFeedback") above ^^
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
        image.color = new Color32(224, 78, 92, 235);
        image.rectTransform.sizeDelta = new Vector2(FeedbackSize, FeedbackSize);
        PrefabList.Add(go); 
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

    public void ClearErrorCounts()
    {
        Error_Dict = Error_Dict.ToDictionary(kvp => kvp.Key, kvp => 0);
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
