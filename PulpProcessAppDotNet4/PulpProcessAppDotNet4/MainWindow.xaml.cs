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
        Alkutila,
        Kaynnissa,
        Keskeytetty
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ProcessCommunicator processCommunicator;
        private ProcessState currentState;

        public MainWindow()
        {
            InitializeComponent();
            

            // Access the shared ViewModel
            var app = (App)Application.Current;
            //DataContext = app.LogViewModel; // Set the DataContext to the shared ViewModel

            // Initialize the ProcessCommunicator
            processCommunicator = new ProcessCommunicator();
            DataContext = processCommunicator.ProcessData;  // Bind UI to ProcessData

            InitializeState();
            processCommunicator.InitializeAsync().ConfigureAwait(false);
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
        // Button click event to open ParameterWindow, now a state manager too. TODO: Refactor.
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
