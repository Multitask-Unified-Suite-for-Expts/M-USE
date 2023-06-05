using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using UnityEngine;
using UnityEngine.Networking;


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
        string accessToken = "sl.BfiZbnHQneQM9L5wsCX52l676WY6AURssbTzjVcqlme6n86DsKIp2srHvB414CLPbKQB-c2tPi6jzUUZFmCJzS0n_Y8Ft0AD37cRBj72gKQtWNZaDfKwBpnTj-yY_nTs5wqleJE";

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



