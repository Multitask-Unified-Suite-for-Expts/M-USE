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



using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using USE_Utilities;
using System.IO.Ports;


public class SerialPortThreaded : MonoBehaviour
{
	public string SerialPortAddress;
	public int SerialPortSpeed;
	private static SerialPort sp;
	public int dataVerbosity; //(2 = full, 1 = terse, 0 = packed hex)

	[HideInInspector]
	private List<string> receivedBuffer = new List<string>(), sentBuffer = new List<string>();

	public List<MessageToSend> toSendBuffer = new List<MessageToSend>();

	public ExpectedResponseCode checkForResponseCode;
	private string codePrefixToCheck;
	public long maxRecvCodeCheckTime = 10;
	public object responseCheckLocker = new object();
	//public EventWaitHandle _waitForRecvCodeDetected = new EventWaitHandle(false, EventResetMode.AutoReset);

	public int minMsBetweenSending = 3, msBetweenReceiving = 2, readTimeout = 1, writeTimeout = 3, initTimeout = 2000;
	public long lastSentTimeStamp;
	private EventWaitHandle _waitForSendCode = new AutoResetEvent(false), _waitForRecvCodeDetected = new AutoResetEvent(false);
	public bool active;

	public List<string> recvBufferCheck = new List<string>();

	public void Initialize()
	{
		sp = new SerialPort(SerialPortAddress, SerialPortSpeed);
		sp.Open();
		new Thread(FinishInit).Start();
    }

    private void FinishInit()
	{
		Thread.Sleep(initTimeout);
		sp.ReadTimeout = 1;
		Thread.Sleep(5);
		sp.WriteTimeout = 3;
		Thread.Sleep(5);
		sp.DiscardInBuffer();
		active = true;
		StartSendLoop();
		StartRecvLoop();
        Session.SessionLevel.waitForSerialPort = false;
        Thread.CurrentThread.Abort();
		
	}

	public void StartSendLoop()
	{
		new Thread(SendLoop).Start();
	}

	public void StartRecvLoop()
	{
		new Thread(RecvLoop).Start();
	}

	private void SendLoop()
	{
		while (active)
		{
			if (toSendBuffer.Count > 0)
			{
				long currTime = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);
				if (currTime - lastSentTimeStamp > minMsBetweenSending * 10000)
				{
					ExpectedResponseCode expectedResponseCode = null;
					lock (toSendBuffer)
					{

						Debug.LogWarning("WRITING CODE: " + toSendBuffer[0].Code.ToString());

						sp.Write(toSendBuffer[0].Code + "\n");
						lastSentTimeStamp = currTime;
						lock (sentBuffer)
							sentBuffer.Add(currTime.ToString() + "\t" + toSendBuffer[0].Code);
						expectedResponseCode = toSendBuffer[0].ExpectedResponseCode;
						toSendBuffer.RemoveAt(0);
					}

					if (expectedResponseCode != null)
					{
						if (checkForResponseCode != null)
						{
							lock (responseCheckLocker)
							{
								checkForResponseCode = expectedResponseCode;
								checkForResponseCode.TimeAdded = currTime;
							}
						}
						else
						{
							checkForResponseCode = expectedResponseCode;
							checkForResponseCode.TimeAdded = currTime;
						}
						_waitForRecvCodeDetected.WaitOne();
					}
					else
						Thread.Sleep(minMsBetweenSending);
				}
			}
			else
				_waitForSendCode.WaitOne();
		}
		Thread.CurrentThread.Abort();
	}

	private void RecvLoop()
	{
		bool lastMsgUnfinished = false;
		string lastMsg = "";
		while (active)
		{
			if (sp.BytesToRead > 0)
			{
				long timestamp = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);
				string rawInput = sp.ReadExisting();
				if (lastMsgUnfinished)
				{
					rawInput = lastMsg + rawInput;
					lastMsgUnfinished = false;
				}
				string[] input = rawInput.Split('\n');
				if (rawInput[rawInput.Length - 1] != '\n')
				{
					lastMsgUnfinished = true;
					lastMsg = input[input.Length - 1];
                }
                else
				{
					lastMsg = "";
					lastMsgUnfinished = false;
				}

                for (int lineCount = 0; lineCount < input.Length; lineCount++)
				{
					switch (dataVerbosity)
					{
						case 0:
							if (!lastMsgUnfinished || lineCount < input.Length - 1)
							{
								string myLine = input[lineCount].TrimEnd();
								if (!string.IsNullOrEmpty(myLine))
								{
									lock (receivedBuffer)
									{
                                        receivedBuffer.Add(timestamp.ToString() + "\t" + myLine);
                                    }
                                    if (checkForResponseCode != null)
									{
										lock (responseCheckLocker)
										{
											bool codeMatched = true;
											foreach (string code in checkForResponseCode.CodeFragments)
											{
												if (!myLine.ToLower().Contains(code.ToLower()))
												{
													codeMatched = false;
													break;
												}
											}
											if (codeMatched)
											{
												checkForResponseCode = null;
												_waitForRecvCodeDetected.Set();
											}
											else if (timestamp - checkForResponseCode.TimeAdded >= maxRecvCodeCheckTime * 10000)
											{
												Debug.Log("SerialPortThreaded RecvLoop checking for incoming code " + checkForResponseCode.CodeFragments[1] + " timed out.");
												checkForResponseCode = null;
												_waitForRecvCodeDetected.Set();
											}
										}
									}
								}
								else if (checkForResponseCode != null)
								{
									if (timestamp - checkForResponseCode.TimeAdded >= maxRecvCodeCheckTime * 10000)
									{
										Debug.Log("SerialPortThreaded RecvLoop no incoming code, checking for incoming code " + checkForResponseCode.CodeFragments[1] + " timed out.");
										checkForResponseCode = null;
										_waitForRecvCodeDetected.Set();
									}
								}
							}
							break;
						default:
							Debug.LogError("Serial Port: Unknown data verbosity of " + dataVerbosity.ToString());
							break;
					}
				}
			}
			else if (checkForResponseCode != null)
			{
				long timestamp = TimeStamp.ConvertToUnixTimestamp(DateTime.Now);
				if (timestamp - checkForResponseCode.TimeAdded >= maxRecvCodeCheckTime * 10000)
				{
					Debug.Log("SerialPortThreaded RecvLoop no incoming code, checking for incoming code " + checkForResponseCode.CodeFragments[1] + " timed out.");
					checkForResponseCode = null;
					_waitForRecvCodeDetected.Set();
				}
			}
			Thread.Sleep(msBetweenReceiving);
		}
		Thread.CurrentThread.Abort();
	}

	public void ClosePort()
	{
		active = false;
		sp.Close();
	}


	public void AddToSend(string message, List<string> codesToCheck = null)
	{
		MessageToSend messageToSend = new MessageToSend(message);
		if (codesToCheck != null)
			messageToSend.ExpectedResponseCode = new ExpectedResponseCode(codesToCheck);
		lock (toSendBuffer)
		{
			toSendBuffer.Add(messageToSend);
		}
		_waitForSendCode.Set();
	}

	public void AddToSend(List<string> messages)
	{
		lock (toSendBuffer)
		{
			foreach(string message in messages)
				AddToSend(message);
		}
		//_waitForSendCode.Set();
	}

	public int BufferCount(string bufferType)
	{
		switch (bufferType.ToLower())
		{
			case ("tosend"):
				lock (toSendBuffer)
					return toSendBuffer.Count;
			case ("received"):
				lock (receivedBuffer)
					return receivedBuffer.Count;
			case ("sent"):
				lock (sentBuffer)
					return sentBuffer.Count;
			default:
				Debug.Log("Serial Port: Unknown buffer type: " + bufferType + "requested.");
				return 0;
		}
	}

	public string BufferToString(string bufferType)
	{
		string outputString = "";

		if (bufferType == "received")
			lock (receivedBuffer)
			{
				outputString = ConvertBuffer(receivedBuffer);
                receivedBuffer.Clear();
			}
		else if (bufferType == "sent")
			lock (sentBuffer)
			{
				outputString = ConvertBuffer(sentBuffer);
				sentBuffer.Clear();
			}
		else
			Debug.LogError("Unknown serial buffer type " + bufferType + ".");

		return outputString;
	}

	public string ConvertBuffer(List<string> buffer)
	{
		string prefix = Time.frameCount + "\t" + Time.time + "\t";
		if (buffer.Count > 0)
			return prefix + string.Join("\n" + prefix, buffer.ToArray());
		return "";
	}

	public List<string> GetBuffer(string bufferType)
	{
		if (bufferType == "received")
			return receivedBuffer;
		else if (bufferType == "sent")
			return sentBuffer;
		//else if (bufferType == "tosend")
			//return toSendBuffer;
		else
			Debug.LogError("Unknown serial buffer type " + bufferType + ".");
		return new List<string>();

	}

	//public void AddCodeToCheck(List<string> codes)
	//{
	//	if (checkForResponseCode != null)
	//	{
	//		_waitForRecvCodeDetected.WaitOne();
	//		lock (checkForResponseCode)
	//		{
	//			checkForResponseCode = new ExpectedResponseCode(codes);
	//		}
	//		_waitForRecvCodeDetected.Reset();
	//	}
	//	else
	//	{
	//		checkForResponseCode = new ExpectedResponseCode(codes);
	//	}
	//}

}

public class ExpectedResponseCode
{
	public List<string> CodeFragments { get; }
	public long TimeAdded { get; set; }

	public ExpectedResponseCode(List<string> code)
	{
		CodeFragments = code;
	}
}

public class MessageToSend
{
	public string Code { get; }
	public ExpectedResponseCode ExpectedResponseCode { get; set;  }

	public MessageToSend(string code)
	{
		Code = code;
	}
}
