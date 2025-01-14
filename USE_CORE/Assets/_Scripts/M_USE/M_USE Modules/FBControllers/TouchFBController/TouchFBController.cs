/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/




using System.Collections.Generic;
using UnityEngine;
using USE_Data;
using SelectionTracking;
using System;
using System.Linq;


public class TouchFBController : MonoBehaviour
{
    SelectionTracker.SelectionHandler Handler;
    public GameObject InstantiatedGO;
    public GameObject TaskCanvasGO;
    public Canvas TaskCanvas;
    public AudioFBController audioFBController;
    private static GameObject HeldTooLong_Prefab;
    private static GameObject HeldTooShort_Prefab;
    private static GameObject MovedTooFar_Prefab;
    private static GameObject NotSelectablePeriod_Prefab;
    private static List<GameObject> PrefabList;
    public float FeedbackSize = 150f; //Default is 150;
    public float FeedbackDuration = .3f; //Default is .3
    public bool FeedbackOn;
    public bool TouchFbEnabled;
    private bool UseRootGoPos;

    private List<GameObject> ObjectsToIgnore;

    //Textures are currently set in The Trial Template "LoadTextures" method:
    public static Texture2D HeldTooLong_Texture;
    public static Texture2D HeldTooShort_Texture;
    public static Texture2D MovedTooFar_Texture;
    public static Texture2D NotSelectablePeriod_Texture;

    private Dictionary<string, int> Error_Dict;

    public int ErrorCount
    {
        get
        {
            return Error_Dict.Values.Sum();
        }
    }

    //so that other classes can subscribe and know exactly when a type of error feedback is occuring:
    public delegate void TouchErrorFeedbackEventHandler(object sender, TouchFeedbackArgs e);
    public event TouchErrorFeedbackEventHandler TouchErrorFeedbackEvent;


    public void Init(DataController trialData, DataController frameData)
    {
        CreateErrorDict();
        trialData.AddDatum("HeldTooLong", () => Error_Dict["HeldTooLong"]);
        trialData.AddDatum("HeldTooShort", () => Error_Dict["HeldTooShort"]);
        trialData.AddDatum("MovedTooFar", () => Error_Dict["MovedTooFar"]);
        trialData.AddDatum("NotSelectablePeriod", () => Error_Dict["NotSelectablePeriod"]);

        frameData.AddDatum("FeedbackOn", () => FeedbackOn.ToString());
        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);
        InstantiatedGO = null;
    }

    private void CreateErrorDict()
    {
        Error_Dict = new Dictionary<string, int>
        {
            { "HeldTooLong", 0 },
            { "HeldTooShort", 0 },
            { "MovedTooFar", 0 },
            { "NotSelectablePeriod", 0 }
        };
    }

    public void AddToIgnoreList(List<GameObject> objectsToIgnore)
    {
        ObjectsToIgnore ??= new List<GameObject>();
        ObjectsToIgnore.AddRange(objectsToIgnore);
    }

    public void EnableTouchFeedback(SelectionTracker.SelectionHandler handler, float fbDuration, float fbSize, GameObject taskCanvasGO, bool useRootPosition, List<GameObject> objects = null)
    {
        TouchFbEnabled = true;
        Handler = handler;
        FeedbackDuration = fbDuration;
        FeedbackSize = fbSize;
        TaskCanvasGO = taskCanvasGO;
        TaskCanvas = TaskCanvasGO.GetComponent<Canvas>();
        UseRootGoPos = useRootPosition;

        if (HeldTooShort_Prefab == null || HeldTooLong_Prefab == null || MovedTooFar_Prefab == null || NotSelectablePeriod_Prefab == null) //If null, create the prefabs
            CreatePrefabs();
        else //If not null, check if existing prefab's size is same as new size. If not, update the prefab size
            if (HeldTooShort_Prefab.transform.localScale != new Vector3(fbSize, fbSize, 1f))
                SetPrefabSizes(FeedbackSize);

        ObjectsToIgnore = new List<GameObject>();

        Handler.TouchErrorFeedback += OnTouchErrorFeedback; //Subscribe to event
    }

    public void DisableTouchFeedback()
    {
        TouchFbEnabled = false;
        Handler.TouchErrorFeedback -= OnTouchErrorFeedback;
    }

    private void OnTouchErrorFeedback(object sender, TouchFeedbackArgs e)
    {
        if (e.Selection.SelectedGameObject == null || e.Selection.ParentName == "ExperimenterDisplay")
            return;

        if(!FeedbackOn)
        {
            if (ObjectsToIgnore.Contains(e.Selection.SelectedGameObject))
            {
                return;
            }

            TouchErrorFeedbackEvent?.Invoke(this, e);

            //Debug.LogWarning("DUR AT TOUCHFB: " + e.Selection.Duration);

            switch (e.Selection.ErrorType)
            {
                case "DurationTooLong":
                    Error_Dict["HeldTooLong"]++;
                    Debug.Log("SHOWING TOUCH FEEDBACK FOR HOLDING TOO LONG!");
                    ShowTouchFeedback(new TouchFeedback(e.Selection, HeldTooLong_Prefab, this));
                    break;
                case "DurationTooShort":
                    Error_Dict["HeldTooShort"]++;
                    Debug.Log("SHOWING TOUCH FEEDBACK FOR HOLDING TOO SHORT!");
                    ShowTouchFeedback(new TouchFeedback(e.Selection, HeldTooShort_Prefab, this));
                    break;
                case "MovedTooFar":
                    Error_Dict["MovedTooFar"]++;
                    Debug.Log("SHOWING TOUCH FEEDBACK FOR MOVING TOO FAR!");
                    ShowTouchFeedback(new TouchFeedback(e.Selection, MovedTooFar_Prefab, this));
                    break;
                case "NotSelectablePeriod":
                    Error_Dict["NotSelectablePeriod"]++;
                    Debug.Log("SHOWING TOUCH FEEDBACK FOR SELECTING DURING A NON SELECTABLE PERIOD!");
                    ShowTouchFeedback(new TouchFeedback(e.Selection, NotSelectablePeriod_Prefab, this));
                    break;
                default:
                    break;
            }
        }

    }

    private void ShowTouchFeedback(TouchFeedback touchFb)
    {
        Handler.HandlerActive = false;
        FeedbackOn = true;
        audioFBController.Play("Negative");
        if (InstantiatedGO != null)
            Destroy(InstantiatedGO);

        touchFb.Prefab.SetActive(true);
        InstantiatedGO = Instantiate(touchFb.Prefab, TaskCanvasGO.transform);
        touchFb.Prefab.SetActive(false);

        InstantiatedGO.name = "TouchFeedback_GO";
        InstantiatedGO.GetComponent<RectTransform>().anchoredPosition = touchFb.PosOnCanvas;
        Session.EventCodeManager.SendCodeImmediate(Session.EventCodeManager.SessionEventCodes["TouchFBController_FeedbackOn"]);
        
        Invoke(nameof(DestroyTouchFeedback), FeedbackDuration);            
    }

    public void DestroyTouchFeedback() //Called in ShowTouchFeedback method above ^^
    {
        if (InstantiatedGO != null)
        {
            Destroy(InstantiatedGO);
            Session.EventCodeManager.SendCodeImmediate(Session.EventCodeManager.SessionEventCodes["TouchFBController_FeedbackOff"]);
            DeactivatePrefabs();
            Handler.HandlerActive = true;
            FeedbackOn = false;
        }
    }

    private void DeactivatePrefabs()
    {
        foreach (var prefab in PrefabList)
            if(prefab.activeInHierarchy)
                prefab.SetActive(false);
    }

    private void CreatePrefabs()
    {
        PrefabList = new List<GameObject>();

        try
        {
            HeldTooLong_Prefab = CreatePrefab("HeldTooLongGO", HeldTooLong_Texture);
            HeldTooShort_Prefab = CreatePrefab("HeldTooShortGO", HeldTooShort_Texture);
            MovedTooFar_Prefab = CreatePrefab("MovedTooFarGO", MovedTooFar_Texture);
            NotSelectablePeriod_Prefab = CreatePrefab("NotSelectablePeriodGO", NotSelectablePeriod_Texture);
        }
        catch (Exception e)
        {
            Debug.LogWarning("FAILED TO CREATE TOUCHFBCONTROLLER PREFABS! " + e.ToString());
        }
    }

    private GameObject CreatePrefab(string name, Texture2D texture)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<RectTransform>();

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 2;
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
        renderer.color = new Color32(224, 78, 92, 235);

        go.transform.localScale = new Vector3(FeedbackSize, FeedbackSize, 1f);
        
        go.SetActive(false);
        PrefabList.Add(go); 
        return go;
    }

    public void ClearErrorCounts()
    {
        Error_Dict = Error_Dict.ToDictionary(kvp => kvp.Key, kvp => 0);
    }

    public void SetPrefabSizes(float size)
    {
        FeedbackSize = size;

        if (PrefabList.Count > 0)
        {
            foreach (GameObject prefab in PrefabList)
                prefab.transform.localScale = new Vector3(size, size, 1f);
        }
        else
            Debug.LogWarning("Trying to change the prefab sizes, but the prefablist only has " + PrefabList.Count + " items!");
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
            Vector3 objPos = TouchFeedbackController.UseRootGoPos ? Selection.SelectedGameObject.transform.root.position : Selection.SelectedGameObject.transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(objPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, Camera.main, out Vector2 localPoint);
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
