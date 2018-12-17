using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;


public class SerialPortController : MonoBehaviour
{

    public string serialPortAddress;
    public int serialPortSpeed;
    private static SerialPort sp;
    public int dataVerbosity = 0; //(2 = full, 1 = terse, 0 = packed hex)
    public List<string> serialReceiveBuffer = new List<string>();
    public List<string> serialSendBuffer = new List<string>();
    public string recvStr;
    public string sendStr;

    private bool isReady = false;

    private List<string> internalSendBuffer = new List<string>();

    public void Initialize(string serialPortAddress, int serialPortSpeed){
        try{
            sp = new SerialPort(serialPortAddress, serialPortSpeed);
            sp.Open();
            StartCoroutine(InitSP());

            StartCoroutine(ConsumeBuffer());
        }catch{
            Debug.Log("COULDNT OPEN PORT: " + serialPortAddress);
        }
    }

    private IEnumerator InitSP(){
        yield return new WaitForSeconds(2f);
        sp.ReadTimeout = 1;
        yield return new WaitForSeconds(0.001f);
        sp.WriteTimeout = 3;
        yield return new WaitForSeconds(0.001f);
        sp.DiscardInBuffer();
        isReady = true;

        Debug.Log("test");
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


    public void SendString(string message){
        internalSendBuffer.Add(message);
        TrySendString();
    }
    
    public void SendString(List<string> message){
        internalSendBuffer.AddRange(message);
        TrySendString(); // instantly try to consume the first message, the other will be caught by ConsumeBuffer on next frame
    }

    private IEnumerator ConsumeBuffer(){
        while (true){
            if (internalSendBuffer.Count > 0){
                TrySendString();
            }
            yield return new WaitForSeconds(0.001f);
        }
    }

    private void TrySendString(){
        if (isReady){
            isReady = false;
            try{
                string message = internalSendBuffer[0];
                sp.Write(message + "\n");
                serialSendBuffer.Add(message);
                internalSendBuffer.RemoveAt(0);
                Debug.Log(Time.frameCount + ": " + message);
            } catch (Exception e){
                string err = e.Message + "\t" + e.StackTrace;
                Debug.Log(err);
                throw new System.ArgumentException(err);
            }    
            isReady = true;
        }
    }


    public void PrepareData()
    {
        string prefix = Time.frameCount + "\t" + Time.time + "\t";
        recvStr = String.Join("\n" + prefix, serialReceiveBuffer.ToArray());
        sendStr = String.Join("\n" + prefix, serialSendBuffer.ToArray());
        recvStr = prefix + recvStr;
        sendStr = prefix + sendStr;
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