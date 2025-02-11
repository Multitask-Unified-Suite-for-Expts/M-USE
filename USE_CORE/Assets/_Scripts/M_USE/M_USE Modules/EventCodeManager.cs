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
using System;
using System.Linq;
using System.Collections.Generic;
using USE_ExperimentTemplate_Classes;
using System.Text;
using System.Collections;


public class EventCodeManager : MonoBehaviour 
{
    public Dictionary<string, EventCode> SessionEventCodes;

    public List<int> SentBuffer = new List<int>();
    public List<int> Split_SentBuffer = new List<int>();
	public List<int> PreSplit_SentBuffer = new List<int>();

    public long systemTime;
    public bool codesActive;
    public int splitBytes;

	private readonly object sendCodeLocker = new object();

    public string neuralAcquisitionDevice = "Neuralynx", returnedCodePrefix = "Lynx";

	public SyncBoxController SyncBoxController;

    public List<int> FrameEventCodeBuffer = new List<int>();
    public List<int> FrameEventCodesStored;

    private readonly int referenceEventCodeMin = 101;
    private readonly int referenceEventCodeMax = 255;
    private int referenceEventCode = 101; // Same as Min

    public int StimulationCode = 0; //looks for anything other than 0
    public int StimulationCodeStored; //temp storage so it appears in frame data
    private readonly int StimulationCodeMin = 90;  //NEED TO MATCH EVENT CODE CONFIG RANGE FOR "StimulationCondition"
    private readonly int StimulationCodeMax = 100; //NEED TO MATCH EVENT CODE CONFIG RANGE FOR "StimulationCondition"




    // Call it every frame in LateUpdate() methods
    public void CheckFrameEventCodeBuffer() 
    {
        //If there's a stimulation code, send it and use that as the ref code for any other FrameEventcodes in the buffer
        if(StimulationCode > 0)
        {
            SendCodeImmediate(StimulationCode);

            StimulationCodeStored = StimulationCode;
            StimulationCode = 0;

            //If any frame codes exist, store them in frame data
            StoreFrameBufferCodes();

        }
        else //Send Reference Code
        {
            if(FrameEventCodeBuffer.Count > 0)
            {
                SendCodeImmediate(referenceEventCode);

                referenceEventCode++;
                if (referenceEventCode > referenceEventCodeMax)
                    referenceEventCode = referenceEventCodeMin;

                StoreFrameBufferCodes();
            }
        }

    }


    private void SendCodeImmediate(int code)
    {
        if (code < 1)
            Debug.LogError("Code " + code + " is less than 1, and cannot be sent.");

        if (neuralAcquisitionDevice == "Neuroscan")
        {
            code *= 256;
        }

        if (codesActive)
        {
			if (splitBytes <= 1)
			{
                lock (sendCodeLocker)
				{
					SendCode(code);
				}
			}
			else
				lock (sendCodeLocker)
				{
					SendSplitCode(code);
				}
        }
    }

    private void SendCode(int codeToSend)
    {
        SyncBoxController.SendCommand("NEU " + codeToSend.ToString());
        SentBuffer.Add(codeToSend);
    }

    private void SendSplitCode(int code)
    {
        PreSplit_SentBuffer.Add(code);
        int[] splitCode = new int[splitBytes];
        for (int iCode = splitBytes - 1; iCode >= 0; iCode--)
        {
            splitCode[iCode] = (code % 255) + 1;
            code = (int)(code / 255);
        }

        if (code > 0)
            Debug.LogError("Event code " + code + " is too high, and thus not evenly divisible into " + splitBytes + " individual bytes");

        for (int iCode = 0; iCode < splitBytes; iCode++)
        {
            try
            {
                SendCode(splitCode[iCode] * 257);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                Debug.Log("this many in splitCode: " + splitCode.Count());
            }
            Split_SentBuffer.Add(splitCode[iCode]);
        }
    }


    // --------------------------------------------------------------------------------------
    public void SendCodeThisFrame(int code)
    {
        if (IsStimulationCode(code))
        {
            if (StimulationCode == 0)
                StimulationCode = code;
            else
                Debug.Log("ALREADY HAD A STIMULATION CODE! " + code);
        }
        else
        {
            if (!FrameEventCodeBuffer.Contains(code))
                FrameEventCodeBuffer.Add(code);
            else
                Debug.Log("ATTEMPTED TO SEND CODE THAT WAS ALREADY IN BUFFER - CODE: " + code);
        }
    }

    public void SendCodeThisFrame(string codeString)
    {
        EventCode code = SessionEventCodes[codeString];
        if (code != null)
            SendCodeThisFrame(code);
    }

    public void SendCodeThisFrame(EventCode ec)
    {
        if (ec.Value != null)
            SendCodeThisFrame(ec.Value.Value);
        else
        {
            SendCodeImmediate(1);
            Debug.LogWarning("Attempted to send event code with no value specified, code of 1 sent instead.");
        }
    }

    public void SendRangeCodeThisFrame(string codeString, int valueToAdd)
    {
        EventCode code = SessionEventCodes[codeString];
        if (code != null)
        {
            int computedCode = code.Range[0] + valueToAdd;
            if (computedCode > code.Range[1])
                Debug.LogWarning("COMPUTED EVENT CODE IS ABOVE THE SPECIFIED RANGE! | CodeString: " + codeString + " | " + "ValueToAdd: " + valueToAdd + " | " + "ComputedValue: " + computedCode);
            else
            {
                SendCodeThisFrame(computedCode);
            }
        }
    }

    //--------------------------------------------------------------------------------------
    private IEnumerator SendNextFrame_Coroutine(int code)
    {
        yield return null; //Wait a frame
        SendCodeThisFrame(code);
    }

    public void SendCodeNextFrame(int code)
    {
        StartCoroutine(SendNextFrame_Coroutine(code));
    }

    public void SendCodeNextFrame(string codeString)
    {
        EventCode code = SessionEventCodes[codeString];
        if (code != null)
            SendCodeNextFrame(code);
    }

    public void SendCodeNextFrame(EventCode ec)
    {
        if (ec.Value != null)
		    SendCodeNextFrame(ec.Value.Value);
	    else
	    {
		    SendCodeImmediate(1);
		    Debug.LogWarning("Attempted to send event code with no value specified, code of 1 sent instead.");
	    }
    }

    // -------------------------------------------------------------------------------------
    public List<int> GetBuffer(string bufferType)
	{
		if (bufferType == "sent")
			return SentBuffer;
        else if (bufferType == "split")
			return Split_SentBuffer;
		else if (bufferType == "presplit")
			return PreSplit_SentBuffer;
		else
			Debug.LogError("Unknown event code buffer type " + bufferType + ".");
		return new List<int>();
	}

    public void CheckForAndSendEventCode(GameObject target, string beginning = "", string ending = "")
    {
        if (target == null)
        {
            Debug.LogWarning("TARGET IS NULL WHEN CALLING CHECK-FOR-AND-SEND-EVENTCODE!!!!!");
            return;
        }

        StringBuilder eventCodeBuilder = new StringBuilder();

        if (!string.IsNullOrEmpty(beginning))
            eventCodeBuilder.Append(beginning);

        StimDefPointer sdp = target.GetComponent<StimDefPointer>();
        GameObject go = target;

        if (sdp != null && sdp.StimDef.StimGameObject != null)
            go = sdp.StimDef.StimGameObject;

        if (Session.TargetObjects.Contains(go))
            eventCodeBuilder.Append("TargetObject");
        else if (Session.DistractorObjects.Contains(go))
            eventCodeBuilder.Append("DistractorObject");
        else if (Session.IrrelevantObjects.Contains(go))
            eventCodeBuilder.Append("IrrelevantObject");
        else
            eventCodeBuilder.Append("Object");

        if (!string.IsNullOrEmpty(ending))
            eventCodeBuilder.Append(ending);

        SendCodeThisFrame(eventCodeBuilder.ToString());
    }


    private bool IsStimulationCode(int code)
    {
        return code >= StimulationCodeMin && code <= StimulationCodeMax;
    }

    private void StoreFrameBufferCodes()
    {
        if (FrameEventCodeBuffer.Count > 0)
        {
            FrameEventCodesStored.AddRange(FrameEventCodeBuffer);
            FrameEventCodeBuffer.Clear();
        }
    }

}
