using Finance.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Calendar;
using static Finance.Helpers;

namespace Finance
{
    // Refactored

    public abstract partial class RiskManagerBase
    {
        protected IEnvironment Environment { get; set; }
        protected Portfolio Portfolio { get; set; }

        private List<Trade> TradeQueueReference { get; set; }
        private TradeManager TradeManagerReference { get; set; }
        private List<TradeApprovalRuleBase> TradeApprovalRulePipeline { get; set; }

        public void Attach(Portfolio portfolio, TradeManager tradeManager)
        {
            //
            // Attach this RiskManager to a specific Portfolio and TradeManager
            //

            Portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
            TradeManagerReference = tradeManager ?? throw new ArgumentNullException(nameof(tradeManager));

            Environment = portfolio.Environment;
            TradeQueueReference = tradeManager.TradeQueue;

            Portfolio.OnRequestStopForNewPosition += (s, e) =>
            {
                //
                // Raised by the Portfolio when a new position is opened, signaling the need for a stoploss to be placed in the TradeQueue
                //

                // Check
                if (e.position.ExecutedTrades.Count != 1)
                    throw new UnknownErrorException();

                // Generate stop from RiskManager
                var stop = NewStoploss(e.position, e.AsOf);

                // Send stop to TradeManager
                TradeManagerReference.AddStoplossTrades(new Trade[] { stop }.ToList());
            };

            this.InitializeMe();
        }

        #region Initializers

        [Initializer]
        private void InitializeTradeApprovalPipeline()
        {
            TradeApprovalRulePipeline = new List<TradeApprovalRuleBase>();
            InsertTradeApprovalRules(TradeApprovalRulePipeline);
        }

        #endregion

        #region Signal Management & Trade Approval

        public void ProcessSignals(List<Signal> Signals, DateTime AsOf)
        {
            var ret = new ConcurrentBag<Trade>();

            Parallel.ForEach(Signals, signal =>
            {

                if (signal.SignalAction == TradeActionBuySell.None)
                    return;

                if (Portfolio.HasOpenPosition(signal.Security, AsOf))
                    return;

                var trade = new Trade(
                    signal.Security,
                    signal.SignalAction,
                    NewPositionSize(signal, AsOf),
                    TradeType.Limit,
                    NewTradeLimitPrice(signal.Security, signal.SignalAction, AsOf))
                {
                    TradeStatus = TradeStatus.Indicated,
                    TradePriority = TradePriority.NewPositionOpen
                };

                ret.Add(trade);
            });

            ProcessNewTrades(ret.ToList(), AsOf);
        }
        private void ProcessNewTrades(List<Trade> trades, DateTime AsOf)
        {
            // Sort trades by priority
            trades.Sort((x, y) => y.TradePriority.CompareTo(x.TradePriority));

            // Send to user-implemented trade approval pipeline
            ApproveTrades(trades, AsOf);

            trades.RemoveAll(trd => trd.TradeStatus == TradeStatus.Rejected);

            if (!trades.TrueForAll(trd => trd.TradeStatus == TradeStatus.Pending || trd.TradeStatus == TradeStatus.Stoploss))
                throw new UnknownErrorException();

            // Add passed trades to the TramdeManager queue
            TradeManagerReference.AddPendingTrades(trades);
        }
        protected void ApproveTrades(List<Trade> trades, DateTime AsOf)
        {

            // Copy of portfolio to execute trades into
            var portfolioCopy = Portfolio.Copy();

            foreach (Trade trade in trades)
            {
                switch (trade.TradeStatus)
                {
                    case TradeStatus.NotSet:
                    case TradeStatus.Pending:
                    case TradeStatus.Executed:
                    case TradeStatus.Cancelled:
                    case TradeStatus.Rejected:
                        // None of these should be here
                        throw new InvalidTradeOperationException() { message = "Unexpected trade type in approval pipeline" };
                    default:
                        break;
                }


                // See if the trade passes the rule
                if (TradeApprovalRulePipeline.TrueForAll(rule => rule.Run(trade, portfolioCopy, AsOf, TimeOfDay.MarketEndOfDay)))
                {
                    // Execute trade copy into portfolio copy and mark original as pending
                    var tradeCopy = trade.Copy();
                    tradeCopy.MarkExecuted(AsOf, tradeCopy.ExpectedExecutionPrice);
                    portfolioCopy.AddExecutedTrade(tradeCopy);
                    trade.TradeStatus = TradeStatus.Pending;
                }
                else
                {
                    // Reject       
                    trade.TradeStatus = TradeStatus.Rejected;
                }
            }

        }

        protected abstract void InsertTradeApprovalRules(List<TradeApprovalRuleBase> TradeApprovalRulePipeline);
        protected abstract int NewPositionSize(Signal IndicatedTrade, DateTime AsOf);
        protected abstract decimal NewTradeLimitPrice(Security security, TradeActionBuySell tradeAction, DateTime AsOf);

        #endregion
        #region Stoploss Management

        public void UpdateStoplosses(DateTime AsOf)
        {
            var ret = new List<Trade>();
            var openPositions = Portfolio.GetPositions(PositionStatus.Open, AsOf);
            var CurrentStops = TradeManagerReference.GetAllStoplosses(AsOf);

            foreach (Position position in openPositions)
            {
                Trade currentStop = CurrentStops.Where(x => x.Security == position.Security && x.TradeStatus == TradeStatus.Stoploss).SingleOrDefault();

                if (currentStop == null)
                {
                    ret.Add(NewStoploss(position, AsOf));
                }
                else
                {
                    var newStop = currentStop.Copy();
                    newStop.StopPrice = UpdatePositionStoplossPrice(position, currentStop, AsOf);
                    newStop.Quantity = Math.Abs(position.Size(AsOf));
                    newStop.TradeDate = AsOf;
                    ret.Add(newStop);
                    currentStop.TradeStatus = TradeStatus.Cancelled;
                }
            }

            TradeManagerReference.AddStoplossTrades(ret);
        }
        public Trade NewStoploss(Position position, DateTime AsOf)
        {
            var stop = _NewStoploss(position, AsOf);

            stop.TradePriority = TradePriority.StoplossImmediate;
            stop.TradeStatus = TradeStatus.Stoploss;

            // Test verify
            var stopCopy = stop.Copy();
            var portCopy = Portfolio.Copy();
            stopCopy.MarkExecuted(AsOf, stop.ExpectedExecutionPrice);
            portCopy.AddExecutedTrade(stopCopy);

            if (portCopy.GetPositions(PositionStatus.Open, AsOf).Exists(pos => pos.Security == position.Security))
                throw new InvalidTradeForPositionException() { message = "Stop is not valid" };

            return stop;

        }

        protected abstract Trade _NewStoploss(Position position, DateTime AsOf);
        protected abstract decimal UpdatePositionStoplossPrice(Position position, Trade currentStop, DateTime AsOf);

        #endregion
        #region Open Position Management

        public void ScalePositions(DateTime AsOf)
        {
            var ret = new List<Trade>();

            foreach (Position position in Portfolio.GetPositions(PositionStatus.Open, AsOf))
            {
                var trd = ScalePosition(position, AsOf);
                if (trd != null)
                    ret.Add(trd);
            }

            ProcessNewTrades(ret, AsOf);
        }

        protected abstract Trade ScalePosition(Position position, DateTime AsOf);

        #endregion
        #region General Risk Management

        public decimal PortfolioCoreEquity(DateTime AsOf, TimeOfDay timeOfDay)
        {
            return Portfolio.EquityWithLoanValue(AsOf, timeOfDay) - PortfolioRiskEquity(AsOf, timeOfDay);
        }
        public decimal PortfolioRiskEquity(DateTime AsOf, TimeOfDay timeOfDay)
        {
            decimal ret = 0m;
            var currentStops = TradeManagerReference.GetAllStoplosses(AsOf);

            foreach (Position position in Portfolio.GetPositions(PositionStatus.Open, AsOf))
            {
                // Value at risk = current per share price - current stoploss level * quantity (abs)
                decimal lastPx;
                switch (timeOfDay)
                {
                    case TimeOfDay.MarketOpen:
                        lastPx = position.Security.GetPriceBar(AsOf).Open;
                        break;
                    case TimeOfDay.MarketEndOfDay:
                        lastPx = position.Security.GetPriceBar(AsOf).Close;
                        break;
                    default:
                        throw new UnknownErrorException();
                }

                var stop = currentStops.Find(x => x.Security == position.Security);

                decimal valueAtRisk = Math.Abs((lastPx - stop.StopPrice) * position.Size(AsOf));
                ret += valueAtRisk;
            }

            return ret;
        }
        public decimal PortfolioRiskEquityPercent(DateTime AsOf, TimeOfDay timeOfDay)
        {
            return PortfolioRiskEquity(AsOf, timeOfDay) / Portfolio.EquityWithLoanValue(AsOf, timeOfDay);
        }

        #endregion

    }

    /// <summary>
    /// Adjustable parameters
    /// </summary>
    public partial class RiskManager : RiskManagerBase
    {

        #region Risk Management Parameters

        //
        // Runtime-adjustable parameters for controlling risk management
        //

        [TradeSystemParameterInt("Stoploss ATR Period", "Period to use when calculating ATR for stoploss", 14, 90, 1)]
        public int Stoploss_ATR_Period { get; set; } = 14;

        [TradeSystemParameterDecimal("Stoploss ATR Multiple", "Multiple of ATR used to calculate a stoploss level", 3.0, 15.0, 0.5)]
        public decimal Stoploss_ATR_Multiple { get; set; } = 8.0m;

        [TradeSystemParameterDecimal("Initial Position Risk", "Initial Percentage of Equity to risk on a new position", 0.01, 0.05, 0.005)]
        public decimal Initial_Position_Risk_Percentage { get; set; } = 0.02m;

        [TradeSystemParameterDecimal("Limit Price Tolerance", "Variance from Close price to set Limit price for new trades", 0.0, 0.05, 0.01)]
        public decimal Limit_Price_Tolerance_Percent { get; set; } = 0.01m;

        [TradeSystemParameterInt("Maximum Open Positions", "Maximum number of simultaneous open positions", 5, 30, 5)]
        public int Max_Open_Positions { get; set; } = 10;

        [TradeSystemParameterDecimal("Minimum Percent Available Funds", "Minimum percentage of ELV held as available funds", 0.0, .50, .05)]
        public decimal Min_Available_Funds_Percent { get; set; } = .05m;

        [TradeSystemParameterDecimal("Position Scaling Trigger", "Percentage of Position Value held as Unrealized PNL which triggers a position size increase", .10, .50, .05)]
        public decimal Position_Scaling_Trigger { get; set; } = .15m;

        [TradeSystemParameterDecimal("Position Scaling Percentage", "Percentage to increase positions when Unrealized PNL exceeds trigger", 0.1, .50, .05)]
        public decimal Position_Scaling_Percent { get; set; } = .75m;

        #endregion

        public RiskManager() : base()
        {
        }
        public RiskManager Copy()
        {
            var ret = new RiskManager();

            // Copy all values which are marked with a ParameterAttribute tag
            foreach (var prop in GetType().GetProperties())
            {
                if (Attribute.IsDefined(prop, typeof(ParameterAttribute)))
                {
                    prop.SetValue(ret, prop.GetValue(this));
                }
            }

            return ret;

        }

        #region Signal Management & Trade Approval

        protected override void InsertTradeApprovalRules(List<TradeApprovalRuleBase> TradeApprovalRulePipeline)
        {
            //
            // Add each rule which will be executed to approve new trades
            //

            TradeApprovalRulePipeline.Add(new TradeApprovalRule_1());
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_2());
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_3());
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_4());
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_5(Max_Open_Positions));
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_6(Min_Available_Funds_Percent));
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_7(Stoploss_ATR_Period, Stoploss_ATR_Multiple));
        }
        protected override int NewPositionSize(Signal IndicatedTrade, DateTime AsOf)
        {
            var riskDollarsTotal = Portfolio.EquityWithLoanValue(AsOf, TimeOfDay.MarketEndOfDay) * Initial_Position_Risk_Percentage;
            var securityLastATR = IndicatedTrade.Security.GetPriceBar(AsOf).AverageTrueRange(Stoploss_ATR_Period);
            var riskDollarsPerShare = Stoploss_ATR_Multiple * securityLastATR;

            int positionSize = Convert.ToInt32(Math.Round((riskDollarsTotal / riskDollarsPerShare), 0));

            return positionSize;
        }
        protected override decimal NewTradeLimitPrice(Security security, TradeActionBuySell tradeAction, DateTime AsOf)
        {
            var close = security.GetPriceBar(AsOf).Close;
            switch (tradeAction)
            {
                case TradeActionBuySell.None:
                    throw new UnknownErrorException();
                case TradeActionBuySell.Buy:
                    return (close * (1 + Limit_Price_Tolerance_Percent));
                case TradeActionBuySell.Sell:
                    return (close * (1 - Limit_Price_Tolerance_Percent));
                default:
                    throw new UnknownErrorException();
            }
        }

        #endregion
        #region Stoploss Management

        protected override Trade _NewStoploss(Position position, DateTime AsOf)
        {
            //
            // Implementation of stoploss calculation
            //

            if (position.ExecutedTrades.Count > 1)
                throw new UnknownErrorException();

            var entryPrice = position.AverageCost(AsOf);

            // Get the last ATR (prior day's)
            var ATR = position.Security.GetPriceBar(Calendar.PriorTradingDay(AsOf)).AverageTrueRange(Stoploss_ATR_Period);

            decimal stoplossSpan = (ATR * Stoploss_ATR_Multiple);

            var stop = new Trade(
                position.Security,
                (TradeActionBuySell)(-position.PositionDirection.ToInt()),
                Math.Abs(position.Size(AsOf)),
                TradeType.Stop,
                0, (entryPrice - (position.PositionDirection.ToInt() * stoplossSpan)))
            {
                TradeDate = AsOf
            };

            return stop;
        }

        protected override decimal UpdatePositionStoplossPrice(Position position, Trade currentStop, DateTime AsOf)
        {
            //
            // Should only be executed at day's end; only new stoplosses are generated in the morning.  Always use EOD values in functions
            //

            // TODO: Modify to allow user to select different stops
            return TrailingStop(position, currentStop, AsOf);

        }

        protected decimal TrailingStop(Position position, Trade currentStop, DateTime AsOf)
        {
            //
            // Simple trailing stop
            //

            // Get the last ATR
            var lastClose = position.Security.GetPriceBar(AsOf).Close;
            var ATR = position.Security.GetPriceBar(AsOf).AverageTrueRange(Stoploss_ATR_Period);

            // Calculate the difference to the stoploss level
            decimal stoplossSpan = (ATR * Stoploss_ATR_Multiple);

            // Calculate the stop price
            var stopPrice = (lastClose - (position.PositionDirection.ToInt() * stoplossSpan));

            switch (position.PositionDirection)
            {
                case PositionDirection.NotSet:
                    throw new UnknownErrorException();
                case PositionDirection.LongPosition:
                    return Math.Max(stopPrice, currentStop.StopPrice);
                case PositionDirection.ShortPosition:
                    return Math.Min(stopPrice, currentStop.StopPrice);
                default:
                    throw new UnknownErrorException();
            }
        }
        protected decimal TimeReducingTrailingStop(Position position, Trade currentStop, DateTime AsOf)
        {
            //
            // Trailing stop which gradually tightens over time rather than just price movement
            //
            throw new NotImplementedException();
        }

        #endregion
        #region Open Position Management

        protected override Trade ScalePosition(Position position, DateTime AsOf)
        {
            //
            // If a position meets criteria, generate a new trade to increase the position size
            //

            if (position.TotalUnrealizedPnL(AsOf, TimeOfDay.MarketEndOfDay) / position.GrossPositionValue(AsOf, TimeOfDay.MarketEndOfDay) > Position_Scaling_Trigger)
            {
                var trade = new Trade(position.Security,
                    (TradeActionBuySell)position.PositionDirection.ToInt(),
                    Convert.ToInt32(position.Size(AsOf) * Position_Scaling_Percent),
                    TradeType.Limit,
                    NewTradeLimitPrice(position.Security, (TradeActionBuySell)position.PositionDirection.ToInt(), AsOf))
                {
                    TradePriority = TradePriority.ExistingPositionIncrease,
                    TradeStatus = TradeStatus.Indicated,
                    TradeDate = AsOf
                };

                return trade;
            }
            return null;
        }

        #endregion
        #region General Risk Management


        #endregion

    }
}
