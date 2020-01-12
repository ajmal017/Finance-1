using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    public enum TradeActionBuySell
    {
        // Default
        None = 0,
        // Buy to open or cover
        Buy = 1,
        // Sell to close or short
        Sell = -1
    }

    public enum TradeStatus
    {
        // Newly created or otherwise not valid for submission
        NotSet = 0,
        // Being processed for execution, submitted to market
        Pending = 1,
        // Confirmed as executed
        Executed = 2,
        // Cancelled by submitter
        Cancelled = 3,
        // Rejected by system
        Rejected = 4,
        // Resting, stop order
        Stoploss = 5,
        // Indicated by trading strategy for consideration
        Indicated = 6
    }

    public enum TradePriority
    {
        NotSet = 0,
        NewPositionOpen = 1,
        ExistingPositionIncrease = 2,
        ExistingPositionDecrease = 3,
        PositionClose = 4,
        StoplossImmediate = 5
    }

    public enum TradeType
    {
        // Trade will execute at the prevailing bid/ask
        Market = 0,
        // Trade will execute at Limit Price if Limit Price == bid/ask
        Limit = 1,
        // Trade will execute at the prevailing bid/ask if Stop Price is touched
        Stop = 2
    }

    public enum PositionDirection
    {
        NotSet = 0,
        LongPosition = 1,
        ShortPosition = -1
    }

    public enum PortfolioDirection
    {
        ShortOnly = -1,
        LongShort = 0,
        LongOnly = 1
    }

    public enum PositionStatus
    {
        Closed = 0,
        Open = 1
    }

    public enum MovingAverage
    {
        Simple = 0,
        Exponential = 1
    }

    public enum StrategyType
    {
        LongOnly = 0,
        ShortOnly = 1,
        LongShort = 2
    }

    public enum PortfolioMarginType
    {
        CashAccount = 0,
        RegTMargin = 1,
        PortfolioMargin = 2
    }

    public enum SecurityType
    {
        Unknown = 0,
        USCommonEquity = 1
    }

    public enum DataRequestType
    {
        NotSet = 0,
        ExchangeName = 1,
        FirstAvailableDate = 2,
        HistoricalData = 3,
    }

    public enum TimeOfDay
    {
        MarketOpen = 0,
        MarketEndOfDay = 1
    }

    public enum EventFlag
    {
        NotSet = 0,
        BadSymbol = 1,
        RequestError = 2
    }

    public enum SimulationStatus
    {
        NotStarted = 0,
        Running = 1,
        Complete = 2
    }

    public enum ProcessStatus
    {
        ErrorState = -1,
        Ready = 0,
        Working = 1,
        Offline = 2
    }

    public enum SingleSecurityResultType
    {
        StockSeries = 0,
        SignalSeries = 1,
        BalanceSeries = 2,
        TradeSeries = 3
    }

    public enum PortfolioSimulationResultType
    {
        NetLiquidationValue = 0,
    }

    public enum DataProviderType
    {
        InteractiveBrokers = 0
    }

    public enum LogMessageType
    {
        Debug = 0,
        Production = 1,
        Error = 2
    }

    public enum DockSide
    {
        Left = 0,
        Right = 1,
        Top = 2,
        Bottom = 3
    }

    public enum BarSize
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2
    }
}
