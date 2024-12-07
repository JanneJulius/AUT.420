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
    /// Represents the entry point of the application and manages global initialization, logging, and shutdown behavior.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// The global logger instance for logging messages throughout the application.
        /// </summary>
        public static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The global log view model used for managing and displaying log messages in the UI.
        /// </summary>
        public static LogViewModel LogViewModel { get; private set; }

        /// <summary>
        /// The global process communicator for handling communication with the external process API.
        /// </summary>
        public static ProcessCommunicator ProcessCommunicator { get; private set; }

        /// <summary>
        /// The global sequence handler for managing process sequences.
        /// </summary>
        public static SequenceHandler SequenceHandler { get; private set; }

        /// <summary>
        /// Handles application startup logic, including initializing global components and logging.
        /// </summary>
        /// <param name="e">The event arguments for the startup event.</param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize the ViewModel
            LogViewModel = new LogViewModel();

            // Configure logging
            ConfigureLogging();
            logger.Info("Logging configured.");

            try
            {
                // Initialize ProcessCommunicator asynchronously
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

            // Handle unhandled exceptions
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
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        /// <summary>
        /// Configures the NLog logging system, including adding a target for in-memory logging.
        /// </summary>
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

        /// <summary>
        /// Handles application shutdown logic, ensuring that all logs are flushed and resources are cleaned up.
        /// </summary>
        /// <param name="e">The event arguments for the exit event.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            logger.Info("Application is shutting down.");
            LogManager.Shutdown();  // Ensures all logs are flushed on exit
            base.OnExit(e);
        }
    }
}
