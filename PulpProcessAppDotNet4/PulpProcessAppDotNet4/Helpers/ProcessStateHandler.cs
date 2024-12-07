using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulpProcessAppDotNet4.Helpers
{
    /// <summary>
    /// Represents the various states of a process.
    /// </summary>
    public enum ProcessState
    {
        /// <summary>
        /// The process is initialized and ready to start.
        /// </summary>
        Initialized,

        /// <summary>
        /// The process is currently running.
        /// </summary>
        Running,

        /// <summary>
        /// The process is halted and not running.
        /// </summary>
        Halted
    }

    /// <summary>
    /// Handles the state management for a process, allowing state changes and notifications.
    /// </summary>
    public class ProcessStateHandler
    {
        /// <summary>
        /// Occurs when the process state changes.
        /// </summary>
        public event EventHandler<ProcessState> StateChanged;

        /// <summary>
        /// The current state of the process.
        /// </summary>
        private ProcessState _currentState;

        /// <summary>
        /// Gets or sets the current state of the process.
        /// </summary>
        /// <remarks>
        /// When the state changes, the <see cref="StateChanged"/> event is triggered.
        /// </remarks>
        public ProcessState CurrentState
        {
            get => _currentState;
            set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    StateChanged?.Invoke(this, _currentState);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessStateHandler"/> class.
        /// </summary>
        /// <remarks>
        /// Sets the initial process state to <see cref="ProcessState.Initialized"/>.
        /// </remarks>
        public ProcessStateHandler()
        {
            _currentState = ProcessState.Initialized; // Default state
        }
    }
}
