using NLog;
using System.Configuration;
using System.Data;
using System.Windows;

namespace PulpProcessApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public LogViewModel LogViewModel { get; private set; } = null!; // Non-nullable field
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize the ViewModel
        LogViewModel = new LogViewModel(); // Initialize once here

        // Initialize the NLog configuration programmatically
        var config = new NLog.Config.LoggingConfiguration();

        // Create and configure the ObservableCollectionTarget
        var observableTarget = new ObservableCollectionTarget
        {
            LogViewModel = LogViewModel,  // Assign the ViewModel to the target
            Layout = "${longdate} ${level} ${message}" // Define the layout
        };

        // Add the target to the configuration
        config.AddTarget("inMemoryLog", observableTarget);

        // Define the logging rules
        config.AddRule(LogLevel.Info, LogLevel.Fatal, observableTarget);

        // Apply the configuration
        LogManager.Configuration = config;

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
    }

    protected override void OnExit(ExitEventArgs e)
    {
        logger.Info("Application is shutting down.");
        LogManager.Shutdown();  // Ensures all logs are flushed on exit
        base.OnExit(e);
    }
}