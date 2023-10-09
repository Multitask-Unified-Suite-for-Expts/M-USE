/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



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