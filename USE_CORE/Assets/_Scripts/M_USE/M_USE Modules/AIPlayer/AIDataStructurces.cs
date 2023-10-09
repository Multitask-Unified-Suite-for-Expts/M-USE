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