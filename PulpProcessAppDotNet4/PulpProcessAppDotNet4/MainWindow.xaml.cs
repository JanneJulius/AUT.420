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
    /// 
    public enum ProcessState
    {
        Alkutila,
        Kaynnissa,
        Keskeytetty
    }
    public partial class MainWindow : Window
    {
        private ProcessCommunicator processCommunicator;
        private ProcessState currentState;

        public MainWindow()
        {
            InitializeComponent();


            // Access the shared ViewModel
            var app = (App)Application.Current;
            DataContext = app.LogViewModel; // Set the DataContext to the shared ViewModel

            // Initialize the ProcessCommunicator
            processCommunicator = new ProcessCommunicator();
            //DataContext = processCommunicator.ProcessData;  // Bind UI to ProcessData

            InitializeState();
        }
        private void InitializeState()
        {
            currentState = ProcessState.Alkutila;
            UpdateUI();
        }
        private void UpdateUI()
        {
            // Update the connection status display
            ConnectionStatusTextBlock.Text = processCommunicator.IsConnected ? "Yhteys: online" : "Yhteys: offline";

            switch (currentState)
            {
                case ProcessState.Alkutila:
                    StartPauseButton.Content = "Käynnistä";
                    StatusTextBlock.Text = "alkutila";
                    break;
                case ProcessState.Kaynnissa:
                    StartPauseButton.Content = "Keskeytä";
                    StatusTextBlock.Text = "käynnissä";
                    break;
                case ProcessState.Keskeytetty:
                    StartPauseButton.Content = "Käynnistä";
                    StatusTextBlock.Text = "keskeytetty";
                    break;
            }
        }
        // Button click event to open ParameterWindow, now a state manager too.
        private void OnStart(object sender, RoutedEventArgs e)
        {
            if (currentState == ProcessState.Alkutila)
            {
                ParameterWindow parameterWindow = new ParameterWindow();
                parameterWindow.ShowDialog(); // Use ShowDialog to wait for the window to close

                // After ParameterWindow is closed, update to "käynnissä"
                currentState = ProcessState.Kaynnissa;
                UpdateUI();
            }
            else if (currentState == ProcessState.Kaynnissa)
            {
                // Current implementation works as play/pause button, change if needed
                currentState = ProcessState.Keskeytetty;
                UpdateUI();
            }
            else if (currentState == ProcessState.Keskeytetty)
            {
                // "pause"
                currentState = ProcessState.Kaynnissa;
                UpdateUI();
            }
        }
        private void OnReset(object sender, RoutedEventArgs e)
        {
            // Return to alkutila.
            currentState = ProcessState.Alkutila;
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
                    durationCooking: 300, 
                    targetTemperature: 90.0, 
                    targetPressure: 15.0,
                    impregnationTime: 120, 
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
