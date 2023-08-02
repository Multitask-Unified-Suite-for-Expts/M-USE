using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class LogWriter : MonoBehaviour
{
    private readonly List<string> LogMessages = new List<string>();
    public bool StoreDataIsSet; //turned true by sessionLevel script when it sets SessionValues.StoreData's value
    private bool CreatingLogFolder;
    private bool LogFolderCreated;
    private bool LogFileCreated;
    private readonly int Capacity = 50;

    private string ServerLogFolderPath
    {
        get
        {
            if (string.IsNullOrEmpty(ServerManager.SessionDataFolderPath))
            {
                Debug.Log("Trying to Get ServerLogFolderPath but ServerManager.SessionDataFolderPath hasnt been set yet!");
                return null;
            }
            else
            {
                return $"{ServerManager.SessionDataFolderPath}/LogFile";
            }
        }
    }

    private string LocalLogFolderPath
    {
        get
        {
            if (string.IsNullOrEmpty(SessionValues.SessionDataPath))
            {
                Debug.Log("Trying to Get LocalLogFolderPath but SessionValues.SessionDataPath hasnt been set yet!");
                return null;
            }
            else
                return SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "LogFile";
        }
    }

    private string ServerLogFilePath
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

    private string LocalLogFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(SessionValues.SessionDataPath))
            {
                Debug.Log("Trying to Get LocalLogFilePath but SessionValues.SessionDataPath hasnt been set yet!");
                return null;
            }
            else
                return SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "LogFile" + Path.DirectorySeparatorChar + "Player.log";
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

        if (!SessionValues.SessionDef.StoreData) //if storedata is set, but its False, return:
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

        if (LogMessages.Count >= Capacity)
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
        if (SessionValues.WebBuild)
        {
            if (ServerManager.SessionDataFolderCreated)
                yield return ServerManager.CreateFolder(ServerLogFolderPath);
        }
        else
            Directory.CreateDirectory(LocalLogFolderPath);
        
        CreatingLogFolder = false;
        LogFolderCreated = true;
    }

    private IEnumerator CreateLogFile()
    {
        if (SessionValues.WebBuild)
        {
            string content = string.Join("\n", LogMessages.ToArray());
            LogMessages.Clear();
            yield return ServerManager.CreateFileAsync(ServerLogFilePath, content);
        }
        else
        {
            using StreamWriter createFileWriter = File.CreateText(LocalLogFilePath);
            WriteLogMessagesLocally(createFileWriter);
        }
        LogFileCreated = true;
    }

    private IEnumerator AppendDataToLogFile()
    {
        if (SessionValues.WebBuild)
        {
            string content = string.Join("\n", LogMessages.ToArray());
            LogMessages.Clear();
            yield return ServerManager.AppendToFileAsync(ServerLogFilePath, content);
        }
        else
        {
            using StreamWriter appendFileWriter = File.AppendText(LocalLogFilePath);
            WriteLogMessagesLocally(appendFileWriter);
        }
    }


    private void OnApplicationQuit()
    {
        if (!SessionValues.SessionDef.StoreData)
            return;

        if (!LogFolderCreated)
            Debug.Log("TRYING TO WRITE DATA ONAPPLICATIONQUIT BUT LOG FOLDER HASNT BEEN CREATED YET!");

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
