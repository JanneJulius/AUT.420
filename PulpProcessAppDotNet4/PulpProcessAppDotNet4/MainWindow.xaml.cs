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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ProcessCommunicator processCommunicator;
        private LogViewModel logViewModel;

        private readonly SequenceHandler sequenceHandler;
        private ProcessStateHandler processStateHandler;

        public MainWindow()
        {
            InitializeComponent();
           
            // Access the shared ViewModel
            var app = (App)Application.Current;

            // Initialize ProcessCommunicator and ensure it's connected
            processCommunicator = App.ProcessCommunicator;
            if (!processCommunicator.IsConnected)
            {
                processCommunicator.Initialize();
            }

            logViewModel = App.LogViewModel;
            DataContext = new MainViewModel(processCommunicator, logViewModel);  // Bind UI to ProcessData and logs.

            // Initialize the SequenceHandler
            sequenceHandler = App.SequenceHandler;

            // Initialize the state handler and subscribe to its events
            processStateHandler = new ProcessStateHandler();
            processStateHandler.StateChanged += OnProcessStateChanged;

            UpdateUI();
        }
        private void UpdateUI()
        {
            // Update the connection status display
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
        // Button click event to open ParameterWindow, now a state manager too.
        private void OnStart(object sender, RoutedEventArgs e)
        {
            if (processStateHandler.CurrentState == ProcessState.Initialized)
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

                    // Change the state to Running (triggers OnProcessStateChanged)
                    processStateHandler.CurrentState = ProcessState.Running;
                }
            }
        }
        private void OnProcessStateChanged(object sender, ProcessState newState)
        {
            switch (newState)
            {
                case ProcessState.Initialized:
                    // Handle Initialized state if necessary
                    break;

                case ProcessState.Running:
                    // Run the sequence in a background thread to avoid UI blocking
                    Task.Run(() =>
                    {
                        bool success = sequenceHandler.RunWholeSequence();
                        if (!success)
                        {
                            // Log error and return to Halted
                            Dispatcher.Invoke(() =>
                            {
                                processStateHandler.CurrentState = ProcessState.Halted;
                                UpdateUI();
                            });
                        }
                        else
                        {
                            processStateHandler.CurrentState = ProcessState.Initialized;
                        }
                    });
                    break;

                case ProcessState.Halted:
                    // Optionally handle any additional cleanup or notifications for pause
                    break;
            }

            // Update the UI to reflect the new state
            Dispatcher.Invoke(UpdateUI);
        }


        private void OnReset(object sender, RoutedEventArgs e)
        {
            // Reset the process state to "Initialized"
            processStateHandler.CurrentState = ProcessState.Initialized;

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
