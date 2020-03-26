using Finance.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Helpers;

namespace Finance
{
    public partial class Position
    {
        [Key]
        public int PositionId { get; set; } = 0;

        // Static Position ID variable shared across all instances
        private static int _NextPositionId = 0;
        public static int NextPositionId
        {
            get { return ++_NextPositionId; }
        }

        public Security Security { get; set; }

        private PositionDirection _positionDirection;
        public PositionDirection PositionDirection
        {
            get
            {
                return _positionDirection;
            }
            private set
            {
                _positionDirection = value;
            }

        }

        // List of executed trades relating to this position
        public virtual List<Trade> ExecutedTrades { get; set; } = new List<Trade>();

        public Position() { }
        public Position(Security security)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            PositionId = NextPositionId;
        }

    }

    public partial class Position
    {
        public bool IsOpen(DateTime AsOf)
        {
            return (Size(AsOf) != 0 ? true : false);
        }

        /// <summary>
        /// Is either positive or negative
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public int Size(DateTime AsOf)
        {
            // Size is calculated by taking the sum value of all trades up to and including the AsOf date
            IEnumerable<int> ret1 = (from t in ExecutedTrades where t.TradeDate <= AsOf select (t.DirectionalQuantity));
            return ret1 == null ? 0 : ret1.Sum();
        }

        /// <summary>
        /// Average cost of an open position
        /// </summary>
        public decimal AverageCost(DateTime AsOf)
        {
            try
            {
                if (!IsOpen(AsOf))
                    return 0;

                // Get an ordered list of all trades up to and including the AsOf date
                var tradeList = ExecutedTrades.Where(x => x.TradeStatus == TradeStatus.Executed && x.TradeDate <= AsOf).OrderBy(x => x.TradeDate).ToList();

                return tradeList.AverageCost(PositionDirection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns the sum total of all cash transactions (positive or negative) so far in this security
        /// Sells = positive cash, Buys = negative cash
        /// </summary>
        /// <returns></returns>
        public decimal NetCashImpact(DateTime AsOf)
        {
            return (from trade in ExecutedTrades where trade.TradeDate <= AsOf select (trade.TotalCashImpact)).Sum();
        }

        /// <summary>
        /// Returns the current net value (positive or negative) of the current position at the end of the day
        /// </summary>
        /// <returns></returns>
        public decimal GrossPositionValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                switch (MarketValues)
                {
                    case TimeOfDay.MarketOpen:
                        return (Size(AsOf) * Security.GetPriceBarOrLastPrior(AsOf, PriceBarSize.Daily, 1).Open);
                    case TimeOfDay.MarketEndOfDay:
                        return (Size(AsOf) * Security.GetPriceBarOrLastPrior(AsOf, PriceBarSize.Daily, 1).Close);
                    default:
                        throw new InvalidRequestValueException() { message = "MarketValues unknown value" };
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public decimal TotalUnrealizedPnL(DateTime AsOf, TimeOfDay MarketValues)
        {
            decimal RawPNLperShare = 0m;

            switch (MarketValues)
            {
                case TimeOfDay.MarketOpen:
                    RawPNLperShare = ((Security.GetPriceBar(AsOf, PriceBarSize.Daily, false).Open - AverageCost(AsOf)));
                    break;
                case TimeOfDay.MarketEndOfDay:
                    RawPNLperShare = ((Security.GetPriceBar(AsOf, PriceBarSize.Daily, false).Close - AverageCost(AsOf)));
                    break;
                default:
                    throw new InvalidRequestValueException() { message = "MarketValues unknown value" };
            }

            // Correct for long/short
            return (RawPNLperShare * Size(AsOf));
        }
        public decimal TotalRealizedPnL(DateTime AsOf)
        {
            try
            {
                // Realized PNL must be calculated on a per-share basis as trades are executed
                // Each trade which reduces the size of an open position contributes to realized PNL
                // The realized PNL is the execution price of the trade minus the average price of the shares liquidated in that trade

                // If this position has no closing trades, return 0
                if (ExecutedTrades.Where(x => x.TradeActionBuySell.ToInt() != PositionDirection.ToInt()).ToList().Count == 0)
                    return 0m;

                // Get an ordered list of all trades up to and including the last position-reducing trade prior to the AsOf date
                // ie, if there are buys in a long position after our last sale, but before the AsOf date, we don't want that factored into the average buy price
                var tradeList = ExecutedTrades.Where(x => x.TradeStatus == TradeStatus.Executed && x.TradeDate <= AsOf).OrderBy(x => x.TradeDate).ToList();

                // Remove all elements after the last trade in the direction of the position (ie, remove all trailing buys in a long position)
                while (tradeList.Last().TradeActionBuySell.ToInt() == PositionDirection.ToInt())
                    tradeList.Remove(tradeList.Last());

                // Iterate through the trades and compute realized gains
                int sharesOpen = 0;
                decimal averageCost = 0;
                decimal realized = 0;

                foreach (var trd in tradeList)
                {
                    // Buy trades increase position and update average price
                    // Sell trades reduce position and update realized PNL
                    if (trd.TradeActionBuySell.ToInt() == PositionDirection.ToInt())
                    {
                        averageCost = ((averageCost * sharesOpen) + (trd.TotalCashImpactAbsolute)) / (sharesOpen += trd.Quantity);
                    }
                    else if ((int)trd.TradeActionBuySell == -(int)PositionDirection)
                    {
                        realized += (PositionDirection.ToInt() * (trd.ExecutedPrice - averageCost)) * trd.Quantity;
                        sharesOpen -= trd.Quantity;
                    }
                }

                return realized;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }
        public decimal TotalReturnPercentage(DateTime AsOf)
        {

            // Total return is Sum($Opening+$Closing) / Abs($Opening)

            var openingTrades = ExecutedTrades.Where(x => x.TradeActionBuySell.ToInt() == PositionDirection.ToInt()).ToList();
            var closingTrades = ExecutedTrades.Where(x => x.TradeActionBuySell.ToInt() != PositionDirection.ToInt()).ToList();

            decimal openingTotalDollars = openingTrades.Sum(x => x.TotalCashImpact);
            decimal closingTotalDollars;

            if (IsOpen(AsOf))
            {
                // Use the current cash value of the position
                closingTotalDollars = GrossPositionValue(AsOf, TimeOfDay.MarketEndOfDay) + closingTrades.Sum(x => x.TotalCashImpact);
            }
            else
            {
                closingTotalDollars = closingTrades.Sum(x => x.TotalCashImpact);
            }

            var ret = (openingTotalDollars + closingTotalDollars) / Math.Abs(openingTotalDollars);
            return ret;
        }
        public decimal TotalReturnDollars(DateTime AsOf)
        {
            // Total return is Sum($Opening+$Closing) / Abs($Opening)

            var openingTrades = ExecutedTrades.Where(x => x.TradeActionBuySell.ToInt() == PositionDirection.ToInt()).ToList();
            var closingTrades = ExecutedTrades.Where(x => x.TradeActionBuySell.ToInt() != PositionDirection.ToInt()).ToList();

            decimal openingTotalDollars = openingTrades.Sum(x => x.TotalCashImpact);
            decimal closingTotalDollars;

            if (IsOpen(AsOf))
            {
                // Use the current cash value of the position
                closingTotalDollars = GrossPositionValue(AsOf, TimeOfDay.MarketEndOfDay);
            }
            else
            {
                closingTotalDollars = closingTrades.Sum(x => x.TotalCashImpact);
            }

            var ret = (openingTotalDollars + closingTotalDollars);
            return ret;
        }
        public int DaysHeld(DateTime AsOf)
        {
            if (IsOpen(AsOf))
            {
                return Convert.ToInt32((AsOf - ExecutedTrades.Min(x => x.TradeDate)).TotalDays);
            }
            if (AsOf > ExecutedTrades.Max(x => x.TradeDate))
            {
                return Convert.ToInt32((ExecutedTrades.Max(x => x.TradeDate) - ExecutedTrades.Min(x => x.TradeDate)).TotalDays);
            }
            else
            {
                return Convert.ToInt32((AsOf - ExecutedTrades.Min(x => x.TradeDate)).TotalDays);
            }
        }

        public decimal TotalCommissionPaid(DateTime AsOf)
        {
            return ExecutedTrades.Sum(t => TradingEnvironment.Instance.CommissionCharged(t, true));
        }
    }

    /// <summary>
    /// Trade management
    /// </summary>
    public partial class Position
    {
        public void AddExecutedTrade(Trade trade)
        {

            if (trade.Security != Security)
                throw new InvalidTradeForPositionException() { message = "Trade/Position mismatch at execution" };

            if (trade.TradeStatus != TradeStatus.Executed)
                throw new InvalidTradeForPositionException() { message = "Must execute trade before adding to position" };

            // If this is the first trade added to this position, add and set position direction
            if (ExecutedTrades.Count == 0)
            {
                ExecutedTrades.Add(trade);
                PositionDirection = (PositionDirection)trade.TradeActionBuySell;
                return;
            }

            // Double-check that this isn't an already-closed position
            if (Size(trade.TradeDate) == 0)
                throw new InvalidTradeForPositionException() { message = "Invalid trade for position: Position is already closed" };

            // Calculate what the new size would be if the trade were applied
            var newPos = Size(trade.TradeDate) + trade.DirectionalQuantity;

            // If the new size is zero, or if the resulting position is still compliant with the position direction, allow trade
            if (newPos == 0 || (Math.Sign(newPos) == (int)PositionDirection))
            {
                ExecutedTrades.Add(trade);
                return;
            }

            // If the size is non-zero and the new size is opposite the position direction, reject
            if ((Math.Sign(newPos) != (int)PositionDirection))
            {
                throw new InvalidTradeForPositionException() { message = "Invalid trade for position: Trade would result in position direction change" };
            }

            throw new InvalidTradeForPositionException() { message = "Unknown trade error in position" };
        }
    }

    public partial class Position
    {
        /// <summary>
        /// Creates a 'deep' copy of the postion
        /// </summary>
        /// <returns></returns>
        public Position Copy()
        {
            var ret = new Position()
            {
                Security = Security,
                PositionDirection = PositionDirection
            };
            ExecutedTrades.ForEach(x => ret.ExecutedTrades.Add(x.Copy()));
            return ret;
        }
    }

    /// <summary>
    /// Logging and output formatting
    /// </summary>
    public partial class Position
    {
        /// <summary>
        /// Returns a single string of information on the position
        /// </summary>
        /// <returns></returns>
        public string ToString(DateTime AsOf)
        {
            string direction;
            switch (PositionDirection)
            {
                case PositionDirection.NotSet:
                    direction = "FLAT";
                    break;
                case PositionDirection.LongPosition:
                    direction = "LONG";
                    break;
                case PositionDirection.ShortPosition:
                    direction = "SHRT";
                    break;
                default:
                    direction = "ERR ";
                    break;
            }

            return string.Format($"Position {PositionId:0000}: {direction} {Size(AsOf)} {Security.Ticker} at {AverageCost(AsOf):$0.00} average cost.");
        }

        /// <summary>
        /// Returns an array of strings representing all trades in this position, chronologically
        /// </summary>
        /// <returns></returns>
        public List<string> ToStringTrades(DateTime AsOf)
        {
            var ret = new List<string>();
            ExecutedTrades.ForEach(t =>
            {
                if (t.TradeDate <= AsOf)
                    ret.Add(t.ToString());
            });
            return ret;
        }
    }
}
