using Finance;
using Finance.Data;
using Finance.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace Finance_UnitTests
{
    [TestClass]
    public class PortfolioManagerTests
    {

        /// <summary>
        /// Returns a new, initialized portfolio manager
        /// </summary>
        /// <returns></returns>
        private PortfolioManager testPm()
        {
            DataManager dataManager = new DataManager(
               new InteractiveBrokersDataprovider(4002),
               new PriceDatabase());

            PortfolioSetup portfolioSetup = new PortfolioSetup(
                PortfolioDirection.LongOnly,
                PortfolioMarginType.RegTMargin,
                10000m,
                true,
                new DateTime(2015, 1, 2));

            IEnvironment ibkrEnvironment = new IbkrEnvironment();

            ibkrEnvironment.PositionManagementRulesPipeline.AppendRule(
                new Finance.Rules.PositionManagementRule_LiquidityCheck());

            ibkrEnvironment.PositionManagementRulesPipeline.AppendRule(
                new Finance.Rules.PositionManagementRule_StoplossUpdate());

            ibkrEnvironment.PositionManagementRulesPipeline.AppendRule(
                new Finance.Rules.PositionManagementRule_PositionScaling());

            ibkrEnvironment.PreTradeApprovalRulesPipeline.AppendRule(
                new Finance.Rules.TradePreApprovalRule_1());

            ibkrEnvironment.PreTradeApprovalRulesPipeline.AppendRule(
                new Finance.Rules.TradePreApprovalRule_2());

            ibkrEnvironment.PreTradeApprovalRulesPipeline.AppendRule(
                new Finance.Rules.TradePreApprovalRule_3());

            ibkrEnvironment.PreTradeApprovalRulesPipeline.AppendRule(
                new Finance.Rules.TradePreApprovalRule_4());

            ibkrEnvironment.TradeExecutionApprovalRulesPipeline.AppendRule(
                new Finance.Rules.TradeApprovalRule_1());

            ibkrEnvironment.TradeExecutionApprovalRulesPipeline.AppendRule(
                new Finance.Rules.TradeApprovalRule_2());

            ibkrEnvironment.TradeExecutionApprovalRulesPipeline.AppendRule(
                new Finance.Rules.TradeApprovalRule_3());

            Strategy strategy = new Strategy_HighBreakoutLongAtrRisk();

            return new PortfolioManager(dataManager, portfolioSetup, ibkrEnvironment, strategy);
        }

        [TestMethod]
        public void UpdateUniverse()
        {
            var pm = testPm();
            DateTime endDate = new DateTime(2019, 11, 27);

            Security sec1 = pm._DataManager.GetSecurity("TWTR");
            Security sec2 = pm._DataManager.GetSecurity("FB");
            Security sec3 = pm._DataManager.GetSecurity("F");

            bool received1 = false;
            bool received2 = false;
            bool received3 = false;

            pm._DataManager.OnSecurityDataResponse += (s, e) =>
            {
                Console.WriteLine($"Received {e.security.Ticker}");
                if (e.security.Ticker == "TWTR")
                    received1 = true;
                if (e.security.Ticker == "FB")
                    received2 = true;
                if (e.security.Ticker == "F")
                    received3 = true;

            };

            pm._DataManager.UpdateSecurity(sec1, endDate, true);
            pm._DataManager.UpdateSecurity(sec2, endDate, true);
            pm._DataManager.UpdateSecurity(sec3, endDate, true);

            while (!received1 || !received2 || !received3)
                Thread.Sleep(100);

            Thread.Sleep(10000);

            Assert.IsTrue(true);

            pm._DataManager.CloseDataConnection();


        }

        [TestMethod]
        public void TestMethod2()
        {
            var pm = testPm();
            DateTime endDate = new DateTime(2019, 11, 26);

            while (pm.CurrentDate < endDate)
                pm.StepDate();

            //pm._Portfolio.ToStringAllAccounting(endDate).ForEach(x => Console.WriteLine(x));

            Helpers.OutputToTextFile(pm._Portfolio.ToStringAllAccounting(endDate));

            Helpers.OutputToTextFile(pm._Portfolio.ToStringAllActivity(endDate));

            pm._DataManager.CloseDataConnection();

            Logger.EndLog(pm.CurrentDate);
        }
    }
}
