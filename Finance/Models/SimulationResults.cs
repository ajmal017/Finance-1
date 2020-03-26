using Finance.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Calendar;
using static Finance.Helpers;

namespace Finance
{
    /// <summary>
    /// Class containing various fields of information for a simulation
    /// </summary>
    public partial class SimulationResults
    {
        [Browsable(false)]
        private Portfolio portfolio { get; }
        [Browsable(false)]
        private (DateTime start, DateTime end) SimulationTimeSpan { get; }

        public SimulationResults(Portfolio portfolio, (DateTime, DateTime) simulationTimeSpan)
        {
            this.portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
            SimulationTimeSpan = simulationTimeSpan;
        }

        [DisplayFormat("###")]
        private int SimulationTotalDays
        {
            get
            {
                return Convert.ToInt32((SimulationTimeSpan.Item2 - SimulationTimeSpan.Item1).TotalDays);
            }
        }

        [DisplayFormat("0.00%")]
        public decimal TotalReturnPercent
        {
            get
            {
                var endBalance = portfolio.NetLiquidationValue(SimulationTimeSpan.Item2, TimeOfDay.MarketEndOfDay);
                var startingBalance = portfolio.PortfolioSetup.InitialCashBalance;

                var percentReturn = (endBalance - startingBalance) / startingBalance;

                return percentReturn;
            }
        }

        [DisplayFormat("0.00%")]
        public decimal AnnualizedReturnPercent
        {
            get
            {
                var returnPercent = Convert.ToDouble(TotalReturnPercent);
                var dayHeldFraction = (365.0 / SimulationTotalDays);

                return (Math.Pow((1 + returnPercent), (dayHeldFraction)) - 1).ToDecimal();
            }
        }

        /// <summary>
        /// Returns a list of whole month returns, excludes partial months at the start or end of simulation
        /// </summary>
        /// <returns></returns>

        [Browsable(false)]
        public List<decimal> MonthlyReturnsPercents()
        {

            var d1 = Calendar.FirstTradingDayOfMonth(SimulationTimeSpan.Item1);
            if (d1 < SimulationTimeSpan.Item1)
                d1 = Calendar.FirstTradingDayOfMonth(d1.AddMonths(1));

            var ret = new List<decimal>
            {
                portfolio.NetLiquidationValue(d1, TimeOfDay.MarketOpen)
            };

            d1 = d1 = Calendar.FirstTradingDayOfMonth(d1.AddMonths(1));
            while (d1 <= SimulationTimeSpan.Item2)
            {
                ret.Add(portfolio.NetLiquidationValue(d1, TimeOfDay.MarketEndOfDay));
                d1 = d1 = Calendar.FirstTradingDayOfMonth(d1.AddMonths(1));
            }

            var ret2 = new List<decimal>(ret);
            for (int i = 1; i < ret.Count; i++)
            {
                // Change the current value to a percentage change over the last month's value
                ret2[i] = (ret[i] - ret[i - 1]) / ret[i - 1];
            }
            // Remove the first value
            ret2.RemoveAt(0);

            return ret2;
        }

        [DisplayFormat("0.00%")]
        public decimal MaxMonthlyReturnPercent
        {
            get
            {
                return MonthlyReturnsPercents().Max();
            }
        }
        [DisplayFormat("0.00%")]
        public decimal MinMonthlyReturnPercent
        {
            get
            {
                return MonthlyReturnsPercents().Min();
            }
        }

        /// <summary>
        /// Returns a list of daily end of day account values
        /// </summary>
        /// <returns></returns>
        [Browsable(false)]
        public List<decimal> DailyEquityBalances()
        {
            var ret = new List<decimal>();

            var d1 = SimulationTimeSpan.Item1;
            while (d1 <= SimulationTimeSpan.Item2)
            {
                ret.Add(portfolio.NetLiquidationValue(d1, TimeOfDay.MarketEndOfDay));
                d1 = NextTradingDay(d1);
            }

            return ret;
        }

        [DisplayFormat("$0.00")]
        public decimal MaxDrawdownDollars
        {
            get
            {
                var dailyBalances = DailyEquityBalances();
                decimal highBalance = dailyBalances[0];
                decimal maxDraw = 0m;

                foreach (var balance in dailyBalances)
                {
                    if (balance < highBalance)
                    {
                        if (balance - highBalance < maxDraw)
                            maxDraw = balance - highBalance;
                    }
                    else
                    {
                        highBalance = balance;
                    }
                }

                return maxDraw;
            }
        }
        [DisplayFormat("0.00%")]
        public decimal MaxDrawdownPercent
        {
            get
            {
                var dailyBalances = DailyEquityBalances();
                decimal highBalance = dailyBalances[0];
                decimal maxDraw = 0m;

                foreach (var balance in dailyBalances)
                {
                    if (balance < highBalance)
                    {
                        var ddPercent = (balance - highBalance) / highBalance;
                        if (ddPercent < maxDraw)
                            maxDraw = ddPercent;
                    }
                    else
                    {
                        highBalance = balance;
                    }
                }

                return maxDraw;
            }
        }
        [DisplayFormat("###")]
        public int MaxDrawdownRecoveryLengthDays
        {
            // Longest time between high values

            get
            {
                var dailyBalances = DailyEquityBalances();
                decimal highBalance = dailyBalances[0];
                int maxDraw = 0;
                int currentDraw = 0;

                foreach (var balance in dailyBalances)
                {
                    if (balance < highBalance)
                    {
                        currentDraw += 1;
                        if (currentDraw > maxDraw)
                            maxDraw = currentDraw;
                    }
                    else
                    {
                        currentDraw = 0;
                        highBalance = balance;
                    }
                }

                return maxDraw;
            }
        }
        [DisplayFormat("$0.00")]
        public decimal MinAccountEquity
        {
            get
            {
                return DailyEquityBalances().Min();
            }
        }
        [DisplayFormat("$0.00")]
        public decimal MaxAccountEquity
        {
            get
            {
                return DailyEquityBalances().Max();
            }
        }
        [DisplayFormat("###")]
        public decimal MaxOpenPositions
        {
            get
            {
                var maxPos = 0;
                var d1 = SimulationTimeSpan.Item1;
                while (d1 <= SimulationTimeSpan.Item2)
                {
                    var openPos = portfolio.GetPositions(PositionStatus.Open, d1).Count;
                    if (openPos > maxPos)
                        maxPos = openPos;
                    d1 = NextTradingDay(d1);
                }

                return maxPos;
            }
        }
        [DisplayFormat("0.00%")]
        public decimal WinningPositionPercent
        {
            get
            {
                var numWin = (from pos in portfolio.GetPositions(PositionStatus.Closed, SimulationTimeSpan.Item2)
                              where pos.TotalReturnPercentage(SimulationTimeSpan.Item2) > 0
                              select pos.TotalReturnPercentage(SimulationTimeSpan.Item2)).ToList();

                var closedPositionCount = portfolio.GetPositions(PositionStatus.Closed, SimulationTimeSpan.Item2).Count();

                return closedPositionCount == 0 ? 0 : Convert.ToDecimal(numWin.Count / closedPositionCount);

            }
        }
        [DisplayFormat("0.00%")]
        public decimal AverageWinningPositionReturnPercent
        {
            get
            {
                var values = (from pos in portfolio.GetPositions(PositionStatus.Closed, SimulationTimeSpan.Item2)
                              where pos.TotalReturnPercentage(SimulationTimeSpan.Item2) > 0
                              select pos.TotalReturnPercentage(SimulationTimeSpan.Item2)).ToList();
                if (values.Count() > 0)
                    return values.Average();
                else return 0;
            }
        }
        [DisplayFormat("0.00%")]
        public decimal AverageLosingPositionReturnPercent
        {
            get
            {
                var values = (from pos in portfolio.GetPositions(PositionStatus.Closed, SimulationTimeSpan.Item2)
                              where pos.TotalReturnPercentage(SimulationTimeSpan.Item2) <= 0
                              select pos.TotalReturnPercentage(SimulationTimeSpan.Item2)).ToList();

                if (values.Count() > 0)
                    return values.Average();
                else return 0;
            }
        }

        public decimal UnrealizedPnlEndOfSimulation
        {
            get
            {
                return portfolio.GetPositions(PositionStatus.Open, SimulationTimeSpan.Item2).Sum(x => x.TotalUnrealizedPnL(SimulationTimeSpan.Item2, TimeOfDay.MarketEndOfDay));
            }
        }

        [Browsable(false)]
        public decimal AveragePnlCapturedPercentOfMax { get => 0m; }

        [DisplayFormat("###")]
        public int LongestPositionHeldDays
        {
            get
            {
                var ret = portfolio.GetPositions(SimulationTimeSpan.Item2);
                if (ret.Count > 0)
                    return ret.Max(x => x.DaysHeld(SimulationTimeSpan.Item2));

                return 0;
            }
        }
        [DisplayFormat("###")]
        public decimal AveragePositionHeldDays
        {
            get
            {
                var ret = portfolio.GetPositions(SimulationTimeSpan.Item2);
                if (ret.Count > 0)
                    return Convert.ToDecimal(ret.Average(x => x.DaysHeld(SimulationTimeSpan.Item2)));

                return 0;
            }
        }

        [DisplayFormat("$0.00")]
        public decimal TotalCommissions
        {
            get
            {
                var ret = portfolio.GetPositions(SimulationTimeSpan.Item2);
                if (ret.Count > 0)
                    return ret.Sum(x => x.TotalCommissionPaid(SimulationTimeSpan.Item2));

                return 0;
            }
        }

        [DisplayFormat("###")]
        public int TotalTradesExecuted
        {
            get
            {
                var ret = portfolio.GetPositions(SimulationTimeSpan.Item2);
                if (ret.Count > 0)
                    return ret.Sum(x => x.ExecutedTrades.Count);

                return 0;
            }
        }

        #region By Sector Performance

        public List<SectorPerformanceResults> SectorResults()
        {
            var ret = new List<SectorPerformanceResults>();
            foreach (string sector in Settings.Instance.MarketSectors)
            {
                var val = ret.AddAndReturn(new SectorPerformanceResults(sector, SimulationTimeSpan.end));
                portfolio.GetPositions(SimulationTimeSpan.end).Where(p => p.Security.Sector == sector)
                    .ToList().ForEach(p => val.AddPosition(p));
            }

            //ret.RemoveAll(s => s.PositionCount == 0);

            return ret;
        }

        #endregion
    }

    /// <summary>
    /// String output
    /// </summary>
    public partial class SimulationResults
    {
        public new List<string> ToString()
        {

            var ret = new List<string>
            {
                string.Format($"Total days in simulation: {SimulationTotalDays}"),
                string.Format($"Total Return: {TotalReturnPercent:0.00%}"),
                string.Format($"Annualized Return: { AnnualizedReturnPercent:0.00%}"),
                string.Format($"Returns by month (full months only):")
            };
            string monRet = "";
            foreach (var m in MonthlyReturnsPercents())
            {
                monRet += string.Format($"{m:0.00%}, ");
            }
            monRet.TrimEnd(',');
            ret.Add(string.Format($"{monRet}"));

            ret.Add(string.Format($"Max: {MaxMonthlyReturnPercent:0.00%}  Min: {MinMonthlyReturnPercent: 0.00%}"));

            ret.Add(string.Format($"Maximum Drawdown: {MaxDrawdownDollars:$0.00}  ({MaxDrawdownPercent:0.00%}) Duration: {MaxDrawdownRecoveryLengthDays} days"));

            ret.Add(string.Format($"Account equity:  Max {MaxAccountEquity:$0.00}  Min {MinAccountEquity:$0.00}"));

            ret.Add(string.Format($"Maximum open positions: {MaxOpenPositions}"));

            ret.Add(string.Format($"Winning positions: {WinningPositionPercent:0.00%}"));

            ret.Add(string.Format($"Average win: {AverageWinningPositionReturnPercent:0.00%}  Average loss: {AverageLosingPositionReturnPercent:0.00%}"));

            ret.Add(string.Format($"Total Unrealized PNL at end of simulation:{UnrealizedPnlEndOfSimulation:$0.00}"));

            ret.Add(string.Format($"Maximum position held length: {LongestPositionHeldDays}"));

            ret.Add(string.Format($"Total Commission Paid: {TotalCommissions:$0.00}"));

            ret.Add(string.Format($"Total # of trades: {TotalTradesExecuted}"));

            return ret;
        }
    }

    public class SectorPerformanceResults
    {
        private List<Position> SectorPositions { get; set; }

        public string Sector { get; set; }
        private DateTime EndDate { get; }

        public int PositionCount => SectorPositions.Count;

        public int WinningPositions => WinsLosses().wins;
        public int LosingPositions => WinsLosses().losses;

        public decimal AverageWinningPosition => AverageWinLoss().avgWin;
        public decimal AverageLosingPosition => AverageWinLoss().avgLoss;

        public SectorPerformanceResults(string sector, DateTime endDate)
        {
            EndDate = endDate;
            Sector = sector ?? throw new ArgumentNullException(nameof(sector));
            SectorPositions = new List<Position>();
        }

        public void AddPosition(Position position)
        {
            if (!SectorPositions.Contains(position))
                SectorPositions.Add(position);
        }

        private (int wins, int losses) WinsLosses()
        {
            var wins = SectorPositions.Where(x =>
                x.TotalRealizedPnL(EndDate) + x.TotalUnrealizedPnL(EndDate, TimeOfDay.MarketEndOfDay) > 0).Count();

            var losses = SectorPositions.Where(x =>
                x.TotalRealizedPnL(EndDate) + x.TotalUnrealizedPnL(EndDate, TimeOfDay.MarketEndOfDay) <= 0).Count();

            return (wins, losses);
        }
        private (decimal avgWin, decimal avgLoss) AverageWinLoss()
        {
            var avgWin = (from pos in SectorPositions
                          where pos.TotalReturnPercentage(EndDate) > 0
                          select pos.TotalReturnPercentage(EndDate)).Average();

            var avgLoss = (from pos in SectorPositions
                           where pos.TotalReturnPercentage(EndDate) <= 0
                           select pos.TotalReturnPercentage(EndDate)).Average();

            return (avgWin, avgLoss);
        }
    }
}
