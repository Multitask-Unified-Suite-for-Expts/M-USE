using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate;

namespace USE_ExperimenterDisplay
{
    public class ExperimenterDisplayController : MonoBehaviour
    {
        //this gets instantiated and added as a component of the ExperimenterDisplay at the start of each task
        //all panel methods are now controlled and accessible through it
        private ControlLevel_Session_Template SessionLevel;
        private ControlLevel_Task_Template TaskLevel;
        private ControlLevel_Trial_Template TrialLevel;
        
        private List<ExperimenterDisplayPanel> TaskPanels;
        //any other fields necessary to control which display it is on, etc

        public void InitializeExperimenterDisplay(List<ExperimenterDisplayPanel> taskPanels = null)
        {
            //generate the main display, any housekeeping necessary to initialize it

            if (taskPanels != null)
                TaskPanels = taskPanels;
            else
                TaskPanels = DefaultPanels.DefaultPanelList();

            foreach (ExperimenterDisplayPanel panel in TaskPanels)
            {
                panel.InitializePanel();
                //make sure panel is a child of the ExperimenterDisplay object, any other housekeeping necessary
            }
        }

        public void Update()
        {
            //any housekeeping necessary that applies to all panels
            
            foreach (ExperimenterDisplayPanel panel in TaskPanels)
            {
                panel.CustomPanelUpdate();
            }
        }
    }

    public class ExperimenterDisplayPanel
    {
    //     private Rect Position; // + any other fields necessary for all panels
    //     protected Action CustomInitializationMethods;
    //     public Action CustomUpdateMethods;

    protected ControlLevel_Session_Template SessionLevel;
    protected ControlLevel_Task_Template TaskLevel;
    protected ControlLevel_Trial_Template TrialLevel;
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
            // defaultPanelList.Add(new HotkeyPanel());
            // defaultPanelList.Add(new PlayerViewPanel());
            //all other default panels
            return defaultPanelList;
        }

        // private class HotkeyPanel : ExperimenterDisplayPanel
        // {
        //     //add any custom fields needed
        //     public HotkeyPanel()
        //     {
        //         //fill in the { }s with whatever custom init and update methods are associated with this panel
        //         CustomInitializationMethods = () => { };
        //         CustomUpdateMethods = () => { };
        //     }
        // }
        //
        // private class PlayerViewPanel : ExperimenterDisplayPanel
        // {
        //     //add any custom fields needed
        //     public PlayerViewPanel()
        //     {
        //         //fill in the { }s with whatever custom init and update methods are associated with this panel
        //         CustomInitializationMethods = () => { };
        //         CustomUpdateMethods = () => { };
        //     }
        // }
    }
}
