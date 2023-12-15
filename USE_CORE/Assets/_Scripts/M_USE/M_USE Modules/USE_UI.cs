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




using System;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using USE_Data;


namespace USE_UI
{
    public class HumanStartPanel : MonoBehaviour
    {
        [HideInInspector] public GameObject HumanStartPanelGO;

        [HideInInspector] public GameObject StartButtonGO;
        [HideInInspector] public List<GameObject> StartButtonChildren;

        [HideInInspector] public GameObject InstructionsButtonGO;
        [HideInInspector] public GameObject InstructionsGO;
        [HideInInspector] public GameObject TitleTextGO;
        [HideInInspector] public GameObject HumanBackgroundGO;
        [HideInInspector] public GameObject BackgroundPanelGO;
        [HideInInspector] public GameObject EndTaskButtonGO;

        [HideInInspector] public GameObject HumanStartPanelPrefab; //Set to Session In inspector, then passed down

        [HideInInspector] public bool HumanPanelOn;
        [HideInInspector] public bool InstructionsOn;

        [HideInInspector] public Vector3 InitialStartButtonPosition;
        [HideInInspector] public Vector3 InitialInstructionsButtonPosition;
        [HideInInspector] public Vector3 InitialEndTaskButtonPosition;

        private Dictionary<string, string> TaskInstructionsDict;
        private Dictionary<string, string> TaskNamesDict;

        [HideInInspector] public string TaskName;

        private State SetActiveOnInitialization;
        private State SetInactiveOnTermination;


        private void Start()
        {
            CreateDictionaries();
        }

        private void CreateDictionaries()
        {
            TaskInstructionsDict = new Dictionary<string, string>()
            {
                { "AntiSaccade", "You get a brief glimpse of the target object, and then must select it among the distractor objects!" },
                { "ContinuousRecognition", "Unique objects are displayed each trial and you must select an object you haven't previously chosen. If you choose an object you've chosen in a previous trial, you lose!" },
                { "EffortControl", "Choose a balloon to inflate based on the effort required (number of clicks) and the reward amount (number of tokens). Inflate and pop the balloon by clicking it the required number of times!"},
                { "FlexLearning", "The maximal token gain is associated with one specific visual feature that defines one of the objects. Learn the visual feature that provides the most reward!"},
                { "MazeGame", "Navigate your way from the start of the maze to the end of the maze to earn your reward. An incorrect step will require re-touching the last correct step." },
                { "THR", "Learn touching and holding the square for the correct duration to earn your reward. Holding too short, holding too long, and moving outside the square will result in negative feedback." },
                { "VisualSearch", "Each trial, a target object is displayed among distractor objects. Find the targeted object to earn your reward!" },
                { "WhatWhenWhere", "Learn the sequential relationship between objects. Select the objects in the correct sequence to earn your reward!" },
                { "WorkingMemory", "Remember and identify the target object to earn your reward. Don't let the distractor objects fool you!" }
            };
            TaskNamesDict = new Dictionary<string, string>()
            {
                { "AntiSaccade", "Anti Saccade"},
                { "ContinuousRecognition", "Continuous Recognition" },
                { "EffortControl", "Effort Control" },
                { "FlexLearning", "Flexible Learning" },
                { "MazeGame", "Maze Game" },
                { "THR", "Touch Hold Release" },
                { "VisualSearch", "Visual Search" },
                { "WhatWhenWhere", "What When Where" },
                { "WorkingMemory", "Working Memory" }
            };
        }

        //New tasks that are built can call this from their TaskLevel
        public void AddTaskInstructions(string taskNameOneWord, string instructions)
        {
            if (TaskInstructionsDict.ContainsKey(taskNameOneWord))  //if already included, update it
                TaskInstructionsDict[taskNameOneWord] = instructions;
            else
                TaskInstructionsDict.Add(taskNameOneWord, instructions);
        }

        //New tasks that are built can call this from their TaskLevel
        public void AddTaskDisplayName(string taskNameOneWord, string taskNameWithSpace)
        {
            if (TaskNamesDict.ContainsKey(taskNameOneWord)) //if already included, update it

                TaskNamesDict[taskNameOneWord] = taskNameWithSpace;
            else
                TaskNamesDict.Add(taskNameOneWord, taskNameWithSpace);
        }

        //Called by TaskLevel
        public void CreateHumanStartPanel(DataController frameData, Canvas parent, string taskName)
        {
            frameData.AddDatum("HumanPanelOn", () => HumanPanelOn.ToString());
            frameData.AddDatum("InstructionsOn", () => InstructionsOn.ToString());

            HumanStartPanelGO = Instantiate(HumanStartPanelPrefab);
            HumanStartPanelGO.name = taskName + "_HumanPanel";
            HumanStartPanelGO.transform.SetParent(parent.transform, false);
            HumanStartPanelGO.SetActive(false);
            HumanPanelOn = false;

            TitleTextGO = HumanStartPanelGO.transform.Find("TitleText").gameObject;
            TaskName = TaskNamesDict[taskName];
            TitleTextGO.GetComponent<TextMeshProUGUI>().text = TaskName;

            StartButtonGO = HumanStartPanelGO.transform.Find("StartButton").gameObject;
            InitialStartButtonPosition = StartButtonGO.transform.localPosition;

            HumanBackgroundGO = HumanStartPanelGO.transform.Find("HumanBackground").gameObject;
            HumanBackgroundGO.transform.localPosition = new Vector3(0, 0, 1000f);

            BackgroundPanelGO = HumanStartPanelGO.transform.Find("BackgroundPanel").gameObject;

            EndTaskButtonGO = HumanStartPanelGO.transform.Find("EndTaskButton").gameObject;
            if (Session.UsingDefaultConfigs)
                EndTaskButtonGO.AddComponent<ButtonHoverEffect>();
            Button endTaskButton = EndTaskButtonGO.AddComponent<Button>();
            endTaskButton.onClick.AddListener(HandleEndTask);

            InstructionsButtonGO = HumanStartPanelGO.transform.Find("InstructionsButton").gameObject;
            if(Session.UsingDefaultConfigs)
                InstructionsButtonGO.AddComponent<ButtonHoverEffect>();
            Button button = InstructionsButtonGO.AddComponent<Button>();
            button.onClick.AddListener(ToggleInstructions);

            InstructionsGO = HumanStartPanelGO.transform.Find("Instructions").gameObject;
            InstructionsGO.GetComponentInChildren<Text>().text = TaskInstructionsDict[taskName];
            InstructionsGO.SetActive(false);
            InstructionsOn = false;

            InitialInstructionsButtonPosition = InstructionsButtonGO.transform.localPosition;
            InitialEndTaskButtonPosition = EndTaskButtonGO.transform.localPosition;

            SetStartButtonChildren();
        }

        public void HandleEndTask()
        {
            if (Session.TrialLevel != null)
            {
                if (Time.timeScale == 0) //if paused, unpause before ending task
                    Time.timeScale = 1;

                Session.TrialLevel.AbortCode = 5;
                Session.EventCodeManager.SendRangeCode("CustomAbortTrial", Session.TrialLevel.AbortCodeDict["EndTask"]);
                Session.TrialLevel.ForceBlockEnd = true;
                Session.TrialLevel.FinishTrialCleanup();
                Session.TrialLevel.ClearActiveTrialHandlers();
                Session.TaskLevel.SpecifyCurrentState(Session.TaskLevel.GetStateFromName("FinishTask"));
            }
        }

        private void SetStartButtonChildren()
        {
            StartButtonChildren = new List<GameObject>();
            foreach (Transform child in StartButtonGO.transform)
                StartButtonChildren.Add(child.gameObject);
        }

        public void ToggleInstructions() //Used by Subject/Player to toggle Instructions
        {
            InstructionsGO.SetActive(!InstructionsGO.activeInHierarchy);
            InstructionsOn = InstructionsGO.activeInHierarchy;
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes[InstructionsGO.activeInHierarchy ? "InstructionsOn" : "InstructionsOff"]);
        }

        //Called at end of SetupTrial (TrialLevel)
        public void AdjustPanelBasedOnTrialNum(int trialCountInTask, int trialCountInBlock)
        {
            if (trialCountInTask == 0) //Show Full Human Panel With BlueBackground
            {
                HumanBackgroundGO.SetActive(true);
                TitleTextGO.GetComponent<TextMeshProUGUI>().text = TaskName;
                TitleTextGO.SetActive(true);
                BackgroundPanelGO.SetActive(false);
            }
            else
            {
                BackgroundPanelGO.SetActive(true);
                HumanBackgroundGO.SetActive(false);

                if(trialCountInBlock > 0) //Mid block - show only playbutton and instructions
                {
                    TitleTextGO.SetActive(false);
                    StartButtonGO.transform.localPosition = new Vector3(InitialStartButtonPosition.x, InitialStartButtonPosition.y + 75f, InitialStartButtonPosition.z);
                }
                else if(trialCountInBlock == 0 && trialCountInTask != 0) //"New Game" - show text, playbutton, instructions
                {
                    StartButtonGO.transform.localPosition = InitialStartButtonPosition;
                    TitleTextGO.GetComponent<TextMeshProUGUI>().text = "Play Again?";
                    TitleTextGO.SetActive(true);
                }
            }
        }


        public void SetVisibilityOnOffStates(State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            if (setActiveOnInit != null)
            {
                SetActiveOnInitialization = setActiveOnInit;
                SetActiveOnInitialization.StateInitializationFinished += ActivateOnStateInit;
            }
            if (setInactiveOnTerm != null)
            {
                SetInactiveOnTermination = setInactiveOnTerm;
                SetInactiveOnTermination.StateTerminationFinished += DeactivateOnStateTerm;
            }
        }

        private void ActivateOnStateInit(object sender, EventArgs e)
        {
            HumanStartPanelGO.SetActive(true);
            HumanPanelOn = true;
            if(Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HumanStartPanelOn"]);
            if (!StartButtonGO.activeInHierarchy)
                StartButtonGO.SetActive(true);
        }

        private void DeactivateOnStateTerm(object sender, EventArgs e)
        {
            HumanStartPanelGO.SetActive(false);
            HumanPanelOn = false;
            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HumanStartPanelOff"]);
        }

    }

    public class USE_StartButton : MonoBehaviour
    {
        [HideInInspector] public GameObject StartButtonGO;
        [HideInInspector] public Image CoverCircle_Image; //Child circle that can be used to "change circle color" by activating over top of startbutton.
        [HideInInspector] public GameObject PlayIconGO; //Child Play icon
        [HideInInspector] public List<GameObject> StartButtonChildren;
        [HideInInspector] public GameObject StartButtonPrefab;
        [HideInInspector] public bool IsGrating;
        [HideInInspector] public bool IsHuman;

        public State SetActiveOnInitialization;
        public State SetInactiveOnTermination;

        public GameObject CreateStartButton(Canvas parent, Vector3? pos, float? scale, string name = null)
        {            
            StartButtonGO = Instantiate(StartButtonPrefab);
            StartButtonGO.name = name ?? "StartButton";
            StartButtonGO.transform.SetParent(parent.transform, false);
            StartButtonGO.transform.localPosition = pos.HasValue? pos.Value : Vector3.zero;
            CoverCircle_Image = StartButtonGO.transform.Find("CoverCircle").gameObject.GetComponent<Image>();
            PlayIconGO = StartButtonGO.transform.Find("PlayIcon").gameObject;

            StartButtonGO.transform.localScale = scale.HasValue ? new Vector3(scale.Value, scale.Value, 1) : new Vector3(1.2f, 1.2f, 0);

            PlayIconGO.GetComponent<SpriteRenderer>().color = new Color32(38, 188, 250, 255); //LightBlue PlayIcon for non-human version

            StartButtonChildren = new List<GameObject>();
            foreach (Transform child in StartButtonGO.transform)
            {
                StartButtonChildren.Add(child.gameObject);
            }
            StartButtonGO.SetActive(false);

            return StartButtonGO;
        }

        public void ActivateCoverCircle(Color32 color)
        {
            CoverCircle_Image.color = color;
            PlayIconGO.SetActive(false);
            CoverCircle_Image.gameObject.SetActive(true);
        }

        public void DeactivateCoverCircle()
        {
            PlayIconGO.SetActive(true);
            CoverCircle_Image.gameObject.SetActive(false);
        }

        public void SetButtonPosition(Vector3 pos)
        {
            StartButtonGO.transform.localPosition = pos;
        }

        public void SetPlayIconColor(Color32 color)
        {
            PlayIconGO.GetComponent<SpriteRenderer>().color = color;
        }

		public void SetButtonScale(float scale)
		{
            StartButtonGO.transform.localScale = new Vector3(scale, scale, 1);
            HoverEffect hoverComponent = StartButtonGO.GetComponent<HoverEffect>();
            if (hoverComponent != null)
                hoverComponent.originalScale = StartButtonGO.transform.localScale; //update original scale
        }

        public void SetVisibilityOnOffStates(State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            if (setActiveOnInit != null)
            {
                SetActiveOnInitialization = setActiveOnInit;
                SetActiveOnInitialization.StateInitializationFinished += ActivateOnStateInit;
            }
            if (setInactiveOnTerm != null)
            {
                SetInactiveOnTermination = setInactiveOnTerm;
                SetInactiveOnTermination.StateTerminationFinished += InactivateOnStateTerm;
            }
        }

        private void ActivateOnStateInit(object sender, EventArgs e)
        {
            StartButtonGO.SetActive(true);
        }

        private void InactivateOnStateTerm(object sender, EventArgs e)
        {
            StartButtonGO.SetActive(false);
        }


        public IEnumerator GratedFlash(GameObject go, Texture2D newTexture, float duration, GameObject goToDeactivate = null)
        {
            Image image = go.GetComponent<Image>();
            if (image == null)
                Debug.LogError($"TRYING TO GRATE THE IMAGE OF A GAMEOBJECT ({go.name}) THAT DOESNT HAVE AN IMAGE COMPONENT!");

            IsGrating = true;
            Color32 originalColor = image.color;
            Sprite originalSprite = image.sprite;
            image.color = new Color32(255, 153, 153, 255);
            image.sprite = Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height), Vector2.one / 2f);

            yield return new WaitForSeconds(duration);

            image.color = originalColor;
            image.sprite = originalSprite;
            IsGrating = false;

            if(goToDeactivate != null)
                goToDeactivate.SetActive(false);
        }

    }

    public class USE_Backdrop : USE_StartButton
    {
        [HideInInspector] public GameObject BackdropGO;
        [HideInInspector] public Image Image;

        //Used as backdrop for THR
        public GameObject CreateBackdrop(Canvas parent, string name, Color32 color) 
        {
            BackdropGO = new GameObject(name);
            Image = BackdropGO.AddComponent<Image>();
            BackdropGO.transform.SetParent(parent.transform, false);
            Image.rectTransform.anchoredPosition = Vector2.zero;
            RectTransform canvasRect = parent.GetComponent<RectTransform>();
            Image.rectTransform.sizeDelta = new Vector2(canvasRect.rect.width, canvasRect.rect.height);
            Image.color = color;
            BackdropGO.transform.localPosition = Vector3.zero;
            BackdropGO.SetActive(false);
            return BackdropGO;
        }
    }

    public class USE_Square : USE_StartButton
    {
        //*** Inherits StartButtonGO from USE_StartButton ***
        [HideInInspector] public Image Image;

        public GameObject CreateSquareStartButton(Canvas parent, Vector3? localPos, float? scale, Color32? color, string name = null)
        {
            StartButtonGO = new GameObject(name ?? "StartButton");
            Image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            Image.rectTransform.anchoredPosition = Vector2.zero;
            Image.rectTransform.sizeDelta = scale.HasValue? new Vector2(scale.Value, scale.Value) : new Vector2(100f, 100f);
            StartButtonGO.transform.localPosition = localPos.HasValue ? localPos.Value : Vector3.zero;
            Image.color = color.HasValue ? color.Value : new Color32(0, 0, 128, 255);
            StartButtonGO.SetActive(false);
            return StartButtonGO;
        }

        public void SetSquareColor(Color color)
        {
            Image.color = color;
        }

        public void SetSquareSize(float size)
        {
            Image.rectTransform.sizeDelta = new Vector2(size, size);
        }

    }

    public class UI_Debugger
    {
        public GameObject DebugTextGO;
        public TextMeshProUGUI DebugText;
        public RectTransform Rect;

        public void InitDebugger(Canvas parent, Vector2? scale, Vector3? pos, string text = null)
        {
            DebugTextGO = new GameObject("DebugText");
            DebugTextGO.transform.SetParent(parent.transform);
            DebugTextGO.transform.localScale = Vector3.one;
            DebugTextGO.transform.localPosition = pos.HasValue? pos.Value : Vector3.zero;
            Rect = DebugTextGO.AddComponent<RectTransform>();
            Rect.sizeDelta = scale.HasValue? scale.Value : new Vector2(800, 100);
            DebugText = DebugTextGO.AddComponent<TextMeshProUGUI>();
            DebugText.alignment = TextAlignmentOptions.Center;
            DebugText.color = Color.black;
            if (text != null)
                DebugText.text = text;
            DebugTextGO.SetActive(false);
        }

        public void SetFontSize(int fontSize)
        {
            DebugText.fontSize = fontSize;
        }

        public void SetDebugText(string text)
        {
            DebugText.text = text;
        }

        public void SetSize(Vector2 size)
        {
            Rect.sizeDelta = size;
        }

        public void SetTextColor(Color32 color)
        {
            DebugText.color = color;
        }

        public void ActivateDebugText()
        {
            DebugTextGO.SetActive(true);
        }

        public void DeactivateDebugText()
        {
            DebugTextGO.SetActive(false);
        }
    }

    public class USE_Circle : MonoBehaviour
    {
        public GameObject CircleGO;
        public float CircleSize = 10f;
        public Color CircleColor = new Color(1, 1, 1, 1);
        public Image Image;
        public Sprite Sprite;
        public Vector3 LocalPosition = new Vector3(0, 0, 0);
        public State SetActiveOnInitialization;
        public State SetInactiveOnTermination;

        public USE_Circle(Canvas parent, Vector2 circleLocation, float size, string name)
        {
            CircleGO = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UICircle));
            CircleGO.transform.SetParent(parent.transform, false);
            CircleGO.transform.localScale = Vector3.one * size;

            var circle = CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>();
            circle.Fill = true;
            circle.Thickness = 2f;

            var rect = CircleGO.GetComponent<RectTransform>();

            /*rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1920);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1080)*/;
            rect.sizeDelta = new Vector2(1920, 1080);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.anchoredPosition = circleLocation;
            
            CircleGO.SetActive(false);
        }

        //----------------------------------------------------------------------
        public void SetVisibilityOnOffStates(State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            if (setActiveOnInit != null)
            {
                SetActiveOnInitialization = setActiveOnInit;
                SetActiveOnInitialization.StateInitializationFinished += ActivateOnStateInit;
            }
            if (setInactiveOnTerm != null)
            {
                SetInactiveOnTermination = setInactiveOnTerm;
                SetInactiveOnTermination.StateTerminationFinished += InactivateOnStateTerm;
            }
        }
        private void ActivateOnStateInit(object sender, EventArgs e)
        {
            CircleGO.SetActive(true);
        }
        private void InactivateOnStateTerm(object sender, EventArgs e)
        {
            CircleGO.SetActive(false);
        }
        public void SetCircleScale(float size)
        {
            this.CircleGO.transform.localScale = new Vector3(size, size, size);
        }
    }
    public class USE_Line
    {
        public GameObject LineGO;
        public float LineSize = 1f;
        public float LineLength = 0f;
        public Color LineColor = new Color(1, 1, 1, 1);
        public Vector3 LocalPosition = new Vector3(0, 0, 0);
        private Color32 originalColor;
        private Sprite originalSprite;
        public State SetActiveOnInitialization;
        public State SetInactiveOnTermination;
        public USE_Line(Canvas parent, Vector2 start, Vector2 end, Color col, string name, bool adjustAnchor = false)
        {
            LineGO = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UILineRenderer));
            LineGO.transform.SetParent(parent.transform, false);
            if (adjustAnchor)
            {
                LineGO.GetComponent<RectTransform>().anchorMax = Vector2.zero;
                LineGO.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                LineGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; 
            }
            
            LineGO.GetComponent<RectTransform>().sizeDelta = new Vector2(LineSize, LineSize);
            UnityEngine.UI.Extensions.UILineRenderer LineRenderer = LineGO.GetComponent<UnityEngine.UI.Extensions.UILineRenderer>();
            LineLength = Vector2.Distance(start, end);
            LineRenderer.LineThickness = 10f;
            LineRenderer.Points = new Vector2[] { start, end };
            LineRenderer.color = col;
            LineRenderer.RelativeSize = false;
            LineRenderer.SetAllDirty();
        }
        
        
        
        //----------------------------------------------------------------------
        public void SetVisibilityOnOffStates(State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            if (setActiveOnInit != null)
            {
                SetActiveOnInitialization = setActiveOnInit;
                SetActiveOnInitialization.StateInitializationFinished += ActivateOnStateInit;
            }
            if (setInactiveOnTerm != null)
            {
                SetInactiveOnTermination = setInactiveOnTerm;
                SetInactiveOnTermination.StateTerminationFinished += InactivateOnStateTerm;
            }
        }
        private void ActivateOnStateInit(object sender, EventArgs e)
        {
            LineGO.SetActive(true);
        }
        private void InactivateOnStateTerm(object sender, EventArgs e)
        {
            LineGO.SetActive(false);
        }
        public void SetLineWidth(float size)
        {
            this.LineGO.transform.localScale = new Vector3(size, size, size);
        }
    }




}
