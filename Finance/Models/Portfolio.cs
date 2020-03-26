using Finance.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Finance.Calendar;
using static Finance.Helpers;

namespace Finance
{

    public partial class Portfolio
    {

        #region Events

        public event PositionClosedEventHandler PositionClosed;
        private void OnPositionClosed(Position position, DateTime AsOf)
        {
            PositionClosed?.Invoke(this, new PositionDataEventArgs(position, AsOf));
        }

        public event StoplossRequestHandler RequestForNewStop;
        protected void OnRequestForNewStop(Position position, DateTime AsOf)
        {
            RequestForNewStop?.Invoke(this, new PositionDataEventArgs(position, AsOf));
        }

        #endregion

        public string Name { get; }
        public PortfolioSetup PortfolioSetup { get; }
        public List<Position> Positions { get; } = new List<Position>();

        public Portfolio(PortfolioSetup portfolioSetup, string name = "Default Portfolio")
        {
            PortfolioSetup = portfolioSetup ?? throw new ArgumentNullException(nameof(portfolioSetup));
            Name = name;
        }
        public void SetInceptionDate(DateTime date)
        {
            PriorSmaValues.Clear();
        }

        public bool HasOpenPosition(Security security, DateTime AsOf)
        {
            return Positions.Any(x => x.Security == security && x.IsOpen(AsOf));
        }
        public List<Position> GetPositions(PositionStatus positionStatus, DateTime AsOf)
        {
            switch (positionStatus)
            {
                case PositionStatus.Closed:
                    return Positions.Where(pos => !pos.IsOpen(AsOf)).ToList();
                case PositionStatus.Open:
                    return Positions.Where(pos => pos.IsOpen(AsOf)).ToList();
                default:
                    throw new InvalidDataRequestException() { message = "Invalid position status request" };
            }
        }
        public List<Position> GetPositions(DateTime AsOf)
        {
            return Positions.ToList();
        }
        public List<Position> GetPositions(Security security)
        {
            return (from pos in Positions where pos.Security == security select pos).ToList();
        }
        public Position GetPosition(Security security, DateTime AsOf, bool create = false)
        {
            return Positions.Find(x => x.Security == security && x.IsOpen(AsOf)) ?? (create ? Positions.AddAndReturn(new Position(security)) : null);
        }
        public void AddExecutedTrade(Trade trade)
        {
            if (trade.TradeStatus != TradeStatus.Executed)
                throw new InvalidTradeOperationException() { message = "Trade must be marked executed before sending to portfolio" };

            // Get or create current open position
            var position = GetPosition(trade.Security, trade.TradeDate, true);

            position.AddExecutedTrade(trade);

            switch (PortfolioSetup.PortfolioDirection)
            {
                case PortfolioDirection.ShortOnly:
                    if (position.PositionDirection != PositionDirection.ShortPosition)
                        throw new InvalidTradeForPositionException() { message = "Cannot execute trade due to portfolio direction constraints" };
                    break;
                case PortfolioDirection.LongOnly:
                    if (position.PositionDirection != PositionDirection.LongPosition)
                        throw new InvalidTradeForPositionException() { message = "Cannot execute trade due to portfolio direction constraints" };
                    break;
            }

            // Send up a signal that this is a new position and it needs a stoploss assigned
            if (position.ExecutedTrades.Count == 1)
            {
                OnRequestForNewStop(position, trade.TradeDate);
            }

            if (!position.IsOpen(trade.TradeDate))
            {
                OnPositionClosed(position, trade.TradeDate);
            }
        }

        public Portfolio Copy()
        {
            var ret = new Portfolio(PortfolioSetup, string.Format($"{Name} (Copy)"))
            {
                PriorSmaValues = new Dictionary<DateTime, decimal>(PriorSmaValues)
            };
            Positions.ForEach(x => ret.Positions.Add(x.Copy()));

            return ret;
        }
    }

    /// <summary>
    /// Accounting
    /// </summary>
    public partial class Portfolio
    {

        public decimal OpenPositions(DateTime AsOf)
        {
            return Convert.ToDecimal(GetPositions(PositionStatus.Open, AsOf).Count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Total Cash", "$#,##0.00")]
        public decimal TotalCashValue(DateTime AsOf)
        {
            // Starting Cash + (Purchases + Proceeds) + Commissions (as a negative number)
            return PortfolioSetup.InitialCashBalance + TotalCashPurchasesAndProceeds(AsOf) + TotalCommissions(AsOf);
        }

        /// <summary>
        /// Returns a sum total of all purchases (buy long, cover short) and sales (sell long, go short) in the portfolio, as a POSITIVE or NEGATIVE value
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Total Purchases & Proceeds", "$#,##0.00")]
        public decimal TotalCashPurchasesAndProceeds(DateTime AsOf)
        {
            return Positions.Sum(x => x.NetCashImpact(AsOf));
        }

        /// <summary>
        /// Returns a sum total of all commission paid on trades in the portfolio, as a NEGATIVE number
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Total Commission Paid", "$#,##0.00")]
        public decimal TotalCommissions(DateTime AsOf)
        {
            return Positions.Sum(x => TradingEnvironment.Instance.CommissionCharged(x.ExecutedTrades));
        }

        /// <summary>
        /// Returns the sum total of all open positions, POSITIVE or NEGATIVE.  Also referred to as Securities Market Value
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        public decimal StockValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            return LongStockValue(AsOf, MarketValues) + ShortStockValue(AsOf, MarketValues);
        }

        /// <summary>
        /// Another name for StockValue sometimes used
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Securities Market Value", "$#,##0.00")]
        public decimal SecuritiesMarketValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            return StockValue(AsOf, MarketValues);
        }

        /// <summary>
        /// Returns a POSITIVE value
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Long Value", "$#,##0.00")]
        public decimal LongStockValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            return (from p in Positions
                    where p.PositionDirection == PositionDirection.LongPosition
                    where p.IsOpen(AsOf)
                    select p.GrossPositionValue(AsOf, MarketValues)).Sum();
        }

        /// <summary>
        /// Returns a NEGATIVE value
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Short Value", "$#,##0.00")]
        public decimal ShortStockValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            return (from p in Positions
                    where p.PositionDirection == PositionDirection.ShortPosition
                    where p.IsOpen(AsOf)
                    select p.GrossPositionValue(AsOf, MarketValues)).Sum();
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

        [AccountValue("Equity With Loan", "$#,##0.00")]
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
        /// Returns currently available funds
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Available Funds", "$#,##0.00")]
        public decimal AvailableFunds(DateTime AsOf, TimeOfDay MarketValues)
        {
            // Normal formula is ELV-IM, however IBKR counts Initial Margin as all trades, not just the day's. 
            // It is the same as MM; we calculate IM for same-day trades only.
            // return (EquityWithLoanValue(AsOf) - BrokerInitialMarginRequirement(AsOf));
            return (EquityWithLoanValue(AsOf, MarketValues) - BrokerMaintenanceMarginRequirement(AsOf, MarketValues));
        }

        /// <summary>
        /// Returns the ABSOLUTE VALUE sum of all open positions
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Gross Position Value", "$#,##0.00")]
        public decimal GrossPositionValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            return
                LongStockValue(AsOf, MarketValues) +
                Math.Abs(ShortStockValue(AsOf, MarketValues)) +
                LongOptionValue(AsOf, MarketValues) +
                ShortOptionValue(AsOf, MarketValues) +
                FundValue(AsOf, MarketValues);
        }

        [AccountValue("Net Liquidation Value", "$#,##0.00")]
        public decimal NetLiquidationValue(DateTime AsOf, TimeOfDay MarketValues)
        {
            return
                TotalCashValue(AsOf) +
                StockValue(AsOf, MarketValues) +
                LongOptionValue(AsOf, MarketValues) +
                ShortOptionValue(AsOf, MarketValues) +
                FundValue(AsOf, MarketValues);
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
        [AccountValue("Broker Initial Margin", "$#,##0.00")]
        public decimal BrokerInitialMarginRequirement(DateTime AsOf, TimeOfDay MarketValues)
        {
            return Positions.Sum(pos => TradingEnvironment.Instance.BrokerMaintenanceMargin(pos, AsOf, MarketValues));
        }

        /// <summary>
        /// Broker maintenance margin on all open positions
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Broker Maint. Margin", "$#,##0.00")]
        public decimal BrokerMaintenanceMarginRequirement(DateTime AsOf, TimeOfDay MarketValues)
        {
            return Positions.Sum(x => x.IsOpen(AsOf) ? TradingEnvironment.Instance.BrokerMaintenanceMargin(x, AsOf, MarketValues) : 0);
        }

        /// <summary>
        /// Sum of cash and securities, less maintenance margin (essentially)
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Excess Liquidity", "$#,##0.00")]
        public decimal ExcessLiquidity(DateTime AsOf, TimeOfDay MarketValues)
        {
            return
                TotalCashValue(AsOf) +
                StockValue(AsOf, MarketValues) +
                BondValue(AsOf, MarketValues) +
                FundValue(AsOf, MarketValues) +
                EuroAndAsianOptionsValue(AsOf, MarketValues) -
                BrokerMaintenanceMarginRequirement(AsOf, MarketValues);
        }

        /// <summary>
        /// Reg T maintenance margin on all open positions
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>        
        [AccountValue("Reg T Maint. Margin", "$#,##0.00")]
        public decimal RegTMaintenanceMarginRequirement(DateTime AsOf, TimeOfDay MarketValues)
        {
            return Positions.Sum(x => x.IsOpen(AsOf) ? TradingEnvironment.Instance.RegTEndOfDayMargin(x, AsOf, MarketValues) : 0);
        }

        /// <summary>
        /// Returns a total initial Reg T margin requirement for securities executed on the AsOf date
        /// Margin increases with opening orders (longs buys, short sales) and decreases with closing orders (long sells, short covers)
        /// This will return the net increase/decrease in 
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("Reg T Initial Margin", "$#,##0.00")]
        public decimal RegTInitialMarginRequirement(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                decimal ret = 0m;

                // Get all trades going in the direction of their positions executed today (opening positions), this increases margin requirements
                Positions.ForEach(p =>
                {
                    p.ExecutedTrades.ForEach(t =>
                    {
                        if (t.TradeDate == AsOf &&
                        t.TradeStatus == TradeStatus.Executed &&
                        (int)t.TradeActionBuySell == (int)p.PositionDirection)
                            ret += TradingEnvironment.Instance.RegTInitialMargin(t);
                    });
                });

                // Get all trades going opposite the direction of their positions executed today (closing positions), this decreases margin requirements
                Positions.ForEach(p =>
                {
                    p.ExecutedTrades.ForEach(t =>
                    {
                        if (t.TradeDate == AsOf &&
                        t.TradeStatus == TradeStatus.Executed &&
                        (int)t.TradeActionBuySell == -(int)p.PositionDirection)
                            ret -= TradingEnvironment.Instance.RegTInitialMargin(t);
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
        protected Dictionary<DateTime, decimal> PriorSmaValues = new Dictionary<DateTime, decimal>();

        /// <summary>
        /// Calculates the end-of-day SMA account balance for Reg T purposes
        /// </summary>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        [AccountValue("End of Day SMA Balance", "$#,##0.00")]
        public decimal SpecialMemorandumAccountBalance(DateTime AsOf, TimeOfDay MarketValues)
        {
            try
            {
                // If we have calculated the SMA for this day, return from dictionary
                if (PriorSmaValues.ContainsKey(AsOf))
                    return PriorSmaValues[AsOf];

                // Set initial value
                if (AsOf <= PortfolioSetup.InceptionDate)
                    PriorSmaValues.Add(Calendar.PriorTradingDay(PortfolioSetup.InceptionDate), PortfolioSetup.InitialCashBalance);

                // SMA is greater of:
                // SMA1: [Prior Day SMA +/- Change in Day's Cash +/- Today's Trades Initial Margin Requirements]
                // SMA2: [Equity with Loan Value - Reg T Margin]

                // SMA1
                // Assume no cash deposits or withdrawals during program
                // Initial margin is ADDED (subtract a negative) for closing orders (closing open longs/covering shorts), SUBTRACTED for opening orders (opening new longs, selling short)           
                var SMA1 = SpecialMemorandumAccountBalance(PriorTradingDay(AsOf), MarketValues)
                    - RegTInitialMarginRequirement(AsOf, MarketValues);

                // SMA2
                var SMA2 = EquityWithLoanValue(AsOf, MarketValues)
                    - RegTMaintenanceMarginRequirement(AsOf, MarketValues);

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

        [AccountValue("Total Realized PNL", "$#,##0.00")]
        public decimal TotalRealizedPNL(DateTime AsOf, TimeOfDay MarketValues)
        {
            return Positions.Sum(x => x.TotalRealizedPnL(AsOf));
        }

        [AccountValue("Total Unrealized PNL", "$#,##0.00")]
        public decimal TotalUnrealizedPNL(DateTime AsOf, TimeOfDay MarketValues)
        {
            return Positions.Sum(x => x.TotalUnrealizedPnL(AsOf, MarketValues));
        }

        public decimal GetByAccountingSeriesValue(AccountingSeriesValue value, DateTime AsOf, TimeOfDay MarketValues = TimeOfDay.MarketEndOfDay)
        {
            //
            // Invoke accounting method by name provided in AccountingSeriesValue
            //
            var method = typeof(Portfolio).GetMethod(Enum.GetName(typeof(AccountingSeriesValue), value));

            switch (method.GetParameters().Count())
            {
                case 1:
                    return (decimal)method.Invoke(this, new object[] { AsOf });
                case 2:
                    return (decimal)method.Invoke(this, new object[] { AsOf, MarketValues });
                default:
                    return 0;
            }
        }

    }

    /// <summary>
    /// Logging and output formatting
    /// </summary>
    public partial class Portfolio
    {

    }

}
