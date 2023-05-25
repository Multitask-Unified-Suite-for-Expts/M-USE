using System.IO;
using System.Text;
using System.Threading.Tasks;
//using Dropbox.Api;


public class DropboxManager
{
    //private DropboxClient client;

    //public async Task Authenticate()
    //{
    //    string accessToken = "sl.BfH-WGNDemLKhSWWuYW1p5eMMs1NDKOxfmZMmUgElpJf9joEcNpzgK1HqFUFpNSre4amJNxCDchV96U30zWZEzPXnNT_ukh55l0A9ZnD9xdEbNH9e8Zx4_27lcr0u1sFuSCJvM0";

    //    var config = new DropboxClientConfig("MUSE_TestData");
    //    client = new DropboxClient(accessToken, config);
    //}

    //public async Task WriteFilesToDropbox()
    //{
    //    string folderPath = "/Apps/MUSE_TestData/";

    //    // Write the file contents
    //    string fileContent = "TEST CONTENT FOR THE FILE!";

    //    byte[] byteArray = Encoding.UTF8.GetBytes(fileContent);
    //    using (var stream = new MemoryStream(byteArray))
    //    {
    //        var response = await client.Files.UploadAsync(
    //            folderPath + "/TestFile1.txt",
    //            Dropbox.Api.Files.WriteMode.Overwrite.Instance,
    //            body: stream);
    //    }
    //}
}