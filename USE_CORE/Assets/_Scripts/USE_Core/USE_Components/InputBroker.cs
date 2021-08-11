/*
This software is part of the Unified Suite for Experiments (USE).
Information on USE is available at
http://accl.psy.vanderbilt.edu/resources/analysis-tools/unifiedsuiteforexperiments/

Copyright (c) <2018> <Marcus Watson>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

1) The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
2) If this software is used as a component of a project that leads to publication
(e.g. a paper in a scientific journal or a student thesis), the published work
will give appropriate attribution (e.g. citation) to the following paper:
Watson, M.R., Voloh, B., Thomas, C., Hasan, A., Womelsdorf, T. (2018). USE: An
integrative suite for temporally-precise psychophysical experiments in virtual
environments for human, nonhuman, and artificially intelligent agents. BioRxiv:
http://dx.doi.org/10.1101/434944

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

public class InputBroker{

	public enum InputState{
		DOWN,
		PRESSED,
		UP
	}
	
	public static bool isSimulation = false;
	static Dictionary<KeyCode, InputState> keyState = new Dictionary<KeyCode, InputState>();
	static Dictionary<int, InputState> mouseState = new Dictionary<int, InputState>();

	
	static Vector3 _mousePosition;
	public static Vector3 mousePosition{
		get{
			if(isSimulation){
				return _mousePosition;
			}
			return Input.mousePosition;
		}
		set{
			if (isSimulation)
			{
				_mousePosition = value;
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
			mouseState[button] = InputState.PRESSED;	}

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
}
