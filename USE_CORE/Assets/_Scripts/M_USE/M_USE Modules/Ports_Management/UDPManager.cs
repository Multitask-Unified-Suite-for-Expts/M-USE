using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Linq;
//using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using USE_Utilities;

public class UDPManager : MonoBehaviour
{

	// Buffer
	public List<string> UDPReceiveBuffer = new List<string>();
	public List<string> UDPSendBuffer = new List<string>();
	public string recvStr;
	public string sendStr;

	// receiving Thread
//	Thread receiveThread;

	// udpclient object
	UdpClient client;

	// UDP Client/Server Information
	private string IP = "127.0.0.1";
	private int pythonPort = 8051;
	private int unityPort = 8050; // define > init
	private IPEndPoint pythonEndPoint;
	private Socket pythonSock;

	// infos
	private string lastReceivedUDPPacket = "";

	public bool arduinoCalibration = false;
	public bool startRequestNewFrame = false;

	private byte[] send_data;
	private byte[] data;
	private string rawtext;
	private string[] msgs;
	private string msgType;
	private double pythonSentTimeStamp;
	private string calibCommand;
	private float GazeTimeStamp;
	private float joyC;
	private float ArduinoTimeStamp;
	private double unixRecvTimestamp;
	private double unixSendTimestamp;
	private int frame = 2;
	//public ExperimentInfoController experimentInfo;
	public SessionDetails sessionDetails;


//	public bool stopThread = false;

	// private ExternalDataManager externalDataManager;
		

	// This method initializes the UDP connection and sends the first two messages to Python
	public void Initialize()
	{
		// externalDataManager = GameObject.Find ("ScriptManager").GetComponent<ExternalDataManager> ();
		//experimentInfo = GameObject.Find("ExperimenterInfo").GetComponent<ExperimentInfoController>();
		// Define address to send data to
		pythonEndPoint = new IPEndPoint (IPAddress.Parse (IP), pythonPort);
		pythonSock = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		pythonSock.ReceiveBufferSize = 32768;


		// Define client to receive data at
		client = new UdpClient (unityPort);

		// Start background thread to listen for messages from Python
//		receiveThread = new Thread (new ThreadStart (ReceiveData));
//		receiveThread.IsBackground = true;
//		receiveThread.Start ();
	}


	// This function takes in a string and sends it to Python
	public void SendString(string message)
	{
		
		try
		{
			if (!String.IsNullOrEmpty(message))
			{
				unixSendTimestamp = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);
				message += "###TIME " + unixSendTimestamp;
				send_data = Encoding.UTF8.GetBytes(message);
				pythonSock.SendTo(send_data, pythonEndPoint);
				UDPSendBuffer.Add(message);
				//message = "";
			}
		}
		catch (Exception err)
		{
			print(err.ToString());
		}

	}
//	public void calibrateArduino() 
//	{
//		SendString ("ARDUINO CMD:CAF 40000");
//	}

	public void RequestData() 
	{
		SendString("NEWFRAME");
	}

	// receive thread
	public void ReceiveData()
	{
//		while (!stopThread)
//		{
		try
		{
			// Bytes empfangen.
			while(client.Available > 0) {
				// Data received from Python
				data = client.Receive(ref pythonEndPoint);
				unixRecvTimestamp = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);

				// Convert data to UTF8 string format
				rawtext = Encoding.UTF8.GetString(data);
				if (rawtext.Contains('\n'))
					UDPReceiveBuffer.AddRange(rawtext.Split('\n').ToList());
				else if (rawtext.Contains('\r'))
					UDPReceiveBuffer.AddRange(rawtext.Split('\r').ToList());
				else
					UDPReceiveBuffer.Add(rawtext);
			}
			// Added this to reduce Unity CPU% from 100 to 60
//				Thread.Sleep(10);
		}
		catch (Exception err)
		{
			print(err.ToString());
		}
//		}
	}
		
	public void PrepareData(){
		string prefix = Time.frameCount + "\t" + Time.time + "\t";
		if (UDPReceiveBuffer.Count > 0)
		{
			recvStr = String.Join("\n" + prefix, UDPReceiveBuffer.ToArray());
			recvStr = prefix + recvStr;
		}
		else
			recvStr = "";
		if (UDPSendBuffer.Count > 0)
		{
			sendStr = String.Join("\n" + prefix, UDPSendBuffer.ToArray());
			sendStr = prefix + sendStr;
		}
		else
			sendStr = "";
		UDPReceiveBuffer.Clear ();
		UDPSendBuffer.Clear ();
	}

	public void CloseUDP()
	{
		client.Close();
		pythonSock.Close ();
	}
}


