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


using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using USE_ExperimentTemplate_Classes;
using System.Text;


public class EventCodeManager : MonoBehaviour 
{
    public Dictionary<string, EventCode> SessionEventCodes;

    private List<int> CodesToSendBuffer = new List<int>();

    public List<int> SentCodesBuffer = new List<int>();
    public List<int> SplitCodesBuffer = new List<int>();
    public List<int> PreSplitCodesBuffer = new List<int>();

    public long systemTime;
    public bool codesActive;
    public int splitBytes;

	private object sendCodeLocker = new object();
    public string neuralAcquisitionDevice = "Neuralynx", returnedCodePrefix = "Lynx";
	public SyncBoxController SyncBoxController;
    public string data;



    public void CheckForCodesToSend() // Call this once per frame as early as possible at session level
    {
        if (CodesToSendBuffer.Count > 0)
        {
            foreach(int code in CodesToSendBuffer)
            {
                SendCodeImmediate(code);
            }

            CodesToSendBuffer.Clear();
        }
    }

    // -------------------------------------------------------------------------------------
    public void SendCodeImmediate(int code)
    {
        if (code < 1)
            Debug.LogError("Code " + code + " is less than 1, and cannot be sent.");

        if (neuralAcquisitionDevice == "Neuroscan")
        {
            code = code * 256;
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

    public void SendCodeImmediate(string codeString)
    {
        EventCode code = SessionEventCodes[codeString];
		if (code != null)
			SendCodeImmediate(code);
    }

    public void SendCodeImmediate(EventCode ec)
    {
        if (ec.Value != null)
			SendCodeImmediate(ec.Value.Value);
	    else
	    {
		    SendCodeImmediate(1);
		    Debug.LogWarning("Attempted to send event code with no value specified, code of 1 sent instead.");
	    }
    }

    // -------------------------------------------------------------------------------------
    public void SendCodeNextFrame(int code)
    {
        if (!CodesToSendBuffer.Contains(code))
            CodesToSendBuffer.Add(code);
        else
            Debug.Log("ATTEMPTED TO SEND CODE THAT WAS ALREADY IN BUFFER - CODE: " + code);
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

    public void SendRangeCode(string codeString, int valueToAdd)
    {
        EventCode code = SessionEventCodes[codeString];
        if (code != null)
        {
            int computedCode = code.Range[0] + valueToAdd;
            if (computedCode > code.Range[1])
                Debug.LogError("COMPUTED EVENT CODE IS ABOVE THE SPECIFIED RANGE! | CodeString: " + codeString + " | " + "ValueToAdd: " + valueToAdd + " | " + "ComputedValue: " + computedCode);
            else
            {
                SendCodeImmediate(computedCode); 
            }
        }
    }

    // -------------------------------------------------------------------------------------
    private void SendCode(int codeToSend)
	{
        SyncBoxController.SendCommand("NEU " + codeToSend.ToString());     
        SentCodesBuffer.Add(codeToSend); //Add Code actually sent to sentBuffer
	}

	public void SendSplitCode(int code)
    {
        PreSplitCodesBuffer.Add(code);

        int[] splitCodes = new int[splitBytes];
        for (int iCode = splitBytes - 1; iCode >= 0; iCode--)
        {
            splitCodes[iCode] = (code % 255) + 1;
            code = (int)(code / 255);
        }

        if (code > 0)
            Debug.LogError("Event code " + code + " is too high, and thus not evenly divisible into " + splitBytes + " individual bytes");

		for (int iCode = 0; iCode < splitBytes; iCode++)
		{
            int splitCode = splitCodes[iCode];
			try
			{
				SendCode(splitCode * 257);
			}
			catch (Exception e)
			{
				Debug.Log(e);
				Debug.Log("this many in splitCode: " + splitCodes.Count());
			}
            SplitCodesBuffer.Add(splitCode);
		}
    }

    // -------------------------------------------------------------------------------------

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

        Session.EventCodeManager.SendCodeImmediate(eventCodeBuilder.ToString());
    }

    public List<int> GetBuffer(string bufferType)
    {
        bufferType = bufferType.ToLower();

        if (bufferType == "sent")
            return SentCodesBuffer;
        else if (bufferType == "split")
            return SplitCodesBuffer;
        else if (bufferType == "presplit")
            return PreSplitCodesBuffer;
        else
            Debug.LogError("Unknown event code buffer type " + bufferType + ".");
        return new List<int>();

    }

}
