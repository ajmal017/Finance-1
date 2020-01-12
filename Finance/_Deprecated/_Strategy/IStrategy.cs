using Finance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    /// <summary>
    /// Base method for building strategy subclasses
    /// </summary>
    public abstract class Strategy
    {

        public Trade EntryTrade(Security security, Portfolio portfolio, DateTime AsOf)
        {
            var signal = Signal(security, AsOf);

            if (signal == TradeActionBuySell.None)
                return null;

            // Format a new entry trade
            Trade ret = new Trade(security, signal, 0, TradeType.Limit)
            {
                Quantity = InitialPositionSize(security, portfolio, AsOf),
                LimitPrice = security.GetPriceBar(AsOf).Close, // TODO: adjust this to allow some flex at execution
                TradePriority = TradePriority.NewPositionOpen,
                TradeStatus = TradeStatus.Indicated,
                TradeDate = AsOf
            };

            return ret;
        }

        public Trade StoplossTrade(Position position, DateTime AsOf, bool initialStop = false)
        {
            decimal price;
            if (initialStop)
                price = StoplossPrice(position.Security, AsOf, position.PositionDirection, position.Trades.Single().ExecutedPrice);
            else
                price = StoplossPrice(position.Security, AsOf, position.PositionDirection, position.Security.GetPriceBar(AsOf).Close);

            Trade ret = new Trade(position.Security, (TradeActionBuySell)(-(int)position.PositionDirection), Math.Abs(position.Size(AsOf)), TradeType.Stop, price)
            {
                TradePriority = TradePriority.StoplossImmediate,
                TradeStatus = TradeStatus.Stoploss,
                TradeDate = AsOf
            };


            return ret;
        }


        public abstract Trade ScalingTrade(Position position, DateTime AsOf);

        public abstract decimal StoplossPrice(Security security, DateTime AsOf, PositionDirection positionDirection, decimal referencePrice);

        protected abstract TradeActionBuySell Signal(Security security, DateTime AsOf);

        protected abstract int InitialPositionSize(Security security, Portfolio portfolio, DateTime AsOf);

    }

}
