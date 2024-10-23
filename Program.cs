using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;

Console.WriteLine("Connecting!");

UserCredential credential;
using (var stream = new FileStream("secrets.json", FileMode.Open, FileAccess.Read))
{
    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
        GoogleClientSecrets.Load(stream).Secrets,
        new[] { DriveService.Scope.Drive },
        "user", CancellationToken.None, new FileDataStore("Drive.ListMyLibrary"));
}

// Create the service.
var service = new DriveService(new BaseClientService.Initializer()
{
    HttpClientInitializer = credential,
    ApplicationName = "Drive API Sample",
});