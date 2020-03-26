using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Configuration;
using static Finance.Helpers;

namespace Finance.TradeStrategies
{
    public abstract class TradeStrategyBase
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract PriceBarSize BarSize { get; set; }

        public List<Signal> GenerateSignals(List<Security> SecurityList, DateTime AsOf)
        {
            var ret = new List<Signal>();

            foreach (var sec in SecurityList)
            {
                //if((!sec.Loaded))
                    
                if (!(sec.GetPriceBar(AsOf, BarSize, false) == null))
                {
                    var signal = GenerateSignal(sec, AsOf);
                    if (signal != null)
                        ret.Add(signal);
                }
            }

            return ret;
        }

        protected abstract Signal GenerateSignal(Security security, DateTime AsOf);
        public abstract TradeStrategyBase Copy();
    }

    /// <summary>
    /// Simple long-only entry signal generates when price breaks above high of the last [Period] days
    /// </summary>
    [Include(true)]
    public class TradeStrategy_1 : TradeStrategyBase
    {
        public override string Name => "Trailing Bar High";
        public override string Description => "Long Entry N-Day High Breakout Strategy";

        [SettingsCategory(SettingsType.StrategyParameters, typeof(int))]
        [SettingsDescription("Entry Lookback Period")]
        public int EntryPeriod { get; set; } = 14;

        [SettingsCategory(SettingsType.StrategyParameters, typeof(PriceBarSize))]
        [SettingsDescription("Bar Size Used")]
        public override PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_1()
            {
                EntryPeriod = this.EntryPeriod,
                BarSize = this.BarSize
            };
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {
            var barCollection = security.GetPriceBars(AsOf, EntryPeriod, BarSize, false);

            if (barCollection.Count < EntryPeriod)
                return null;

            var highestClose = (from bar in barCollection select bar.Close).Max();

            if (security.GetPriceBar(AsOf, BarSize).Close > highestClose)
                return new Signal(security, BarSize, AsOf, SignalAction.Buy);
            else
                return null;
        }
    }

    /// <summary>
    /// Simple long-only entry signal generates when price breaks above all time high
    /// </summary>
    [Include(true)]
    public class TradeStrategy_2 : TradeStrategyBase
    {
        public override string Name => "ATH Break";
        public override string Description => "Long Entry all-time-high Breakout Strategy";

        [SettingsCategory(SettingsType.StrategyParameters, typeof(int))]
        [SettingsDescription("Minimum Lookback Bars")]
        public int MinimumPeriod { get; set; } = 90;

        [SettingsCategory(SettingsType.StrategyParameters, typeof(PriceBarSize))]
        [SettingsDescription("Bar Size Used")]
        public override PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_2()
            {
                MinimumPeriod = this.MinimumPeriod,
                BarSize = this.BarSize
            };
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {
            var barCollection = security.GetPriceBars(AsOf, BarSize, true);

            if (barCollection.Count < MinimumPeriod)
                return null;

            var highestClose = (from bar in barCollection select bar.Close).Max();

            if (security.GetPriceBar(AsOf, BarSize).Close > highestClose)
                return new Signal(security, BarSize, AsOf, SignalAction.Buy);
            else
                return null;
        }
    }

    /// <summary>
    /// Simple short-only entry signal generates when price breaks below all time low
    /// </summary>
    [Include(true)]
    public class TradeStrategy_3 : TradeStrategyBase
    {
        public override string Name => "ATL Break";
        public override string Description => "Short Entry all-time-low Breakout Strategy";

        [SettingsCategory(SettingsType.StrategyParameters, typeof(int))]
        [SettingsDescription("Entry Lookback Period")]
        public int MinimumPeriod { get; set; } = 90;

        [SettingsCategory(SettingsType.StrategyParameters, typeof(PriceBarSize))]
        [SettingsDescription("Bar Size Used")]
        public override PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_3()
            {
                MinimumPeriod = this.MinimumPeriod,
                BarSize = this.BarSize
            };
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {
            var bars = security.GetPriceBars(AsOf, BarSize, true);

            if (bars.Count < MinimumPeriod)
                return null;

            var lowestClose = (from bar in bars select bar.Close).Min();

            if (security.GetPriceBar(AsOf, BarSize).Close < lowestClose)
                return new Signal(security, BarSize, AsOf, SignalAction.Sell);
            else
                return null;
        }
    }

    /// <summary>
    /// Simple long-short entry signal generates when price breaks above ATH or below ATL
    /// </summary>
    [Include(true)]
    public class TradeStrategy_4 : TradeStrategyBase
    {
        public override string Name => "ATH-ATL Break";
        public override string Description => "Long/Short Entry all-time-high/low Breakout Strategy";

        [SettingsCategory(SettingsType.StrategyParameters, typeof(int))]
        [SettingsDescription("Entry Lookback Period")]
        public int MinimumPeriod { get; set; } = 90;

        [SettingsCategory(SettingsType.StrategyParameters, typeof(PriceBarSize))]
        [SettingsDescription("Bar Size Used")]
        public override PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_4()
            {
                MinimumPeriod = this.MinimumPeriod,
                BarSize = this.BarSize
            };
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {
            var bars = security.GetPriceBars(AsOf, BarSize, true);

            if (bars.Count < MinimumPeriod)
                return null;

            var lowestClose = (from bar in bars select bar.Close).Min();

            if (security.GetPriceBar(AsOf, BarSize).Close < lowestClose)
                return new Signal(security, BarSize, AsOf, SignalAction.Sell);

            var highestClose = (from bar in bars select bar.Close).Max();

            if (security.GetPriceBar(AsOf, BarSize).Close > highestClose)
                return new Signal(security, BarSize, AsOf, SignalAction.Buy);

            return null;
        }
    }

    /// <summary>
    /// Simple long-short entry signal generates when price breaks above or below trailing N day high/low
    /// </summary>
    [Include(true)]
    public class TradeStrategy_5 : TradeStrategyBase
    {
        public override string Name => "Trailing High-Low Break";
        public override string Description => "Long/Short Entry N-day high/low Breakout Strategy";

        [SettingsCategory(SettingsType.StrategyParameters, typeof(int))]
        [SettingsDescription("Entry Lookback Period")]
        public int EntryPeriod { get; set; } = 14;

        [SettingsCategory(SettingsType.StrategyParameters, typeof(PriceBarSize))]
        [SettingsDescription("Bar Size Used")]
        public override PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_5()
            {
                EntryPeriod = this.EntryPeriod,
                BarSize = this.BarSize
            };
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {
            var bars = security.GetPriceBars(AsOf, EntryPeriod, BarSize);

            if (bars.Count < EntryPeriod)
                return null;

            var lowestClose = (from bar in bars select bar.Close).Min();

            if (security.GetPriceBar(AsOf, BarSize).Close < lowestClose)
                return new Signal(security, BarSize, AsOf, SignalAction.Sell);

            var highestClose = (from bar in bars select bar.Close).Max();

            if (security.GetPriceBar(AsOf, BarSize).Close > highestClose)
                return new Signal(security, BarSize, AsOf, SignalAction.Buy);

            return null;
        }
    }

    [Include(true)]
    public class TradeStrategy_6 : TradeStrategyBase
    {
        public override string Name => "Single Trend Transition";
        public override string Description => "Swing Point Trend Transition Entry";

        [SettingsCategory(SettingsType.StrategyParameters, typeof(int))]
        [SettingsDescription("Swingpoint Bar Count")]
        public int BarCount { get; set; } = 6;

        [SettingsCategory(SettingsType.StrategyParameters, typeof(PriceBarSize))]
        [SettingsDescription("Bar Size Used")]
        public override PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_6()
            {
                BarSize = this.BarSize,
                BarCount = this.BarCount
            };
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {
            security.SetSwingPointsAndTrends(BarCount, BarSize);

            var bar = security.GetPriceBar(AsOf, BarSize);
            if (bar == null || bar.PriorBar == null)
                return null;

            // If this bar does not represent a transition point, no signal
            var priorTrend = bar.PriorBar.GetTrendType(BarCount);
            if (bar.GetTrendType(BarCount) == priorTrend)
                return null;

            switch (bar.GetTrendType(BarCount))
            {
                case TrendQualification.NotSet:
                case TrendQualification.AmbivalentSideways:
                case TrendQualification.SuspectSideways:
                case TrendQualification.ConfirmedSideways:
                    //return new Signal(security, AsOf, SignalAction.CloseIfOpen);
                    return null;
                case TrendQualification.SuspectBullish:
                    if (priorTrend != TrendQualification.ConfirmedBullish)
                        return new Signal(security, BarSize, AsOf, SignalAction.Buy);
                    break;
                case TrendQualification.ConfirmedBullish:
                    return new Signal(security, BarSize, AsOf, SignalAction.Buy);
                case TrendQualification.SuspectBearish:
                    if (priorTrend != TrendQualification.ConfirmedBearish)
                        return new Signal(security, BarSize, AsOf, SignalAction.Sell);
                    break;
                case TrendQualification.ConfirmedBearish:
                    return new Signal(security, BarSize, AsOf, SignalAction.Sell);
            }

            return null;
        }
    }

    /// <summary>
    /// Long-Short strategy using overlapping weekly/daily trends
    /// </summary>
    [Include(true)]
    public class TradeStrategy_7 : TradeStrategyBase
    {
        public override string Name => "Day-Week Trend Align";
        public override string Description => "Swing Point Trend Multiple Timeframes";

        [SettingsCategory(SettingsType.StrategyParameters, typeof(int))]
        [SettingsDescription("Swingpoint Bar Count")]
        int BarCount { get; set; } = 6;

        public override PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_7()
            {
                BarSize = this.BarSize,
                BarCount = this.BarCount
            };
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {
            security.SetSwingPointsAndTrends(BarCount, PriceBarSize.Daily);
            security.SetSwingPointsAndTrends(BarCount, PriceBarSize.Weekly);

            // Today's daily trend
            PriceBar dailyBar = security.GetPriceBar(AsOf, PriceBarSize.Daily);
            TrendQualification dailyTrend = dailyBar.GetTrendType(BarCount);

            // Today's weekly trend
            // We use last week's value until this week is completed on Friday, then we can start using that bar
            PriceBar weeklyBar = security.GetPriceBar(AsOf, PriceBarSize.Weekly);

            if (AsOf != Calendar.LastTradingDayOfWeek(AsOf))
                weeklyBar = weeklyBar?.PriorBar;

            if (weeklyBar == null)
                return null;

            TrendQualification weeklyTrend = weeklyBar.GetTrendType(BarCount);

            TrendAlignment currentTrendAlignment = GetTrendAlignment(dailyTrend, weeklyTrend);

            if (currentTrendAlignment == TrendAlignment.Sideways || currentTrendAlignment == TrendAlignment.Opposing)
                return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.CloseIfOpen, 1);

            // Get the trend from the last period
            if (dailyBar.PriorBar == null)
                return null;

            TrendQualification priorDayTrend = dailyBar.PriorBar.GetTrendType(BarCount);
            // Get the weekly trend from the prior day, which will align with yesterday's daily trend
            PriceBar priorWeek = security.GetPriceBar(Calendar.PriorTradingDay(AsOf), PriceBarSize.Weekly);
            if (priorWeek == null)
                return null;
            TrendQualification priorWeekTrend = priorWeek.GetTrendType(BarCount);

            TrendAlignment priorTrendAlignment = GetTrendAlignment(priorDayTrend, priorWeekTrend);

            if (currentTrendAlignment == priorTrendAlignment)
                return null;

            if (currentTrendAlignment == TrendAlignment.Bullish)
            {
                if (dailyTrend == TrendQualification.ConfirmedBullish && weeklyTrend == TrendQualification.ConfirmedBullish)
                    return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.Buy, 1.0);
                //if (dailyTrend == TrendQualification.SuspectBullish && weeklyTrend == TrendQualification.ConfirmedBullish)
                //    return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.Buy, .75);
                //if (dailyTrend == TrendQualification.ConfirmedBullish && weeklyTrend == TrendQualification.SuspectBullish)
                //    return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.Buy, .50);
                //if (dailyTrend == TrendQualification.SuspectBullish && weeklyTrend == TrendQualification.SuspectBullish)
                //    return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.Buy, .25);
            }
            if (currentTrendAlignment == TrendAlignment.Bearish)
            {
                if (dailyTrend == TrendQualification.ConfirmedBearish && weeklyTrend == TrendQualification.ConfirmedBearish)
                    return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.Sell, 1.0);
                //if (dailyTrend == TrendQualification.SuspectBearish && weeklyTrend == TrendQualification.ConfirmedBearish)
                //    return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.Sell, .75);
                //if (dailyTrend == TrendQualification.ConfirmedBearish && weeklyTrend == TrendQualification.SuspectBearish)
                //    return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.Sell, .50);
                //if (dailyTrend == TrendQualification.SuspectBearish && weeklyTrend == TrendQualification.SuspectBearish)
                //    return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.Sell, .25);
            }

            return null;

        }

    }

    [Include(true)]
    public class TradeStrategy_8 : TradeStrategyBase
    {
        public override string Name => "Weekly Trend Transition";
        public override string Description => "Swing Point Trend Weekly";

        public override PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        [SettingsCategory(SettingsType.StrategyParameters, typeof(int))]
        [SettingsDescription("Swingpoint Bar Count")]
        int BarCount { get; set; } = 6;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_8()
            {
                BarSize = this.BarSize,
                BarCount = this.BarCount
            };
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {
            security.SetSwingPointsAndTrends(BarCount, PriceBarSize.Weekly);

            // Only generate signals after the last close of the week has finalized this bar
            if (AsOf != Calendar.LastTradingDayOfWeek(AsOf))
                return null;

            // Today's weekly trend
            PriceBar weeklyBar = security.GetPriceBar(AsOf, PriceBarSize.Weekly);

            if (weeklyBar == null)
                return null;

            TrendQualification weeklyTrend = weeklyBar.GetTrendType(BarCount);

            // Get the weekly trend from the prior week
            PriceBar priorWeek = weeklyBar.PriorBar;

            TrendQualification priorWeekTrend = priorWeek?.GetTrendType(BarCount) ?? TrendQualification.NotSet;

            if (weeklyTrend == priorWeekTrend)
                return null;

            if (weeklyTrend == TrendQualification.AmbivalentSideways ||
                weeklyTrend == TrendQualification.ConfirmedSideways ||
                weeklyTrend == TrendQualification.SuspectSideways)
                return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.CloseIfOpen, 1);

            if (weeklyTrend == TrendQualification.SuspectBullish ||
                weeklyTrend == TrendQualification.ConfirmedBullish)
                return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.Buy, 1);

            if (weeklyTrend == TrendQualification.SuspectBearish ||
                weeklyTrend == TrendQualification.ConfirmedBearish)
                return new Signal(security, PriceBarSize.Daily, AsOf, SignalAction.Sell, 1);


            return null;

        }

    }

    /// <summary>
    /// Trend transition strategy which examines sector trends as well
    /// </summary>
    [Include(true)]
    public class TradeStrategy_9 : TradeStrategyBase
    {
        public override string Name => @"Single Trend Transition w/ Sectors";
        public override string Description => "Swing Point Trend Transition Entry with Sector Trends";

        [SettingsCategory(SettingsType.StrategyParameters, typeof(int))]
        [SettingsDescription("Swingpoint Bar Count")]
        public int BarCount { get; set; } = 6;

        [SettingsCategory(SettingsType.StrategyParameters, typeof(PriceBarSize))]
        [SettingsDescription("Bar Size Used")]
        public override PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_9()
            {
                BarSize = this.BarSize,
                BarCount = this.BarCount
            };
        }

        private TrendQualification GetSectorTrend(Security security, DateTime AsOf)
        {
            return IndexManager.Instance.GetTrendIndexBySector(security.Sector, this.BarSize).GetStrongestTrend(AsOf);
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {

            TrendQualification sectorTrend = GetSectorTrend(security, AsOf);

            security.SetSwingPointsAndTrends(BarCount, BarSize);

            var bar = security.GetPriceBar(AsOf, BarSize);
            if (bar == null || bar.PriorBar == null)
                return null;

            // If this bar does not represent a transition point, no signal
            var priorTrend = bar.PriorBar.GetTrendType(BarCount);
            if (bar.GetTrendType(BarCount) == priorTrend)
                return null;

            switch (bar.GetTrendType(BarCount))
            {
                case TrendQualification.NotSet:
                case TrendQualification.AmbivalentSideways:
                case TrendQualification.SuspectSideways:
                case TrendQualification.ConfirmedSideways:
                    //return new Signal(security, AsOf, SignalAction.CloseIfOpen);
                    return null;
                case TrendQualification.SuspectBullish:
                    if (priorTrend != TrendQualification.ConfirmedBullish && GetTrendAlignment(sectorTrend, TrendQualification.SuspectBullish) == TrendAlignment.Bullish)
                        return new Signal(security, BarSize, AsOf, SignalAction.Buy);
                    break;
                case TrendQualification.ConfirmedBullish:
                    if (GetTrendAlignment(sectorTrend, TrendQualification.ConfirmedBullish) == TrendAlignment.Bullish)
                        return new Signal(security, BarSize, AsOf, SignalAction.Buy);
                    break;
                case TrendQualification.SuspectBearish:
                    if (priorTrend != TrendQualification.ConfirmedBearish && GetTrendAlignment(sectorTrend, TrendQualification.SuspectBullish) == TrendAlignment.Bearish)
                        return new Signal(security, BarSize, AsOf, SignalAction.Sell);
                    break;
                case TrendQualification.ConfirmedBearish:
                    if (GetTrendAlignment(sectorTrend, TrendQualification.ConfirmedBearish) == TrendAlignment.Bearish)
                        return new Signal(security, BarSize, AsOf, SignalAction.Sell);
                    break;
            }

            return null;
        }
    }


}