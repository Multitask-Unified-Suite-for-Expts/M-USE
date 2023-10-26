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



using UnityEngine;
using USE_Data;
using USE_StimulusManagement;
using System;
using System.Collections.Generic;


public abstract class InputTracker : MonoBehaviour
{
    public GameObject TargetedGameObject;
    public StimDef TargetedStimDef;
    protected int AllowedDisplay = -1;
    public Vector3? CurrentInputScreenPosition;

    public List<GameObject> ShotgunGoAboveThreshold;
    public GameObject ShotgunModalTarget;
    public GameObject SimpleRaycastTarget;

    public ShotgunRaycast ShotgunRaycast;
    public float ShotgunThreshold;

    public bool UsingShotgunHandler;



    public delegate bool IsSelectionPossible();


    public void Awake()
    {
        ShotgunRaycast = GameObject.Find("MiscScripts").GetComponent<ShotgunRaycast>();
    }
    public void Init(DataController frameData, int allowedDisplay)
    {
        AddFieldsToFrameData(frameData);
        AllowedDisplay = allowedDisplay;
    }

    private void Update()
    {
        CustomUpdate();
        FindCurrentTarget();
    }

    public abstract void AddFieldsToFrameData(DataController frameData);

    public abstract void FindCurrentTarget();

    public virtual void CustomUpdate() //Anything a particular tracker needs to track that isn't a target neccessarily (ex: click count). 
    {
    }
    

}
