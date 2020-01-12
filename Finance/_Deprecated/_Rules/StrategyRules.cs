using Finance;
using Finance.Data;
using Finance.Models;
using System;

namespace Finance.Rules
{
    /*
     *  Trade Strategy Rules implement the selected trading strategy for this system.
     *  Rules are executed on individual securities to generate entry trades marked as
     *  conditional and passed back to the Manager for approval. 
     */

    ///// <summary>
    ///// Call the Strategy to produce an entry trade on the given security
    ///// </summary>
    //public class TradeStrategyRule_1 : TradeStrategyRule<Security>
    //{
    //    public override Trade Run(Security sec, Portfolio port, DateTime AsOf)
    //    {
    //        return port.Strategy.EntryTrade(sec, port, AsOf);
    //    }
    //}


}
