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
using UnityEditor.PackageManager;

public class DropboxManager
{
    public static DropboxClient Client;

    public static string SessionFolderPath;


    public DropboxManager()
    {
        SessionFolderPath = "/Session_Data_" + DateTime.Now.ToString("MMddyy_HHmmss");
    }

    public async Task Authenticate()
    {
        string accessToken = "sl.BfIxNOA0Sfb1_ofI-QWwZovddNKvcIN7NAuiM6qZpx08FjvCTS9GXyn3VktAT46vmYivOeOJeUxWCI9-7cC9-7ddk-feoBtGswB2YrJj2U_yCWsGX1AwJsX-RiGgscWfvgEWdn0";

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
            var fileMetadata = await Client.Files.GetMetadataAsync(filePath);

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


