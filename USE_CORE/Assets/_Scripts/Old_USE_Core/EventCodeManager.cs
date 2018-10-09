using UnityEngine;
using System;
using System.Collections;
using USE_Utilities;

public class EventCodeManager : MonoBehaviour {

	private int frameChecker = 0;
	public int? msgToSend = null;
	public bool sendingMsg = false;
	public int? sentMsg;
	public long systemTime;

	public string neuralAcquisitionDevice = "Neuralynx";

	private UDPManager udpManager;
	private SerialPortController serialPortController;

	void Awake()
	{
		udpManager = GameObject.Find("ScriptManager").GetComponent<UDPManager>();
		serialPortController = GameObject.Find("ScriptManager").GetComponent<SerialPortController>();
	}

	public void EventCodeFixedUpdate()
	{
		sentMsg = null;
		if (Time.frameCount > frameChecker)
		{
			systemTime = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);
			frameChecker = Time.frameCount;
			if (msgToSend != null)
			{
				sendingMsg = true;
				SendCode(msgToSend);
				sentMsg = msgToSend;
				msgToSend = null;
				sendingMsg = false;
			}
		}
	}

	public void SendCode(int? code)
	{
		if (neuralAcquisitionDevice == "Neuroscan")
		{
			code = code * 256;
		}
		serialPortController.SendString("NEU " + msgToSend.ToString());
		print(msgToSend);
	}

}
