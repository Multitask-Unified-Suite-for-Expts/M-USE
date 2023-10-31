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



using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using TemporalOrderJudgement_Namespace;
using UnityEngine.UI;
using ConfigDynamicUI;
using TMPro;


public class TemporalOrderJudgement_TrialLevel : ControlLevel_Trial_Template
{
    public TemporalOrderJudgement_TrialDef CurrentTrial => GetCurrentTrialDef<TemporalOrderJudgement_TrialDef>();
    public TemporalOrderJudgement_TaskLevel CurrentTaskLevel => GetTaskLevel<TemporalOrderJudgement_TaskLevel>();
    public TemporalOrderJudgement_TaskDef CurrentTask => GetTaskDef<TemporalOrderJudgement_TaskDef>();

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration;

    public GameObject TOJ_CanvasGO;

    private GameObject StartButton;

    private GameObject VisualStimGO;
    private GameObject CrossGO;
    private GameObject ResponsePanelGO;
    private GameObject SimultaneousPanelGO; //Child of ResponsePanelGO;
    private GameObject AudioPanelGO; //Child of ResponsePanelGO;
    private GameObject VisualPanelGO; //Child of ResponsePanelGO;

    private string CorrectAnswer;

    private string ResponseString;

    private bool GotTrialCorrect;

    private bool VisualStimDisplayed;
    private bool AudioPlayed;

    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State FixationCross = new State("FixationCross");
        State Display = new State("Display");
        State PostDisplayDelay = new State("PostDisplayDelay");
        State Response = new State("Response");
        State Feedback = new State("Feedback");
        AddActiveStates(new List<State> { InitTrial, FixationCross, Display, PostDisplayDelay, Response, Feedback });

        Add_ControlLevel_InitializationMethod(() =>
        {
            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(TOJ_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }
        });

        //SetupTrial state --------------------------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddDefaultInitializationMethod(() =>
        {
            GotTrialCorrect = false;
            DetermineCorrectAnswer();
            CreateStims();
            LoadResponsePanel();
            LoadConfigUIVariables();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, Response);
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 30, TOJ_CanvasGO, true);

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), FixationCross);

        //FixatioCross state -------------------------------------------------------------------------------------------------------------------------------------------
        FixationCross.AddSpecificInitializationMethod(() => CrossGO.SetActive(true));
        FixationCross.AddTimer(() => CurrentTrial.CrossDuration, Display, () => CrossGO.SetActive(false));

        //Display state ------------------------------------------------------------------------------------------------------------------------------------------------
        Display.AddSpecificInitializationMethod(() =>
        {
            VisualStimDisplayed = false;
            AudioPlayed = false;
            //Start both coroutines at same time. 
            StartCoroutine(DisplayStimCoroutine());
            StartCoroutine(AudioCoroutine());
        });
        Display.SpecifyTermination(() => VisualStimDisplayed && AudioPlayed, PostDisplayDelay);

        //PostDisplayDelay State ----------------------------------------------------------------------------------------------------------------------------------------
        PostDisplayDelay.AddTimer(() => CurrentTrial.PostDisplayDelayDuration, Response, () => VisualStimGO.SetActive(false));

        //Response state ------------------------------------------------------------------------------------------------------------------------------------------------
        Response.AddSpecificInitializationMethod(() =>
        {
            ResponsePanelGO.SetActive(true);
            ResponseString = null;
        });
        Response.AddUpdateMethod(() =>
        {
            if (InputBroker.GetKeyDown(KeyCode.UpArrow))
                ResponseString = "Simultaneous";
            else if (InputBroker.GetKeyDown(KeyCode.LeftArrow))
                ResponseString = "Visual";
            else if (InputBroker.GetKeyDown(KeyCode.RightArrow))
                ResponseString = "Audio";
        });
        Response.SpecifyTermination(() => ResponseString != null, Feedback);
        Response.AddTimer(() => CurrentTrial.ResponseDuration, Feedback);

        //Feedback state ------------------------------------------------------------------------------------------------------------------------------------------------
        Feedback.AddSpecificInitializationMethod(() =>
        {
            if (ResponseString == null)
                return;

            if (ResponseString.ToLower() == CorrectAnswer.ToLower())
                GotTrialCorrect = true;

            AudioFBController.Play(GotTrialCorrect ? "Positive" : "Negative");

            switch(ResponseString)
            {
                case "Audio":
                    VisualPanelGO.SetActive(false);
                    SimultaneousPanelGO.SetActive(false);
                    AudioPanelGO.GetComponentInChildren<TextMeshProUGUI>().color = GotTrialCorrect ? Color.green : Color.red;
                    break;
                case "Visual":
                    SimultaneousPanelGO.SetActive(false);
                    AudioPanelGO.SetActive(false);
                    VisualPanelGO.GetComponentInChildren<TextMeshProUGUI>().color = GotTrialCorrect ? Color.green : Color.red;

                    break;
                case "Simultaneous":
                    AudioPanelGO.SetActive(false);
                    VisualPanelGO.SetActive(false);
                    SimultaneousPanelGO.GetComponentInChildren<TextMeshProUGUI>().color = GotTrialCorrect ? Color.green : Color.red;
                    break;
            }

            //If you want to change the UI's heading text:
            //ResponsePanelGO.transform.Find("ResponseHeading").gameObject.GetComponentInChildren<TextMeshProUGUI>().text = GotTrialCorrect ? "Correct!" : "Wrong!";
            //ResponsePanelGO.transform.Find("ResponseHeading").gameObject.GetComponentInChildren<TextMeshProUGUI>().color = GotTrialCorrect ? Color.green : Color.red;
        });
        Feedback.AddTimer(() => CurrentTrial.FeedbackDuration, FinishTrial, () => ResponsePanelGO.SetActive(false));
    }

    private void DetermineCorrectAnswer()
    {
        if (CurrentTrial.AudioStimOnsetDelay == CurrentTrial.VisualStimOnsetDelay)
            CorrectAnswer = "Simultaneous";
        else if (CurrentTrial.AudioStimOnsetDelay < CurrentTrial.VisualStimOnsetDelay)
            CorrectAnswer = "Audio";
        else
            CorrectAnswer = "Visual";
    }

    private IEnumerator DisplayStimCoroutine()
    {
        yield return new WaitForSeconds(CurrentTrial.VisualStimOnsetDelay);
        VisualStimGO.SetActive(true);
        VisualStimDisplayed = true;
    }

    private IEnumerator AudioCoroutine()
    {
        yield return new WaitForSeconds(CurrentTrial.AudioStimOnsetDelay);
        AudioFBController.Play("Hammer");
        AudioPlayed = true;
    }

    private void CreateStims()
    {
        if (VisualStimGO != null)
            Destroy(VisualStimGO);

        //Create visual stim:
        VisualStimGO = new GameObject("VisualStim");
        VisualStimGO.SetActive(false);
        VisualStimGO.transform.parent = TOJ_CanvasGO.transform;
        VisualStimGO.transform.localPosition = CurrentTrial.VisualStimPosition; //set to pos specified in trial def
        VisualStimGO.transform.localScale = CurrentTrial.VisualStimSize; //set to size specified in trial def
        Image stimImage = VisualStimGO.AddComponent<Image>();
        stimImage.sprite = Resources.Load<Sprite>(CurrentTrial.VisualStimIdentity); //Load the stim's image from resources!
        if (CurrentTrial.VisualStimRandomColor)
            stimImage.color = GetRandomColor();

        //Create Cross stim:
        CrossGO = new GameObject("Cross");
        CrossGO.SetActive(false);
        CrossGO.transform.parent = TOJ_CanvasGO.transform;
        CrossGO.transform.localPosition = CurrentTrial.CrossPosition; //set to pos specified in trial def
        CrossGO.transform.localScale = CurrentTrial.CrossSize; //set to size specified in trial def
        Image crossImage = CrossGO.AddComponent<Image>();
        crossImage.sprite = Resources.Load<Sprite>(CurrentTrial.CrossIdentity); //Load the cross image from resources!
        if (CurrentTrial.CrossRandomColor)
            crossImage.color = GetRandomColor();
    }

    private void LoadResponsePanel()
    {
        if (ResponsePanelGO != null)
            Destroy(ResponsePanelGO);

        ResponsePanelGO = Instantiate(Resources.Load<GameObject>("ResponsePanel"));
        ResponsePanelGO.name = "ResponsePanel";
        ResponsePanelGO.transform.SetParent(TOJ_CanvasGO.transform);
        ResponsePanelGO.transform.localScale = Vector3.one;
        ResponsePanelGO.transform.localPosition = Vector3.zero;
        ResponsePanelGO.SetActive(false);

        //get references to 3 children:
        SimultaneousPanelGO = ResponsePanelGO.transform.Find("Simultaneous").gameObject;
        AudioPanelGO = ResponsePanelGO.transform.Find("Audio").gameObject;
        VisualPanelGO = ResponsePanelGO.transform.Find("Visual").gameObject;
    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
    }
    
}
