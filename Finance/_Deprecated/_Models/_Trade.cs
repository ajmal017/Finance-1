using Finance;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Helpers;

namespace Finance.Models
{
    public partial class Trade
    {
        [Key]
        public int TradeId { get; set; }

        // Static Trade ID variable shared across all instances
        private static int _NextTradeId = 0;
        public static int NextTradeId
        {
            get { return ++_NextTradeId; }
        }

        public virtual Security Security { get; set; }

        public TradeActionBuySell TradeActionBuySell { get; set; }
        public TradeType TradeType { get; set; }
        public TradePriority TradePriority { get; set; }

        public DateTime TradeDate { get; set; }

        public DateTime SettleDate { get; set; }

        // Quantity of the trade, as a positive number
        public int Quantity { get; set; }

        public decimal LimitPrice { get; set; } = 0m;
        public decimal StopPrice { get; set; } = 0m;
        public decimal ExecutedPrice { get; set; } = 0m;

        public bool ApiTrade { get; set; }

    }

    /// <summary>
    /// Trade status and cross-reference
    /// </summary>
    public partial class Trade
    {
        // Maintains status of this trade and modifies/cancels related trades when required (ie, multiple stoploss trades input

        private TradeStatus _TradeStatus;
        public TradeStatus TradeStatus
        {
            get => _TradeStatus;
            set
            {
                if (_TradeStatus == TradeStatus.Cancelled && value != TradeStatus.Cancelled)
                    throw new InvalidTradeOperationException()
                    {
                        message = "Attempted to change status of Cancelled trade"
                    };

                if (value == TradeStatus.Executed)
                {
                    // Settle is T+2 for US equities
                    SettleDate = Calendar.SettleDate(TradeDate, Security.SecurityType);
                    ExecutionCancelsOthers.RemoveAll(x => x.TradeId != TradeId);
                    foreach (Trade trade in ExecutionCancelsOthers)
                        trade.TradeStatus = TradeStatus.Cancelled;
                }
                _TradeStatus = value;
            }
        }

        /// <summary>
        /// List of associated trades which cancel upon execution of this trade.
        /// </summary>
        public virtual List<Trade> ExecutionCancelsOthers { get; set; } = new List<Trade>();

    }

    /// <summary>
    /// Methods
    /// </summary>
    public partial class Trade
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="security"></param>
        /// <param name="tradeAction"></param>
        /// <param name="quantity"></param>
        /// <param name="tradeType"></param>
        /// <param name="limitOrStopPrice"></param>
        public Trade(Security security, TradeActionBuySell tradeAction, int quantity, TradeType tradeType, decimal limitOrStopPrice = 0, bool apiTrade = true, bool IgnoreTradeId = false)
        {

            Security = security;
            TradeActionBuySell = tradeAction;
            Quantity = quantity;
            TradeType = tradeType;

            switch (tradeType)
            {
                case TradeType.Market:
                    break;
                case TradeType.Limit:
                    LimitPrice = limitOrStopPrice;
                    break;
                case TradeType.Stop:
                    StopPrice = limitOrStopPrice;
                    break;
                case TradeType.StopLimit:
                    LimitPrice = limitOrStopPrice;
                    StopPrice = limitOrStopPrice;
                    break;
                default:
                    break;
            }

            LimitPrice = limitOrStopPrice;

            ApiTrade = apiTrade;

            TradeStatus = TradeStatus.NotSet;
            TradePriority = TradePriority.NotSet;

            if (!IgnoreTradeId)
                TradeId = NextTradeId;

        }

        /// <summary>
        /// Executes a trade
        /// </summary>
        /// <param name="executionPrice"></param>
        /// <returns></returns>
        public void Execute(Portfolio portfolio, decimal executionPrice, DateTime executionDate, int shares)
        {
            if (TradeStatus != TradeStatus.Pending)
                throw new InvalidTradeOperationException();

            ExecutedPrice = executionPrice;
            TradeDate = executionDate;
            TradeStatus = TradeStatus.Executed;

            portfolio.AddTrade(this);
        }

        /// <summary>
        /// Gives us the +/- quantity based on trade direction
        /// </summary>
        public int DirectionalQuantity
        {
            get
            {
                return (int)TradeActionBuySell * Quantity;
            }
        }

        /// <summary>
        /// Returns the total cash impact of the initial trade execution, or if not executed yet, the expected (limit) price
        /// Buys will return a negative value, sells will return a positive
        /// </summary>
        public decimal TotalCashImpact
        {
            get
            {
                switch (TradeStatus)
                {
                    case TradeStatus.Executed:
                        return -(DirectionalQuantity * ExecutedPrice);
                    default:
                        return -(DirectionalQuantity * LimitPrice);
                }

            }
        }

        /// <summary>
        /// Returns the total dollar impact of the initial trade execution, or if not executed yet, the expected (limit) price
        /// Always a positive number
        /// </summary>
        public decimal TotalCashImpactAbsolute
        {
            get
            {
                switch (TradeStatus)
                {
                    case TradeStatus.Executed:
                        return (Quantity * ExecutedPrice);
                    default:
                        return (Quantity * LimitPrice);
                }

            }
        }

        /// <summary>
        /// Returns a boolean value indicating whether or not this limit trade will execute based on price action on a given day
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public bool LimitPriceWillExecute(DateTime AsOf)
        {
            if (TradeType != TradeType.Limit)
                throw new InvalidTradeOperationException() { message = "Trade must be Limit type to check value" };

            switch (TradeActionBuySell)
            {
                case TradeActionBuySell.None:
                    throw new InvalidTradeOperationException() { message = "TradeType not set" };
                case TradeActionBuySell.Buy:
                    if (LimitPrice >= Security.GetPriceBar(AsOf).Low)
                        return true;
                    return false;
                case TradeActionBuySell.Sell:
                    if (LimitPrice <= Security.GetPriceBar(AsOf).High)
                        return true;
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns a price at which this trade would be executed, unadjusted for slippage.  Throws exception if not executed.
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public decimal LimitPriceExecuted(DateTime AsOf)
        {

            var bar = Security.GetPriceBar(AsOf);

            switch (TradeActionBuySell)
            {
                case TradeActionBuySell.None:
                    throw new InvalidTradeOperationException();
                case TradeActionBuySell.Buy:
                    {
                        // If the limit price is within the range of the day's prices, execute at limit price
                        // If the limit price is above the range of the day's prices, we would execute at the open price
                        if (LimitPrice >= bar.Low && LimitPrice <= bar.High)
                        {
                            return LimitPrice;
                        }
                        else if (LimitPrice > bar.High)
                        {
                            return bar.Open;
                        }
                        throw new InvalidTradeOperationException() { message = "Limit trade not executable at these prices" };
                    }
                case TradeActionBuySell.Sell:
                    {
                        // If the limit price is within the range of the day's prices, execute at limit price
                        // If the limit price is below the range of the day's prices, we would execute at the open price
                        if (LimitPrice >= bar.Low && LimitPrice <= bar.High)
                        {
                            return LimitPrice;
                        }
                        else if (LimitPrice < bar.Low)
                        {
                            return bar.Open;
                        }
                        throw new InvalidTradeOperationException() { message = "Limit trade not executable at these prices" };
                    }
                default:
                    throw new InvalidTradeOperationException();
            }

        }

    }

    /// <summary>
    /// Equality implementations
    /// </summary>
    public partial class Trade : IEquatable<Trade>, IComparable<Trade>
    {
        /// <summary>
        /// Compares trades based on total transaction size
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Trade other)
        {
            if (other.TotalCashImpactAbsolute > TotalCashImpactAbsolute)
                return -1;
            return 1;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Trade);
        }

        public bool Equals(Trade other)
        {
            return other != null &&
                   TradeId == other.TradeId;
        }

        public override int GetHashCode()
        {
            return -612597508 + TradeId.GetHashCode();
        }

        public static bool operator ==(Trade trade1, Trade trade2)
        {
            return EqualityComparer<Trade>.Default.Equals(trade1, trade2);
        }

        public static bool operator !=(Trade trade1, Trade trade2)
        {
            return !(trade1 == trade2);
        }
    }

    /// <summary>
    /// Copy
    /// </summary>
    public partial class Trade
    {
        public Trade Copy()
        {
            Trade ret = new Trade(Security, TradeActionBuySell, Quantity, TradeType, LimitPrice, ApiTrade, true)
            {
                _TradeStatus = _TradeStatus,
                LimitPrice = LimitPrice,
                StopPrice = StopPrice,
                ExecutedPrice = ExecutedPrice,
                SettleDate = SettleDate,
                TradeDate = TradeDate,
                TradePriority = TradePriority,
                TradeId = TradeId
            };
            ExecutionCancelsOthers.ForEach(x => ret.ExecutionCancelsOthers.Add(x.Copy()));

            return ret;
        }
    }

    /// <summary>
    /// Logging and output formatting
    /// </summary>
    public partial class Trade
    {
        public override string ToString()
        {
            string tradeAction;
            switch (TradeActionBuySell)
            {
                case TradeActionBuySell.None:
                    tradeAction = "NON";
                    break;
                case TradeActionBuySell.Buy:
                    tradeAction = "BOT";
                    break;
                case TradeActionBuySell.Sell:
                    tradeAction = "SLD";
                    break;
                default:
                    tradeAction = "ERR";
                    break;
            }

            if (TradeType == TradeType.Stop && TradeStatus != TradeStatus.Executed)
                return string.Format($"STOP  {TradeId:0000}: {tradeAction} {Quantity} {Security.Ticker,-4} at {StopPrice:$##0.00} on {TradeDate.ToShortDateString(),-8} ");

            else
                return string.Format($"Trade {TradeId:0000}: {tradeAction} {Quantity} {Security.Ticker,-4} at {ExecutedPrice:$##0.00} on {TradeDate.ToShortDateString(),-8} ");
        }

    }
}
