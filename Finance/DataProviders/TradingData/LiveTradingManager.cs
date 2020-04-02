using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance.Data;
using static Finance.Logger;
using System.Windows.Forms;
using System.ComponentModel;

namespace Finance.LiveTrading
{
    public class LiveTradingManager : INotifyPropertyChanged
    {
        private static LiveTradingManager _Instance { get; set; }
        public static LiveTradingManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new LiveTradingManager(LiveTradingProvider.Instance);
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

        public event LiveTradeEventHandler TradeStatusUpdate;
        protected void OnTradeStatusUpdate(LiveTrade trade)
        {
            TradeStatusUpdate?.Invoke(this, new LiveTradeEventArgs(trade));
        }

        #endregion

        private LiveTradingProvider TradingProvider { get; } = null;
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

            StopStreamingDataForPositions();

            // Set the active account
            TradingProvider.SetActiveAccount(accountId);

            StartStreamingDataForPositions();
        }
        private void StartStreamingDataForPositions()
        {
            if (ActiveAccount == null)
                return;

            foreach (var livePosition in ActiveAccount.Portfolio.Positions)
            {
                LiveDataProvider.Instance.RequestSnapshotQuotes(livePosition.Security);
            }
        }
        private void StopStreamingDataForPositions()
        {
            if (ActiveAccount == null)
                return;

            foreach (var livePosition in ActiveAccount.Portfolio.Positions)
            {
                LiveDataProvider.Instance.CancelStreamingQuotes(livePosition.Security);
            }
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
                LiveTradeEntryForm.Instance.SetActiveSecurity(ActivePosition.Security);
                LiveTradeEntryForm.Instance.SetActiveAccount(ActiveAccount);

                SingleSecurityIndicatorForm.Instance.SetSecurity(ActivePosition.Security);
            }
            else
            {
                ActivePosition = null;
            }
        }

        #endregion
        #region Trades

        private int Offset = 1;
        private List<int> PendingApprovalCodes = new List<int>();

        #endregion

        private LiveTradingManager(LiveTradingProvider tradingProvider)
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

            TradingProvider.OpenPositionsUpdate += (s, e) =>
            {
                if (!LiveDataProvider.Instance.Connected)
                {
                    Log(new LogMessage("Trading Manager", "Cannot request streaming data for position; Data Manager not connected", LogMessageType.SystemError));
                    return;
                }

                LiveDataProvider.Instance.RequestSnapshotQuotes(e.Position.Security);
            };
        }

        public (LiveTrade trade, LiveTrade stopTrade)? ApproveOrder(LiveOrder order, LiveOrder stopOrder)
        {
            var account = LiveTradingManager.Instance.ActiveAccount;

            if (LiveRiskManager.Instance.ApproveOrder(order, stopOrder, account))
            {
                var approvedTrade = OrderToTrade(order);
                approvedTrade.ApprovalCode = ApprovalCode(this.Offset++);
                PendingApprovalCodes.Add(approvedTrade.ApprovalCode);

                if (stopOrder != null)
                {
                    var approvedStop = OrderToTrade(stopOrder);
                    approvedStop.ApprovalCode = ApprovalCode(this.Offset++);
                    PendingApprovalCodes.Add(approvedStop.ApprovalCode);

                    return (approvedTrade, approvedStop);
                }

                return (approvedTrade, null);

            }
            else
                return null;
        }
        private LiveTrade OrderToTrade(LiveOrder order)
        {
            var ret = new LiveTrade(
                order.Security,
                order.OrderDirection,
                order.OrderType,
                order.OrderSize,
                order.LimitPrice);

            return ret;
        }
        private int ApprovalCode(int offset)
        {
            return DateTime.Now.GetHashCode();
        }
        public void CancelTrades(LiveTrade trade, LiveTrade stopTrade)
        {
            PendingApprovalCodes.RemoveAll(x => x == trade.ApprovalCode);
            if (stopTrade != null)
                PendingApprovalCodes.RemoveAll(x => x == stopTrade.ApprovalCode);
        }
        public bool ExecuteTrades(LiveTrade trade, LiveTrade stopTrade)
        {
            if (!PendingApprovalCodes.Contains(trade.ApprovalCode) || (stopTrade != null && !PendingApprovalCodes.Contains(stopTrade.ApprovalCode)))
            {
                Log(new LogMessage("Trade Mgr", $"ERROR: Trades not approved: {trade} and {stopTrade}", LogMessageType.TradingError));
                return false;
            }

            ActiveAccount.Portfolio.AddTrade(trade);
            if (stopTrade != null)
                ActiveAccount.Portfolio.AddTrade(stopTrade);

            TradingProvider.SubmitTrades(trade, stopTrade);

            CancelTrades(trade, stopTrade);

            return true;
        }
    }
}
