using System;
using System.Linq;
using static Finance.Helpers;

namespace Finance
{

    /// <summary>
    /// Strategy which opens a long position if the last close breaks above a high range indicated by EntryPeriod.
    /// Initial risk is a set equity percentage utilizing an ATR trailing multiple stoploss.
    /// </summary>
    public class Strategy_HighBreakoutLongAtrRisk : Strategy
    {

        public int EntryPeriod { get; set; } = 28;
        public int AtrPeriod { get; set; } = 28;

        public decimal AtrStopMultiple { get; set; } = 12.0m;
        public TradeType TypeOfStopUsed { get; set; } = TradeType.Stop;

        public decimal AtrScalingMultiple { get; set; } = 5.0m;
        public decimal scalingPercent { get; set; } = .25m;

        public decimal InitialPositionEquityRiskPercentage { get; set; } = .05m;

        public override Trade ScalingTrade(Position position, DateTime AsOf)
        {

            return null;

            var currentAtrPNL = position.CurrentAtrPnL(AsOf, AtrPeriod);

            if (currentAtrPNL > AtrScalingMultiple)
            {

                var newShares = Convert.ToInt32(Math.Abs(position.Size(AsOf)) * scalingPercent);

                var scaleTrade = new Trade(
                    position.Security,
                    (TradeActionBuySell)position.Direction(),
                    newShares,
                    TradeType.Limit,
                    position.Security.GetPriceBar(AsOf).Close)
                {
                    TradePriority = TradePriority.ExistingPositionIncrease,
                    TradeStatus = TradeStatus.Indicated,
                    TradeDate = AsOf
                };

                return scaleTrade;
            }
            else
            {
                // No adjustment
                return null;
            }

        }

        public override decimal StoplossPrice(Security security, DateTime AsOf, PositionDirection positionDirection, decimal referencePrice)
        {
            var atr = security.GetPriceBar(AsOf).AverageTrueRange(AtrPeriod);
            var stopDif = (atr * AtrStopMultiple);

            // Subtract diff from close for long trades, add for short trades
            decimal ret = (referencePrice - (stopDif * (int)positionDirection));

            return Math.Round(ret, 2);
        }

        protected override int InitialPositionSize(Security security, Portfolio portfolio, DateTime AsOf)
        {
            try
            {
                // Initial risk for our new position
                var totalEquity = portfolio.EquityWithLoanValue(AsOf);
                // Last ATR of the security we are looking at
                var sec = security.GetPriceBar(AsOf, false);
                var securityLastATR = sec.AverageTrueRange(AtrPeriod);

                return Helpers.InitialPositionSize(totalEquity, InitialPositionEquityRiskPercentage, securityLastATR, AtrStopMultiple);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return 0;
            }
        }

        protected override TradeActionBuySell Signal(Security security, DateTime AsOf)
        {
            var highestLevelInPeriod = (from bar in security.GetPriceBars(AsOf, EntryPeriod, false) select bar.Close).Max();

            if (security.GetPriceBar(AsOf).Close > highestLevelInPeriod)
                return TradeActionBuySell.Buy;

            return TradeActionBuySell.None;
        }
    }

}
