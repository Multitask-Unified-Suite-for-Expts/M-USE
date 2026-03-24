using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class KT_ObjectManager : MonoBehaviour
{
    public List<KT_Object> TrialObjects;
    public List<KT_Object> TargetList;
    public List<KT_Object> DistractorList;

    private Transform ObjectParent;

    public readonly Vector2 xRange = new Vector2(-825f, 825f);
    public readonly Vector2 yRange = new Vector2(-425f, 325f); 

    public delegate void ObjectEventHandler();
    public event ObjectEventHandler OnTargetAnimationStarted;
    public event ObjectEventHandler OnDistractorAnimationStarted;
    public event ObjectEventHandler OnTargetMissed;
    public event ObjectEventHandler OnDistractorAvoided;
    public event ObjectEventHandler OnSectionChanged;

    private string Section = "";
    private float SectionChange_StartTime = 0f;

    public float MaxTouchDuration;

    public List<Vector2> StartingPositions = new List<Vector2>();

    public Queue<Vector2> StartingPositions_Queue = new Queue<Vector2>();

    public Dictionary<string, Sprite> SpriteDict;


    private void Awake()
    {
        TargetList = new List<KT_Object>();
        DistractorList = new List<KT_Object>();

        SpriteDict = new Dictionary<string, Sprite>();
        
        SetRandomStartingPositions();
    }

    private void Update()
    {
        CheckIfSectionChanged();
    }

    public float GetTimeSinceLastSectionChange()
    {
        return Time.time - SectionChange_StartTime;
    }

    private void CheckIfSectionChanged()
    {
        string sectionThisFrame = DetermineCurrentSection();

        if(Section != sectionThisFrame)
        {
            OnSectionChanged?.Invoke();
            SectionChange_StartTime = Time.time;
        }
        Section = sectionThisFrame;
    }

    private string DetermineCurrentSection()
    {
        if (TrialObjects == null)
            return "0T0D";

        int targetCount = 0;
        int distractorCount = 0;

        foreach (var obj in TrialObjects)
        {
            if (obj == null || !obj.gameObject.activeInHierarchy)
                continue;
            else
            {
                if (obj.IsTarget)
                    targetCount++;
                else
                    distractorCount++;
            }
        }

        return targetCount + "T" + distractorCount + "D";
    }

    public string GetSection()
    {
        return Section;
    }    


    public Vector2 GetRandomStartingPosition() //Already randomized at this point
    {
        Vector2 startPos = StartingPositions_Queue.Dequeue(); //grab latest start pos
        StartingPositions_Queue.Enqueue(startPos); //Put it back at the end of the queue
        return startPos;
    }

    public void SetRandomStartingPositions()
    {
        for(float x = -750; x <= 750f; x+= 250f)
        {
            for(float y = -350f; y <= 350f; y+= 175f)
            {
                StartingPositions.Add(new Vector2(x, y));
            }
        }

        Shuffle(StartingPositions);

        //Add to queue:
        foreach (Vector2 startPos in StartingPositions)
            StartingPositions_Queue.Enqueue(startPos);
    }

    private void Shuffle<T>(List<T> list)
    {
        if(list == null || list.Count < 1)
        {
            Debug.LogError("LIST IS NULL");
            return;
        }

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1); // inclusive upper bound
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void AnimationStarted(KT_Object obj)
    {
        if (obj.IsTarget)
            OnTargetAnimationStarted?.Invoke();
        else
            OnDistractorAnimationStarted?.Invoke();

    }

    public void NoSelectionDuringResponseWindow(KT_Object obj)
    {
        if (obj.IsTarget)
        {
            OnTargetMissed?.Invoke();
        }
        else
            OnDistractorAvoided?.Invoke();
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



    public List<KT_Object> CreateObjects(List<KT_Object_ConfigValues> objects)
    {
        if (objects == null)
            Debug.LogError("OBJECTS ARE NULL");

        TrialObjects = new List<KT_Object>();

        foreach(KT_Object_ConfigValues configValues in objects)
        {
            try
            {
                GameObject go = Instantiate(Resources.Load<GameObject>("Pacman")); //start with default prefab

                go.GetComponent<PacmanDrawer>().ManualStart();

                go.name = configValues.IsTarget ? $"Target_" + configValues.Index : $"Distractor_" + configValues.Index;
                go.SetActive(false);

                go.transform.SetParent(ObjectParent);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(configValues.Size, configValues.Size);
                go.GetComponent<CircleCollider2D>().radius = configValues.Size * .52f; //Set Collider radius

                KT_Object obj = go.AddComponent<KT_Object>();
                obj.SetupObject(this, configValues);

                if (obj.IsTarget)
                    TargetList.Add(obj);
                else
                    DistractorList.Add(obj);

                TrialObjects.Add(obj);
            }
            catch(Exception ex)
            {
                Debug.LogWarning("ERROR CREATING OBJECT WITH INDEX NUMBER " + configValues.Index + " | Error Message: " + ex.Message);
            }
        }
        return TrialObjects;
    }


    public IEnumerator ChangeObjColorCoroutine(KT_Object obj, Vector3 changeToColor, float duration)
    {
        if (obj == null)
            Debug.LogError("OBJ IS NULL");

        if (obj.TryGetComponent<Image>(out var objImage))
            objImage.color = new Color(changeToColor[0] / 255f, changeToColor[1] / 255f, changeToColor[2] / 255f, obj.Opacity);
        else
            Debug.LogError("IMAGE COMPONENT IS NULL");
        
        yield return new WaitForSeconds(duration);

        if (objImage != null)
        {
            if (obj.usePacman)
                objImage.color = new Color(obj.ObjectColor[0] / 255f, obj.ObjectColor[1] / 255f, obj.ObjectColor[2] / 255f, obj.Opacity);
            else
                objImage.color = new Color(255f, 255f, 255f, obj.Opacity); //not using pacman
        }
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
        foreach (var obj in TargetList)
        {
            if(obj.ActivateDelay == 0)
            {
                obj.gameObject.SetActive(true);
            }
        }
    }

    public void ActivateInitialDistractors()
    {
        foreach (var obj in DistractorList)
        {
            if (obj.ActivateDelay == 0)
            {
                obj.gameObject.SetActive(true);
            }
        }
    }

    public void ActivateInitialObjectsMovement()
    {
        if (TargetList.Count > 0)
        {
            foreach (KT_Object target in TargetList)
            {
                if (target.ActivateDelay == 0)
                    target.ActivateMovement();
            }
        }

        if (DistractorList.Count > 0)
        {
            foreach (KT_Object distractor in DistractorList)
            {
                if (distractor.ActivateDelay == 0)
                    distractor.ActivateMovement();
            }
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
        StartCoroutine(obj.Fade(0f, 1f, obj.ActivateMovement));
    }


}




public class KT_Object : MonoBehaviour
{
    public KT_ObjectManager ObjManager;

    public PacmanDrawer PacmanDrawer;

    public bool usePacman
    {
        get
        {
            return OpenSpriteName == "Pacman" || ClosedSpriteName == "Pacman";
        }
    }


    //From Object Config:
    public int Index;
    public string ObjectName;
    public string ClosedSpriteName;
    public string OpenSpriteName;
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
    public Vector3 ObjectColor;
    public float Opacity;
    public int SliderChange;
    public int ActivateDelay;
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
    public float IntervalStartTime;
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

    public bool CurrentIntervalSuccessful = false;
    public bool SelectedDuringCurrentInterval = false;


    public bool MissedCurrentIntervalResponseWindow = false;


    public float FadeDuration = .15f;

    public Image ImageComponent;

    public bool Fading = false;

    public bool BeforeResponseWindow;
    public bool WithinResponseWindow;
    public bool AfterResponseWindow;

    public Sprite ClosedSprite;
    public Sprite OpenSprite;

    private float LastCollisionResponseTime = -999f;
    private readonly float CollisionResponseCooldown = 0.05f;



    public KT_Object()
    {
        Cycles = new List<Cycle>();
    }


    public void SetupObject(KT_ObjectManager objManager, KT_Object_ConfigValues configValue)
    {
        ObjManager = objManager;
        Index = configValue.Index;
        ObjectName = configValue.ObjectName;
        ClosedSpriteName = configValue.ClosedSpriteName;
        OpenSpriteName = configValue.OpenSpriteName;
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

        if (configValue.RewardPulsesBySec != null)
            RewardPulsesBySec = configValue.RewardPulsesBySec;
        else
            RewardPulsesBySec = null;

        RatesAndDurations = configValue.RatesAndDurations;
        ObjectColor = configValue.ObjectColor;
        Opacity = configValue.Opacity;
        SliderChange = configValue.SliderChange;
        AngleRanges = configValue.AngleRanges;
        NumDestWithoutBigTurn = configValue.NumDestWithoutBigTurn;
        MouthAngles = configValue.MouthAngles;
        ActivateDelay = configValue.ActivateDelay;

        if (!TryGetComponent<Image>(out ImageComponent))
            Debug.LogError("IMAGE COMPONENT IS NULL");

        if (OpenSpriteName != null)
        {
            OpenSprite = GetSprite(OpenSpriteName);
            if(OpenSprite != null) //It starts as pacman, so if its null here then dont have to do anything
                ImageComponent.sprite = OpenSprite; //Start Object with Closed sprite (
        }
        if(ClosedSpriteName != null)
        {
            ClosedSprite = GetSprite(ClosedSpriteName);
        }
        
        if (usePacman)
        {
            GetComponent<Image>().color = new Color(ObjectColor[0] / 255f, ObjectColor[1] / 255f, ObjectColor[2] / 255f, Opacity);
        }
        else
        {
            Color color = GetComponent<Image>().color;
            color.a = Opacity;
            GetComponent<Image>().color = color;
        }

        StartingPosition = ObjManager.GetRandomStartingPosition();
        transform.localPosition = new Vector3(StartingPosition.x, StartingPosition.y, 0);

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

        SetNewDestination();

        SetupMarker(); //Uncomment when want to use the Marker for debugging purposes

        PacmanDrawer = gameObject.GetComponent<PacmanDrawer>();
        PacmanDrawer.ClosedLineThickness = ClosedLineThickness;

        if(usePacman)
        {
            PacmanDrawer.DrawMouth(OpenAngle);
        }
    }

    public Sprite GetSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return null;

        if (ObjManager == null || ObjManager.SpriteDict == null)
        {
            Debug.LogError("ObjManager or SpriteDict is null.");
            return null;
        }

        if (ObjManager.SpriteDict.TryGetValue(spriteName, out Sprite sprite) && sprite != null)
        {
            return sprite;
        }

        Sprite loaded = Resources.Load<Sprite>("Object_Sprites/" + spriteName);
        if (loaded == null)
        {
            if(!usePacman)
            {
                Debug.LogError($"NOT USING PACMAN & FAILED TO LOAD SPRITE '{spriteName}' FROM RESOURCES/ Object_Sprites/");
            }
            return null;
        }

        ObjManager.SpriteDict[spriteName] = loaded;
        return loaded;
    }

    public IEnumerator Fade(float startAlpha, float endAlpha, Action onComplete)
    {
        if(ImageComponent != null)
        {
            Fading = true;

            Color color = ImageComponent.color;
            color.a = Mathf.Clamp01(startAlpha);
            ImageComponent.color = color;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            float timeElapsed = 0f;

            while (timeElapsed < FadeDuration)
            {
                timeElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(timeElapsed / FadeDuration);
                color.a = Mathf.Lerp(startAlpha, endAlpha, t);
                ImageComponent.color = color;
                yield return null;
            }

            color.a = Mathf.Clamp01(endAlpha);
            ImageComponent.color = color;

            Fading = false;

            onComplete?.Invoke();
        }
    }

    private void CheckRewardsMatchDurations()
    {
        if (RewardPulsesBySec == null)
            return;

        float durationSum = RatesAndDurations.Sum(value => value.y);
        if (RewardPulsesBySec.Count() != durationSum)
            Debug.Log("NUMBER OF REWARD SECONDS DOES NOT MATCH THE SUM OF ALL THE Y VALUES IN THE RatesAndDurations variable!");
    }

    List<float> GenerateRandomIntervals(int numIntervals, float duration)
    {
        List<float> randomFloats = new List<float>() { duration };

        // FALLBACK: Check if it's even possible to fit the intervals
        float minPossibleDuration = numIntervals * MinAnimGap;

        if (minPossibleDuration > duration)
        {
            Debug.LogWarning("MIN POSSIBLE DURATION GREATER THAN DURATION");

            // Fallback: Generate evenly spaced intervals
            float interval = duration / (numIntervals + 1);

            for (int i = 1; i <= numIntervals; i++)
            {
                randomFloats.Add(i * interval);
            }

            randomFloats.Sort();

            return randomFloats;
        }

        // NORMAL: Try to generate random intervals
        int maxAttempts = 1000;

        for (int i = 0; i < numIntervals; i++)
        {
            float randomValue;
            int attempts = 0;
            bool foundValid = false;

            do
            {
                randomValue = Random.Range(0f, duration);
                attempts++;

                // Check if this value is far enough from all existing values
                bool tooClose = randomFloats.Any(value => Mathf.Abs(value - randomValue) < MinAnimGap);

                if (!tooClose)
                {
                    foundValid = true;
                    break;
                }

                if (attempts >= maxAttempts)
                {
                    // Fallback: Fill remaining with evenly spaced intervals
                    randomFloats.Sort();
                    float lastValue = randomFloats[randomFloats.Count - 1];
                    int remaining = numIntervals - i;
                    float remainingDuration = duration - lastValue;
                    float spacing = remainingDuration / (remaining + 1);

                    for (int j = 1; j <= remaining; j++)
                    {
                        randomFloats.Add(lastValue + j * spacing);
                    }

                    randomFloats.Sort();
                    return randomFloats;
                }

            } while (!foundValid);

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
        {
            StartCoroutine(Fade(1f, 0f, DestroyObj));
        }
    }


    private void Update()
    {
        BeforeResponseWindow = IntervalStartTime > 0 && Time.time - IntervalStartTime < ResponseWindow.x;
        WithinResponseWindow = IntervalStartTime > 0 && Time.time - IntervalStartTime > ResponseWindow.x && Time.time - IntervalStartTime <= ResponseWindow.y;
        AfterResponseWindow = IntervalStartTime > 0 && Time.time - IntervalStartTime >= ResponseWindow.y;

        //See if missed the window
        if (IntervalStartTime > 0 && !MissedCurrentIntervalResponseWindow)
        {
            if (Time.time - IntervalStartTime >= ResponseWindow.y)
            {
                MissedCurrentIntervalResponseWindow = true;

                if(!CurrentIntervalSuccessful) //wont ever be true for distractors (fine, controlled by trial level)
                    ObjManager.NoSelectionDuringResponseWindow(this); //Trigger event so data can be incremented
            }
        }

        if (MoveAroundScreen && !CurrentCycle.CycleComplete)
        {
            if (Time.time - CurrentCycle.cycleStartTime >= CurrentCycle.duration)
            {
                CurrentCycle.CycleComplete = true;
                NextCycle();
            }
            else if (Time.time - CurrentCycle.cycleStartTime >= CurrentCycle.currentInterval && CurrentCycle.intervals.Count > 0)
            {
                StartCoroutine(AnimationCoroutine());
                CurrentCycle.NextInterval();
            }
            

        }

        HandlePausingWhileBeingSelected();
        HandleInput();

        RewardTimer += Time.deltaTime;
        if (RewardTimer >= RewardInterval)
        {
            SetCurrentRewardValue();
            RewardTimer = 0;
        }
        
    }

    private void FixedUpdate()
    {
        if (MoveAroundScreen && !ObjectPaused)
        {
            if (AtDestination())
                SetNewDestination();

            MoveTowardsDestination();

            if (RotateTowardsDest)
                RotateTowardsDirection();

            if (Marker != null)
                Marker.transform.localPosition = CurrentDestination;
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
        ObjManager.AnimationStarted(this);

        if(usePacman)
            gameObject.GetComponent<PacmanDrawer>().DrawClosedMouth();
        else
            ImageComponent.sprite = ClosedSprite;
        
        CurrentAnimationStatus = AnimationStatus.Closed;
        yield return new WaitForSeconds(CloseDuration);

        if(usePacman)
        {
            SetCurrentOpenAngle();
            gameObject.GetComponent<PacmanDrawer>().DrawMouth(CurrentMouthAngle);
        }
        else
        {
            ImageComponent.sprite = OpenSprite;
        }
        CurrentAnimationStatus = AnimationStatus.Open;
    }



    private void HandlePausingWhileBeingSelected()
    {
        if (InputBroker.GetMouseButtonDown(0))
        {
            GameObject hit = InputBroker.SimpleRaycast(InputBroker.mousePosition);
            if (hit != null && hit == gameObject)
            {
                ObjectSelected = true;
                ObjectPaused = true;
                TimeOfPause = Time.time;
            }
        }
        else if(ObjectPaused && (InputBroker.GetMouseButtonUp(0) || Time.time - TimeOfPause >= ObjManager.MaxTouchDuration)) //removed.... (add .1 so the touch fb dissapears before object starts moving again)
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
        IntervalStartTime = 0; //used to be Time.time 
        CurrentCycle = Cycles[0];
        CurrentCycle.StartCycle();
    }

    private void MoveTowardsDestination()
    {
        Vector3 currentPos = transform.localPosition;
        Vector3 targetPos = CurrentDestination;

        Vector3 toTarget = targetPos - currentPos;
        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        Direction = toTarget.normalized;

        Vector3 nextPos = Vector3.MoveTowards(
            currentPos,
            targetPos,
            Speed * Time.fixedDeltaTime
        );

        nextPos.x = Mathf.Round(nextPos.x);
        nextPos.y = Mathf.Round(nextPos.y);

        transform.localPosition = nextPos;

    }

    private void RotateTowardsDirection()
    {
        if (Direction.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        //My old way:
        //float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        //transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public void SetNewDestination()
    {
        float currentAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;

        float randomChance = Random.value;
        float angleOffset;

        Vector2 smallRange = AngleRanges[0];
        Vector2 mediumRange = AngleRanges[1];
        Vector2 largeRange = AngleRanges[2];

        float smallChance = AngleProbs.x;
        float mediumChance = AngleProbs.x + AngleProbs.y;

        if (randomChance < smallChance)
            angleOffset = Random.Range(smallRange.x, smallRange.y);
        else if (randomChance < mediumChance)
            angleOffset = Random.Range(mediumRange.x, mediumRange.y);
        else
            angleOffset = Random.Range(largeRange.x, largeRange.y);

        if (PreviousAngleOffsets.Count > NumDestWithoutBigTurn)
            PreviousAngleOffsets.RemoveAt(0);

        if (angleOffset > 90f)
        {
            foreach (float offset in PreviousAngleOffsets)
            {
                if (Mathf.Abs(offset) > 90f)
                {
                    angleOffset = Random.Range(0f, 46f);
                    break;
                }
            }
        }

        if (Random.value < 0.5f)
            angleOffset = -angleOffset;

        PreviousAngleOffsets.Add(angleOffset);

        currentAngle += angleOffset;

        float radianAngle = currentAngle * Mathf.Deg2Rad;
        Vector3 change = new Vector3(Mathf.Cos(radianAngle), Mathf.Sin(radianAngle), 0f);

        Vector3 destination = transform.localPosition + change * NextDestDist;

        destination.x = Mathf.Clamp(destination.x, ObjManager.xRange.x, ObjManager.xRange.y);
        destination.y = Mathf.Clamp(destination.y, ObjManager.yRange.x, ObjManager.yRange.y);

        CurrentDestination = destination;
        NewDestStartTime = Time.time;
    }



    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contactCount > 0)
        {
            // Get the collision normal (perpendicular to the surface)
            Vector2 collisionNormal = collision.contacts[0].normal;
            
            // Check if we hit another KT_Object (pacman-to-pacman collision)
            KT_Object otherPacman = collision.gameObject.GetComponent<KT_Object>();
            
            if (otherPacman != null)
            {
                // For pacman collisions: normal is the line between centers
                Vector3 centerToCenter = (transform.localPosition - otherPacman.transform.localPosition).normalized;
                collisionNormal = new Vector2(centerToCenter.x, centerToCenter.y);
            }

            // Store old direction
            Vector2 oldDirection = new Vector2(Direction.x, Direction.y);
            
            // Apply realistic reflection physics
            Vector2 reflectedDirection = Vector2.Reflect(oldDirection, collisionNormal);
            Direction = new Vector3(reflectedDirection.x, reflectedDirection.y, 0);
            
            
            SetNewDestination();
        }
        else if(!IsTarget)
        {
            Direction = -Direction;
            SetNewDestination();
        }

    }


    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time - NewDestStartTime >= MaxCollisionTime)
        {            
            if (collision.contactCount > 0)
            {
                Vector2 collisionNormal = collision.contacts[0].normal;
                Vector2 oldDirection = new Vector2(Direction.x, Direction.y);
                Vector2 reflectedDirection = Vector2.Reflect(oldDirection, collisionNormal);
                Direction = new Vector3(reflectedDirection.x, reflectedDirection.y, 0);
            }
            else
            {
                Direction = -Direction;
            }
            SetNewDestination();
        }
    }
  

    public bool AtDestination()
    {
        float distanceThreshold = 5f;
        float distanceToDestination = Vector3.Distance(transform.localPosition, CurrentDestination);

        return distanceToDestination <= distanceThreshold;
    }

 
     public void DestroyObj()
    {
        if (ObjManager != null)
            ObjManager.RemoveFromObjectList(this);

        if (gameObject != null)
            Destroy(gameObject);

        if (Marker != null)
            Destroy(Marker);

        Destroy(this);

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
    public string ClosedSpriteName;
    public string OpenSpriteName;
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
    public int[] RewardPulsesBySec = null;
    public Vector2[] RatesAndDurations;
    public Vector3 AngleProbs;
    public Vector2[] AngleRanges;
    public int NumDestWithoutBigTurn;
    public Vector3 ObjectColor;
    public float Opacity;
    public int SliderChange;
    public int ActivateDelay;
    public Vector2[] MouthAngles;


}

public class Cycle
{
    public KT_Object sa_Object;

    public float duration;
    public List<float> intervals;
    public float currentInterval;
    public float cycleStartTime;

    public bool CycleComplete = false;

    public bool AfterFirstAnimation = false;


    public void StartCycle()
    {
        currentInterval = intervals[0];
        cycleStartTime = Time.time;
        AfterFirstAnimation = false;
        CycleComplete = false;
    }

    public void NextInterval()
    {
        sa_Object.IntervalStartTime = Time.time;

        sa_Object.SelectedDuringCurrentInterval = false;
        sa_Object.MissedCurrentIntervalResponseWindow = false; //reset since start of next interval
        sa_Object.CurrentIntervalSuccessful = false;

        intervals.RemoveAt(0);
        if (intervals.Count > 0)
            currentInterval = intervals[0];


        if (!AfterFirstAnimation)
        {
            AfterFirstAnimation = true;
        }
    }

}