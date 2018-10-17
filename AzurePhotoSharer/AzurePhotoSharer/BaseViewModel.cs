using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AzurePhotoSharer
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected BaseViewModel() { }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return false;

            field = value;
            RaisePropertyChanged(name);

            return true;
        }

        protected void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
