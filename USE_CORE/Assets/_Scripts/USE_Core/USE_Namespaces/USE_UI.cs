using System;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using System.Collections;


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
            CircleGO.GetComponent<RectTransform>().anchoredPosition = circleLocation;// new Vector3(calibPointPixel.x, calibPointPixel.y, exptViewCam.nearClipPlane);
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


