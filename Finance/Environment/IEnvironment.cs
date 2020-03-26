using Finance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Helpers;

namespace Finance
{
    public static class TradingEnvironment
    {
        public static TradingEnvironmentType TradingEnvironmentType { get; private set; }
        private static IEnvironment _Instance { get; set; }
        public static IEnvironment Instance
        {
            get
            {
                if (_Instance == null || TradingEnvironmentType != Settings.Instance.TradingEnvironment)
                {
                    switch (Settings.Instance.TradingEnvironment)
                    {
                        case TradingEnvironmentType.InteractiveBrokersApi:
                            _Instance = new IbkrEnvironment(true);
                            break;
                        case TradingEnvironmentType.InteractiveBrokersTws:
                            _Instance = new IbkrEnvironment(false);
                            break;
                        default:
                            break;
                    }
                }
                TradingEnvironmentType = Settings.Instance.TradingEnvironment;
                return _Instance;
            }
        }
    }
    public interface IEnvironment
    {

        decimal RegTEndOfDayInitialMargin { get; set; }

        decimal InitialMarginLong { get; set; }
        decimal MaintenanceMarginLong { get; set; }

        decimal InitialMarginShort { get; set; }
        decimal MaintenanceMarginShort { get; set; }

        decimal MinimumEquityWithLoanValueNewPosition { get; set; }

        bool NegateCommissionForTesting { get; set; }

        /// <summary>
        /// Margin that must be maintained on the initial trade date
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        decimal BrokerInitialMargin(Trade trd);

        /// <summary>
        /// Margin calculated at EOR for new trades under Reg T
        /// </summary>
        /// <param name="trd"></param>
        /// <returns></returns>
        decimal RegTInitialMargin(Trade trd);

        /// <summary>
        /// Maintenance margin required to hold for positions > 1 day
        /// </summary>
        /// <param name="p"></param>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        decimal BrokerMaintenanceMargin(Position pos, DateTime AsOf, TimeOfDay MarketValues);

        decimal RegTEndOfDayMargin(Position pos, DateTime AsOf, TimeOfDay MarketValues);

        decimal CommissionCharged(Trade trd, bool API = false);
        decimal CommissionCharged(List<Trade> trds, bool API = false);

        /// <summary>
        /// Expected price slippage
        /// </summary>
        /// <param name="Price"></param>
        /// <returns></returns>
        decimal Slippage(decimal Price);

        /// <summary>
        /// Execution price adjusted for slippage based on trade direction
        /// </summary>
        /// <param name="Price"></param>
        /// <param name="tradeAction"></param>
        /// <returns></returns>
        decimal SlippageAdjustedPrice(decimal Price, TradeActionBuySell tradeAction);

    }
    public class IbkrEnvironment : IEnvironment
    {
        public decimal RegTEndOfDayInitialMargin { get; set; } = .50m;

        public decimal MaintenanceMarginLong { get; set; } = .25m;
        public decimal MaintenanceMarginShort { get; set; } = .30m;

        public decimal InitialMarginLong { get; set; } = .25m;
        public decimal InitialMarginShort { get; set; } = .30m;

        public decimal MinimumEquityWithLoanValueNewPosition { get; set; } = 2000.00m;

        public bool NegateCommissionForTesting { get; set; } = false;

        public bool ApiTrading { get; }

        public IbkrEnvironment(bool apiTrading)
        {
            ApiTrading = apiTrading;
        }

        /// <summary>
        /// Returns commission charged on a single trade as a negative dollar value
        /// </summary>
        /// <param name="trd">Trade object</param>
        /// <param name="ApiTrading">Flag indicating if the trade was placed through the API</param>
        /// <returns></returns>
        public decimal CommissionCharged(Trade trd, bool ApiTrading)
        {
            if (NegateCommissionForTesting)
                return 0.0m;

            try
            {
                var perShare = .005m;
                if (ApiTrading)
                    perShare = .0075m;

                var ret = perShare * trd.Quantity;

                // Minimum of $1.00 per trade
                ret = Math.Max(ret, 1.00m);

                // Maximum of .5% of trade value
                ret = Math.Min(ret, Math.Abs(.005m * trd.TotalCashImpact));

                return -ret;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns total commission charged on a list of trades
        /// </summary>
        /// <param name="trds"></param>
        /// <param name="ApiTrading"></param>
        /// <returns></returns>
        public decimal CommissionCharged(List<Trade> trds, bool ApiTrading)
        {
            if (NegateCommissionForTesting)
                return 0.0m;

            try
            {
                return trds.Sum(x => CommissionCharged(x, ApiTrading));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Positive value of initial margin
        /// </summary>
        /// <param name="trd"></param>
        /// <returns></returns>
        public decimal BrokerInitialMargin(Trade trd)
        {
            switch (trd.TradeActionBuySell)
            {
                case TradeActionBuySell.Buy:
                    {
                        var ret = (InitialMarginLong * trd.TotalCashImpactAbsolute);
                        if (ret < 2000.0m)
                        {
                            return Math.Min(2000.0m, trd.TotalCashImpactAbsolute);
                        }
                        return ret;
                    }
                case TradeActionBuySell.Sell:
                    {
                        if (trd.LimitPrice <= 2.50m)
                        {
                            // $2.50 per share or minimum of $2000.00
                            return Math.Max((trd.Quantity * 2.50m), 2000.0m);
                        }
                        if (trd.LimitPrice <= 5.00m)
                        {
                            // Market value of stock or minimum of $2000.00
                            return Math.Max((trd.TotalCashImpactAbsolute), 2000.0m);
                        }
                        if (trd.LimitPrice <= 16.67m)
                        {
                            // $5.00 per share or minimum of $2000.00
                            return Math.Max((trd.Quantity * 5.00m), 2000.0m);
                        }
                        else
                        {
                            // Margin percentage requirement or minimum of $2000.00
                            return Math.Max(InitialMarginShort * trd.TotalCashImpactAbsolute, 2000.0m);
                        }
                    }
                default:
                    {
                        throw new InvalidTradeOperationException();
                    }
            }
        }

        /// <summary>
        /// Positive value of initial Reg T margin
        /// </summary>
        /// <param name="trd"></param>
        /// <returns></returns>
        public decimal RegTInitialMargin(Trade trd)
        {
            return (RegTEndOfDayInitialMargin * trd.TotalCashImpactAbsolute);
        }

        /// <summary>
        /// Positive value of overnight margin.  Calculated same as initial margin, but based on the closing day's value of the position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="AsOf"></param>
        /// <returns>Positive value of maintenance margin</returns>
        public decimal BrokerMaintenanceMargin(Position position, DateTime AsOf, TimeOfDay MarketValues)
        {
            PriceBar currentBar = position.Security.GetPriceBar(AsOf, PriceBarSize.Daily);
            if (currentBar == null)
                throw new InvalidTradingDateException() { message = "Could not retrieve price bar data" };

            switch (position.PositionDirection)
            {
                case PositionDirection.LongPosition:
                    {
                        var ret = (MaintenanceMarginLong * position.GrossPositionValue(AsOf, MarketValues));
                        if (ret < 2000.0m)
                        {
                            return Math.Min(2000.0m, position.GrossPositionValue(AsOf, MarketValues));
                        }
                        return ret;
                    }
                case PositionDirection.ShortPosition:
                    {

                        // From IBKR website: "All short transactions in margin accounts are subject to a minimum initial margin requirement of $2,000."
                        var ValuePerShare = 0m;

                        if (MarketValues == TimeOfDay.MarketOpen)
                            ValuePerShare = currentBar.Open;
                        else if (MarketValues == TimeOfDay.MarketEndOfDay)
                            ValuePerShare = currentBar.Close;
                        else
                            throw new InvalidRequestValueException() { message = "MarketValues not set" };

                        var Shares = Math.Abs(position.Size(AsOf));

                        if (ValuePerShare <= 2.50m)
                        {
                            // $2.50 per share or minimum of $2000.00
                            return Math.Max((Shares * 2.50m), 2000.0m);
                        }
                        if (ValuePerShare <= 5.00m)
                        {
                            // Market value of stock or minimum of $2000.00
                            return Math.Max(Math.Abs(position.GrossPositionValue(AsOf, MarketValues)), 2000.0m);
                        }
                        if (ValuePerShare <= 16.67m)
                        {
                            // $2.50 per share or minimum of $2000.00
                            return Math.Max((Shares * 5.00m), 2000.0m);
                        }
                        else
                        {
                            // Margin percentage requirement or minimum of $2000.00
                            return Math.Max(MaintenanceMarginShort * Math.Abs(position.GrossPositionValue(AsOf, MarketValues)), 2000.0m);
                        }
                    }
                default:
                    {
                        throw new UnknownErrorException();
                    }
            }
        }

        /// <summary>
        /// Calculates margin requirements for purposes of Reg T
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public decimal RegTEndOfDayMargin(Position pos, DateTime AsOf, TimeOfDay MarketValues)
        {
            return (RegTEndOfDayInitialMargin * Math.Abs(pos.GrossPositionValue(AsOf, MarketValues)));
        }

        public decimal Slippage(decimal Price)
        {
            if (Price < 1)
                return .06m;
            if (Price < 2)
                return .05m;
            if (Price < 5)
                return .03m;

            return .02m;
        }
        public decimal SlippageAdjustedPrice(decimal Price, TradeActionBuySell tradeAction)
        {
            return Math.Round((Price + ((int)tradeAction) * Slippage(Price)), 3);
        }
    }
}
