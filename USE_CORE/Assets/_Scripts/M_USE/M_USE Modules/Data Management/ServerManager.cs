using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class ServerManager
{
    private readonly HttpClient httpClient;
    private readonly string serverUrl;

    public ServerManager(string serverUrl)
    {
        httpClient = new HttpClient();
        this.serverUrl = serverUrl;
    }

    public async Task WriteTextFileAsync(string fileName, string content)
    {
        string url = $"{serverUrl}/{fileName}";

        try
        {
            var response = await httpClient.PutAsync(url, new StringContent(content));
            response.EnsureSuccessStatusCode();
            Debug.Log($"File {fileName} written successfully!");
        }
        catch (HttpRequestException e)
        {
            Debug.Log($"An error occurred while reading file: {fileName} | ErrorMessage: {e.Message}");
        }
    }

    public async Task<string> ReadTextFileAsync(string fileName)
    {
        string url = $"{serverUrl}/{fileName}";

        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (HttpRequestException e)
        {
            Debug.Log($"An error occurred while reading file: {fileName} | ErrorMessage: {e.Message}");
            return string.Empty;
        }
    }
}