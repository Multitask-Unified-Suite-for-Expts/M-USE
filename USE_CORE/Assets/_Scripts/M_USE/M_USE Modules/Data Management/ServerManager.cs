using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;



public static class ServerManager //Used with the PHP scripts
{
    public static string ServerURL = "http://m-use.psy.vanderbilt.edu:8080"; //will move to serverConfig

    public static string RootDataFolder = "DATA"; //They specify path on new init screen
    private static string SessionDataFolder;
    public static string SessionDataFolderPath
    {
        get
        {
            return $"{RootDataFolder}/{SessionDataFolder}";
        }
    }

    public static string RootConfigFolder = "CONFIGS"; //Marcus wants us to hardcode it. TELL THEM TO NAME FOLDER CONFIGS!
    public static string SessionConfigFolder; //Set with the value of the Dropdown after they click confirm
    public static string SessionConfigFolderPath
    {
        get
        {
            return $"{RootConfigFolder}/{SessionConfigFolder}";
        }
    }

    private static List<string> foldersCreatedList = new List<string>();

    public static bool SessionDataFolderCreated; //used for logWriter


    public static IEnumerator TestServerConnection(Action<bool> callback)
    {
        string url = $"{ServerURL}/testConnection.php";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Server connection test successful!");
            callback?.Invoke(true);
        }
        else
        {
            Debug.Log($"Server connection test failed. Error: {request.error}");
            callback?.Invoke(false);
        }
    }

    public static IEnumerator CreateFolder(string folderPath)
    {
        string url = $"{ServerURL}/createFolder.php?path={folderPath}";
        WWWForm formData = new WWWForm();
        formData.AddField("path", folderPath);
        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
        {
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? $"Successful CreateFolder Request: {request.downloadHandler.text} | FolderPath: {folderPath}" : $"ERROR CREATING FOLDER | Error: {request.error}");
        }
        foldersCreatedList.Add(folderPath);
    }

    public static IEnumerator GetSessionConfigFolders(Action<List<string>> callback)
    {
        string url = $"{ServerURL}/getFolderNames.php?directoryPath={RootConfigFolder}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
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

    public static IEnumerator CreateFileAsync(string path, string fileName, string content)
    {
        string url = $"{ServerURL}/createFile.php?path={path}";
        using UnityWebRequest request = UnityWebRequest.Put(url, content);
        request.method = UnityWebRequest.kHttpVerbPUT;
        request.SetRequestHeader("Content-Type", "text/plain");
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"Successful CreateFile Request: {request.downloadHandler.text} | File: {fileName}" : $"ERROR CREATING FILE: {fileName} | Error: {request.error}");
    }

    public static IEnumerator AppendToFileAsync(string folderPath, string fileName, string rowData)
    {
        yield return GetFileStringAsync(folderPath, fileName, originalFileContentsArray =>
        {
            if (originalFileContentsArray != null)
            {
                string path = $"{folderPath}/{fileName}";
                string url = $"{ServerURL}/updateFile.php?path={path}";

                string updatedFileContents = originalFileContentsArray[1] + "\n" + rowData;
                WWWForm formData = new WWWForm();
                formData.AddField("data", updatedFileContents);

                IEnumerator appendCoroutine = WriteFileCoroutine(url, formData);
                CoroutineHelper.StartCoroutine(appendCoroutine);
            }
            else
                Debug.Log("ORIGINAL CONTENTS IS NULL FOR FILE: " + fileName);
        });
    }

    private static IEnumerator WriteFileCoroutine(string url, WWWForm formData)
    {
        using UnityWebRequest request = UnityWebRequest.Post(url, formData);
        request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"Success writing file to server!" : $"FAILED writing file! | Error: {request.error}");
    }

    public static IEnumerator GetFileStringAsync(string path, string searchString, Action<string[]> callback)
    {
        string url = $"{ServerURL}/getFile.php?path={path}&searchString={searchString}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            yield return null;

        string[] resultArray;
        if (request.result == UnityWebRequest.Result.Success)
        {
            string result = request.downloadHandler.text;

            Debug.Log(result == "File not found" ? ("File NOT Found on Server: " + searchString) : ("Found File On Server: " + searchString));
            if (result == "File not found")
                resultArray = null;
            else
            {
                resultArray = result.Split(new[] { "\n##########\n" }, StringSplitOptions.None);
            }
        }
        else
        {
            resultArray = null;
            Debug.Log($"ERROR FINDING FILE: {searchString} | ERROR: {request.error}");
        }
        callback?.Invoke(resultArray);
    }

    public static IEnumerator GetFileBytesAsync(string path, string searchString, Action<byte[]> callback)
    {
        string url = $"{ServerURL}/getFile.php?path={path}&searchString={searchString}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            yield return null;

        byte[] result = null;
        if (request.result == UnityWebRequest.Result.Success)
        {
            result = request.downloadHandler.data;
            Debug.Log(result.Length == 0 ? ("File Not Found on Server: " + searchString) : ("Found File On Server: " + searchString));
        }
        else
            Debug.Log($"ERROR FINDING FILE: {searchString} | ERROR: {request.error}");

        callback?.Invoke(result);
    }


    public static IEnumerator CopyFolder(string sourcePath, string destinationPath)
    {
        string url = $"{ServerURL}/copyFolder.php?sourcePath={sourcePath}&destinationPath={destinationPath}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"Folder copied successfully!" : $"FAILED TO COPY FOLDER! ERROR: {request.error}");
    }

    public static IEnumerator LoadTextureFromServer(string filePath, Action<Texture2D> callback)
    {
        string url = $"{ServerURL}/{filePath}";

        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(request);
            callback?.Invoke(tex);
        }
        else
        {
            Debug.Log($"FAILED TO LOAD TEXTURE FROM SERVER | ERROR: {request.error}");
            callback?.Invoke(null);
        }
    }

    public static void SetSessionDataFolder(string sessionDataFolder)
    {
        SessionDataFolder = sessionDataFolder;
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
