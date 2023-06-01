using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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