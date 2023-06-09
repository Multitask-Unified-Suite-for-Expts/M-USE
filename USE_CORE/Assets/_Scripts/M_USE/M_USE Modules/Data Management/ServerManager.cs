using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using UnityEngine.Networking;



public static class ServerManager
{
    private static readonly string serverURL = "serverURLGoesHere";
    private static string sessionDataFolder;
    public static string sessionConfigFolder; //Will be whatever they select in the dropdown



    public static void SetSessionDataFolder()
    {
        sessionDataFolder = "DATA__" + "Session_" + "__Subject_" + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");
    }

    public static IEnumerator GetConfigFile(string fileName, string subFolder = null)
    {
        string path = subFolder == null ? $"/CONFIGS/{sessionConfigFolder}/{fileName}" : $"/CONFIGS/{sessionConfigFolder}/{subFolder}/{fileName}";
        string url = $"{serverURL}/getConfigFile.php?path={path}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Config file {fileName} read successfully!");
                string configFileContents = request.downloadHandler.text;
                Debug.Log("FILE CONTENTS: " + configFileContents);
            }
            else
                Debug.Log($"An error occurred while reading config file: {fileName} | ErrorMessage: {request.error}");
        }
    }

    public static IEnumerator CreateDataFile(string fileName, string fileHeaders)
    {
        string path = $"/DATA/{sessionConfigFolder}/{fileName}";
        string url = $"{serverURL}/createFile.php?path={path}";

        using (UnityWebRequest request = UnityWebRequest.Put(url, fileHeaders))
        {
            request.method = UnityWebRequest.kHttpVerbPUT;
            request.SetRequestHeader("Content-Type", "text/plain");
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS! Created file: {fileName}" : $"FAILED! Error creating file: {fileName} | Error: {request.error}");
        }
    }

    public static IEnumerator AppendDataToFile(string fileName, string rowData)
    {
        string path = $"/DATA/{sessionConfigFolder}/{fileName}";
        string url = $"{serverURL}/appendData.php?path={path}";

        using (UnityWebRequest request = UnityWebRequest.Post(url, rowData))
        {
            request.SetRequestHeader("Content-Type", "text/plain");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log($"Data appended to file {fileName} successfully!");
            else
                Debug.Log($"An error occurred while appending data to file: {fileName} | ErrorMessage: {request.error}");
        }
    }
}
  