using System.ComponentModel;
using PulpProcessAppDotNet4.Helpers;

namespace PulpProcessAppDotNet4.Helpers
{
    /// <summary>
    /// Represents process data and provides change notification for UI bindings.
    /// </summary>
    public class ProcessData : INotifyPropertyChanged
    {
        /// <summary>
        /// The level indicator value for Tank 100.
        /// </summary>
        private int _LI100;

        /// <summary>
        /// Gets or sets the level indicator value for Tank 100.
        /// </summary>
        /// <remarks>
        /// Changes to this property trigger the <see cref="PropertyChanged"/> event.
        /// </remarks>
        public int LI100
        {
            get => _LI100;
            set
            {
                if (_LI100 != value)
                {
                    _LI100 = value;
                    OnPropertyChanged(nameof(LI100));
                }
            }
        }

        /// <summary>
        /// The level indicator value for Tank 200.
        /// </summary>
        private int _LI200;

        /// <summary>
        /// Gets or sets the level indicator value for Tank 200.
        /// </summary>
        /// <remarks>
        /// Changes to this property trigger the <see cref="PropertyChanged"/> event.
        /// </remarks>
        public int LI200
        {
            get => _LI200;
            set
            {
                if (_LI200 != value)
                {
                    _LI200 = value;
                    OnPropertyChanged(nameof(LI200));
                }
            }
        }

        /// <summary>
        /// The pressure indicator value for Tank 300.
        /// </summary>
        private int _PI300;

        /// <summary>
        /// Gets or sets the pressure indicator value for Tank 300.
        /// </summary>
        /// <remarks>
        /// Changes to this property trigger the <see cref="PropertyChanged"/> event.
        /// </remarks>
        public int PI300
        {
            get => _PI300;
            set
            {
                if (_PI300 != value)
                {
                    _PI300 = value;
                    OnPropertyChanged(nameof(PI300));
                }
            }
        }

        /// <summary>
        /// The temperature indicator value for Tank 300.
        /// </summary>
        private double _TI300;

        /// <summary>
        /// Gets or sets the temperature indicator value for Tank 300.
        /// </summary>
        /// <remarks>
        /// Changes to this property trigger the <see cref="PropertyChanged"/> event.
        /// </remarks>
        public double TI300
        {
            get => _TI300;
            set
            {
                if (_TI300 != value)
                {
                    _TI300 = value;
                    OnPropertyChanged(nameof(TI300));
                }
            }
        }

        /// <summary>
        /// The level indicator value for Tank 400.
        /// </summary>
        private int _LI400;

        /// <summary>
        /// Gets or sets the level indicator value for Tank 400.
        /// </summary>
        /// <remarks>
        /// Changes to this property trigger the <see cref="PropertyChanged"/> event.
        /// </remarks>
        public int LI400
        {
            get => _LI400;
            set
            {
                if (_LI400 != value)
                {
                    _LI400 = value;
                    OnPropertyChanged(nameof(LI400));
                }
            }
        }

        /// <summary>
        /// The limit switch state for Tank 300 (positive).
        /// </summary>
        private bool _LSplus300;

        /// <summary>
        /// Gets or sets the limit switch state for Tank 300 (positive).
        /// </summary>
        /// <remarks>
        /// Changes to this property trigger the <see cref="PropertyChanged"/> event.
        /// </remarks>
        public bool LSplus300
        {
            get => _LSplus300;
            set
            {
                if (_LSplus300 != value)
                {
                    _LSplus300 = value;
                    OnPropertyChanged(nameof(LSplus300));
                }
            }
        }

        /// <summary>
        /// The limit switch state for Tank 300 (negative).
        /// </summary>
        private bool _LSminus300;

        /// <summary>
        /// Gets or sets the limit switch state for Tank 300 (negative).
        /// </summary>
        /// <remarks>
        /// Changes to this property trigger the <see cref="PropertyChanged"/> event.
        /// </remarks>
        public bool LSminus300
        {
            get => _LSminus300;
            set
            {
                if (_LSminus300 != value)
                {
                    _LSminus300 = value;
                    OnPropertyChanged(nameof(LSminus300));
                }
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

