using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class SessionBuilder : MonoBehaviour
{
    public GameObject Queue_GridParent;
    private RectTransform Queue_GridParent_Rect;
    public GameObject AddTasks_GridParent;

    private List<QueueItem> QueueItems = new List<QueueItem>();
    private List<TaskObject> Tasks = new List<TaskObject>();

    AudioSource buttonAudioSource;
    AudioClip buttonClickedAudioClip;

    GameObject ExpDisplay_TaskOrder_GridParent;
    GameObject RunButton;

    [HideInInspector] public bool RunButtonClicked;

    private const string AudioClipName = "GridItemAudio";
    private const string RunButtonName = "RunButton";

    private Color32 InactiveTaskIconBorderColor = new Color32(65, 65, 65, 255);
    private Color32 InactiveTaskIconTextColor = new Color32(134, 138, 147, 255);

    private Dictionary<string, Color32> TaskColors;


    private void Awake()
    {
        Queue_GridParent_Rect = Queue_GridParent.GetComponent<RectTransform>();
        if (Queue_GridParent_Rect == null)
            Debug.LogError("QUEUE GRID PARENT RECT IS NULL");

        AddTaskColors();
    }

    private void AddTaskColors()
    {
        TaskColors = new Dictionary<string, Color32>()
        {
            {"AntiSaccade", new Color32(47, 175, 166, 255) },
            {"ContinuousRecognition", new Color32(246, 126, 125, 255) },
            {"EffortControl", new Color32(0, 107, 166, 255) },
            {"FlexLearning", new Color32(166, 111, 88, 255) },
            {"FruitRunner", new Color32(248, 152, 29, 255) },
            {"MazeGame", new Color32(164, 153, 102, 255) },
            {"SustainedAttention", new Color32(136, 137, 237, 255) },
            {"THR", new Color32(158, 84, 97, 255) },
            {"VisualSearch", new Color32(27, 185, 20, 255) },
            {"WhatWhenWhere", new Color32(199, 199, 166, 255) },
            {"WorkingMemory", new Color32(5, 190, 251, 255) },
        };
    }

    public Color32 GetTaskColor(string taskName)
    {
        if(TaskColors == null)
            Debug.LogError("TASK COLORS ARE NULL");


        if(TaskColors.ContainsKey(taskName))
        {
            return TaskColors[taskName];
        }
        else
        {
            Debug.LogWarning("TASK COLOR DOESNT EXIST FOR " + taskName + ", SO ITS BEING GIVEN A RANDOM COLOR");
            return new Color32((byte)Random.value, (byte)Random.value, (byte)Random.value, 1);
        }

    }


    public void SetTaskData(string taskName, int trialsCompleted, float taskDuration, OrderedDictionary taskSummaryData)
    {
        TaskObject task = Tasks.Find(t => t.TaskName == taskName);
        if (task != null)
        {
            task.TrialsCompleted += trialsCompleted; //have to increment this for when there's multiple configs for same task
            task.TaskResultsData = taskSummaryData;
            task.TaskDurationInMin = Mathf.CeilToInt(taskDuration / 60);
        }
        else
            Debug.LogError("COULDNT FIND A TASK WITH NAME " + taskName);
    }

    public string GetTaskNameFromConfigName(string configName)
    {
        if(QueueItems != null)
        {
            foreach(QueueItem task in QueueItems)
            {
                if (task.ConfigName == configName)
                    return task.TaskName;
            }
        }
        return null;
    }


    public List<QueueItem> GetQueueItems()
    {
        return QueueItems;
    }

    public QueueItem GetItemInQueue(int index)
    {
        if (QueueItems == null)
        {
            Debug.LogError("QUEUE ITEMS IS NULL");
            return null;
        }
        return QueueItems[index];
    }

    public int GetQueueLength()
    {
        if (QueueItems == null)
            Debug.LogError("QUEUE ITEMS IS NULL");
        

        return QueueItems.Count;
    }

    private void UpdateQueue()
    {
        for (int i = 0; i < QueueItems.Count; i++)
        {
            Transform current = QueueItems[i].gameObject.transform;
            current.SetSiblingIndex(i);

            QueueItem queueItem = current.gameObject.GetComponent<QueueItem>();
            if(queueItem == null)
            {
                Debug.LogError("QUEUE ITEM COMPONENT IS NULL");
                return;
            }
            queueItem.SpotInQueue = i + 1;
        }

        //UPDATE THE NUMBER FOR EACH ITEM IN THE QUEUE:
        foreach (QueueItem item in QueueItems)
        {
            item.Index_Text.text = item.SpotInQueue.ToString();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(Queue_GridParent_Rect);
    }

    public void OnArrowPressed(bool moveUp)
    {
        GameObject selectedGO = EventSystem.current.currentSelectedGameObject;
        GameObject mainParent = selectedGO.transform.parent.transform.parent.gameObject;

        QueueItem queueItem = mainParent.GetComponent<QueueItem>();
        if (queueItem != null)
        {
            int index = queueItem.SpotInQueue - 1;
            int newIndex = moveUp ? index - 1 : index + 1;
            if ((moveUp && index == 0) || (!moveUp && index == QueueItems.Count - 1))
                return;

            QueueItems.RemoveAt(index);
            QueueItems.Insert(newIndex, queueItem);
            UpdateQueue();
        }
        else
        {
            Debug.LogError("ARROW PRESSED BUT QUEUE ITEM COMPONENT IS NULL");
            return;
        }
        PlayAudio(buttonClickedAudioClip);
    }

    public void OnMoveUpArrowPressed()
    {
        OnArrowPressed(true);
    }

    public void OnMoveDownArrowPressed()
    {
        OnArrowPressed(false);
    }


    public void ManualStart(GameObject taskOrderGridParent)
    {
        try
        {
            buttonAudioSource = gameObject.AddComponent<AudioSource>();
            buttonClickedAudioClip = Resources.Load<AudioClip>(AudioClipName);
            RunButton = transform.Find(RunButtonName).gameObject;
            RunButton.AddComponent<Button>().onClick.AddListener(OnDoneButtonClicked);

            ExpDisplay_TaskOrder_GridParent = taskOrderGridParent;
        }
        catch(Exception e)
        {
            Debug.LogError("FAILED FINDING GAMEOBJECTS DURING START METHOD OF SESSIONBUILDER " + " | Error: " + e.Message);
        }

        AddTasks();

        gameObject.SetActive(true);

    }

    private void OnDoneButtonClicked()
    {
        PlayAudio(buttonClickedAudioClip);
        RunButtonClicked = true;
    }

    private bool TaskListContainsTaskName(string taskName)
    {
        if(Tasks != null && Tasks.Count > 0)
        {
            foreach(var task in Tasks)
            {
                if (task.TaskName == taskName)
                    return true;
            }
        }
        return false;
    }

    public List<TaskObject> GetTasks()
    {
        if (Tasks != null && Tasks.Count > 0)
            return Tasks;
        return null;
    }
    public TaskObject GetTaskByName(string taskName)
    {
        if (Tasks != null && Tasks.Count > 0)
        {
            foreach (var task in Tasks)
            {
                if (task.TaskName == taskName)
                    return task;
            }
        }
        return null;
    }


    public void AddTasks()
    {
        try
        {
            if (Session.SessionDef.TaskMappings == null)
            {
                Debug.LogError("TASK MAPPINGS IS NULL!");
                return;
            }

            foreach (DictionaryEntry task in Session.SessionDef.TaskMappings)
            {
                string configName = (string)task.Key;
                string taskName = (string)task.Value;

                string initials = GetInitials(taskName);

                string splitName = GetSplitName(taskName);

                TaskObject taskObject;

                //Make sure the task list isn't null:
                Tasks ??= new List<TaskObject>();

                //if not a task already, create a new one, otherwise just add the config name to the existing tasks's config list:
                if(!TaskListContainsTaskName(taskName))
                {
                    //create grid gameobject for the task:
                    GameObject item = Instantiate(Resources.Load<GameObject>("Tasks_GridItem"));
                    item.name = taskName;
                    item.transform.SetParent(AddTasks_GridParent.transform);
                    item.transform.localScale = Vector3.one;
                    item.transform.localPosition = Vector3.one;
                    item.transform.eulerAngles = Camera.main.transform.eulerAngles;

                    TextMeshProUGUI textComponent = item.transform.Find("Icon").transform.Find("Border").transform.Find("Front").transform.Find("Icon_Text").GetComponent<TextMeshProUGUI>();
                    textComponent.text = initials;

                    Color32 taskColor = GetTaskColor(taskName);
                    textComponent.color = taskColor;

                    item.transform.Find("Icon").transform.Find("Border").GetComponent<Image>().color = taskColor;

                    item.transform.Find("TaskName_Text").GetComponent<TextMeshProUGUI>().text = splitName;

                    item.SetActive(true);

                    taskObject = item.AddComponent<TaskObject>();
                    taskObject.SetupObject(taskName, splitName, initials);
                    taskObject.SetDropdown(item.transform.Find("Dropdown").GetComponent<TMP_Dropdown>());
                    taskObject.TaskColor = taskColor;

                    item.transform.Find("Dropdown").transform.Find("AddButton").gameObject.AddComponent<Button>().onClick.AddListener(OnAddToQueueButtonPress);

                    //create dropdown item for the config name:
                    taskObject.AddOption(configName);

                    Tasks.Add(taskObject);
                }
                else
                {
                    //ADD CONFIG TO EXISTING DROPDOWN LIST:
                    taskObject = GetTaskByName(taskName);
                    if (taskObject != null)
                        taskObject.AddOption(configName);
                }


                //ADD CONFIG TO QUEUE SINCE WHATEVER IN TASK MAPPINGS SHOULD BE INITIALLY QUEUE'D UP:
                AddToQueue(taskObject, configName);
            }

        }
        catch(Exception e)
        {
            Debug.LogError("ERROR WHILE ADDING TASKS IN THE SESSIONBUILDER | ERROR: " + e.Message);
        }


    }


    private void OnAddToQueueButtonPress()
    {
        PlayAudio(buttonClickedAudioClip);

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        Transform parent = selectedObject.transform.parent;
        string configName = selectedObject.transform.parent.transform.Find("Label").GetComponent<TextMeshProUGUI>().text;
        TaskObject taskObj = parent.parent.gameObject.GetComponent<TaskObject>();

        if (taskObj != null)
            AddToQueue(taskObj, configName);
    }


    public void AddToQueue(TaskObject taskObject, string configName)
    {
        try
        {
            GameObject item = Instantiate(Resources.Load<GameObject>("Queue_GridItem"));
            item.name = configName;
            item.transform.SetParent(Queue_GridParent.transform);
            item.transform.localScale = Vector3.one;
            item.transform.localPosition = Vector3.one;
            item.transform.eulerAngles = Camera.main.transform.eulerAngles;

            item.transform.Find("Number").transform.Find("Number_Text").GetComponent<TextMeshProUGUI>().text = (QueueItems.Count + 1).ToString();

            TextMeshProUGUI textComponent = item.transform.Find("Icon").transform.Find("Border").transform.Find("Front").transform.Find("Icon_Text").GetComponent<TextMeshProUGUI>();
            textComponent.text = taskObject.TaskInitials;

            Color32 taskColor = GetTaskColor(taskObject.TaskName);

            textComponent.color = taskColor;

            item.transform.Find("Icon").transform.Find("Border").GetComponent<Image>().color = taskColor;


            item.transform.Find("ConfigName_Text").GetComponent<TextMeshProUGUI>().text = configName;
            item.transform.Find("X").gameObject.AddComponent<Button>().onClick.AddListener(OnRemoveFromQueueButtonPress);
            item.transform.Find("Arrows").transform.Find("UpArrow").gameObject.AddComponent<Button>().onClick.AddListener(OnMoveUpArrowPressed);
            item.transform.Find("Arrows").transform.Find("DownArrow").gameObject.AddComponent<Button>().onClick.AddListener(OnMoveDownArrowPressed);

            item.SetActive(true);

            QueueItem queueComponent = item.AddComponent<QueueItem>();
            queueComponent.SetupItem(taskObject.TaskName, configName, QueueItems.Count + 1);
            QueueItems.Add(queueComponent);
            taskObject.AddToQueueList(queueComponent);

            AddIconToExpDisplay(taskObject);
        }
        catch(Exception e)
        {
            Debug.LogError("ERROR WHILE ADDING TO QUEUE | ERROR: " + e.Message);
        }
    }

    public void AddIconToExpDisplay(TaskObject taskObject)
    {
        GameObject item = Instantiate(Resources.Load<GameObject>("TaskOrder_GridItem"));
        item.name = taskObject.TaskInitials;
        item.transform.SetParent(ExpDisplay_TaskOrder_GridParent.transform);
        item.transform.localScale = Vector3.one;
        item.transform.localPosition = Vector3.one;
        item.transform.eulerAngles = Camera.main.transform.eulerAngles;

        item.GetComponent<Image>().color = taskObject.TaskColor;

        TextMeshProUGUI textComponent = item.transform.Find("Item_Front").transform.Find("Item_Text").GetComponent<TextMeshProUGUI>();
        textComponent.text = taskObject.TaskInitials;
        textComponent.color = taskObject.TaskColor;

        item.SetActive(true);
    }

    private void OnRemoveFromQueueButtonPress()
    {
        PlayAudio(buttonClickedAudioClip);

        GameObject parent = EventSystem.current.currentSelectedGameObject.transform.parent.gameObject;
        QueueItem queueItem = parent.GetComponent<QueueItem>();
        if(queueItem == null)
        {
            Debug.LogError("QUEUE ITEM COMPONENT IS NULL");
            return;
        }
        int index = queueItem.SpotInQueue - 1;
        QueueItems.RemoveAt(index);
        Destroy(parent);

        //UPDATE THE NUMBER and SPOT-IN-QUEUE FOR EACH REMAINING ITEM:
        foreach (QueueItem item in QueueItems)
        {
            int newNum = QueueItems.IndexOf(item) + 1;
            item.Index_Text.text = newNum.ToString();
            item.SpotInQueue = newNum; 
        }

        //ALSO REMOVE THE ICON FROM THE TASK ORDER SECTION OF EXP DISPLAY:
        Destroy(ExpDisplay_TaskOrder_GridParent.transform.GetChild(index).gameObject);
    }

    //Called in termination method of RunTask. Removes the first icon on the Exp Display task order list
    public void SetExpDisplayIconAsInactive(int index)
    {
        GameObject icon = ExpDisplay_TaskOrder_GridParent.transform.GetChild(index).gameObject;
        if(icon == null)
        {
            Debug.LogError("ICON IS NULL");
            return;
        }
        icon.GetComponent<Image>().color = InactiveTaskIconBorderColor;
        TextMeshProUGUI taskInitialsText = icon.GetComponentInChildren<TextMeshProUGUI>();
        taskInitialsText.color = InactiveTaskIconTextColor;
    }

    public void PlayAudio(AudioClip clip)
    {
        if (clip != null)
        {
            buttonAudioSource.clip = clip;
            buttonAudioSource.Play();
        }
        else
            Debug.LogWarning("CANT PLAY AUDIO CLIP BECAUSE IT IS NULL!");
    }

    private string GetSplitName(string taskName)
    {
        string splitName = "";
        for(int i = 0; i < taskName.Length; i++)
        {
            if (char.IsUpper(taskName[i]) && i > 0)
                splitName += " ";

            splitName += taskName[i];
        }
        return splitName;
    }

    private string GetInitials(string taskName)
    {
        string initials = "";
        int count = 0;
        for (int i = 0; i < taskName.Length && count < 3; i++)
        {
            if (char.IsUpper(taskName[i]))
            {
                initials += taskName[i];
                count++;
            }
        }
        return initials;
    }

}

public class QueueItem : MonoBehaviour
{
    public string TaskName;
    public string ConfigName;
    public int SpotInQueue;
    public TextMeshProUGUI Index_Text;

    public void SetupItem(string taskName, string configName, int queueIndex)
    {
        TaskName = taskName;
        ConfigName = configName;
        SpotInQueue = queueIndex;

        Index_Text = gameObject.transform.Find("Number").transform.Find("Number_Text").GetComponent<TextMeshProUGUI>();
    }

}

public class TaskObject : MonoBehaviour
{
    public string TaskName;
    public string SplitTaskName;
    public string TaskInitials;
    public Color32 TaskColor;
    public List<QueueItem> TaskQueueItems;
    public TMP_Dropdown Dropdown;
    public int TrialsCompleted;
    public int TaskDurationInMin;
    public OrderedDictionary TaskResultsData;

    public void SetupObject(string taskName, string splitName, string taskInitials)
    {
        TaskQueueItems = new List<QueueItem>();

        TaskName = taskName;
        SplitTaskName = splitName;
        TaskInitials = taskInitials;
    }

    public void AddToQueueList(QueueItem item)
    {
        TaskQueueItems ??= new List<QueueItem>();
        TaskQueueItems.Add(item);
    }

    public void SetDropdown(TMP_Dropdown dropdown)
    {
        Dropdown = dropdown;
        Dropdown.ClearOptions();
    }

    public void AddOption(string configName)
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(configName);
        options.Add(option);
        Dropdown.AddOptions(options);
    }



}

