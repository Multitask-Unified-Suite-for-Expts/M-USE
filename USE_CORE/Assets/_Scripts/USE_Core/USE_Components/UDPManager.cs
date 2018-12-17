using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor;
using System.Text;
using System.Net;
using System.Net.Sockets;
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

	public bool startRequestNewFrame = false;

	private byte[] send_data;
	private byte[] data;
	private string rawtext;
	private string[] msgs;
	private string msgType;
	private double pythonSentTimeStamp;
	private double unixRecvTimestamp;
	private double unixSendTimestamp;
	private int frame = 2;

	// private ExternalDataManager externalDataManager;
	public void UpdateClientInfo(string ip){
		IP = ip;
		Initialize();
	}

	// This method initializes the UDP connection and sends the first two messages to Python
	public void Initialize()
	{
		CloseUDP();
		// externalDataManager = GameObject.Find ("ScriptManager").GetComponent<ExternalDataManager> ();
		// Define address to send data to
		pythonEndPoint = new IPEndPoint (IPAddress.Parse (IP), pythonPort);
		pythonSock = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


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
				message += "\tTIME " + unixSendTimestamp;
				send_data = Encoding.UTF8.GetBytes(message);
				pythonSock.SendTo(send_data, pythonEndPoint);
				UDPSendBuffer.Add('"' + message + '"' + "\t" + unixSendTimestamp + "\t" + TimeStamp.ConvertToUnixTimestamp(DateTime.Now) + "\n");
			}
		}
		catch (Exception err)
		{
			print(err.ToString());
		}

	}

	//********* BV: eyetracker bridge specific??
	// public void RequestData() 
	// {
	// 	SendString("NEWFRAME");
	// }

	// receive thread
	public void ReceiveData()
	{
		try
		{
			// Bytes empfangen.
			while(client.Available > 0) {
				// Data received from Python
				data = client.Receive(ref pythonEndPoint);
				unixRecvTimestamp = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);

				// Convert data to UTF8 string format
				rawtext = Encoding.UTF8.GetString(data);
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
		PrepareData("");
	}	
	public void PrepareData(string prefix){
		// string prefix = experimentInfo.SubjectName + "\t" + Time.frameCount + "\t" + Time.time + "\t";
		prefix = prefix  + "\t" + Time.frameCount + "\t" + Time.time + "\t";
		recvStr = String.Join ("\n" + prefix, UDPReceiveBuffer.ToArray ());
		sendStr = String.Join ("\n" + prefix, UDPSendBuffer.ToArray ());
		recvStr = prefix + recvStr;
		sendStr = prefix + sendStr;
		UDPReceiveBuffer.Clear ();
		UDPSendBuffer.Clear ();
	}

	public void CloseUDP()
	{
//		if (receiveThread != null) 
//		{
//			stopThread = true;
//			receiveThread.Join ();
//		}
		if (client!=null){
			client.Close();
			pythonSock.Close ();
		}
	}
}


