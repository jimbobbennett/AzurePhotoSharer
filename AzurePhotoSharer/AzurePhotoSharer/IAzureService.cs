using AzurePhotoSharer.Shared;
using Plugin.Media.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzurePhotoSharer
{
    public interface IAzureService
    {
        Task<bool> Authenticate();

        Task<bool> IsLoggedIn();

        Task UploadPhoto(MediaFile photo);
        Task<IEnumerable<PhotoMetadata>> SyncPhotos();
    }
}