using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ExperimenterDisplay
{
    public class ExperimenterDisplayController : MonoBehaviour
    {
        //this gets instantiated and added as a component of the ExperimenterDisplay at the start of each task
        //all panel methods are now controlled and accessible through it
        private List<ExperimenterDisplayPanel> TaskPanels;
        //any other fields necessary to control which display it is on, etc
        public GameObject experimenterDisplayCanvas;

        public GameObject InitializeExperimenterDisplay(List<ExperimenterDisplayPanel> taskPanels = null)
        {
            //generate the main display, any housekeeping necessary to initialize it

            experimenterDisplayCanvas = Instantiate(Resources.Load<GameObject>("ExperimenterInfo"));
            experimenterDisplayCanvas.name = "ExperimenterDisplayCanvas";

            if (taskPanels != null)
                TaskPanels = taskPanels;
            else
                TaskPanels = DefaultPanels.DefaultPanelList();

            foreach (ExperimenterDisplayPanel panel in TaskPanels)
            {
                panel.InitializePanel();
                //make sure panel is a child of the ExperimenterDisplay object, any other housekeeping necessary
            }

            return experimenterDisplayCanvas;
        }

        public void Update()
        {
            //any housekeeping necessary that applies to all panels

            foreach (ExperimenterDisplayPanel panel in TaskPanels)
            {
                panel.CustomUpdateMethods();
            }
        }
    }

    public class ExperimenterDisplayPanel
    {
        private Rect Position; // + any other fields necessary for all panels
        protected Action CustomInitializationMethods;
        public Action CustomUpdateMethods;

        public void InitializePanel()
        {
            //commands to create panel, assign it to appropriate display, assign position - anything that is universal to all panels
            GameObject myGO;
            GameObject myText;
            Canvas myCanvas;
            Text text;
            RectTransform rectTransform;

            // Canvas
            myGO = new GameObject();
            myGO.name = "TestCanvas";
            myGO.AddComponent<Canvas>();

            myCanvas = myGO.GetComponent<Canvas>();
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

            CustomInitializationMethods();
        }

    }

    static class DefaultPanels
    {
        public static List<ExperimenterDisplayPanel> DefaultPanelList()
        {
            List<ExperimenterDisplayPanel> defaultPanelList = new List<ExperimenterDisplayPanel>();
            defaultPanelList.Add(new HotkeyPanel());
            defaultPanelList.Add(new PlayerViewPanel());
            //all other default panels
            return defaultPanelList;
        }

        private class HotkeyPanel : ExperimenterDisplayPanel
        {
            //add any custom fields needed
            public HotkeyPanel()
            {
                //whatever custom init and update methods are associated with this panel
                CustomInitializationMethods = () => { };
                CustomUpdateMethods = () => { };
            }
        }

        private class PlayerViewPanel : ExperimenterDisplayPanel
        {
            //add any custom fields needed
            public PlayerViewPanel()
            {
                //whatever custom init and update methods are associated with this panel
                CustomInitializationMethods = () => { };
                CustomUpdateMethods = () => { };
            }
        }
    }
}
