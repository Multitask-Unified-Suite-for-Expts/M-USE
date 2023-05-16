using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
//using iView;
using System.Text;
using EyeTrackerData_Namespace;
//using MGcommon;
using USE_Common_Namespace;
using USE_Settings;


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
    public Vector3 gazePositionWorld;
    public bool[] eyeValidity = new bool[2];
    public bool eyetrackerConnectionLost;
    public int gazeRequestFrame;
   // private ScreenTransformations screenTransformations;
    private string recvLine = "";
    private string messageType = "";
    private string[] messages;

    public Int64 eyetrackerTimeStamp;
    public Int64 arduinoTimeStamp;

    public string[] calibMsgPt;
    public string calibMsgResult;
    public string[] calibMsgFinishedResult;


    private UDPManager udpManager;
    private SerialPortThreaded serialPortController;

    void Start()
    {
        var scriptManager = GameObject.Find("ScriptManager");
        if (scriptManager != null)
        {
            udpManager = scriptManager.GetComponent<UDPManager>();
            serialPortController = scriptManager.GetComponent<SerialPortThreaded>();
        }

        bool usingCalibration = false;

        replay = (bool)SessionSettings.Get("sessionConfig", "usingReplayer");
        eyeTrackType = (int)SessionSettings.Get("sessionConfig", "eyeTrackType");
        usingCalibration = (bool)SessionSettings.Get("sessionConfig", "usingCalibration");
    }

    public void ConsumeUDPReceiveBuffer()
    {
        calibMsgPt = new string[0];
        calibMsgResult = "";
        calibMsgFinishedResult = new string[0];
        if (udpManager == null)
            return;
        for (int i = 0; i < udpManager.UDPReceiveBuffer.Count; i++)
        {
            recvLine = udpManager.UDPReceiveBuffer[i];
            //print (recvLine);
            messages = recvLine.Split(new string[] { "###" }, StringSplitOptions.None);
            messageType = messages[0];
            switch (messages[0]) {
                case "CALIB":
                    switch (messages[1])
                    {
                        case "calibration_point":
                            calibMsgPt = messages;
                            break;
                        case "calibration_result":
                            calibMsgResult += recvLine;
                            if (i < udpManager.UDPReceiveBuffer.Count - 1)
                                calibMsgResult += '\n';
                            break;
                        case "finished_sending_calibration_results":
                            calibMsgFinishedResult = messages;
                            break;
                        default:
                            Debug.Log("Unknown CALIB message of type " + messages[1] + "received. Ignoring it.");
                            break;
                    }
                    break;
                case "GAZEDATA":
                    try
                    {
                        if (!eyetrackerConnectionLost)
                        {
                            gazeAdcsPos = new Vector2(float.Parse(messages[1].Split(' ')[1]), float.Parse(messages[2].Split(' ')[1]));
                            eyeValidity = new bool[2] { Convert.ToBoolean(int.Parse(messages[3].Split(' ')[1])), Convert.ToBoolean(int.Parse(messages[4].Split(' ')[1])) };
                            eyetrackerTimeStamp = Int64.Parse(messages[5].Split(' ')[1]);
                            gazeRequestFrame = int.Parse(messages[6].Split(' ')[1]);
                        }
                        else
                        {
                            gazeAdcsPos = new Vector2(-9999, -9999);
                            eyeValidity = new bool[2] { false, false };
                            eyetrackerTimeStamp = -9999;
                            gazeRequestFrame = -9999;
                        }
                    }
                    catch (Exception)
                    {
                        //print("Error or Missing Message(" + messages + ") on Frame# " + Time.frameCount);
                    }
                    break;
                case "NOTIFICATION":
                    switch (messages[1])
                    {
                        case "eyetracker_notification_connection_lost":
                            Debug.Log("Eyetracker connection lost");
                            eyetrackerConnectionLost = true;
                            break;
                        case "eyetracker_notification_connection_restored":
                            Debug.Log("Eyetracker connection restored");
                            eyetrackerConnectionLost = false;
                            break;
                        default:
                            Debug.Log("Unknown eyetracker notification message received. Ignoring it. Message is: " + messages[1]);
                            break;
                    }
                    break;
                default:
                    Debug.Log("Unknown UDP message of type " + messages[0] + "received. Ignoring it.");
                    break;
            //if (String.Equals(messageType, "CALIB"))
            //{
               /* if (String.Equals(messages[1], "calibration_point"))
                {
                    calibMsgPt = messages;
                }
                else if (String.Equals(messages[1], "calibration_result"))
                {
                    calibMsgResult += recvLine;
                    if (i < udpManager.UDPReceiveBuffer.Count - 1)
                        calibMsgResult += '\n';
                }
                else if (String.Equals(messages[1], "finished_sending_calibration_results"))
                {
                    calibMsgFinishedResult = messages;
                }*/
            //}
            //else if (String.Equals(messageType, "GAZEDATA"))
            //{
            /*
                try
                {
                    gazeAdcsPos = new Vector2(float.Parse(messages[1].Split(' ')[1]), float.Parse(messages[2].Split(' ')[1]));
                    eyeValidity = new bool[2] { Convert.ToBoolean(int.Parse(messages[3].Split(' ')[1])), Convert.ToBoolean(int.Parse(messages[4].Split(' ')[1])) };
                    eyetrackerTimeStamp = Int64.Parse(messages[5].Split(' ')[1]);
                    gazeRequestFrame = int.Parse(messages[6].Split(' ')[1]);
                }
                catch (Exception)
                {
                    //print("Error or Missing Message(" + messages + ") on Frame# " + Time.frameCount);
                }*/
            //}
            //else if (String.Equals(messageType, "NOTIFICATION"))
            //{
            //}
        }
        }

    }

    public void ConsumeSerialReceiveBuffer()
    {
		List<string> serialRecv = serialPortController.GetBuffer("received");
        for (int i = 0; i < serialRecv.Count; i++)
        {
            recvLine = Time.frameCount + "\t" + Time.time + "\t" + serialRecv[i].TrimEnd();
            if (serialRecv.Count > 0)
            {
                recvLine = recvLine + "\n";
            }
        }
    }

    public void EyetrackerDataHandler()
    {
        if (!replay)
        {
           // screenTransformations = new ScreenTransformations();
            if (eyeTrackType == 1)
            { //mouse cursor position as fixation
                gazePositionWorld = InputBroker.mousePosition; //
            }
            else if (eyeTrackType == 2)
            { //Real eyetracker
                if (!(eyeValidity[0] | eyeValidity[1]))
                {
                    gazePositionWorld = new Vector3(float.NaN, float.NaN, float.NaN);
                }
                else
                {
                   // gazePositionWorld = screenTransformations.AdcsToScreenPoint(gazeAdcsPos); //should AdcsToScreenPoint become a variable in this script?
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