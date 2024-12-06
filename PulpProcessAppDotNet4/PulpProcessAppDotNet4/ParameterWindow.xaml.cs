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
