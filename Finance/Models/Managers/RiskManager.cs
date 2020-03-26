using Finance.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance.PositioningStrategies;
using System.Configuration;
using static Finance.Calendar;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance
{
    public class RiskManager
    {
        #region Events

        public event EventHandler PositionSizingStrategyChanged;
        private void OnPositionSizingStrategyChanged()
        {
            PositionSizingStrategyChanged?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Risk Management Parameters

        [SettingsCategory(SettingsType.RiskParameters, typeof(decimal))]
        [SettingsDescription("Initial Position Risk")]
        public decimal Initial_Position_Risk_Percentage { get; set; } = 0.02m;

        [SettingsCategory(SettingsType.RiskParameters, typeof(decimal))]
        [SettingsDescription("Limit Price Slippage Tolerance")]
        public decimal Limit_Price_Tolerance_Percent { get; set; } = 0.01m;

        [SettingsCategory(SettingsType.RiskParameters, typeof(int))]
        [SettingsDescription("Max # Open Positions")]
        public int Max_Open_Positions
        {
            get => _Max_Open_Positions;
            set
            {
                // Need to modify trade rule initialized with this parameter
                _Max_Open_Positions = value;
                var rule = GetRuleByName("MaxOpenPositions");
                if (rule != null && rule is TradeApprovalRule_5 rule_5)
                {
                    rule_5.UpdateParameter(value);
                }
            }
        }
        private int _Max_Open_Positions { get; set; } = 25;

        [SettingsCategory(SettingsType.RiskParameters, typeof(decimal))]
        [SettingsDescription("Minimum Available Funds Percent")]
        public decimal Min_Available_Funds_Percent { get; set; } = .05m;

        //[SettingsCategory(SettingsType.RiskParameters, typeof(bool))]
        //[SettingsDescription("Position Scaling On")]
        //public bool Position_Scaling_OnOff { get; set; } = false;

        //[SettingsCategory(SettingsType.RiskParameters, typeof(decimal))]
        //[SettingsDescription("Position Change Scale Trigger")]
        //public decimal Position_Scaling_Trigger { get; set; } = .15m;

        //[SettingsCategory(SettingsType.RiskParameters, typeof(decimal))]
        //[SettingsDescription("Position Scaling Percent")]
        //public decimal Position_Scaling_Percent { get; set; } = .25m;

        #endregion
        #region Security Filter Parameters

        [SettingsCategory(SettingsType.SecurityFilterParameters, typeof(Currency))]
        [SettingsDescription("Minimum Share Price for Signal Generation")]
        public decimal Minimum_Security_Price { get; set; } = 5.00m;

        [SettingsCategory(SettingsType.SecurityFilterParameters, typeof(Currency))]
        [SettingsDescription("Maximum Share Price for Signal Generation")]
        public decimal Maximum_Security_Price { get; set; } = 250.00m;

        [SettingsCategory(SettingsType.SecurityFilterParameters, typeof(int))]
        [SettingsDescription("Minimum Average 30-Day Volume (mm)")]
        public double Minimum_Average_Volume { get; set; } = 25000;

        public List<string> Included_Sectors_Source_List => Settings.Instance.MarketSectors;
        private List<string> _Included_Sectors { get; set; }
        [SettingsCategory(SettingsType.SecurityFilterParameters, typeof(OptionList))]
        [SettingsDescription("Sectors")]
        [DefaultSettingValue("Included_Sectors_Source_List")]
        public List<string> Included_Sectors
        {
            get
            {
                if (_Included_Sectors == null)
                    _Included_Sectors = Settings.Instance.MarketSectors;
                return _Included_Sectors;
            }
            set
            {
                _Included_Sectors = value;
            }
        }

        #endregion

        protected Portfolio Portfolio { get; set; }

        private List<Trade> TradeQueueReference { get; set; }
        private TradeManager TradeManagerReference { get; set; }
        private List<TradeApprovalRuleBase> TradeApprovalRulePipeline { get; set; }

        private List<Security> SecurityUniverse => RefDataManager.Instance.GetAllSecurities().Where(x => !x.Excluded).ToList();

        // Implement this at the simulation manager level like we do Strategies (seperate selector)
        public List<PositioningStrategyBase> AllPositioningAndStoplossMethods { get; set; }
        public PositioningStrategyBase ActivePositioningStrategy { get; set; }
        public void SetPositioningAndStoplossMethod(PositioningStrategyBase positioningStrategy)
        {
            // Set by name to ensure that the ActiveStrategy is referencing local object
            ActivePositioningStrategy = AllPositioningAndStoplossMethods.Find(x => x.Name == positioningStrategy.Name);
            OnPositionSizingStrategyChanged();
        }

        public void Attach(Portfolio portfolio, TradeManager tradeManager)
        {
            //
            // Attach this RiskManager to a specific Portfolio and TradeManager
            //

            Portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
            TradeManagerReference = tradeManager ?? throw new ArgumentNullException(nameof(tradeManager));

            TradeQueueReference = tradeManager.TradeQueue;

            Portfolio.RequestForNewStop += (s, e) =>
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

        private RiskManager()
        {
        }
        public static RiskManager Default()
        {
            var ret = new RiskManager();
            ret.AllPositioningAndStoplossMethods = Helpers.AllPositioningStrategies();
            ret.SetPositioningAndStoplossMethod(ret.AllPositioningAndStoplossMethods.FirstOrDefault());
            return ret;
        }
        public RiskManager Copy()
        {
            var ret = new RiskManager();

            // Copy all values which are marked with a settings tag
            foreach (var prop in GetType().GetProperties())
            {
                if (Attribute.IsDefined(prop, typeof(SettingsCategoryAttribute)))
                {
                    prop.SetValue(ret, prop.GetValue(this));
                }
            }

            ret.AllPositioningAndStoplossMethods = new List<PositioningStrategyBase>();
            foreach (var strategy in this.AllPositioningAndStoplossMethods)
            {
                ret.AllPositioningAndStoplossMethods.Add(strategy.Copy());
            }
            ret.SetPositioningAndStoplossMethod(this.ActivePositioningStrategy);

            return ret;
        }

        #region Initializers

        [Initializer]
        private void InitializeTradeApprovalPipeline()
        {
            TradeApprovalRulePipeline = new List<TradeApprovalRuleBase>();
            PopulateTradeApprovalRules(TradeApprovalRulePipeline);
        }
        protected void PopulateTradeApprovalRules(List<TradeApprovalRuleBase> TradeApprovalRulePipeline)
        {
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_0("NonZeroTradeSize"));
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_1("MinAccountEquity"));
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_2("MinAvailableFunds"));
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_3("MaxGrossPosValue"));
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_4("NonZeroSMA"));
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_5("MaxOpenPositions", Max_Open_Positions));
            TradeApprovalRulePipeline.Add(new TradeApprovalRule_6("MinAvailFundsPercent", Min_Available_Funds_Percent));
        }

        protected TradeApprovalRuleBase GetRuleByName(string name)
        {
            if (TradeApprovalRulePipeline == null)
                return null;

            if (TradeApprovalRulePipeline.Exists(x => x.Name == name))
                return TradeApprovalRulePipeline.Find(x => x.Name == name);

            return null;
        }

        #endregion

        #region Signal Management & Trade Approval

        public List<Security> GetSecurityUniverse()
        {
            return ApplyPreFilters(SecurityUniverse);
        }

        /// <summary>
        /// Applies filters to security universe before generating signals
        /// </summary>
        /// <param name="securities"></param>
        /// <returns></returns>
        protected List<Security> ApplyPreFilters(List<Security> securities)
        {
            var ret = new List<Security>(securities);
            ret.RemoveAll(x => !Included_Sectors.Contains(x.Sector));
            return ret;
        }

        /// <summary>
        /// Applies filters to securities just prior to generating a new trade
        /// </summary>
        /// <param name="security"></param>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        protected bool ApplyTimeOfSignalFilters(Security security, DateTime AsOf)
        {
            PriceBar bar = security.GetPriceBar(AsOf, PriceBarSize.Daily);

            bool ret =
                bar.Close >= Minimum_Security_Price &
                bar.Close <= Maximum_Security_Price &
                security.AverageVolume(AsOf, PriceBarSize.Daily, 30) >= Minimum_Average_Volume;

            return ret;
        }

        public void ProcessSignals(List<Signal> Signals, DateTime AsOf)
        {
            var ret = new ConcurrentBag<Trade>();

            foreach (var signal in Signals)
            {
                //
                // No Signal
                //
                if (signal.SignalAction == SignalAction.None)
                    continue;

                //
                // Close open positions
                //
                if (signal.SignalAction == SignalAction.CloseIfOpen)
                {
                    if (Portfolio.HasOpenPosition(signal.Security, AsOf))
                    {
                        Position openPosition = Portfolio.GetPosition(signal.Security, AsOf);
                        // Generate a trade to close the current position

                        // Process as potential trade for approval
                        var closeTrade = new Trade(
                            signal.Security,
                            (TradeActionBuySell)(-openPosition.PositionDirection.ToInt()),
                            Math.Abs(openPosition.Size(AsOf)),
                            TradeType.Market, 0, 0, signal.Security.GetPriceBar(AsOf, signal.SignalBarSize).Close)
                        {
                            TradeStatus = TradeStatus.Indicated,
                            TradePriority = TradePriority.PositionClose
                        };

                        ret.Add(closeTrade);
                    }
                    continue;
                }

                //
                // Buy or Sell which is opposite current position (close position)
                //
                if (Portfolio.HasOpenPosition(signal.Security, AsOf) && (signal.SignalAction == SignalAction.Buy || signal.SignalAction == SignalAction.Sell))
                {
                    Position openPosition = Portfolio.GetPosition(signal.Security, AsOf);
                    int sizetest = openPosition.Size(AsOf);

                    // If the signal is indicating in the direction of the open position, ignore
                    if (openPosition.PositionDirection.ToInt() == signal.SignalAction.ToInt())
                        continue;

                    // If the signal is opposite the current position, generate a trade to close the current position

                    // Process as potential trade for approval
                    var closeTrade = new Trade(
                        signal.Security,
                        (TradeActionBuySell)(-openPosition.PositionDirection.ToInt()),
                        Math.Abs(openPosition.Size(AsOf)),
                        TradeType.Market, 0, 0, signal.Security.GetPriceBar(AsOf, signal.SignalBarSize).Close)
                    {
                        TradeStatus = TradeStatus.Indicated,
                        TradePriority = TradePriority.PositionClose
                    };

                    ret.Add(closeTrade);
                    continue;
                }

                //
                // Validate that the portfoio can accept direction of trade
                //
                switch (Portfolio.PortfolioSetup.PortfolioDirection)
                {
                    case PortfolioDirection.ShortOnly:
                        if (signal.SignalAction == SignalAction.Buy)
                            continue;
                        break;
                    case PortfolioDirection.LongOnly:
                        if (signal.SignalAction == SignalAction.Sell)
                            continue;
                        break;
                }

                //
                // Apply security filters (this might be better off in the trade manager but technically speaking belongs here
                //
                if (!ApplyTimeOfSignalFilters(signal.Security, AsOf))
                    continue;

                //
                // Process as potential trade for approval
                //
                try
                {
                    var trade = new Trade(
                        signal.Security,
                        (TradeActionBuySell)signal.SignalAction.ToInt(),
                        NewPositionSize(signal, AsOf),
                        TradeType.Limit,
                        NewTradeLimitPrice(signal.Security, (TradeActionBuySell)signal.SignalAction.ToInt(), AsOf))
                    {
                        TradeStatus = TradeStatus.Indicated,
                        TradePriority = TradePriority.NewPositionOpen,
                        TradePriorityScore = signal.SignalStrength
                    };

                    ret.Add(trade);
                }
                catch (CancelTradeException ex)
                {
                    Log(new LogMessage("Signal Processing", ex.ToString(), LogMessageType.SecurityError));
                    continue;
                }
            }

            ProcessNewTrades(ret.ToList(), AsOf);
        }
        private void ProcessNewTrades(List<Trade> trades, DateTime AsOf)
        {
            // Sort trades by priority
            //trades.Sort((x, y) => y.TradePriority.CompareTo(x.TradePriority));

            trades = trades.OrderByDescending(x => x.TradePriority).ThenByDescending(x => x.TradePriorityScore).ToList();

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


                //
                // Trades to close a position are automatically approved
                //
                if (trade.TradePriority == TradePriority.ExistingPositionDecrease ||
                    trade.TradePriority == TradePriority.PositionClose ||
                    trade.TradePriority == TradePriority.StoplossImmediate)
                {
                    trade.TradeStatus = TradeStatus.Pending;
                }
                //
                // All other trades are processed through the rules pipeline
                //
                else if (TradeApprovalRulePipeline.TrueForAll(rule => rule.Run(trade, portfolioCopy, AsOf, TimeOfDay.MarketEndOfDay)))
                {
                    // Execute trade copy into portfolio copy and mark original as pending
                    var tradeCopy = trade.Copy();
                    tradeCopy.MarkExecuted(AsOf, tradeCopy.ExpectedExecutionPrice);
                    portfolioCopy.AddExecutedTrade(tradeCopy);
                    trade.TradeStatus = TradeStatus.Pending;
                }
                else
                {
                    // Reject trades which do not pass any one rule
                    trade.TradeStatus = TradeStatus.Rejected;
                }
            }

        }

        protected int NewPositionSize(Signal IndicatedTrade, DateTime AsOf)
        {
            return ActivePositioningStrategy.NewPositionSize(Portfolio, IndicatedTrade, AsOf, Initial_Position_Risk_Percentage);
        }
        protected decimal NewTradeLimitPrice(Security security, TradeActionBuySell tradeAction, DateTime AsOf)
        {
            var close = security.GetPriceBar(AsOf, PriceBarSize.Daily).Close;
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

        public void UpdateStoplosses(DateTime AsOf)
        {
            var ret = new List<Trade>();
            var openPositions = Portfolio.GetPositions(PositionStatus.Open, AsOf);
            var currentStops = TradeManagerReference.GetAllStoplosses(AsOf);

            foreach (Position position in openPositions)
            {
                Trade currentStop = currentStops.Where(x => x.Security == position.Security && x.TradeStatus == TradeStatus.Stoploss).SingleOrDefault();

                if (currentStop == null)
                {
                    ret.Add(NewStoploss(position, AsOf));
                }
                else
                {
                    var newStop = currentStop.Copy();
                    newStop.StopPrice = ActivePositioningStrategy.UpdatePositionStoplossPrice(position, currentStop, AsOf);
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
            Trade stop = ActivePositioningStrategy.NewStoploss(position, AsOf);
            stop.TradePriority = TradePriority.StoplossImmediate;
            stop.TradeStatus = TradeStatus.Stoploss;

            if (!ValidStoploss(position, stop, AsOf))
                throw new InvalidStoplossTradeException() { message = "Stoploss is invalid" };

            // Verify the stop by executing into test portfolio and verifying zero balance
            var stopCopy = stop.Copy();
            var portCopy = Portfolio.Copy();
            stopCopy.MarkExecuted(AsOf, stop.ExpectedExecutionPrice);
            portCopy.AddExecutedTrade(stopCopy);

            if (portCopy.GetPositions(PositionStatus.Open, AsOf).Exists(pos => pos.Security == position.Security))
                throw new InvalidTradeForPositionException() { message = "Stop is not valid" };

            return stop;
        }

        private bool ValidStoploss(Position position, Trade stoploss, DateTime AsOf)
        {
            // Make sure the stoploss is not below the current price for a short or above for a long
            PriceBar lastBar = position.Security.GetPriceBar(AsOf, PriceBarSize.Daily);

            if (stoploss.TradeType != TradeType.Stop || stoploss.TradePriority != TradePriority.StoplossImmediate)
                return false;

            switch (position.PositionDirection)
            {
                case PositionDirection.LongPosition:
                    {
                        if (stoploss.TradeActionBuySell != TradeActionBuySell.Sell)
                            return false;
                        if (stoploss.ExpectedExecutionPrice >= lastBar.Close)
                            return false;
                    }
                    break;
                case PositionDirection.ShortPosition:
                    {
                        if (stoploss.TradeActionBuySell != TradeActionBuySell.Buy)
                            return false;
                        if (stoploss.ExpectedExecutionPrice <= lastBar.Close)
                            return false;
                    }
                    break;
            }

            return true;
        }

        #endregion
        #region Open Position Management

        // Position scaling when you get around to it

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
                        lastPx = position.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Open;
                        break;
                    case TimeOfDay.MarketEndOfDay:
                        lastPx = position.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Close;
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
}
