using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using Tuni.MppOpcUaClientLib;
using System.Diagnostics;
using NLog;

namespace PulpProcessAppDotNet4.Helpers
{
    /// <summary>
    /// Handles the execution and management of process sequences in the pulp processing application.
    /// </summary>
    public class SequenceHandler
    {
        // Main sequence names
        /// <summary>
        /// Represents the Impregnation sequence name.
        /// </summary>
        private const string IMPREGNATION = "Impregnation";

        /// <summary>
        /// Represents the Black Liquor Fill sequence name.
        /// </summary>
        private const string BLACK_LIQUOR_FILL = "Black liquor fill";

        /// <summary>
        /// Represents the White Liquor Fill sequence name.
        /// </summary>
        private const string WHITE_LIQUOR_FILL = "White liquor fill";

        /// <summary>
        /// Represents the Cooking sequence name.
        /// </summary>
        private const string COOKING = "Cooking";

        /// <summary>
        /// Represents the Discharge sequence name.
        /// </summary>
        private const string DISCHARGE = "Discharge";

        /// <summary>
        /// The name of the currently active sequence.
        /// </summary>
        public string activeSequence;

        /// <summary>
        /// Indicates whether the entire sequence is completed.
        /// </summary>
        public bool isWholeSequenceComplete = false;

        /// <summary>
        /// Indicates if an error has occurred in the sequence.
        /// </summary>
        public bool hasSequenceError = false;

        /// <summary>
        /// Gets or sets the duration of the cooking sequence in seconds.
        /// </summary>
        public double DurationCooking { get; set; }

        /// <summary>
        /// Gets or sets the target temperature for the cooking sequence in degrees Celsius.
        /// </summary>
        public double TargetTemperature { get; set; }

        /// <summary>
        /// Gets or sets the target pressure for the cooking sequence in bar.
        /// </summary>
        public double TargetPressure { get; set; }

        /// <summary>
        /// Gets or sets the impregnation duration in seconds.
        /// </summary>
        public double ImpregnationTime { get; set; }

        /// <summary>
        /// Control value for valve V104, ranging from 0 (closed) to 100 (fully open).
        /// </summary>
        private double V104ControlValue = 100;

        /// <summary>
        /// Logger instance for logging sequence-related information.
        /// </summary>
        private static Logger log = App.logger;

        /// <summary>
        /// Instance of the MppClient used for API calls to control process items.
        /// </summary>
        private MppClient apiClient;

        /// <summary>
        /// Instance of the ProcessCommunicator used to interact with process data.
        /// </summary>
        private ProcessCommunicator communicator = App.ProcessCommunicator;

        /// <summary>
        /// Thread that handles the sequence execution logic.
        /// </summary>
        private Thread sequencedrivethread;



        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceHandler"/> class.
        /// </summary>
        /// <param name="durationCooking">The duration of the cooking sequence in seconds.</param>
        /// <param name="targetTemperature">The target temperature for the cooking sequence in degrees Celsius.</param>
        /// <param name="targetPressure">The target pressure for the cooking sequence in bar.</param>
        /// <param name="impregnationTime">The duration of the impregnation sequence in seconds.</param>
        public SequenceHandler(double durationCooking, double targetTemperature, double targetPressure, double impregnationTime)
        {
            log.Info("Sequence Driver started");

            DurationCooking = durationCooking;
            TargetTemperature = targetTemperature;
            TargetPressure = targetPressure;
            ImpregnationTime = impregnationTime;

            apiClient = communicator.apiClient;

            // Thread that handles sequence logic
            sequencedrivethread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
            });
            sequencedrivethread.Start();
        }

        /// <summary>
        /// Runs the complete sequence, including Impregnation, Black Liquor Fill, White Liquor Fill, Cooking, and Discharge.
        /// </summary>
        /// <remarks>
        /// This sequence ensures that all operations are executed in the proper order and aborts if any step fails.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the entire sequence completes successfully; otherwise, <c>false</c>.
        /// </returns>
        public bool RunWholeSequence()
        {
            try
            {
                log.Info("Starting whole sequence...");

                if (apiClient == null)
                {
                    log.Error("Client not found, reinitializing...");
                    communicator.Initialize();
                    apiClient = communicator.apiClient;

                    if (apiClient == null)  // Check again
                    {
                        log.Error("Failed to reconnect the client.");
                        return false;
                    }
                }
                apiClient.SetOnOffItem("P100_P200_PRESET", true);

                // Step 1: Impregnation
                if (!RunImpregnation())
                {
                    log.Error("Failed during Impregnation. Aborting...");
                    return false;
                }

                // Step 2: Black Liquor Fill
                if (!RunBlackLiquorFill())
                {
                    log.Error("Failed during Black Liquor Fill. Aborting...");
                    return false;
                }

                // Step 3: White Liquor Fill
                if (!RunWhiteLiquorFill())
                {
                    log.Error("Failed during White Liquor Fill. Aborting...");
                    return false;
                }

                // Step 4: Cooking
                if (!RunCooking())
                {
                    log.Error("Failed during Cooking. Aborting...");
                    return false;
                }

                // Step 5: Discharge
                if (!RunDischarge())
                {
                    log.Error("Failed during Discharge. Aborting...");
                    return false;
                }

                log.Info("Whole sequence completed successfully!");

                ResetToInitialState();
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Unknown error during whole sequence: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resets all process components to their initial state.
        /// </summary>
        /// <remarks>
        /// Ensures that all valves are closed, pumps are turned off, heaters are turned off,
        /// and any other components are returned to their starting conditions.
        /// </remarks>
        public bool ResetToInitialState()
        {
            try
            {
                log.Info("Resetting process to initial state...");

                // Close all valves
                apiClient.SetOnOffItem("V103", false);
                apiClient.SetOnOffItem("V201", false);
                apiClient.SetOnOffItem("V204", false);
                apiClient.SetOnOffItem("V301", false);
                apiClient.SetOnOffItem("V302", false);
                apiClient.SetOnOffItem("V303", false);
                apiClient.SetOnOffItem("V304", false);
                apiClient.SetOnOffItem("V401", false);
                apiClient.SetOnOffItem("V404", false);

                // Set valve openings to 0 (fully closed)
                apiClient.SetValveOpening("V102", 0);
                apiClient.SetValveOpening("V104", 0);

                // Turn off all pumps
                apiClient.SetPumpControl("P100", 0);
                apiClient.SetPumpControl("P200", 0);

                // Turn off all heaters
                apiClient.SetOnOffItem("E100", false);

                log.Info("Process successfully reset to initial state.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error while resetting process to initial state: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs the Impregnation sequence.
        /// </summary>
        /// <remarks>
        /// Handles the Impregnation step, including opening and closing valves, activating pumps, and waiting for specific conditions.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the sequence completes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool RunImpregnation()
        {
            try
            {
                log.Info("Starting sequence: Impregnation");
                activeSequence = IMPREGNATION;

                // Initial operations
                if (!EM2_OP1()) throw new Exception("Failed to execute EM2_OP1 (Open impregnation outlet).");
                if (!EM5_OP1()) throw new Exception("Failed to execute EM5_OP1 (Open route to digester/T300 and pump P200).");
                if (!EM3_OP2()) throw new Exception("Failed to execute EM3_OP2 (Open inlet and outlet to impregnation tank T200).");

                // Wait until LS+300 is activated
                WaitForCondition(() => communicator.ProcessData.LSplus300, TimeSpan.FromSeconds(30), "LS+300 was not activated.");

                // Closing outlets
                if (!EM3_OP1()) throw new Exception("Failed to execute EM3_OP1 (Close outlets).");

                // Wait for impregnation time
                log.Info($"Waiting for impregnation duration: {ImpregnationTime} seconds.");
                Thread.Sleep(TimeSpan.FromSeconds(ImpregnationTime));

                // Final operations
                if (!EM2_OP2()) throw new Exception("Failed to execute EM2_OP2 (Close impregnation outlet).");
                if (!EM5_OP3()) throw new Exception("Failed to execute EM5_OP3 (Close route to digester/T300 and pump off).");
                if (!EM3_OP6()) throw new Exception("Failed to execute EM3_OP6 (Close all inlets and outlets).");
                if (!EM3_OP8()) throw new Exception("Failed to execute EM3_OP8 (Depressurize digester/T300).");

                log.Info("Sequence completed: Impregnation");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in Impregnation sequence: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs the Black Liquor Fill sequence.
        /// </summary>
        /// <remarks>
        /// Handles filling the black liquor tank by opening valves, starting pumps, and monitoring levels.
        /// ”LI400 indicates that enough liquor has been displaced" => LI400 is less than 35mm
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the sequence completes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool RunBlackLiquorFill()
        {
            try
            {
                log.Info("Starting sequence: Black Liquor Fill");
                activeSequence = BLACK_LIQUOR_FILL;

                // Initial operations
                if (!EM3_OP2()) throw new Exception("Failed to execute EM3_OP2 (Open inlet/outlet to Tank T200).");
                if (!EM5_OP1()) throw new Exception("Failed to execute EM5_OP1 (Open route to digester/T300 and pump P200).");
                if (!EM4_OP1()) throw new Exception("Failed to execute EM4_OP1 (Open outlet for black liquor fill).");

                // Wait for LI400 level
                WaitForCondition(() => communicator.ProcessData.LI400 < 35, TimeSpan.FromSeconds(100), "LI400 did not reach the required level.");

                // Final operations
                if (!EM3_OP6()) throw new Exception("Failed to execute EM3_OP6 (Close all inlets and outlets).");
                if (!EM5_OP3()) throw new Exception("Failed to execute EM5_OP3 (Close route to digester/T300 and turn off pump P200).");
                if (!EM4_OP2()) throw new Exception("Failed to execute EM4_OP2 (Close black liquor fill outlet).");

                log.Info("Sequence completed: Black Liquor Fill");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in Black Liquor Fill sequence: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs the White Liquor Fill sequence.
        /// </summary>
        /// <remarks>
        /// Handles filling the white liquor tank by opening valves, starting pumps, and monitoring levels.
        /// ”LI400 indicates that enough liquor has been displaced" => LI400 is more than 80 mm
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the sequence completes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool RunWhiteLiquorFill()
        {
            try
            {
                log.Info("Starting sequence: White Liquor Fill");
                activeSequence = WHITE_LIQUOR_FILL;

                // Initial operations
                if (!EM3_OP3()) throw new Exception("Failed to execute EM3_OP3 (Open inlet/outlet to Black Liquor Tank T400).");
                if (!EM1_OP2()) throw new Exception("Failed to execute EM1_OP2 (Open route to digester/T300 and pump P100).");

                // Wait for LI400 level
                WaitForCondition(() => communicator.ProcessData.LI400 > 80, TimeSpan.FromSeconds(100), "LI400 did not reach the required level.");

                // Final operations
                if (!EM3_OP6()) throw new Exception("Failed to execute EM3_OP6 (Close all inlets and outlets).");
                if (!EM1_OP4()) throw new Exception("Failed to execute EM1_OP4 (Close route to digester/T300 and turn off pump P100).");

                log.Info("Sequence completed: White Liquor Fill");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in White Liquor Fill sequence: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Runs the Cooking sequence.
        /// </summary>
        /// <remarks>
        /// Handles the cooking step by opening valves, regulating temperature and pressure, and monitoring conditions.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the sequence completes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool RunCooking()
        {
            try
            {
                log.Info("Starting sequence: Cooking");
                activeSequence = COOKING;

                // Initial setup
                if (!EM3_OP4()) throw new Exception("Failed to execute EM3_OP4 (Open inlet/outlet to White Liquor Tank T100).");
                if (!EM1_OP1()) throw new Exception("Failed to execute EM1_OP1 (Open route to digester/T300, pump P100, and heat).");

                // Wait for target temperature
                WaitForCondition(() => communicator.ProcessData.TI300 >= TargetTemperature, TimeSpan.FromSeconds(300), "TI300 did not reach the target cooking temperature.");

                // Close outlets and prepare for regulated cooking
                if (!EM3_OP1()) throw new Exception("Failed to execute EM3_OP1 (Close outlets).");
                if (!EM1_OP2()) throw new Exception("Failed to execute EM1_OP2 (Open route to digester/T300 and pump P100).");

                // Regulate pressure and temperature
                if (!U1_OP1()) throw new Exception("Failed to execute U1_OP1 (Regulate digester/T300 pressure).");
                if (!U1_OP2()) throw new Exception("Failed to execute U1_OP2 (Regulate digester/T300 temperature).");

                // Wait for cooking duration
                log.Info($"Waiting for cooking duration: {DurationCooking} seconds.");
                Thread.Sleep(TimeSpan.FromSeconds(DurationCooking));

                // Stop regulation and finalize
                if (!U1_OP3()) throw new Exception("Failed to execute U1_OP3 (Stop digester/T300 pressure regulation).");
                if (!U1_OP4()) throw new Exception("Failed to execute U1_OP4 (Stop digester/T300 temperature regulation).");
                if (!EM3_OP6()) throw new Exception("Failed to execute EM3_OP6 (Close all inlets and outlets).");
                if (!EM1_OP4()) throw new Exception("Failed to execute EM1_OP4 (Close route to digester/T300 and stop pump P100).");
                if (!EM3_OP8()) throw new Exception("Failed to execute EM3_OP8 (Depressurize digester/T300).");

                log.Info("Sequence completed: Cooking");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in Cooking sequence: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs the Discharge sequence.
        /// </summary>
        /// <remarks>
        /// Handles the discharge step by opening valves, depressurizing, and monitoring conditions.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the sequence completes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool RunDischarge()
        {
            try
            {
                log.Info("Starting sequence: Discharge");
                activeSequence = DISCHARGE;

                // Initial operations
                if (!EM5_OP2()) throw new Exception("Failed to execute EM5_OP2 (Open route to White Liquor Tank T100 and start pump P200).");
                if (!EM3_OP5()) throw new Exception("Failed to execute EM3_OP5 (Allow digester/T300 discharge).");

                // Wait for LS-300 deactivation
                WaitForCondition(() => !communicator.ProcessData.LSminus300, TimeSpan.FromSeconds(100), "LS-300 was not deactivated.");

                // Final operations
                if (!EM5_OP4()) throw new Exception("Failed to execute EM5_OP4 (Close route to White Liquor Tank T100 and stop pump P200).");
                if (!EM3_OP7()) throw new Exception("Failed to execute EM3_OP7 (Close discharge outlets).");

                log.Info("Sequence completed: Discharge");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in Discharge sequence: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Waits for a specific condition to become true within a given timeout period.
        /// </summary>
        /// <param name="condition">The condition to evaluate as a <see cref="Func{Boolean}"/>.</param>
        /// <param name="timeout">The maximum amount of time to wait for the condition to be met.</param>
        /// <param name="errorMessage">The error message to include if the timeout is exceeded.</param>
        /// <exception cref="TimeoutException">Thrown when the condition is not met within the timeout period.</exception>
        private void WaitForCondition(Func<bool> condition, TimeSpan timeout, string errorMessage)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException(errorMessage);
                }
                Thread.Sleep(50); // Avoid CPU overuse by sleeping briefly
            }
        }

        /// <summary>
        /// Executes the EM1_OP1 operation to open valves, turn on a pump, and turn on a heater.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V102.  
        /// PH2: Open valve V304.  
        /// PH3: Turn on pump P100.  
        /// PH4: Turn on heater E100.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM1_OP1()
        {
            try
            {
                apiClient.SetValveOpening("V102", 100);
                apiClient.SetOnOffItem("V304", true);
                apiClient.SetPumpControl("P100", 100);
                apiClient.SetOnOffItem("E100", true);
                log.Info("EM1_OP1 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM1_OP1 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM1_OP2 operation to open valves and turn on a pump.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V102.  
        /// PH2: Open valve V304.  
        /// PH3: Turn on pump P100.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM1_OP2()
        {
            try
            {
                apiClient.SetValveOpening("V102", 100);
                apiClient.SetOnOffItem("V304", true);
                apiClient.SetPumpControl("P100", 100);
                log.Info("EM1_OP2 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM1_OP2 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM1_OP3 operation to close valves, turn off a pump, and turn off a heater.
        /// </summary>
        /// <remarks>
        /// PH1: Close valve V102.  
        /// PH2: Close valve V304.  
        /// PH3: Turn off pump P100.  
        /// PH4: Turn off heater E100.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM1_OP3()
        {
            try
            {
                apiClient.SetValveOpening("V102", 0);
                apiClient.SetOnOffItem("V304", false);
                apiClient.SetPumpControl("P100", 0);
                apiClient.SetOnOffItem("E100", false);
                log.Info("EM1_OP3 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM1_OP3 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM1_OP4 operation to close valves and turn off a pump.
        /// </summary>
        /// <remarks>
        /// PH1: Close valve V102.  
        /// PH2: Close valve V304.  
        /// PH3: Turn off pump P100.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM1_OP4()
        {
            try
            {
                apiClient.SetValveOpening("V102", 0);
                apiClient.SetOnOffItem("V304", false);
                apiClient.SetPumpControl("P100", 0);
                log.Info("EM1_OP4 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM1_OP4 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM2_OP1 operation to open valve V201.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V201.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM2_OP1()
        {
            try
            {
                apiClient.SetOnOffItem("V201", true);
                log.Info("EM2_OP1 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM2_OP1 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM2_OP2 operation to close valve V201.
        /// </summary>
        /// <remarks>
        /// PH1: Close valve V201.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM2_OP2()
        {
            try
            {
                apiClient.SetOnOffItem("V201", false);
                log.Info("EM2_OP2 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM2_OP2 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM3_OP1 operation to close valves V104, V204, and V401.
        /// </summary>
        /// <remarks>
        /// PH1: Close valve V104.  
        /// PH2: Close valve V204.  
        /// PH3: Close valve V401.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM3_OP1()
        {
            try
            {
                apiClient.SetValveOpening("V104", 0);
                apiClient.SetOnOffItem("V204", false);
                apiClient.SetOnOffItem("V401", false);
                log.Info("EM3_OP1 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP1 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM3_OP2 operation to open valves V204 and V301.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V204.  
        /// PH2: Open valve V301.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM3_OP2()
        {
            try
            {
                apiClient.SetOnOffItem("V204", true);
                apiClient.SetOnOffItem("V301", true);
                log.Info("EM3_OP2 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP2 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM3_OP3 operation to open valves V301 and V401.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V301.  
        /// PH2: Open valve V401.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM3_OP3()
        {
            try
            {
                apiClient.SetOnOffItem("V301", true);
                apiClient.SetOnOffItem("V401", true);
                log.Info("EM3_OP3 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP3 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM3_OP4 operation to open valves V104 and V301.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V104.  
        /// PH2: Open valve V301.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM3_OP4()
        {
            try
            {
                apiClient.SetValveOpening("V104", 100);
                apiClient.SetOnOffItem("V301", true);
                log.Info("EM3_OP4 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP4 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM3_OP5 operation to open valves V204 and V302.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V204.  
        /// PH2: Open valve V302.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM3_OP5()
        {
            try
            {
                apiClient.SetOnOffItem("V204", true);
                apiClient.SetOnOffItem("V302", true);
                log.Info("EM3_OP5 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP5 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM3_OP6 operation to close multiple valves: V104, V204, V301, and V401.
        /// </summary>
        /// <remarks>
        /// PH1: Close valve V104.  
        /// PH2: Close valve V204.  
        /// PH3: Close valve V301.  
        /// PH4: Close valve V401.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM3_OP6()
        {
            try
            {
                apiClient.SetValveOpening("V104", 0);
                apiClient.SetOnOffItem("V204", false);
                apiClient.SetOnOffItem("V301", false);
                apiClient.SetOnOffItem("V401", false);
                log.Info("EM3_OP6 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP6 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM3_OP7 operation to close valves V302 and V204.
        /// </summary>
        /// <remarks>
        /// PH1: Close valve V302.  
        /// PH2: Close valve V204.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM3_OP7()
        {
            try
            {
                apiClient.SetOnOffItem("V302", false);
                apiClient.SetOnOffItem("V204", false);
                log.Info("EM3_OP7 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP7 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM3_OP8 operation, which involves opening and then closing valve V204 with a delay.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V204.  
        /// Delay: 1 second.  
        /// PH2: Close valve V204.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM3_OP8()
        {
            try
            {
                apiClient.SetOnOffItem("V204", true);
                Thread.Sleep(1000);
                apiClient.SetOnOffItem("V204", false);
                log.Info("EM3_OP8 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP8 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM4_OP1 operation, which opens valve V404.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V404.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM4_OP1()
        {
            try
            {
                apiClient.SetOnOffItem("V404", true);
                log.Info("EM4_OP1 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM4_OP1 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM4_OP2 operation, which closes valve V404.
        /// </summary>
        /// <remarks>
        /// PH1: Close valve V404.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM4_OP2()
        {
            try
            {
                apiClient.SetOnOffItem("V404", false);
                log.Info("EM4_OP2 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM4_OP2 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM5_OP1 operation to open valve V303 and turn on pump P200.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V303.  
        /// PH2: Turn on pump P200.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM5_OP1()
        {
            try
            {
                apiClient.SetOnOffItem("V303", true);
                apiClient.SetPumpControl("P200", 100);
                log.Info("EM5_OP1 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM5_OP1 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM5_OP2 operation to open valves V103 and V303, and turn on pump P200.
        /// </summary>
        /// <remarks>
        /// PH1: Open valve V103.  
        /// PH2: Open valve V303.  
        /// PH3: Turn on pump P200.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM5_OP2()
        {
            try
            {
                apiClient.SetOnOffItem("V103", true);
                apiClient.SetOnOffItem("V303", true);
                apiClient.SetPumpControl("P200", 100);
                log.Info("EM5_OP2 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM5_OP2 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM5_OP3 operation to close valve V303 and turn off pump P200.
        /// </summary>
        /// <remarks>
        /// PH1: Close valve V303.  
        /// PH2: Turn off pump P200.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM5_OP3()
        {
            try
            {
                apiClient.SetOnOffItem("V303", false);
                apiClient.SetPumpControl("P200", 0);
                log.Info("EM5_OP3 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM5_OP3 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the EM5_OP4 operation, which involves closing valves and turning off a pump.
        /// </summary>
        /// <remarks>
        /// PH1: Close valve V103.  
        /// PH2: Close valve V303.  
        /// PH3: Turn off pump P200.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool EM5_OP4()
        {
            try
            {
                apiClient.SetOnOffItem("V103", false);
                apiClient.SetOnOffItem("V303", false);
                apiClient.SetPumpControl("P200", 0);
                log.Info("EM5_OP4 run successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM5_OP4 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clamps a value between a minimum and maximum range.
        /// </summary>
        /// <param name="value">The value to be clamped.</param>
        /// <param name="min">The minimum value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        /// <returns>The clamped value within the specified range.</returns>
        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Executes the U1_OP1 operation to maintain pressure by throttling valve V104.
        /// </summary>
        /// <remarks>
        /// PH1: Keep PI300 at the target pressure by adjusting the valve opening.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool U1_OP1()
        {
            try
            {
                V104ControlValue -= 0.001 * (TargetPressure - communicator.ProcessData.PI300);
                V104ControlValue = Clamp(V104ControlValue, 0, 100);

                apiClient.SetValveOpening("V104", (int)V104ControlValue);
                bool heaterControl = communicator.ProcessData.TI300 < TargetTemperature;
                apiClient.SetOnOffItem("E100", heaterControl);

                log.Info("U1_OP1 executed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in U1_OP1 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the U1_OP2 operation to maintain temperature using heater E100.
        /// </summary>
        /// <remarks>
        /// PH1: Keep TI300 at the target temperature by controlling heater E100.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool U1_OP2()
        {
            try
            {
                bool heaterControl = communicator.ProcessData.TI300 < TargetTemperature;
                apiClient.SetOnOffItem("E100", heaterControl);

                log.Info("U1_OP2 executed successfully: Heater E100 is " + (heaterControl ? "ON" : "OFF"));
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in U1_OP2 execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the U1_OP3 operation to close valve V104.
        /// </summary>
        /// <remarks>
        /// PH1: Close valve V104.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool U1_OP3()
        {
            try
            {
                apiClient.SetValveOpening("V104", 0);
                log.Info("U1_OP3 executed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in U1_OP3 execution: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Executes the U1_OP4 operation to turn off heater E100.
        /// </summary>
        /// <remarks>
        /// PH1: Turn off heater E100.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the operation executes successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool U1_OP4()
        {
            try
            {
                apiClient.SetOnOffItem("E100", false);
                log.Info("U1_OP4 executed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in U1_OP4 execution: {ex.Message}");
                return false;
            }
        }
    }
}
