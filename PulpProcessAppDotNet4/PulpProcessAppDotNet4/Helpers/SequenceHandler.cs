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
    /// Class to handling the sequences.
    /// </summary>
    class SequenceHandler
    {

        // Main sequence names
        private const string IMPREGNATION = "Impregnation";
        private const string BLACK_LIQUOR_FILL = "Black liquor fill";
        private const string WHITE_LIQUOR_FILL = "White liquor fill";
        private const string COOKING = "Cooking";
        private const string DISCHARGE = "Discharge";


        public string activeSequence;
        public bool isWholeSequenceComplete = false;
        public bool hasSequenceError = false;
        public double DurationCooking { get; set; }
        public double TargetTemperature { get; set; }
        public double TargetPressure { get; set; }
        public double ImpregnationTime { get; set; }


        private double V104ControlValue = 100;

        private static Logger log = App.logger;
        private MppClient apiClient;
        private ProcessCommunicator communicator;

        private Thread sequencedrivethread;



        public SequenceHandler(double durationCooking, double targetTemperature, double targetPressure, double impregnationTime, ProcessCommunicator initCommunicator)
        {
            log.Info("Sequence Driver started");

            DurationCooking = durationCooking;
            TargetTemperature = targetTemperature;
            TargetPressure = targetPressure;
            ImpregnationTime = impregnationTime;

            apiClient = initCommunicator.apiClient;
            communicator = initCommunicator;

            // Thread that handles sequence logic
            sequencedrivethread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
            });
            sequencedrivethread.Start();
            
        }


        /// <summary>
        /// Runs the complete sequence : Impregnation, Black Liquor Fill, White Liquor Fill, Cooking, and Discharge.
        /// </summary>
        public bool RunWholeSequence()
        {
            try
            {
                log.Info("Starting whole sequence...");

                if (apiClient == null)
                {
                    log.Error("Client not found.");
                    return false;
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

                log.Info("Whole sequence runned succesfully!");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Unknown error during whole sequence: {ex.Message}");
                return false;
            }
        }



        /// <summary>
        /// Runs the Impregnation sequence.
        /// </summary>
        private bool RunImpregnation()
        {
            try
            {
                log.Info("Starting sequence: Impregnation");
                activeSequence = IMPREGNATION;

                // Initial operations: Open impregnation outlet and prepare the system
                if (!EM2_OP1()) throw new Exception("Failed to execute EM2_OP1 (Open impregnation outlet).");
                if (!EM5_OP1()) throw new Exception("Failed to execute EM5_OP1 (Open route to digester/T300 and pump P200).");
                if (!EM3_OP2()) throw new Exception("Failed to execute EM3_OP2 (Open inlet and outlet to impregnation tank T200).");

                // Wait until LS+300 is activated
                WaitForCondition(() => communicator.ProcessData.LSplus300, TimeSpan.FromSeconds(30), "LS+300 was not activated.");

                // Close outlets after condition is met
                if (!EM3_OP1()) throw new Exception("Failed to execute EM3_OP1 (Close outlets).");

                // Wait for the impregnation time to pass
                log.Info($"Waiting for impregnation duration: {ImpregnationTime} seconds.");
                Thread.Sleep(TimeSpan.FromSeconds(ImpregnationTime));

                // Final operations: Close outlet, stop pump, and depressurize digester
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
        private bool RunBlackLiquorFill()
        {
            try
            {
                log.Info("Starting sequence: Black Liquor Fill");
                activeSequence = BLACK_LIQUOR_FILL;

                // Perform initial operations
                if (!EM3_OP2()) throw new Exception("Failed to execute EM3_OP2 (Open inlet/outlet to Tank T200).");
                if (!EM5_OP1()) throw new Exception("Failed to execute EM5_OP1 (Open route to digester/T300 and pump P200).");
                if (!EM4_OP1()) throw new Exception("Failed to execute EM4_OP1 (Open outlet for black liquor fill).");

                // Wait until LI400 indicates enough liquor has been displaced
                WaitForCondition(() => communicator.ProcessData.LI400 > 27, TimeSpan.FromSeconds(20), "LI400 did not reach the required level.");

                // Perform final operations
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
        private bool RunWhiteLiquorFill()
        {
            try
            {
                log.Info("Starting sequence: White Liquor Fill");
                activeSequence = WHITE_LIQUOR_FILL;

                // Perform initial operations
                if (!EM3_OP3()) throw new Exception("Failed to execute EM3_OP3 (Open inlet/outlet to Black Liquor Tank T400).");
                if (!EM1_OP2()) throw new Exception("Failed to execute EM1_OP2 (Open route to digester/T300 and pump P100).");

                // Wait until LI400 indicates that enough liquor has been displaced
                WaitForCondition(() => communicator.ProcessData.LI400 > 27, TimeSpan.FromSeconds(20), "LI400 did not reach the required level.");

                // Perform final operations
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
        private bool RunCooking()
        {
            try
            {
                log.Info("Starting sequence: Cooking");
                activeSequence = COOKING;

                // Initial setup
                if (!EM3_OP4()) throw new Exception("Failed to execute EM3_OP4 (Open inlet/outlet to White Liquor Tank T100).");
                if (!EM1_OP1()) throw new Exception("Failed to execute EM1_OP1 (Open route to digester/T300, pump P100, and heat).");

                // Wait until TI300 reaches the cooking temperature
                WaitForCondition(() => communicator.ProcessData.TI300 >= TargetTemperature, TimeSpan.FromSeconds(300), "TI300 did not reach the target cooking temperature.");

                // Close outlets and prepare for regulated cooking
                if (!EM3_OP1()) throw new Exception("Failed to execute EM3_OP1 (Close outlets).");
                if (!EM1_OP2()) throw new Exception("Failed to execute EM1_OP2 (Open route to digester/T300 and pump P100).");

                // Regulate digester pressure and temperature
                if (!U1_OP1()) throw new Exception("Failed to execute U1_OP1 (Regulate digester/T300 pressure).");
                if (!U1_OP2()) throw new Exception("Failed to execute U1_OP2 (Regulate digester/T300 temperature).");

                // Wait for the cooking time to pass
                log.Info($"Waiting for cooking duration: {DurationCooking} seconds.");
                Thread.Sleep(TimeSpan.FromSeconds(DurationCooking));

                // Stop pressure and temperature regulation
                if (!U1_OP3()) throw new Exception("Failed to execute U1_OP3 (Stop digester/T300 pressure regulation).");
                if (!U1_OP4()) throw new Exception("Failed to execute U1_OP4 (Stop digester/T300 temperature regulation).");

                // Finalize the sequence by closing all inlets/outlets and depressurizing
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
        private bool RunDischarge()
        {
            try
            {
                log.Info("Starting sequence: Discharge");
                activeSequence = DISCHARGE;

                // Initial operations: Open route to White Liquor Tank and start pump
                if (!EM5_OP2()) throw new Exception("Failed to execute EM5_OP2 (Open route to White Liquor Tank T100 and start pump P200).");
                if (!EM3_OP5()) throw new Exception("Failed to execute EM3_OP5 (Allow digester/T300 discharge).");

                // Wait until LS-300 is deactivated
                WaitForCondition(() => !communicator.ProcessData.LSminus300, TimeSpan.FromSeconds(100), "LS-300 was not deactivated.");

                // Final operations: Close routes and outlets
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




        /// Waits for a specific condition to become true within a timeout period.
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

        // Run EM1_OP1
        /*
         * PH1: Open valve V102
         * PH2: Open valve V304
         * PH3: Pump P100 on
         * PH4: Heater E100 on
         * */
        private bool EM1_OP1()
        {
            try
            {
                apiClient.SetValveOpening("V102", 100);
                apiClient.SetOnOffItem("V304", true);
                apiClient.SetPumpControl("P100", 100);
                apiClient.SetOnOffItem("E100", true);
                log.Info("EM1_OP1 runned succesfully");

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM1_OP1 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM1_OP2
        /*
         * PH1: Open valve V102
         * PH2: Open valve V304
         * PH3: Pump P100 on
         * */
        private bool EM1_OP2()
        {
            try
            {
                apiClient.SetValveOpening("V102", 100);
                apiClient.SetOnOffItem("V304", true);
                apiClient.SetPumpControl("P100", 100);
                log.Info("EM1_OP2 runned succesfully");

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM1_OP2 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM1_OP3
        /*
         * PH1: Close valve V102
         * PH2: Close valve V304
         * PH3: Pump P100 off
         * PH4: Heater E100 off
         * */
        private bool EM1_OP3()
        {
            try
            {
                apiClient.SetValveOpening("V102", 0);
                apiClient.SetOnOffItem("V304", false);
                apiClient.SetPumpControl("P100", 0);
                apiClient.SetOnOffItem("E100", false);
                log.Info("EM1_OP3 runned succesfully");

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM1_OP3 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM1_OP4
        /*
         * PH1: Close valve V102
         * PH2: Close valve V304
         * PH3: Pump P100 off
         * */
        private bool EM1_OP4()
        {
            try
            {
                apiClient.SetValveOpening("V102", 0);
                apiClient.SetOnOffItem("V304", false);
                apiClient.SetPumpControl("P100", 0);
                log.Info("EM1_OP4 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM1_OP4 execution: {ex.Message}");
                return false;
            }
        }


        // Run EM2_OP1
        /*
         * PH1: Open valve V201
         * */
        private bool EM2_OP1()
        {
            try
            {
                apiClient.SetOnOffItem("V201", true);
                log.Info("EM2_OP1 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM2_OP1 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM2_OP1
        /*
         * PH1: Close valve V201
         * */
        private bool EM2_OP2()
        {
            try
            {
                apiClient.SetOnOffItem("V201", false);
                log.Info("EM2_OP2 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM2_OP2 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM3_OP1
        /*
         * PH1: Close valve V104
         * PH2: Close valve V204
         * PH3: Close valve V401
         * */
        private bool EM3_OP1()
        {
            try
            {
                apiClient.SetValveOpening("V104", 0);
                apiClient.SetOnOffItem("V204", false);
                apiClient.SetOnOffItem("V401", false);
                log.Info("EM3_OP1 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP1 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM3_OP2
        /*
         * PH1: Open valve V104
         * PH2: Open valve V301
         * */
        private bool EM3_OP2()
        {
            try
            {
                apiClient.SetOnOffItem("V204", true);
                apiClient.SetOnOffItem("V301", true);
                log.Info("EM3_OP2 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP2 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM3_OP3
        /*
         * PH1: Open valve V301
         * PH2: Open valve V401
         * */
        private bool EM3_OP3()
        {
            try
            {
                apiClient.SetOnOffItem("V301", true);
                apiClient.SetOnOffItem("V401", true);
                log.Info("EM3_OP3 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP3 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM3_OP4
        /*
         * PH1: Open valve V104
         * PH2: Open valve V301
         * */
        private bool EM3_OP4()
        {
            try
            {
                apiClient.SetValveOpening("V104", 100);
                apiClient.SetOnOffItem("V301", true);
                log.Info("EM3_OP4 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP4 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM3_OP5
        /*
         * PH1: Open valve V204
         * PH2: Open valve V302
         * */
        private bool EM3_OP5()
        {
            try
            {
                apiClient.SetOnOffItem("V204", true);
                apiClient.SetOnOffItem("V302", true);
                log.Info("EM3_OP5 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP5 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM3_OP6
        /*
         * PH1: Close valve V104
         * PH2: Close valve V204
         * PH3: Close valve V301
         * PH4: Close valve V401
         * */
        private bool EM3_OP6()
        {
            try
            {
                apiClient.SetValveOpening("V104", 0);
                apiClient.SetOnOffItem("V204", false);
                apiClient.SetOnOffItem("V301", false);
                apiClient.SetOnOffItem("V401", false);
                log.Info("EM3_OP6 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP6 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM3_OP7
        /*
         * PH1: Close valve V302
         * PH2: Close valve V204
         * */
        private bool EM3_OP7()
        {
            try
            {
                apiClient.SetOnOffItem("V302", false);
                apiClient.SetOnOffItem("V204", false);
                log.Info("EM3_OP7 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP7 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM3_OP8
        /*
         * PH1: Open valve V204
         * Delay: 1 second 
         * PH2: Close valve V204
         * */
        private bool EM3_OP8()
        {
            try
            {
                apiClient.SetOnOffItem("V204", true);
                Thread.Sleep(1000);
                apiClient.SetOnOffItem("V204", false);
                log.Info("EM3_OP8 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM3_OP8 execution: {ex.Message}");
                return false;
            }
        }


        // Run EM4_OP1
        /*
         * PH1: Open valve V404
         * */
        private bool EM4_OP1()
        {
            try
            {
                apiClient.SetOnOffItem("V404", true);
                log.Info("EM4_OP1 runned succesfully");


                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM4_OP1 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM4_OP2
        /*
         * PH1: Close valve V404
         * */
        private bool EM4_OP2()
        {
            try
            {
                apiClient.SetOnOffItem("V404", false);
                log.Info("EM4_OP2 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM4_OP2 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM5_OP1
        /*
         * PH1: Open valve V303
         * PH2: Pump P200 on
         * */
        private bool EM5_OP1()
        {
            try
            {
                apiClient.SetOnOffItem("V303", true);
                apiClient.SetPumpControl("P200", 100);
                log.Info("EM5_OP1 runned succesfully");


                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM5_OP1 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM5_OP2
        /*
         * PH1: Open valve V103
         * PH2: Open valve V303
         * PH3: Pump P200 on
         * */
        private bool EM5_OP2()
        {
            try
            {
                apiClient.SetOnOffItem("V103", true);
                apiClient.SetOnOffItem("V303", true);
                apiClient.SetPumpControl("P200", 100);
                log.Info("EM5_OP2 runned succesfully");

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM5_OP2 execution: {ex.Message}");
                return false;
            }
        }

        // Run EM5_OP3
        /*
         * PH1: Close valve V303
         * PH2: Pump P200 off
         * */
        private bool EM5_OP3()
        {
            try
            {
                apiClient.SetOnOffItem("V303", false);
                apiClient.SetPumpControl("P200", 0);
                log.Info("EM5_OP3 runned succesfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM5_OP3 execution: {ex.Message}");
                return false;
            }
        }


        // Run EM5_OP4
        /*
         * PH1: Close valve V103
         * PH2: Close valve V303
         * PH3: Pump P200 off
         * */
        private bool EM5_OP4()
        {
            try
            {
                apiClient.SetOnOffItem("V103", false);
                apiClient.SetOnOffItem("V303", false);
                apiClient.SetPumpControl("P200", 0);
                log.Info("EM5_OP4 runned succesfully");


                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in EM5_OP4 execution: {ex.Message}");
                return false;
            }
        }

        // Math.Clamp is not yet available in .NET 4
        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // Run U1_OP1
        /*
         * PH1: Keep PI300 at Pc by throttling valve V104
         * */
        private bool U1_OP1()
        {
            try
            {
                // Calculate the new control value for V104
                V104ControlValue -= 0.001 * (TargetPressure - communicator.ProcessData.PI300);
                V104ControlValue = Clamp(V104ControlValue, 0, 100);

                // Update the valve opening and heater state
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

        // Run U1_OP2
        /*
         * PH1: Keep TI 300 at T by using heater E100
         * */
        private bool U1_OP2()
        {
            try
            {
                // Check if the current temperature (TI300) is below the target temperature
                bool heaterControl = communicator.ProcessData.TI300 < TargetTemperature;

                // Turn heater E100 on or off based on the temperature check
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

        // Run U1_OP3
        /*
         * PH1: Close valve V104
         * */
        private bool U1_OP3()
        {
            try
            {
                apiClient.SetValveOpening("V104", 0);
                log.Info("U1_OP3 executed successfully");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error in U1_OP3 execution: {ex.Message}");
                return false;
            }
        }


        // Run U1_OP4
        /*
         * PH1: Turn Heater E100 off
         * */
        private bool U1_OP4()
        {
            try
            {               
                apiClient.SetOnOffItem("E100", false);
                log.Info("U1_OP4 executed successfully");
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
