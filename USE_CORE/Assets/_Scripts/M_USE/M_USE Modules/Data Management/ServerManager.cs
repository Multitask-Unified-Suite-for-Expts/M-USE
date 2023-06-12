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
    private static readonly string ServerURL = "serverURLGoesHere";
    private static string SessionDataFolder; //Created once after they hit confirm
    private static string SessionConfigFolder; //Will be whatever they select in the dropdown after hitting confirm



    public static void SetSessionConfigFolder(string sessionconfigFolder)
    {
        SessionConfigFolder = sessionconfigFolder;
    }

    public static IEnumerator CreateSessionDataFolder(string subjectID, string sessionID)
    {
        SessionDataFolder = "DATA__" + "Session_" + sessionID + "__Subject_" + subjectID + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");
        string path = $"/DATA/{SessionDataFolder}";
        string url = $"{ServerURL}/createFolder.php?path={path}";

        using (UnityWebRequest request = UnityWebRequest.Put(url, string.Empty)) //empty body will create a folder
        {
            request.method = UnityWebRequest.kHttpVerbPUT;
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? "SUCCESS Creating Session Folder!" : $"FAILED! Error Creating Session Folder! | Error: {request.error}");
        }
    }


    public static IEnumerator<List<string>> GetFolderNames(string directoryPath)
    {
        string url = $"{ServerURL}/getFolderNames.php?directoryPath={directoryPath}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                yield return null;

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully got the folder names from the server!");
                string jsonResponse = request.downloadHandler.text;
                List<string> folderNames = JsonUtility.FromJson<List<string>>(jsonResponse);
                yield return folderNames;
            }
            else
                Debug.Log($"An error occurred while getting folder names. Error: {request.error}");
        }
    }

    public static IEnumerator<string> GetFileString(string searchString, string subFolder = null)
    {
        string path = subFolder == null ? $"/CONFIGS/{SessionConfigFolder}" : $"/CONFIGS/{SessionConfigFolder}/{subFolder}";
        string url = $"{ServerURL}/getFile.php?path={path}&searchString={searchString}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                yield return null;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string configFileContents = request.downloadHandler.text;
                Debug.Log($"File containing '{searchString}' found. Contents: {configFileContents}");
                yield return configFileContents;
            }
            else
            {
                Debug.Log($"An error occurred while searching for file containing '{searchString}'. Error: {request.error}");
                yield return null;
            }
        }
    }


    public static IEnumerator CreateDataFile(string fileName, string fileHeaders)
    {
        string path = $"/DATA/{SessionConfigFolder}/{fileName}";
        string url = $"{ServerURL}/createFile.php?path={path}";

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
        string path = $"/DATA/{SessionConfigFolder}/{fileName}";
        string url = $"{ServerURL}/appendData.php?path={path}";

        using (UnityWebRequest request = UnityWebRequest.Post(url, rowData))
        {
            request.SetRequestHeader("Content-Type", "text/plain");
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS! Appended to {fileName}" : $"FAILED! Error appending to {fileName} | Error: {request.error}");
        }
    }
}





//public static class SFTP_ServerManager
//{
//    private static SftpClient sftpClient;

//    private static string hostname = "localhost";
//    private static string username = "ntraczewski";
//    private static string password = "dziadziu";
//    private static int port = 22;

//    private static string mainDataFolderName = "SFTP_TestData";
//    private static string sessionDataFolderName; //Unique session data folder name
//    public static string sessionDataFolderPath
//    {
//        get
//        {
//            return mainDataFolderName + "/" + sessionDataFolderName;
//        }
//    }

//    private static string mainConfigFolderName = "SFTP_TestSessionConfigs";
//    public static string sessionConfigFolderName;
//    public static string sessionConfigFolderPath
//    {
//        get
//        {
//            return mainConfigFolderName + "/" + sessionConfigFolderName;
//        }
//    }

//    private static bool sessionFolderCreated;



//    public static void Init() //Called by InitScreen start method
//    {
//        if (sftpClient != null)
//            return;
//        sftpClient = new SftpClient(hostname, port, username, password);
//        sftpClient.Connect();
//        HandleSessionConfigFolders();
//    }

//    public static void Disconnect()
//    {
//        if (sftpClient.IsConnected)
//            sftpClient.Disconnect();
//    }

//    private static async void HandleSessionConfigFolders()
//    {
//        List<string> folders = await SFTP_ServerManager.GetFolders("SFTP_TestSessionConfigs");

//        FolderDropdown folderDropdown = GameObject.Find("Dropdown").GetComponent<FolderDropdown>();
//        folderDropdown.SetFolders(folders);
//    }

//    public static async Task<List<string>> GetFolders(string sessionConfigFolderName)
//    {
//        try
//        {
//            string path = sessionConfigFolderName; // sessionFolderName + "/" + sessionConfigFolderName;

//            var directoryItems = await Task.Run(() => sftpClient.ListDirectory(path));
//            var folders = directoryItems
//                .Where(item => item.IsDirectory)
//                .Select(item => item.Name)
//                .ToList();

//            return folders;
//        }
//        catch (Exception e)
//        {
//            Debug.Log($"An error occurred while getting immediate folders: {e.Message}");
//        }
//        return new List<string>();
//    }

//    public static void CreateSessionDataFolder(string subjectID, string sessionID)
//    {
//        if (sessionFolderCreated)
//            return;

//        sessionDataFolderName = "DATA__" + "Session_" + sessionID + "__Subject_" + subjectID + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");

//        try
//        {
//            sftpClient.CreateDirectory(sessionDataFolderPath);
//            Debug.Log($"Session folder created at: {sessionDataFolderPath}");
//        }
//        catch (Exception e)
//        {
//            Debug.Log($"AN ERROR OCCURED WHILE CREATING SESSION FOLDER! | ERROR: {e.Message}");
//        }
//        sessionFolderCreated = true;
//    }

//    public static IEnumerator CreateFileWithColumnTitles(string fileName, string fileHeaders)
//    {
//        string remoteFilePath = $"{sessionDataFolderPath}/{fileName}";
//        byte[] fileData = Encoding.UTF8.GetBytes(fileHeaders);
//        UnityWebRequest request = UnityWebRequest.Put(remoteFilePath, fileData);
//        yield return request.SendWebRequest();
//        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"FILE {fileName} CREATED SUCCESSFULLY!" : $"ERROR CREATING FILE {fileName}!");
//    }

//    public static IEnumerator AppendDataToExistingFile(string fileName, string rowData)
//    {
//        string remoteFilePath = $"{sessionDataFolderPath}/{fileName}";
//        byte[] fileData = Encoding.UTF8.GetBytes("\n" + rowData);
//        UnityWebRequest request = new UnityWebRequest(remoteFilePath, "POST");
//        UploadHandlerRaw uploadHandler = new UploadHandlerRaw(fileData);
//        request.uploadHandler = uploadHandler;
//        yield return request.SendWebRequest();
//        Debug.Log(request.result == UnityWebRequest.Result.Success ? $"DATA APPENDED TO FILE: {fileName} SUCCESSFULLY!" : $"ERROR APPENDING DATA TO FILE: {fileName}!");
//    }


//    public static IEnumerator GetFileFromServer(string folderPath, string fileSearchString)
//    {
//        UnityWebRequest www = UnityWebRequest.Get(folderPath);
//        yield return www.SendWebRequest();

//        if (www.result == UnityWebRequest.Result.Success)
//        {
//            string[] files = www.downloadHandler.text.Split('\n');
//            string matchingFile = files.FirstOrDefault(file => file.ToLower().Contains(fileSearchString.ToLower()));

//            if (matchingFile != null)
//            {
//                string remoteFilePath = $"{folderPath}/{matchingFile}";
//                UnityWebRequest request = UnityWebRequest.Get(remoteFilePath);
//                yield return request.SendWebRequest();
//                if (request.result == UnityWebRequest.Result.Success)
//                {
//                    Debug.Log($"SUCCESSFUL GOT FILE FROM SERVER PATH: {remoteFilePath}");
//                    string fileContent = request.downloadHandler.text; //NEED TO DO SOMETHING WITH THE STRING!
//                }
//                else
//                    Debug.Log($"ERROR GETTING FILE FROM SERVER PATH: {remoteFilePath}");
//            }
//            else
//                Debug.Log("NO MATCHING FILE FOUND FOR SEARCH STRING: " + fileSearchString);
//        }
//        else
//            Debug.Log($"ERROR GETTING FILE FROM SERVER PATH: {folderPath}. | ERROR: {www.error}");
//    }

//}
