using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using USE_States;
using AIPlayer;

[System.Serializable]
public class AIInterfaceTutorial10 : AIInterface
{
    public ControlLevel_Trial_Tutorial10_complete seq;
    public ControlLevel_Block_Tutorial10_complete seqBlock;
    public ControlLevel_Main_Tutorial10_complete seqMain;
    public Camera mainCamera;
    bool isTrialStarted = false;
    bool isGoPeriodStarted = false;
    bool isRewardEpoch = false;
    bool isBlockEnded = false;
    bool isExperimentEnded = false;
    bool isBlockEndCheked = false;
    bool isExperimentEndCheked = false;
    float reward = 0;
    bool useScreenshot;
    string screenshot_path;
    List<Stim> ls = new List<Stim>();
    List<Vector3> lv = new List<Vector3>();

    public UnityEvent CallOnStart;
    public GameObject[] disableOnAwake;
    public Camera[] disableCamerasOnAwake;

    void Awake()
    {
        ControlLevel.mainLevelSpecified = false;
        foreach (GameObject g in disableOnAwake)
            g.SetActive(false);
        foreach (Camera c in disableCamerasOnAwake)
            c.enabled = false;

        seqMain.quitApplicationAtEnd = false;
    }
    public override int getActionSize()
    {
        return 2;
    }
    public override IEnumerator reset(bool useScreenshot, string screenshot_path)
    {
        this.useScreenshot = useScreenshot;
        this.screenshot_path = screenshot_path;
        Debug.Log("Reseting the AI Player");
        CallOnStart.Invoke();
        this.setup();

        Debug.Log("wait till the next trial is set");
        // wait till the start of next trial, and then set the observation to the new trial configuration
        while (!isTrialStarted)
        {
            yield return new WaitForEndOfFrame();
        }
        yield return 0;
    }
    public override IEnumerator step(Observation outObservation)
    {
        seq.startTrial = true;
        yield return StartCoroutine(setObservation(outObservation));
    }

    void insertStimSorted(Stim s)
    {
        var v = getScreenPointForObject(s.gameObject);
        int k = ls.Count;
        for (int i = 0; i < ls.Count; i++)
        {
            if (lv[i].x < v.x)
            {
                k = i;
                break;
            }
        }
        ls.Insert(k, s);
        lv.Insert(k, v);
    }
    IEnumerator setObservation(Observation o)
    {
        yield return 0;
        Debug.Log("wait till the start of next trial, and then set the observation to the new trial configuration");
        // wait till the start of next trial, and then set the observation to the new trial configuration
        while (!isTrialStarted)
        {
            yield return new WaitForEndOfFrame();
        }
        isTrialStarted = false;

        Debug.Log("wait till the GO epoch, and then take the given action");
        while (!isGoPeriodStarted)
        {
            yield return new WaitForEndOfFrame();
        }
        isGoPeriodStarted = false;
        InputBroker.mousePosition = new Vector2(0, 0);
        yield return new WaitForEndOfFrame();
        // populate the Observation data structure
        // set the the observation according to the configuration of the trial
        ls.Clear();
        lv.Clear();

        foreach (Stim s in seq.stims)
        {
            insertStimSorted(s);
        }

        o.vector = new List<float>();
        foreach (Stim s in ls)
        {
            foreach (int fv in s.featureValues)
            {
                o.vector.Add(fv);
            }
        }
        if (useScreenshot)
        {
            var image = getScreenshot();
            o.screenshot_path = System.IO.Path.GetFullPath(screenshot_path);
        }
        Debug.Log("Observation:" + o.vector);
    }

    byte[] getScreenshot()
    {
        int width = Screen.width;
        int height = Screen.height;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        // Read screen contents into the texture
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // Encode texture into PNG
        byte[] bytes = tex.EncodeToJPG();
        Destroy(tex);
        System.IO.File.WriteAllBytes(screenshot_path, bytes);
        Debug.Log("screenshot written to path: " + screenshot_path);
        return bytes;
    }
    public override IEnumerator act(int action, StepResult stepResult)
    {
        Debug.Log("action:" + action);
        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(SelectObject(ls[action].gameObject));

        // wait till reward period, and read the reward
        Debug.Log("wait till reward period, and read the reward");
        while (!isRewardEpoch)
        {
            yield return new WaitForEndOfFrame();
        }
        isRewardEpoch = false;
        stepResult.reward = reward;

        // wait till block end check is done
        while (!isBlockEndCheked)
        {
            yield return new WaitForEndOfFrame();
        }
        while (!isExperimentEndCheked)
        {
            yield return new WaitForEndOfFrame();
        }

        stepResult.isTrialEnd = true;
        stepResult.isBlockEnd = isBlockEnded;
        stepResult.isExperimentEnd = isExperimentEnded;
        isBlockEndCheked = false;
        isExperimentEndCheked = false;
    }
    void setup()
    {
        Debug.Log("Setting AI Player for FLU");
        InputBroker.isSimulation = true;
        seq.startTrial = false;
        seq.OnStartTrial += () =>
        {
            Debug.Log("OnStartTrial");
            this.isTrialStarted = true;
        };
        seq.OnGoPeriod += () =>
        {
            Debug.Log("OnGoPeriod");
            this.isGoPeriodStarted = true;
        };
        seq.OnReward += (reward) =>
        {
            Debug.Log("OnReward, with reward:" + reward);
            this.reward = reward;
            this.isRewardEpoch = true;
            seq.startTrial = false;
        };
        seqBlock.OnBlockEnd += (ended) =>
        {
            Debug.Log("Block ended: " + ended);
            this.isBlockEnded = ended;
            isBlockEndCheked = true;
        };
        seqMain.OnExperimentEnd += (ended) =>
        {
            Debug.Log("Experiment ended: " + ended);
            this.isExperimentEnded = ended;
            isExperimentEndCheked = true;
        };
        seq.OnAbortTrial += (abortCode) =>
        {
            StartCoroutine(HandleTrialAborted(abortCode));
        };
    }

    IEnumerator HandleTrialAborted(int abortCode)
    {
        Debug.Log("trial aborted, wait till the start of next trial");
        // wait till the start of next trial for it to be ready to send observation upon request
        while (!isTrialStarted)
        {
            yield return new WaitForEndOfFrame();
        }
        this.AbortTrial(abortCode);
    }

    IEnumerator SelectObject(GameObject g)
    {
        Debug.Log("selecting object");
        InputBroker.mousePosition = getScreenPointForObject(g);
        // if (!Application.isEditor)
        // {
        //     InputBroker.mousePosition = InputBroker.mousePosition + new Vector3(Screen.width, 0f, 0f);
        // }
        yield return StartCoroutine(InputBroker.ClickMouseButton(0));
        Debug.Log("object selected");
    }

    public Vector2 getScreenPointForObject(GameObject g)
    {
        return mainCamera.WorldToScreenPoint(g.transform.position);
    }
}
