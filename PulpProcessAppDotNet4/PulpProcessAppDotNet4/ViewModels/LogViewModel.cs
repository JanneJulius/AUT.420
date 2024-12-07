using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

/// <summary>
/// Represents the current logging model, viewable directly from the UI via data binding.
/// </summary>
/// <remarks>
/// This class provides an <see cref="ObservableCollection{T}"/> of log messages that can be displayed in real-time
/// within the UI. The <see cref="Logs"/> collection is updated on the UI thread to ensure thread safety.
/// </remarks>
public class LogViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the collection of log messages.
    /// </summary>
    /// <remarks>
    /// This collection is bound to the UI, allowing log messages to be displayed in real-time.
    /// </remarks>
    public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();

    /// <summary>
    /// Adds a new log message to the <see cref="Logs"/> collection.
    /// </summary>
    /// <param name="message">The log message to add.</param>
    /// <remarks>
    /// Ensures the <see cref="Logs"/> collection is updated on the UI thread to maintain thread safety.
    /// </remarks>
    public void AddLog(string message)
    {
        // Ensure updates are on the UI thread
        Application.Current.Dispatcher.Invoke(() => Logs.Add(message));
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for a specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
