using Microsoft.WindowsAzure.Storage.Table;

namespace AzurePhotoSharer.Functions
{
    public class CloudPhotoMetadata : TableEntity
    {
        public string BlobName { get; set; }
        public string Description { get; set; }
    }
}
