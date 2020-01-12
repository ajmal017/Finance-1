using Finance;
using Finance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance.Rules
{

    /*
     *  Position Management rules are rule which govern existing positions and overall portfolio risk.
     *  These rules are implemented at end of day and generate trades which increase, reduce, or close positions.
     *  These rules will enver generate a trade to open a new position.
     */

    // Rule Description
    /// <summary>
    /// If excess liquidity less than 0 at any time during the day, positions are liquidated
    /// </summary>
    public class PositionManagementRule_LiquidityCheck : PositionManagementRule<Portfolio>
    {
        public override void Run(Portfolio port, DateTime AsOf, bool UseOpeningValues = false)
        {
            // TODO: How do we implement something that best represents real-time using only EOD data?

            return;

            throw new NotImplementedException();
        }
    }

    // Rule Description
    /// <summary>
    /// Checks and updates stoploss trades at EOD using stoploss rules outlined in the Portfolio strategy.
    /// Trades are placed directly into positions and not returned
    /// </summary>
    public class PositionManagementRule_StoplossUpdate : PositionManagementRule<Portfolio>
    {
        public override void Run(Portfolio port, DateTime AsOf, bool UseOpeningValues = false)
        {
            // For each position, calculate what the new stoploss level would be based on day's close

            // Only EOD execution
            if (UseOpeningValues)
                return;

            foreach (var position in port.GetAllPositions(AsOf))
            {

                // Generate a new stoploss trade based on the security closing price
                var stopTrade = port.Strategy.StoplossTrade(position, AsOf, false);

                // Position handles updating
                position.UpdateStoplossTrade(stopTrade);
            }

        }
    }

    public class PositionManagementRule_PositionScaling : PositionManagementRule<Portfolio>
    {
        public override void Run(Portfolio port, DateTime AsOf, bool UseOpeningValues = false)
        {
            // For each position, evaluate current PNL and determine if the position should be scaled up

            // Only EOD execution
            if (UseOpeningValues)
                return;

            foreach (var position in port.GetAllPositions(AsOf))
            {

                // Generate a new scaling trade based on the security closing price
                var scaleTrade = port.Strategy.ScalingTrade(position, AsOf);

                if (scaleTrade != null)
                    port.PendingTrades.Add(scaleTrade);
            }

        }
    }

}
