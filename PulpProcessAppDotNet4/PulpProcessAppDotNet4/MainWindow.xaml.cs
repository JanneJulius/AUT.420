using PulpProcessAppDotNet4.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace PulpProcessAppDotNet4
{
    /// <summary>
    /// Represents the main application window that integrates process management and UI updates.
    /// </summary>
    /// <remarks>
    /// This class handles user interaction, updates UI elements, and manages process states via <see cref="ProcessCommunicator"/>, 
    /// <see cref="LogViewModel"/>, <see cref="SequenceHandler"/>, and <see cref="ProcessStateHandler"/>.
    /// </remarks>
    public partial class MainWindow : Window
    {
        private readonly ProcessCommunicator processCommunicator;
        private readonly LogViewModel logViewModel;
        private readonly SequenceHandler sequenceHandler;
        private readonly ProcessStateHandler processStateHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <remarks>
        /// Sets up data contexts, initializes communication, and subscribes to process state changes.
        /// </remarks>
        public MainWindow()
        {
            InitializeComponent();

            var app = (App)Application.Current;

            // Initialize ProcessCommunicator and ensure it's connected
            processCommunicator = App.ProcessCommunicator;
            if (!processCommunicator.IsConnected)
            {
                processCommunicator.Initialize();
            }

            logViewModel = App.LogViewModel;
            DataContext = new MainViewModel(processCommunicator, logViewModel);

            // Initialize the SequenceHandler and ProcessStateHandler
            sequenceHandler = App.SequenceHandler;
            processStateHandler = new ProcessStateHandler();
            processStateHandler.StateChanged += OnProcessStateChanged;

            UpdateUI();
            UpdateButtonState();
        }

        /// <summary>
        /// Updates the UI elements to reflect the current connection and process states.
        /// </summary>
        private void UpdateUI()
        {
            ConnectionStatusTextBlock.Text = processCommunicator.IsConnected ? "Yhteys: online" : "Yhteys: offline";

            switch (processStateHandler.CurrentState)
            {
                case ProcessState.Initialized:
                    StartPauseButton.Content = "Käynnistä";
                    StatusTextBlock.Text = "alkutila";
                    break;
                case ProcessState.Running:
                    StartPauseButton.Content = "Keskeytä";
                    StatusTextBlock.Text = "käynnissä";
                    break;
                case ProcessState.Halted:
                    StartPauseButton.Content = "Käynnistä";
                    StatusTextBlock.Text = "keskeytetty";
                    break;
            }
        }

        /// <summary>
        /// Handles the Start button click event to open the parameter window and update the sequence parameters.
        /// </summary>
        private void OnStart(object sender, RoutedEventArgs e)
        {
            if (processStateHandler.CurrentState == ProcessState.Initialized)
            {
                ParameterWindow parameterWindow = new ParameterWindow(processCommunicator);
                if (parameterWindow.ShowDialog() == true)
                {
                    var parameters = parameterWindow.ParameterData;

                    // Update the SequenceHandler with parameters
                    sequenceHandler.DurationCooking = parameters.DurationCooking;
                    sequenceHandler.TargetTemperature = parameters.TargetTemperature;
                    sequenceHandler.TargetPressure = parameters.TargetPressure;
                    sequenceHandler.ImpregnationTime = parameters.ImpregnationTime;

                    // Change the state to Running
                    processStateHandler.CurrentState = ProcessState.Running;
                }
            }
        }

        /// <summary>
        /// Handles changes in the process state and updates the UI accordingly.
        /// </summary>
        private void OnProcessStateChanged(object sender, ProcessState newState)
        {
            switch (newState)
            {
                case ProcessState.Initialized:
                case ProcessState.Halted:
                    Dispatcher.Invoke(() =>
                    {
                        ConnectionButton.IsEnabled = true; // Enable the button
                    });
                    break;

                case ProcessState.Running:
                    Dispatcher.Invoke(() =>
                    {
                        ConnectionButton.IsEnabled = false; // Disable the button
                    });

                    Task.Run(() =>
                    {
                        bool success = sequenceHandler.RunWholeSequence();
                        Dispatcher.Invoke(() =>
                        {
                            processStateHandler.CurrentState = success ? ProcessState.Initialized : ProcessState.Halted;
                            ConnectionButton.IsEnabled = true; // Re-enable the button after the sequence
                            UpdateUI();
                        });
                    });
                    break;
            }

            Dispatcher.Invoke(UpdateUI);
        }

        /// <summary>
        /// Handles the Reset button click event to reset the process state to Initialized.
        /// </summary>
        private void OnReset(object sender, RoutedEventArgs e)
        {
            processStateHandler.CurrentState = ProcessState.Initialized;
            UpdateUI();
        }

        /// <summary>
        /// Handles the Connection button click event to toggle connection state (connect or disconnect).
        /// </summary>
        private async void ConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectionButton.IsEnabled = false;
            try
            {
                if (processCommunicator.IsConnected)
                {
                    await Task.Run(() => processCommunicator.DisconnectAsync());
                }
                else
                {
                    await Task.Run(() => processCommunicator.ReconnectAsync());
                }

                UpdateButtonState();
                UpdateUI();
            }
            finally
            {
                ConnectionButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Updates the Connection button content based on the current connection state.
        /// </summary>
        private void UpdateButtonState()
        {
            ConnectionButton.Content = processCommunicator.IsConnected ? "Katkaise yhteys" : "Yhdistä";
        }

        /// <summary>
        /// Ensures the process communicator disconnects when the window is closed.
        /// </summary>
        protected override async void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            await Task.Run(() => processCommunicator.DisconnectAsync());
        }
    }
}
