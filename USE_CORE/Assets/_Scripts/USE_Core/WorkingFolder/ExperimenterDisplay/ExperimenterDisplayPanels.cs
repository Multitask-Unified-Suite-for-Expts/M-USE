using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ExperimenterDisplayPanels 
{   public class Panel
    {
        GameObject testCanvas = new GameObject("TestCanvas");

        // Update is called once per frame
        public void initPanel(GameObject experimenterInfo)
        {
            GameObject myGO;
            GameObject myText;
            Canvas myCanvas;
            Text text;
            RectTransform rectTransform;

            // Canvas
            myGO = new GameObject();
            myGO.name = "TestCanvas";
            myGO.AddComponent<Canvas>();
            myGO.transform.SetParent(experimenterInfo.transform);
            
            myCanvas = myGO.GetComponent<Canvas>();
            myCanvas.targetDisplay = 1;
            myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            myGO.AddComponent<CanvasScaler>();
            myGO.AddComponent<GraphicRaycaster>();

            // Text
            myText = new GameObject();
            myText.transform.parent = myGO.transform;
            myText.name = "text";

            text = myText.AddComponent<Text>();
            Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            text.font = ArialFont;
            text.text = "text";
            text.fontSize = 100;

            // Text position
            rectTransform = text.GetComponent<RectTransform>();
            rectTransform.localPosition = new Vector3(0, 0, 0);
            rectTransform.sizeDelta = new Vector2(400, 200);

        }

    }
    
    

    

}
