using System;
using Finance.LiveTrading;

namespace Finance
{

    #region Simulation

    public abstract class TradeApprovalRuleBase
    {
        public string Name { get; }

        protected TradeApprovalRuleBase(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Executes rule on trade and returns a true/false pass 
        /// </summary>
        /// <param name="trade"></param>
        /// <param name="portfolio"></param>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public bool Run(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            return Rule(trade, portfolio, AsOf, timeOfDay);
        }

        protected abstract bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay);
    }

    public class TradeApprovalRule_0 : TradeApprovalRuleBase
    {
        //
        // PASS: Size must be greater than 0
        //       

        public TradeApprovalRule_0(string name) : base(name)
        {
        }
        protected override bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            if (trade.Quantity > 0)
                return true;
            else
                return false;
        }

    }
    public class TradeApprovalRule_1 : TradeApprovalRuleBase
    {
        //
        // PASS: Account must have a minimum equity with loan value or commodities net liquidation value to open a new position.
        //
        public TradeApprovalRule_1(string name) : base(name)
        {
        }

        protected override bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            if (portfolio.EquityWithLoanValue(AsOf, timeOfDay) < TradingEnvironment.Instance.MinimumEquityWithLoanValueNewPosition)
                return false;
            else
                return true;
        }
    }
    public class TradeApprovalRule_2 : TradeApprovalRuleBase
    {
        //
        // PASS: Available Funds after trade execution must be greater than or equal to 0
        //

        public TradeApprovalRule_2(string name) : base(name)
        {

        }

        protected override bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            // Set the expected execution price of the trade
            var tradeCopy = trade.Copy();
            var portCopy = portfolio.Copy();

            decimal execPx = 0;

            // Set the expected execution price of the trade
            switch (tradeCopy.TradeType)
            {
                case TradeType.Market:
                    if (timeOfDay == TimeOfDay.MarketOpen)
                        execPx = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Open;
                    else if (timeOfDay == TimeOfDay.MarketEndOfDay)
                        execPx = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Close;
                    break;
                case TradeType.Limit:
                case TradeType.Stop:
                    execPx = trade.ExpectedExecutionPrice;
                    break;
                default:
                    throw new UnknownErrorException();
            }

            // Execute the copied trade and place in copied portfolio
            tradeCopy.MarkExecuted(AsOf, execPx);
            portCopy.AddExecutedTrade(tradeCopy);

            // Check values of new portfolio
            if (portCopy.AvailableFunds(AsOf, timeOfDay) < 0m)
                return false;
            else
                return true;
        }
    }
    public class TradeApprovalRule_3 : TradeApprovalRuleBase
    {
        //
        // PASS: Gross Position Value must not be more than 30 times the 
        // Net Liquidation Value minus the futures options value at time of trade
        //

        public TradeApprovalRule_3(string name) : base(name)
        {

        }

        protected override bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            // Create a copy of the trade and portfolio to analyze
            var tradeCopy = trade.Copy();
            var portCopy = portfolio.Copy();

            decimal execPx = 0;

            // Set the expected execution price of the trade
            switch (tradeCopy.TradeType)
            {
                case TradeType.Market:
                    if (timeOfDay == TimeOfDay.MarketOpen)
                        execPx = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Open;
                    else if (timeOfDay == TimeOfDay.MarketEndOfDay)
                        execPx = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Close;
                    break;
                case TradeType.Limit:
                case TradeType.Stop:
                    execPx = trade.ExpectedExecutionPrice;
                    break;
                default:
                    throw new UnknownErrorException();
            }

            // Execute the copied trade and place in copied portfolio
            tradeCopy.MarkExecuted(AsOf, execPx);
            portCopy.AddExecutedTrade(tradeCopy);

            var newPosition = portCopy.GetPosition(tradeCopy.Security, AsOf, false);

            // Check values of new portfolio
            if (portCopy.GrossPositionValue(AsOf, timeOfDay) > portCopy.NetLiquidationValue(AsOf, timeOfDay) * 30)
                return false;
            else
                return true;
        }
    }
    public class TradeApprovalRule_4 : TradeApprovalRuleBase
    {
        //
        // PASS: SMA Account Balance cannot be negative after the trade is executed
        //

        public TradeApprovalRule_4(string name) : base(name)
        {

        }

        protected override bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            // Create a copy of the trade and portfolio to analyze
            var tradeCopy = trade.Copy();
            var portCopy = portfolio.Copy();

            decimal execPx = 0;

            // Set the expected execution price of the trade
            switch (tradeCopy.TradeType)
            {
                case TradeType.Market:
                    if (timeOfDay == TimeOfDay.MarketOpen)
                        execPx = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Open;
                    else if (timeOfDay == TimeOfDay.MarketEndOfDay)
                        execPx = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Close;
                    break;
                case TradeType.Limit:
                case TradeType.Stop:
                    execPx = trade.ExpectedExecutionPrice;
                    break;
                default:
                    throw new UnknownErrorException();
            }

            // Execute the copied trade and place in copied portfolio
            tradeCopy.MarkExecuted(AsOf, execPx);
            portCopy.AddExecutedTrade(tradeCopy);

            // Check values of new portfolio
            if (portCopy.SpecialMemorandumAccountBalance(AsOf, timeOfDay) < 0m)
                return false;
            else
                return true;
        }
    }
    public class TradeApprovalRule_5 : TradeApprovalRuleBase
    {
        //
        // PASS: Positions open after the trade is executed cannot exceed preset quantity
        //

        int Max_Open_Positions { get; set; }

        public TradeApprovalRule_5(string name, int max_Open_Positions) : base(name)
        {
            Max_Open_Positions = max_Open_Positions;
        }
        public void UpdateParameter(int max_Open_Positions)
        {
            this.Max_Open_Positions = max_Open_Positions;
        }

        protected override bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            // Create a copy of the trade and portfolio to analyze
            var tradeCopy = trade.Copy();
            var portCopy = portfolio.Copy();

            decimal execPx = 0;

            // Set the expected execution price of the trade
            switch (tradeCopy.TradeType)
            {
                case TradeType.Market:
                    if (timeOfDay == TimeOfDay.MarketOpen)
                        execPx = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Open;
                    else if (timeOfDay == TimeOfDay.MarketEndOfDay)
                        execPx = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Close;
                    break;
                case TradeType.Limit:
                case TradeType.Stop:
                    execPx = trade.ExpectedExecutionPrice;
                    break;
                default:
                    throw new UnknownErrorException();
            }

            // Execute the copied trade and place in copied portfolio
            tradeCopy.MarkExecuted(AsOf, execPx);
            portCopy.AddExecutedTrade(tradeCopy);

            // Check values of new portfolio
            if (portCopy.GetPositions(PositionStatus.Open, AsOf).Count > Max_Open_Positions)
                return false;
            else
                return true;
        }

    }
    public class TradeApprovalRule_6 : TradeApprovalRuleBase
    {
        //
        // PASS: Available funds in the account must be above preset percentage level after the trade is executed
        //

        decimal Minimum_Available_Funds_Percentage { get; set; }

        public TradeApprovalRule_6(string name, decimal minimum_Available_Funds_Percentage) : base(name)
        {
            Minimum_Available_Funds_Percentage = minimum_Available_Funds_Percentage;
        }
        public void UpdateParameter(int minimum_Available_Funds_Percentage)
        {
            this.Minimum_Available_Funds_Percentage = minimum_Available_Funds_Percentage;
        }
        protected override bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            // Create a copy of the trade and portfolio to analyze
            var tradeCopy = trade.Copy();
            var portCopy = portfolio.Copy();

            decimal execPx = 0;

            // Set the expected execution price of the trade
            switch (tradeCopy.TradeType)
            {
                case TradeType.Market:
                    if (timeOfDay == TimeOfDay.MarketOpen)
                        execPx = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Open;
                    else if (timeOfDay == TimeOfDay.MarketEndOfDay)
                        execPx = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Close;
                    break;
                case TradeType.Limit:
                case TradeType.Stop:
                    execPx = trade.ExpectedExecutionPrice;
                    break;
                default:
                    throw new UnknownErrorException();
            }

            // Execute the copied trade and place in copied portfolio
            tradeCopy.MarkExecuted(AsOf, execPx);
            portCopy.AddExecutedTrade(tradeCopy);

            // Check values of new portfolio
            if (portCopy.AvailableFunds(AsOf, timeOfDay) / portCopy.EquityWithLoanValue(AsOf, timeOfDay) < Minimum_Available_Funds_Percentage)
                return false;
            else
                return true;
        }

    }
    public class TradeApprovalRule_7 : TradeApprovalRuleBase
    {
        //
        // PASS: The expected stoploss level must be above zero
        //

        int Stoploss_Atr_Period { get; set; }
        decimal Stoploss_Multiple { get; set; }

        public TradeApprovalRule_7(string name, int stoploss_atr_period, decimal stoploss_multiple) : base(name)
        {
            Stoploss_Atr_Period = stoploss_atr_period;
            Stoploss_Multiple = stoploss_multiple;
        }
        public void UpdateParameters(int stoploss_atr_period, decimal stoploss_multiple)
        {
            Stoploss_Atr_Period = stoploss_atr_period;
            Stoploss_Multiple = stoploss_multiple;
        }
        protected override bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            PriceBar bar = trade.Security.GetPriceBar(AsOf, PriceBarSize.Daily);
            decimal atr = bar.AverageTrueRange(Stoploss_Atr_Period);

            if (bar.Close - (atr * Stoploss_Multiple) < 0)
                return false;

            return true;
        }

    }

    #endregion

    #region Live Rules

    public class TradeApprovalMessage
    {
        public int RuleNumber { get; }
        public string RuleName { get; }
        public LiveTradeApprovalMessageType Result { get; }
        public string Message { get; }

        public TradeApprovalMessage(int ruleNumber, string ruleName, LiveTradeApprovalMessageType result, string message = "")
        {
            RuleNumber = ruleNumber;
            RuleName = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
            Result = result;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public override string ToString()
        {
            switch (Result)
            {
                case LiveTradeApprovalMessageType.Failed:
                    return $"FAILED rule {RuleNumber} ({RuleName}): {Message}";
                case LiveTradeApprovalMessageType.Passed:
                    return $"PASSED rule {RuleNumber} ({RuleName})";
                case LiveTradeApprovalMessageType.Warning:
                    return $"WARNING on rule {RuleNumber} ({RuleName}): {Message}";
                default:
                    throw new UnknownErrorException() { message = "Rule result not set" };
            }

        }
    }

    public abstract class LiveTradeApprovalRuleBase
    {
        public abstract string Name { get; }

        protected LiveTradeApprovalRuleBase()
        {
        }

        /// <summary>
        /// Executes rule on trade and returns a true/false pass 
        /// </summary>
        /// <param name="trade"></param>
        /// <param name="portfolio"></param>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public TradeApprovalMessage Run(LiveOrder order, LiveAccount account)
        {
            return Rule(order, account);
        }

        protected abstract TradeApprovalMessage Rule(LiveOrder order, LiveAccount account);
    }

    public class LiveTradeApprovalRule_0 : LiveTradeApprovalRuleBase
    {
        public override string Name => "Sanity Check";

        //
        // PASS: Sanity check on parameters
        //       
        public LiveTradeApprovalRule_0()
        {
        }

        protected override TradeApprovalMessage Rule(LiveOrder order, LiveAccount account)
        {
            if (order.OrderSize <= 0)
                return new TradeApprovalMessage(0, Name, LiveTradeApprovalMessageType.Failed, "Order Size Invalid");

            if (order.LimitPrice <= 0)
                return new TradeApprovalMessage(0, Name, LiveTradeApprovalMessageType.Failed, "Order Price Invalid");

            if (order.OrderDirection == TradeActionBuySell.None)
                return new TradeApprovalMessage(0, Name, LiveTradeApprovalMessageType.Failed, "Order Direction Not Set");

            if (order.Security == null)
                return new TradeApprovalMessage(0, Name, LiveTradeApprovalMessageType.Failed, "Security Not Specified");

            return new TradeApprovalMessage(0, Name, LiveTradeApprovalMessageType.Passed);
        }
    }

    #endregion

}
