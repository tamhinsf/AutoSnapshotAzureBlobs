using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoSnapshotAzureBlobs
{
    public static class AutoSnapshotAzureBlobs
    {
        private static readonly string AUTOSNAPSHOTAZUREBLOBS_CONNECTION_STRING = System.Environment.GetEnvironmentVariable("AutoSnapshotAzureBlobs_ConnectionString");
 
        private static string GetBlobNameFromUrl(string bloblUrl)
        {
            var uri = new Uri(bloblUrl);
            var cloudBlob = new CloudBlob(uri);
            return cloudBlob.Name;
        }
        private static string GetBlobContainerFromUrl(string bloblUrl)
        {
            var uri = new Uri(bloblUrl);
            var cloudBlob = new CloudBlob(uri);
            return cloudBlob.Container.Name;
        }

        [FunctionName("AutoSnapshotAzureBlobs")]
        public static async Task Run(
            [EventGridTrigger]EventGridEvent eventGridEvent,
            ILogger log)
        {
            try
            {
                var createdEvent = ((JObject)eventGridEvent.Data).ToObject<StorageBlobCreatedEventData>();
                if(eventGridEvent.EventType != "Microsoft.Storage.BlobCreated")
                {
                    log.LogInformation($"No snapshot created.  Not a Create Block Blob Event.  You may want to add filters to your Event Grid subscription to reduce the number of Functions executions.");
                    log.LogInformation($"Event Type: {eventGridEvent.EventType}.  Blob Type: {createdEvent.BlobType}");
                    return;
                }
                var storageAccount = CloudStorageAccount.Parse(AUTOSNAPSHOTAZUREBLOBS_CONNECTION_STRING);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(GetBlobContainerFromUrl(createdEvent.Url));
                var blobName = GetBlobNameFromUrl(createdEvent.Url);
                var blockBlob = container.GetBlockBlobReference(blobName);
                await blockBlob.CreateSnapshotAsync();
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                throw;
            }
        }
    }
}
