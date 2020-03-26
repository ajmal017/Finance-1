using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance.Data;
using System.ComponentModel;
using IBApi;

namespace Finance.LiveTrading
{
    public class LiveAccount : INotifyPropertyChanged
    {

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnValueChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event OpenPositionEventHandler PositionChanged;
        protected void OnPositionChanged(LivePosition position)
        {
            PositionChanged?.Invoke(this, new OpenPositionEventArgs(position));
        }

        #endregion

        public string AccountId { get; }

        [AccountValue("Available Funds", "$#,##0.00")]
        public decimal AvailableFunds { get; set; }
        [AccountValue("Buying Power", "$#,##0.00")]
        public decimal BuyingPower { get; set; }
        [AccountValue("Cash Balance", "$#,##0.00")]
        public decimal CashBalance { get; set; }

        [AccountValue("Equity with Loan Value", "$#,##0.00")]
        public decimal EquityWithLoanValue { get; set; }
        [AccountValue("Excess Liquidity", "$#,##0.00")]
        public decimal ExcessLiquidity { get; set; }
        [AccountValue("Initial Margin Requirement", "$#,##0.00")]
        public decimal InitMarginReq { get; set; }
        [AccountValue("Maintenance Margin Requirement", "$#,##0.00")]
        public decimal MaintMarginReq { get; set; }

        [AccountValue("Gross Position Value", "$#,##0.00")]
        public decimal GrossPositionValue { get; set; }
        [AccountValue("Net Liquidation Value", "$#,##0.00")]
        public decimal NetLiquidation { get; set; }
        [AccountValue("Market Value of Stock", "$#,##0.00")]
        public decimal StockMarketValue { get; set; }


        [AccountValue("Reg T Margin Balance", "$#,##0.00")]
        public decimal RegTMargin { get; set; }
        [AccountValue("Reg T Equity", "$#,##0.00")]
        public decimal RegTEquity { get; set; }
        [AccountValue("SMA Account Balance", "$#,##0.00")]
        public decimal SMA { get; set; }

        [AccountValue("Realized PNL", "$#,##0.00")]
        public decimal RealizedPnL { get; set; }
        [AccountValue("Unrealized PNL", "$#,##0.00")]
        public decimal UnrealizedPnL { get; set; }

        public virtual LivePortfolio Portfolio { get; protected set; }

        public LiveAccount(string accountId)
        {
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            Portfolio = new LivePortfolio(AccountId);

            this.InitializeMe();
        }

        [Initializer]
        private void InitializeHandlers()
        {
            Portfolio.PositionChanged += (s, e) => OnPositionChanged(e.Position);
        }

        public T UpdatePosition<T>(string ticker, decimal position, decimal averageCost) where T : LivePosition, new()
        {
            if (Portfolio.HasOpenPosition(ticker))
            {
                T pos = Portfolio.GetPosition<T>(ticker);
                pos.UpdateValues(position, averageCost);
                return pos;
            }
            else
            {
                Security security = RefDataManager.Instance.GetSecurity(ticker);
                if (security == null)
                    throw new LiveTradeSystemException() { message = $"Cannot create position: Security {ticker} not found in database" };

                T ret = Portfolio.GetPosition<T>(security, true);
                ret.UpdateValues(position, averageCost);
                return ret;
            }
        }

        public virtual T MockCopy<T>() where T : LiveAccount, new()
        {
            T ret = ((T)this).MemberwiseClone() as T;
            ret.Portfolio = null;
            PropertyChanged = null;
            PositionChanged = null;

            return ret;
        }
    }
    public class IbkrAccount : LiveAccount
    {
        public new IbkrPortfolio Portfolio { get; protected set; }

        public IbkrAccount(string accountId) : base(accountId)
        {

            //
            // Set up portfolio
            //
            Portfolio = new IbkrPortfolio(AccountId);
            Portfolio.PositionChanged += (s, e) => OnPositionChanged(e.Position);
        }

        public void SetAccountValue(string field, string value)
        {
            if (GetType().GetProperty(field) == null)
                return;

            if (Decimal.TryParse(value, out var pVal) == false)
                return;

            GetType().GetProperty(field).SetValue(this, pVal);
            OnValueChanged(field);
        }
    }

    public class LivePortfolio
    {
        #region Events

        public event OpenPositionEventHandler PositionChanged;
        protected void OnPositionChanged(LivePosition position)
        {
            PositionChanged?.Invoke(this, new OpenPositionEventArgs(position));
        }

        #endregion

        public LivePortfolio(string accountId)
        {
            this.AccountId = accountId;
            this.Positions = new BindingList<LivePosition>();
            this.Trades = new List<LiveTrade>();
        }

        public string AccountId { get; protected set; }
        public virtual BindingList<LivePosition> Positions { get; protected set; }
        protected virtual List<LiveTrade> Trades { get; set; }

        public void AddTrade(LiveTrade trade)
        {

        }
        public bool HasOpenPosition(string ticker)
        {
            return Positions.SingleOrDefault(x => x.Ticker == ticker && x.IsOpen) != null;
        }
        public T GetPosition<T>(Security security, bool create = true) where T : LivePosition, new()
        {
            var ret = (from position in Positions
                       where position.Security.Ticker == security.Ticker
                       where position.IsOpen
                       select position).SingleOrDefault();

            if (ret != null)
                return (T)ret;

            ret = Positions.AddAndReturn(new T() { Security = security });

            ret.PositionChanged += (s, e) => OnPositionChanged(e.Position);

            return (T)ret;
        }
        public T GetPosition<T>(string ticker) where T : LivePosition
        {
            var ret = (from position in Positions
                       where position.Security.Ticker == ticker
                       where position.IsOpen
                       select position).SingleOrDefault();

            return (T)ret;
        }
        public List<T> GetPositions<T>(bool includeClosed = true) where T : LivePosition
        {
            return includeClosed ? this.Positions as List<T> : this.Positions.Where(x => x.IsOpen).ToList() as List<T>;
        }

    }
    public class IbkrPortfolio : LivePortfolio
    {
        public new BindingList<IbkrPosition> Positions { get; protected set; }

        public IbkrPortfolio(string accountId) : base(accountId) { }

    }

    public class LivePosition
    {
        #region Events

        public event OpenPositionEventHandler PositionChanged;
        protected void OnPositionChanged(LivePosition position)
        {
            PositionChanged?.Invoke(this, new OpenPositionEventArgs(position));
        }

        #endregion

        public Security Security { get; set; }

        [AccountValue("Symbol", "")]
        public string Ticker => Security?.Ticker ?? string.Empty;

        [AccountValue("Company", "")]
        public string CompanyName => Security?.LongName ?? string.Empty;

        private decimal _AverageCost { get; set; }
        [AccountValue("Average Cost", "$#,##0.00")]
        public decimal AverageCost
        {
            get => _AverageCost;
            set
            {
                if (_AverageCost != value)
                {
                    _AverageCost = value;
                    OnPositionChanged(this);
                }
            }
        }

        private decimal _Size { get; set; }
        [AccountValue("Position Size", "#,##0.0")]
        public decimal Size
        {
            get => _Size;
            set
            {
                if (_Size != value)
                {
                    _Size = value;
                    OnPositionChanged(this);
                }
            }
        }
        public bool IsOpen => Size != 0;

        public PositionDirection PositionDirection => Size > 0 ? PositionDirection.LongPosition : PositionDirection.ShortPosition;

        public LivePosition()
        {
        }

        public void UpdateValues(decimal? position = null, decimal? averageCost = null)
        {
            if (position.HasValue)
                this.Size = position.Value;

            if (averageCost.HasValue)
                this.AverageCost = averageCost.Value;

            OnPositionChanged(this);
        }
    }
    public class IbkrPosition : LivePosition
    {
        public Contract Contract
        {
            get
            {
                return new Contract()
                {
                    Currency = "USD",
                    Symbol = Security.Ticker,
                    PrimaryExch = Security.Exchange,
                    SecType = "STK"
                };
            }
        }

        public IbkrPosition() { }
    }

    public class LiveTrade
    {
        Security Security { get; } = null;

        public LiveTradeStatus TradeStatus { get; private set; } = LiveTradeStatus.NotSet;

        public decimal SubmittedQuantity { get; private set; }
        private List<decimal> FilledQuantities = new List<decimal>();
        public decimal FilledQuantity => FilledQuantities.Sum();
        public decimal UnfilledQuantity => SubmittedQuantity - FilledQuantity;

        public decimal LimitPrice { get; } = -1;
        public decimal LastFillPrice { get; set; }
        public decimal AverageFillPrice { get; set; }

    }

    public class LiveOrder
    {
        public Security Security { get; }
        public TradeActionBuySell TradeActionBuySell { get; }
        public decimal LimitPrice { get; }

        public LiveOrder(Security security, TradeActionBuySell tradeActionBuySell, decimal limitPrice)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            TradeActionBuySell = tradeActionBuySell;
            LimitPrice = limitPrice;
        }
    }
}
