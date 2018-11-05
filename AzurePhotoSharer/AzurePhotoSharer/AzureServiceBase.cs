using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;
using AzurePhotoSharer.Shared;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace AzurePhotoSharer
{
    public abstract class AzureServiceBase : IAzureService
    {
        private const string AuthTokenKey = "auth-token";
        private const string UserIdKey = "user-id";

        protected const string CallbackName = "azurephotosharer";
        protected const string AzureAppName = "<Your app name>";
        //protected readonly static string FunctionAppUrl = $"https://{AzureAppName}.azurewebsites.net";
        protected const string FunctionAppUrl = "http://localhost:7071";

        public MobileServiceClient Client { get; }

        protected AzureServiceBase()
        {
            Client = new MobileServiceClient(FunctionAppUrl);
        }
        
        async Task TryLoadUserDetails()
        {
            if (Client.CurrentUser != null) return;

            var authToken = await SecureStorage.GetAsync(AuthTokenKey);
            var userId = await SecureStorage.GetAsync(UserIdKey);

            if (!string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(userId))
            {
                Client.CurrentUser = new MobileServiceUser(userId)
                {
                    MobileServiceAuthenticationToken = authToken
                };

                MessagingCenter.Send<IAzureService>(this, "LoggedIn");
            }
        }

        protected abstract Task AuthenticateUser();

        public async Task<bool> Authenticate()
        {
            if (await IsLoggedIn()) return true;
            await AuthenticateUser();

            if (Client.CurrentUser != null)
            {
                MessagingCenter.Send<IAzureService>(this, "LoggedIn");
                await SecureStorage.SetAsync(AuthTokenKey, Client.CurrentUser.MobileServiceAuthenticationToken);
                await SecureStorage.SetAsync(UserIdKey, Client.CurrentUser.UserId);
                await Application.Current.SavePropertiesAsync();
            }

            return await IsLoggedIn();
        }

        public async Task<bool> IsLoggedIn()
        {
            return true;
            //await TryLoadUserDetails();
            //return Client.CurrentUser != null;
        }

        public async Task UploadPhoto(MediaFile photo)
        {
            using (var s = photo.GetStreamWithImageRotatedForExternalStorage())
            {
                var bytes = new byte[s.Length];
                await s.ReadAsync(bytes, 0, bytes.Length);

                var content = new Photo
                {
                    PhotoBase64 = Convert.ToBase64String(bytes)
                };

                var json = JToken.FromObject(content);

                try
                {
                    await Client.InvokeApiAsync($"photo/{Guid.NewGuid().ToString("N")}", json);
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }
        
        public async Task<IEnumerable<PhotoMetadata>> SyncPhotos()
        {
            try
            {
                var metadata = await Client.InvokeApiAsync<IEnumerable<PhotoMetadata>>("photos", HttpMethod.Get, null);
                
                foreach (var item in metadata)
                {
                    var fileName = Path.Combine(FileSystem.CacheDirectory, $"{item.BlobName}.jpg");
                    if (!File.Exists(fileName))
                    {
                        var photo = await Client.InvokeApiAsync<Photo>($"photo/{item.BlobName}", HttpMethod.Get, null);
                        using (var fs = new FileStream(fileName, FileMode.Create))
                        {
                            var bytes = Convert.FromBase64String(photo.PhotoBase64);
                            await fs.WriteAsync(bytes, 0, bytes.Length);
                        }
                    }
                }

                return metadata;
            }
            catch
            {
                return new List<PhotoMetadata>();
            }
        }
    }
}
