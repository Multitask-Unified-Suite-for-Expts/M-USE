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




using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class LogWriter : MonoBehaviour
{
    private readonly List<string> LogMessages = new List<string>();
    [HideInInspector] public bool StoreDataIsSet; //turned true by sessionLevel script when it sets SessionValues.StoreData's value
    private bool CreatingLogFolder;
    private bool LogFolderCreated;
    private bool LogFileCreated;
    private readonly int Capacity = 50;

    private string ServerLog_FolderPath
    {
        get
        {
            if (string.IsNullOrEmpty(ServerManager.SessionDataFolderPath))
            {
                Debug.LogWarning("Trying to Get ServerLogFolderPath but ServerManager.SessionDataFolderPath hasnt been set yet!");
                return null;
            }
            else
            {
                return $"{ServerManager.SessionDataFolderPath}/LogFile";
            }
        }
    }

    private string LocalLog_FolderPath
    {
        get
        {
            if (string.IsNullOrEmpty(Session.SessionDataPath))
            {
                Debug.LogWarning("Trying to Get LocalLogFolderPath but SessionValues.SessionDataPath hasnt been set yet!");
                return null;
            }
            else
                return $"{Session.SessionDataPath}{Path.DirectorySeparatorChar}LogFile";
        }
    }

    private string ServerLog_FilePath
    {
        get
        {
            if (string.IsNullOrEmpty(ServerManager.SessionDataFolderPath))
            {
                Debug.Log("Trying to Get ServerLogFilePath but ServerManager.SessionDataFolderPath hasnt been set yet!");
                return null;
            }
            else
                return $"{ServerManager.SessionDataFolderPath}/LogFile/Player.log";
        }
    }

    private string LocalLog_FilePath
    {
        get
        {
            if (string.IsNullOrEmpty(Session.SessionDataPath))
            {
                Debug.Log("Trying to Get LocalLogFilePath but SessionValues.SessionDataPath hasnt been set yet!");
                return null;
            }
            else
                return $"{Session.SessionDataPath}{Path.DirectorySeparatorChar}LogFile{Path.DirectorySeparatorChar}Player.log";
        }
    }



    private void Start()
    {
        Application.logMessageReceived += HandleLogMessage;
        Application.quitting += OnApplicationQuit;
    }

    private void HandleLogMessage(string logMessage, string stackTrace, LogType type)
    {
        LogMessages.Add(logMessage);

        if (!StoreDataIsSet) //Wait for StoreData to be set
            return;

        if (!Session.StoreData) //if storedata is set, but its False, return:
        {
            LogMessages.Clear();
            return;
        }

        if(!LogFolderCreated)
        {
            if(!CreatingLogFolder)
            {
                CreatingLogFolder = true;
                StartCoroutine(CreateLogFolder());
            }
        }

        if (LogMessages.Count >= Capacity && LogFolderCreated) //adding "and LogFolderCreated" to try and prevent the bug seema's encountering
        {
            if(!LogFileCreated)
                StartCoroutine(CreateLogFile());
            else
                StartCoroutine(AppendDataToLogFile());
        }
    }

    private void WriteLogMessagesLocally(StreamWriter writer)
    {
        if (LogMessages.Count > 0)
        {
            foreach (string message in LogMessages)
                writer.WriteLine(message);
            LogMessages.Clear();
        }
    }

    private IEnumerator CreateLogFolder()
    {
        if (Session.StoringDataOnServer)
        {
            if (ServerManager.SessionDataFolderCreated)
                yield return ServerManager.CreateFolder(ServerLog_FolderPath);
        }
        else if (Session.StoringDataLocally)
            Directory.CreateDirectory(LocalLog_FolderPath);
        
        CreatingLogFolder = false;
        LogFolderCreated = true;
    }

    private IEnumerator CreateLogFile()
    {
        if (Session.StoringDataOnServer)
        {
            string content = string.Join("\n", LogMessages.ToArray());
            LogMessages.Clear();
            yield return ServerManager.CreateFileAsync(ServerLog_FilePath, content);
        }
        else if (Session.StoringDataLocally)
        {
            using StreamWriter createFileWriter = File.CreateText(LocalLog_FilePath);
            WriteLogMessagesLocally(createFileWriter);
        }
        LogFileCreated = true;
    }

    private IEnumerator AppendDataToLogFile()
    {
        if (Session.StoringDataOnServer)
        {
            string content = string.Join("\n", LogMessages.ToArray());
            LogMessages.Clear();
            yield return ServerManager.AppendToFileAsync(ServerLog_FilePath, content);
        }
        else if(Session.StoringDataLocally)
        {
            using StreamWriter appendFileWriter = File.AppendText(LocalLog_FilePath);
            WriteLogMessagesLocally(appendFileWriter);
        }
    }


    private void OnApplicationQuit()
    {
        if (!Session.StoreData)
            return;

        if (!LogFolderCreated)
        {
            Debug.LogWarning("TRYING TO WRITE DATA ONAPPLICATIONQUIT BUT LOG FOLDER HASNT BEEN CREATED YET!");
            return; //Do we want this here to stop from getting down below since no log folder created? //or maybe not neccessary cuz creating a file also creates folder?
        }

        if (LogFileCreated)
            StartCoroutine(AppendDataToLogFile());
        else
            StartCoroutine(CreateLogFile());
    }


    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLogMessage;
        Application.quitting -= OnApplicationQuit;
    }

}
