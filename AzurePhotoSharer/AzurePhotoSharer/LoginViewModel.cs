using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace AzurePhotoSharer
{
    public class LoginViewModel : BaseViewModel
    {
        public LoginViewModel()
        {
            LoginCommand = new Command(async () => await Login());
        }

        async Task Login()
        {
            var azureService = DependencyService.Get<IAzureService>();
            if (await azureService.Authenticate())
                await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        public ICommand LoginCommand { get; }
    }
}
