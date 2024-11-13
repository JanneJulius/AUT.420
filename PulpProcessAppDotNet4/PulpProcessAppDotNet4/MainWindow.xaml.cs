﻿using PulpProcessAppDotNet4.Helpers;
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
            InitializeState();

            // Access the shared ViewModel
            var app = (App)Application.Current;
            DataContext = app.LogViewModel; // Set the DataContext to the shared ViewModel

            // Initialize the ProcessCommunicator
            processCommunicator = new ProcessCommunicator();
            processCommunicator.InitializeAsync().ConfigureAwait(false);
        }
        private void InitializeState()
        {
            currentState = ProcessState.Alkutila;
            UpdateUI();
        }
        private void UpdateUI()
        {
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
        // Button click event to open ParameterWindow
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
                // If the process is running, pressing "Keskeytä" pauses it
                currentState = ProcessState.Keskeytetty;
                UpdateUI();
            }
            else if (currentState == ProcessState.Keskeytetty)
            {
                // If paused, pressing "Keskeytä" resumes the process
                currentState = ProcessState.Kaynnissa;
                UpdateUI();
            }
        }
        private void OnReset(object sender, RoutedEventArgs e)
        {
            // Return to the initial state regardless of the current state
            currentState = ProcessState.Alkutila;
            UpdateUI();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
