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

    public enum ProcessState
    {
        Initialized,
        Running,
        Halted
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ProcessCommunicator processCommunicator;
        private LogViewModel logViewModel;
        private ProcessState currentState;
        private readonly SequenceHandler sequenceHandler;

        public MainWindow()
        {
            InitializeComponent();


            // Access the shared ViewModel
            var app = (App)Application.Current;

            // Initialize the ProcessCommunicator
            processCommunicator = new ProcessCommunicator();
            logViewModel = ((App)Application.Current).LogViewModel;
            DataContext = new MainViewModel(processCommunicator, logViewModel);  // Bind UI to ProcessData and logs.

            // Initialize the SequenceHandler with default values
            sequenceHandler = new SequenceHandler(0, 0, 0, 0, processCommunicator);

            InitializeState();
        }
        private void InitializeState()
        {
            currentState = ProcessState.Initialized;
            UpdateUI();
        }
        private void UpdateUI()
        {
            // Update the connection status display
            ConnectionStatusTextBlock.Text = processCommunicator.IsConnected ? "Yhteys: online" : "Yhteys: offline";

            switch (currentState)
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
        // Button click event to open ParameterWindow, now a state manager too. TODO: Refactor.
        private void OnStart(object sender, RoutedEventArgs e)
        {
            if (currentState == ProcessState.Initialized)
            {
                ParameterWindow parameterWindow = new ParameterWindow(processCommunicator);
                if (parameterWindow.ShowDialog() == true)
                {
                    var parameters = parameterWindow.ParameterData;

                    // Update the SequenceHandler with the parameters
                    sequenceHandler.DurationCooking = parameters.DurationCooking;
                    sequenceHandler.TargetTemperature = parameters.TargetTemperature;
                    sequenceHandler.TargetPressure = parameters.TargetPressure;
                    sequenceHandler.ImpregnationTime = parameters.ImpregnationTime;

                    // Run the sequence in a background thread to avoid UI blocking
                    Task.Run(() =>
                    {
                        bool success = sequenceHandler.RunWholeSequence();
                    });

                    // After ParameterWindow is closed, update to "käynnissä"
                    currentState = ProcessState.Running;
                    UpdateUI();
                }
            }
            else if (currentState == ProcessState.Running)
            {
                // Current implementation works as play/pause button, change if needed
                currentState = ProcessState.Halted;
                UpdateUI();
            }
            else if (currentState == ProcessState.Halted)
            {
                // "pause"
                currentState = ProcessState.Running;
                UpdateUI();
            }
        }
        private void OnReset(object sender, RoutedEventArgs e)
        {
            // Return to alkutila.
            currentState = ProcessState.Initialized;
            UpdateUI();
        }

        private async void ConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectionButton.IsEnabled = false; // Disable button during operation
            try
            {
                if (processCommunicator.IsConnected)
                {
                    bool success = await Task.Run(() => processCommunicator.DisconnectAsync());
                }
                else
                {
                    // Run the synchronous Reconnect on a background thread
                    bool success = await Task.Run(() => processCommunicator.ReconnectAsync());
                }

                // Update the button's content based on the new state
                UpdateButtonState();
                UpdateUI();
            }
            catch (Exception ex)
            {
            }
            finally
            {
                ConnectionButton.IsEnabled = true; // Re-enable button
            }
        }

        /// <summary>
        /// Handles the button click event to run the whole sequence.
        /// </summary>
        private async void RunSequenceButton_Click(object sender, RoutedEventArgs e)
        {
            RunSequenceButton.IsEnabled = false; // Disable the button to prevent multiple clicks
            try
            {

                // Initialize a helper for running the sequence
                var sequenceHandler = new SequenceHandler(
                    durationCooking: 30, 
                    targetTemperature: 30, 
                    targetPressure: 115,
                    impregnationTime: 30, 
                    initCommunicator: processCommunicator
                );

                bool success = await Task.Run(() => sequenceHandler.RunWholeSequence());

            }
            catch (Exception ex)
            {

            }
            finally
            {
                RunSequenceButton.IsEnabled = true; // Re-enable the button
            }
        }


        private void UpdateButtonState()
        {
            if (processCommunicator.IsConnected)
            {
                ConnectionButton.Content = "Disconnect";
            }
            else
            {
                ConnectionButton.Content = "Reconnect";
            }
        }

        protected override async void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            bool success = await Task.Run(() => processCommunicator.DisconnectAsync());
        }
    }
}
