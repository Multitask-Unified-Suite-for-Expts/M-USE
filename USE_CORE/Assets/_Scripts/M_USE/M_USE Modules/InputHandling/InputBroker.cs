﻿/*
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
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;


public class InputBroker
{

	public enum InputState{
		DOWN,
		PRESSED,
		UP
	}
	
	public static bool isSimulation = false;
	static Dictionary<KeyCode, InputState> keyState = new Dictionary<KeyCode, InputState>();
	static Dictionary<int, InputState> mouseState = new Dictionary<int, InputState>();

    static Vector3 _mousePosition;
	static Vector2 _gazePosition;

    public static Vector3 mousePosition{
		get{
			if(isSimulation)
				return _mousePosition;
			return Input.mousePosition;
		}
		set{
			if (isSimulation)
			{
				_mousePosition = value;
			}
		}
	}
    public static Vector2 gazePosition
    {
        get
        {            
			if (isSimulation)
            {
                return _gazePosition;
            }
			return CurrentGazePositionOnDisplayArea();        
		}
        set
        {
            if (isSimulation)
            {
                _gazePosition = value;
            }
        }
    }

    public static void SetMouseButtonDown(int button)
	{
		if (!mouseState.ContainsKey(button))
			mouseState.Add(button, InputState.DOWN);
		else
			mouseState[button] = InputState.DOWN;
	}

	public static void SetMouseButton(int button)
	{
		if (!mouseState.ContainsKey(button))
			mouseState.Add(button, InputState.PRESSED);
		else
			mouseState[button] = InputState.PRESSED;
	}

	public static void SetMouseButtonUp(int button)
	{
		if (!mouseState.ContainsKey(button))
			mouseState.Add(button, InputState.UP);
		else
			mouseState[button] = InputState.UP;
	}
	
	public static bool GetMouseButtonDown(int button)
	{
		if (!isSimulation)
			return Input.GetMouseButtonDown(button);
		else
		{
			if (mouseState.ContainsKey(button))
				return mouseState[button] == InputState.DOWN;
			return false;
		}
	}
	
	public static bool GetMouseButton(int button)
	{
		if (!isSimulation)
			return Input.GetMouseButton(button);
		else
		{
			if (mouseState.ContainsKey(button))
				return mouseState[button] == InputState.PRESSED;
			return false;
		}
	}
	
	public static bool GetMouseButtonUp(int button)
	{
		if (!isSimulation)
			return Input.GetMouseButtonUp(button);
		else
		{
			if (mouseState.ContainsKey(button))
				return mouseState[button] == InputState.UP;
			return false;
		}
	}
	
	
	public static void DeleteMouseButton(int button)
	{
		mouseState.Remove(button);
	}
	
	public static bool GetKey(KeyCode key)
	{
		if (!isSimulation)
			return Input.GetKey(key);
		else
		{
			if (keyState.ContainsKey(key))
				return keyState[key] == InputState.PRESSED;
			return false;
		}
	}
	
	public static bool GetKeyDown(KeyCode key)
	{
		if (!isSimulation)
			return Input.GetKeyDown(key);
		else
		{
			if (keyState.ContainsKey(key))
				return keyState[key] == InputState.DOWN;
			return false;
		}
	}
	
	public static bool GetKeyUp(KeyCode key)
	{
		if (!isSimulation)
			return Input.GetKeyUp(key);
		else
		{
			if (keyState.ContainsKey(key))
				return keyState[key] == InputState.UP;
			return false;
		}
	}
	
	public static void SetKeyDown(KeyCode key)
	{
		if (!keyState.ContainsKey(key))
			keyState.Add(key, InputState.DOWN);
		else
			keyState[key] = InputState.DOWN;
	}
	
	public static void SetKey(KeyCode key)
	{

		if (!keyState.ContainsKey(key))
			keyState.Add(key, InputState.PRESSED);
		else
			keyState[key] = InputState.PRESSED;
	}
	
	public static void SetKeyUp(KeyCode key)
	{		
		if (!keyState.ContainsKey(key))
			keyState.Add(key, InputState.UP);
		else
			keyState[key] = InputState.UP;
	}
	
	public static void DeleteKey(KeyCode key){
		keyState.Remove(key);
	}
	
	public static bool anyKeyDown{
		get{
			if(!isSimulation)
				return Input.anyKeyDown;
			else{
				foreach(InputState state in keyState.Values){
					if (state == InputState.DOWN)
						return true;
				}
				return false;
			}
		}
	}
	
	public static bool anyKey{
		get
		{
			if (!isSimulation)
				return Input.anyKey;
			else
			{
				foreach (InputState state in keyState.Values)
				{
					if (state == InputState.PRESSED)
						return true;
				}
				return false;
			}
		}
	}
	
	
	
	public static IEnumerator ClickKey(KeyCode key)
	{
		InputBroker.SetKeyDown(key);
		yield return new WaitForEndOfFrame();
		InputBroker.SetKey(key);
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		InputBroker.SetKeyUp(key);
		yield return new WaitForEndOfFrame();
		InputBroker.DeleteKey(key);
	}

	
	public static IEnumerator HoldKey(KeyCode key)
	{
		InputBroker.SetKeyDown(key);
		yield return new WaitForEndOfFrame();
		InputBroker.SetKey(key);
	}

	
	public static IEnumerator ReleaseKey(KeyCode key)
	{
		InputBroker.SetKeyUp(key);
		yield return new WaitForEndOfFrame();
		InputBroker.DeleteKey(key);
	}


	public static IEnumerator ClickMouseButton(int button)
	{
		InputBroker.SetMouseButtonDown(button);
		yield return new WaitForEndOfFrame();
		InputBroker.SetMouseButton(button);
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		InputBroker.SetMouseButtonUp(button);
		yield return new WaitForEndOfFrame();
		InputBroker.DeleteMouseButton(button);
	}

    public static GameObject RaycastBoth(Vector3 touchPos)
    {
        GameObject target = null;
        float distance2D = 0;
        float distance3D = 0;


		//3D:
		RaycastHit hit;
		if (Physics.Raycast(Camera.main.ScreenPointToRay(touchPos), out hit, Mathf.Infinity))
		{
			target = hit.transform.gameObject;
			distance3D = (hit.point - touchPos).magnitude;
		}

		//2D:
		PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touchPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
			if (result.gameObject != null)
            {
                distance2D = (result.gameObject.transform.position - touchPos).magnitude;
                if (target == null || (distance3D != 0 && (distance2D < distance3D)))
                {
                    target = result.gameObject;
                    break;
                }
            }
        }
        return target;
    }
	private static Vector2 CurrentGazePositionOnDisplayArea()
	{
        
        Vector2 screenPoint = new Vector2(float.NaN, float.NaN);
        if (Session.TobiiEyeTrackerController?.iEyeTracker != null)
        { // Get the most recent gaze data point
            var gazeData = Session.TobiiEyeTrackerController.EyeTracker?.LatestGazeData; // REFER TO TOBII EYETRACKER CONTROLLER

			if (gazeData != null)
			{
				// Get the gaze points for each eye
				var leftGazePoint = gazeData.Left.GazePointOnDisplayArea;
				var rightGazePoint = gazeData.Right.GazePointOnDisplayArea;

				// Check if both eyes are valid
				if (gazeData.Left.GazePointValid && gazeData.Right.GazePointValid)
				{
					// Average the gaze points from both eyes
					var combinedGazePoint = new Vector2(
						(leftGazePoint.x + rightGazePoint.x) / 2f,
						(leftGazePoint.y + rightGazePoint.y) / 2f);
					screenPoint = new Vector2(Screen.width * combinedGazePoint.x, Screen.height * (1 - combinedGazePoint.y));
                    // Convert the combined gaze point to screen coordinates
                }
				else if (gazeData.Left.GazePointValid)
				{
                    // Use the gaze point from the left eye
                    screenPoint = new Vector2(Screen.width * leftGazePoint.x, Screen.height * (1 - leftGazePoint.y));
                }
				else if (gazeData.Right.GazePointValid)
				{
                    // Use the gaze point from the right eye

                    screenPoint = new Vector2(Screen.width * rightGazePoint.x, Screen.height * (1 - rightGazePoint.y));
                }
            }
        }
		return screenPoint;
    }
}
