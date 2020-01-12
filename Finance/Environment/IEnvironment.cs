using Finance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    /// <summary>
    /// Implement to define trading-system specific functions and variables
    /// </summary>
    public interface IEnvironmentOld
    {

        decimal RegTEndOfDayInitialMargin { get; set; }

        decimal InitialMarginLong { get; set; }
        decimal MaintenanceMarginLong { get; set; }

        decimal InitialMarginShort { get; set; }
        decimal MaintenanceMarginShort { get; set; }

        decimal MinimumEquityWithLoanValueNewPosition { get; set; }

        bool NegateCommissionForTesting { get; set; }

        /// <summary>
        /// Margin that must be maintained on the initial trade date
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        decimal BrokerInitialMargin(Trade trd);

        /// <summary>
        /// Margin calculated at EOR for new trades under Reg T
        /// </summary>
        /// <param name="trd"></param>
        /// <returns></returns>
        decimal RegTInitialMargin(Trade trd);

        /// <summary>
        /// Maintenance margin required to hold for positions > 1 day
        /// </summary>
        /// <param name="p"></param>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        decimal BrokerMaintenanceMargin(Position pos, DateTime AsOf, TimeOfDay MarketValues);

        decimal RegTEndOfDayMargin(Position pos, DateTime AsOf, TimeOfDay MarketValues);

        decimal CommissionCharged(Trade trd, bool API);
        decimal CommissionCharged(List<Trade> trds, bool API);

        /// <summary>
        /// Expected price slippage
        /// </summary>
        /// <param name="Price"></param>
        /// <returns></returns>
        decimal Slippage(decimal Price);

        /// <summary>
        /// Execution price adjusted for slippage based on trade direction
        /// </summary>
        /// <param name="Price"></param>
        /// <param name="tradeAction"></param>
        /// <returns></returns>
        decimal SlippageAdjustedPrice(decimal Price, TradeActionBuySell tradeAction);

        ///// <summary>
        ///// rules pipeline for managing open positions
        ///// </summary>
        //PositionManagementRulesPipeline PositionManagementRulesPipeline { get; set; }
        //PreTradeApprovalRulesPipeline PreTradeApprovalRulesPipeline { get; set; }
        //TradeExecutionApprovalRulesPipeline TradeExecutionApprovalRulesPipeline { get; set; }

    }

    public interface IEnvironment
    {

        decimal RegTEndOfDayInitialMargin { get; set; }

        decimal InitialMarginLong { get; set; }
        decimal MaintenanceMarginLong { get; set; }

        decimal InitialMarginShort { get; set; }
        decimal MaintenanceMarginShort { get; set; }

        decimal MinimumEquityWithLoanValueNewPosition { get; set; }

        bool NegateCommissionForTesting { get; set; }

        /// <summary>
        /// Margin that must be maintained on the initial trade date
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        decimal BrokerInitialMargin(Trade trd);

        /// <summary>
        /// Margin calculated at EOR for new trades under Reg T
        /// </summary>
        /// <param name="trd"></param>
        /// <returns></returns>
        decimal RegTInitialMargin(Trade trd);

        /// <summary>
        /// Maintenance margin required to hold for positions > 1 day
        /// </summary>
        /// <param name="p"></param>
        /// <param name="AsOf"></param>
        /// <returns></returns>
        decimal BrokerMaintenanceMargin(Position pos, DateTime AsOf, TimeOfDay MarketValues);

        decimal RegTEndOfDayMargin(Position pos, DateTime AsOf, TimeOfDay MarketValues);

        decimal CommissionCharged(Trade trd, bool API = false);
        decimal CommissionCharged(List<Trade> trds, bool API = false);

        /// <summary>
        /// Expected price slippage
        /// </summary>
        /// <param name="Price"></param>
        /// <returns></returns>
        decimal Slippage(decimal Price);

        /// <summary>
        /// Execution price adjusted for slippage based on trade direction
        /// </summary>
        /// <param name="Price"></param>
        /// <param name="tradeAction"></param>
        /// <returns></returns>
        decimal SlippageAdjustedPrice(decimal Price, TradeActionBuySell tradeAction);

    }
}
