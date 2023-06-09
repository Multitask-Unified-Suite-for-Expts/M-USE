using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using UnityEngine.Networking;
using Renci.SshNet;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using UnityEditor.PackageManager.Requests;


public static class ServerManager //Used with the PHP scripts
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





public static class SFTP_ServerManager
{
    private static SftpClient sftpClient;

    private static string hostname = "localhost";
    private static string username = "ntraczewski";
    private static string password = "dziadziu";
    private static int port = 22;

    private static string mainDataFolderName = "SFTP_TestData";
    private static string sessionDataFolderName; //Unique session data folder name
    public static string sessionDataFolderPath
    {
        get
        {
            return mainDataFolderName + "/" + sessionDataFolderName;
        }
    }

    private static string mainConfigFolderName = "SFTP_TestSessionConfigs";
    public static string sessionConfigFolderName;
    public static string sessionConfigFolderPath
    {
        get
        {
            return mainConfigFolderName + "/" + sessionConfigFolderName;
        }
    }

    private static bool sessionFolderCreated;



    public static void Init() //Called by InitScreen start method
    {
        if (sftpClient != null)
            return;
        sftpClient = new SftpClient(hostname, port, username, password);
        sftpClient.Connect();
        HandleSessionConfigFolders();
    }

    public static void Disconnect()
    {
        if (sftpClient.IsConnected)
            sftpClient.Disconnect();
    }

    private static async void HandleSessionConfigFolders()
    {
        List<string> folders = await SFTP_ServerManager.GetFolders("SFTP_TestSessionConfigs");

        FolderDropdown folderDropdown = GameObject.Find("Dropdown").GetComponent<FolderDropdown>();
        folderDropdown.SetFolders(folders);
    }

    public static async Task<List<string>> GetFolders(string sessionConfigFolderName)
    {
        try
        {
            string path = sessionConfigFolderName; // sessionFolderName + "/" + sessionConfigFolderName;

            var directoryItems = await Task.Run(() => sftpClient.ListDirectory(path));
            var folders = directoryItems
                .Where(item => item.IsDirectory)
                .Select(item => item.Name)
                .ToList();

            return folders;
        }
        catch (Exception e)
        {
            Debug.Log($"An error occurred while getting immediate folders: {e.Message}");
        }
        return new List<string>();
    }

    public static void CreateSessionDataFolder(string subjectID, string sessionID)
    {
        if (sessionFolderCreated)
            return;

        sessionDataFolderName = "DATA__" + "Session_" + sessionID + "__Subject_" + subjectID + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");

        try
        {
            sftpClient.CreateDirectory(sessionDataFolderPath);
            Debug.Log($"Session folder created at: {sessionDataFolderPath}");
        }
        catch (Exception e)
        {
            Debug.Log($"AN ERROR OCCURED WHILE CREATING SESSION FOLDER! | ERROR: {e.Message}");
        }
        sessionFolderCreated = true;
    }

    public static IEnumerator CreateFileWithColumnTitles(string fileName, string fileHeaders)
    {
        string remoteFilePath = $"{sessionDataFolderPath}/{fileName}";
        byte[] fileData = Encoding.UTF8.GetBytes(fileHeaders);
        UnityWebRequest request = UnityWebRequest.Put(remoteFilePath, fileData);
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"FILE {fileName} CREATED SUCCESSFULLY!" : $"ERROR CREATING FILE {fileName}!");
    }

    public static IEnumerator AppendDataToExistingFile(string fileName, string rowData)
    {
        string remoteFilePath = $"{sessionDataFolderPath}/{fileName}";
        byte[] fileData = Encoding.UTF8.GetBytes("\n" + rowData);
        UnityWebRequest request = new UnityWebRequest(remoteFilePath, "POST");
        UploadHandlerRaw uploadHandler = new UploadHandlerRaw(fileData);
        request.uploadHandler = uploadHandler;
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"DATA APPENDED TO FILE: {fileName} SUCCESSFULLY!" : $"ERROR APPENDING DATA TO FILE: {fileName}!");
    }


    public static IEnumerator GetFileFromServer(string folderPath, string fileSearchString)
    {
        UnityWebRequest www = UnityWebRequest.Get(folderPath);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string[] files = www.downloadHandler.text.Split('\n');
            string matchingFile = files.FirstOrDefault(file => file.ToLower().Contains(fileSearchString.ToLower()));

            if (matchingFile != null)
            {
                string remoteFilePath = $"{folderPath}/{matchingFile}";
                UnityWebRequest request = UnityWebRequest.Get(remoteFilePath);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"SUCCESSFUL GOT FILE FROM SERVER PATH: {remoteFilePath}");
                    string fileContent = request.downloadHandler.text; //NEED TO DO SOMETHING WITH THE STRING!
                }
                else
                    Debug.Log($"ERROR GETTING FILE FROM SERVER PATH: {remoteFilePath}");
            }
            else
                Debug.Log("NO MATCHING FILE FOUND FOR SEARCH STRING: " + fileSearchString);
        }
        else
            Debug.Log($"ERROR GETTING FILE FROM SERVER PATH: {folderPath}. | ERROR: {www.error}");
    }

}
