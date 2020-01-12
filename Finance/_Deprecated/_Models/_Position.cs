using Finance;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Helpers;

namespace Finance.Models
{
    public partial class Position
    {
        [Key]
        public int PositionId { get; set; }

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
        public virtual List<Trade> Trades { get; set; } = new List<Trade>();

        // List of pending trades relating to this position
        public virtual List<Trade> Stops { get; set; } = new List<Trade>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="security"></param>
        /// <param name="IgnorePositionId"></param>
        public Position(Security security)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            PositionId = NextPositionId;
        }
    }

    /// <summary>
    /// Trade management
    /// </summary>
    public partial class Position
    {
        /// <summary>
        /// Adds a trade to the trade list if valid.  Updates any parameters as necessary
        /// </summary>
        /// <param name="trd"></param>
        /// <returns></returns>
        public void AddExecutedTrade(Trade trd)
        {

            // Make sure this is a valid, executed trade in this security
            if (trd.Security != Security ||
                trd.TradeStatus != TradeStatus.Executed ||
                trd.TradeActionBuySell == TradeActionBuySell.None ||
                trd.Quantity <= 0)
                throw new InvalidTradeForPositionException() { message = "Cannot add trade to position" };

            // If this is the first trade added to this position, add and set position direction
            if (Trades.Count == 0)
            {
                Trades.Add(trd);
                PositionDirection = (PositionDirection)trd.TradeActionBuySell;
                return;
            }

            // Double-check that this isn't an already-closed position
            if (Size(trd.TradeDate) == 0)
                throw new InvalidTradeForPositionException() { message = "Cannot trade in position, position is already closed" };

            // Calculate what the new size would be if the trade were applied
            var newPos = Size(trd.TradeDate) + trd.DirectionalQuantity;

            // If the new size is zero, or if the resulting position is still compliant with the position direction, allow trade
            if (newPos == 0 || (Math.Sign(newPos) == (int)PositionDirection))
            {
                Trades.Add(trd);
                return;
            }

            // If the size is non-zero and the new size is opposite the position direction, reject
            if ((Math.Sign(newPos) != (int)PositionDirection))
            {
                throw new InvalidTradeForPositionException() { message = "Invalid: Trade would result in position direction change" };
            }

            throw new InvalidTradeForPositionException() { message = "Unknown trade error in position" };

        }

        /// <summary>
        /// Updates stoploss if it would move it in the direction of the trade.  In other words, the 
        /// stoploss level should never decrease for long trades, and never increase for short trades.
        /// Trade should be marked as conditional
        /// </summary>
        /// <param name="trd"></param>
        /// <returns></returns>
        public void UpdateStoplossTrade(Trade trd)
        {
            // Incorrect trade type being added
            if (trd.TradeType != TradeType.Stop)
                throw new InvalidStoplossTradeException() { message = "Trade must be Stop type to add to position." };

            if (trd.TradeStatus != TradeStatus.Stoploss)
                throw new InvalidStoplossTradeException() { message = "Stoploss trade must be marked as conditional." };

            if (trd.Quantity != Size(trd.TradeDate))
                throw new InvalidStoplossTradeException() { message = "Stoploss quantity does not match position size" };

            // Should only be one active stop trade saved since we update each time
            var currentStoplossTrade = GetCurrentStoploss(trd.TradeDate);

            // If no stops are saved yet, add and return
            if (currentStoplossTrade == null)
            {
                Stops.Add(trd);
                return;
            }

            // Update the stop if it is moves in the direction of the trade vis a vis the old stop
            if ((trd.StopPrice - currentStoplossTrade.StopPrice) * Direction() > 0)
            {
                // Cancel the old trade
                currentStoplossTrade.TradeStatus = TradeStatus.Cancelled;
                // Add the new one
                Stops.Add(trd);
            }

            // Update the stop if the position size has changed
            if (Size(trd.TradeDate) > currentStoplossTrade.Quantity)
            {
                // Cancel the old trade
                currentStoplossTrade.TradeStatus = TradeStatus.Cancelled;
                // Add the new one
                Stops.Add(trd);
            }

        }

        /// <summary>
        /// Returns the valid stoploss trade as of date provided
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        private Trade GetCurrentStoploss(DateTime AsOf)
        {
            return (from trd in Stops
                    where trd.TradeStatus == TradeStatus.Stoploss && trd.TradeDate <= AsOf
                    select trd).SingleOrDefault();
        }

    }

    /// <summary>
    /// Current position values
    /// </summary>
    public partial class Position
    {
        /// <summary>
        /// Returns the first trade date
        /// </summary>
        /// <returns></returns>
        public DateTime DateOpened()
        {
            try
            {
                if (Trades.Count > 0)
                    return (from trd in Trades select trd.TradeDate).Min();

                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Is the position open or not
        /// </summary>
        public bool Open(DateTime AsOf)
        {
            try
            {
                // The position is open if the size AsOf is non-zero
                return (Size(AsOf) != 0 ? true : false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Size of the position (positive or negative)
        /// </summary>
        public int Size(DateTime AsOf)
        {
            try
            {
                // Size is calculated by taking the sum value of all trades up to and including the AsOf date
                IEnumerable<int> ret1 = (from t in Trades where t.TradeDate <= AsOf select (t.DirectionalQuantity));
                return ret1 == null ? 0 : ret1.Sum();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 1 (long) or -1 (short)
        /// </summary>
        public int Direction()
        {
            try
            {
                return (int)PositionDirection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Average cost of an open position
        /// </summary>
        public decimal AverageCost(DateTime AsOf)
        {
            try
            {
                if (!Open(AsOf))
                    return 0;

                // Get an ordered list of all trades up to and including the AsOf date
                var tradeList = Trades.Where(x => x.TradeStatus == TradeStatus.Executed && x.TradeDate <= AsOf).OrderBy(x => x.TradeDate).ToList();

                return tradeList.AverageCost(PositionDirection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns the CLOSE for associated security on a given day
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public decimal ClosingValuePerShare(DateTime AsOf)
        {
            return Security.GetPriceBar(AsOf, false).Close;
        }

        /// <summary>
        /// Returns the OPEN for associated security on a given day
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public decimal OpeningValuePerShare(DateTime AsOf)
        {
            return Security.GetPriceBar(AsOf, false).Open;
        }

        /// <summary>
        /// Returns the PNL as a multiple of ATR based on the ATR of the last reported bar and our current average cost
        /// </summary>
        /// <returns></returns>
        public decimal CurrentAtrPnL(DateTime AsOf, int period = 14, MovingAverage movingAverageType = MovingAverage.Simple, bool UseOpeningValue = false)
        {
            try
            {
                decimal RawPNLperShare = 0m;
                // Calculate the dollar change per share, adjusted to the direction of our trade (positive = profit, negative = loss)
                if (UseOpeningValue)
                    RawPNLperShare = ((Security.GetPriceBar(AsOf).Open - AverageCost(AsOf)) * Direction());
                else
                    RawPNLperShare = ((Security.GetPriceBar(AsOf).Close - AverageCost(AsOf)) * Direction());
                decimal LastATR = Security.GetPriceBar(AsOf).AverageTrueRange(period);

                // Divide the PnL per share by the last reported ATR and return
                return (RawPNLperShare / LastATR);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns the current net value (positive or negative) of the current position at the end of the day
        /// </summary>
        /// <returns></returns>
        public decimal CurrentPositionValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                switch (MarketValues)
                {
                    case TimeOfDay.NotSet:
                        throw new InvalidRequestValueException() { message = "MarketValues not set" };
                    case TimeOfDay.MarketOpen:
                        return (Size(AsOf) * Security.GetPriceBar(AsOf, false).Open);
                    case TimeOfDay.MarketEndOfDay:
                        return (Size(AsOf) * Security.GetPriceBar(AsOf, false).Close);
                    default:
                        throw new InvalidRequestValueException() { message = "MarketValues unknown value" };
                }
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
            try
            {
                return (from trade in Trades where trade.TradeDate <= AsOf select (trade.TotalCashImpact)).Sum();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Net $ PnL of the position
        /// </summary>
        /// <returns></returns>
        public decimal TotalUnrealizedPnL(DateTime AsOf, TimeOfDay MarketValues)
        {

            try
            {
                decimal RawPNLperShare = 0m;

                switch (MarketValues)
                {
                    case TimeOfDay.NotSet:
                        throw new InvalidRequestValueException() { message = "MarketValues not set" };
                    case TimeOfDay.MarketOpen:
                        RawPNLperShare = ((Security.GetPriceBar(AsOf, false).Open - AverageCost(AsOf)));
                        break;
                    case TimeOfDay.MarketEndOfDay:
                        RawPNLperShare = ((Security.GetPriceBar(AsOf, false).Close - AverageCost(AsOf)));
                        break;
                    default:
                        throw new InvalidRequestValueException() { message = "MarketValues unknown value" };
                }

                // Correct for long/short
                return (RawPNLperShare * Size(AsOf) * Direction());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Total PnL from executed trades
        /// </summary>
        /// <returns></returns>
        public decimal TotalRealizedPnL(DateTime AsOf)
        {
            try
            {
                // Realized PNL must be calculated on a per-share basis as trades are executed
                // Each trade which reduces the size of an open position contributes to realized PNL
                // The realized PNL is the execution price of the trade minus the average price of the shares liquidated in that trade

                // If this position has no closing trades, return 0
                if (Trades.Where(x => x.TradeActionBuySell.ToInt() != PositionDirection.ToInt()).ToList().Count == 0)
                    return 0m;

                // Get an ordered list of all trades up to and including the last position-reducing trade prior to the AsOf date
                // ie, if there are buys in a long position after our last sale, but before the AsOf date, we don't want that factored into the average buy price
                var tradeList = Trades.Where(x => x.TradeStatus == TradeStatus.Executed && x.TradeDate <= AsOf).OrderBy(x => x.TradeDate).ToList();

                // Remove all elements after the last trade in the direction of the position (ie, remove all trailing buys in a long position)
                while ((int)tradeList.Last().TradeActionBuySell == (int)PositionDirection)
                    tradeList.Remove(tradeList.Last());

                // Iterate through the trades and compute realized gains
                int sharesOpen = 0;
                decimal averageCost = 0;
                decimal realized = 0;

                // TODO: TestMethod for this stuff
                foreach (var trd in tradeList)
                {
                    // Buy trades increase position and update average price
                    // Sell trades reduce position and update realized PNL
                    if ((int)trd.TradeActionBuySell == (int)PositionDirection)
                    {
                        averageCost = ((averageCost * sharesOpen) + (trd.TotalCashImpactAbsolute)) / (sharesOpen += trd.Quantity);
                    }
                    else if ((int)trd.TradeActionBuySell == -(int)PositionDirection)
                    {
                        realized += ((int)PositionDirection * (trd.ExecutedPrice - averageCost)) * trd.Quantity;
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

        /// <summary>
        /// Returns total of all commission paid on trades for this position
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public decimal TotalCommissionPaid(IEnvironment environment, DateTime AsOf)
        {
            try
            {
                return Trades.Sum(t => environment.CommissionCharged(t, t.ApiTrade));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

    }

    /// <summary>
    /// Copy
    /// </summary>
    public partial class Position
    {

        /// <summary>
        /// Creates a 'deep' copy of the postion
        /// </summary>
        /// <returns></returns>
        public Position Copy()
        {
            var ret = new Position(Security)
            {
                PositionDirection = PositionDirection
            };

            Trades.ForEach(x => ret.Trades.Add(x.Copy()));
            Stops.ForEach(x => ret.Stops.Add(x.Copy()));

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

            return string.Format($"Position {PositionId:0000}: {direction} {Size(AsOf)} {Security.Ticker} at {AverageCost(AsOf):$0.00} avgerage cost, {ClosingValuePerShare(AsOf):$0.00} last close.");
        }

        /// <summary>
        /// Returns an array of strings representing all trades in this position, chronologically
        /// </summary>
        /// <returns></returns>
        public List<string> ToStringTrades(DateTime AsOf)
        {
            var ret = new List<string>();
            Trades.ForEach(t =>
            {
                if (t.TradeDate <= AsOf)
                    ret.Add(t.ToString());
            });
            return ret;
        }
    }
}

