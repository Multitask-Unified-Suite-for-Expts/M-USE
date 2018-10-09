using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
//using iView;
using System.Text;
//using MGcommon;
using USE_Common_Namespace;


public class ExternalDataManager : MonoBehaviour
{

	public bool replay;
	public int eyeTrackType = 2; //1 = mousetracking, 2 = Tobii via Python App
	public int joystickType = 1; //1 = standard Unity input, 2 = custom joystick via neurarduino, 3 = wii mouseemulation

	private float joyMax = 255f;
	private float joy0Buffer = 10;
	private Vector2 rawJoy;
	public Vector2 joyPosition;

	public Vector2 gazeAdcsPos;
	public Vector3 eyePosition;
	public bool[] eyeValidity = new bool[2];
	public int gazeRequestFrame;

	private string sentBuffer = "";
	private string recvLine = "";
	private string rawText = "";
	private string messageType = "";
	private string[] messages;


	public Int64 eyetrackerTimeStamp;
	public Int64 arduinoTimeStamp;

	public string rawSerialData;
	private string subject;


	private UDPManager udpManager;
	//	private SequenceDefinitionCalibration sequenceDefinitionCalibration;
	private SerialPortController serialPortController;

	private bool expectingRestOfLine = false;
	private string startOfLine;

	private SequenceDefinition_Calibration calibSequence;

	void OnEnable()
	{
		var scriptManager = GameObject.Find("ScriptManager");
		if (scriptManager != null)
		{
			udpManager = scriptManager.GetComponent<UDPManager>();
			serialPortController = scriptManager.GetComponent<SerialPortController>();
		}

		bool usingCalibration = false;

		replay = ConfigReader.sessionSettings.Bool["usingReplayer"];
		eyeTrackType = ConfigReader.sessionSettings.Int["eyeTrackType"];
		usingCalibration = ConfigReader.sessionSettings.Bool["usingCalibration"];

		if (usingCalibration)
		{
			calibSequence = GameObject.Find("SequenceDefinitions").GetComponent<SequenceDefinition_Calibration>();
		}
	}

	public void ConsumeUDPReceiveBuffer()
	{
		if (udpManager == null)
			return;
		for (int i = 0; i < udpManager.UDPReceiveBuffer.Count; i++)
		{
			recvLine = udpManager.UDPReceiveBuffer[i];
			//print (recvLine);
			messages = recvLine.Split('\t');
			messageType = messages[0];
			if (String.Equals(messageType, "CALIB"))
			{
				string calibMsg = messages[1];
				if (String.Equals(calibMsg, "calibration_point"))
				{
					if (String.Equals(messages[2], "calibration_status_success"))
					{
						calibSequence.currentCalibrationPointFinished = true;
					}
					else
					{
						calibSequence.SwitchEpoch(0);
					}
				}
				else if (String.Equals(calibMsg, "calibration_result"))
				{
					calibSequence.RecordCalibrationResult(recvLine);
				}
				else if (String.Equals(calibMsg, "finished_sending_calibration_results"))
				{
					calibSequence.DisplayCalibrationResults();
				}
			}
			//if (String.Equals(messageType, "FRAMEDATA"))
			//{
			//	eyetrackerTimeStamp = Int64.Parse(messages[3].Split(' ')[1]);
			//	arduinoTimeStamp = Int64.Parse(messages[6].Split(' ')[1]);
			//	gazeAdcsPos = new Vector2(float.Parse(messages[1].Split(' ')[1]), float.Parse(messages[2].Split(' ')[1]));
			//	rawJoy = new Vector2(float.Parse(messages[4].Split(' ')[1]), 255 - float.Parse(messages[5].Split(' ')[1]));
			//}
			//if (String.Equals(messageType, "EYE_FRAMEDATA"))
			//{
			//	gazeAdcsPos = new Vector2(float.Parse(messages[1].Split(' ')[1]), float.Parse(messages[2].Split(' ')[1]));
			//	eyetrackerTimeStamp = Int64.Parse(messages[3].Split(' ')[1]);
			//}
			else if (String.Equals(messageType, "GAZEDATA"))
			{
				gazeAdcsPos = new Vector2(float.Parse(messages[1].Split(' ')[1]), float.Parse(messages[2].Split(' ')[1]));
				eyeValidity = new bool[2] { Convert.ToBoolean(int.Parse(messages[3].Split(' ')[1])), Convert.ToBoolean(int.Parse(messages[4].Split(' ')[1])) };
				eyetrackerTimeStamp = Int64.Parse(messages[5].Split(' ')[1]);
				gazeRequestFrame = int.Parse(messages[6].Split(' ')[1]);
			}
		}
	}

    public void ConsumeSerialReceiveBuffer()
    {
        rawSerialData = "";
        for (int i = 0; i < serialPortController.serialReceiveBuffer.Count; i++)
        {
            recvLine = Time.frameCount + "\t" + Time.time + "\t" + serialPortController.serialReceiveBuffer[i].TrimEnd();
            if (serialPortController.serialReceiveBuffer.Count > 0)
            {
                recvLine = recvLine + "\n";
            }
        }
    }

    public void EyetrackerDataHandler()
	{
		if (!replay)
		{
			if (eyeTrackType == 1)
			{ //mouse cursor position as fixation
				eyePosition = InputBroker.mousePosition; //
			}
			else if (eyeTrackType == 2)
			{ //Real eyetracker
				if (!(eyeValidity[0] | eyeValidity[1]))
				{
					eyePosition = new Vector3(float.NaN, float.NaN, float.NaN);
				}
				else
				{
					eyePosition = ScreenTransformations.AdcsToScreenPoint(gazeAdcsPos); //should AdcsToScreenPoint become a variable in this script?
				}
			}
		}
	}

	public void JoystickDataHandler()
	{
		if (joystickType == 1)
		{
			joyPosition = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
		}
		else if (joystickType == 2)
		{
			joyPosition = new Vector2(RawJoyConverter(rawJoy[0]), RawJoyConverter(rawJoy[1]));
		}
		else if (joystickType == 3)
		{
			float range1 = 2.5f; //4.75f; //on xy axis
			float range2 = 3f;//5.75f; //diagnoal
			joyPosition = new Vector2(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"));
			if (joyPosition[0] != 0 && joyPosition[1] != 0)
			{
				joyPosition[0] /= range2;
				joyPosition[1] /= range2;
			}
			else
			{
				joyPosition[0] /= range1;
				joyPosition[1] /= range1;
			}
		}
	}

	float RawJoyConverter(float rawAxis)
	{

		float joyMiddle = joyMax / 2;
		float min0 = joyMiddle - joy0Buffer;
		float max0 = joyMiddle + joy0Buffer;
		float newAxis = 0;

		if (rawAxis >= min0 && rawAxis <= max0)
		{
			newAxis = 0;
		}
		else if (rawAxis < min0)
		{
			newAxis = -1 * (1 - (rawAxis / min0));
		}
		else if (rawAxis > max0)
		{
			newAxis = (rawAxis - max0) / (joyMax - max0);
		}
		return newAxis;
	}
}
