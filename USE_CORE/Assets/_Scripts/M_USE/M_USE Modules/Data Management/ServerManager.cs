using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
//using System.Net.Http;
//using UnityEditor.PackageManager.Requests;
//using Renci.SshNet;
using UnityEngine.Networking;



public static class ServerManager //Used with the PHP scripts
{
    private static readonly string ServerURL = "http://localhost:8888/";
    private static string SessionDataFolder; //Created once after they hit confirm
    public static string SessionConfigFolder; //Will be whatever they select in the dropdown after hitting confirm



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

    public static IEnumerator CreateSessionDataFolder(string subjectID, string sessionID) //WORKS!
    {
        SessionDataFolder = "DATA__" + "Session_" + sessionID + "__Subject_" + subjectID + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");
        string path = $"DATA/{SessionDataFolder}";
        string url = $"{ServerURL}/createFolder.php?path={path}";

        WWWForm formData = new WWWForm();
        formData.AddField("path", path);
        using UnityWebRequest request = UnityWebRequest.Post(url, formData);
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? "SUCCESS Creating Session Folder!" : $"FAILED! Error Creating Session Folder! | Error: {request.error}");
    }

    public static IEnumerator CreateDataSubFolder(string subFolderName)
    {
        string path = $"DATA/{SessionDataFolder}/{subFolderName}";
        string url = $"{ServerURL}/createFolder.php?path={path}";

        WWWForm formData = new WWWForm();
        formData.AddField("path", path);
        using UnityWebRequest request = UnityWebRequest.Post(url, formData);
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS CREATING SUB FOLDER {subFolderName}." : $"FAILED! Error Creating Sub Folder! | Error: {request.error}");

    }

    public static IEnumerator CreateDataFileAsync(string fileName, string fileHeaders) //Will also replace existing file on server if exists. 
    {
        string path = $"DATA/{SessionDataFolder}/{fileName}";
        string url = $"{ServerURL}/createFile.php?path={path}";

        using UnityWebRequest request = UnityWebRequest.Put(url, fileHeaders);
        request.method = UnityWebRequest.kHttpVerbPUT;
        request.SetRequestHeader("Content-Type", "text/plain");
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS! Created/replaced file: {fileName}" : $"FAILED! Error creating/replacing file: {fileName} | Error: {request.error}");
    }



    public static IEnumerator AppendDataToFileAsync(string fileName, string rowData)
    {
        IEnumerator<string> getFileStringCoroutine = GetDataFileStringAsync(fileName);
        yield return CoroutineHelper.StartCoroutine(getFileStringCoroutine);
        string originalFileContents = getFileStringCoroutine.Current;

        if (originalFileContents != null)
        {
            string path = $"DATA/{SessionDataFolder}/{fileName}";
            string url = $"{ServerURL}/updateFile.php?path={path}";

            string updatedFileContents = originalFileContents + "\n" + rowData;
            WWWForm formData = new WWWForm();
            formData.AddField("data", updatedFileContents);

            using UnityWebRequest request = UnityWebRequest.Post(url, formData);
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS! Appended to {fileName}" : $"FAILED! Error appending to {fileName} | Error: {request.error}");
        }
    }

    public static IEnumerator<string> GetDataFileStringAsync(string searchString)
    {
        string path = $"DATA/{SessionDataFolder}";
        string url = $"{ServerURL}/getFile.php?path={path}&searchString={searchString}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            yield return null;

        yield return request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : null;
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"FILE CONTAINING '{searchString}' FOUND." : $"ERROR SEARCHING FOR FILE CONTAINING: '{searchString}'. | ERROR: {request.error}");
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
