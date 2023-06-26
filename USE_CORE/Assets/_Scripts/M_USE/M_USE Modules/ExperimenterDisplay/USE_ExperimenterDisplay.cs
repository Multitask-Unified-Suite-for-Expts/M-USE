using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Session;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;

namespace USE_ExperimenterDisplay
{
    public class ExperimenterDisplayController : MonoBehaviour
    {
        //this gets instantiated and added as a component of the ExperimenterDisplay at the start of each task
        //all panel methods are now controlled and accessible through it
        protected ControlLevel_Session_Template SessionLevel;
        protected ControlLevel_Task_Template TaskLevel;
        protected ControlLevel_Trial_Template TrialLevel;
        protected GameObject ExperimenterDisplayGameObject;

        public List<ExperimenterDisplayPanel> TaskPanels;
        //any other fields necessary to control which display it is on, etc

        public void InitializeExperimenterDisplay(ControlLevel_Session_Template sessionLevel,GameObject expDisplayObject, List<ExperimenterDisplayPanel> taskPanels = null)
        {
            //generate the main display, any housekeeping necessary to initialize it
            SessionLevel = sessionLevel;
            ExperimenterDisplayGameObject = expDisplayObject;

            if (taskPanels != null)
                TaskPanels = taskPanels;
            else
                TaskPanels = DefaultPanels.DefaultPanelList();

            foreach (ExperimenterDisplayPanel panel in TaskPanels)
            {
                panel.InitializePanel();
                panel.SessionLevel = SessionLevel;
                //make sure panel is a child of the ExperimenterDisplay object, any other housekeeping necessary
            }

            //MoveToLayer(ExperimenterDisplayGameObject.transform, SessionLevel.ExperimenterDisplayLayer);

        }

        public void Update()
        {
            //any housekeeping necessary that applies to all panels

            foreach (ExperimenterDisplayPanel panel in TaskPanels)
            {
                panel.CustomPanelUpdate();
            }
        }

        public void ResetTask(ControlLevel_Task_Template taskLevel, ControlLevel_Trial_Template trialLevel)
        {

            TaskLevel = taskLevel;
            TrialLevel = trialLevel;
            foreach (ExperimenterDisplayPanel panel in TaskPanels)
            {
                panel.TaskLevel = taskLevel;
                panel.TrialLevel = trialLevel;
            }

        }

        private void MoveToLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
                MoveToLayer(child, layer);
        }
    }

    public class ExperimenterDisplayPanel : MonoBehaviour
    {
        public ControlLevel_Session_Template SessionLevel;
        public ControlLevel_Task_Template TaskLevel;
        public ControlLevel_Trial_Template TrialLevel;
        protected GameObject PanelObject;

        public void InitializePanel()
        {
            //commands to create panel, assign it to appropriate display, assign position - anything that is universal to all panels

            CustomPanelInitialization();
        }

        public virtual void CustomPanelInitialization()
        {

        }

        public virtual void CustomPanelUpdate()
        {

        }

    }

    static class DefaultPanels
    {
        public static List<ExperimenterDisplayPanel> DefaultPanelList()
        {
            List<ExperimenterDisplayPanel> defaultPanelList = new List<ExperimenterDisplayPanel>();
            defaultPanelList.Add(GameObject.Find("TrialInfoPanel").AddComponent<TrialInfoPanel>()); 
            defaultPanelList.Add(GameObject.Find("BlockInfoPanel").AddComponent<BlockInfoPanel>()); 
            defaultPanelList.Add(GameObject.Find("SessionInfoPanel").AddComponent<SessionInfoPanel>()); 
            defaultPanelList.Add(GameObject.Find("HotKeyPanel").AddComponent<HotKeyPanel>()); 
            defaultPanelList.Add(GameObject.Find("LogPanel").AddComponent<LogPanel>());

            return defaultPanelList;
        }
    }
}
