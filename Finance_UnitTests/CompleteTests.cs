using Finance;
using Finance.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Finance_UnitTests
{
    [TestClass]
    public class CompleteTests
    {

        DateTime testDateStart = new DateTime(2018, 01, 02);
        DateTime testDateEnd = new DateTime(2019, 12, 05);

        const decimal startingBalance = 10000.0m;
        const int dataPort = 4002;

        private PortfolioSetup portfolioSetup()
        {
            PortfolioSetup setup = new PortfolioSetup(PortfolioDirection.LongOnly,
                PortfolioMarginType.RegTMargin,
               startingBalance,
                true,
                testDateStart);

            return setup;
        }

        private IEnvironment environment = new IbkrEnvironment();

        private Security testSecurity(string ticker)
        {
            var ret = new Security() { Ticker = ticker };

            var bars = ret.GetPriceBars(testDateStart, testDateEnd);
            foreach (var bar in bars)
            {
                bar.SetPriceValues(10.0m, 20.0m, 5.0m, 15.0m);
            }

            return ret;
        }

        private PriceDatabase database = new PriceDatabase();
        private DataProvider dataProvider = new IbkrDataProvider(dataPort);
        private DataManager dataManager;

        private TradeStrategyBase strategy = new TradeStrategy_1();
        private StrategyManager strategyManager;

        private RiskManager riskManager = new RiskManager();

    }
}
