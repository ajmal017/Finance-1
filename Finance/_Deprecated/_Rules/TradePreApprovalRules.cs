using Finance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance.Rules
{


    // Rule Description
    /// <summary>
    /// Implements rule stating that you must have a minimum equity with loan value or commodities net liquidation value to open a new position.
    /// </summary>
    public class TradePreApprovalRule_1 : TradeApprovalRule<Portfolio>
    {
        public override void Run(Portfolio port, Trade trade)
        {
            if (trade.TradeStatus == TradeStatus.Cancelled || trade.TradeStatus == TradeStatus.Rejected)
                return;

            // TODO: Ensure that dates don't inadvertantly look ahead.  Make sure TradeDate isn't set until trades are approved.
            if (port.EquityWithLoanValue(trade.TradeDate) < port.Environment.MinimumEquityWithLoanValueNewPosition)
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
    /// </summary>
    public class TradePreApprovalRule_2 : TradeApprovalRule<Portfolio>
    {
        public override void Run(Portfolio port, Trade trade)
        {
            if (trade.TradeStatus == TradeStatus.Cancelled || trade.TradeStatus == TradeStatus.Rejected)
                return;

            var whatIfport = port.Copy();
            var whatIftrade = trade.Copy();

            whatIftrade.TradeStatus = TradeStatus.Pending;
            whatIftrade.Execute(whatIfport, whatIftrade.LimitPrice, whatIftrade.TradeDate, whatIftrade.Quantity);

            if (whatIfport.AvailableFunds(whatIftrade.TradeDate) < 0m)
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
    /// </summary>
    public class TradePreApprovalRule_3 : TradeApprovalRule<Portfolio>
    {
        public override void Run(Portfolio port, Trade trade)
        {
            if (trade.TradeStatus == TradeStatus.Cancelled || trade.TradeStatus == TradeStatus.Rejected)
                return;

            if (port.Environment.GetType() != typeof(IbkrEnvironment))
                return;

            var whatIfport = port.Copy();
            var whatIftrade = trade.Copy();

            whatIftrade.TradeStatus = TradeStatus.Pending;
            whatIftrade.Execute(whatIfport, whatIftrade.LimitPrice, whatIftrade.TradeDate, whatIftrade.Quantity);

            if (whatIfport.GrossPositionValue(trade.TradeDate) > (whatIfport.NetLiquidationValue(trade.TradeDate) * 30m))
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
    /// Trade must not bring the portfolio SMA to less than 0
    /// </summary>
    public class TradePreApprovalRule_4 : TradeApprovalRule<Portfolio>
    {
        public override void Run(Portfolio port, Trade trade)
        {
            if (trade.TradeStatus == TradeStatus.Cancelled || trade.TradeStatus == TradeStatus.Rejected)
                return;

            if (port.Environment.GetType() != typeof(IbkrEnvironment))
                return;

            var whatIfport = port.Copy();
            var whatIftrade = trade.Copy();

            whatIftrade.TradeStatus = TradeStatus.Pending;
            whatIftrade.Execute(whatIfport, whatIftrade.LimitPrice, whatIftrade.TradeDate, whatIftrade.Quantity);

            if (whatIfport.SpecialMemorandumAccountBalance(trade.TradeDate) < 0)
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
