using PulpProcessAppDotNet4.Helpers;
using System.ComponentModel;

namespace PulpProcessAppDotNet4
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ProcessData ProcessData { get; set; }
        public LogViewModel LogViewModel { get; set; }

        public MainViewModel(ProcessCommunicator processCommunicator, LogViewModel logViewModel)
        {
            ProcessData = processCommunicator.ProcessData;
            LogViewModel = logViewModel;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
