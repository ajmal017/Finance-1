using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Helpers;

namespace Finance.PositioningStrategies
{
    public abstract class PositioningStrategyBase
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract int NewPositionSize(Portfolio portfolio, Signal IndicatedTrade, DateTime AsOf, decimal initialRiskPercent);
        public abstract Trade NewStoploss(Position position, DateTime AsOf);
        public abstract decimal UpdatePositionStoplossPrice(Position position, Trade currentStop, DateTime AsOf);

        public abstract PositioningStrategyBase Copy();
    }

    [Include(true)]
    public class AtrSizingAndStoploss : PositioningStrategyBase
    {
        public override string Name => "ATR";
        public override string Description => "Position Sizing and Stoploss calculated as multiple of ATR";

        [SettingsCategory(SettingsType.PositionManagementParameters, typeof(int))]
        [SettingsDescription("Stoploss ATR Period")]
        public int Stoploss_ATR_Period { get; set; } = 14;

        [SettingsCategory(SettingsType.PositionManagementParameters, typeof(decimal))]
        [SettingsDescription("Stoploss ATR Multiple")]
        public decimal Stoploss_ATR_Multiple { get; set; } = 8.0m;

        [SettingsCategory(SettingsType.PositionManagementParameters, typeof(decimal))]
        [SettingsDescription(@"Stoploss Creep Percent/Day")]
        public decimal Stoploss_Creep_Percent { get; set; } = 0.00m;

        public override int NewPositionSize(Portfolio portfolio, Signal IndicatedTrade, DateTime AsOf, decimal initialRiskPercent)
        {
            //
            // Calculate the total $ risk we are taking based on account equity and indicated risk perentage
            //
            decimal riskDollarsTotal = portfolio.EquityWithLoanValue(AsOf, TimeOfDay.MarketEndOfDay) * initialRiskPercent;

            //
            // Get the last ATR value of the security
            //
            decimal securityLastATR = IndicatedTrade.Security.GetPriceBarOrLastPrior(AsOf, PriceBarSize.Daily, 1).AverageTrueRange(Stoploss_ATR_Period);

            // If the ATR is 0, cancel the trade
            if (securityLastATR == 0)
                throw new CancelTradeException("ATR is zero", IndicatedTrade.Security, AsOf, GetCurrentMethod());

            //
            // Calculate how many dollars are at risk per share
            //
            decimal riskDollarsPerShare = Stoploss_ATR_Multiple * securityLastATR;

            //
            // Calculate the position size as a multiple of risk per share
            //
            int positionSize = Convert.ToInt32(Math.Round((riskDollarsTotal / riskDollarsPerShare), 0));

            return positionSize;
        }
        public override Trade NewStoploss(Position position, DateTime AsOf)
        {
            //
            // This method only generates a stoploss for a new position on the initial trade date
            //
            if (position.ExecutedTrades.Count > 1)
                throw new UnknownErrorException();

            //
            // The average cost of this new position is the entry price
            //
            decimal entryPrice = position.AverageCost(AsOf);

            //
            // The last valid ATR, which is the day prior to AsOf assuming the trade was executed today
            //
            decimal securityLastATR = position.Security.GetPriceBarOrLastPrior(Calendar.PriorTradingDay(AsOf), PriceBarSize.Daily, 1).AverageTrueRange(Stoploss_ATR_Period);

            //
            // Calculate how many dollars are at risk per share
            //
            decimal riskDollarsPerShare = Stoploss_ATR_Multiple * securityLastATR;

            //
            // The initial stop price is the entry price plus/minus the risk dollars per share
            //
            decimal stopPrice = (entryPrice - (position.PositionDirection.ToInt() * riskDollarsPerShare));

            //
            // If the calculated stop price is less than or equal to 0, set a minimum value (this probably needs a look)
            //
            if (stopPrice <= 0)
                stopPrice = 0.01m;

            //
            // Generate the initial stoploss trade
            //
            var stop = new Trade(position.Security, (TradeActionBuySell)(-position.PositionDirection.ToInt()), Math.Abs(position.Size(AsOf)), TradeType.Stop, 0, stopPrice)
            {
                TradeDate = AsOf
            };

            return stop;

        }
        public override decimal UpdatePositionStoplossPrice(Position position, Trade currentStop, DateTime AsOf)
        {
            //
            // This method updates a position's stoploss after market close on AsOf
            //
            return TrailingStop(position, currentStop, AsOf);
        }
        private decimal TrailingStop(Position position, Trade currentStop, DateTime AsOf)
        {
            //
            // Get the last price bar of the position
            //
            PriceBar bar = position.Security.GetPriceBarOrLastPrior(AsOf, PriceBarSize.Daily, 1);
            decimal lastClose = bar.Close;

            //
            // Get the last ATR, which is the AsOf date
            //
            var securityLastAtr = bar.AverageTrueRange(Stoploss_ATR_Period);

            //
            // Calculate the stop price baseline
            //
            decimal riskDollarsPerShare = (securityLastAtr * Stoploss_ATR_Multiple);
            decimal stopPrice = (lastClose - (position.PositionDirection.ToInt() * riskDollarsPerShare));

            if (stopPrice <= 0)
                stopPrice = 0.01m;

            //
            // If a creeping stop is enabled, calculate the new stop price based on change from the existing stop
            //
            decimal adjustedBaseStop = currentStop.StopPrice;
            if (Stoploss_Creep_Percent > 0)
            {
                switch (position.PositionDirection)
                {
                    case PositionDirection.LongPosition:
                        // Increase base stop
                        adjustedBaseStop *= (1 + Stoploss_Creep_Percent);
                        break;
                    case PositionDirection.ShortPosition:
                        // Decrease base stop 
                        adjustedBaseStop *= (1 - Stoploss_Creep_Percent);
                        break;
                }
            }

            //
            // Compare the new baseline stop vs the adjusted prior stop, and select whichever is more conservative
            //
            switch (position.PositionDirection)
            {
                case PositionDirection.NotSet:
                    throw new UnknownErrorException();
                case PositionDirection.LongPosition:
                    return Math.Max(stopPrice, adjustedBaseStop);
                case PositionDirection.ShortPosition:
                    return Math.Min(stopPrice, adjustedBaseStop);
                default:
                    throw new UnknownErrorException();
            }
        }

        public override PositioningStrategyBase Copy()
        {
            var ret = new AtrSizingAndStoploss();

            // Copy all values which are marked with a settings tag
            foreach (var prop in GetType().GetProperties())
            {
                if (Attribute.IsDefined(prop, typeof(SettingsCategoryAttribute)))
                {
                    prop.SetValue(ret, prop.GetValue(this));
                }
            }

            return ret;
        }
    }

    [Include(true)]
    public class SwingPointSizingAndStoploss : PositioningStrategyBase
    {
        /*
         *  Places a stoploss outside the first prior swingpoint which would contraindicate the current trend
         *  ie, for a LONG position, the stoploss would be placed just below the last SPL, as any move below would terminate the trend
         */

        public override string Name => "Swing Point";
        public override string Description => "Position Sizing and Stoploss calculated from latest swing point";

        [SettingsCategory(SettingsType.PositionManagementParameters, typeof(PriceBarSize))]
        [SettingsDescription("Price Bar Size")]
        public PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        [SettingsCategory(SettingsType.PositionManagementParameters, typeof(int))]
        [SettingsDescription("Bar Count for Swingpoint Calculations")]
        public int BarCount { get; set; } = 6;

        [SettingsCategory(SettingsType.PositionManagementParameters, typeof(decimal))]
        [SettingsDescription(@"Initial Stop Buffer %")]
        public decimal Initial_Buffer_Percent { get; set; } = 0.01m;

        [SettingsCategory(SettingsType.PositionManagementParameters, typeof(decimal))]
        [SettingsDescription(@"Stoploss Creep Percent/Day")]
        public decimal Stoploss_Creep_Percent { get; set; } = 0.00m;

        [SettingsCategory(SettingsType.PositionManagementParameters, typeof(int))]
        [SettingsDescription("Backup Stoploss ATR Period")]
        public int Stoploss_ATR_Period { get; set; } = 14;

        [SettingsCategory(SettingsType.PositionManagementParameters, typeof(decimal))]
        [SettingsDescription("Backup Stoploss ATR Multiple")]
        public decimal Stoploss_ATR_Multiple { get; set; } = 8.0m;

        private AtrSizingAndStoploss BackupStoploss = new AtrSizingAndStoploss();

        public override int NewPositionSize(Portfolio portfolio, Signal IndicatedTrade, DateTime AsOf, decimal initialRiskPercent)
        {
            IndicatedTrade.Security.SetSwingPointsAndTrends(BarCount, BarSize);

            decimal riskDollarsTotal = portfolio.EquityWithLoanValue(AsOf, TimeOfDay.MarketEndOfDay) * initialRiskPercent;

            // First prior swingpoint is first prior that is REALIZED at the time of signal, so we have to look at least BarCount bars back
            PriceBar firstPriorSwingPoint = null;
            switch (BarSize)
            {
                case PriceBarSize.Daily:
                    firstPriorSwingPoint = IndicatedTrade.Security.GetPriceBar(Calendar.PriorTradingDay(AsOf, BarCount), BarSize);
                    break;
                case PriceBarSize.Weekly:
                    firstPriorSwingPoint = IndicatedTrade.Security.GetPriceBar(Calendar.PriorTradingWeekStart(AsOf, BarCount), BarSize);
                    break;
                case PriceBarSize.Monthly:
                    firstPriorSwingPoint = IndicatedTrade.Security.GetPriceBar(Calendar.PriorTradingMonthStart(AsOf, BarCount), BarSize);
                    break;
                case PriceBarSize.Quarterly:
                    firstPriorSwingPoint = IndicatedTrade.Security.GetPriceBar(Calendar.PriorTradingMonthStart(AsOf, BarCount), BarSize);
                    break;
                default:
                    break;
            }

            decimal stopPrice = -1m;
            decimal expExecutionPx = IndicatedTrade.Security.GetPriceBar(AsOf, BarSize).Close;

            switch (IndicatedTrade.SignalAction)
            {
                case SignalAction.Sell:
                    // First prior swingpoint high
                    while ((firstPriorSwingPoint.GetSwingPointType(BarCount) & SwingPointType.SwingPointHigh) == 0)
                    {
                        firstPriorSwingPoint = firstPriorSwingPoint.PriorBar;
                    }
                    stopPrice = firstPriorSwingPoint.High + .01m;
                    break;
                case SignalAction.Buy:
                    // First prior swingpoint low
                    while ((firstPriorSwingPoint.GetSwingPointType(BarCount) & SwingPointType.SwingPointLow) == 0)
                    {
                        firstPriorSwingPoint = firstPriorSwingPoint.PriorBar;
                    }
                    stopPrice = firstPriorSwingPoint.Low - .01m;
                    break;
            }

            // If the last valid swingpoint is on the wrong side of our trade (above exp trade price for a long, below a short), use an ATR atop
            switch (IndicatedTrade.SignalAction)
            {
                case SignalAction.Sell:
                    if (stopPrice >= expExecutionPx)
                    {
                        BackupStoploss.Stoploss_ATR_Multiple = this.Stoploss_ATR_Multiple;
                        BackupStoploss.Stoploss_ATR_Period = this.Stoploss_ATR_Period;
                        return BackupStoploss.NewPositionSize(portfolio, IndicatedTrade, AsOf, initialRiskPercent);
                    }
                    break;
                case SignalAction.Buy:
                    if (stopPrice <= expExecutionPx)
                    {
                        BackupStoploss.Stoploss_ATR_Multiple = this.Stoploss_ATR_Multiple;
                        BackupStoploss.Stoploss_ATR_Period = this.Stoploss_ATR_Period;
                        return BackupStoploss.NewPositionSize(portfolio, IndicatedTrade, AsOf, initialRiskPercent);
                    }
                    break;
            }

            decimal initialBuffer = expExecutionPx * initialRiskPercent;
            decimal riskDollarsPerShare = Math.Abs(IndicatedTrade.Security.GetPriceBar(AsOf, BarSize).Close - stopPrice);

            if (riskDollarsPerShare < initialBuffer)
                riskDollarsPerShare = initialBuffer;

            int positionSize = Convert.ToInt32(Math.Round((riskDollarsTotal / riskDollarsPerShare), 0));

            return positionSize;
        }
        public override Trade NewStoploss(Position position, DateTime AsOf)
        {
            position.Security.SetSwingPointsAndTrends(BarCount, BarSize);

            if (position.ExecutedTrades.Count > 1)
                throw new UnknownErrorException();

            // First prior swingpoint is first prior that is REALIZED at the time of signal, so we have to look at least BarCount bars back
            PriceBar firstPriorSwingPoint = null;
            switch (BarSize)
            {
                case PriceBarSize.Daily:
                    firstPriorSwingPoint = position.Security.GetPriceBar(Calendar.PriorTradingDay(AsOf, BarCount), BarSize);
                    break;
                case PriceBarSize.Weekly:
                    firstPriorSwingPoint = position.Security.GetPriceBar(Calendar.PriorTradingWeekStart(AsOf, BarCount), BarSize);
                    break;
                case PriceBarSize.Monthly:
                    firstPriorSwingPoint = position.Security.GetPriceBar(Calendar.PriorTradingMonthStart(AsOf, BarCount), BarSize);
                    break;
                case PriceBarSize.Quarterly:
                    firstPriorSwingPoint = position.Security.GetPriceBar(Calendar.PriorTradingMonthStart(AsOf, BarCount), BarSize);
                    break;
                default:
                    break;
            }

            decimal stopPrice = -1m;
            decimal expExecutionPx = position.Security.GetPriceBar(AsOf, BarSize).Close;

            switch (position.PositionDirection)
            {
                case PositionDirection.ShortPosition:
                    // First prior swingpoint high
                    while ((firstPriorSwingPoint.GetSwingPointType(BarCount) & SwingPointType.SwingPointHigh) == 0)
                    {
                        firstPriorSwingPoint = firstPriorSwingPoint.PriorBar;
                    }
                    stopPrice = firstPriorSwingPoint.High + .01m;
                    break;
                case PositionDirection.LongPosition:
                    // First prior swingpoint low
                    while ((firstPriorSwingPoint.GetSwingPointType(BarCount) & SwingPointType.SwingPointLow) == 0)
                    {
                        firstPriorSwingPoint = firstPriorSwingPoint.PriorBar;
                    }
                    stopPrice = firstPriorSwingPoint.Low - .01m;
                    break;
            }

            // If the last valid swingpoint is on the wrong side of our trade (above exp trade price for a long, below a short), use an ATR atop
            switch (position.PositionDirection)
            {
                case PositionDirection.LongPosition:
                    if (stopPrice >= expExecutionPx)
                    {
                        BackupStoploss.Stoploss_ATR_Multiple = this.Stoploss_ATR_Multiple;
                        BackupStoploss.Stoploss_ATR_Period = this.Stoploss_ATR_Period;
                        return BackupStoploss.NewStoploss(position, AsOf);
                    }
                    break;
                case PositionDirection.ShortPosition:
                    if (stopPrice <= expExecutionPx)
                    {
                        BackupStoploss.Stoploss_ATR_Multiple = this.Stoploss_ATR_Multiple;
                        BackupStoploss.Stoploss_ATR_Period = this.Stoploss_ATR_Period;
                        return BackupStoploss.NewStoploss(position, AsOf);
                    }
                    break;
            }

            if (stopPrice <= 0)
                stopPrice = 0.01m;

            var stop = new Trade(
                position.Security,
                (TradeActionBuySell)(-position.PositionDirection.ToInt()),
                Math.Abs(position.Size(AsOf)),
                TradeType.Stop,
                0, stopPrice)
            {
                TradeDate = AsOf
            };

            return stop;
        }
        public override decimal UpdatePositionStoplossPrice(Position position, Trade currentStop, DateTime AsOf)
        {
            position.Security.SetSwingPointsAndTrends(BarCount, BarSize);

            // First prior swingpoint is first prior that is REALIZED at the time of signal, so we have to look at least BarCount bars back
            PriceBar firstPriorSwingPoint = null;
            switch (BarSize)
            {
                case PriceBarSize.Daily:
                    firstPriorSwingPoint = position.Security.GetPriceBar(Calendar.PriorTradingDay(AsOf, BarCount), BarSize);
                    break;
                case PriceBarSize.Weekly:
                    firstPriorSwingPoint = position.Security.GetPriceBar(Calendar.PriorTradingWeekStart(AsOf, BarCount), BarSize);
                    break;
                case PriceBarSize.Monthly:
                    firstPriorSwingPoint = position.Security.GetPriceBar(Calendar.PriorTradingMonthStart(AsOf, BarCount), BarSize);
                    break;
                case PriceBarSize.Quarterly:
                    firstPriorSwingPoint = position.Security.GetPriceBar(Calendar.PriorTradingMonthStart(AsOf, BarCount), BarSize);
                    break;
                default:
                    break;
            }

            decimal stopPrice = -1m;
            decimal lastPx = position.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Close;

            switch (position.PositionDirection)
            {
                case PositionDirection.ShortPosition:
                    // First prior swingpoint high
                    while ((firstPriorSwingPoint.GetSwingPointType(BarCount) & SwingPointType.SwingPointHigh) == 0)
                    {
                        firstPriorSwingPoint = firstPriorSwingPoint.PriorBar;
                    }
                    stopPrice = firstPriorSwingPoint.High + .01m;
                    break;
                case PositionDirection.LongPosition:
                    // First prior swingpoint low
                    while ((firstPriorSwingPoint.GetSwingPointType(BarCount) & SwingPointType.SwingPointLow) == 0)
                    {
                        firstPriorSwingPoint = firstPriorSwingPoint.PriorBar;
                    }
                    stopPrice = firstPriorSwingPoint.Low - .01m;
                    break;
            }

            // If the last valid swingpoint is on the wrong side of our trade (above exp trade price for a long, below a short), use an ATR atop
            switch (position.PositionDirection)
            {
                case PositionDirection.LongPosition:
                    if (stopPrice >= position.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Close)
                    {
                        BackupStoploss.Stoploss_ATR_Multiple = this.Stoploss_ATR_Multiple;
                        BackupStoploss.Stoploss_ATR_Period = this.Stoploss_ATR_Period;
                        return BackupStoploss.UpdatePositionStoplossPrice(position, currentStop, AsOf);
                    }
                    break;
                case PositionDirection.ShortPosition:
                    if (stopPrice >= position.Security.GetPriceBar(AsOf, PriceBarSize.Daily).Close)
                    {
                        BackupStoploss.Stoploss_ATR_Multiple = this.Stoploss_ATR_Multiple;
                        BackupStoploss.Stoploss_ATR_Period = this.Stoploss_ATR_Period;
                        return BackupStoploss.UpdatePositionStoplossPrice(position, currentStop, AsOf);
                    }
                    break;
            }

            if (stopPrice <= 0)
                stopPrice = 0.01m;

            // Calculate the adjusted base stop if we are using a creeping stop
            decimal adjustedBaseStop = currentStop.StopPrice;
            if (Stoploss_Creep_Percent > 0)
            {
                switch (position.PositionDirection)
                {
                    case PositionDirection.LongPosition:
                        // Increase base stop
                        adjustedBaseStop *= (1 + Stoploss_Creep_Percent);
                        break;
                    case PositionDirection.ShortPosition:
                        // Decrease base stop 
                        adjustedBaseStop *= (1 - Stoploss_Creep_Percent);
                        break;
                }
            }

            switch (position.PositionDirection)
            {
                case PositionDirection.LongPosition:
                    return Math.Max(stopPrice, adjustedBaseStop);
                case PositionDirection.ShortPosition:
                    return Math.Min(stopPrice, adjustedBaseStop);
                default:
                    throw new UnknownErrorException();
            }

        }

        public override PositioningStrategyBase Copy()
        {
            var ret = new SwingPointSizingAndStoploss();

            // Copy all values which are marked with a settings tag
            foreach (var prop in GetType().GetProperties())
            {
                if (Attribute.IsDefined(prop, typeof(SettingsCategoryAttribute)))
                {
                    prop.SetValue(ret, prop.GetValue(this));
                }
            }

            return ret;
        }
    }
}
