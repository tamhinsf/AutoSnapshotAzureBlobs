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
        private static readonly string MONITORED_STORAGE_ACCOUNT_CONNECTION_STRING = System.Environment.GetEnvironmentVariable("MonitoredStorageAccount");
 
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
                var storageAccount = CloudStorageAccount.Parse(AUTOSNAPSHOTAZUREBLOBS_MONITORED_STORAGE_ACCOUNT_CONNECTION_STRING);
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
