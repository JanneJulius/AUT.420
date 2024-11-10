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

        public MainWindow()
        {
            InitializeComponent();

            // Access the shared ViewModel
            var app = (App)Application.Current;
            DataContext = app.LogViewModel; // Set the DataContext to the shared ViewModel

            // Initialize the ProcessCommunicator
            processCommunicator = new ProcessCommunicator();
            processCommunicator.InitializeAsync().ConfigureAwait(false);

 
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
