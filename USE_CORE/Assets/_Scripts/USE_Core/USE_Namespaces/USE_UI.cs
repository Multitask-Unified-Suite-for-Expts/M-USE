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

namespace USE_UI{

	public class USE_Button{

		Button button;
		
		public Vector3 ButtonPosition;
		public Vector3 ButtonScale;
		public Color ButtonColor;
		public string ButtonText;

		public State SetActiveOnInitialization;
		public State SetInactiveOnTermination;

		public bool pressed = false;

		public USE_Button(Canvas canvas){

			ButtonPosition = new Vector3(0f, 0f, 0f);
			ButtonScale = new Vector3(1f, 1f, 1f);
			ButtonColor = new Color(0.28f, 0.56f, 0.88f);
			ButtonText = "";

			var newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
			newButton.transform.SetParent(canvas.transform, false);
			button = newButton.GetComponent<Button>();

		}

		public USE_Button(Vector3 position, Vector3 scale, Canvas canvas){

			ButtonPosition = position;
			ButtonScale = scale;
			ButtonColor = new Color(0.28f, 0.56f, 0.88f);
			ButtonText = "";

			var newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
			newButton.transform.SetParent(canvas.transform, false);
			button = newButton.GetComponent<Button>();

		}

		public USE_Button(Vector3 position, Vector3 scale, Canvas canvas, Color color){

			ButtonPosition = position;
			ButtonScale = scale;
			ButtonColor = color;
			ButtonText = "";

			var newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
			newButton.transform.SetParent(canvas.transform, false);
			button = newButton.GetComponent<Button>();

		}

		public USE_Button(Vector3 position, Vector3 scale, Canvas canvas, Color color, string text){

			ButtonPosition = position;
			ButtonScale = scale;
			ButtonColor = color;
			ButtonText = text;

			var newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
			newButton.transform.SetParent(canvas.transform, false);
			button = newButton.GetComponent<Button>();

		}

		public void defineButton(){

			button.gameObject.transform.position = ButtonPosition;
			button.transform.localScale = ButtonScale;
			ColorBlock cb = button.colors;
			cb.normalColor = ButtonColor;
			button.colors = cb;
			button.GetComponentInChildren<Text>().text = ButtonText;
			button.onClick.AddListener(EventOnClick);

		}

		private void EventOnClick(){
			pressed = true;
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
			ToggleVisibility(true);
		}

		private void InactivateOnStateTerm(object sender, EventArgs e)
		{
			ToggleVisibility(false);
		}

		public void ToggleVisibility(bool visibility)
		{
			 button.gameObject.SetActive(visibility);
		}

	}



}

