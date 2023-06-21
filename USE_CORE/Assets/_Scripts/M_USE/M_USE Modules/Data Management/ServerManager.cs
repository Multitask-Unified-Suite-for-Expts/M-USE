using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;



public static class ServerManager //Used with the PHP scripts
{
    private static readonly string ServerURL = "http://m-use.psy.vanderbilt.edu:8080";

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

    public static bool SessionDataFolderCreated;


    public static IEnumerator CreateSessionDataFolder(string subjectID, string sessionID)
    {
        SessionDataFolder = "DATA__" + "Session_" + sessionID + "__Subject_" + subjectID + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");
        yield return CreateFolder(SessionDataFolderPath);
        SessionDataFolderCreated = true;
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

    public static IEnumerator CreateFileAsync(string path, string fileName, string content)
    {
        string url = $"{ServerURL}/createFile.php?path={path}";

        using UnityWebRequest request = UnityWebRequest.Put(url, content);
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

                IEnumerator appendCoroutine = WriteFileCoroutine(url, formData);
                CoroutineHelper.StartCoroutine(appendCoroutine);
            }
        });
    }

    private static IEnumerator WriteFileCoroutine(string url, WWWForm formData)
    {
        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
        {
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? $"Success writing file" : $"FAILED writing file! | Error: {request.error}");
        }
    }


    public static IEnumerator GetFileAsync(string path, string searchString, Action<string> callback)
    {
        string url = $"{ServerURL}/getFile.php?path={path}&searchString={searchString}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            yield return null;

        string result = "";
        if (request.result == UnityWebRequest.Result.Success)
        {
            if (result == "File not found")
            {
                result = null;
                Debug.Log("GetFileAsync Server method worked, but the file was not found! File: " + searchString);
            }
            else
            {
                result = request.downloadHandler.text;
                Debug.Log("Found file: " + searchString);
            }
        }
        else
            Debug.Log($"ERROR FINDING FILE: {searchString} | ERROR: {request.error}");

        callback?.Invoke(result);
    }



    public static IEnumerator CopyFolder(string sourcePath, string destinationPath)
    {
        string url = $"{ServerURL}/copyFolder.php?sourcePath={sourcePath}&destinationPath={destinationPath}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? $"Folder copied successfully!" : $"FAILED TO COPY FOLDER! ERROR: {request.error}");
        }
    }

    public static IEnumerator CopyFileAsync(string currentPath, string newPath, string fileName) //not being used i dont think
    {
        yield return GetFileAsync(currentPath, fileName, fileContents =>
        {
            if (fileContents != null)
            {
                string url = $"{ServerURL}/createFile.php?path={newPath}";
                WWWForm formData = new WWWForm();
                formData.AddField("data", fileContents);
                IEnumerator writeFileCoroutine = WriteFileCoroutine(url, formData);
                CoroutineHelper.StartCoroutine(writeFileCoroutine);
            }
            else
                Debug.Log("FILE CONTENTS NULL AFTER TRYING TO GET FILE ASYNC!");
        });
    }

    public static IEnumerator LoadTextureFromServer(string filePath, Action<Texture2D> callback)
    {
        string url = $"{ServerURL}/{filePath}";

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                callback?.Invoke(tex);
            }
            else
            {
                Debug.Log($"Failed to load PNG file. Error: {request.error}");
                callback?.Invoke(null);
            }
        }
    }


    public static string GetSessionDataFolder()
    {
        return SessionDataFolder;
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
