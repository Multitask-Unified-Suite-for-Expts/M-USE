using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using USE_ExperimentTemplate_Classes;
using Random = UnityEngine.Random;


public class SA_ObjectManager : MonoBehaviour
{
    public List<SA_Object> TargetList;
    public List<SA_Object> DistractorList;

    public static List<Vector3> StartingPositions;
    public static List<Vector3> StartingPositionsUsed;

    private Transform ObjectParent;

    public static readonly Vector2 xRange = new Vector2(-800f, 800f);
    public static readonly Vector2 yRange = new Vector2(-400f, 325f);

    public delegate void CycleEventHandler();
    public event CycleEventHandler OnTargetIntervalMissed;
    public event CycleEventHandler OnDistractorAvoided;

    public Dictionary<string, EventCode> TaskEventCodes; //task event codes passed in so can trigger "Object Animation Occured" event code


    public void NoSelectionDuringInterval(SA_Object obj)
    {
        if (obj.IsTarget)
            OnTargetIntervalMissed?.Invoke();
        else
            OnDistractorAvoided?.Invoke();
    }

    private void Awake()
    {
        TargetList = new List<SA_Object>();
        DistractorList = new List<SA_Object>();
        StartingPositions = new List<Vector3>();
        StartingPositionsUsed = new List<Vector3>();

        CalculateStartingPositions();
    }

    public void SetObjectParent(Transform parentTransform)
    {
        ObjectParent = parentTransform;
    }

    public void RemoveFromObjectList(SA_Object obj)
    {
        if (obj.IsTarget)
            TargetList.Remove(obj);
        else
            DistractorList.Remove(obj);
    }

    public void ActivateObjectMovement()
    {
        if (TargetList.Count > 0)
        {
            foreach (SA_Object target in TargetList)
                target.ActivateMovement();
        }

        if (DistractorList.Count > 0)
        {
            foreach (SA_Object distractor in DistractorList)
                distractor.ActivateMovement();
        }
    }

    public List<SA_Object> CreateObjects(List<SA_Object_ConfigValues> objects)
    {
        List<SA_Object> trialObjects = new List<SA_Object>();

        foreach(SA_Object_ConfigValues configValues in objects)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("PacmanCircle"));

            go.GetComponent<PacmanDrawer>().ManualStart();

            go.name = configValues.IsTarget ? $"Target" : $"Distractor";
            go.SetActive(false);
            go.transform.SetParent(ObjectParent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(configValues.Size, configValues.Size);
            go.GetComponent<Image>().color = new Color(configValues.ObjectColor[0], configValues.ObjectColor[1], configValues.ObjectColor[2]);
            go.GetComponent<CircleCollider2D>().radius = configValues.Size * .567f; //Set Collider radius

            SA_Object obj = go.AddComponent<SA_Object>();
            obj.SetupObject(this, configValues);

            if (obj.IsTarget)
                TargetList.Add(obj);
            else
                DistractorList.Add(obj);

            trialObjects.Add(obj);
        }

        return trialObjects;
    }
    
    private void CalculateStartingPositions()
    {
        int[] xValues = new int[] { -800, -400, 0, 400, 800 };
        int[] yValues = new int[] { 325, 84, -158, -400};

        for (int i = 0; i < yValues.Length; i++)
        {
            float y = yValues[i];
            for (int j = 0; j < xValues.Length; j++)
            {
                float x = xValues[j];
                StartingPositions.Add(new Vector3(x, y, 0));
            }
        }
    }

    public void AddToList(SA_Object obj)
    {
        if (obj.IsTarget)
            TargetList.Add(obj);
        else
            DistractorList.Add(obj);
    }

    public void DestroyExistingObjects()
    {
        List<SA_Object> targetListCopy = new List<SA_Object>(TargetList);
        List<SA_Object> distractorListCopy = new List<SA_Object>(DistractorList);

        foreach(SA_Object obj in targetListCopy)
        {
            if (obj != null)
                obj.DestroyObj();
        }

        foreach (SA_Object obj in distractorListCopy)
        {
            if (obj != null)
                obj.DestroyObj();
        }

        TargetList.Clear();
        DistractorList.Clear();
    }

    public void ActivateTargets()
    {
        foreach (var target in TargetList)
        {
            target.gameObject.SetActive(true);
        }
    }

    public void DeactivateTargets()
    {
        foreach (var target in TargetList)
            target.gameObject.SetActive(false);
    }

    public void ActivateDistractors()
    {
        foreach (var distractor in DistractorList)
        {
            distractor.gameObject.SetActive(true);
        }
    }

    public void DeactivateDistractors()
    {
        foreach (var target in DistractorList)
            target.gameObject.SetActive(false);
    }


}

public class SA_Object : MonoBehaviour
{
    public SA_ObjectManager ObjManager;

    public PacmanDrawer PacmanDrawer;

    //From Object Config:
    public int Index;
    public string ObjectName;
    public float OpenAngle; //90 or 75 as of now
    public int ClosedLineThickness;
    public float MinAnimGap;
    public bool IsTarget;
    public bool RotateTowardsDest;
    public float Speed;
    public float Size;
    public float NextDestDist;
    public Vector2 ResponseWindow;
    public float CloseDuration;
    public Vector2[] RatesAndDurations;
    public Vector3 AngleProbs;
    public Vector2[] AngleRanges;
    public int NumDestWithoutBigTurn;
    public float[] ObjectColor;
    public int SliderChange;

    public Vector2 StartingPosition;
    public Vector3 CurrentDestination;
    public bool MoveAroundScreen;
    public bool ObjectPaused;
    public GameObject Marker;
    public Vector3 Direction;
    private float NewDestStartTime;
    private readonly float MaxCollisionTime = .25f;
    public float AnimStartTime;
    public bool WithinDuration;
    private readonly List<float> PreviousAngleOffsets = new List<float>();
    public List<Cycle> Cycles;
    public Cycle CurrentCycle;


    public SA_Object()
    {
        Cycles = new List<Cycle>();
    }


    public void SetupObject(SA_ObjectManager objManager, SA_Object_ConfigValues configValue)
    {
        ObjManager = objManager;
        Index = configValue.Index;
        ObjectName = configValue.ObjectName;
        OpenAngle = configValue.OpenAngle;
        ClosedLineThickness = configValue.ClosedLineThickness;
        IsTarget = configValue.IsTarget;
        AngleProbs = configValue.AngleProbs;
        RotateTowardsDest = configValue.RotateTowardsDest;
        MinAnimGap = configValue.MinAnimGap;
        ResponseWindow = configValue.ResponseWindow;
        Speed = configValue.Speed;
        Size = configValue.Size;
        NextDestDist = configValue.NextDestDist;
        CloseDuration = configValue.CloseDuration;
        RatesAndDurations = configValue.RatesAndDurations;
        ObjectColor = configValue.ObjectColor;
        SliderChange = configValue.SliderChange;
        AngleRanges = configValue.AngleRanges;
        NumDestWithoutBigTurn = configValue.NumDestWithoutBigTurn;

        foreach (var rateAndDur in RatesAndDurations)
        {
            Cycle cycle = new()
            {
                sa_Object = this,
                duration = rateAndDur.y,
                intervals = GenerateRandomIntervals((int)(rateAndDur.y * rateAndDur.x), rateAndDur.y)
            };
            Cycles.Add(cycle);
        }

        SetRandomStartingPosition();
        SetNewDestination();

        SetupMarker(); //Marker for debugging purposes

        PacmanDrawer = gameObject.GetComponent<PacmanDrawer>();
        PacmanDrawer.ClosedLineThickness = ClosedLineThickness;
        PacmanDrawer.DrawMouth(OpenAngle);

    }

    List<float> GenerateRandomIntervals(int numIntervals, float duration)
    {
        List<float> randomFloats = new List<float>() { duration }; //add the ending number as a interval. 1) so last interval will end here, and 2) so that no randomly gen numbers below will be too close to the final value and thus subject may not have time to select before cycle ends. 

        for (int i = 0; i < numIntervals; i++)
        {
            float randomValue;
            do
            {
                randomValue = Random.Range(0f, duration);
            } while (randomFloats.Any(value => Mathf.Abs(value - randomValue) < MinAnimGap));
            randomFloats.Add(randomValue);
        }
        randomFloats.Sort();

        //if (IsTarget)
        //    foreach (var num in randomFloats)
        //        Debug.LogWarning("INTERVAL: " + num);

        return randomFloats;
    }

    private void NextCycle()
    {
        Cycles.RemoveAt(0);

        if (Cycles.Count >= 1)
        {
            CurrentCycle = Cycles[0];
            CurrentCycle.StartCycle();
        }
        else
            DestroyObj();
    }

    private void Update()
    {
        if(MoveAroundScreen)
        {
            if(Time.time - CurrentCycle.cycleStartTime >= CurrentCycle.currentInterval && CurrentCycle.intervals.Count > 0)
            {
                StartCoroutine(AnimationCoroutine());
                CurrentCycle.NextInterval();
            }

            if(Time.time - CurrentCycle.cycleStartTime >= CurrentCycle.duration)
            {
                NextCycle();
            }

            WithinDuration = AnimStartTime > 0 && Time.time - AnimStartTime > ResponseWindow.x && Time.time - AnimStartTime <= ResponseWindow.y;

            HandlePausingWhileBeingSelected();
            HandleInput();
        }
    }

    private void FixedUpdate()
    {
        if(MoveAroundScreen && !ObjectPaused)
        {
            if (AtDestination())
                SetNewDestination();            

            MoveTowardsDestination();

            if(RotateTowardsDest)
                RotateTowardsDirection();

            Marker.transform.localPosition = CurrentDestination;
        }
    }

    private IEnumerator AnimationCoroutine()
    {
        AnimStartTime = Time.time;
        Session.EventCodeManager.AddToFrameEventCodeBuffer(ObjManager.TaskEventCodes["ObjectAnimationBegins"]);
        gameObject.GetComponent<PacmanDrawer>().DrawClosedMouth();
        yield return new WaitForSeconds(CloseDuration);
        gameObject.GetComponent<PacmanDrawer>().DrawMouth(OpenAngle);
    }

    private void HandlePausingWhileBeingSelected()
    {
        if (InputBroker.GetMouseButtonDown(0))
        {
            GameObject hit = InputBroker.RaycastBoth(InputBroker.mousePosition);
            if (hit != null && hit == gameObject)
                ObjectPaused = true;
        }

        if (InputBroker.GetMouseButtonUp(0))
        {
            GameObject hit = InputBroker.RaycastBoth(InputBroker.mousePosition);
            if (hit != null && hit == gameObject)
                ObjectPaused = false;
        }
    }

    private void HandleInput()
    {
        //Marker Toggle:
        if (InputBroker.GetKeyDown(KeyCode.M))
            ToggleMarker();

        //Rotation Toggle:
        if (InputBroker.GetKeyDown(KeyCode.U))
        {
            RotateTowardsDest = !RotateTowardsDest;
            if (!RotateTowardsDest)
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        //Increase/Decrease NextDestDist:
        if (InputBroker.GetKeyDown(KeyCode.UpArrow))
            NextDestDist *= 1.5f;
        if (InputBroker.GetKeyDown(KeyCode.DownArrow))
            NextDestDist /= 1.5f;
    }

    public void ActivateMovement()
    {
        MoveAroundScreen = true;

        AnimStartTime = 0; //used to be Time.time 

        CurrentCycle = Cycles[0];
        CurrentCycle.StartCycle();
    }

    private void MoveTowardsDestination()
    {
        if (CurrentDestination != null)
        {
            Direction = (CurrentDestination - transform.localPosition).normalized;
            Vector3 nextPos = transform.localPosition + Speed * Time.deltaTime * Direction;
            transform.localPosition = nextPos;
        }
    }

    private void RotateTowardsDirection()
    {
        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public void SetNewDestination()
    {
        float currentAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg; //Extract angle from current Direction

        float randomChance = Random.value; //Get randomNum between 0 and .99
        float angleOffset;

        if (randomChance < AngleProbs.x) //AngleProbs.x is the "Chance of a small angle"
            angleOffset = Random.Range(AngleRanges[0].x, AngleRanges[0].y); //Small Angle Range
        else if (randomChance < AngleProbs.x + AngleProbs.y) //AngleProbs.y is the "Chance of a medium angle"
            angleOffset = Random.Range(AngleRanges[1].x, AngleRanges[1].y); //Medium Angle Range
        else
            angleOffset = Random.Range(AngleRanges[2].x, AngleRanges[2].y); //Large Angle Range

       
        if (PreviousAngleOffsets.Count > NumDestWithoutBigTurn)
            PreviousAngleOffsets.RemoveAt(0);

        if (angleOffset > 90f)
        {
            foreach (float offset in PreviousAngleOffsets)
            {
                if (Mathf.Abs(offset) > 90f)
                {
                    angleOffset = Random.Range(0, 46);
                    break;
                }
            }
        }

        float randomNum = Random.value; //Randomize whether to make it negative
        if (randomNum < .5f) //50% chance to be negative
            angleOffset = -angleOffset;

        PreviousAngleOffsets.Add(angleOffset);
        
        currentAngle += angleOffset;
        float radianAngle = Mathf.Deg2Rad * currentAngle;
        float newX = Mathf.Cos(radianAngle);
        float newY = Mathf.Sin(radianAngle);
        Vector3 change = new Vector3(newX, newY, 0);
        Vector3 destination = transform.localPosition + change * NextDestDist;
        float xDiff = Mathf.Clamp(destination.x, SA_ObjectManager.xRange.x, SA_ObjectManager.xRange.y) - destination.x;
        float yDiff = Mathf.Clamp(destination.y, SA_ObjectManager.yRange.x, SA_ObjectManager.yRange.y) - destination.y;
        destination += new Vector3(xDiff, yDiff, 0);

        CurrentDestination = destination;
        NewDestStartTime = Time.time;

    }

    private void SetRandomStartingPosition()
    {
        int randomIndex;
        Vector3 newRandomPos;

        do
        {
            randomIndex = Random.Range(0, SA_ObjectManager.StartingPositions.Count);
            newRandomPos = SA_ObjectManager.StartingPositions[randomIndex];
        }
        while (SA_ObjectManager.StartingPositionsUsed.Contains(newRandomPos));

        SA_ObjectManager.StartingPositionsUsed.Add(newRandomPos);

        StartingPosition = newRandomPos;
        transform.localPosition = newRandomPos;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(!IsTarget)
        {
            Direction = -Direction;
            SetNewDestination();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time - NewDestStartTime >= MaxCollisionTime)
        {
            Direction = -Direction;
            SetNewDestination();   
        }
    }

    public bool AtDestination()
    {
        float distanceThreshold = 5f;
        float distanceToDestination = Vector3.Distance(transform.localPosition, CurrentDestination);

        if(distanceToDestination <= distanceThreshold)
            return true;
        
        return false;
    }

    public void DestroyObj()
    {
        ObjManager.RemoveFromObjectList(this);

        if(gameObject != null)
            Destroy(gameObject);
        if(Marker != null)
            Destroy(Marker);

    }

    private void SetupMarker()
    {
        if (Marker != null)
            Destroy(Marker);
        Marker = Instantiate(Resources.Load<GameObject>("DestinationMarker"));
        Marker.name = gameObject.name + "_Marker";
        Marker.transform.SetParent(GameObject.Find("SustainedAttention_Canvas").transform);
        Marker.transform.localScale = new Vector3(.3f, .3f, .3f);
        Marker.transform.localPosition = CurrentDestination;
        Marker.GetComponent<Image>().color = new Color(ObjectColor[0], ObjectColor[1], ObjectColor[2]);
        Marker.SetActive(false);
    }

    private void ToggleMarker()
    {
        Marker.SetActive(!Marker.activeInHierarchy);
    }
}

//Class is used to read in SA Object Config values. Can't read in directly to SA_Object because that creates the gameobject. We need to create the gameobject after getting the object values.
public class SA_Object_ConfigValues
{
    //From Object Config:
    public int Index;
    public string ObjectName;
    public float OpenAngle;
    public int ClosedLineThickness;
    public float MinAnimGap;
    public bool IsTarget;
    public bool RotateTowardsDest;
    public float Speed;
    public float Size;
    public float NextDestDist;
    public Vector2 ResponseWindow;
    public float CloseDuration;
    public Vector2[] RatesAndDurations;
    public Vector3 AngleProbs;
    public Vector2[] AngleRanges;
    public int NumDestWithoutBigTurn;
    public float[] ObjectColor;
    public int SliderChange;

}

public class Cycle
{
    public SA_Object sa_Object;

    public float duration;
    public List<float> intervals;
    public float currentInterval;
    public float cycleStartTime;

    public bool selectedDuringCurrentInterval;
    public bool firstIntervalStarted;


    public void StartCycle()
    {
        currentInterval = intervals[0];
        cycleStartTime = Time.time;
        firstIntervalStarted = false;
    }

    public void NextInterval()
    {
        if(firstIntervalStarted && !selectedDuringCurrentInterval) //Skip first interval cuz hasn't animated yet.
            sa_Object.ObjManager.NoSelectionDuringInterval(sa_Object); //Trigger event so data can be incremented
        
        intervals.RemoveAt(0);
        if (intervals.Count > 0)
            currentInterval = intervals[0];

        selectedDuringCurrentInterval = false;

        if (!firstIntervalStarted)
            firstIntervalStarted = true;
    }

}