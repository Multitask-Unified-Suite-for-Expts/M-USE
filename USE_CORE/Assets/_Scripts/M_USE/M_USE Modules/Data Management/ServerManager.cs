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



    public static IEnumerator CreateSessionDataFolder(string subjectID, string sessionID) //WORKS!
    {
        SessionDataFolder = "DATA__" + "Session_" + sessionID + "__Subject_" + subjectID + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");
        string path = $"DATA/{SessionDataFolder}";
        string url = $"{ServerURL}/createFolder.php?path={path}";

        WWWForm formData = new WWWForm();
        formData.AddField("path", path);

        using (UnityWebRequest request = UnityWebRequest.Post(url, formData)) //empty body will create a folder
        {
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? "SUCCESS Creating Session Folder!" : $"FAILED! Error Creating Session Folder! | Error: {request.error}");
        }
    }

    public static List<string> GetSessionFolderNames() //WORKS!
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

    public static IEnumerator CreateDataFileAsync(string fileName, string fileHeaders) //WAs working
    {
        Debug.Log("CREATING DATA FILE: " + fileName);

        string path = $"DATA/{SessionDataFolder}/{fileName}";
        string url = $"{ServerURL}/createFile.php?path={path}";

        using (UnityWebRequest request = UnityWebRequest.Put(url, fileHeaders))
        {
            request.method = UnityWebRequest.kHttpVerbPUT;
            request.SetRequestHeader("Content-Type", "text/plain");
            yield return request.SendWebRequest();
            Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS! Created/replaced file: {fileName}" : $"FAILED! Error creating/replacing file: {fileName} | Error: {request.error}");
        }
    }

    //public static IEnumerator AppendDataToFileAsync(string fileName, string rowData)
    //{
    //    Debug.Log("APPENDING DATA TO FILE: " + fileName);

    //    IEnumerator<string> getFileStringCoroutine = GetDataFileStringAsync(fileName);
    //    yield return CoroutineHelper.StartCoroutine(getFileStringCoroutine);

    //    string originalFileContents = getFileStringCoroutine.Current;
    //    if (originalFileContents != null)
    //    {
    //        string updatedFileContents = originalFileContents + "\n" + rowData;

    //        string path = $"DATA/{SessionDataFolder}/{fileName}";
    //        string url = $"{ServerURL}/updateFile.php?path={path}";

    //        WWWForm formData = new WWWForm();
    //        formData.AddField("data", updatedFileContents);

    //        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
    //        {
    //            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
    //            yield return request.SendWebRequest();

    //            Debug.Log(request.result == UnityWebRequest.Result.Success ? $"SUCCESS! Appended to {fileName}" : $"FAILED! Error appending to {fileName} | Error: {request.error}");
    //        }
    //    }
    //}

    //public static IEnumerator<string> GetDataFileStringAsync(string searchString)
    //{
    //    Debug.Log("GETTING FILE STRING ASYNC!");

    //    string path = $"DATA/{SessionDataFolder}";
    //    string url = $"{ServerURL}/getFile.php?path={path}&searchString={searchString}";

    //    using (UnityWebRequest request = UnityWebRequest.Get(url))
    //    {
    //        var operation = request.SendWebRequest();

    //        while (!operation.isDone)
    //            yield return null;

    //        if (request.result == UnityWebRequest.Result.Success)
    //        {
    //            string configFileContents = request.downloadHandler.text;
    //            Debug.Log($"File containing '{searchString}' found. Contents: {configFileContents}");
    //            yield return configFileContents;
    //        }
    //        else
    //        {
    //            Debug.Log($"An error occurred while searching for file containing '{searchString}'. Error: {request.error}");
    //            yield return null;
    //        }
    //    }
    //}



    public static IEnumerator<string> GetConfigFileStringAsync(string searchString, string subFolder = null)
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
