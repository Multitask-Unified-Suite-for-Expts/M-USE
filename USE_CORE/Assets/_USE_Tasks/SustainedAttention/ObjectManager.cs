using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class ObjectManager : MonoBehaviour
{
    public List<SA_Object> TargetList;
    public List<SA_Object> DistractorList;

    public static List<Vector3> StartingPositions;
    public static List<Vector3> StartingPositionsUsed;

    private Transform ObjectParent;

    public static readonly Vector2 xRange = new Vector2(-800f, 800f);
    public static readonly Vector2 yRange = new Vector2(-400f, 325f);


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

    public void CreateObjects(bool isTarget, bool rotateTowardsDest, float minAnimGap, Vector2 responseWindow, float closeDuration, int[] sizes, int[] speeds, float[] nextDestDistances, Vector2[] intervalsAndDurations, Color color)
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
            obj.SetupObject(isTarget, rotateTowardsDest, minAnimGap, responseWindow, closeDuration, speeds[i], sizes[i], nextDestDistances[i], intervalsAndDurations);

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
                obj.DestroyObj();
            TargetList.Clear();
        }
        if (DistractorList.Count > 0)
        {
            foreach (SA_Object obj in DistractorList)
                obj.DestroyObj();
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
    public float MinAnimGap;
    public bool IsTarget;
    public bool RotateTowardsDest;
    public float Speed;
    public float Size;
    public float NextDestDist;
    public Vector2 ResponseWindow;
    public float CloseDuration;

    public Vector2[] RateAndDurations;

    public List<Vector3> Visited;
    public Vector2 StartingPosition;
    public Vector3 CurrentDestination;
    public bool Move; //Controls whether or not they're moving around the screen
    public bool Paused;

    public GameObject Marker;
    public Vector3 Direction;

    private float NewDestStartTime;
    private readonly float MaxCollisionTime = .25f;

    public int CurrentIndex = 0;
    private int AnimationsCompleted = 0;
    private float NextAnimationTime;

    public float AnimStartTime;

    public bool WithinDuration;

    private readonly List<float> PreviousAngleOffsets = new List<float>();


    public SA_Object()
    {
        Visited = new List<Vector3>();
        CurrentIndex = 0;
    }

    public void DestroyObj()
    {
        Destroy(gameObject);
        Destroy(Marker);
    }

    public void SetupObject(bool isTarget, bool rotateTowardsDest, float minAnimGap, Vector2 responseWindow, float closeDuration, float speed, float size, float nextDestDist, Vector2[] intervalsAndDurations)
    {
        IsTarget = isTarget;
        RotateTowardsDest = rotateTowardsDest;
        MinAnimGap = minAnimGap;
        ResponseWindow = responseWindow;
        Speed = speed;
        Size = size;
        NextDestDist = nextDestDist;
        CloseDuration = closeDuration;
        RateAndDurations = intervalsAndDurations;

        SetRandomStartingPosition();
        SetNewDestination();

        SetupMarker(); //Marker for debugging purposes
    }

    private void SetNextAnimationTime()
    {
        float rate = RateAndDurations[CurrentIndex].x;
        NextAnimationTime = Time.time + Random.Range(MinAnimGap, rate);
    }

    int CalcTotalAnimationsPerDuration()
    {
        float rate = RateAndDurations[CurrentIndex].x;
        float duration = RateAndDurations[CurrentIndex].y;
        return (int) MathF.Round(duration / rate);
    }

    private void Update()
    {
        if(Move)
        {
            if (Time.time >= NextAnimationTime)
            {
                StartCoroutine(AnimationCoroutine());

                AnimationsCompleted++;

                if (AnimationsCompleted >= CalcTotalAnimationsPerDuration())
                {
                    AnimationsCompleted = 0;
                    CurrentIndex++;

                    if (CurrentIndex >= RateAndDurations.Length)
                    {
                        DestroyObj();
                        return;
                    }
                }
                SetNextAnimationTime();
            }

            if (Time.time - AnimStartTime > ResponseWindow.x && Time.time - AnimStartTime <= ResponseWindow.y)
                WithinDuration = true;
            else
                WithinDuration = false;

            HandlePausingDuringSelection();

            HandleMarkerToggle();
            HandleRotationToggle();
        }
    }

    private void FixedUpdate()
    {
        if(Move && !Paused)
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

    private void HandlePausingDuringSelection()
    {
        if (InputBroker.GetMouseButtonDown(0))
        {
            GameObject hit = InputBroker.RaycastBoth(InputBroker.mousePosition);
            if (hit != null && hit == gameObject)
                Paused = true;
        }

        if (InputBroker.GetMouseButtonUp(0))
        {
            GameObject hit = InputBroker.RaycastBoth(InputBroker.mousePosition);
            if (hit != null && hit == gameObject)
                Paused = false;
        }
    }

    private void HandleMarkerToggle()
    {
        if (InputBroker.GetKeyDown(KeyCode.M))
            ToggleMarker();
    }

    private void HandleRotationToggle()
    {
        if (InputBroker.GetKeyDown(KeyCode.U))
        {
            RotateTowardsDest = !RotateTowardsDest;
            if (!RotateTowardsDest)
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    public void ActivateMovement()
    {
        Move = true;
        SetNextAnimationTime();
        AnimStartTime = Time.time;
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
        float chanceSmall = 0.4f;
        float chanceMedium = 0.5f;

        float currentAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg; //Extract angle from current Direction

        float randomChance = Random.value; //Get randomNum between 0 and 1
        float angleOffset;

        if (randomChance < chanceSmall)
            angleOffset = Random.Range(0, 16f);
        else if (randomChance < chanceSmall + chanceMedium)
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
        float xDiff = Mathf.Clamp(destination.x, ObjectManager.xRange.x, ObjectManager.xRange.y) - destination.x;
        float yDiff = Mathf.Clamp(destination.y, ObjectManager.yRange.x, ObjectManager.yRange.y) - destination.y;
        destination += new Vector3(xDiff, yDiff, 0);

        CurrentDestination = destination;
        NewDestStartTime = Time.time;

    }

    private void SetRandomStartingPosition()
    {
        int randomIndex;
        Vector3 randomPos;

        do
        {
            randomIndex = Random.Range(0, ObjectManager.StartingPositions.Count);
            randomPos = ObjectManager.StartingPositions[randomIndex];
        }
        while (ObjectManager.StartingPositionsUsed.Contains(randomPos));

        ObjectManager.StartingPositionsUsed.Add(randomPos);

        StartingPosition = randomPos;
        transform.localPosition = randomPos;
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
        {
            Visited.Add(transform.localPosition);
            return true;
        }
        return false;
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
