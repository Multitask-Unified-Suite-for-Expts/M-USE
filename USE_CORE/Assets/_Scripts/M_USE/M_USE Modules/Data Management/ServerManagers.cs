using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using UnityEngine;
using UnityEngine.Networking;
using Renci.SshNet;
using System.Collections.Generic;
using System.Linq;


public static class SFTP_ServerManager
{
    private static SftpClient sftpClient;

    private static string hostname = "localhost";
    private static string username = "ntraczewski";
    private static string password = "dziadziu";
    private static int port = 22;

    private static string mainFolderName = "SFTP_TestData";
    private static string sessionFolderName;
    private static string sessionFolderPath;


    public static void Init()
    {
        sftpClient = new SftpClient(hostname, port, username, password);
        sessionFolderName = "SessionData_" + DateTime.Now.ToString("MMddyy_HHmmss");
        sessionFolderPath = mainFolderName + "/" + sessionFolderName;
        Connect();
        CreateSessionFolder();
        HandleServerManager();
    }

    private static async void HandleServerManager()
    {
        await SFTP_ServerManager.GetAndSetSessionConfigFolders("SFTP_TestSessionConfigs");
    }

    public static void Connect()
    {
        sftpClient.Connect();
    }

    public static void Disconnect()
    {
        sftpClient.Disconnect();
    }

    private static void CreateSessionFolder()
    {
        try
        {
            sftpClient.CreateDirectory(sessionFolderPath);
            Debug.Log($"Session folder created at: {sessionFolderPath}");
        }
        catch (Exception e)
        {
            Debug.Log($"An error occurred while creating session folder: {e.Message}");
        }
    }

    public static async Task<string> ReadFileFromServerAsync(string folderPath, string fileName)
    {
        string remoteFilePath = $"{folderPath}/{fileName}";

        try
        {
            if (sftpClient.Exists(remoteFilePath))
            {
                Debug.Log("EXTERNAL FILE EXISTS AT " + remoteFilePath);
                using (var stream = new MemoryStream())
                {
                    await Task.Run(() => sftpClient.DownloadFile(remoteFilePath, stream)); //download file into memory stream
                    stream.Position = 0;
                    using (var reader = new StreamReader(stream)) //read content of stream 
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
            else
                Debug.Log($"File ({fileName}) does not exist on the server!");
        }
        catch(Exception e)
        {
            Debug.Log($"An error occurred while reading file: {fileName} | ErrorMessage: {e.Message}");
        }

        return string.Empty;
    }

    public static async Task CreateFileWithColumnTitles(string fileName, string fileHeaders)
    {
        string remoteFilePath = $"{sessionFolderPath}/{fileName}";

        try
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileHeaders)))
            {
                await Task.Run(() => sftpClient.UploadFile(stream, remoteFilePath));
            }
            Debug.Log($"File ({fileName}) created successfully with column titles!");
        }
        catch (Exception e)
        {
            Debug.Log($"An error occurred while creating file: {fileName} | ErrorMessage: {e.Message}");
        }
    }

    public static async Task AppendDataToExistingFile(string fileName, string rowData)
    {
        string remoteFilePath = $"{sessionFolderPath}/{fileName}";

        try
        {
            if (sftpClient.Exists(remoteFilePath))
            {
                using (var stream = new MemoryStream())
                {
                    await Task.Run(() => sftpClient.DownloadFile(remoteFilePath, stream)); // Download existing file
                    byte[] rowDataBytes = Encoding.UTF8.GetBytes("\n" + rowData);
                    stream.Write(rowDataBytes, 0, rowDataBytes.Length);
                    stream.Position = 0; // Reset stream position
                    await Task.Run(() => sftpClient.UploadFile(stream, remoteFilePath)); // Upload modified file
                }
                Debug.Log($"Data appended to file {fileName} successfully!");
            }
            else // If file doesn't exist, create a new file with the rowData
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(rowData)))
                {
                    await Task.Run(() => sftpClient.UploadFile(stream, remoteFilePath));
                }
                Debug.Log($"File {fileName} created and data added successfully!");
            }
        }
        catch (Exception e)
        {
            Debug.Log($"An error occurred while appending data to file: {fileName} | ErrorMessage: {e.Message}");
        }
    }

    public static async Task<List<string>> GetAndSetSessionConfigFolders(string sessionConfigFolderName)
    {
        try
        {
            string path = sessionConfigFolderName; // sessionFolderName + "/" + sessionConfigFolderName;

            var directoryItems = await Task.Run(() => sftpClient.ListDirectory(path));
            var folders = directoryItems
                .Where(item => item.IsDirectory)
                .Select(item => item.Name)
                .ToList();

            FolderDropdown folderDropdown = GameObject.Find("Dropdown").GetComponent<FolderDropdown>();
            folderDropdown.SetFolders(folders);

            return folders;
        }
        catch (Exception e)
        {
            Debug.Log($"An error occurred while getting immediate folders: {e.Message}");
        }
        return new List<string>();
    }


}


public class ServerPHPManager
{
    private readonly string serverUrl;
    public string SessionFolderPath;

    public ServerPHPManager(string serverUrl)
    {
        this.serverUrl = serverUrl;
        SessionFolderPath = "/SessionData_" + DateTime.Now.ToString("MMddyy_HHmmss");
    }

    public IEnumerator CreateFileWithColumnTitles(string fileName, string fileHeaders)
    {
        string url = $"{serverUrl}/create-file.php?sessionFolderPath={SessionFolderPath}&filename={fileName}";

        using (UnityWebRequest request = UnityWebRequest.Put(url, fileHeaders))
        {
            request.method = UnityWebRequest.kHttpVerbPUT;
            request.SetRequestHeader("Content-Type", "text/plain");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log($"File {fileName} created successfully with column titles!");
            else
                Debug.Log($"An error occurred while creating file: {fileName} | ErrorMessage: {request.error}");
        }
    }

    public IEnumerator AppendDataToExistingFile(string fileName, string rowData)
    {
        string url = $"{serverUrl}/append-data.php?sessionFolderPath={SessionFolderPath}&filename={fileName}";

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


public class ServerManager
{
    private readonly HttpClient httpClient;
    private readonly string serverUrl;

    private string sessionFolderPath;


    public ServerManager(string serverUrl) //API's url
    {
        httpClient = new HttpClient();
        this.serverUrl = serverUrl;
        sessionFolderPath = "/SessionData_" + DateTime.Now.ToString("MMddyy_HHmmss");
    }

    public async Task CreateFileWithColumnTitles(string fileName, string fileHeaders)
    {
        string url = $"{serverUrl}/{sessionFolderPath}/{fileName}";

        try
        {
            var response = await httpClient.PutAsync(url, new StringContent(fileHeaders, Encoding.UTF8));
            response.EnsureSuccessStatusCode();
            Debug.Log($"File {fileName} created successfully with column titles!");
        }
        catch (HttpRequestException e)
        {
            Debug.Log($"An error occurred while creating file: {fileName} | ErrorMessage: {e.Message}");
        }
    }

    public async Task AppendDataToExistingFile(string fileName, string rowData)
    {
        string url = $"{serverUrl}/{sessionFolderPath}/{fileName}";

        try
        {
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string existingContent = await response.Content.ReadAsStringAsync();
                string updatedContent = existingContent + "\n" + rowData;
                response = await httpClient.PutAsync(url, new StringContent(updatedContent, Encoding.UTF8));
                response.EnsureSuccessStatusCode();
                Debug.Log($"Data appended to file {fileName} successfully!");
            }
            else //If file doesnt exist, create new file with the rowData
            {
                response = await httpClient.PutAsync(url, new StringContent(rowData, Encoding.UTF8));
                response.EnsureSuccessStatusCode();
                Debug.Log($"File {fileName} created and data added successfully!");
            }
        }
        catch (HttpRequestException e)
        {
            Debug.Log($"An error occurred while appending data to file: {fileName} | ErrorMessage: {e.Message}");
        }
    }
}


public class DropboxManager
{
    public static DropboxClient Client;

    public static string SessionFolderPath;


    public DropboxManager()
    {
        SessionFolderPath = "/SessionData_" + DateTime.Now.ToString("MMddyy_HHmmss");
        HandleAuthentication();
    }

    private async void HandleAuthentication()
    {
        await Authenticate();
    }

    public async Task Authenticate()
    {
        string accessToken = "sl.Bf3uU_Sl7Ot8TxS77zHLTmT2YHiLw3LMxJHfM7RkuykF9BNpG7FmofYDqTeKXJWZkJxWuWtAkuVFB2O3NkeA9TRidlILXWivsqOkmPwjNmK3nq963OL1fhid8I5QL9hDVhdqfNk";

        var config = new DropboxClientConfig("MUSE_TestData");
        Client = new DropboxClient(accessToken, config);
    }

    public async Task CreateFileWithColumnTitles(string fileName, string fileHeaders)
    {
        string filePath = SessionFolderPath + "/" + fileName;

        byte[] byteArray = Encoding.UTF8.GetBytes(fileHeaders);
        using (var stream = new MemoryStream(byteArray))
        {
            var response = await Client.Files.UploadAsync(filePath, WriteMode.Overwrite.Instance, body: stream);
        }
    }

    public async Task AppendDataToExistingFile(string fileName, string rowData)
    {
        string filePath = SessionFolderPath + "/" + fileName;

        try
        {
            Metadata fileMetadata = await Client.Files.GetMetadataAsync(filePath);

            var file = await Client.Files.DownloadAsync(filePath);
            var existingContent = await file.GetContentAsStringAsync();

            string updatedContent = existingContent + "\n" + rowData;

            byte[] byteArray = Encoding.UTF8.GetBytes(updatedContent);
            using (var stream = new MemoryStream(byteArray))
            {
                var response = await Client.Files.UploadAsync(filePath, WriteMode.Overwrite.Instance, body: stream);
            }
        }
        catch (ApiException<GetMetadataError> ex)
        {
            string fileContent = rowData;

            byte[] byteArray = Encoding.UTF8.GetBytes(fileContent);
            using (var stream = new MemoryStream(byteArray))
            {
                var response = await Client.Files.UploadAsync(filePath, WriteMode.Overwrite.Instance, body: stream);
            }
        }
    }


}



