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



using System.Collections.Generic;
using UnityEngine;
using USE_Data;


public class GazeTracker : InputTracker
{

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("GazePosition", () => InputBroker.gazePosition != null ? InputBroker.gazePosition : new Vector2(float.NaN, float.NaN));
        frameData.AddDatum("SimpleRaycastTarget", () => SimpleRaycastTarget != null ? SimpleRaycastTarget.name : null);
        frameData.AddDatum("ShotgunModalTarget", () => ShotgunRaycastTarget != null ? ShotgunRaycastTarget.name : null);
    }


    public override void FindCurrentTarget()
    {
        CurrentInputScreenPosition = InputBroker.gazePosition;

        if (Camera.main == null)
        {
            Debug.LogError("MAIN CAMERA NULL");
            return;
        }

        if (CurrentInputScreenPosition.Value.x < 0 || CurrentInputScreenPosition.Value.y < 0 || CurrentInputScreenPosition.Value.x > Screen.width || CurrentInputScreenPosition.Value.y > Screen.height
        || float.IsNaN(CurrentInputScreenPosition.Value.x) || float.IsNaN(CurrentInputScreenPosition.Value.y) || float.IsNaN(CurrentInputScreenPosition.Value.z))
        {
            CurrentInputScreenPosition = null;
        }

        if (CurrentInputScreenPosition != null)
        {
            SimpleRaycastTarget = InputBroker.SimpleRaycast(CurrentInputScreenPosition.Value); //Normal raycast

            if (UsingShotgunHandler)
            {
                if (SimpleRaycastTarget != null)
                    ShotgunRaycastTarget = SimpleRaycastTarget; //If hit an object directly, set shotgun value as well
                else
                    ShotgunRaycastTarget = InputBroker.ShotgunRaycast(CurrentInputScreenPosition.Value);
            }
            //else
            //{
            //    Debug.LogWarning("NOT USING SHOTGUN HANDLER !!!! THIS IS DEFINITELY THE PROBLEM!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            //    return;
            //}

            //if (ShotgunRaycastTarget != null)
            //    Debug.LogWarning("SCREEN POS: " + CurrentInputScreenPosition.Value + " | SHOTGUN TARGET: " + ShotgunRaycastTarget.name);
            //else
            //    Debug.LogWarning("SCREEN POS: " + CurrentInputScreenPosition.Value + " | SHOTGUN TARGET IS NULL!!!!!!!!!!!!!!!!!!");

            if (ShotgunRaycastTarget != null)
                Debug.LogWarning("SHOTGUN TARGET = " + ShotgunRaycastTarget.name);
        }
        else
        {
            ShotgunRaycastTarget = null;
            SimpleRaycastTarget = null;
        }
    }


}
