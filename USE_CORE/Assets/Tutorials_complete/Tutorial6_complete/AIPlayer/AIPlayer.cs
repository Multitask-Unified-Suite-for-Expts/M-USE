using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIPlayer{

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
                yield return StartCoroutine(next());
                int action = Random.Range(0, this.getActionSize());
                yield return StartCoroutine(act(action));
            }
        }
    }
}