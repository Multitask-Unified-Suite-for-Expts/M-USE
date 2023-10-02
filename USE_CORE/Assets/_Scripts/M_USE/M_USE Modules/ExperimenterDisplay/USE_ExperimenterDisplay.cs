/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/






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
        protected GameObject ExperimenterDisplayGO;

        public List<ExperimenterDisplayPanel> TaskPanels;
        //any other fields necessary to control which display it is on, etc

        public void InitializeExperimenterDisplay(ControlLevel_Session_Template sessionLevel,GameObject expDisplayObject, List<ExperimenterDisplayPanel> taskPanels = null)
        {
            //generate the main display, any housekeeping necessary to initialize it
            SessionLevel = sessionLevel;
            ExperimenterDisplayGO = expDisplayObject;

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
            List<ExperimenterDisplayPanel> defaultPanelList = new List<ExperimenterDisplayPanel>
            {
                GameObject.Find("TrialInfoPanel").AddComponent<TrialInfoPanel>(),
                GameObject.Find("BlockInfoPanel").AddComponent<BlockInfoPanel>(),
                GameObject.Find("SessionInfoPanel").AddComponent<SessionInfoPanel>(),
                GameObject.Find("HotKeyPanel").AddComponent<HotKeyPanel>(),
                GameObject.Find("LogPanel").AddComponent<LogPanel>()
            };

            return defaultPanelList;
        }
    }
}
