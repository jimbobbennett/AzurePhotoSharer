using Microsoft.WindowsAzure.MobileServices;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(AzurePhotoSharer.UWP.AzureService))]

namespace AzurePhotoSharer.UWP
{
    public class AzureService : AzureServiceBase
    {
        protected override async Task AuthenticateUser()
        {
            await Client.LoginAsync(MobileServiceAuthenticationProvider.MicrosoftAccount, CallbackName);
        }
    }
}
