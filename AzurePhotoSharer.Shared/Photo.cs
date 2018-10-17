namespace AzurePhotoSharer.Shared
{
    public class Photo
    {
        public string PhotoBase64 { get; set; }
    }

    public class PhotoMetadata
    {
        public string BlobName { get; set; }
        public string Description { get; set; }
    }
}
