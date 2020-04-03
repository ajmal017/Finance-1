using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    public enum SettingsType
    {
        Application = 0,
        ReferenceData = 1,
        Trading = 2,
        Testing = 3,
        Layout = 4,
        Style = 5,
        SimulationParameters = 6,
        SecurityFilterParameters = 7,
        RiskParameters = 8,
        StrategyParameters = 9,
        PortfolioParameters = 10,
        PositionManagementParameters = 11,
        IndexManagementParameters = 12,
        LiveTrading = 13,
        LiveRisk = 14
    }
    public enum ApplicationMode
    {
        [Description("Testing Mode")]
        Testing = 0,
        [Description("Production Mode")]
        Production = 1
    }
    public enum TradingEnvironmentType
    {
        [Description("Interactive Brokers API")]
        InteractiveBrokersApi = 0,
        [Description("Interactive Brokers TWS")]
        InteractiveBrokersTws = 1
    }
    public enum PortfolioMarginType
    {
        CashAccount = 0,
        RegTMargin = 1,
        PortfolioMargin = 2
    }

    public enum DataProviderType
    {
        [Description("Not Set")]
        NotSet = 0,
        [Description("Interactive Brokers")]
        InteractiveBrokers = 1,
        [Description("IEX Cloud")]
        IEXCloud = 2
    }
    public enum TradingProviderType
    {
        [Description("Not Set")]
        NotSet = 0,
        [Description("Interactive Brokers")]
        InteractiveBrokers = 1
    }

    public enum IexCloudMode
    {
        [Description("Sandbox Mode")]
        Sandbox = 0,
        [Description("Production Mode")]
        Production = 1
    }

    public enum SecurityType
    {
        Unknown = 0,
        CommonStock = 1,
        ETF = 2,
        ADR = 3,
        REIT = 4,
        ClosedEndFund = 5,
        SecondaryIssue = 6,
        LimitedPartnership = 7,
        Warrant = 8,
        Right = 9,
        Unit = 10,
        Temp = 11
    }
    public enum TradeActionBuySell
    {
        // Sell to close or short
        [Description("SELL")]
        Sell = -1,
        // Default
        [Description("Not Set")]
        None = 0,
        // Buy to open or cover
        [Description("BUY")]
        Buy = 1
    }
    public enum SignalAction
    {
        Sell = -1,
        None = 0,
        Buy = 1,
        CloseIfOpen = 2
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
        [Description("MKT")]
        Market = 0,
        // Trade will execute at Limit Price if Limit Price == bid/ask
        [Description("LMT")]
        Limit = 1,
        // Trade will execute at the prevailing bid/ask if Stop Price is touched
        [Description("STP")]
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
        [Description("Short Only")]
        ShortOnly = -1,
        [Description("Long and Short")]
        LongShort = 0,
        [Description("Long Only")]
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

    [Flags]
    public enum SwingPointType
    {
        [Description("None")]
        None = 0,
        [Description("Potential Swing Point Low")]
        PotentialSwingPointLow = 1,
        [Description("Swing Point Low")]
        SwingPointLow = 2,
        [Description("Potential Swing Point High")]
        PotentialSwingPointHigh = 4,
        [Description("Swing Point High")]
        SwingPointHigh = 8
    }
    public enum TrendAction
    {
        Continuation = 0,
        SidewaysTransition = 1,
        BullishTransition = 2,
        BearishTransition = 3
    }
    public enum TrendQualification
    {
        NotSet = 0,
        [Description("Ambivalent Sideways")]
        AmbivalentSideways = 1,
        [Description("Suspect Sideways")]
        SuspectSideways = 2,
        [Description("Confirmed Sideways")]
        ConfirmedSideways = 4,
        [Description("Suspect Bullish")]
        SuspectBullish = 8,
        [Description("Confirmed Bullish")]
        ConfirmedBullish = 16,
        [Description("Suspect Bearish")]
        SuspectBearish = 32,
        [Description("Confirmed Bearish")]
        ConfirmedBearish = 64
    }
    public enum TrendAlignment
    {
        NotSet = 0,
        SidewaysBullish = 1,
        Bullish = 2,
        SidewaysBearish = 3,
        Bearish = 4,
        Sideways = 5,
        Opposing = 6
    }

    public enum SwingPointTest
    {
        None = 0,
        TestHigh = 1,
        TestLow = 2
    }
    public enum SwingPointTestPriceResult
    {
        CloseDoesNotExceepSwingPoint = 0,
        CloseExceedsSwingPoint = 1
    }
    public enum SwingPointTestVolumeResult
    {
        VolumeExpands = 0,
        VolumeContracts = 1
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
        Complete = 2,
        Error = 3
    }
    public enum ControlStatus
    {
        [Description("Ready")]
        Ready = 3,
        [Description("Working")]
        Working = 2,
        [Description("Error")]
        ErrorState = 1,
        [Description("Offline")]
        Offline = 0

    }

    public enum LogMessageType
    {
        Debug = 0,
        Production = 1,
        SystemError = 2,
        SecurityError = 3,
        TradingNotification = 4,
        TradingError = 5,
        TradingSystemMessage = 6,
        SCRAM = 7,
        TradeWarning = 8
    }
    public enum ControlEdge
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 3,
        Bottom = 4
    }

    public enum PriceBarSize
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2,
        Quarterly = 3
    }
    public enum AccountingSeriesValue
    {
        EquityWithLoanValue = 0,
        TotalCashValue = 1,
        StockValue = 2,
        LongStockValue = 3,
        ShortStockValue = 4,
        NetLiquidationValue = 5,
        BrokerMaintenanceMarginRequirement = 6,
        RegTMaintenanceMarginRequirement = 7,
        SpecialMemorandumAccountBalance = 8,
        OpenPositions = 9
    }

    public enum SecurityGroupName
    {
        [Description("All Securities")]
        All = 0,
        [Description("Dow Jones")]
        [FileName("DowJonesSecurities.txt")]
        DowJones = 1,
        [Description("S&P 500")]
        SandP500 = 2,
        [Description("NASDAQ")]
        Nasdaq = 3
    }
    public enum ChartSeriesView
    {
        Security = 0,
        Volume = 1,
        Positions = 2,
        Trades = 3,
        SwingPoints = 4,
        Signals = 5,
        AccountEquity = 6
    }

    public enum DataProviderRequestType
    {
        NotSet = -1,
        SecurityContractData = 0,
        SecurityPriceData = 1,
        SecurityVolumeData = 2,
        SecurityCompanyInfo = 3
    }
    public enum IbkrDataProviderRequestType
    {
        NotSet = 0,
        SecurityExchangeName = 1,
        SecurityFirstAvailableDate = 2,
        SecurityHistoricalData = 3,
    }
    public enum DataProviderRequestStatus
    {
        [Description("Pending")]
        Pending = 0,
        [Description("Submitted")]
        Submitted = 1,
        [Description("Partial Response")]
        PartialResponse = 2,
        [Description("Request Complete")]
        CompleteResponse = 3,
        [Description("Error Response")]
        ErrorResponse = 4,
        [Description("Request Cancelled")]
        Cancelled = 5,
        [Description("Processing")]
        Processing = 6
    }
    public enum DataProviderErrorType
    {
        NoError = 0,
        ConnectionError = 1,
        InvalidSecurity = 2,
        Cancelled = 3,
        InvalidRequest = 4,
        SystemError = 5
    }

    public enum LiveTradeStatus
    {
        NotSet = 0,
        PendingSubmit = 1,
        PendingCancel = 2,
        PreSubmitted = 3,
        Submitted = 4,
        ApiCancelled = 5,
        Cancelled = 6,
        Filled = 7,
        Inactive = 8
    }
    public enum TradingAccountProviderStatusFlag
    {
        Error = -1,
        NA = 0,
        Connected = 1,
        Disconnected = 2
    }

    [Flags]
    public enum CustomSecurityTag
    {
        [Description("No Flags")]
        None = 0,
        [Description("Excluded")]
        Excluded = 1,
        [Description("Favorite")]
        Favorite = 2,
        [Description("Recently Traded")]
        RecentlyTraded = 4,
        [Description("Missing Data")]
        MissingData = 8,
        [Description("Zero Volume")]
        ZeroVolume = 16

    }
    [Flags]
    public enum CustomPriceBarTag
    {
        [Description("No Flags")]
        None = 0,
        [Description("Excluded")]
        Excluded = 1,
        [Description("Favorite")]
        Favorite = 2
    }

    public enum SecurityFilterType
    {
        None = 0,
        Industry = 1,
        Sector = 2,
        SIC = 3,
        SecurityType = 4,
        Trend = 5
    }

    public enum LiveQuoteType
    {
        NotSet = 0,
        Bid = 1,
        Ask = 2,
        Open = 3,
        Trade = 4
    }
    public enum LiveTradeApprovalMessageType
    {
        [Description("FAILED")]
        Failed = 0,
        [Description("PASSED")]
        Passed = 1, 
        [Description("WARNING")]
        Warning = 2
    }

    [Flags]
    public enum CandleStickPattern
    {
        None = 0,
        BullishHammer = 1,
    }
    [Flags]
    public enum Technical
    {
        None = 0,
        RisingVolume = 1,
        FallingVolume = 2
    }

    public enum ReturnFormat
    {
        Percent = 0,
        ATR = 1
    }
}
