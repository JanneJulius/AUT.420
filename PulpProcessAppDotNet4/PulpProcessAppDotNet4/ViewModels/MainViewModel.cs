using PulpProcessAppDotNet4.Helpers;
using System.ComponentModel;

namespace PulpProcessAppDotNet4
{
    /// <summary>
    /// Represents the main view model for the application, combining process data and logging functionality.
    /// </summary>
    /// <remarks>
    /// This view model acts as a central data context for the application's UI, providing access to process data
    /// and log messages for real-time updates.
    /// </remarks>
    public class MainViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the process data model used for UI bindings.
        /// </summary>
        /// <remarks>
        /// The <see cref="ProcessData"/> instance provides real-time data about the application's processes.
        /// </remarks>
        public ProcessData ProcessData { get; set; }

        /// <summary>
        /// Gets or sets the log view model used for managing and displaying log messages.
        /// </summary>
        /// <remarks>
        /// The <see cref="LogViewModel"/> instance allows log messages to be displayed in the UI.
        /// </remarks>
        public LogViewModel LogViewModel { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="processCommunicator">The <see cref="ProcessCommunicator"/> instance used to manage process data.</param>
        /// <param name="logViewModel">The <see cref="LogViewModel"/> instance used to manage log messages.</param>
        /// <remarks>
        /// This constructor links the process communicator's data and the log view model to the main view model for centralized access.
        /// </remarks>
        public MainViewModel(ProcessCommunicator processCommunicator, LogViewModel logViewModel)
        {
            ProcessData = processCommunicator.ProcessData;
            LogViewModel = logViewModel;
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
}
