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

namespace USE_UI
{
    public class HumanStartPanel : MonoBehaviour
    {
        public GameObject HumanStartPanelGO;

        public GameObject StartButtonGO;

        public GameObject InstructionsButtonGO;
        public GameObject InstructionsGO;
        public GameObject TitleTextGO;
        public GameObject HumanBackgroundGO;
        public GameObject BackgroundPanelGO;

        public GameObject HumanStartPanelPrefab; //Set to Session In inspector, then passed down

        public bool HumanPanelOn;
        public bool InstructionsOn;

        public Dictionary<string, string> TaskInstructionsDict = new Dictionary<string, string>()
        {
            { "ContinuousRecognition", "Each trial, objects are displayed and you must choose an object you haven't chosen in a previous trial." },
            { "EffortControl", "Choose a balloon to inflate. Inflate the balloon by clicking the required number of times. Pop the balloon for your reward!"},
            { "FlexLearning", "Learn the visual feature that provides the most reward!"},
            { "MazeGame", "Find your way to the end of the Maze to earn your reward!" },
            { "THR", "Touch and hold the square for the correct duration to earn your reward!" },
            { "VisualSearch", "Find the targeted object to earn your reward!" },
            { "WhatWhenWhere", "Select the objects in the correct sequence to earn your reward!" },
            { "WorkingMemory", "Remember and identify the target object to earn your reward!" }
        };
        public Dictionary<string, string> TaskNamesDict = new Dictionary<string, string>()
        {
            { "ContinuousRecognition", "Continuous Recognition" },
            { "THR", "Touch Hold Release" },
            { "EffortControl", "Effort Control" },
            { "FlexLearning", "Flexible Learning" },
            { "MazeGame", "Maze Game" },
            { "VisualSearch", "Visual Search" },
            { "WhatWhenWhere", "What When Where" },
            { "WorkingMemory", "Working Memory" },

        };

        public string TaskName;

        public State SetActiveOnInitialization;
        public State SetInactiveOnTermination;

        [HideInInspector] public static EventCodeManager EventCodeManager;
        [HideInInspector] public static Dictionary<string, EventCode> SessionEventCodes;


        public void SetupDataAndCodes(DataController frameData, EventCodeManager eventCodeManager, Dictionary<string, EventCode> sessionEventCodes)
        {
            SessionEventCodes = sessionEventCodes;
            EventCodeManager = eventCodeManager;

            frameData.AddDatum("HumanPanelOn", () => HumanPanelOn.ToString());
            frameData.AddDatum("InstructionsOn", () => InstructionsOn.ToString());
        }


        public void CreateHumanStartPanel(Canvas parent, string taskName)
        {
            HumanStartPanelGO = Instantiate(HumanStartPanelPrefab);
            HumanStartPanelGO.name = taskName + "_HumanPanel";
            HumanStartPanelGO.transform.SetParent(parent.transform, false);

            TitleTextGO = HumanStartPanelGO.transform.Find("TitleText").gameObject;
            TaskName = TaskNamesDict[taskName];
            TitleTextGO.GetComponent<TextMeshProUGUI>().text = TaskName;

            StartButtonGO = HumanStartPanelGO.transform.Find("StartButton").gameObject;

            HumanBackgroundGO = HumanStartPanelGO.transform.Find("HumanBackground").gameObject;
            BackgroundPanelGO = HumanStartPanelGO.transform.Find("BackgroundPanel").gameObject;

            InstructionsButtonGO = HumanStartPanelGO.transform.Find("InstructionsButton").gameObject;
            Button button = InstructionsButtonGO.AddComponent<Button>();
            button.onClick.AddListener(ToggleInstructions);

            InstructionsGO = HumanStartPanelGO.transform.Find("Instructions").gameObject;
            InstructionsGO.GetComponentInChildren<Text>().text = TaskInstructionsDict[taskName];
            InstructionsGO.SetActive(false);
            InstructionsOn = false;

            HumanStartPanelGO.SetActive(false);
            HumanPanelOn = false;
        }


        public void ToggleInstructions() //Used by Subject/Player to toggle Instructions
        {
            InstructionsGO.SetActive(InstructionsGO.activeInHierarchy ? false : true);
            InstructionsOn = InstructionsGO.activeInHierarchy ? true : false;
            EventCodeManager.SendCodeImmediate(SessionEventCodes[InstructionsGO.activeInHierarchy ? "InstructionsOn" : "InstructionsOff"]);

        }

        public void AdjustPanelBasedOnTrialNum(int trialCountInBlock)
        {
            if (trialCountInBlock == 0)
            {
                if (!HumanBackgroundGO.activeInHierarchy)
                    HumanBackgroundGO.SetActive(true);

                TitleTextGO.GetComponent<TextMeshProUGUI>().text = TaskName;
                if (!TitleTextGO.activeInHierarchy)
                    TitleTextGO.SetActive(true);

                if (BackgroundPanelGO.activeSelf)
                    BackgroundPanelGO.SetActive(false);
            }
            else if (trialCountInBlock > 0)
            {
                BackgroundPanelGO.SetActive(true);
                HumanBackgroundGO.SetActive(false);
                TitleTextGO.GetComponent<TextMeshProUGUI>().text = "Trial " + (trialCountInBlock + 1);
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
                SetInactiveOnTermination.StateTerminationFinished += InactivateOnStateTerm;
            }
        }

        private void ActivateOnStateInit(object sender, EventArgs e)
        {
            HumanStartPanelGO.SetActive(true);
            HumanPanelOn = true;
            EventCodeManager.SendCodeImmediate(SessionEventCodes["HumanStartPanelOn"]);
        }

        private void InactivateOnStateTerm(object sender, EventArgs e)
        {
            HumanStartPanelGO.SetActive(false);
            HumanPanelOn = false;
            EventCodeManager.SendCodeImmediate(SessionEventCodes["HumanStartPanelOff"]);
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

            CircleGO.AddComponent<CanvasRenderer>();
            CircleGO.transform.SetParent(parent.transform, false);
            CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().fill = true;
            CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().thickness = 2f;
            CircleGO.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
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