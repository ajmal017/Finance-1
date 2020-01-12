using System;
using System.Collections.Generic;
using System.Linq;

namespace Finance
{
    // Refactored

    public partial class TradeManager
    {
        private Portfolio Portfolio { get; }
        private IEnvironment Environment { get; }
        public List<Trade> TradeQueue { get; } = new List<Trade>();

        public TradeManager(Portfolio portfolio)
        {
            Portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
            Environment = Portfolio.Environment;
        }

        public void AddPendingTrades(List<Trade> trades)
        {
            if (!trades.TrueForAll(x => x.TradeStatus == TradeStatus.Pending))
                throw new InvalidTradeOperationException() { message = "Cannot add non-pending trades in this method" };

            TradeQueue.AddRange(trades);
        }
        public void AddStoplossTrades(List<Trade> trades)
        {
            if (!trades.TrueForAll(x => x.TradeStatus == TradeStatus.Stoploss))
                throw new InvalidTradeOperationException() { message = "Cannot add non-stoploss trades in this method" };

            TradeQueue.AddRange(trades);
        }
        public List<Trade> GetAllStoplosses(DateTime AsOf)
        {
            return TradeQueue.Where(trd => trd.TradeStatus == TradeStatus.Stoploss && trd.TradeDate <= AsOf).ToList();
        }
        public List<Trade> GetHistoricalTrades(Security security)
        {
            return TradeQueue.Where(trd => trd.Security == security).ToList();
        }

        public void ProcessTradeQueue(DateTime AsOf, TimeOfDay timeOfDay)
        {
            //
            // Call Execution methods based on the time of day being actioned
            //

            // Order by priority
            TradeQueue.Sort((x, y) => y.TradePriority.CompareTo(x.TradePriority));

            switch (timeOfDay)
            {
                case TimeOfDay.MarketOpen:
                    {
                        // Execute all trades which are triggered by opening prices
                        _1_OpeningStops(AsOf);
                        _2_MarketAndLimitTradesAtOpen(AsOf);
                    }
                    break;
                case TimeOfDay.MarketEndOfDay:
                    {
                        // Execute all trades which are triggered by daily price range
                        _3_EndOfDayStops(AsOf);
                        _4_EndOfDayLimitTrades(AsOf);
                        _5_EndOfDayCheck(AsOf);
                    }
                    break;
                default:
                    break;
            }
        }

        protected void _1_OpeningStops(DateTime AsOf)
        {
            var Stops = TradeQueue.Where(x => x.TradeStatus == TradeStatus.Stoploss);

            if (Stops.Count() == 0)
                return;

            foreach (Trade stop in Stops)
                TryExecuteStopTrade(stop, AsOf, TimeOfDay.MarketOpen);
        }
        protected void _2_MarketAndLimitTradesAtOpen(DateTime AsOf)
        {
            var Trades = TradeQueue.Where(x =>
                x.TradeStatus == TradeStatus.Pending &&
                (x.TradeType == TradeType.Market || x.TradeType == TradeType.Limit)).ToArray();

            if (Trades.Count() == 0)
                return;

            for (int i = 0; i < Trades.Count(); i++)
            {
                if (Trades[i].TradeType == TradeType.Market)
                    TryExecuteMarketTrade(Trades[i], AsOf);
                if (Trades[i].TradeType == TradeType.Limit)
                    TryExecuteLimitTrade(Trades[i], AsOf, TimeOfDay.MarketOpen);
            }
        }
        protected void _3_EndOfDayStops(DateTime AsOf)
        {
            var Stops = TradeQueue.Where(x => x.TradeStatus == TradeStatus.Stoploss);

            if (Stops.Count() == 0)
                return;

            foreach (Trade stop in Stops)
                TryExecuteStopTrade(stop, AsOf, TimeOfDay.MarketEndOfDay);
        }
        protected void _4_EndOfDayLimitTrades(DateTime AsOf)
        {
            var Trades = TradeQueue.Where(x =>
                x.TradeStatus == TradeStatus.Pending &&
                x.TradeType == TradeType.Limit).ToArray();

            if (Trades.Count() == 0)
                return;

            for (int i = 0; i < Trades.Count(); i++)
            {
                // If the limit trade could not execute by EOD, cancel
                if (!TryExecuteLimitTrade(Trades[i], AsOf, TimeOfDay.MarketEndOfDay))
                {
                    Trades[i].TradeStatus = TradeStatus.Cancelled;
                }
            }
        }
        protected void _5_EndOfDayCheck(DateTime AsOf)
        {
            // All Market trades should be executed
            if (!TradeQueue.Where(trd => trd.TradeType == TradeType.Market).ToList()
                .TrueForAll(trd => trd.TradeStatus == TradeStatus.Executed))
            {
                throw new TradeQueueException() { message = "Trade Queue failed end of day check: unactioned Market type orders still exist" };
            }

            // All Limit trades should either be executed or cancelled
            if (!TradeQueue.Where(trd => trd.TradeType == TradeType.Limit).ToList()
                .TrueForAll(trd => trd.TradeStatus == TradeStatus.Executed || trd.TradeStatus == TradeStatus.Cancelled))
            {
                var badTrd = TradeQueue.Where((trd => trd.TradeType == TradeType.Limit && trd.TradeStatus != TradeStatus.Executed));
                throw new TradeQueueException() { message = "Trade Queue failed end of day check: unactioned Limit type orders still exist" };
            }

            // There should be one active stoploss for each open position
            if (!Portfolio.GetPositions(PositionStatus.Open, AsOf)
                .TrueForAll(pos => TradeQueue.Exists(trd => trd.TradeStatus == TradeStatus.Stoploss && trd.Security == pos.Security)))
            {
                throw new TradeQueueException() { message = "Trade Queue failed end of day check: missing appropriate number of stoploss trades in queue" };
            }
        }

        protected void TryExecuteStopTrade(Trade trade, DateTime AsOf, TimeOfDay timeOfDay)
        {
            if (trade.TradeType != TradeType.Stop)
                throw new InvalidTradeOperationException() { message = "Must provide Stop type trade" };

            PriceBar usedPriceBar = trade.Security.GetPriceBar(AsOf);
            if (usedPriceBar == null)
                throw new InvalidTradingDateException() { message = "Could not retrieve execution price bar" };

            switch (timeOfDay)
            {
                case TimeOfDay.MarketOpen:
                    {
                        switch (trade.TradeActionBuySell)
                        {
                            case TradeActionBuySell.None:
                                throw new InvalidTradeOperationException();
                            case TradeActionBuySell.Buy:
                                if (usedPriceBar.Open >= trade.StopPrice)
                                    ExecuteTrade(trade, usedPriceBar.Open, AsOf);
                                break;
                            case TradeActionBuySell.Sell:
                                if (usedPriceBar.Open <= trade.StopPrice)
                                    ExecuteTrade(trade, usedPriceBar.Open, AsOf);
                                break;
                        }
                    }
                    break;
                case TimeOfDay.MarketEndOfDay:
                    {
                        switch (trade.TradeActionBuySell)
                        {
                            case TradeActionBuySell.None:
                                throw new InvalidTradeOperationException();
                            case TradeActionBuySell.Buy:
                                if (usedPriceBar.High >= trade.StopPrice)
                                    ExecuteTrade(trade, trade.StopPrice, AsOf);
                                break;
                            case TradeActionBuySell.Sell:
                                if (usedPriceBar.Low <= trade.StopPrice)
                                    ExecuteTrade(trade, trade.StopPrice, AsOf);
                                break;
                        }
                    }
                    break;
            }
        }
        protected void TryExecuteMarketTrade(Trade trade, DateTime AsOf)
        {
            if (trade.TradeType != TradeType.Market)
                throw new InvalidTradeOperationException() { message = "Must provide Market type trade" };

            PriceBar usedPriceBar = trade.Security.GetPriceBar(AsOf);
            if (usedPriceBar == null)
                throw new InvalidTradingDateException() { message = "Could not retrieve execution price bar" };

            ExecuteTrade(trade, usedPriceBar.Open, AsOf);

        }
        protected bool TryExecuteLimitTrade(Trade trade, DateTime AsOf, TimeOfDay timeOfDay)
        {
            if (trade.TradeType != TradeType.Limit)
                throw new InvalidTradeOperationException() { message = "Must provide Limit type trade" };

            PriceBar usedPriceBar = trade.Security.GetPriceBar(AsOf);
            if (usedPriceBar == null)
                throw new InvalidTradingDateException() { message = "Could not retrieve execution price bar" };

            switch (timeOfDay)
            {
                case TimeOfDay.MarketOpen:
                    {
                        switch (trade.TradeActionBuySell)
                        {
                            case TradeActionBuySell.None:
                                throw new InvalidTradeOperationException();
                            case TradeActionBuySell.Buy:
                                if (usedPriceBar.Open <= trade.LimitPrice)
                                    ExecuteTrade(trade, usedPriceBar.Open, AsOf);
                                return true;
                            case TradeActionBuySell.Sell:
                                if (usedPriceBar.Open >= trade.LimitPrice)
                                    ExecuteTrade(trade, usedPriceBar.Open, AsOf);
                                return true;
                        }
                    }
                    return false;
                case TimeOfDay.MarketEndOfDay:
                    {
                        switch (trade.TradeActionBuySell)
                        {
                            case TradeActionBuySell.None:
                                throw new InvalidTradeOperationException();
                            case TradeActionBuySell.Buy:
                                if (usedPriceBar.Low <= trade.LimitPrice)
                                {
                                    ExecuteTrade(trade, trade.LimitPrice, AsOf);
                                    return true;
                                }
                                else
                                    return false;
                            case TradeActionBuySell.Sell:
                                if (usedPriceBar.High >= trade.LimitPrice)
                                {
                                    ExecuteTrade(trade, trade.LimitPrice, AsOf);
                                    return true;
                                }
                                else
                                    return false;
                        }
                    }
                    return false;
                default:
                    return false;
            }


        }

        protected void ExecuteTrade(Trade trade, decimal executionPrice, DateTime AsOf)
        {
            // Adjust price for slippage
            executionPrice = Environment.SlippageAdjustedPrice(executionPrice, trade.TradeActionBuySell);

            // Mark executed
            trade.MarkExecuted(AsOf, executionPrice);

            // Place in portfolio
            Portfolio.AddExecutedTrade(trade);
        }
    }
}
