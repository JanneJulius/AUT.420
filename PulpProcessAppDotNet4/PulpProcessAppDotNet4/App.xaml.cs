using NLog;
using PulpProcessAppDotNet4.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PulpProcessAppDotNet4
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // Global instances accessible throughout the app
        public static LogViewModel LogViewModel { get; private set; }
        public static ProcessCommunicator ProcessCommunicator { get; private set; }
        public static SequenceHandler SequenceHandler { get; private set; }


        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize the ViewModel
            LogViewModel = new LogViewModel(); // Initialize once here

            // Initialize the NLog configuration programmatically
            ConfigureLogging();
            logger.Info("logging configured");

            try
            {
                // Run ProcessCommunicator initialization asynchronously
                bool success = await Task.Run(() =>
                {
                    ProcessCommunicator = new ProcessCommunicator();
                    return ProcessCommunicator.Initialize();
                });

                if (!success)
                {
                    logger.Error("Failed to initialize ProcessCommunicator.");
                    MessageBox.Show("Failed to initialize communication with the process.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    logger.Info("ProcessCommunicator initialized successfully.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during ProcessCommunicator initialization.");
                MessageBox.Show("An error occurred during initialization.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Initialize the SequenceHandler with default values
            SequenceHandler = new SequenceHandler(0, 0, 0, 0);

            // Log application start
            logger.Info("Application started.");

            // Optionally, handle unhandled exceptions to log them
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                logger.Error(ev.ExceptionObject as Exception, "Unhandled exception occurred.");
            };
            DispatcherUnhandledException += (s, ev) =>
            {
                logger.Error(ev.Exception, "Unhandled UI exception occurred.");
                ev.Handled = true;  // Prevents the application from crashing
            };

            // Create and show the main window
            var mainWindow = new MainWindow(); // Replace with your MainWindow class
            mainWindow.Show();
        }

        private void ConfigureLogging()
        {
            // Create a new NLog configuration
            var config = new NLog.Config.LoggingConfiguration();

            // Create and configure the ObservableCollectionTarget
            var observableTarget = new ObservableCollectionTarget
            {
                LogViewModel = LogViewModel,  // Assign the ViewModel to the target
                Layout = "${longdate} ${level} ${message}", // Define the layout
            };

            // Add the target to the configuration
            config.AddTarget("inMemoryLog", observableTarget);

            // Define the logging rules
            config.AddRule(LogLevel.Info, LogLevel.Fatal, observableTarget);

            // Apply the configuration
            LogManager.Configuration = config;
        }
        protected override void OnExit(ExitEventArgs e)
        {
            logger.Info("Application is shutting down.");
            LogManager.Shutdown();  // Ensures all logs are flushed on exit
            base.OnExit(e);
        }
    }
}
