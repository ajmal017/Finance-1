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

}