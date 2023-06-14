using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;



public static class ServerManager //Used with the PHP scripts
{
    private static readonly string ServerURL = "http://localhost:8888/";
    private static readonly string RootDataFolderPath = "DATA";
    private static string SessionDataFolder; //Created once after they hit confirm
    public static string SessionDataFolderPath
    {
        get
        {
            return $"{RootDataFolderPath}/{SessionDataFolder}";
        }
    }

    private static readonly string RootConfigFolderPath = "CONFIGS";
    private static string SessionConfigFolder; //Will be whatever they select in the dropdown after hitting confirm
    public static string SessionConfigFolderPath
    {
        get
        {
            return $"{RootConfigFolderPath}/{SessionConfigFolder}";
        }
    }

    private static List<string> folderList = new List<string>();



    public static bool FolderCreated(string folderPath)
    {
        return folderList.Contains(folderPath);
    }

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
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS CREATING FOLDER AT {folderPath}." : $"FAILED! Error Creating Folder! | Error: {request.error}");
        folderList.Add(folderPath);
    }


    public static List<string> GetSessionConfigFolderNames() //WORKS!
    {
        string path = "CONFIGS";
        string url = $"{ServerURL}/getFolderNames.php?directoryPath={path}";
        List<string> folderNames = new List<string>();

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                continue;

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully got the folder names from the server!");
                string plainTextResponse = request.downloadHandler.text;
                string[] folderNameArray = plainTextResponse.Split(',');

                folderNames.AddRange(folderNameArray);
            }
            else
                Debug.Log($"An error occurred while getting folder names. Error: {request.error}");
        }
        return folderNames;
    }

    public static IEnumerator<string> GetConfigFileStringAsync(string searchString, string subFolder = null)
    {
        string path = subFolder == null ? $"/CONFIGS/{SessionConfigFolder}" : $"/CONFIGS/{SessionConfigFolder}/{subFolder}";
        string url = $"{ServerURL}/getFile.php?path={path}&searchString={searchString}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            yield return null;

        yield return request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : null;
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"FILE CONTAINING '{searchString}' FOUND." : $"ERROR SEARCHING FOR FILE CONTAINING: '{searchString}'. | ERROR: {request.error}");
    }


    public static IEnumerator CreateFileAsync(string path, string fileName, string fileHeaders) //Will also replace existing file on server if exists. 
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
        IEnumerator<string> getFileStringCoroutine = GetFileAsync(folderPath, fileName);
        yield return CoroutineHelper.StartCoroutine(getFileStringCoroutine);
        string originalFileContents = getFileStringCoroutine.Current;

        if (originalFileContents != null)
        {
            string path = $"{folderPath}/{fileName}";
            string url = $"{ServerURL}/updateFile.php?path={path}";

            string updatedFileContents = originalFileContents + "\n" + rowData;
            WWWForm formData = new WWWForm();
            formData.AddField("data", updatedFileContents);

            using UnityWebRequest request = UnityWebRequest.Post(url, formData);
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS! Appended to file at {fileName}" : $"FAILED! Error appending to {fileName} | Error: {request.error}");
        }
    }

    public static IEnumerator<string> GetFileAsync(string path, string searchString)
    {
        string url = $"{ServerURL}/getFile.php?path={path}&searchString={searchString}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            yield return null;

        yield return request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : null;
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"FILE CONTAINING '{searchString}' FOUND." : $"ERROR SEARCHING FOR FILE CONTAINING: '{searchString}'. | ERROR: {request.error}");
    }


    public static string GetSessionDataFolderName()
    {
        return SessionDataFolder;
    }

    public static string GetSessionConfigFolderName()
    {
        return SessionConfigFolder;
    }

    public static void SetSessionConfigFolderName(string sessionConfigFolderName)
    {
        SessionConfigFolder = sessionConfigFolderName;
    }

    //public static IEnumerator CreateDataFolder(string folderPath)
    //{
    //    string path = $"DATA/{folderPath}";
    //    string url = $"{ServerURL}/createFolder.php?path={path}";

    //    WWWForm formData = new WWWForm();
    //    formData.AddField("path", path);
    //    using UnityWebRequest request = UnityWebRequest.Post(url, formData);
    //    yield return request.SendWebRequest();
    //    Debug.Log(request.result == UnityWebRequest.Result.Success ? "SUCCESS Creating Data Folder!" : $"FAILED! Error Creating Data Folder! | Error: {request.error}");
    //}

    //public static IEnumerator CreateConfigFolder(string folderPath)
    //{
    //    string path = $"CONFIGS/{folderPath}";
    //    string url = $"{ServerURL}/createFolder.php?path={path}";

    //    WWWForm formData = new WWWForm();
    //    formData.AddField("path", path);
    //    using UnityWebRequest request = UnityWebRequest.Post(url, formData);
    //    yield return request.SendWebRequest();
    //    Debug.Log(request.result == UnityWebRequest.Result.Success ? "SUCCESS Creating Config Folder!" : $"FAILED! Error Creating Config Folder! | Error: {request.error}");
    //}


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
