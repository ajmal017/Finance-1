using System;

namespace Finance
{
    public class TradingSystemException : Exception
    {
        public string message;
        public string location = $"Exception thrown from {Helpers.GetCurrentMethod()}";
    }

    public class InvalidTradingDateException : TradingSystemException
    {
        public new string message = "Not a valid trading date";
    }
    public class SecurityNotFoundException : TradingSystemException
    {
        public new string message = "Security not found in database";
    }
    public class InvalidDateOrderException : TradingSystemException { }
    public class InvalidTradeOperationException : TradingSystemException { }
    public class InvalidTradeForPositionException : TradingSystemException { }
    public class InvalidStoplossTradeException : TradingSystemException { }
    public class InvalidRuleOrderException : TradingSystemException { }
    public class RulePipelineException : TradingSystemException { }
    public class InvalidDataRequestException : TradingSystemException { }
    public class InvalidRequestValueException : TradingSystemException { }
    public class TradeQueueException : TradingSystemException { }
    public class UnknownErrorException : TradingSystemException { }
    public class DataproviderConnectionException : TradingSystemException { }
    public class IEXDataProviderException : TradingSystemException { }
    public class SwingPointOperationException : TradingSystemException { }
    public class TrendOperationException : TradingSystemException { }

    public class CancelTradeException : TradingSystemException
    {
        public Security Security { get; set; }
        public DateTime AsOf { get; set; }
        public string Method { get; set; }

        public override string ToString()
        {
            return string.Format($"Signal rejected: {Security.Ticker} could not validate new position {AsOf.ToShortDateString()}");
        }

        public CancelTradeException(string cancelMessage, Security security, DateTime asOf, string method)
        {
            message = cancelMessage;
            Security = security ?? throw new ArgumentNullException(nameof(security));
            AsOf = asOf;
            Method = method ?? throw new ArgumentNullException(nameof(method));
        }
    }
    public class LiveTradeSystemException : TradingSystemException
    {

    }
}