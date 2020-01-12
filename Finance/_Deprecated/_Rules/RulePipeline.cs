using Finance.Models;
using System;
using System.Collections.Generic;

namespace Finance
{
    ///// <summary>
    ///// Generates a list of potential trades based on a set of rules applied against a universe of securities    /// 
    ///// </summary>
    //public class StrategyRulesPipeline
    //{
    //    List<TradeStrategyRule<Security>> RuleList { get; } = new List<TradeStrategyRule<Security>>();

    //    /// <summary>
    //    /// Add a rule to the back end of the pipeline
    //    /// </summary>
    //    /// <param name="rule"></param>
    //    public void AppendRule(TradeStrategyRule<Security> rule)
    //    {
    //        rule.RuleId = RuleList.Count;
    //        RuleList.Add(rule);
    //    }

    //    /// <summary>
    //    /// Executes rules on each security in list and generates Indicated trades for return
    //    /// </summary>
    //    /// <returns></returns>
    //    public void Run(List<Security> securities, Portfolio portfolio, DateTime AsOf)
    //    {
    //        if (RuleList.Count == 0)
    //            throw new RulePipelineException() { message = $"No rules in {GetType().Name} pipeline to execute" };

    //        // Generate Indicated trades and add to the portfolio pending trade queue
    //        securities.ForEach(sec => RuleList.ForEach(rule =>
    //        {
    //            var trd = rule.Run(sec, portfolio, AsOf);
    //            if (trd != null)
    //                portfolio.PendingTrades.Add(trd);
    //        }));
    //    }
    //}

    /// <summary>
    /// Evaluates trades against the portfolio and returns trades approved for execution (does not append to portfolio directly)
    /// </summary>
    public class PreTradeApprovalRulesPipeline
    {
        List<TradeApprovalRule<Portfolio>> RuleList { get; } = new List<TradeApprovalRule<Portfolio>>();

        /// <summary>
        /// Add a rule to the back end of the pipeline
        /// </summary>
        /// <param name="rule"></param>
        public void AppendRule(TradeApprovalRule<Portfolio> rule)
        {
            rule.RuleId = RuleList.Count;
            RuleList.Add(rule);
        }

        /// <summary>
        /// Executes rules on all Indicated trades in the portfolio and Rejects if needed
        /// </summary>
        /// <returns></returns>
        public void Run(Portfolio portfolio, DateTime AsOf)
        {
            if (RuleList.Count == 0)
                throw new RulePipelineException() { message = $"No rules in {GetType().Name} pipeline to execute" };

            // Sort trades by descending priority
            var IndicatedTrades = portfolio.GetIndicatedTrades(AsOf);
            IndicatedTrades.Sort((x, y) => y.TradePriority.CompareTo(x.TradePriority));

            IndicatedTrades.ForEach(trd =>
             {
                 RuleList.ForEach(rule => rule.Run(portfolio, trd));

                 if (trd.TradeStatus != TradeStatus.Rejected)
                 {
                     trd.TradeStatus = TradeStatus.Pending;
                     trd.TradeDate = Calendar.NextTradingDay(trd.TradeDate);
                 }
             });
        }
    }

    /// <summary>
    /// Executes rules against current portfolio and generates trades to increase, reduce, or close positions.
    /// </summary>
    public class PositionManagementRulesPipeline
    {
        List<PositionManagementRule<Portfolio>> RuleList { get; } = new List<PositionManagementRule<Portfolio>>();

        /// <summary>
        /// Add a rule to the back end of the pipeline
        /// </summary>
        /// <param name="rule"></param>
        public void AppendRule(PositionManagementRule<Portfolio> rule)
        {
            rule.RuleId = RuleList.Count;
            RuleList.Add(rule);
        }

        /// <summary>
        /// Executes all position management rules
        /// </summary>
        /// <returns></returns>
        public void Run(Portfolio portfolio, DateTime AsOf, bool UseOpeningValues = false)
        {
            if (RuleList.Count == 0)
                throw new RulePipelineException() { message = $"No rules in {GetType().Name} pipeline to execute" };

            RuleList.ForEach(rule => rule.Run(portfolio, AsOf, UseOpeningValues));
        }
    }

    /// <summary>
    /// Evaluates trades and executes into portfolio sequentially.  Trades are executed in order of priority and 
    /// portfolio values are re-calculated
    /// </summary>
    public class TradeExecutionApprovalRulesPipeline
    {
        List<TradeApprovalRule<Portfolio>> RuleList { get; } = new List<TradeApprovalRule<Portfolio>>();

        /// <summary>
        /// Add a rule to the back end of the pipeline
        /// </summary>
        /// <param name="rule"></param>
        public void AppendRule(TradeApprovalRule<Portfolio> rule)
        {
            rule.RuleId = RuleList.Count;
            RuleList.Add(rule);
        }

        /// <summary>
        /// Executes rules on all trades in the portfolio Pending queue and Rejects if needed
        /// </summary>
        /// <returns></returns>
        public void Run(Portfolio portfolio, DateTime AsOf)
        {
            if (RuleList.Count == 0)
                throw new RulePipelineException() { message = $"No rules in {GetType().Name} pipeline to execute" };

            // When we are approving new trades, we need to avaluate against a portfolio
            // which already accounts for all required/guaranteed trades (closeouts, etc)
            Portfolio whatIfPortfolio = portfolio.Copy();

            // Sort pending trades by descending priority
            var pendingTrades = portfolio.GetPendingTrades(AsOf);
            pendingTrades.Sort((x, y) => y.TradePriority.CompareTo(x.TradePriority));

            pendingTrades.ForEach(trd =>
            {
                // Trades not requiring approval should execute first
                switch (trd.TradePriority)
                {
                    case TradePriority.NotSet:
                    case TradePriority.NewPositionOpen:
                    case TradePriority.ExistingPositionIncrease:
                        {
                            // Run rules against trade and hypothetical lookahead portfolio
                            RuleList.ForEach(rule => rule.Run(whatIfPortfolio, trd));
                            break;
                        }
                    case TradePriority.ExistingPositionDecrease:
                    case TradePriority.PositionClose:
                    case TradePriority.StoplossImmediate:
                        {
                            var px = Math.Max(trd.LimitPrice, trd.StopPrice);
                            // Remain pending and assume execution.  Execute copy into lookahead portfolio
                            Trade whatIfTrade = trd.Copy();
                            whatIfTrade.Execute(whatIfPortfolio, px, trd.TradeDate, trd.Quantity);
                            break;
                        }
                    default:
                        throw new InvalidTradeOperationException() { message = "Unknown error in Trade Approval pipeline" };
                }
            });
        }
    }

}
