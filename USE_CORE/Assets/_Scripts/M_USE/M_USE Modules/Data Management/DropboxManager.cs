using Dropbox.Api;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using System.Collections;
using System.Collections.Generic;
using Dropbox.Api.Files;


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
        string accessToken = "sl.BfhXsNM70Vxqx8NnQRCJuk3d79kOCU34QSpNbhFQIoTqKU0gc9ZFh3-Ay6c7YjW6rARXBpqN5i06jbw-s7PiQ20bAF0_Tu17HH4QvcghDw5AWA58-NnNmsAtqAN216p0fj9a5qg";

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


