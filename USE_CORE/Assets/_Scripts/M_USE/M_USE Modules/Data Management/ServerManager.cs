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
using UnityEngine.Networking;


public static class ServerManager //Used with the PHP scripts
{
    public static string ServerURL = ""; //input into initscreen. can eventually remove the value. 

    public static readonly string ServerStimFolderPath = "Resources/Stimuli"; //path to stim folder. currently just having them set it up this way. 
    public static readonly string ServerContextFolderPath = "Resources/Contexts"; //path to contexts folder. currently just having them set it up this way. 

    public static string RootDataFolder = "DATA"; //They specify path on new init screen
    private static string SessionDataFolder;
    public static string SessionDataFolderPath
    {
        get
        {
            return $"{RootDataFolder}/{SessionDataFolder}";
        }
    }

    public static string RootConfigFolder = "CONFIGS"; //We tell them in the documentation the folder needs to be named CONFIGS.
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
        if (TestURL())
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
                Debug.LogWarning($"SERVER CONNECTION TEST FAILED! ERROR: {request.error}");
                callback?.Invoke(false);
            }
        }
        else
            callback?.Invoke(false);
    }

    public static bool TestURL()
    {
        try
        {
            using UnityWebRequest request = UnityWebRequest.Get(ServerURL);
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log("SERVER URL TEST FAILED!  |  " + ex.Message);
            return false;
        }
    }

    public static IEnumerator LoadAudioFromServer(string filePath, Action<AudioClip> callback)
    {
        string url = $"{ServerURL}/{filePath}";

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
            callback?.Invoke(audioClip);
        }
        else
        {
            Debug.Log($"FAILED TO LOAD AUDIO FROM SERVER | ERROR: {request.error}");
            callback?.Invoke(null);
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

    public static IEnumerator CreateFileAsync(string filePath, string content)
    {
        string url = $"{ServerURL}/createFile.php?path={filePath}";
        using UnityWebRequest request = UnityWebRequest.Put(url, content);
        request.method = UnityWebRequest.kHttpVerbPUT;
        request.SetRequestHeader("Content-Type", "text/plain");
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"Successful CreateFile Request: {request.downloadHandler.text} | FilePath: {filePath}" : $"ERROR CREATING FILE AT PATH: {filePath} | Error: {request.error}");
    }

    public static IEnumerator AppendToFileAsync(string filePath, string rowData)
    {
        yield return GetFileStringAsync(filePath, originalFileContentsArray =>
        {
            if (originalFileContentsArray != null)
            {
                string url = $"{ServerURL}/updateFile.php?path={filePath}";

                string updatedFileContents = originalFileContentsArray[1] + "\n" + rowData;
                WWWForm formData = new WWWForm();
                formData.AddField("data", updatedFileContents);

                IEnumerator appendCoroutine = WriteFileCoroutine(url, formData);
                CoroutineHelper.StartCoroutine(appendCoroutine);
            }
            else
                Debug.Log("ORIGINAL CONTENTS IS NULL FOR FILE AT PATH: " + filePath);
        });
    }

    private static IEnumerator WriteFileCoroutine(string url, WWWForm formData)
    {
        using UnityWebRequest request = UnityWebRequest.Post(url, formData);
        request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"Success writing file to server!" : $"FAILED writing file! | Error: {request.error}");
    }


    public static IEnumerator GetFilePath(string folderPath, string searchString, Action<string> callback)
    {
        string url = $"{ServerURL}/getFilePath.php?folderPath={folderPath}&searchString={searchString}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            yield return null;

        if (request.result == UnityWebRequest.Result.Success)
        {
            string result = request.downloadHandler.text;
            Debug.Log(result == "File not found" ? ("File NOT Found on Server: " + searchString) : ("Found File On Server: " + searchString));

            if (result == "File not found")
                callback?.Invoke(null);
            else
                callback?.Invoke(result);
        }
        else
        {
            Debug.Log($"ERROR FINDING FILE: {searchString} | ERROR: {request.error}");
            callback?.Invoke(null);
        }
    }


    public static IEnumerator GetFileStringAsync(string path, Action<string[]> callback)
    {
        string url = $"{ServerURL}/getFile.php?path={path}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            yield return null;

        string[] resultArray;
        if (request.result == UnityWebRequest.Result.Success)
        {
            string result = request.downloadHandler.text;

            Debug.Log(result == "File not found" ? ("File NOT Found on Server at path: " + path) : ("Found File On Server at path: " + path));
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
            Debug.Log($"ERROR FINDING FILE AT PATH: {path} | ERROR: {request.error}");
        }
        callback?.Invoke(resultArray);
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
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(filePath);
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
