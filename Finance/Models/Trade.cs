using Finance;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Helpers;

namespace Finance
{
    [NotMapped]
    public partial class Trade
    {

        public int TradeId { get; } = 0;

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
                    _SettleDate = Calendar.SettleDate(TradeDate, Security.SecurityType);
                }
                _TradeStatus = value;
            }
        }

        public DateTime TradeDate { get; set; }

        private DateTime _SettleDate { get; set; }
        public DateTime SettleDate
        {
            get
            {
                return _SettleDate;
            }
        }

        public int Quantity { get; set; }
        public int DirectionalQuantity => (int)TradeActionBuySell * Quantity;

        public decimal LimitPrice { get; set; } = 0m;
        public decimal StopPrice { get; set; } = 0m;
        public decimal ExecutedPrice { get; set; } = 0m;

        // Returns either stop or limit price depending on trade type
        public decimal ExpectedExecutionPrice
        {
            get
            {
                switch (TradeType)
                {
                    case TradeType.Market:
                        throw new InvalidDataRequestException() { message = "Cannot determine expected execution price for Market type trade" };
                    case TradeType.Limit:
                        return LimitPrice;
                    case TradeType.Stop:
                        return StopPrice;
                    default:
                        throw new UnknownErrorException();
                }
            }
        }

        public Trade() { }
        public Trade(Security security,
            TradeActionBuySell tradeAction,
            int quantity,
            TradeType tradeType,
            decimal limitPrice = 0,
            decimal stopPrice = 0)
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
                    if (limitPrice == 0)
                        throw new InvalidTradeOperationException() { message = "Limit trades must specify limit price" };
                    LimitPrice = limitPrice;
                    break;
                case TradeType.Stop:
                    if (stopPrice == 0)
                        throw new InvalidTradeOperationException() { message = "Stop trades must specify stop price" };
                    StopPrice = stopPrice;
                    break;
                default:
                    break;
            }

            TradeStatus = TradeStatus.NotSet;
            TradePriority = TradePriority.NotSet;

            TradeId = NextTradeId;

        }

    }

    public partial class Trade
    {
        /// <summary>
        /// Mark this trade as executed and update needed fields
        /// </summary>
        /// <param name="executionDate"></param>
        /// <param name="executionPrice"></param>
        public void MarkExecuted(DateTime executionDate, decimal executionPrice)
        {
            if (TradeActionBuySell == TradeActionBuySell.None)
                throw new InvalidTradeOperationException() { message = "TradeAction not set, cannot execute" };
            if (Quantity <= 0)
                throw new InvalidTradeOperationException() { message = "Invalid trade quantity, cannot execute" };

            TradeDate = executionDate;
            ExecutedPrice = executionPrice;
            TradeStatus = TradeStatus.Executed;
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
                        return -(DirectionalQuantity * ExpectedExecutionPrice);
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
                        return (Quantity * ExpectedExecutionPrice);
                }

            }
        }
    }

    public partial class Trade
    {
        public Trade Copy()
        {
            Trade ret = new Trade()
            {
                Security = Security,
                TradeActionBuySell = TradeActionBuySell,
                Quantity = Quantity,
                TradeType = TradeType,
                LimitPrice = LimitPrice,
                StopPrice = StopPrice,
                _TradeStatus = _TradeStatus,
                ExecutedPrice = ExecutedPrice,
                _SettleDate = SettleDate,
                TradeDate = TradeDate,
                TradePriority = TradePriority
                // TradeID will be unset
            };

            return ret;
        }

    }
    
    public partial class Trade : IEquatable<Trade>
    {
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

        public static bool operator ==(Trade base1, Trade base2)
        {
            return EqualityComparer<Trade>.Default.Equals(base1, base2);
        }

        public static bool operator !=(Trade base1, Trade base2)
        {
            return !(base1 == base2);
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
