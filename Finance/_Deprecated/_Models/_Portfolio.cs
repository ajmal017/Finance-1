using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Finance.Calendar;
using static Finance.Helpers;

namespace Finance.Models
{

    /// <summary>
    /// Defines the data members of the Portfolio class
    /// </summary>
    public partial class Portfolio
    {

        public string PortfolioName { get; set; } = "Default Portfolio 1";

        // List of all positions (opened and closed) in the portfolio
        public virtual List<Position> Positions { get; set; }
        // List of pending trades (either resting or active; stops, new trades, etc)
        public virtual List<Trade> PendingTrades { get; set; }

        // The trading environment (broker)
        public IEnvironment Environment { get; }

        // Trading strategy values to employ on this portfolio
        public Strategy Strategy { get; }

        // Setup values
        public PortfolioSetup PortfolioSetup { get; }

        public PortfolioDirection PortfolioDirection => PortfolioSetup.PortfolioDirection;
        public PortfolioMarginType PortfolioMarginType => PortfolioSetup.PortfolioMarginType;
        public decimal InitialCashBalance => PortfolioSetup.InitialCashBalance;

        public Portfolio(IEnvironment environment, PortfolioSetup portfolioSetup, Strategy strategy)
        {
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            PortfolioSetup = portfolioSetup ?? throw new ArgumentNullException(nameof(portfolioSetup));
            Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

            Positions = new List<Position>();
            PendingTrades = new List<Trade>();

            // Set initial value in PriorSMA for portfolio inception date
            PriorSmaValues.Add(Calendar.PriorTradingDay(portfolioSetup.InceptionDate), portfolioSetup.InitialCashBalance);
        }
    }

    /// <summary>
    /// Trading
    /// </summary>
    public partial class Portfolio
    {

        /// <summary>
        /// Returns a list of trades which are ready to be reviewed and executed
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public List<Trade> GetAllPendingTrades(DateTime AsOf)
        {
            List<Trade> ret = (from trd in PendingTrades
                               where trd.TradeStatus == TradeStatus.Pending
                               && trd.TradeDate == AsOf
                               select trd).ToList();

            if (ret.Count == 0)
                return null;

            // Sort by descending priority
            ret.Sort((x, y) => y.TradePriority.CompareTo(x.TradePriority));

            return ret;
        }

        /// <summary>
        /// Adds a trade to either the appropriate position if executed, or the pending trade list.
        /// Assumes trade has been previously validated
        /// </summary>
        /// <param name="trade"></param>
        /// <returns>True if successfully added</returns>
        public bool AddTrade(Trade trade)
        {
            switch (trade.TradeStatus)
            {
                case TradeStatus.Executed:
                    return GetPosition(trade.Security, trade.TradeDate, true).AddExecutedTrade(trade);
                case TradeStatus.Pending:
                case TradeStatus.Stoploss:
                    PendingTrades.Add(trade);
                    return true;
                case TradeStatus.Cancelled:
                case TradeStatus.NotSet:
                case TradeStatus.Rejected:
                    return false;
                default:
                    return false;
            }

        }

        /// <summary>
        /// Returns all trades newly indicated by the trading strategy system for consideration
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public List<Trade> GetIndicatedTrades(DateTime AsOf)
        {
            try
            {
                return (PendingTrades.Where(x => x.TradeDate == AsOf &&
                x.TradeStatus == TradeStatus.Indicated)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns all trades newly indicated by the trading strategy system for consideration
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public List<Trade> GetPendingTrades(DateTime AsOf)
        {
            try
            {
                return (PendingTrades.Where(x => x.TradeDate == AsOf &&
                x.TradeStatus == TradeStatus.Pending)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Executes all standing stoploss trades that have been triggered
        /// </summary>
        /// <param name="AsOf"></param>
        /// <param name="UseOpeningValue">If True, executes trades which have been triggered only by a Security's opening value</param>
        public void ExecuteStoplossTrades(DateTime AsOf, bool UseOpeningValue = false)
        {
            foreach (Position pos in GetAllPositions(AsOf))
            {
                var bar = pos.Security.GetPriceBar(AsOf, false);
                var stop = pos.GetCurrentStoploss(AsOf);
                // Execute the trade based only on the security opening price for this date (gap open scenario)
                if (UseOpeningValue)
                {
                    // If open is below stoploss for a long or above stoploss for a short, execute at the open price            
                    var dif = (bar.Open - stop.StopPrice) * pos.Direction();

                    if (dif <= 0)
                    {
                        stop.TradeStatus = TradeStatus.Pending;
                        stop.Execute(this, Environment.SlippageAdjustedPrice(bar.Open, stop.TradeActionBuySell), AsOf, stop.Quantity);
                    }
                }
                // Execute the trade if the stoploss would have been triggered by the price range today (EOD)
                else
                {
                    // If low is below stoploss for a long or high is above stoploss for a short, execute at the stop price
                    var difLow = (bar.Low - stop.StopPrice) * pos.Direction();
                    if (difLow <= 0)
                    {
                        stop.TradeStatus = TradeStatus.Pending;
                        stop.Execute(this, Environment.SlippageAdjustedPrice(stop.StopPrice, stop.TradeActionBuySell), AsOf, stop.Quantity);
                    }

                    var difHigh = (bar.High - stop.StopPrice) * pos.Direction();
                    if (difHigh <= 0)
                    {
                        stop.TradeStatus = TradeStatus.Pending;
                        stop.Execute(this, Environment.SlippageAdjustedPrice(stop.StopPrice, stop.TradeActionBuySell), AsOf, stop.Quantity);
                    }
                }
            }
        }

    }

    /// <summary>
    /// Position management
    /// </summary>
    public partial class Portfolio
    {

        /// <summary>
        /// Checks to see if an open position exists in a given security
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public bool HasPosition(Security security, DateTime AsOf)
        {
            try
            {
                return Positions.Any(x => x.Security == security && x.Open(AsOf));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Returns current open position for a security or creates a new position
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public Position GetPosition(Security security, DateTime AsOf, bool create = false)
        {
            try
            {
                return Positions.Find(x => x.Security == security && x.Open(AsOf)) ?? (create ? Positions.AddAndReturn(new Position(security)) : null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns a list of all currently open positions
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public List<Position> GetAllPositions(DateTime AsOf, PositionStatus positionStatus)
        {
            try
            {
                switch (positionStatus)
                {
                    case PositionStatus.Closed:
                        return Positions.Where(x => !x.Open(AsOf)).ToList();
                    case PositionStatus.Open:
                        return Positions.Where(x => x.Open(AsOf)).ToList();
                    default:
                        throw new InvalidRequestValueException() { message = "positionStatus not set" };
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Executes Position Management rules to ensure the portfolio remains within margin and liquidity paramenters
        /// as outlined in the IEnvironment object
        /// </summary>
        /// <param name="AsOf"></param>
        /// <param name="UseOpeningValue"></param>
        public void ExecutePositionManagement(DateTime AsOf, bool UseOpeningValue)
        {
            // Executes position management rules on portfolio.  Any trades required to bring the portfolio into compliance
            // are placed in the pending trade queue with priority Immediate
            Environment.PositionManagementRulesPipeline.Run(this, AsOf, UseOpeningValue);

        }

        /// <summary>
        /// Processes all trades in the PendingTrades queue which have a pending status
        /// </summary>
        /// <param name="AsOf"></param>
        public void ProcessPendingTrades(DateTime AsOf)
        {

            // Run execution rules on all trades in the Pending queue.  Trades will be marked Pending if approved
            Environment.TradeExecutionApprovalRulesPipeline.Run(this, AsOf);

            // Process each Pending trade in order of descending priority
            GetAllPendingTrades(AsOf)?.ForEach(trd =>
            {
                // Price bar for the execution date
                var bar = trd.Security.GetPriceBar(AsOf, false);

                if (AsOf.IsBetween(new DateTime(2016, 9, 1), new DateTime(2016, 9, 13)))
                {
                    var i = 0;
                }

                // Trades not requiring approval should execute first
                switch (trd.TradePriority)
                {
                    case TradePriority.NotSet:
                    case TradePriority.NewPositionOpen:
                        {
                            // Open a new position and generate a new stoploss trade
                            if (trd.LimitPriceWillExecute(AsOf))
                            {
                                if (GetPosition(trd.Security, AsOf, false) != null)
                                    throw new InvalidTradeOperationException()
                                    {
                                        message = "Trade marked as New Position when postion already exists"
                                    };

                                // Price action during the day will cause this limit trade to execute at or around the limit price
                                var slippageAdjustedExecutionPrice = Environment.SlippageAdjustedPrice(trd.LimitPriceExecuted(AsOf), trd.TradeActionBuySell);
                                trd.Execute(this, slippageAdjustedExecutionPrice, AsOf, trd.Quantity);

                                // Assign a stoploss to the new position
                                var pos = GetPosition(trd.Security, AsOf, false);
                                pos.UpdateStoplossTrade(Strategy.StoplossTrade(pos, AsOf, true));
                            }
                            else
                            {
                                // Cancel trade at end of day
                                trd.TradeStatus = TradeStatus.Cancelled;
                            }
                            break;
                        }
                    case TradePriority.ExistingPositionIncrease:
                        {
                            // Make sure this position hasn't closed out today
                            if (GetPosition(trd.Security, AsOf, false) == null)
                            {
                                trd.TradeStatus = TradeStatus.Cancelled;
                            }
                            // Price action during the day will cause this limit trade to execute at or around the limit price
                            if (trd.LimitPriceWillExecute(AsOf))
                            {

                                var slippageAdjustedExecutionPrice = Environment.SlippageAdjustedPrice(trd.LimitPriceExecuted(AsOf), trd.TradeActionBuySell);
                                trd.Execute(this, slippageAdjustedExecutionPrice, AsOf, trd.Quantity);

                                // Updatestoploss on existing position
                                var pos = GetPosition(trd.Security, AsOf, false);
                                pos.UpdateStoplossTrade(Strategy.StoplossTrade(pos, AsOf, false));
                            }
                            else
                            {
                                // Cancel at end of date
                                trd.TradeStatus = TradeStatus.Cancelled;
                            }
                            break;
                        }
                    case TradePriority.ExistingPositionDecrease:
                        {
                            throw new NotImplementedException();
                        }
                    case TradePriority.PositionClose:
                        {
                            throw new NotImplementedException();
                        }
                    case TradePriority.StoplossImmediate:
                        {
                            // These shouldn't be handled here
                            throw new NotImplementedException();
                        }
                    default:
                        throw new InvalidTradeOperationException() { message = "Unknown error in Trade Execution module" };
                }
            });

        }

        /// <summary>
        /// Execute the new trade strategy contained in Strategy and add indicated trades to pending trade queue
        /// </summary>
        /// <param name="AsOf"></param>
        public void ExecuteTradeGenerationStrategy(List<Security> Securities, DateTime AsOf)
        {
            var trds = (from sec in Securities select Strategy.EntryTrade(sec, this, AsOf)).ToList();

            trds.RemoveAll(x => x == null);
            trds.RemoveAll(x => HasPosition(x.Security, AsOf));

            if (trds.Count > 0)
            {
                // Send trades to queue
                PendingTrades.AddRange(trds);
            }
        }

        /// <summary>
        /// Pre-approve all trades int he pending trade queue
        /// </summary>
        /// <param name="AsOf"></param>
        public void ExecuteTradePreApproval(DateTime AsOf)
        {
            Environment.PreTradeApprovalRulesPipeline.Run(this, AsOf);
        }

    }

}
