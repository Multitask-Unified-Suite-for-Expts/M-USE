using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class ObjectManager : MonoBehaviour
{
    public List<SA_Object> TargetList;
    public List<SA_Object> DistractorList;

    public static List<Vector3> Destinations;
    public static List<Vector3> StartingPositionsUsed;

    private Transform ObjectParent;

    private void Awake()
    {
        TargetList = new List<SA_Object>();
        DistractorList = new List<SA_Object>();
        StartingPositionsUsed = new List<Vector3>();
        CalculateDestinations();
    }

    public void SetObjectParent(Transform parentTransform)
    {
        ObjectParent = parentTransform;
    }

    public void DestroyExistingObjects()
    {
        if (TargetList.Count > 0)
        {
            foreach (SA_Object obj in TargetList)
                Destroy(obj);
        }
        if (DistractorList.Count > 0)
        {
            foreach (SA_Object obj in DistractorList)
                Destroy(obj);
        }
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

    public void CreateObjects(bool isTarget, bool rotateTowardsDest, int[] sizes, int[] speeds, float[] nextDestDistances, float[] animIntervals, int[] rewards, Color color)
    {
        if(sizes.Length != speeds.Length || sizes.Length != animIntervals.Length || sizes.Length != rewards.Length)
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
            obj.SetupObject(isTarget, rotateTowardsDest, speeds[i], sizes[i], nextDestDistances[i], animIntervals[i], rewards[i]);

            if (isTarget)
                TargetList.Add(obj);
            else
                DistractorList.Add(obj);
        }
    }
    
    private void CalculateDestinations()
    {
        Destinations = new List<Vector3>();
        //int[] xValues = new int[] { -300, -150, 0, 150, 300};
        //int[] yValues = new int[] { 150, 0, -150};
        int[] xValues = new int[] { -750, -600, -450, -300, -150, 0, 150, 300, 450, 600, 750 };
        int[] yValues = new int[] { 300, 150, 0, -150, -300};

        for (int i = 0; i < yValues.Length; i++)
        {
            float y = yValues[i];
            for (int j = 0; j < xValues.Length; j++)
            {
                float x = xValues[j];
                Destinations.Add(new Vector3(x, y, 0));
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
    public bool IsTarget;
    public bool RotateTowardsDest;
    public float Speed;
    public float Size;
    public float NextDestDist;

    public float AnimInterval; //not used yet
    public int Reward; //not used yet
    public List<Vector3> Visited;
    public Vector2 StartingPosition;
    public Vector3 CurrentDestination;
    public bool Move;
    public GameObject Marker;
    public Vector3 Direction;

    private float NewDestStartTime;
    private readonly float MaxCollisionTime = .25f;

    private static Vector2 xRange = new Vector2(-750f, 750f);
    private static Vector2 yRange = new Vector2(-350f, 350f);

    private float AnimStartTime;

    private int ImageCount = 0;


    public SA_Object()
    {
        Visited = new List<Vector3>();
    }

    public void SetupObject(bool isTarget, bool rotateTowardsDest, float speed, float size, float nextDestDist, float interval, int reward)
    {
        IsTarget = isTarget;
        RotateTowardsDest = rotateTowardsDest;
        Speed = speed;
        Size = size;
        NextDestDist = nextDestDist;
        AnimInterval = interval;
        Reward = reward;

        SetRandomStartingPosition();
        SetNewDestination();

        SetupMarker(); //Marker for debugging purposes
    }

    private void Update()
    {
        if(Move)
        {
            if (AtDestination())
                SetNewDestination();            

            MoveTowardsDestination();

            if(RotateTowardsDest)
                RotateTowardsDirection();

            Marker.transform.localPosition = CurrentDestination;

            if(Time.time - AnimStartTime >= AnimInterval)
            {
                Animate();
                AnimStartTime = Time.time;
            }
            
        }
    }

    private void Animate()
    {
        ImageCount = 1 - ImageCount;
        gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(ImageCount == 1 ? "closed_Transparent" : "open_Transparent");
    }



    public void ActivateMovement()
    {
        Move = true;
        //Marker.SetActive(true);
        AnimStartTime = Time.time;
    }

    private void MoveTowardsDestination()
    {
        if(CurrentDestination != null)
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
        float chanceSmall = 0.25f;
        float chanceMedium = 0.6f;

        float currentAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg; //Extract angle from current Direction

        float randomChance = Random.value; //Get randomNum between 0 and 1
        float newDirection;

        if (randomChance < chanceSmall)
            newDirection = Random.Range(0, 16f);
        else if (randomChance < chanceSmall + chanceMedium)
            newDirection = Random.Range(16f, 46f);
        else
            newDirection = Random.Range(46f, 181f);

        float randomNum = Random.value;
        if (randomNum < .5f)
            newDirection = -newDirection;
        
        currentAngle += newDirection;

        float radianAngle = Mathf.Deg2Rad * currentAngle;

        float newX = Mathf.Cos(radianAngle);
        float newY = Mathf.Sin(radianAngle);

        Vector3 change = new Vector3(newX, newY, 0);
        Vector3 destination = transform.localPosition + change * NextDestDist;

        float xDiff = Mathf.Clamp(destination.x, xRange.x, xRange.y) - destination.x;
        float yDiff = Mathf.Clamp(destination.y, yRange.x, yRange.y) - destination.y;

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
            randomIndex = Random.Range(0, ObjectManager.Destinations.Count);
            randomPos = ObjectManager.Destinations[randomIndex];
        }
        while (ObjectManager.StartingPositionsUsed.Contains(randomPos));

        ObjectManager.StartingPositionsUsed.Add(randomPos);

        StartingPosition = randomPos;
        transform.localPosition = randomPos;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Direction = -Direction;
        SetNewDestination();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time - NewDestStartTime >= MaxCollisionTime)
        {
            Debug.LogWarning("BEEN COLLIIDING FOR TOO LONG, SETTING NEW DESTINATION!");
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
        Marker.name = gameObject.name + "Clone";
        Marker.transform.SetParent(GameObject.Find("SustainedAttention_Canvas").transform);
        Marker.transform.localScale = new Vector3(.3f, .3f, .3f);
        Marker.transform.localPosition = CurrentDestination;
        if (!IsTarget)
            Marker.GetComponent<Image>().color = Color.magenta;
        else
            Marker.GetComponent<Image>().color = Color.yellow;
        Marker.SetActive(false);
    }
}
