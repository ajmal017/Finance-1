using System;

namespace Finance
{

    public abstract class TradeApprovalRuleBase
    {
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

        /// <summary>
        /// Specific rule is implemented in derived class
        /// </summary>
        /// <param name="trade"></param>
        /// <param name="portfolio"></param>
        /// <param name="AsOf"></param>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        protected abstract bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay);
    }

    public class TradeApprovalRule_1 : TradeApprovalRuleBase
    {
        //
        // PASS: Account must have a minimum equity with loan value or commodities net liquidation value to open a new position.
        //

        protected override bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            if (portfolio.EquityWithLoanValue(AsOf, timeOfDay) < portfolio.Environment.MinimumEquityWithLoanValueNewPosition)
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
                        execPx = trade.Security.GetPriceBar(AsOf).Open;
                    else if (timeOfDay == TimeOfDay.MarketEndOfDay)
                        execPx = trade.Security.GetPriceBar(AsOf).Close;
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
                        execPx = trade.Security.GetPriceBar(AsOf).Open;
                    else if (timeOfDay == TimeOfDay.MarketEndOfDay)
                        execPx = trade.Security.GetPriceBar(AsOf).Close;
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
                        execPx = trade.Security.GetPriceBar(AsOf).Open;
                    else if (timeOfDay == TimeOfDay.MarketEndOfDay)
                        execPx = trade.Security.GetPriceBar(AsOf).Close;
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

        int Max_Open_Positions { get; }

        public TradeApprovalRule_5(int max_Open_Positions)
        {
            Max_Open_Positions = max_Open_Positions;
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
                        execPx = trade.Security.GetPriceBar(AsOf).Open;
                    else if (timeOfDay == TimeOfDay.MarketEndOfDay)
                        execPx = trade.Security.GetPriceBar(AsOf).Close;
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

        decimal Minimum_Available_Funds_Percentage { get; }

        public TradeApprovalRule_6(decimal minimum_Available_Funds_Percentage)
        {
            Minimum_Available_Funds_Percentage = minimum_Available_Funds_Percentage;
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
                        execPx = trade.Security.GetPriceBar(AsOf).Open;
                    else if (timeOfDay == TimeOfDay.MarketEndOfDay)
                        execPx = trade.Security.GetPriceBar(AsOf).Close;
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

        int Stoploss_Atr_Period { get; }
        decimal Stoploss_Multiple { get; }

        public TradeApprovalRule_7(int stoploss_atr_period, decimal stoploss_multiple)
        {
            Stoploss_Atr_Period = stoploss_atr_period;
            Stoploss_Multiple = stoploss_multiple;
        }
        protected override bool Rule(Trade trade, Portfolio portfolio, DateTime AsOf, TimeOfDay timeOfDay)
        {
            PriceBar bar = trade.Security.GetPriceBar(AsOf);
            decimal atr = bar.AverageTrueRange(Stoploss_Atr_Period);

            if (bar.Close - (atr * Stoploss_Multiple) < 0)
                return false;

            return true;
        }

    }

}
