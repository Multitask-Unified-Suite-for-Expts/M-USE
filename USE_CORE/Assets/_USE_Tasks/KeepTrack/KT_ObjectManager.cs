using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.UI;
using USE_ExperimentTemplate_Classes;
using Random = UnityEngine.Random;


public class KT_ObjectManager : MonoBehaviour
{
    public List<KT_Object> TargetList;
    public List<KT_Object> DistractorList;

    private Transform ObjectParent;

    public readonly Vector2 xRange = new Vector2(-800f, 800f);
    public readonly Vector2 yRange = new Vector2(-400f, 325f);

    public delegate void CycleEventHandler();
    public event CycleEventHandler OnTargetIntervalMissed;
    public event CycleEventHandler OnDistractorAvoided;

    public Dictionary<string, EventCode> TaskEventCodes; //task event codes passed in so can trigger "Object Animation Occured" event code

    public float MaxTouchDuration;

    public Vector2 MostRecentCollisionPoint;


    public void NoSelectionDuringInterval(KT_Object obj)
    {
        if (obj.IsTarget)
            OnTargetIntervalMissed?.Invoke();
        else
            OnDistractorAvoided?.Invoke();
    }

    private void Awake()
    {
        TargetList = new List<KT_Object>();
        DistractorList = new List<KT_Object>();
    }

    public void SetObjectParent(Transform parentTransform)
    {
        ObjectParent = parentTransform;
    }

    public void RemoveFromObjectList(KT_Object obj)
    {
        if (obj.IsTarget)
            TargetList.Remove(obj);
        else
            DistractorList.Remove(obj);
    }

    public void ActivateInitialObjectsMovement()
    {
        if (TargetList.Count > 0)
        {
            foreach (KT_Object target in TargetList)
            {
                if(target.ActivateDelay == 0)
                    target.ActivateMovement();
            }
        }

        if (DistractorList.Count > 0)
        {
            foreach (KT_Object distractor in DistractorList)
            {
                if(distractor.ActivateDelay == 0)
                    distractor.ActivateMovement();
            }
        }
    }

    public List<KT_Object> CreateObjects(List<KT_Object_ConfigValues> objects)
    {
        List<KT_Object> trialObjects = new List<KT_Object>();

        foreach(KT_Object_ConfigValues configValues in objects)
        {
            try
            {
                GameObject go = Instantiate(Resources.Load<GameObject>("PacmanCircle"));

                go.GetComponent<PacmanDrawer>().ManualStart();

                go.name = configValues.IsTarget ? $"Target" : $"Distractor";
                go.SetActive(false);
                go.transform.SetParent(ObjectParent);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(configValues.Size, configValues.Size);

                go.GetComponent<Image>().color = new Color(
                                                            configValues.ObjectColor[0] / 255f,
                                                            configValues.ObjectColor[1] / 255f,
                                                            configValues.ObjectColor[2] / 255f,
                                                            1f // Ensure alpha is fully opaque
                                                        );

                go.GetComponent<CircleCollider2D>().radius = configValues.Size * .567f; //Set Collider radius

                KT_Object obj = go.AddComponent<KT_Object>();
                obj.SetupObject(this, configValues);

                if (obj.IsTarget)
                    TargetList.Add(obj);
                else
                    DistractorList.Add(obj);

                trialObjects.Add(obj);
            }
            catch(Exception ex)
            {
                Debug.LogWarning("ERROR CREATING OBJECT WITH INDEX NUMBER " + configValues.Index + " | Error Message: " + ex.Message);
            }
        }

        return trialObjects;
    }
    

    public void AddToList(KT_Object obj)
    {
        if (obj.IsTarget)
            TargetList.Add(obj);
        else
            DistractorList.Add(obj);
    }

    public void DestroyExistingObjects()
    {
        List<KT_Object> targetListCopy = new List<KT_Object>(TargetList);
        List<KT_Object> distractorListCopy = new List<KT_Object>(DistractorList);

        foreach(KT_Object obj in targetListCopy)
        {
            if (obj != null)
                obj.DestroyObj();
        }

        foreach (KT_Object obj in distractorListCopy)
        {
            if (obj != null)
                obj.DestroyObj();
        }

        TargetList.Clear();
        DistractorList.Clear();
    }

    public void ActivateInitialTargets()
    {
        foreach (var target in TargetList)
        {
            if(target.ActivateDelay == 0)
                target.gameObject.SetActive(true);
        }
    }

    public void ActivateInitialDistractors()
    {
        foreach (var distractor in DistractorList)
        {
            if (distractor.ActivateDelay == 0)
                distractor.gameObject.SetActive(true);
        }
    }

    public void ActivateRemainingObjects()
    {
        foreach(var target in TargetList)
        {
            if (target.ActivateDelay == 0)
                continue;

            StartCoroutine(ActivateObjectCoroutine(target));
        }

        foreach (var distractor in DistractorList)
        {
            if (distractor.ActivateDelay == 0)
                continue;

            StartCoroutine(ActivateObjectCoroutine(distractor));
        }
    }

    public IEnumerator ActivateObjectCoroutine(KT_Object obj)
    {
        yield return new WaitForSeconds(obj.ActivateDelay);
        obj.gameObject.SetActive(true);
        obj.ActivateMovement();
    }

    //Not Used:
    public void DeactivateTargets()
    {
        foreach (var target in TargetList)
            target.gameObject.SetActive(false);
    }

    //Not Used:
    public void DeactivateDistractors()
    {
        foreach (var target in DistractorList)
            target.gameObject.SetActive(false);
    }

}



public class KT_Object : MonoBehaviour
{
    public KT_ObjectManager ObjManager;

    public PacmanDrawer PacmanDrawer;

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
    public Vector2 StartingPosition;
    public Vector2 ResponseWindow;
    public float CloseDuration;
    public int[] RewardPulsesBySec = null;
    public Vector2[] RatesAndDurations;
    public Vector3 AngleProbs;
    public Vector2[] AngleRanges;
    public int NumDestWithoutBigTurn;
    public float[] ObjectColor;
    public int SliderChange;
    public float ActivateDelay;
    public Vector2[] MouthAngles;

    public Vector3 CurrentDestination;
    public bool MoveAroundScreen;
    public bool ObjectSelected;
    public bool ObjectPaused;
    public float TimeOfPause;
    public GameObject Marker;
    public Vector3 Direction;
    private float NewDestStartTime;
    private readonly float MaxCollisionTime = .25f;
    public float AnimStartTime;
    public bool WithinDuration;
    private readonly List<float> PreviousAngleOffsets = new List<float>();
    public List<Cycle> Cycles;
    public Cycle CurrentCycle;

    public float CurrentMouthAngle;

    public enum AnimationStatus { Open, Closed };
    public AnimationStatus CurrentAnimationStatus = AnimationStatus.Open; //start as closed

    public float ActivationStartTime;

    private float RewardTimer;
    private float RewardInterval = 1f; //1 sec before going to next reward value

    public int CurrentRewardValue;


    public KT_Object()
    {
        Cycles = new List<Cycle>();
    }


    public void SetupObject(KT_ObjectManager objManager, KT_Object_ConfigValues configValue)
    {
        ObjManager = objManager;
        Index = configValue.Index;
        ObjectName = configValue.ObjectName;
        OpenAngle = configValue.OpenAngle;
        ClosedLineThickness = configValue.ClosedLineThickness;
        IsTarget = configValue.IsTarget;
        StartingPosition = configValue.StartingPosition;
        AngleProbs = configValue.AngleProbs;
        RotateTowardsDest = configValue.RotateTowardsDest;
        MinAnimGap = configValue.MinAnimGap;
        ResponseWindow = configValue.ResponseWindow;
        Speed = configValue.Speed;
        Size = configValue.Size;
        NextDestDist = configValue.NextDestDist;
        CloseDuration = configValue.CloseDuration;

        if (configValue.RewardPulsesBySec != null)
            RewardPulsesBySec = configValue.RewardPulsesBySec;
        else
            RewardPulsesBySec = null;

        RatesAndDurations = configValue.RatesAndDurations;
        ObjectColor = configValue.ObjectColor;
        SliderChange = configValue.SliderChange;
        AngleRanges = configValue.AngleRanges;
        NumDestWithoutBigTurn = configValue.NumDestWithoutBigTurn;
        MouthAngles = configValue.MouthAngles;

        ActivateDelay = configValue.ActivateDelay;

        CheckRewardsMatchDurations();

        foreach (var rateAndDur in RatesAndDurations)
        {
            Cycle cycle = new Cycle()
            {
                sa_Object = this,
                duration = rateAndDur.y,
                intervals = GenerateRandomIntervals((int)(rateAndDur.y * rateAndDur.x), rateAndDur.y)
            };
            Cycles.Add(cycle);
        }

        transform.localPosition = new Vector3(StartingPosition.x, StartingPosition.y, 0);

        SetNewDestination();

        SetupMarker(); //Marker for debugging purposes

        PacmanDrawer = gameObject.GetComponent<PacmanDrawer>();
        PacmanDrawer.ClosedLineThickness = ClosedLineThickness;
        PacmanDrawer.DrawMouth(OpenAngle);
    }

    private void CheckRewardsMatchDurations()
    {
        if (RewardPulsesBySec == null)
            return;

        float durationSum = RatesAndDurations.Sum(value => value.y);
        if (RewardPulsesBySec.Count() != durationSum)
            Debug.LogError("NUMBER OF REWARD SECONDS DOES NOT MATCH THE SUM OF ALL THE Y VALUES IN THE RatesAndDurations variable!");
    }

    List<float> GenerateRandomIntervals(int numIntervals, float duration)
    {
        List<float> randomFloats = new List<float>() { duration };  //add the ending number as a interval. 1) so last interval will end here, and 2) so that no randomly gen numbers below will be too close to the final value and thus subject may not have time to select before cycle ends. 

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
        if (MoveAroundScreen)
        {
            if (Time.time - CurrentCycle.cycleStartTime >= CurrentCycle.currentInterval && CurrentCycle.intervals.Count > 0)
            {
                StartCoroutine(AnimationCoroutine());
                CurrentCycle.NextInterval();
            }

            if (Time.time - CurrentCycle.cycleStartTime >= CurrentCycle.duration)
            {
                NextCycle();
            }

            WithinDuration = AnimStartTime > 0 && Time.time - AnimStartTime > ResponseWindow.x && Time.time - AnimStartTime <= ResponseWindow.y;

            HandlePausingWhileBeingSelected();
            HandleInput();

            //Call it every second instead of every frame:
            RewardTimer += Time.deltaTime;
            if (RewardTimer >= RewardInterval)
            {
                SetCurrentRewardValue();
                RewardTimer = 0;
            }

        }
    }

    private void SetCurrentRewardValue()
    {
        int timeSinceActivation_Rounded = Mathf.CeilToInt(Time.time - ActivationStartTime);

        if (RewardPulsesBySec != null && RewardPulsesBySec.Count() >= timeSinceActivation_Rounded)
        {
            CurrentRewardValue = RewardPulsesBySec[timeSinceActivation_Rounded - 1];
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

    private void SetCurrentOpenAngle()
    {
        if (MouthAngles == null || MouthAngles.Count() < 1)
            Debug.LogError("MOUTH ANGLES iS NULL OR EMPTY!");

        float randomValue = Random.value;
        float cumulative = 0f;

        foreach (Vector2 angleProb in MouthAngles)
        {
            cumulative += angleProb.y;
            if (randomValue <= cumulative)
            {
                CurrentMouthAngle = angleProb.x;
                return;
            }
        }
        CurrentMouthAngle = MouthAngles[MouthAngles.Length - 1].x; //If somehow no angle was selected(due to precision issues), return the last angle as a fallback
    }

    private IEnumerator AnimationCoroutine()
    {
        AnimStartTime = Time.time;
        Session.EventCodeManager.SendCodeThisFrame(ObjManager.TaskEventCodes["ObjectAnimationBegins"]);
        gameObject.GetComponent<PacmanDrawer>().DrawClosedMouth();
        CurrentAnimationStatus = AnimationStatus.Closed;
        yield return new WaitForSeconds(CloseDuration);
        SetCurrentOpenAngle();
        gameObject.GetComponent<PacmanDrawer>().DrawMouth(CurrentMouthAngle);
        CurrentAnimationStatus = AnimationStatus.Open;
    }

    private void HandlePausingWhileBeingSelected()
    {
        if (InputBroker.GetMouseButtonDown(0))
        {
            GameObject hit = InputBroker.RaycastBoth(InputBroker.mousePosition);
            if (hit != null && hit == gameObject)
            {
                ObjectSelected = true;
                ObjectPaused = true;
                TimeOfPause = Time.time;
            }
        }
        else if(ObjectPaused && (InputBroker.GetMouseButtonUp(0) || Time.time - TimeOfPause >= ObjManager.MaxTouchDuration + .1f)) //add .1 so the touch fb dissapears before object starts moving again
            ObjectPaused = false;

        //if(ObjectPaused && InputBroker.GetMouseButton(0))
        //    CurrentCycle.cycleStartTime += Time.deltaTime; //Push the cycle start time back while they're selecting.
        
        if (ObjectSelected && InputBroker.GetMouseButtonUp(0))
        {
            ObjectSelected = false;
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

        ActivationStartTime = Time.time;

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
        float xDiff = Mathf.Clamp(destination.x, ObjManager.xRange.x, ObjManager.xRange.y) - destination.x;
        float yDiff = Mathf.Clamp(destination.y, ObjManager.yRange.x, ObjManager.yRange.y) - destination.y;
        destination += new Vector3(xDiff, yDiff, 0);

        CurrentDestination = destination;
        NewDestStartTime = Time.time;

    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        //if(collision.contactCount > 0)
        //{
        //    foreach(ContactPoint2D contact in collision.contacts)
        //    {
        //        Vector2 collisionPoint = contact.point;

        //        if(collisionPoint != ObjManager.MostRecentCollisionPoint)
        //        {
        //            Debug.LogWarning("COLLISION AT POINT: " + collisionPoint);
        //            //send the event code here!
        //        }
        //        ObjManager.MostRecentCollisionPoint = collisionPoint;
        //    }
        //}

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
        Marker.transform.SetParent(GameObject.Find("KeepTrack_Canvas").transform);
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

//Class is used to read in KT Object Config values. Can't read in directly to KT_Object because that creates the gameobject. We need to create the gameobject after getting the object values.
public class KT_Object_ConfigValues
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
    public Vector2 StartingPosition;
    public float NextDestDist;
    public Vector2 ResponseWindow;
    public float CloseDuration;
    public int[] RewardPulsesBySec = null;
    public Vector2[] RatesAndDurations;
    public Vector3 AngleProbs;
    public Vector2[] AngleRanges;
    public int NumDestWithoutBigTurn;
    public float[] ObjectColor;
    public int SliderChange;
    public float ActivateDelay;
    public Vector2[] MouthAngles;


}

public class Cycle
{
    public KT_Object sa_Object;

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