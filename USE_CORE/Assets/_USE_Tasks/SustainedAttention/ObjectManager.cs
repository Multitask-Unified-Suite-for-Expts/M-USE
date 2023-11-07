using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

public class ObjectManager : MonoBehaviour
{
    public List<SA_Object> TargetList;
    public List<SA_Object> DistractorList;

    public List<Vector3> Destinations;

    public static List<Vector3> StartingPositionsUsed;
    public static List<Vector3> CurrentDestinations;


    public ObjectManager()
    {
        TargetList = new List<SA_Object>();
        DistractorList = new List<SA_Object>();
        StartingPositionsUsed = new List<Vector3>();
        CurrentDestinations = new List<Vector3>();
        CalculateDestinations();
    }

    private void Update()
    {
        if(TargetList.Count > 0)
        {
            foreach(SA_Object target in TargetList)
            {
                if(target.Move)
                {
                    if(target.IsColliding)
                    {
                        CurrentDestinations.Remove(target.CurrentDestination);
                        SetNewDestination(target);
                        target.IsColliding = false;
                    }
                    if (target.AtDestination())
                    {
                        CurrentDestinations.Remove(target.CurrentDestination);
                        target.Visited.Add(target.transform.localPosition);
                        SetRandomDestination(target);   
                    }
                }
            }
        }

        if (DistractorList.Count > 0)
        {
            foreach (SA_Object distractor in DistractorList)
            {
                if(distractor.Move)
                {
                    //if (distractor.IsColliding)
                    //{
                    //    CurrentDestinations.Remove(distractor.CurrentDestination);
                    //    SetNewDestination(distractor);
                    //    distractor.IsColliding = false;
                    //}
                    if (distractor.AtDestination())
                    {
                        CurrentDestinations.Remove(distractor.CurrentDestination);
                        distractor.Visited.Add(distractor.transform.localPosition);
                        SetRandomDestination(distractor);   
                    }
                }
            }
        }
    }

    public void MoveObjects()
    {
        if (TargetList.Count > 0)
        {
            foreach (SA_Object target in TargetList)
            {
                target.Move = true;
                target.Marker.SetActive(true);
            }
        }

        if (DistractorList.Count > 0)
        {
            foreach (SA_Object distractor in DistractorList)
            {
                distractor.Move = true;
                distractor.Marker.SetActive(true);
            }
        }
    }

    public void CreateObject(Transform parent, bool isTarget, Vector2 size, float speed, float interval, int reward, Color? color = null)
    {
        GameObject go = Instantiate(Resources.Load<GameObject>("Target_Open"));
        go.name = isTarget ? "Target" : "Distractor";
        go.SetActive(false);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.GetComponent<RectTransform>().sizeDelta = size;
        if (color != null)
            go.GetComponent<Image>().color = color.Value;

        SA_Object obj = go.AddComponent<SA_Object>();
        obj.Setup(isTarget, speed, interval, reward);
        SetRandomStartingPosition(obj);
        SetRandomDestination(obj);

        if (isTarget)
            TargetList.Add(obj);
        else
            DistractorList.Add(obj);
    }

    private void SetRandomStartingPosition(SA_Object obj)
    {
        int randomIndex;
        Vector3 randomPos;

        do
        {
            randomIndex = Random.Range(0, Destinations.Count);
            randomPos = Destinations[randomIndex];
        }
        while (StartingPositionsUsed.Contains(randomPos));

        StartingPositionsUsed.Add(randomPos);

        obj.StartingPosition = randomPos;
        obj.gameObject.transform.localPosition = randomPos;
    }

    private void SetRandomDestination(SA_Object obj)
    {
        int randomIndex;
        Vector3 randomPos;
        do
        {
            randomIndex = Random.Range(0, Destinations.Count);
            randomPos = Destinations[randomIndex];
        }
        while (CurrentDestinations.Contains(randomPos));
        CurrentDestinations.Add(randomPos);
        obj.CurrentDestination = randomPos;
    }

    private void SetNewDestination(SA_Object obj)
    {
        float destinationDistance = 100f;
        float random = Random.Range(0, 2);
        float newY = random == 0 ? obj.Direction.y : -obj.Direction.y;
        Vector3 perpendicularDirection = new Vector3(newY, obj.Direction.x, 0);
        Vector3 newDestination = obj.transform.localPosition + perpendicularDirection.normalized * destinationDistance;

        //check if CurrentDestinations list includes newDestination (+/- a range i can specify)
        //if it is within the range, calculate a new destination

        float minDistance = 100f;
        bool destinationValid = true;
        List<Vector3> destinations = new List<Vector3>(CurrentDestinations);
        foreach(Vector3 existingDestination in destinations)
        {
            if (Vector3.Distance(newDestination, existingDestination) < minDistance)
            {
                destinationValid = false;
                break;
            }
        }

        if (destinationValid)
        {
            CurrentDestinations.Add(newDestination);
            obj.CurrentDestination = newDestination;
        }
        else
        {
            Debug.LogWarning("TOO CLOSE! GONNA RECURSE!");
            SetNewDestination(obj);
        }


        //CurrentDestinations.Add(newDestination);
        //obj.CurrentDestination = newDestination;

    }

    private void CalculateDestinations()
    {
        Destinations = new List<Vector3>();
        int[] xValues = new int[] { -150, 0, 150};
        int[] yValues = new int[] { 0 };
        //int[] xValues = new int[] { -750, -600, -450, -300, -150, 0, 150, 300, 450, 600, 750 };
        //int[] yValues = new int[] { 300, 150, 0, -150, -300, -450 };

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
    public float Speed;
    public float Interval;
    public int Reward;
    public Vector2 StartingPosition;
    public Vector3 CurrentDestination;
    public List<Vector3> Visited;
    public bool Move;
    public bool NeedsNewDestination;
    public bool IsColliding;
    public GameObject Marker;
    public Vector3 Direction;


    private void Awake()
    {
        Visited = new List<Vector3>();
    }

    public void Setup(bool isTarget, float speed, float interval, int reward)
    {
        IsTarget = isTarget;
        Speed = speed;
        Interval = interval;
        Reward = reward;

        SetupMarker(); //Marker for debugging purposes
    }

    private void Update()
    {
        if(Move)
        {
            MoveTowardsDestination();
            Marker.transform.localPosition = CurrentDestination;
        }
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IsColliding = true;
    }

    //private void OnCollisionExit2D(Collision2D collision)
    //{
    //    IsColliding = false;
    //}

    public bool AtDestination()
    {
        float distanceThreshold = 1f;
        float distanceToDestination = Vector3.Distance(transform.localPosition, CurrentDestination);
        return distanceToDestination <= distanceThreshold;
    }

    private void SetupMarker()
    {
        if (Marker != null)
            Destroy(Marker);
        Marker = Instantiate(Resources.Load<GameObject>("DestinationMarker"));
        Marker.transform.SetParent(GameObject.Find("SustainedAttention_Canvas").transform);
        Marker.transform.localScale = new Vector3(1f, 1f, 1f);
        Marker.transform.localPosition = CurrentDestination;
        if (!IsTarget)
            Marker.GetComponent<Image>().color = Color.red;
        Marker.SetActive(false);
    }
}
