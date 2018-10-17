using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace AzurePhotoSharer
{
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            TakePhotoCommand = new Command(async () => await TakePhoto());
            ChoosePhotoCommand = new Command(async () => await ChoosePhoto());
            RefreshPhotosCommand = new Command(async () => await RefreshPhotos());

            RefreshPhotosCommand.Execute(null);
            
            MessagingCenter.Subscribe<IAzureService>(this, "LoggedIn", async s => await RefreshPhotos());
        }

        private async Task RefreshPhotos()
        {
            IsRefreshing = true;
            
            try
            {
                var azureService = DependencyService.Get<IAzureService>();
                if (!await azureService.IsLoggedIn())
                    return;

                var metadata = await azureService.SyncPhotos();
                foreach (var item in metadata.Where(p => Photos.All(x => x.FileName != p.BlobName)))
                {
                    Photos.Add(new PhotoViewModel(item));
                }
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        bool isRefreshing;
        public bool IsRefreshing
        {
            get => isRefreshing;
            set => Set(ref isRefreshing, value);
        }

        public ObservableCollection<PhotoViewModel> Photos { get; } = new ObservableCollection<PhotoViewModel>();

        async Task TakePhoto()
        {
            var options = new StoreCameraMediaOptions { };
            var photo = await CrossMedia.Current.TakePhotoAsync(options);
            await UploadPhoto(photo);
        }

        async Task ChoosePhoto()
        {
            var options = new PickMediaOptions { };
            var photo = await CrossMedia.Current.PickPhotoAsync(options);
            await UploadPhoto(photo);
        }

        async Task UploadPhoto(MediaFile photo)
        {
            if (photo == null) return;
            
            await DependencyService.Get<IAzureService>().UploadPhoto(photo);
            await Task.Delay(10000);
            RefreshPhotosCommand.Execute(null);
        }

        public ICommand TakePhotoCommand { get; }
        public ICommand ChoosePhotoCommand { get; }
        public ICommand RefreshPhotosCommand { get; }
    }
}
