using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIPlayer{

    public class AIPlayer:MonoBehaviour {
        public bool DEBUG = false;
        bool loadedScene = false;
        [HideInInspector]
        public StepResult stepResult;
        [HideInInspector]
        public Observation observation;
        [HideInInspector]
        public AIInterface task;

        // Config Variables
        public string TaskSceneName;
        public IEnumerator reset(bool useScreenshot = false, string screenshot_path="screenshot.jpg"){
            AsyncOperation asyncLoad = null;
            if(loadedScene){
                asyncLoad = SceneManager.UnloadSceneAsync(TaskSceneName);
                while (!asyncLoad.isDone)
                {
                    yield return null;
                }
            }
            asyncLoad = SceneManager.LoadSceneAsync(TaskSceneName, LoadSceneMode.Additive);
            loadedScene = true;
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            task = GameObject.FindObjectsOfType<AIInterface>()[0];
            yield return StartCoroutine(task.reset(useScreenshot, screenshot_path));
        }

        public IEnumerator step(){
            this.observation = new Observation();
            yield return StartCoroutine(task.step(observation));
        }

        public IEnumerator act(int action){
            this.stepResult = new StepResult(0);
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
                yield return StartCoroutine(step());
                int action = Random.Range(0, this.getActionSize());
                yield return StartCoroutine(act(action));
            }
        }
    }
}