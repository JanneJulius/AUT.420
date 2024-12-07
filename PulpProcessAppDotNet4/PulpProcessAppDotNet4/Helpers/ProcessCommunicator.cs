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
    /// Manages the connection and communication with the external process API via OPC UA.
    /// </summary>
    public class ProcessCommunicator
    {
        /// <summary>
        /// The endpoint URL of the OPC UA server.
        /// </summary>
        private const string API_ENDPOINT = "opc.tcp://127.0.0.1:8087";

        /// <summary>
        /// Represents the connected state.
        /// </summary>
        private const bool IS_CONNECTED = true;

        /// <summary>
        /// Represents the disconnected state.
        /// </summary>
        private const bool IS_DISCONNECTED = false;

        /// <summary>
        /// Stores the last known connection status to avoid duplicate logs.
        /// </summary>
        private string lastConnectionStatus = null;

        /// <summary>
        /// The OPC UA client instance for communicating with the process API.
        /// </summary>
        public MppClient apiClient;

        /// <summary>
        /// Configuration parameters for connecting to the OPC UA server.
        /// </summary>
        private readonly ConnectionParamsHolder connectionConfig;

        /// <summary>
        /// Indicates whether the client is connected to the OPC UA server.
        /// </summary>
        public bool IsConnected { get; private set; } = IS_DISCONNECTED;

        /// <summary>
        /// Represents the process data model for UI bindings and logic.
        /// </summary>
        public ProcessData ProcessData { get; private set; } = new ProcessData();

        /// <summary>
        /// Dictionary mapping process item keys to their update handlers.
        /// </summary>
        private readonly Dictionary<string, Action<MppValue>> processItemHandlers;

        /// <summary>
        /// Logger instance for logging messages and errors.
        /// </summary>
        private static Logger log = App.logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessCommunicator"/> class.
        /// </summary>
        public ProcessCommunicator()
        {
            connectionConfig = new ConnectionParamsHolder(API_ENDPOINT);

            // Define mappings between item keys and update actions
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

        /// <summary>
        /// Initializes the OPC UA client and sets up event handlers and subscriptions.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the initialization is successful; otherwise, <c>false</c>.
        /// </returns>
        public bool Initialize()
        {
            try
            {
                apiClient = new MppClient(connectionConfig);

                // Attach event handlers
                apiClient.ConnectionStatus += new MppClient.ConnectionStatusEventHandler(OnConnectionStatusChanged);
                apiClient.ProcessItemsChanged += new MppClient.ProcessItemsChangedEventHandler(OnProcessItemsChanged);
                apiClient.Init();

                AddSubscriptions();

                IsConnected = IS_CONNECTED;
                log.Info("Connection established successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex, "Connection attempt failed.");
                return false;
            }
        }

        /// <summary>
        /// Adds subscriptions to the OPC UA client for process items.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if subscriptions are added successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool AddSubscriptions()
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

        /// <summary>
        /// Handles changes in connection status.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments containing connection status details.</param>
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

        /// <summary>
        /// Handles updates to process items from the OPC UA server.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments containing the changed process items.</param>
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
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error processing item changes.");
            }
        }

        /// <summary>
        /// Disconnects the OPC UA client if it is currently connected.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the disconnection is successful; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> DisconnectAsync()
        {
            if (IsConnected && apiClient != null)
            {
                await Task.Run(() =>
                {
                    apiClient.Dispose();
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

        /// <summary>
        /// Reconnects to the OPC UA server by reinitializing the client connection.
        /// </summary>
        /// <returns>
        /// Returns a task representing the asynchronous operation, with a <c>true</c> result if successful.
        /// </returns>
        public Task<bool> ReconnectAsync()
        {
            return Task.Run(() => Initialize());
        }
    }
}
