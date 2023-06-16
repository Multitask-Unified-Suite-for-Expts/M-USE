using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;



public static class ServerManager //Used with the PHP scripts
{
    private static readonly string ServerURL = "http://m-use.psy.vanderbilt.edu:8080/";

    private static string SessionDataFolder; //Set once they hit InitScreen Confirm button
    public static string SessionDataFolderPath
    {
        get
        {
            return $"DATA/{SessionDataFolder}";
        }
    }

    private static string SessionConfigFolder; //Set with the value of the Dropdown after they click confirm
    public static string SessionConfigFolderPath
    {
        get
        {
            return $"CONFIGS/{SessionConfigFolder}";
        }
    }

    private static List<string> foldersCreatedList = new List<string>();



    public static IEnumerator CreateSessionDataFolder(string subjectID, string sessionID)
    {
        SessionDataFolder = "DATA__" + "Session_" + sessionID + "__Subject_" + subjectID + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");
        yield return CreateFolder(SessionDataFolderPath);
    }

    public static IEnumerator CreateFolder(string folderPath)
    {
        string url = $"{ServerURL}/createFolder.php?path={folderPath}";

        WWWForm formData = new WWWForm();
        formData.AddField("path", folderPath);
        using UnityWebRequest request = UnityWebRequest.Post(url, formData);
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"Success creating folder at: {folderPath}." : $"FAILED! Error Creating Folder! | Error: {request.error}");
        foldersCreatedList.Add(folderPath);
    }


    public static IEnumerator GetSessionConfigFolders(Action<List<string>> callback)
    {
        string url = $"{ServerURL}/getFolderNames.php?directoryPath=CONFIGS";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();
            yield return operation;

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully got the folder names from the server!");
                string plainTextResponse = request.downloadHandler.text;
                string[] folderNameArray = plainTextResponse.Split(',');
                List<string> folderNames = new List<string>(folderNameArray);
                callback?.Invoke(folderNames);
            }
            else
            {
                Debug.Log($"An error occurred while getting folder names. Error: {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    public static IEnumerator CreateFileAsync(string path, string fileName, string fileHeaders) 
    {
        string url = $"{ServerURL}/createFile.php?path={path}";

        using UnityWebRequest request = UnityWebRequest.Put(url, fileHeaders);
        request.method = UnityWebRequest.kHttpVerbPUT;
        request.SetRequestHeader("Content-Type", "text/plain");
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS! Created file: {fileName}" : $"FAILED! Error creating file: {fileName} | Error: {request.error}");
    }




    public static IEnumerator AppendToFileAsync(string folderPath, string fileName, string rowData)
    {
        yield return GetFileAsync(folderPath, fileName, originalFileContents =>
        {
            if (originalFileContents != null)
            {
                string path = $"{folderPath}/{fileName}";
                string url = $"{ServerURL}/updateFile.php?path={path}";

                string updatedFileContents = originalFileContents + "\n" + rowData;
                WWWForm formData = new WWWForm();
                formData.AddField("data", updatedFileContents);

                IEnumerator appendCoroutine = AppendToFileCoroutine(url, formData, fileName);
                CoroutineHelper.StartCoroutine(appendCoroutine);
            }
        });
    }

    private static IEnumerator AppendToFileCoroutine(string url, WWWForm formData, string fileName)
    {
        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
        {
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS! Appended to file at {fileName}" : $"FAILED! Error appending to {fileName} | Error: {request.error}");
        }
    }


    public static IEnumerator GetFileAsync(string path, string searchString, Action<string> callback)
    {
        string url = $"{ServerURL}/getFile.php?path={path}&searchString={searchString}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            yield return null;

        string result = request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : null;
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"FILE CONTAINING '{searchString}' FOUND." : $"ERROR SEARCHING FOR FILE CONTAINING: '{searchString}'. | ERROR: {request.error}");

        callback?.Invoke(result);
    }


    public static void SetSessionConfigFolderName(string sessionConfigFolderName) //Used to Set Session Config folder name based on what they picked in dropdown!
    {
        SessionConfigFolder = sessionConfigFolderName;
    }

    public static bool FolderCreated(string folderPath)
    {
        return foldersCreatedList.Contains(folderPath);
    }


}






public static class CoroutineHelper
{
    private class CoroutineHolder : MonoBehaviour { }
    private static CoroutineHolder holder;
    private static CoroutineHolder Holder
    {
        get
        {
            if (holder == null)
            {
                holder = new GameObject("CoroutineHolder").AddComponent<CoroutineHolder>();
                UnityEngine.Object.DontDestroyOnLoad(holder.gameObject);
            }
            return holder;
        }
    }

    public static Coroutine StartCoroutine(IEnumerator coroutine)
    {
        return Holder.StartCoroutine(coroutine);
    }

    public static void StopCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            Holder.StopCoroutine(coroutine);
        }
    }
}
