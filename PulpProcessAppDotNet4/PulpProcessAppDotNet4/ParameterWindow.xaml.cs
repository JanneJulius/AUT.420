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
    /// Represents a window for setting process parameters, allowing users to input values for cooking duration, temperature, pressure, and impregnation time.
    /// </summary>
    public partial class ParameterWindow : Window
    {
        /// <summary>
        /// Gets the parameter data entered by the user.
        /// </summary>
        /// <remarks>
        /// This property holds the values for the parameters such as cooking duration, target temperature, target pressure, and impregnation time.
        /// It is populated when the "Set Parameters" button is clicked.
        /// </remarks>
        public ParameterData ParameterData { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterWindow"/> class.
        /// </summary>
        /// <param name="processCommunicator">The <see cref="ProcessCommunicator"/> instance providing the process data for binding.</param>
        public ParameterWindow(ProcessCommunicator processCommunicator)
        {
            InitializeComponent();
            DataContext = processCommunicator.ProcessData;
        }

        /// <summary>
        /// Handles the "Set Parameters" button click event, validating and storing user input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method collects input from the text boxes, validates it, and stores the values in the <see cref="ParameterData"/> property.
        /// If an error occurs during input validation or parsing, an error message is displayed to the user.
        /// </remarks>
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
                // Display error message if input validation or parsing fails
                MessageBox.Show($"Error: {ex.Message}", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
