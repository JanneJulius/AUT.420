using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

public class LogViewModel : INotifyPropertyChanged
{
    public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();

    public void AddLog(string message)
    {
        // Ensure updates are on the UI thread
        Application.Current.Dispatcher.Invoke(() => Logs.Add(message));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
