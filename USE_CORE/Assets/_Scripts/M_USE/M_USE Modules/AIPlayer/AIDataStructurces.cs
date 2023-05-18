using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AIPlayer{

    
    [System.Serializable]
    public class Observation{
        public List<float> vector = null;
        public string screenshot_path = null;
    }
    [System.Serializable]
    public class StepResult{
        public float reward = 0;
        public bool isBlockEnd = false;
        public bool isTrialEnd = false;
        public bool isExperimentEnd = false;

        public StepResult(float reward){
            this.reward = reward;
            this.isTrialEnd = false;
            this.isExperimentEnd = false;
            this.isBlockEnd = false;
        }
    }

    public abstract class AIInterface:MonoBehaviour{
        public event System.Action<int> OnAbortTrial;

        public void AbortTrial(int abortCode){
            if(OnAbortTrial != null)
                OnAbortTrial.Invoke(abortCode);
        }
        public abstract int getActionSize();
        public abstract IEnumerator reset(bool useScreenshot, string screenshot_path="screenshot.jpg");
        public abstract IEnumerator step(Observation outObservation);
        public abstract IEnumerator act(int action, StepResult outStepResult);
    }
}