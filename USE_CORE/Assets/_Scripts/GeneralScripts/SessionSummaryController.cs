using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Random = UnityEngine.Random;


public class SessionSummaryController : MonoBehaviour
{
    [HideInInspector] public SessionBuilder SessionBuilder;
    [HideInInspector] public bool EndSessionButtonClicked;


    //Set in inspector:
    public Transform GridParent;
    public Transform Graph_GridParent; //can prob remove later if not using graphs anymore
    public GameObject TaskResults;
    public Transform TaskResults_GridParent;
    public GameObject BackArrow;
    public GameObject EndSessionButton;
    public TextMeshProUGUI HeaderText;



    private void Start()
    {
        if(BackArrow != null)
            BackArrow.AddComponent<Button>().onClick.AddListener(OnBackArrowClicked);

        if (EndSessionButton != null)
            EndSessionButton.AddComponent<Button>().onClick.AddListener(OnEndSessionButtonClicked);
    }

    private void OnEndSessionButtonClicked()
    {
        EndSessionButtonClicked = true;
    }

    private void OnBackArrowClicked()
    {
        if (TaskResults != null)
            TaskResults.SetActive(false);

        HeaderText.text = "Session Summary";
    }

    private void ResetTaskResultsGrid()
    {
        foreach (Transform child in TaskResults_GridParent)
            Destroy(child.gameObject);
    }

    private void OnTaskIconClicked()
    {
        ResetTaskResultsGrid(); //Destroy any existing data before instantiating new data

        TaskObject task = SessionBuilder.GetTaskByName(EventSystem.current.currentSelectedGameObject.transform.name);

        if(task == null)
        {
            Debug.LogError("TASK IS NULL");
            return;
        }

        HeaderText.text = task.SplitTaskName;

        if (task.TaskResultsData != null)
        {
            int count = 0;
            foreach(DictionaryEntry entry in task.TaskResultsData)
            {
                GameObject go = Instantiate(Resources.Load<GameObject>("TaskResults_GridItem"));
                go.name = entry.Key.ToString();
                go.transform.SetParent(TaskResults_GridParent.transform);
                go.transform.localScale = Vector3.one;
                go.transform.localPosition = Vector3.zero;
                go.transform.rotation = Quaternion.Euler(5f, 0f, 0f); //adjust rotation

                TextMeshProUGUI itemText = go.GetComponentInChildren<TextMeshProUGUI>();
                itemText.text = $"{entry.Key}:  <color=#0681B5><b>{entry.Value}</b></color>";

                count++;
            }
        }
        else
            Debug.LogWarning("TASK SUMMARY DATA IS NULL");

        TaskResults.SetActive(true);
    }

    public void CreateTaskSummaryGridItems(List<TaskObject> tasks)
    {
        try
        {
            foreach (TaskObject task in tasks)
            {
                if (task.TrialsCompleted < 1)
                    break;

                GameObject go = Instantiate(Resources.Load<GameObject>("SessionSummary_GridItem"));
                go.name = task.TaskName;
                go.transform.SetParent(GridParent);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.rotation = Quaternion.Euler(5f, 0f, 0f); //adjust rotation

                Color32 taskColor = task.TaskColor;

                Transform border = go.transform.Find("Icon");
                border.GetComponent<Image>().color = taskColor;

                go.AddComponent<Button>().onClick.AddListener(OnTaskIconClicked);

                TextMeshProUGUI textComponent = border.Find("Icon_Front").transform.Find("Icon_Text").GetComponent<TextMeshProUGUI>();
                textComponent.text = task.TaskInitials;
                textComponent.color = taskColor;

                TextMeshProUGUI taskNameText = go.transform.Find("TaskName").GetComponent<TextMeshProUGUI>();
                taskNameText.color = taskColor;
                taskNameText.text = task.SplitTaskName;

                go.transform.Find("NumTrials").GetComponent<TextMeshProUGUI>().text = $"{task.TrialsCompleted} Trials";
                go.transform.Find("Duration").GetComponent<TextMeshProUGUI>().text = task.TaskDurationInMin == 1 ? $"{task.TaskDurationInMin:0} Minute" : $"{task.TaskDurationInMin:0} Minutes";
            }
        }
        catch(Exception e)
        {
            Debug.LogError("FAILED CREATING GRID ITEMS! ERROR: " + e.Message);
        }
    }


}

