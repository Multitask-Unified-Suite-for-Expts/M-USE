using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using USE_Utilities;
//using System.Threading;


public class SerialPortController : MonoBehaviour
{

    public string serialPortAddress;
    public int serialPortSpeed;
    //public SerialPortController instance = null;
    //public StringBuilder serialData = new StringBuilder("", 1000); //Assign 1000 characters - there are 34 per line of packed hex output from the neurarduino, so this is overkill.
    private static SerialPort sp;
    public int dataVerbosity = 0; //(2 = full, 1 = terse, 0 = packed hex)

    //	private bool stopThread = false;

    //	private Thread receiveThread;

    public List<string> serialReceiveBuffer = new List<string>();
    public List<string> serialSendBuffer = new List<string>();
    public string recvStr;
    public string sendStr;
	private List<string> toSendBuffer = new List<string>();
	private long lastSentTimeStamp = 0;
	private float minTicksBetweenCodes = 5000;

	private void Update()
	{
		if (toSendBuffer.Count > 0)
			StartCoroutine(SendBufferedMessages());
	}

	private void LateUpdate()
	{
		if (toSendBuffer.Count > 0)
			StartCoroutine(SendBufferedMessages());
	}

	public void Initialize()
    {
        sp = new SerialPort(serialPortAddress, serialPortSpeed);
        sp.Open();
        StartCoroutine(SetupNeurarduino());
    }

    public void ReceiveData()
    {
        if (sp.BytesToRead > 0)
        {
            string rawInput = sp.ReadExisting();
            string[] splitInput = rawInput.Split('\n');
            for (int lineCount = 0; lineCount < splitInput.Length; lineCount++)
            {
                switch (dataVerbosity)
                {
                    case 0:
                        string myLine = splitInput[lineCount].TrimEnd();
                        if (!String.IsNullOrEmpty(myLine))
                        {
                            serialReceiveBuffer.Add(myLine);
                        }
                        break;
                }
            }
        }
    }

	public IEnumerator SendBufferedMessages()
	{
		while (toSendBuffer.Count > 0)
		{
			SendString(toSendBuffer[0]);
			toSendBuffer.RemoveAt(0);
			yield return new WaitForSeconds(0.0005f);
		}
	}

	private IEnumerator SetupNeurarduino()
    {
        yield return new WaitForSeconds(2f);
        sp.ReadTimeout = 1;
        yield return new WaitForSeconds(0.001f);
        sp.WriteTimeout = 3;
        yield return new WaitForSeconds(0.001f);
        sp.DiscardInBuffer();
        SendString("INI");
        yield return new WaitForSeconds(0.001f);
        SendString("TIM 0");
        yield return new WaitForSeconds(0.001f);
        SendString("LIN 33");
        yield return new WaitForSeconds(0.001f);
        SendString("LVB " + dataVerbosity);
        yield return new WaitForSeconds(0.001f);
        SendString("NSU 2");
        yield return new WaitForSeconds(0.001f);
        SendString("NPD 10");
        yield return new WaitForSeconds(0.001f);
        SendString("NHD 2");
        yield return new WaitForSeconds(0.001f);
        SendString("NDW 16");
        yield return new WaitForSeconds(0.001f);
        SendString("ECH 0");
        yield return new WaitForSeconds(0.001f);
        SendString("QRY");
        yield return new WaitForSeconds(0.001f);
        SendString("CAO 20000");
        // Start background thread to listen for messages from Python
        //		receiveThread = new Thread (new ThreadStart (ReceiveData));
        //		receiveThread.IsBackground = true;
        //		receiveThread.Start ();
    }

    //	public void SendEventCode(int value){
    //		SendString("NEU " + value);
    ////		EventCodeManager.msgToSend = null;
    ////		EventCodeManager.msgSentThisFrame = value;
    //	}

    public void SendString(string message)
    {
        try
        {
			long currTime = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);
			if (currTime - lastSentTimeStamp > minTicksBetweenCodes)
			{
				sp.Write(message + "\n");
				serialSendBuffer.Add(message);
				lastSentTimeStamp = currTime;
			}
			else
			{
				toSendBuffer.Add(message);
				// StartCoroutine(SendBufferedMessages());
			}
			
        }
        catch (Exception e)
        {
            string err = e.Message + "\t" + e.StackTrace;
            Debug.Log(err);
            throw new System.ArgumentException(err);
        }
    }
	public void SimpleLineClear(){
		//a stupid little function written by Seth to hopefully solve IO timeout on windows
		//possibly windows just needs to keep buffer clear and/or read to stop firewall from activating
		string mystring = sp.ReadLine ();
		//sp.DiscardInBuffer ();
	}
    public void PrepareData()
    {
        string prefix = Time.frameCount + "\t" + Time.time + "\t";
		if (serialReceiveBuffer.Count > 0)
		{
			recvStr = String.Join("\n" + prefix, serialReceiveBuffer.ToArray());
			recvStr = prefix + recvStr;
		}
		else
			recvStr = "";
		if (serialSendBuffer.Count > 0)
		{
			sendStr = String.Join("\n" + prefix, serialSendBuffer.ToArray());
			sendStr = prefix + sendStr;
		}
		else
			sendStr = "";
        serialReceiveBuffer.Clear();
        serialSendBuffer.Clear();
    }

    public void CloseSP()
    {
        //		stopThread = true;
        SendString("INI");
        sp.DiscardInBuffer();
        sp.Close();
    }
}