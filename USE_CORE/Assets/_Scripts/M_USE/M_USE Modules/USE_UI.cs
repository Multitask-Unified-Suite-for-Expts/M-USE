using System;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using USE_Data;
using USE_ExperimentTemplate_Classes;
using USE_UI;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Session;
using USE_ExperimentTemplate_Trial;

namespace USE_UI
{
    public class HumanStartPanel : MonoBehaviour
    {
        [HideInInspector] public GameObject HumanStartPanelGO;

        [HideInInspector] public GameObject StartButtonGO;

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

        [HideInInspector] public Dictionary<string, string> TaskInstructionsDict = new Dictionary<string, string>()
        {
            { "ContinuousRecognition", "Each trial, several objects will be displayed and you must choose an object you haven't chosen in a previous trial!" },
            { "EffortControl", "Choose a balloon to inflate based on the effort required (click) and the reward amount (tokens). Pop the balloon by clicking the required number of times!"},
            { "FlexLearning", "Learn the visual feature that provides the most reward!"},
            { "MazeGame", "Find your way to the end of the Maze to earn your reward!" },
            { "THR", "Learn to touch and hold the square. Earn your reward by holding the square for the correct duration!" },
            { "VisualSearch", "Find the targeted object to earn your reward!" },
            { "WhatWhenWhere", "Select the objects in the correct sequence to earn your reward!" },
            { "WorkingMemory", "Remember and identify the target object to earn your reward!" }
        };
        [HideInInspector] public Dictionary<string, string> TaskNamesDict = new Dictionary<string, string>()
        {
            { "ContinuousRecognition", "Continuous Recognition" },
            { "EffortControl", "Effort Control" },
            { "FlexLearning", "Flexible Learning" },
            { "MazeGame", "Maze Game" },
            { "THR", "Touch Hold Release" },
            { "VisualSearch", "Visual Search" },
            { "WhatWhenWhere", "What When Where" },
            { "WorkingMemory", "Working Memory" },

        };

        [HideInInspector] public Dictionary<string, float> Task_HumanBackgroundZPos_Dict = new Dictionary<string, float>()
        {
            { "ContinuousRecognition", 1000f },
            { "EffortControl", 500f },
            { "FlexLearning", 1000f },
            { "MazeGame", 500f },
            { "THR", 1000f },
            { "VisualSearch", 1000f },
            { "WhatWhenWhere", 500f },
            { "WorkingMemory", 1000f },
        };

        [HideInInspector] public string TaskName;

        private State SetActiveOnInitialization;
        private State SetInactiveOnTermination;

        [HideInInspector] public static EventCodeManager EventCodeManager;
        [HideInInspector] public static Dictionary<string, EventCode> SessionEventCodes;

        public ControlLevel_Session_Template SessionLevel;
        public ControlLevel_Task_Template TaskLevel;
        public ControlLevel_Trial_Template TrialLevel;



        //Called by TaskLevel
        public void SetupDataAndCodes(DataController frameData, EventCodeManager eventCodeManager, Dictionary<string, EventCode> sessionEventCodes)
        {
            SessionEventCodes = sessionEventCodes;
            EventCodeManager = eventCodeManager;

            frameData.AddDatum("HumanPanelOn", () => HumanPanelOn.ToString());
            frameData.AddDatum("InstructionsOn", () => InstructionsOn.ToString());
        }

        //Called by TaskLevel
        public void CreateHumanStartPanel(Canvas parent, string taskName)
        {
            HumanStartPanelGO = Instantiate(HumanStartPanelPrefab);
            HumanStartPanelGO.name = taskName + "_HumanPanel";
            HumanStartPanelGO.transform.SetParent(parent.transform, false);

            TitleTextGO = HumanStartPanelGO.transform.Find("TitleText").gameObject;
            TaskName = TaskNamesDict[taskName];
            TitleTextGO.GetComponent<TextMeshProUGUI>().text = TaskName;

            StartButtonGO = HumanStartPanelGO.transform.Find("StartButton").gameObject;
            InitialStartButtonPosition = StartButtonGO.transform.localPosition;
            StartButtonGO.AddComponent<HoverEffect>();

            HumanBackgroundGO = HumanStartPanelGO.transform.Find("HumanBackground").gameObject;
            HumanBackgroundGO.transform.localPosition = new Vector3(0, 0, Task_HumanBackgroundZPos_Dict[taskName]);

            BackgroundPanelGO = HumanStartPanelGO.transform.Find("BackgroundPanel").gameObject;

            EndTaskButtonGO = HumanStartPanelGO.transform.Find("EndTaskButton").gameObject;
            EndTaskButtonGO.AddComponent<HoverEffect>();
            Button endTaskButton = EndTaskButtonGO.AddComponent<Button>();
            endTaskButton.onClick.AddListener(HandleEndTask);

            InstructionsButtonGO = HumanStartPanelGO.transform.Find("InstructionsButton").gameObject;
            InstructionsButtonGO.AddComponent<HoverEffect>();
            Button button = InstructionsButtonGO.AddComponent<Button>();
            button.onClick.AddListener(ToggleInstructions);

            InstructionsGO = HumanStartPanelGO.transform.Find("Instructions").gameObject;
            InstructionsGO.GetComponentInChildren<Text>().text = TaskInstructionsDict[taskName];
            InstructionsGO.SetActive(false);
            InstructionsOn = false;

            if(Application.isEditor)
                AdjustButtonPositions();

            HumanStartPanelGO.SetActive(false);
            HumanPanelOn = false;
        }

        private void AdjustButtonPositions()
        {
            InstructionsButtonGO.transform.localPosition = new Vector3(InstructionsButtonGO.transform.localPosition.x, InstructionsButtonGO.transform.localPosition.y + 44f, InstructionsButtonGO.transform.localPosition.z);
            EndTaskButtonGO.transform.localPosition = new Vector3(EndTaskButtonGO.transform.localPosition.x, EndTaskButtonGO.transform.localPosition.y + 44f, EndTaskButtonGO.transform.localPosition.z);
        }


        public void HandleEndTask()
        {
            if (TrialLevel != null)
            {
                TrialLevel.AbortCode = 5;
                TrialLevel.ForceBlockEnd = true;
                TrialLevel.FinishTrialCleanup();
                TrialLevel.ClearActiveTrialHandlers();
                TaskLevel.SpecifyCurrentState(TaskLevel.GetStateFromName("FinishTask"));
            }
        }

        public void ToggleInstructions() //Used by Subject/Player to toggle Instructions
        {
            InstructionsGO.SetActive(InstructionsGO.activeInHierarchy ? false : true);
            InstructionsOn = InstructionsGO.activeInHierarchy ? true : false;
            EventCodeManager.SendCodeImmediate(SessionEventCodes[InstructionsGO.activeInHierarchy ? "InstructionsOn" : "InstructionsOff"]);

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
                    //TitleTextGO.GetComponent<TextMeshProUGUI>().text = "Trial " + (trialCountInBlock + 1);
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

        public void SetSessionLevel(ControlLevel_Session_Template sessionLevel)
        {
            SessionLevel = sessionLevel;
        }
        public void SetTaskLevel(ControlLevel_Task_Template taskLevel)
        {
            TaskLevel = taskLevel;
        }
        public void SetTrialLevel(ControlLevel_Trial_Template trialLevel)
        {
            TrialLevel = trialLevel;
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
            HumanStartPanelGO.SetActive(true);
            HumanPanelOn = true;
            EventCodeManager.SendCodeImmediate(SessionEventCodes["HumanStartPanelOn"]);
            if (!StartButtonGO.activeInHierarchy)
                StartButtonGO.SetActive(true);
        }

        private void InactivateOnStateTerm(object sender, EventArgs e)
        {
            HumanStartPanelGO.SetActive(false);
            HumanPanelOn = false;
            EventCodeManager.SendCodeImmediate(SessionEventCodes["HumanStartPanelOff"]);
        }



    }

    public class USE_TaskButton : MonoBehaviour
    {
        public GameObject TaskButtonGO;
        public float ButtonSize = 10f;
        public Color ButtonColor = new Color(1f, 1f, 1f, 1f);
        public RawImage Image;
        public Vector3 LocalPosition = new Vector3(0, 0, 0);
        private Color32 originalColor;
        private Sprite originalSprite;
        public string configName;
        public string taskName;
        
        public USE_TaskButton(Canvas parent, Vector3 localPos, float size, string configName)
        {
            LocalPosition = localPos;
            ButtonSize = size;
            TaskButtonGO = new GameObject(configName + "Button");
            TaskButtonGO.AddComponent<USE_TaskButton>();
            TaskButtonGO.GetComponent<USE_TaskButton>().configName = configName;
            Image = TaskButtonGO.AddComponent<RawImage>();
            TaskButtonGO.transform.SetParent(parent.transform, false);
            Image.rectTransform.anchoredPosition = Vector2.zero;
            Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            Image.color = ButtonColor;
            TaskButtonGO.transform.localPosition = LocalPosition;
            TaskButtonGO.SetActive(true);
        }
    }
    public class USE_StartButton : MonoBehaviour
	{
		public GameObject StartButtonGO;
		public float ButtonSize = 10f;
		public Color ButtonColor = new Color(0, 0, 128, 255);
		public Image Image;
        public Vector3 LocalPosition = new Vector3(0, 0, 0);
        private Color32 originalColor;
        private Sprite originalSprite;
        public bool IsGrating = false;

        public State SetActiveOnInitialization;
        public State SetInactiveOnTermination;


        //--------------------------Constructors----------------------------

        //Main Constructor:
        public USE_StartButton(Canvas parent, Vector3 localPos, float size)
        {
            LocalPosition = localPos;
            ButtonSize = size;
            StartButtonGO = new GameObject("StartButton");
            Image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            Image.rectTransform.anchoredPosition = Vector2.zero;
            Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            Image.color = ButtonColor;
            StartButtonGO.transform.localPosition = LocalPosition;
            StartButtonGO.SetActive(false);

        }

        public USE_StartButton(Canvas parent)
        {
            StartButtonGO = new GameObject("StartButton");
            Image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            Image.rectTransform.anchoredPosition = Vector2.zero;
            Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            Image.color = ButtonColor;
            StartButtonGO.transform.localPosition = LocalPosition;
            StartButtonGO.SetActive(false);
        }

        //Used by THR:
        public USE_StartButton(Canvas parent, string name)
		{
			StartButtonGO = new GameObject(name);
			Image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            Image.rectTransform.anchoredPosition = Vector2.zero;
            Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
			Image.color = ButtonColor;
            StartButtonGO.transform.localPosition = LocalPosition;
            StartButtonGO.SetActive(false);
        }

        //For a fullscreen backdrop (for THR):
        public USE_StartButton(Canvas parent, string name, Color32 color, bool fullScreen)
        {
            StartButtonGO = new GameObject(name);
            Image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            Image.rectTransform.anchoredPosition = Vector2.zero;
            RectTransform canvasRect = parent.GetComponent<RectTransform>();
            if (fullScreen)
                Image.rectTransform.sizeDelta = new Vector2(canvasRect.rect.width, canvasRect.rect.height);
            else
                Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            Image.color = color;
            Image.canvas.sortingOrder = -1;
            StartButtonGO.transform.localPosition = LocalPosition;
            StartButtonGO.SetActive(false);
        }

        //----------------------------------------------------------------------
        public void SetButtonPosition(Vector3 pos)
        {
            StartButtonGO.transform.localPosition = pos;
        }

        public void SetButtonColor(Color color)
		{
			ButtonColor = color;
			Image.color = ButtonColor;
		}

		public void SetButtonSize(float size)
		{
			ButtonSize = size;
            Image.rectTransform.localScale = new Vector2(ButtonSize, ButtonSize);
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


        public IEnumerator GratedFlash(Texture2D newTexture, float duration)
        {
            IsGrating = true;
            originalColor = Image.color;
            originalSprite = Image.sprite;
            Image.color = new Color32(255, 153, 153, 255);
            Image.sprite = Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height), Vector2.one / 2f);

            yield return new WaitForSeconds(duration);

            Image.color = originalColor;
            Image.sprite = originalSprite;
            IsGrating = false;
        }


        public void GratedStartButtonFlash(Texture2D newTexture, float duration, bool deactivateAfter)
        {
            if (!IsGrating)
            {
                IsGrating = true;
                if (!StartButtonGO.activeInHierarchy)
                    StartButtonGO.SetActive(true);
                originalColor = Image.color;
                originalSprite = Image.sprite;

                Image.color = new Color32(224, 78, 92, 255);
                Image.sprite = Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height), Vector2.one / 2f);
            }
            if (duration <= 0)
            {
                Image.color = originalColor;
                Image.sprite = originalSprite;
                if (deactivateAfter)
                    StartButtonGO.SetActive(false);

                IsGrating = false;
            }
            
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
        private Color32 originalColor;
        private Sprite originalSprite;
        public State SetActiveOnInitialization;
        public State SetInactiveOnTermination;
        public USE_Circle(Canvas parent, Vector2 circleLocation, float size, string name)
        {
            CircleGO = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UICircle));
            CircleGO.transform.SetParent(parent.transform, false);
            CircleGO.transform.localScale = new Vector3(size, size, size);
            CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().Fill = true;
            CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().Thickness = 2f;
            CircleGO.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            CircleGO.GetComponent<RectTransform>().anchorMax = Vector2.zero;
            CircleGO.GetComponent<RectTransform>().anchoredPosition = circleLocation;
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
    public class USE_Line : MonoBehaviour
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
        public USE_Line(Canvas parent, Vector2 start, Vector2 end, Color col, string name)
        {
            LineGO = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UILineRenderer));
            LineGO.transform.SetParent(parent.transform, false);
            LineGO.GetComponent<RectTransform>().anchorMax = Vector2.zero;
            LineGO.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            LineGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            LineGO.GetComponent<RectTransform>().sizeDelta = new Vector2(LineSize, LineSize);
            UnityEngine.UI.Extensions.UILineRenderer LineRenderer = LineGO.GetComponent<UnityEngine.UI.Extensions.UILineRenderer>();
            LineLength = Vector2.Distance(start, end);
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







//Unused constructors:

//public USE_StartButton(Canvas parent, Vector3 localPos)
//{
//    LocalPosition = localPos;
//    StartButtonGO = new GameObject("StartButton");
//    Image = StartButtonGO.AddComponent<Image>();
//    StartButtonGO.transform.SetParent(parent.transform, false);
//    Image.rectTransform.anchoredPosition = Vector2.zero;
//    Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
//    Image.color = ButtonColor;
//    StartButtonGO.transform.localPosition = LocalPosition;
//    StartButtonGO.SetActive(false);
//}

//public USE_StartButton(Canvas parent, float size)
//{
//    ButtonSize = size;
//    StartButtonGO = new GameObject("StartButton");
//    Image = StartButtonGO.AddComponent<Image>();
//    StartButtonGO.transform.SetParent(parent.transform, false);
//    Image.rectTransform.anchoredPosition = Vector2.zero;
//    Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
//    Image.color = ButtonColor;
//    StartButtonGO.transform.localPosition = LocalPosition;
//    StartButtonGO.SetActive(false);
//}