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

        [DisplayValue("Available Funds", "$#,##0.00")]
        public decimal AvailableFunds { get; set; }
        [DisplayValue("Buying Power", "$#,##0.00")]
        public decimal BuyingPower { get; set; }
        [DisplayValue("Cash Balance", "$#,##0.00")]
        public decimal CashBalance { get; set; }

        [DisplayValue("Equity with Loan Value", "$#,##0.00")]
        public decimal EquityWithLoanValue { get; set; }
        [DisplayValue("Excess Liquidity", "$#,##0.00")]
        public decimal ExcessLiquidity { get; set; }
        [DisplayValue("Initial Margin Requirement", "$#,##0.00")]
        public decimal InitMarginReq { get; set; }
        [DisplayValue("Maintenance Margin Requirement", "$#,##0.00")]
        public decimal MaintMarginReq { get; set; }

        [DisplayValue("Gross Position Value", "$#,##0.00")]
        public decimal GrossPositionValue { get; set; }
        [DisplayValue("Net Liquidation Value", "$#,##0.00")]
        public decimal NetLiquidation { get; set; }
        [DisplayValue("Market Value of Stock", "$#,##0.00")]
        public decimal StockMarketValue { get; set; }


        [DisplayValue("Reg T Margin Balance", "$#,##0.00")]
        public decimal RegTMargin { get; set; }
        [DisplayValue("Reg T Equity", "$#,##0.00")]
        public decimal RegTEquity { get; set; }
        [DisplayValue("SMA Account Balance", "$#,##0.00")]
        public decimal SMA { get; set; }

        [DisplayValue("Realized PNL", "$#,##0.00")]
        public decimal RealizedPnL { get; set; }
        [DisplayValue("Unrealized PNL", "$#,##0.00")]
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
            this.Positions = new List<LivePosition>();
            this.Trades = new List<LiveTrade>();
        }

        public string AccountId { get; protected set; }
        public virtual List<LivePosition> Positions { get; protected set; }
        protected virtual List<LiveTrade> Trades { get; set; }

        public void AddTrade(LiveTrade trade)
        {
            if (!Trades.Exists(x => x.TradeId == trade.TradeId))
            {
                Trades.Add(trade);
            }
        }
        public LiveTrade GetTrade(int tradeId)
        {
            return Trades.Find(X => X.TradeId == tradeId);
        }

        public bool HasOpenPosition(string ticker)
        {
            return Positions.SingleOrDefault(x => x.Ticker == ticker && x.IsOpen) != null;
        }
        public T GetPosition<T>(Security security, bool create = true) where T : LivePosition, new()
        {
            var ret = (from position in Positions
                       where position.Security.Ticker == security.Ticker
                       //where position.IsOpen
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

    public class LivePosition : INotifyPropertyChanged
    {
        #region Events

        public event OpenPositionEventHandler PositionChanged;
        protected void OnPositionChanged(LivePosition position)
        {
            PositionChanged?.Invoke(this, new OpenPositionEventArgs(position));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private Security _Security { get; set; }
        public Security Security
        {
            get => _Security;
            set
            {
                _Security = value;
                _Security.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
            }
        }

        [DisplayValue("Symbol", "")]
        public string Ticker => Security?.Ticker ?? string.Empty;

        [DisplayValue("Company", "")]
        public string CompanyName => Security?.LongName ?? string.Empty;

        private decimal _AverageCost { get; set; }
        [DisplayValue("Average Cost", "$#,##0.00")]
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
        [DisplayValue("Position Size", "#,##0.0")]
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

        [DisplayValue("Basis Cost", "$#,##0.0")]
        public decimal BasisCost
        {
            get => Size * AverageCost;
        }

        [DisplayValue("Last Trade", "$#,##0.00")]
        public decimal LastTrade => Security.LastTrade;

        [DisplayValue("Market Value", "$#,##0.0")]
        public decimal MarketValue
        {
            get => LastTrade * Size;
        }

        private decimal? _UnrlPNLDollars { get; set; } = null;
        [DisplayValue("Unrl. PNL", "$#,##0.00")]
        public decimal UnrlPNLDollars
        {
            get
            {
                if (LastTrade == 0)
                    return _UnrlPNLDollars ?? 0;
                else
                    return (LastTrade - AverageCost) * Size;
            }
        }

        [DisplayValue("PNL %", "0.00%")]
        public decimal UnrlPNLPercent
        {
            get
            {
                if (AverageCost == 0 || Size == 0)
                    return 0;
                return (UnrlPNLDollars / Size / AverageCost) * Math.Sign(Size);
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
        public void BrokerReportedUnrlPnl(decimal pnl)
        {
            this._UnrlPNLDollars = pnl;
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

    public class LiveOrder
    {
        public Security Security { get; }

        [DisplayValue("Order Direction", "")]
        public TradeActionBuySell OrderDirection { get; }

        [DisplayValue("Order Type", "")]
        public TradeType OrderType { get; }

        [DisplayValue("Limit Px", "$#,##0.00")]
        public decimal LimitPrice { get; }

        [DisplayValue("Order Size", "#,##0.00")]
        public decimal OrderSize { get; }

        [DisplayValue("Limit Px", "$#,##0.00")]
        public decimal TotalMoney => OrderSize * LimitPrice * -(OrderDirection.ToInt());

        [DisplayValue("Commission", "$#,##0.00")]
        public decimal Commission => TradingEnvironment.Instance.CommissionCharged(this);

        public LiveOrder(Security security, TradeActionBuySell tradeActionBuySell, decimal size, decimal limitPrice, TradeType orderType)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            OrderDirection = tradeActionBuySell;
            LimitPrice = limitPrice;
            OrderSize = size;
            OrderType = orderType;
        }

        public override string ToString()
        {
            return string.Format($"{OrderDirection.Description()} {OrderSize} {Security.Ticker} @ {LimitPrice:$#,##0.00}");
        }
    }

    public class LiveTrade
    {
        public int TradeId { get; set; }

        public Security Security { get; } = null;

        public int ApprovalCode { get; set; }

        public TradeActionBuySell TradeDirection { get; } = TradeActionBuySell.None;
        public TradeType TradeType { get; }

        public LiveTradeStatus TradeStatus { get; set; } = LiveTradeStatus.NotSet;

        public decimal SubmittedQuantity { get; private set; }
        public decimal FilledQuantity { get; set; }
        public decimal UnfilledQuantity => SubmittedQuantity - FilledQuantity;

        public decimal LimitPrice { get; } = -1;
        public decimal LastFillPrice { get; set; }
        public decimal AverageFillPrice { get; set; }

        public LiveTrade(Security security, TradeActionBuySell tradeDirection, TradeType tradeType, decimal submittedQuantity, decimal limitPrice)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            TradeDirection = tradeDirection;
            TradeType = tradeType;
            SubmittedQuantity = submittedQuantity;
            LimitPrice = limitPrice;
        }

        public override string ToString()
        {
            return string.Format($"{TradeDirection.Description()} {SubmittedQuantity} {Security.Ticker} @ {LimitPrice:$#,##0.00}");
        }
    }

}
