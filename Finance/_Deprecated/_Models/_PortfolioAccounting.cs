using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Finance.Calendar;
using static Finance.Helpers;

namespace Finance.Models
{
    /// <summary>
    /// Accounting
    /// </summary>
    public partial class Portfolio
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Total Cash")]
        public decimal TotalCashValue(DateTime AsOf)
        {
            try
            {
                // Starting Cash + (Purchases + Proceeds) + Commissions (as a negative number)
                return InitialCashBalance + TotalCashPurchasesAndProceeds(AsOf) + TotalCommissions(AsOf);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns a sum total of all purchases (buy long, cover short) and sales (sell long, go short) in the portfolio, as a POSITIVE or NEGATIVE value
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Total Purchases & Proceeds")]
        public decimal TotalCashPurchasesAndProceeds(DateTime AsOf)
        {
            try
            {
                return Positions.Sum(x => x.NetCashImpact(AsOf));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns a sum total of all commission paid on trades in the portfolio, as a NEGATIVE number
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Total Commission Paid")]
        public decimal TotalCommissions(DateTime AsOf)
        {
            try
            {
                return Positions.Sum(x => Environment.CommissionCharged(x.Trades));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns the sum total of all open positions, POSITIVE or NEGATIVE.  Also referred to as Securities Market Value
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public decimal StockValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {                
                return LongStockValue(AsOf, MarketValues) + ShortStockValue(AsOf, MarketValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Another name for StockValue sometimes used
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Securities Market Value")]
        public decimal SecuritiesMarketValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            return StockValue(AsOf, MarketValues);
        }

        /// <summary>
        /// Returns a POSITIVE value
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Long Value")]
        public decimal LongStockValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                return (from p in Positions
                        where p.PositionDirection == PositionDirection.LongPosition
                        select p.CurrentPositionValue(AsOf, MarketValues)).Sum();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns a NEGATIVE value
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Short Value")]
        public decimal ShortStockValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                return (from p in Positions
                        where p.PositionDirection == PositionDirection.ShortPosition
                        select p.CurrentPositionValue(AsOf, MarketValues)).Sum();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        public decimal LongOptionValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            // Not used
            return 0m;
        }
        public decimal ShortOptionValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            // Not used
            return 0m;
        }
        public decimal BondValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            // Not used
            return 0m;
        }
        public decimal FundValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            // Not used
            return 0m;
        }
        public decimal EuroAndAsianOptionsValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            // Not used
            return 0m;
        }

        [StringOutputFormat("Equity With Loan")]
        public decimal EquityWithLoanValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            return
                TotalCashValue(AsOf) +
                StockValue(AsOf, MarketValues) +
                BondValue(AsOf, MarketValues) +
                FundValue(AsOf, MarketValues) +
                EuroAndAsianOptionsValue(AsOf, MarketValues);
        }

        /// <summary>
        /// Returns funds that will be available after execution of a trade (assumed to be at limit price)
        /// </summary>
        /// <param name="AsOf"></param>
        /// <param name="trade"></param>
        /// <returns></returns>
        public decimal PostTradeAvailableFunds(DateTime AsOf, Trade trade, TimeOfDay MarketValues)
        {
            try
            {
                return (EquityWithLoanValue(AsOf, MarketValues) - Environment.BrokerInitialMargin(trade));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns currently available funds
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Available Funds")]
        public decimal AvailableFunds(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                // Normal formula is ELV-IM, however IBKR counts Initial Margin as all trades, not just the day's. 
                // It is the same as MM; we calculate IM for same-day trades only.
                // return (EquityWithLoanValue(AsOf) - BrokerInitialMarginRequirement(AsOf));
                return (EquityWithLoanValue(AsOf, MarketValues) - BrokerMaintenanceMarginRequirement(AsOf, MarketValues));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns the ABSOLUTE VALUE sum of all open positions
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Gross Position Value")]
        public decimal GrossPositionValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                return
                    LongStockValue(AsOf, MarketValues) +
                    Math.Abs(ShortStockValue(AsOf, MarketValues)) +
                    LongOptionValue(AsOf, MarketValues) +
                    ShortOptionValue(AsOf, MarketValues) +
                    FundValue(AsOf, MarketValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        [StringOutputFormat("Net Liquidation Value")]
        public decimal NetLiquidationValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                return
                    TotalCashValue(AsOf) +
                    StockValue(AsOf, MarketValues) +
                    LongOptionValue(AsOf, MarketValues) +
                    ShortOptionValue(AsOf, MarketValues) +
                    FundValue(AsOf, MarketValues);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public decimal FuturesOptionsValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            // Not used
            return 0m;
        }

        /// <summary>
        /// Returns a total initial margin requirement
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Broker Initial Margin")]
        public decimal BrokerInitialMarginRequirement(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                return Positions.Sum(pos => Environment.BrokerMaintenanceMargin(pos, AsOf, MarketValues));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Broker maintenance margin on all open positions
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Broker Maint. Margin")]
        public decimal BrokerMaintenanceMarginRequirement(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                return Positions.Sum(x => Environment.BrokerMaintenanceMargin(x, AsOf, MarketValues));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Sum of cash and securities, less maintenance margin (essentially)
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Excess Liquidity")]
        public decimal ExcessLiquidity(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                return
                    TotalCashValue(AsOf) +
                    StockValue(AsOf, MarketValues) +
                    BondValue(AsOf, MarketValues) +
                    FundValue(AsOf, MarketValues) +
                    EuroAndAsianOptionsValue(AsOf, MarketValues) -
                    BrokerMaintenanceMarginRequirement(AsOf, MarketValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Reg T maintenance margin on all open positions
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>        
        [StringOutputFormat("Reg T Maint. Margin")]
        public decimal RegTMaintenanceMarginRequirement(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                return Positions.Sum(x => Environment.RegTEndOfDayMargin(x, AsOf, MarketValues));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns a total initial Reg T margin requirement for securities executed on the AsOf date
        /// Margin increases with opening orders (longs buys, short sales) and decreases with closing orders (long sells, short covers)
        /// This will return the net increase/decrease in 
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("Reg T Initial Margin")]
        public decimal RegTInitialMarginRequirement(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                decimal ret = 0m;

                // Get all trades going in the direction of their positions executed today (opening positions), this increases margin requirements
                Positions.ForEach(p =>
                {
                    p.Trades.ForEach(t =>
                    {
                        if (t.TradeDate == AsOf &&
                        t.TradeStatus == TradeStatus.Executed &&
                        (int)t.TradeActionBuySell == (int)p.PositionDirection)
                            ret += Environment.RegTInitialMargin(t);
                    });
                });

                // Get all trades going opposite the direction of their positions executed today (closing positions), this decreases margin requirements
                Positions.ForEach(p =>
                {
                    p.Trades.ForEach(t =>
                    {
                        if (t.TradeDate == AsOf &&
                        t.TradeStatus == TradeStatus.Executed &&
                        (int)t.TradeActionBuySell == -(int)p.PositionDirection)
                            ret -= Environment.RegTInitialMargin(t);
                    });
                });

                return ret;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        // Dictionary stores prior calculations of SMA, so we don't have to recurse through the entire series for each calculation
        private Dictionary<DateTime, decimal> PriorSmaValues = new Dictionary<DateTime, decimal>();

        /// <summary>
        /// Calculates the end-of-day SMA account balance for Reg T purposes
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [StringOutputFormat("End of Day SMA Balance")]
        public decimal SpecialMemorandumAccountBalance(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                // If we have calculated the SMA for this day, return from dictionary
                if (PriorSmaValues.ContainsKey(AsOf))
                    return PriorSmaValues[AsOf];

                // SMA is greater of:
                // SMA1: [Prior Day SMA +/- Change in Day's Cash +/- Today's Trades Initial Margin Requirements]
                // SMA2: [Equity with Loan Value - Reg T Margin]

                // SMA1
                // Assume no cash deposits or withdrawals during program
                // Initial margin is ADDED (subtract a negative) for closing orders (closing open longs/covering shorts), SUBTRACTED for opening orders (opening new longs, selling short)
                // TODO: do we need to count today's commission charges as a change in cash? Probably            
                var SMA1 = SpecialMemorandumAccountBalance(PriorTradingDay(AsOf), MarketValues) - RegTInitialMarginRequirement(AsOf, MarketValues);

                // SMA2
                var SMA2 = EquityWithLoanValue(AsOf, MarketValues) - RegTMaintenanceMarginRequirement(AsOf, MarketValues);

                // SMA is the greater of SMA1 and SMA2
                var SMAfinal = Math.Max(SMA1, SMA2);

                // Save to values dictionary and return
                return PriorSmaValues.AddAndReturn(AsOf, SMAfinal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        [StringOutputFormat("Total Realized PNL")]
        public decimal TotalRealizedPNL(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                return Positions.Sum(x => x.TotalRealizedPnL(AsOf));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        [StringOutputFormat("Total Unrealized PNL")]
        public decimal TotalUnrealizedPNL(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                return Positions.Sum(x => x.TotalUnrealizedPnL(AsOf, MarketValues));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

    }

    /// <summary>
    /// What-If implementation
    /// </summary>
    public partial class Portfolio
    {

        /// <summary>
        /// Creates a 'deep' copy of the portfolio which can be modified without impacting original
        /// </summary>
        /// <returns></returns>
        public Portfolio Copy()
        {
            var ret = new Portfolio(Environment, PortfolioSetup, Strategy);

            Positions.ForEach(x => ret.Positions.Add(x.Copy()));
            PendingTrades.ForEach(x => ret.PendingTrades.Add(x.Copy()));

            ret.PortfolioName = string.Format($"{PortfolioName} (Copy)");

            return ret;
        }

        /// <summary>
        /// Returns a Copy of the current portfolio object reflecting the trade being executed
        /// </summary>
        /// <param name="PotentialTrade">Valid trade with set limit price</param>
        /// <returns></returns>
        public Portfolio WhatIf(Trade PotentialTrade)
        {

            var trd = PotentialTrade.Copy();

            var port = Copy();
            trd.Execute(port, trd.LimitPrice, trd.TradeDate, trd.Quantity);
            return port;

        }

    }

    /// <summary>
    /// Logging and output formatting
    /// </summary>
    public partial class Portfolio
    {
        public List<string> ToStringAllActivity(DateTime AsOf)
        {

            var ret = new List<string>
            {
                // Name of the portfolio followed by a divider
                string.Format($"\r\n{PortfolioName}  activity as of {AsOf.ToShortDateString()}\r\n--------------------")
            };

            Positions.ForEach(p =>
            {
                ret.Add(p.ToString(AsOf));
                ret.AddRange(p.ToStringTrades(AsOf));
            });

            return ret;
        }

        public List<string> ToStringAllAccounting(DateTime AsOf, bool UseOpeningValues = false)
        {
            var ret = new List<string>
            {
                // Name of the portfolio followed by a divider
                string.Format($"\r\n{PortfolioName} balances as of {AsOf.ToShortDateString()}\r\n--------------------")
            };

            foreach (MethodInfo method in GetType().GetMethods())
            {
                var attr = method.GetCustomAttribute(typeof(StringOutputFormatAttribute));
                if (attr == null)
                    continue;

                if (method.GetParameters().Count() == 1)
                    ret.Add(((StringOutputFormatAttribute)attr).ToString(method.Invoke(this, new object[] { AsOf })));
                if (method.GetParameters().Count() == 2)
                    ret.Add(((StringOutputFormatAttribute)attr).ToString(method.Invoke(this, new object[] { AsOf, UseOpeningValues })));
            }

            return ret;
        }
    }


}
