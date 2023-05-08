using System;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using USE_Data;
using USE_ExperimentTemplate_Classes;

namespace USE_UI
{
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
        public USE_StartButton(Canvas parent, Vector3 localPos)
        {
            LocalPosition = localPos;
            StartButtonGO = new GameObject("StartButton");
            Image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            Image.rectTransform.anchoredPosition = Vector2.zero;
            Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            Image.color = ButtonColor;
            StartButtonGO.transform.localPosition = LocalPosition;
            StartButtonGO.SetActive(false);
        }
        public USE_StartButton(Canvas parent, float size)
        {
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


    public class USE_Instructions : MonoBehaviour
    {
        public static GameObject InstructionsGO;
        public static GameObject InstructionsButtonGO;
        public Button button;

        public static bool InstructionsOn;
        public static bool InstructionsButtonOn;

        [HideInInspector] public static EventCodeManager EventCodeManager;
        [HideInInspector] public static Dictionary<string, EventCode> SessionEventCodes;

        public Dictionary<string, string> TaskInstructionsDict = new Dictionary<string, string>()
        {
            { "ContinuousRecognition", "Each trial, objects are displayed and you must choose an object you haven't chosen in a previous trial." },
            { "EffortControl", "Choose a balloon to inflate. Inflate the balloon by clicking the required number of times. Pop the balloon for your reward!"},
            { "FlexLearning", "Select the correct object to earn your reward!"},
            { "MazeGame", "Find your way to the end of the Maze to earn your reward!" },
            { "THR", "Touch and hold the square for the correct duration to earn your reward!" },
            { "VisualSearch", "Select the correct object to earn your reward!" },
            { "WhatWhenWhere", "Select the objects in the correct sequence to earn your reward!" },
            { "WorkingMemory", "Find the target object among the distractors to earn your reward!" }
        };


        public USE_Instructions(GameObject instructionsPrefab, GameObject buttonPrefab, Canvas parent, string taskName, DataController frameData, EventCodeManager eventCodeManager, Dictionary<string, EventCode> sessionEventCodes)
        {
            SessionEventCodes = sessionEventCodes;
            EventCodeManager = eventCodeManager;

            InstructionsGO = Instantiate(instructionsPrefab, parent.transform);
            InstructionsGO.name = taskName + "_Instructions";
            InstructionsGO.GetComponentInChildren<Text>().text = TaskInstructionsDict[taskName];
            InstructionsGO.SetActive(false);
            InstructionsOn = false;

            InstructionsButtonGO = Instantiate(buttonPrefab, parent.transform);
            InstructionsButtonGO.name = taskName + "_InstructionsButton";
            button = InstructionsButtonGO.GetComponent<Button>();
            button.onClick.AddListener(ToggleInstructions);
            InstructionsButtonOn = true;
            EventCodeManager.SendCodeImmediate(SessionEventCodes["InstructionsButtonOn"]);

            frameData.AddDatum("InstructionsOn", () => InstructionsOn.ToString());
            frameData.AddDatum("InstructionsButtonOn", () => InstructionsButtonOn.ToString());
        }

        public static void ToggleInstructions() //Used by Subject/Player to toggle Instructions
        {
            InstructionsGO.SetActive(InstructionsGO.activeInHierarchy ? false : true);
            InstructionsOn = InstructionsGO.activeInHierarchy ? true : false;
            EventCodeManager.SendCodeImmediate(SessionEventCodes[InstructionsGO.activeInHierarchy ? "InstructionsOn" : "InstructionsOff"]);

        }

        public static void ToggleInstructionsButton() //Used by hotkeypanel to toggle Button
        {
            InstructionsButtonGO.SetActive(InstructionsButtonGO.activeInHierarchy ? false : true);

            //If deactivating instructions button, deactivate the intructions too
            if (!InstructionsButtonGO.activeInHierarchy && InstructionsGO.activeInHierarchy) 
                InstructionsGO.SetActive(false);

            InstructionsButtonOn = InstructionsButtonGO.activeInHierarchy ? true : false;
            EventCodeManager.SendCodeImmediate(SessionEventCodes[InstructionsButtonGO.activeInHierarchy ? "InstructionsButtonOn" : "InstructionsButtonOff"]);
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


