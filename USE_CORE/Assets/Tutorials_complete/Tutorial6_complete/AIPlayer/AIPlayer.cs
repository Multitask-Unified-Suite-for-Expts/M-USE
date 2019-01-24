using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        public StepResult(float reward, bool isDone){
            this.reward = reward;
            
            this.isTrialEnd = false;
            this.isExperimentEnd = false;
            this.isBlockEnd = false;
        }
    }

    public abstract class TaskInterface:MonoBehaviour{
        public event System.Action<int> OnAbortTrial;

        public void AbortTrial(int abortCode){
            OnAbortTrial.Invoke(abortCode);
        }
        public abstract int getActionSize();
        public abstract IEnumerator reset(bool useScreenshot, string screenshot_path="screenshot.jpg");
        public abstract IEnumerator next(Observation outObservation);
        public abstract IEnumerator act(int action, StepResult outStepResult);
    }

    public class AIPlayer:MonoBehaviour {
        public bool DEBUG = false;
        bool loadedScene = false;
        public StepResult stepResult;
        public Observation observation;

        public TaskInterface task;

        // Config Variables
        public string NameTaskScene;
        public IEnumerator reset(bool useScreenshot = false, string screenshot_path="screenshot.jpg"){
            AsyncOperation asyncLoad = null;
            if(loadedScene){
                asyncLoad = SceneManager.UnloadSceneAsync(NameTaskScene);
                while (!asyncLoad.isDone)
                {
                    yield return null;
                }
            }
            asyncLoad = SceneManager.LoadSceneAsync(NameTaskScene, LoadSceneMode.Additive);
            loadedScene = true;
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            task = GameObject.FindObjectsOfType<TaskInterface>()[0];
            yield return StartCoroutine(task.reset(useScreenshot, screenshot_path));
        }

        public IEnumerator next(){
            this.observation = new Observation();
            yield return StartCoroutine(task.next(observation));
        }

        public IEnumerator act(int action){
            this.stepResult = new StepResult(0, false);
            yield return StartCoroutine(task.act(action, stepResult));
        }

        public int getActionSize(){
            return task.getActionSize();
        }

        void Start(){
            if(DEBUG){
                StartCoroutine(TestPlay());                
            }
        }

        IEnumerator TestPlay(){
            yield return StartCoroutine(reset());
            while(true){
                int action = Random.Range(0, this.getActionSize());
                yield return StartCoroutine(act(action));
            }
        }

    }
}