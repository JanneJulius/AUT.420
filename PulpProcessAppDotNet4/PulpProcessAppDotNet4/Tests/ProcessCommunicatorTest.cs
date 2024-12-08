using Xunit;
using System;
using System.Threading.Tasks;
using Tuni.MppOpcUaClientLib;
using PulpProcessAppDotNet4.Helpers;
using System.Reflection;

namespace PulpProcessAppDotNet4.Tests
{
    /// <summary>
    /// Contains integration tests for the <see cref="ProcessCommunicator"/> class,
    /// verifying its behavior when communicating with an OPC UA server.
    /// </summary>
    public class ProcessCommunicatorTests
    {
        /// <summary>
        /// Verifies that the <see cref="ProcessCommunicator.Initialize"/> method returns true
        /// and sets <see cref="ProcessCommunicator.IsConnected"/> to true when the OPC UA server is accessible.
        /// </summary>
        [Fact]
        public void Initialize_ShouldReturnTrue_WhenServerIsAccessible()
        {
            // Arrange
            var communicator = new ProcessCommunicator();

            // Act
            bool result = communicator.Initialize();

            // Assert
            Assert.True(result, "Initialize should return true when the local OPC UA server is accessible.");
            Assert.True(communicator.IsConnected, "Communicator should be marked as connected after successful initialization.");
        }

        /// <summary>
        /// Ensures that a new instance of <see cref="ProcessCommunicator"/> initializes
        /// <see cref="ProcessCommunicator.ProcessData"/> with default values.
        /// </summary>
        [Fact]
        public void ProcessData_ShouldBeInitializedWithDefaults()
        {
            // Arrange
            var communicator = new ProcessCommunicator();

            // Act & Assert
            Assert.NotNull(communicator.ProcessData);
            Assert.Equal(0, communicator.ProcessData.LI100);
            Assert.Equal(0, communicator.ProcessData.LI200);
            Assert.Equal(0, communicator.ProcessData.PI300);
            Assert.Equal(0.0, communicator.ProcessData.TI300);
            Assert.Equal(0, communicator.ProcessData.LI400);
            Assert.False(communicator.ProcessData.LSplus300);
            Assert.False(communicator.ProcessData.LSminus300);
        }

        /// <summary>
        /// Checks that <see cref="ProcessCommunicator.DisconnectAsync"/> returns true 
        /// and sets <see cref="ProcessCommunicator.IsConnected"/> to false when the communicator
        /// is currently connected.
        /// </summary>
        [Fact]
        public async Task DisconnectAsync_ShouldReturnTrue_AndSetIsConnectedToFalse_WhenConnected()
        {
            var communicator = new ProcessCommunicator();
            bool initResult = communicator.Initialize();
            Assert.True(initResult, "Initialize should succeed before we can disconnect.");

            bool disconnectResult = await communicator.DisconnectAsync();

            Assert.True(disconnectResult, "DisconnectAsync should return true when disconnected successfully.");
            Assert.False(communicator.IsConnected, "After disconnection, IsConnected should be false.");
        }

        /// <summary>
        /// Verifies that <see cref="ProcessCommunicator.DisconnectAsync"/> returns false
        /// when the communicator is not connected (i.e., <see cref="ProcessCommunicator.Initialize"/> has not been called).
        /// </summary>
        [Fact]
        public async Task DisconnectAsync_ShouldReturnFalse_WhenNotConnected()
        {
            var communicator = new ProcessCommunicator();
            // Not calling Initialize, so communicator is disconnected already.

            bool disconnectResult = await communicator.DisconnectAsync();
            Assert.False(disconnectResult, "DisconnectAsync should return false when already disconnected.");
        }

        /// <summary>
        /// Ensures that <see cref="ProcessCommunicator.ReconnectAsync"/> returns true
        /// and sets <see cref="ProcessCommunicator.IsConnected"/> to true 
        /// after a successful reconnection attempt.
        /// </summary>
        [Fact]
        public async Task ReconnectAsync_ShouldReturnTrue_WhenReconnectionSucceeds()
        {
            var communicator = new ProcessCommunicator();
            bool initResult = communicator.Initialize();
            Assert.True(initResult, "Initial Initialize should succeed.");

            bool disconnectResult = await communicator.DisconnectAsync();
            Assert.True(disconnectResult, "Disconnect should succeed.");

            bool reconnectResult = await communicator.ReconnectAsync();
            Assert.True(reconnectResult, "ReconnectAsync should return true when reconnection succeeds.");
            Assert.True(communicator.IsConnected, "After reconnection, IsConnected should be true.");
        }
    }
}
