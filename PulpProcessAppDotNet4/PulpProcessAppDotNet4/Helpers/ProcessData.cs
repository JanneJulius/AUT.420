using System.ComponentModel;
using PulpProcessAppDotNet4.Helpers;

namespace PulpProcessAppDotNet4.Helpers
{
    /// <summary>
    /// Creates event handlers for the UI interface to use.
    /// </summary>
    public class ProcessData : INotifyPropertyChanged
    {
        private int _LI100;
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

        private int _LI200;
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

        private int _PI300;
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
        private double _TI300;
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
        private int _LI400;
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
        private bool _LSplus300;
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

        private bool _LSminus300;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

