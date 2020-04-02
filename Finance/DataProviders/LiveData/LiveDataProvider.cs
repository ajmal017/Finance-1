using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Windows.Forms;
using Finance.Data;

namespace Finance.Data
{
    public abstract class LiveDataProvider : IProviderStatus
    {
        public static bool NeedsInitialize { get; set; } = false;
        private static DataProviderType CurrentDataProviderType { get; set; }

        private static LiveDataProvider _Instance { get; set; }
        public static LiveDataProvider Instance
        {
            get
            {
                if (_Instance == null || (Settings.Instance.LiveDataProvider != CurrentDataProviderType))
                {
                    switch (Settings.Instance.LiveDataProvider)
                    {
                        case DataProviderType.InteractiveBrokers:
                            CurrentDataProviderType = DataProviderType.InteractiveBrokers;
                            _Instance = new IbkrLiveDataProvider(Settings.Instance.DataProviderPort, 0);
                            NeedsInitialize = true;
                            break;
                        case DataProviderType.IEXCloud:
                            throw new NotImplementedException();
                        default:
                            return null;
                    }
                }

                return _Instance;
            }
        }

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event LiveQuoteEventHandler LiveQuoteReceived;
        protected void OnLiveQuoteReceived(Security security, LiveQuoteType quoteType, DateTime quoteTime, decimal quotePrice, long quoteVolume)
        {
            LiveQuoteReceived?.Invoke(this, new LiveQuoteEventArgs(
                security, quoteType, quoteTime, quotePrice, quoteVolume));
        }

        #endregion

        #region Status Indicator Control

        private ControlStatus _Status { get; set; } = ControlStatus.Offline;
        public ControlStatus Status
        {
            get => _Status;
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    OnPropertyChanged("Status");
                    StatusMessage = Status.Description();
                }
            }
        }

        private string _StatusMessage { get; set; } = "";
        public string StatusMessage
        {
            get
            {
                return _StatusMessage;
            }
            set
            {
                if (_StatusMessage != value)
                {
                    _StatusMessage = value;
                    OnPropertyChanged("StatusMessage");
                }
            }
        }

        private string _StatusMessage2 { get; set; } = "";
        public string StatusMessage2
        {
            get
            {
                return _StatusMessage2;
            }
            set
            {
                if (_StatusMessage2 != value)
                {
                    _StatusMessage2 = value;
                    OnPropertyChanged("StatusMessage");
                }
            }
        }

        #endregion

        public abstract string Name { get; }
        private bool _Connected { get; set; }
        public bool Connected
        {
            get
            {
                return _Connected;
            }
            protected set
            {
                if (_Connected != value)
                {
                    _Connected = value;
                    Status = (_Connected ? ControlStatus.Ready : ControlStatus.Offline);
                    OnPropertyChanged("Connected");                    
                }
            }
        }

        public abstract void Connect();
        public abstract void Disconnect();

        public abstract void RequestSnapshotQuotes(Security security);
        public abstract void RequestStreamingQuotes(Security security);
        public abstract void CancelStreamingQuotes(Security security = null);
    }
}
