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
using UnityEngine.Rendering.PostProcessing;
using System.Collections;
using USE_UI;

namespace USE_UI
{
	public class USE_StartButton
	{
		public GameObject StartButtonGO;
		public float ButtonSize = 120f;
		public Color ButtonColor = new Color(0, 0, 128, 255);
		public Image Image;
        public Vector3 LocalPosition = new Vector3(0, 0, 0);

        public State SetActiveOnInitialization;
        public State SetInactiveOnTermination;

        //--------------------------Constructors----------------------------
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
            Image image = StartButtonGO.AddComponent<Image>();
            StartButtonGO.transform.SetParent(parent.transform, false);
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            image.color = ButtonColor;
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
			Image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
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


        public IEnumerator GratedStartButtonFlash(Texture2D newTexture, float duration, bool deactivateAfter)
        {
            if (!StartButtonGO.activeInHierarchy)
                StartButtonGO.SetActive(true);

            Color32 originalColor = Image.color;
            Sprite originalSprite = Image.sprite;

            Image.color = new Color32(224, 78, 92, 255);
            Image.sprite = Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height), Vector2.one / 2f);
            yield return new WaitForSeconds(duration);
            Image.color = originalColor;
            Image.sprite = originalSprite;
            if (deactivateAfter)
                StartButtonGO.SetActive(false);
        }

    }

}





//Additional Constructors:

//public USE_StartButton(Canvas parent, Color color)
//{
//    ButtonColor = color;
//    StartButtonGO = new GameObject("StartButton");
//    Image image = StartButtonGO.AddComponent<Image>();
//    StartButtonGO.transform.SetParent(parent.transform, false);
//    image.rectTransform.anchoredPosition = Vector2.zero;
//    image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
//    image.color = ButtonColor;
//    StartButtonGO.SetActive(false);
//}

//public USE_StartButton(Canvas parent, Color color, float size)
//{
//    ButtonColor = color;
//    ButtonSize = size;
//    StartButtonGO = new GameObject("StartButton");
//    Image image = StartButtonGO.AddComponent<Image>();
//    StartButtonGO.transform.SetParent(parent.transform, false);
//    image.rectTransform.anchoredPosition = Vector2.zero;
//    image.rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);
//    image.color = ButtonColor;
//    StartButtonGO.SetActive(false);
//}

