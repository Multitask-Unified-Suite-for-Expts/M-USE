using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using TriLib;
using USE_States;
using Object = UnityEngine.Object;
using UnityEngine.Events;

namespace USE_UI
{
	public class USE_StartButton
	{
		public GameObject StartButtonGO;
		public float ButtonSize = 120f;
		public Color ButtonColor = new Color(0, 0, 128, 255);
		public Image Image;

        public State SetActiveOnInitialization;
        public State SetInactiveOnTermination;

        public USE_StartButton(Canvas parent)
		{
			StartButtonGO = new GameObject("StartButton");
			Image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            Image.rectTransform.anchoredPosition = Vector2.zero;
            Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
			Image.color = ButtonColor;
            StartButtonGO.SetActive(false);
        }
        public USE_StartButton(Canvas parent, Color color)
        {
			ButtonColor = color;
			StartButtonGO = new GameObject("StartButton");
            Image image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            image.color = ButtonColor;
            StartButtonGO.SetActive(false);
        }
        public USE_StartButton(Canvas parent, float size)
        {
            ButtonSize = size;
			StartButtonGO = new GameObject("StartButton");
            Image image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            image.color = ButtonColor;
            StartButtonGO.SetActive(false);
        }
        public USE_StartButton(Canvas parent, Color color, float size)
        {
            ButtonColor = color;
            ButtonSize = size;
            StartButtonGO = new GameObject("StartButton");
            Image image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            image.color = ButtonColor;
            StartButtonGO.SetActive(false);
        }

		public void SetButtonColor(Color color)
		{
			ButtonColor = color;
			Image.color = ButtonColor;
		}

		public void SetButtonSize(float size)
		{
			ButtonSize = size;
			Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
		}

    }

    //public class USE_Button
    //{
    //	Button button;

    //	public Vector3 ButtonPosition;
    //	public Vector3 ButtonScale;
    //	public Color ButtonColor;
    //	public string ButtonText;

    //	public State SetActiveOnInitialization;
    //	public State SetInactiveOnTermination;

    //	public bool pressed = false;

    //	public USE_Button(Canvas canvas)
    //	{
    //		//ButtonPosition = new Vector3(0f, 0f, 0f);
    //		//ButtonScale = new Vector3(1f, 1f, 1f);
    //		//ButtonColor = new Color(0.28f, 0.56f, 0.88f);
    //		//ButtonText = "";

    //		GameObject newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
    //		newButton.transform.SetParent(canvas.transform, false);
    //		button = newButton.GetComponent<Button>();
    //	}

    //	public USE_Button(Vector3 position, Vector3 scale, Canvas canvas)
    //	{
    //           GameObject newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
    //		newButton.transform.SetParent(canvas.transform, false);
    //		button = newButton.GetComponent<Button>();

    //		ButtonPosition = position;
    //           ButtonScale = scale;
    //		ButtonColor = new Color(0.28f, 0.56f, 0.88f);
    //		ButtonText = "";
    //       }

    //       public USE_Button(Vector3 position, Vector3 scale, Canvas canvas, Color color)
    //	{
    //		//ButtonPosition = position;
    //		//ButtonScale = scale;
    //		//ButtonColor = color;
    //		//ButtonText = "";

    //           GameObject newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
    //		newButton.transform.SetParent(canvas.transform, false);
    //		button = newButton.GetComponent<Button>();

    //           var colors = button.colors;
    //           colors.normalColor = color;

    //		newButton.GetComponent<Image>().color = color;
    //	}

    //	public USE_Button(Vector3 position, Vector3 scale, Canvas canvas, Color color, string text)
    //	{
    //		//ButtonPosition = position;
    //		//ButtonScale = scale;
    //		//ButtonColor = color;
    //		//ButtonText = text;

    //		GameObject newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
    //		newButton.transform.SetParent(canvas.transform, false);
    //		button = newButton.GetComponent<Button>();

    //           var colors = button.colors;
    //           colors.normalColor = color;

    //           button.GetComponentInChildren<Text>().text = text;
    //		//button.GetComponentInChildren<Text>().color = textColor; //add this for text color change. 

    //		newButton.GetComponent<Image>().color = color;

    //	}

    //	public USE_Button AddEventListener(UnityAction functionName)
    //	{
    //		button.onClick.AddListener(functionName);
    //		return this;
    //	}

    //	//      public void defineButton()
    //	//{
    //	//	button.gameObject.transform.position = ButtonPosition;
    //	//	button.transform.localScale = ButtonScale;
    //	//	ColorBlock cb = button.colors;
    //	//	cb.normalColor = ButtonColor;
    //	//	button.colors = cb;
    //	//	button.GetComponentInChildren<Text>().text = ButtonText;
    //	//	button.onClick.AddListener(EventOnClick);
    //	//}


    //	//private void EventOnClick()
    //	//{
    //	//	pressed = true;
    //	//	Debug.Log("CLICKED!");
    //	//}

    //	public void SetVisibilityOnOffStates(State setActiveOnInit = null, State setInactiveOnTerm = null)
    //	{
    //		if (setActiveOnInit != null)
    //		{
    //			SetActiveOnInitialization = setActiveOnInit;
    //			SetActiveOnInitialization.StateInitializationFinished += ActivateOnStateInit;
    //		}
    //		if (setInactiveOnTerm != null)
    //		{
    //			SetInactiveOnTermination = setInactiveOnTerm;
    //			SetInactiveOnTermination.StateTerminationFinished += InactivateOnStateTerm;
    //		}
    //	}

    //	private void ActivateOnStateInit(object sender, EventArgs e)
    //	{
    //		ToggleVisibility(true);
    //	}

    //	private void InactivateOnStateTerm(object sender, EventArgs e)
    //	{
    //		ToggleVisibility(false);
    //	}

    //	public void ToggleVisibility(bool visibility)
    //	{
    //		 button.gameObject.SetActive(visibility);
    //	}

    //}



}

