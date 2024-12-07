using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulpProcessAppDotNet4.Helpers
{
    /// <summary>
    /// Represents the parameter data required for process control and sequences.
    /// </summary>
    public class ParameterData
    {
        /// <summary>
        /// Gets or sets the duration of the cooking sequence in seconds.
        /// </summary>
        /// <remarks>
        /// This parameter defines how long the cooking process will run.
        /// </remarks>
        public double DurationCooking { get; set; }

        /// <summary>
        /// Gets or sets the target temperature for the process in degrees Celsius.
        /// </summary>
        /// <remarks>
        /// This parameter determines the desired temperature during the cooking process.
        /// </remarks>
        public double TargetTemperature { get; set; }

        /// <summary>
        /// Gets or sets the target pressure for the process in bar.
        /// </summary>
        /// <remarks>
        /// This parameter specifies the pressure required during the cooking sequence.
        /// </remarks>
        public double TargetPressure { get; set; }

        /// <summary>
        /// Gets or sets the impregnation time in seconds.
        /// </summary>
        /// <remarks>
        /// This parameter defines the duration for the impregnation phase of the process.
        /// </remarks>
        public double ImpregnationTime { get; set; }
    }
}
