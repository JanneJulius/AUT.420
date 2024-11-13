using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tuni.MppOpcUaClientLib;
using System.Windows;

namespace PulpProcessAppDotNet4.Helpers
{
    /// <summary>
    /// Manages the connection with the external process API.
    /// </summary>
    class ProcessCommunicator
    {
        private const string API_ENDPOINT = "opc.tcp://127.0.0.1:8087";
        private const bool IS_CONNECTED = true;
        private const bool IS_DISCONNECTED = false;

        private MppClient apiClient;
        private ConnectionParamsHolder connectionConfig;

        public bool IsConnected { get; private set; } = IS_DISCONNECTED;
        public ProcessData ProcessData { get; private set; } = new ProcessData();

        private readonly Dictionary<string, Action<MppValue>> processItemHandlers;

        private static Logger log = App.logger;

        public  ProcessCommunicator()
        {
            connectionConfig = new ConnectionParamsHolder(API_ENDPOINT);

            // Define mappings between item keys and actions
            processItemHandlers = new Dictionary<string, Action<MppValue>>
            {
                { "LI100", value => ProcessData.LI100 = (int)value.GetValue() },
                { "LI200", value => ProcessData.LI200 = (int)value.GetValue() },
                { "PI300", value => ProcessData.PI300 = (int)value.GetValue() },
                { "TI300", value => ProcessData.TI300 = (double)value.GetValue() },
                { "LI400", value => ProcessData.LI400 = (int)value.GetValue() },
                { "LS+300", value => ProcessData.LSplus300 = (bool)value.GetValue() },
                { "LS-300", value => ProcessData.LSminus300 = (bool)value.GetValue() }
            };

        }

        public async Task InitializeAsync()
        {
            try
            {
                apiClient = new MppClient(connectionConfig);
                // Attach event handlers
                apiClient.ConnectionStatus += OnConnectionStatusChanged;
                apiClient.ProcessItemsChanged += OnProcessItemsChanged;

                // Initialize the API client asynchronously
                await Task.Run(() => apiClient.Init());  // Run this on a background thread

                await AddSubscriptionsAsync();  // Ensure subscriptions are added asynchronously

                IsConnected = IS_CONNECTED;
                log.Info("Connection established successfully.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Connection failed.");
            }
        }

        private async Task AddSubscriptionsAsync()
        {
            try
            {
                // Add subscriptions asynchronously
                await Task.Run(() =>
                {
                    foreach (var itemKey in processItemHandlers.Keys)
                    {
                        apiClient.AddToSubscription(itemKey);
                    }
                });

                log.Info("Subscriptions added successfully.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to add subscriptions.");
            }
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            try
            {
                IsConnected = e.StatusInfo.FullStatusString == "Connected";
                log.Info($"Connection status updated: {(IsConnected ? "Connected" : "Disconnected")}");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error in status update.");
            }
        }



        private void OnProcessItemsChanged(object sender, ProcessItemChangedEventArgs e)
        {
            try
            {
                foreach (var item in e.ChangedItems)
                {
                    if (processItemHandlers.TryGetValue(item.Key, out var handler))
                    {
                        handler(item.Value);
                    }
                    else
                    {
                        log.Warn($"Unhandled process item: {item.Key}");
                    }
                }
                log.Info("Process items updated successfully.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error processing item changes.");
            }
        }




    }
}
