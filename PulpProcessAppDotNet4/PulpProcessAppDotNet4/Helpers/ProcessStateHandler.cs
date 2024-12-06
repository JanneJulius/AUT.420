using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulpProcessAppDotNet4.Helpers
{
public enum ProcessState
    {
        Initialized,
        Running,
        Halted
    }

    public class ProcessStateHandler
    {
        public event EventHandler<ProcessState> StateChanged;

        private ProcessState _currentState;

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

        public ProcessStateHandler()
        {
            _currentState = ProcessState.Initialized; // Default state
        }
    }
}
