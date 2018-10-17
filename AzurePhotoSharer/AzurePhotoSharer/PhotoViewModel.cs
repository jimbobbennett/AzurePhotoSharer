using AzurePhotoSharer.Shared;
using System.IO;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace AzurePhotoSharer
{
    public class PhotoViewModel : BaseViewModel
    {
        public PhotoViewModel(PhotoMetadata photoMetadata)
        {
            Description = photoMetadata.Description;
            FileName = photoMetadata.BlobName;
            Photo = ImageSource.FromFile(Path.Combine(FileSystem.CacheDirectory, $"{photoMetadata.BlobName}.jpg"));
        }

        public string FileName { get; }
        public string Description { get; }
        public ImageSource Photo { get; }
    }
}
