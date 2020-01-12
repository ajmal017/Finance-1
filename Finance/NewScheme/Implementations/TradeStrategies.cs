using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    /// <summary>
    /// Simple long-only entry signal generates when price breaks above high of the last [Period] days
    /// </summary>
    public class TradeStrategy_1 : TradeStrategyBase
    {
        public override string Name => "Trade Strategy 1";
        public override string Description => "Long Entry N-Day High Breakout Strategy";

        [TradeSystemParameterInt("Entry Period", "Lookback period for high breakout signal", 14, 89, 1)]
        public int EntryPeriod { get; set; } = 14;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_1() { EntryPeriod = EntryPeriod };
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {
            var barCollection = security.GetPriceBars(AsOf, EntryPeriod, false);

            if (barCollection.Count < EntryPeriod)
                return null;

            var highestClose = (from bar in security.GetPriceBars(AsOf, EntryPeriod, false) select bar.Close).Max();

            if (security.GetPriceBar(AsOf).Close > highestClose)
                return new Signal(security, AsOf, TradeActionBuySell.Buy);
            else
                return null;
        }
    }

    /// <summary>
    /// Simple long-only entry signal generates when price breaks above all time high
    /// </summary>
    public class TradeStrategy_2 : TradeStrategyBase
    {
        public override string Name => "Trade Strategy 2";
        public override string Description => "Long Entry all-time-high Breakout Strategy";

        [TradeSystemParameterInt("Minimum Bars Available", "Fewest bars on which we want to generate a signal", 90, 99999, 1)]
        public int MinimumPeriod { get; set; } = 90;

        public override TradeStrategyBase Copy()
        {
            return new TradeStrategy_2();
        }

        protected override Signal GenerateSignal(Security security, DateTime AsOf)
        {
            var bars = security.GetPriceBars(AsOf, true);

            if (bars.Count < MinimumPeriod)
                return null;

            var highestClose = (from bar in bars select bar.Close).Max();

            if (security.GetPriceBar(AsOf).Close > highestClose)
                return new Signal(security, AsOf, TradeActionBuySell.Buy);
            else
                return null;
        }
    }


}
