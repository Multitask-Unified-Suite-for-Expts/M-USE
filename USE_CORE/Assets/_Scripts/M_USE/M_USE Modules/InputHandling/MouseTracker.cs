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



using System;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;


public class MouseTracker : InputTracker
{
    public int[] ButtonCompletedClickCount = {0, 0 , 0};
    public int[] ButtonStatus = {0, 0, 0};
    public float[] ButtonPressDuration = {0, 0, 0};



    public void ResetClicks()
    {
        Array.Clear(ButtonCompletedClickCount, 0, ButtonCompletedClickCount.Length);
        Array.Clear(ButtonPressDuration, 0, ButtonPressDuration.Length);
    }

    public int[] GetClickCount()
    {
        return ButtonCompletedClickCount;
    }

    //this should include every button
    public override void CustomUpdate()
    {
        for (int iButton = 0; iButton < 3; iButton++)
        {
            GameObject target = UsingShotgunHandler ? ShotgunRaycastTarget : SimpleRaycastTarget;

            if (InputBroker.GetMouseButtonUp(iButton))
            {
                ButtonCompletedClickCount[iButton]++;
                if (target != null)
                    Session.EventCodeManager.CheckForAndSendEventCode(target, $"Button{iButton}ReleasedFrom", null);
            }

            if (InputBroker.GetMouseButton(iButton))
            {
                ButtonStatus[iButton] = 1;
                if (InputBroker.GetMouseButtonDown(iButton))
                {
                    ButtonPressDuration[iButton] = 0;

                    if (target != null)
                       Session.EventCodeManager.CheckForAndSendEventCode(target, $"Button{iButton}PressedOn", null);
                }
                else
                    ButtonPressDuration[iButton] += Time.deltaTime;
            }
            else
                ButtonStatus[iButton] = 0;
        }
    }

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("MousePosition", () => InputBroker.mousePosition != null ? InputBroker.mousePosition : new Vector3(float.NaN, float.NaN, float.NaN));
        frameData.AddDatum("MouseButtonStatus", () => "[" + string.Join(",",ButtonStatus) + "]");
        frameData.AddDatum("SimpleRaycastTarget", ()=> SimpleRaycastTarget != null ? SimpleRaycastTarget.name : null);
        frameData.AddDatum("ShotgunModalTarget", ()=> ShotgunRaycastTarget != null ? ShotgunRaycastTarget.name : null);

        frameData.AddDatum("JoystickInputX", () => Input.GetAxis("Horizontal"));
        frameData.AddDatum("JoystickInputZ", () => Input.GetAxis("Vertical"));
    }


    public override void FindCurrentTarget()
    {
        ValidateInputScreenPosition(InputBroker.mousePosition);

        if (CurrentInputScreenPosition != null && Camera.main != null)
        {
            SimpleRaycastTarget = InputBroker.SimpleRaycast(CurrentInputScreenPosition.Value); //Normal raycast

            if (UsingShotgunHandler)
            {
                if (SimpleRaycastTarget != null)
                    ShotgunRaycastTarget = SimpleRaycastTarget; //If hit an object directly, set shotgun value as well
                else
                    ShotgunRaycastTarget = InputBroker.ShotgunRaycast(CurrentInputScreenPosition.Value);
            }

            //if (ShotgunRaycastTarget != null)
            //    Debug.LogWarning("SHOTGUN TARGET = " + ShotgunRaycastTarget.name);
        }
        else
        {
            ShotgunRaycastTarget = null;
            SimpleRaycastTarget = null;
        }
    }




}
