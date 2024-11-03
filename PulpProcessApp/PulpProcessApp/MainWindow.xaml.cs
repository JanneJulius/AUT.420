using NLog;
using System.Text;
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

namespace PulpProcessApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DispatcherTimer logTimer;

    public MainWindow()
    {
        InitializeComponent();

        // Access the shared ViewModel
        var app = (App)Application.Current;
        DataContext = app.LogViewModel; // Set the DataContext to the shared ViewModel

        // Initialize the timer
        logTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5) // Log every 5 seconds
        };
        logTimer.Tick += LogTimer_Tick;
        logTimer.Start();
    }

    private void LogTimer_Tick(object? sender, EventArgs e)
    {
        // Log some random data and add to ViewModel
        string logMessage = "Test";
        ((App)Application.Current).LogViewModel.AddLog(logMessage); // Use shared ViewModel
    }

    protected override void OnClosed(EventArgs e)
    {
        logTimer.Stop(); // Stop the timer when the window is closed
        base.OnClosed(e);
    }
}
