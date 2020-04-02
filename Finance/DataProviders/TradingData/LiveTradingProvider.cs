using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Windows.Forms;
using Finance.Data;
using static Finance.Logger;

namespace Finance.LiveTrading
{
    public abstract class LiveTradingProvider : IProviderStatus
    {
        private static TradingProviderType CurrentProvider = TradingProviderType.NotSet;
        private static LiveTradingProvider _Instance { get; set; }
        public static LiveTradingProvider Instance
        {
            get
            {
                if (_Instance == null || CurrentProvider != Settings.Instance.TradingProvider)
                {
                    CurrentProvider = Settings.Instance.TradingProvider;
                    switch (CurrentProvider)
                    {
                        case TradingProviderType.NotSet:
                            throw new UnknownErrorException() { message = "No trading provider selected" };
                        case TradingProviderType.InteractiveBrokers:
                            _Instance = new IbkrLiveTradingProvider(Settings.Instance.IbkrTradingProviderPort, 2);
                            break;
                        default:
                            break;
                    }
                }
                return _Instance;
            }
        }

        #region Events

        public event TradingAccountIdListEventHandler TradingAccountIdList;
        protected void OnTradingAccountList(List<string> accountIds)
        {
            TradingAccountIdList?.Invoke(this, new TradingAccountListEventArgs(accountIds));
        }

        public event ActiveAccountChanged ActiveAccountChanged;
        protected void OnActiveAccountChanged()
        {
            ActiveAccountChanged?.Invoke(this, new AccountUpdateEventArgs(ActiveAccount));
            RequestAccountUpdate(ActiveAccount);
        }

        public event AccountUpdateEventHandler AccountInformationUpdate;
        protected void OnAccountUpdate(LiveAccount account)
        {
            AccountInformationUpdate?.Invoke(this, new AccountUpdateEventArgs(account));
        }

        public event TradeStatusUpdateEventHandler TradeStatusUpdate;
        protected void OnTradeStatusUpdate(LiveTrade trade)
        {
            TradeStatusUpdate?.Invoke(this, new TradeStatusUpdateEventArgs(trade));
        }

        public event OpenPositionEventHandler OpenPositionsUpdate;
        protected void OnOpenPositionUpdate(LivePosition position)
        {
            OpenPositionsUpdate?.Invoke(this, new OpenPositionEventArgs(position));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private bool _Connected { get; set; } = false;
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

        public abstract string Name { get; }

        public List<LiveAccount> AvailableAccounts { get; } = new List<LiveAccount>();

        private LiveAccount _ActiveAccount { get; set; }
        public LiveAccount ActiveAccount
        {
            get
            {
                return _ActiveAccount;
            }
            protected set
            {
                if (_ActiveAccount == value)
                    return;

                _ActiveAccount = value;
                OnActiveAccountChanged();
            }
        }

        protected LiveTradingProvider()
        {
        }

        public abstract void Connect();
        public abstract void Disconnect();

        public abstract void RequestAccountUpdate(LiveAccount account);
        public abstract void RequestAllPositions();

        public abstract void SubmitTrades(LiveTrade trade, LiveTrade stopTrade);
        
        public void SetActiveAccount(string accountId)
        {
            var acct = AvailableAccounts.Find(x => x.AccountId == accountId);
            if (acct == null)
            {
                Log(new LogMessage(ToString(), $"Could not load account {accountId}", LogMessageType.TradingError));
                return;
            }
            ActiveAccount = acct;
        }

    }
}
