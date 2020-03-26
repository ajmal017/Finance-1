using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Logger;
using System.Windows.Forms;
using System.ComponentModel;

namespace Finance.LiveTrading
{
    public class TradingAccountManager : INotifyPropertyChanged
    {
        private static TradingAccountManager _Instance { get; set; }
        public static TradingAccountManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new TradingAccountManager(TradingAccountProvider.Instance);
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

        public event ActiveAccountChanged ActiveAccountChanged;
        protected void OnActiveAccountChanged(LiveAccount account)
        {
            ActiveAccountChanged?.Invoke(this, new AccountUpdateEventArgs(account));
        }

        public event OpenPositionEventHandler PositionChanged;
        protected void OnPositionChanged(LivePosition position)
        {
            PositionChanged?.Invoke(this, new OpenPositionEventArgs(position));
        }

        #endregion

        private TradingAccountProvider TradingProvider { get; } = null;
        public bool Connected => TradingProvider?.Connected ?? false;
        
        #region Accounts

        public LiveAccount ActiveAccount => TradingProvider.ActiveAccount;
        public List<string> ProviderAccountNumbers { get; } = new List<string>();
        public void SelectActiveAccount(string accountId)
        {
            if (!ProviderAccountNumbers.Contains(accountId))
            {
                Log(new LogMessage(ToString(), $"Cannot activate {accountId}", LogMessageType.TradingError));
                return;
            }

            TradingProvider.SetActiveAccount(accountId);
        }

        #endregion

        #region Positions

        public LivePosition ActivePosition { get; protected set; }
        public void SetActivePosition(LivePosition position)
        {
            if (position != null && ActiveAccount.Portfolio.HasOpenPosition(position.Ticker))
            {
                ActivePosition = ActiveAccount.Portfolio.GetPosition<LivePosition>(position.Ticker);

                LiveQuoteForm.Instance.SetActiveSecurity(ActivePosition.Security);
            }
            else
            {
                ActivePosition = null;
            }
        }

        #endregion

        private TradingAccountManager(TradingAccountProvider tradingProvider)
        {
            TradingProvider = tradingProvider ?? throw new ArgumentNullException(nameof(tradingProvider));
            InitializeHandlers();
        }

        private void InitializeHandlers()
        {
            TradingProvider.ActiveAccountChanged += (s, e) =>
            {
                OnActiveAccountChanged(e.Account);
            };

            TradingProvider.TradingAccountIdList += (s, e) =>
            {
                ProviderAccountNumbers.Clear();
                ProviderAccountNumbers.AddRange(e.AccountIds);
                OnPropertyChanged("ProviderAccountNumbers");
            };

            TradingProvider.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Connected")
                    OnPropertyChanged("Connected");
            };
        }
    }
}
