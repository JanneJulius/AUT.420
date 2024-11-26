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
    /// Represents a collection of key process metrics.
    /// </summary>
    public class ProcessData
    {
        public int LI100 { get; set; }
        public int LI200 { get; set; }
        public int PI300 { get; set; }
        public double TI300 { get; set; }
        public int LI400 { get; set; }
        public bool LSplus300 { get; set; }
        public bool LSminus300 { get; set; }
    }


    /// <summary>
    /// Manages the connection with the external process API.
    /// </summary>
    class ProcessCommunicator
    {
        private const string API_ENDPOINT = "opc.tcp://127.0.0.1:8087";
        private const bool IS_CONNECTED = true;
        private const bool IS_DISCONNECTED = false;
        private string lastConnectionStatus = null;

        public MppClient apiClient;
        private ConnectionParamsHolder connectionConfig;

        public bool IsConnected { get; private set; } = IS_DISCONNECTED;
        public ProcessData ProcessData { get; private set; } = new ProcessData();
        private readonly Dictionary<string, Action<MppValue>> processItemHandlers;

        private static Logger log = App.logger;

        public ProcessCommunicator()
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

        public bool Initialize()
        {

            try
            {
                apiClient = new MppClient(connectionConfig);


                // Attach event handlers
                apiClient.ConnectionStatus += new MppClient.ConnectionStatusEventHandler(OnConnectionStatusChanged);
                apiClient.ProcessItemsChanged += new MppClient.ProcessItemsChangedEventHandler(OnProcessItemsChanged);
                apiClient.Init();
                //Task.Run(() => apiClient.Init()).Wait();
                AddSubscriptions();

                IsConnected = IS_CONNECTED;
                log.Info("Connection established successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Connection attempt  failed.");   
                return false;
            }
            
        }

        private  bool AddSubscriptions()
        {
            try
            {
       
                foreach (var itemKey in processItemHandlers.Keys)
                {
                    apiClient.AddToSubscription(itemKey);
                }

                log.Info("Subscriptions added successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to add subscriptions.");
                return false;
            }
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            try
            {
              
                string currentStatus = e.StatusInfo.FullStatusString;

                // Avoid duplicate status logs
                if (currentStatus == lastConnectionStatus)
                    return;

                lastConnectionStatus = currentStatus;
                IsConnected = currentStatus == "Connected";

                log.Info($"Connection status updated: {currentStatus}");
                
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

        // Disconnects the client if connected
        public async Task<bool> DisconnectAsync()
        {
            if (IsConnected && apiClient != null)
            {
                await Task.Run(() =>
                {
                    apiClient.Dispose(); // Run Dispose on a background thread
                    apiClient = null;
                });

                IsConnected = IS_DISCONNECTED;
                log.Info("Disconnected successfully.");
                return true;
            }
            else
            {
                log.Warn("Attempted to disconnect when already disconnected.");
                return false;
            }
        }

        // Reconnects by reinitializing the client connection
        public Task<bool> ReconnectAsync()
        {
            return Task.Run(() => Initialize());
        }
    }
}