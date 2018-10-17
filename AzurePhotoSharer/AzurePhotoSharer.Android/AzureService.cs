using Microsoft.WindowsAzure.MobileServices;
using Plugin.CurrentActivity;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(AzurePhotoSharer.Droid.AzureService))]

namespace AzurePhotoSharer.Droid
{
    public class AzureService : AzureServiceBase
    {
        protected override async Task AuthenticateUser()
        {
            await Client.LoginAsync(CrossCurrentActivity.Current.Activity,
                                    MobileServiceAuthenticationProvider.MicrosoftAccount,
                                    CallbackName);
        }
    }
}