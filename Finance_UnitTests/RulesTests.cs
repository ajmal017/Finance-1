using Finance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Finance_UnitTests
{
    [TestClass]
    public class RulesTests
    {
        DateTime testDate = new DateTime(2019, 11, 20);

        private Portfolio testPortfolio(decimal startingBalance)
        {
            IEnvironment environment = new IbkrEnvironment();
            PortfolioSetup setup = new PortfolioSetup(PortfolioDirection.LongOnly,
                PortfolioMarginType.RegTMargin,
               startingBalance,
                true,
                new DateTime(2019, 11, 20));

            return new Portfolio(environment, setup);
        }

        private Security testSecurity(string ticker)
        {
            var ret = new Security() { Ticker = ticker };

            ret.GetPriceBar(testDate, true).SetPriceValues(10.0m, 20.0m, 5.0m, 15.0m);

            return ret;
        }

        // Trade Approval Rule 1
        [TestMethod]
        public void TestMethod1()
        {

            var portfolio = testPortfolio(1000.0m);

            var trade = new Trade(testSecurity("ABC"), TradeActionBuySell.Buy, 100, TradeType.Market);

            var rule = new TradeApprovalRule_1();

            // Should be false since we only have $1000 total available funds (need minimum $2000)
            Assert.IsFalse(rule.Run(trade, portfolio, testDate, TimeOfDay.MarketOpen));

            portfolio = testPortfolio(3000.0m);

            // Should pass now
            Assert.IsTrue(rule.Run(trade, portfolio, testDate, TimeOfDay.MarketOpen));

        }

        // Trade Approval Rule 2
        [TestMethod]
        public void TestMethod2()
        {

            var portfolio = testPortfolio(2000.0m);

            var trade = new Trade(testSecurity("ABC"), TradeActionBuySell.Buy, 100, TradeType.Market);

            var rule = new TradeApprovalRule_2();

            // Should pass
            Assert.IsTrue(rule.Run(trade, portfolio, testDate, TimeOfDay.MarketOpen));

            trade = new Trade(testSecurity("ABC"), TradeActionBuySell.Buy, 250, TradeType.Market);

            // Should fail
            Assert.IsFalse(rule.Run(trade, portfolio, testDate, TimeOfDay.MarketOpen));

        }
    }
}
