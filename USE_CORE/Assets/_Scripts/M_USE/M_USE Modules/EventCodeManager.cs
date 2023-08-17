using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using USE_Utilities;
using System.Threading;
using USE_ExperimentTemplate_Classes;

public class EventCodeManager : MonoBehaviour 
{
    public Dictionary<string, EventCode> SessionEventCodes;

    private int frameChecker = 0;
    private List<int> toSendBuffer = new List<int>();
    public List<int> sentBuffer = new List<int>();
    public List<int> splitSentBuffer = new List<int>();
	public List<int> preSplitBuffer = new List<int>();
    public long systemTime;
    public bool codesActive;
    public int splitBytes;

	//private EventWaitHandle _waitForMessage = new AutoResetEvent(false);
	//private int codeToSend;
	private int? checkCode;
	private int checkReceivedCodePause = 1;
	private int maxCheckReceivedCodeLoops = 10;
	private object checkCodeLocker = new object(), checkLoopLocker = new object(), sendCodeLocker = new object();
	private bool checking;

    public string neuralAcquisitionDevice = "Neuralynx", returnedCodePrefix = "Lynx";

	private SerialPortThreaded serialPortController;
	public SyncBoxController SyncBoxController;

    void Awake()
    {
        // serialPortController = GameObject.Find("ScriptManager").GetComponent<SerialPortThreaded>();
    }

    public void EventCodeFixedUpdate()
    {
        if (Time.frameCount > frameChecker)
        {
            systemTime = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);
            frameChecker = Time.frameCount;
            if (toSendBuffer.Count > 0)
            {
                foreach (int code in toSendBuffer)
                {
                    SendCodeImmediate(code);
				}
                toSendBuffer.Clear();
            }
        }
    }

    public void EventCodeLateUpdate()
	{
        sentBuffer.Clear();
        splitSentBuffer.Clear();
		preSplitBuffer.Clear();
    }

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
    
    public void SendCodeNextFrame(int code)
    {
        toSendBuffer.Add(code);
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

	private void SendCode(int codeToSend)
	{
		SyncBoxController.SendCommand("NEU " + codeToSend.ToString(), new List<string> { returnedCodePrefix, codeToSend.ToString("X") });
		sentBuffer.Add(codeToSend);
	}

	public void SendSplitCode(int code)
    {
		preSplitBuffer.Add(code);
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
				//Debug.Log("EventCodeManager SendSplitCode. Original Code: " + preSplitBuffer[preSplitBuffer.Count - 1] +
					//", Code " + (iCode + 1) + "of 2, Split Code: " + splitCode[iCode] + ".");
				SendCode(splitCode[iCode] * 257);
			}
			catch (Exception e)
			{
				Debug.Log(e);
				Debug.Log("this many in splitCode: " + splitCode.Count());
			}
			splitSentBuffer.Add(splitCode[iCode]);
		}
    }

	public List<int> GetBuffer(string bufferType)
	{
		if (bufferType == "sent")
			return sentBuffer;
		else if (bufferType == "split")
			return splitSentBuffer;
		else if (bufferType == "presplit")
			return preSplitBuffer;
		else
			Debug.LogError("Unknown event code buffer type " + bufferType + ".");
		return new List<int>();

	}
	
	
}
