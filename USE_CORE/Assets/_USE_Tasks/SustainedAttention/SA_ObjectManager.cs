using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
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

    public void CreateObjects(bool isTarget, Vector3 angleProbs, bool rotateTowardsDest, float minAnimGap, Vector2 responseWindow, float closeDuration, int[] sizes, int[] speeds, float[] nextDestDistances, Vector2[] intervalsAndDurations, Color color)
    {
        if(sizes.Length != speeds.Length)
        {
            Debug.LogError("ERROR CREATING SA OBJECTS. NOT ALL ARRAYS CONTAIN SAME NUMBER OF VALUES!");
            return;
        }

        for(int i = 0; i < sizes.Length; i++)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("Target_Open"));
            go.name = isTarget ? $"Target{i+1}" : $"Distractor{i+1}";
            go.SetActive(false);
            go.transform.SetParent(ObjectParent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(sizes[i], sizes[i]);
            go.GetComponent<Image>().color = color;

            go.GetComponent<CircleCollider2D>().radius = sizes[i] * .567f; //Set Collider radius

            SA_Object obj = go.AddComponent<SA_Object>();
            obj.SetupObject(this, isTarget, angleProbs, rotateTowardsDest, minAnimGap, responseWindow, closeDuration, speeds[i], sizes[i], nextDestDistances[i], intervalsAndDurations);

            if (isTarget)
                TargetList.Add(obj);
            else
                DistractorList.Add(obj);
        }
    }
    
    private void CalculateStartingPositions()
    {
        int[] xValues = new int[] { -800, -600, -400, -200, 0, 200, 400, 600, 800 };
        int[] yValues = new int[] { 325, 180, 35, -110, -255, -400};

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
        if (TargetList.Count > 0)
        {
            foreach (SA_Object obj in TargetList)
            {
                if(obj != null)
                    obj.DestroyObj();
            }
            TargetList.Clear();
        }
        if (DistractorList.Count > 0)
        {
            foreach (SA_Object obj in DistractorList)
            {
                if(obj != null)
                    obj.DestroyObj();
            }
            DistractorList.Clear();
        }
    }

    public void ActivateTargets()
    {
        foreach (var target in TargetList)
            target.gameObject.SetActive(true);
    }

    public void DeactivateTargets()
    {
        foreach (var target in TargetList)
            target.gameObject.SetActive(false);
    }

    public void ActivateDistractors()
    {
        foreach (var distractor in DistractorList)
            distractor.gameObject.SetActive(true);
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

    public Vector3 AngleProbs;
    public float MinAnimGap;
    public bool IsTarget;
    public bool RotateTowardsDest;
    public float Speed;
    public float Size;
    public float NextDestDist;
    public Vector2 ResponseWindow;
    public float CloseDuration;

    public Vector2[] RateAndDurations;

    public Vector2 StartingPosition;
    public Vector3 CurrentDestination;
    public bool Move; //Controls whether or not they're moving around the screen
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


    public void SetupObject(SA_ObjectManager objManager, bool isTarget, Vector3 angleProbs, bool rotateTowardsDest, float minAnimGap, Vector2 responseWindow, float closeDuration, float speed, float size, float nextDestDist, Vector2[] ratesAndDurations)
    {
        ObjManager = objManager;
        IsTarget = isTarget;
        AngleProbs = angleProbs;
        RotateTowardsDest = rotateTowardsDest;
        MinAnimGap = minAnimGap;
        ResponseWindow = responseWindow;
        Speed = speed;
        Size = size;
        NextDestDist = nextDestDist;
        CloseDuration = closeDuration;
        RateAndDurations = ratesAndDurations;

        foreach(var rateAndDur in RateAndDurations)
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
        if(Move)
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
        if(Move && !ObjectPaused)
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
        gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("PacmanClosed");
        yield return new WaitForSeconds(CloseDuration);
        gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("PacmanOpen");
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
        Move = true;

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

        float randomChance = Random.value; //Get randomNum between 0 and 1
        float angleOffset;

        if (randomChance < AngleProbs.x) //AngleProbs.x is the "Chance of a small angle"
            angleOffset = Random.Range(0, 16f);
        else if (randomChance < AngleProbs.x + AngleProbs.y) //AngleProbs.y is the "Chance of a medium angle"
            angleOffset = Random.Range(16f, 46f);
        else
            angleOffset = Random.Range(46f, 181f);

        if (PreviousAngleOffsets.Count > 4) //Could make a variable "NumMovesWithoutTurningAround"
            PreviousAngleOffsets.RemoveAt(0);

        if(angleOffset > 90f)
        {
           foreach(float offset in PreviousAngleOffsets)
            {
                if(Mathf.Abs(offset) > 90f)
                {
                    angleOffset = Random.Range(0, 46);
                    break;
                }
            }
        }

        float randomNum = Random.value; //Randomize whether to make it negative
        if (randomNum < .5f)
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
        if (!IsTarget)
            Marker.GetComponent<Image>().color = Color.magenta;
        else
            Marker.GetComponent<Image>().color = Color.yellow;
        Marker.SetActive(false);
    }

    private void ToggleMarker()
    {
        Marker.SetActive(!Marker.activeInHierarchy);
    }
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