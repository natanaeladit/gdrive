using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Microsoft.Extensions.Configuration;
using Google.Apis.Download;
using Google.Apis.Upload;
using System.Net.Mime;
using Google;


Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

Console.WriteLine("Connecting!");

#region OAuth2
//UserCredential credential;
//using (var stream = new FileStream("secrets.json", FileMode.Open, FileAccess.Read))
//{
//    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
//        GoogleClientSecrets.Load(stream).Secrets,
//        new[] { DriveService.Scope.Drive },
//        "user", CancellationToken.None, new FileDataStore("Drive.ListMyLibrary"));
//}

//// Create the service.
//var service = new DriveService(new BaseClientService.Initializer()
//{
//    HttpClientInitializer = credential,
//    ApplicationName = "Drive API Sample",
//});
#endregion

#region Service account
var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("secrets.json", optional: false);
IConfiguration config = builder.Build();

ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(config["client_email"])
               {
                   Scopes = new[] { DriveService.Scope.DriveFile, DriveService.Scope.Drive }
               }.FromPrivateKey(config["private_key"]));

// Create the service.
var service = new DriveService(new BaseClientService.Initializer()
{
    HttpClientInitializer = credential,
    ApplicationName = "Drive API Sample"
});

var fileList = service.Files.List();

Console.WriteLine("Connected!");
#endregion

const int KB = 0x400;
const int DownloadChunkSize = 256 * KB;
const string UploadFileName = @"upload.txt";
const string DownloadDirectoryName = @"download";
const string ContentType = @"text/plain";

Google.Apis.Drive.v3.Data.File uploadedFile = null;

try
{
    await UploadFileAsync(service);

    // uploaded succeeded
    Console.WriteLine("\"{0}\" was uploaded successfully", uploadedFile?.Name);
    await DownloadFileAsync(service, uploadedFile?.WebContentLink!);
    await DeleteFileAsync(service, uploadedFile!);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
async Task<IUploadProgress> UploadFileAsync(DriveService service)
{
    var name = UploadFileName;
    if (name.LastIndexOf('\\') != -1)
    {
        name = name.Substring(name.LastIndexOf('\\') + 1);
    }

    var uploadStream = new System.IO.FileStream(UploadFileName, System.IO.FileMode.Open,
        System.IO.FileAccess.Read);

    var insert = service.Files.Create(new Google.Apis.Drive.v3.Data.File { Name = name }, uploadStream, ContentType);

    insert.ChunkSize = FilesResource.CreateMediaUpload.MinimumChunkSize * 2;
    insert.ProgressChanged += Upload_ProgressChanged;
    insert.ResponseReceived += Upload_ResponseReceived;

    return await insert.UploadAsync();
}

async Task DownloadFileAsync(DriveService service, string url)
{
    var downloader = new MediaDownloader(service);
    downloader.ChunkSize = DownloadChunkSize;
    // add a delegate for the progress changed event for writing to console on changes
    downloader.ProgressChanged += Download_ProgressChanged;

    // figure out the right file type base on UploadFileName extension
    var lastDot = UploadFileName.LastIndexOf('.');
    var fileName = DownloadDirectoryName + @"\Download" +
        (lastDot != -1 ? "." + UploadFileName.Substring(lastDot + 1) : "");
    using (var fileStream = new System.IO.FileStream(fileName,
        System.IO.FileMode.Create, System.IO.FileAccess.Write))
    {
        var progress = await downloader.DownloadAsync(url, fileStream);
        if (progress.Status == DownloadStatus.Completed)
        {
            Console.WriteLine(fileName + " was downloaded successfully");
        }
        else
        {
            Console.WriteLine("Download {0} was interpreted in the middle. Only {1} were downloaded. ",
                fileName, progress.BytesDownloaded);
        }
    }
}

async Task DeleteFileAsync(DriveService service, Google.Apis.Drive.v3.Data.File file)
{
    Console.WriteLine("Deleting file '{0}'...", file.Id);
    await service.Files.Delete(file.Id).ExecuteAsync();
    Console.WriteLine("File was deleted successfully");
}

void Download_ProgressChanged(IDownloadProgress progress)
{
    Console.WriteLine(progress.Status + " " + progress.BytesDownloaded);
}

void Upload_ProgressChanged(IUploadProgress progress)
{
    Console.WriteLine(progress.Status + " " + progress.BytesSent);
}

void Upload_ResponseReceived(Google.Apis.Drive.v3.Data.File file)
{
    uploadedFile = file;
}