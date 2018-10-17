using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using AzurePhotoSharer.Shared;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.CognitiveServices.ContentModerator;
using ApiKeyServiceClientCredentials = Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials;

namespace AzurePhotoSharer.Functions
{
    public static class PhotoManager
    {
        static PhotoManager()
        {
            // Nasty hack for local!
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));
            var photoMetadataTable = storageAccount.CreateCloudTableClient().GetTableReference("photometadata");
            var photoContainer = storageAccount.CreateCloudBlobClient().GetContainerReference("photos");
            photoMetadataTable.CreateIfNotExistsAsync();
            photoContainer.CreateIfNotExistsAsync();
        }

        readonly static ContentModeratorClient ContentModeratorClient = 
            new ContentModeratorClient(new ApiKeyServiceClientCredentials(Environment.GetEnvironmentVariable("ContentModeratorKey")))
        {
            Endpoint = "https://westeurope.api.cognitive.microsoft.com/"
        };

        static async Task<bool> IsAllowed(byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes))
            {
                var moderation = await ContentModeratorClient.ImageModeration
                    .EvaluateFileInputAsync(ms);
                if (moderation.IsImageAdultClassified.GetValueOrDefault() ||
                    moderation.IsImageRacyClassified.GetValueOrDefault())
                    return false;
            }

            return true;
        }
        
        [FunctionName("UploadPhoto")]
        public static async Task<IActionResult> UploadPhoto(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "photo/{name}")]HttpRequest req,
            [Blob("photos/{name}", FileAccess.Write, Connection = "StorageConnectionString")] Stream imageStream,
            ILogger log)
        {
            var body = await req.ReadAsStringAsync();
            var content = JsonConvert.DeserializeObject<Photo>(body);
            var imageBytes = Convert.FromBase64String(content.PhotoBase64);

            if (!await IsAllowed(imageBytes))
            {
                log.LogWarning("Naughty image found");
                return new BadRequestResult();
            }
            
            await imageStream.WriteAsync(imageBytes, 0, imageBytes.Length);

            return new OkResult();
        }

        readonly static ComputerVisionClient ComputerVisionClient = 
            new ComputerVisionClient(new ApiKeyServiceClientCredentials(Environment.GetEnvironmentVariable("ComputerVisionKey")))
        {
            Endpoint = "https://westeurope.api.cognitive.microsoft.com/"
        };

        [FunctionName("PhotoBlobTrigger")]
        [return: Table("photometadata", Connection = "StorageConnectionString")]
        public static async Task<CloudPhotoMetadata> PhotoBlobTrigger(
            [BlobTrigger("photos/{name}", Connection = "StorageConnectionString")] Stream imageStream, 
            string name, 
            ILogger log)
        {
            var descriptions = await ComputerVisionClient.DescribeImageInStreamAsync(imageStream);
            var description = descriptions.Captions.FirstOrDefault()?.Text;

            log.LogInformation($"Processing blog {name}");
            return new CloudPhotoMetadata
            {
                PartitionKey = "Photometadata",
                RowKey = name,
                BlobName = name,
                Description = description
            };            
        }

        [FunctionName("GetAllPhotos")]
        public static async Task<IActionResult> GetAllPhotos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "photos")] HttpRequest req,
            [Table("photometadata", Connection = "StorageConnectionString")] CloudTable cloudTable,
            ILogger log)
        {
            var query = new TableQuery<CloudPhotoMetadata>();
            var results = await cloudTable.ExecuteQuerySegmentedAsync(query, new TableContinuationToken());
            return new OkObjectResult(results);
        }

        [FunctionName("DownloadPhoto")]
        public static async Task<IActionResult> DownloadPhoto(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "photo/{name}")]HttpRequest req,
            [Blob("photos/{name}", FileAccess.Read, Connection = "StorageConnectionString")] Stream imageStream,
            ILogger log)
        {
            var bytes = new byte[imageStream.Length];
            await imageStream.ReadAsync(bytes, 0, Convert.ToInt32(imageStream.Length));
            var content = new Photo { PhotoBase64 = Convert.ToBase64String(bytes) };

            return new OkObjectResult(content);
        }
    }
}
