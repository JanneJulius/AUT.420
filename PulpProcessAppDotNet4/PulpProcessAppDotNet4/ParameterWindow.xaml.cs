using PulpProcessAppDotNet4.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PulpProcessAppDotNet4
{
    /// <summary>
    /// Interaction logic for ParameterWindow.xaml
    /// </summary>
    public partial class ParameterWindow : Window
    {
        public ParameterData ParameterData { get; private set; }
        public ParameterWindow(ProcessCommunicator processCommunicator)
        {
            InitializeComponent();
            DataContext = processCommunicator.ProcessData;
        }

        // Event handler for the "Aseta" button
        private void OnSetParameters(object sender, RoutedEventArgs e)
        {
            List<string> errorMessages = new List<string>();
            try
            {
                // Collect user input and store it in ParameterData
                ParameterData = new ParameterData
                {
                    DurationCooking = double.Parse(DurationCookingTextBox.Text),
                    TargetTemperature = double.Parse(TargetTemperatureTextBox.Text),
                    TargetPressure = double.Parse(TargetPressureTextBox.Text),
                    ImpregnationTime = double.Parse(ImpregnationTimeTextBox.Text)
                };
                if (ParameterData.TargetPressure >= 300)
                {
                    throw new ArgumentOutOfRangeException("Keittopaine", "Target pressure must be between 0 - 300 bars.");
                }
                if(ParameterData.TargetTemperature >= 100)
                {
                    throw new ArgumentOutOfRangeException("Kohdelämpötila", "Target temperature must be between 20 - 100 celcius.");
                }
                if(ParameterData.DurationCooking >= 180)
                {
                    throw new ArgumentOutOfRangeException("Keittoaika", "Cooking duration must be between 0 - 180 seconds.");
                }
                if(ParameterData.ImpregnationTime >= 180)
                {
                    throw new ArgumentOutOfRangeException("Kyllästysaika", "Impregnation time must be between 0 - 180 seconds.");
                }
                // Close the window and set DialogResult to true
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
