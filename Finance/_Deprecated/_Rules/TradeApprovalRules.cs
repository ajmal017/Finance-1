using Finance;
using Finance.Data;
using Finance.Models;
using System;

namespace Finance.Rules
{

    // Rule Description
    /// <summary>
    /// Implements rule stating that you must have a minimum equity with loan value or commodities net liquidation value to open a new position.
    /// This rule is to be applied immediately prior to trade execution and uses the Trade Date Open value.
    /// </summary>
    public class TradeApprovalRule_1 : TradeApprovalRule<Portfolio>
    {
        public override void Run(Portfolio port, Trade trade)
        {
            if (trade.TradeStatus == TradeStatus.Cancelled || trade.TradeStatus == TradeStatus.Rejected)
                return;
                                    

            // TODO: Ensure that dates don't inadvertantly look ahead.  Make sure TradeDate isn't set until trades are approved.
            if (port.EquityWithLoanValue(trade.TradeDate,true) < port.Environment.MinimumEquityWithLoanValueNewPosition)
            {
                // REJECT
                trade.TradeStatus = TradeStatus.Rejected;
                return;
            }
            else
            {
                // DO NOT REJECT
                return;
            }
        }
    }

    // Rule Description
    /// <summary>
    /// Implements rule stating that available funds after the order must be greater than or equal to zero, or the trade would be rejected
    /// This rule is to be applied immediately prior to trade execution and uses the Trade Date Open value.
    /// </summary>
    public class TradeApprovalRule_2 : TradeApprovalRule<Portfolio>
    {
        public override void Run(Portfolio port, Trade trade)
        {
            if (trade.TradeStatus == TradeStatus.Cancelled || trade.TradeStatus == TradeStatus.Rejected)
                return;

            var whatIfport = port.Copy();
            var whatIftrade = trade.Copy();
            whatIftrade.Execute(whatIfport, whatIftrade.LimitPrice, whatIftrade.TradeDate, whatIftrade.Quantity);

            if (whatIfport.AvailableFunds(whatIftrade.TradeDate, true) < 0m)
            {
                // REJECT
                trade.TradeStatus = TradeStatus.Rejected;
                return;
            }
            else
            {
                // DO NOT REJECT
                return;
            }
        }
    }

    // Rule Description
    /// <summary>
    /// Implements an IBKR rule that Gross Position Value must not be more than 30x the Net Liquidation Value at time of trade
    /// This rule is to be applied immediately prior to trade execution and uses the Trade Date Open value.
    /// </summary>
    public class TradeApprovalRule_3 : TradeApprovalRule<Portfolio>
    {
        public override void Run(Portfolio port, Trade trade)
        {
            if (trade.TradeStatus == TradeStatus.Cancelled || trade.TradeStatus == TradeStatus.Rejected)
                return;

            if (port.Environment.GetType() != typeof(IbkrEnvironment))
                return;

            if (port.GrossPositionValue(trade.TradeDate, true) > (port.NetLiquidationValue(trade.TradeDate,true) * 30m))
            {
                // REJECT
                trade.TradeStatus = TradeStatus.Rejected;
                return;
            }
            else
            {
                // DO NOT REJECT
                return;
            }
        }
    }

}
