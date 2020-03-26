using Finance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Finance.Calendar;
using static Finance.Helpers;

namespace Finance_UnitTests
{
    [TestClass]
    public class PortfolioAndPositionTests
    {

        private Security testSecurity1()
        {
            return new Security("XYZ", SecurityType.USCommonEquity);
        }

        private Trade testTradeBuy(Security testSec)
        {
            return new Trade(testSec, TradeActionBuySell.Buy, 100, TradeType.Market) { TradeStatus = TradeStatus.Pending };
        }

        private Trade testTradeSell(Security testSec)
        {
            return new Trade(testSec, TradeActionBuySell.Sell, 100, TradeType.Market) { TradeStatus = TradeStatus.Pending };
        }

        [TestMethod]
        public void EmptyPortfolioAccounting()
        {
            PortfolioSetup testSetupParams = new PortfolioSetup(PortfolioDirection.LongShort, PortfolioMarginType.RegTMargin, 10000, new DateTime(2019, 11, 18));
            Portfolio testPortfolio = new Portfolio(testSetupParams);

            DateTime AsOf = new DateTime(2019, 11, 18);
            decimal balance = testSetupParams.InitialCashBalance;

            Assert.AreEqual(balance, testPortfolio.TotalCashValue(AsOf));

            Assert.AreEqual(0, testPortfolio.TotalCashPurchasesAndProceeds(AsOf));

            Assert.AreEqual(0, testPortfolio.TotalCommissions(AsOf));

            Assert.AreEqual(0, testPortfolio.StockValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0, testPortfolio.LongStockValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0, testPortfolio.ShortStockValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0, testPortfolio.LongOptionValue(AsOf, TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(0, testPortfolio.ShortOptionValue(AsOf, TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(0, testPortfolio.BondValue(AsOf, TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(0, testPortfolio.FundValue(AsOf, TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(0, testPortfolio.EuroAndAsianOptionsValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(balance, testPortfolio.EquityWithLoanValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0, testPortfolio.GrossPositionValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(balance, testPortfolio.NetLiquidationValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0, testPortfolio.FuturesOptionsValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(balance, testPortfolio.ExcessLiquidity(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0, testPortfolio.BrokerMaintenanceMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0, testPortfolio.RegTMaintenanceMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0, testPortfolio.BrokerInitialMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0, testPortfolio.RegTInitialMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(balance, testPortfolio.SpecialMemorandumAccountBalance(AsOf, TimeOfDay.MarketEndOfDay));

        }
        
        [TestMethod]
        public void SimpleTradeTestLong1()
        {
            PortfolioSetup testSetupParams = new PortfolioSetup(PortfolioDirection.LongShort, PortfolioMarginType.RegTMargin, 10000, new DateTime(2019, 11, 18));
            Portfolio testPortfolio = new Portfolio(testSetupParams);

            var testSec = testSecurity1();
            var testTrade = testTradeBuy(testSec);
            var balance = testSetupParams.InitialCashBalance;

            // Valid monday with full trading week
            DateTime date = new DateTime(2019, 11, 18);

            var tradeBar = testSec.GetPriceBar(date, true);
            tradeBar.SetPriceValues(10m, 15m, 5m, 11m);

            var executionPrice = tradeBar.Open;

            // Mark this as an executed trade - buy 100 shares @ 10.00
            testTrade.MarkExecuted(date, executionPrice);
            testPortfolio.AddExecutedTrade(testTrade);

            // Check position functions
            var pos = testPortfolio.GetPosition(testSec, date);

            Assert.IsTrue(pos.PositionDirection == PositionDirection.LongPosition);
            Assert.IsTrue(pos.IsOpen(date));

            Assert.AreEqual(100, pos.Size(date));
            Assert.AreEqual(10.0m, pos.AverageCost(date));

            Assert.AreEqual(1100.0m, pos.GrossPositionValue(date, TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(-1000.0m, pos.NetCashImpact(date));
            Assert.AreEqual(100.0m, pos.TotalUnrealizedPnL(date, TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(0.0m, pos.TotalRealizedPnL(date));

            // Check portfolio accounting

            Assert.AreEqual(1, testPortfolio.Positions.Count);

            DateTime AsOf = new DateTime(2019, 11, 18);

            decimal commission = TradingEnvironment.Instance.CommissionCharged(testTrade);


            Assert.AreEqual(-1.0m, commission);

            Assert.AreEqual(balance - 1000.0m - 1.0m, testPortfolio.TotalCashValue(AsOf));

            Assert.AreEqual(-1000m, testPortfolio.TotalCashPurchasesAndProceeds(AsOf));

            Assert.AreEqual(-1.0m, testPortfolio.TotalCommissions(AsOf));

            Assert.AreEqual(1100m, testPortfolio.StockValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(1100m, testPortfolio.LongStockValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0, testPortfolio.ShortStockValue(AsOf, TimeOfDay.MarketEndOfDay));

            //Assert.AreEqual(0, testPortfolio.LongOptionValue(asof));
            //Assert.AreEqual(0, testPortfolio.ShortOptionValue(asof));
            //Assert.AreEqual(0, testPortfolio.BondValue(asof));
            //Assert.AreEqual(0, testPortfolio.FundValue(asof));
            //Assert.AreEqual(0, testPortfolio.EuroAndAsianOptionsValue(asof));

            Assert.AreEqual(8999m + 1100m, testPortfolio.EquityWithLoanValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(1100.0m, testPortfolio.GrossPositionValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(8999m + 1100m, testPortfolio.NetLiquidationValue(AsOf, TimeOfDay.MarketEndOfDay));

            //Assert.AreEqual(0, testPortfolio.FuturesOptionsValue(asof));

            Assert.AreEqual(1100.0m, testPortfolio.BrokerMaintenanceMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(550.0m, testPortfolio.RegTMaintenanceMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(1100.0m, testPortfolio.BrokerInitialMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(500.0m, testPortfolio.RegTInitialMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(8999.0m, testPortfolio.ExcessLiquidity(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(9549.0m, testPortfolio.SpecialMemorandumAccountBalance(AsOf, TimeOfDay.MarketEndOfDay));

        }

        [TestMethod]
        public void SimpleTradeTestShort1()
        {
            PortfolioSetup testSetupParams = new PortfolioSetup(PortfolioDirection.LongShort, PortfolioMarginType.RegTMargin, 10000, new DateTime(2019, 11, 18));
            Portfolio testPortfolio = new Portfolio(testSetupParams);

            var testSec = testSecurity1();
            var testTrade = testTradeSell(testSec);
            var balance = testSetupParams.InitialCashBalance;

            // Valid monday with full trading week
            DateTime date = new DateTime(2019, 11, 18);

            var tradeBar = testSec.GetPriceBar(date, true);
            tradeBar.SetPriceValues(10m, 15m, 5m, 10m);

            var executionPrice = tradeBar.Open;

            // Mark this as an executed trade - buy 100 shares @ 10.00            
            testTrade.MarkExecuted(tradeBar.BarDateTime, executionPrice);
            testPortfolio.AddExecutedTrade(testTrade);

            var pos = testPortfolio.GetPosition(testSec, date);

            Assert.IsTrue(pos.PositionDirection == PositionDirection.ShortPosition);

            // Check portfolio accounting

            Assert.AreEqual(1, testPortfolio.Positions.Count);

            DateTime AsOf = new DateTime(2019, 11, 18);

            decimal commission = TradingEnvironment.Instance.CommissionCharged(testTrade);

            Assert.AreEqual(-1.0m, commission);

            Assert.AreEqual(balance + 1000.0m - 1.0m, testPortfolio.TotalCashValue(AsOf));

            Assert.AreEqual(1000m, testPortfolio.TotalCashPurchasesAndProceeds(AsOf));

            Assert.AreEqual(-1.0m, testPortfolio.TotalCommissions(AsOf));

            Assert.AreEqual(-1000m, testPortfolio.StockValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0m, testPortfolio.LongStockValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(-1000m, testPortfolio.ShortStockValue(AsOf, TimeOfDay.MarketEndOfDay));

            //Assert.AreEqual(0, testPortfolio.LongOptionValue(asof));
            //Assert.AreEqual(0, testPortfolio.ShortOptionValue(asof));
            //Assert.AreEqual(0, testPortfolio.BondValue(asof));
            //Assert.AreEqual(0, testPortfolio.FundValue(asof));
            //Assert.AreEqual(0, testPortfolio.EuroAndAsianOptionsValue(asof));

            Assert.AreEqual(balance + commission, testPortfolio.EquityWithLoanValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(1000.0m, testPortfolio.GrossPositionValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(balance + commission, testPortfolio.NetLiquidationValue(AsOf, TimeOfDay.MarketEndOfDay));

            //Assert.AreEqual(0, testPortfolio.FuturesOptionsValue(asof));

            Assert.AreEqual(2000.0m, testPortfolio.BrokerMaintenanceMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(500.0m, testPortfolio.RegTMaintenanceMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(2000.0m, testPortfolio.BrokerInitialMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(500.0m, testPortfolio.RegTInitialMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(7999.0m, testPortfolio.ExcessLiquidity(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(9500.0m, testPortfolio.SpecialMemorandumAccountBalance(AsOf, TimeOfDay.MarketEndOfDay));

        }

        [TestMethod]
        public void MultiTradeTest1()
        {
            PortfolioSetup testSetupParams = new PortfolioSetup(PortfolioDirection.LongShort, PortfolioMarginType.RegTMargin, 10000, new DateTime(2019, 11, 18));
            Portfolio testPortfolio = new Portfolio(testSetupParams);
            var testSec = testSecurity1();
            var testTrade = testTradeBuy(testSec);
            var balance = testSetupParams.InitialCashBalance;

            // Valid monday with full trading week
            DateTime date = new DateTime(2019, 11, 18);

            var tradeBar = testSec.GetPriceBar(date, true);
            tradeBar.SetPriceValues(10m, 15m, 5m, 10m);

            var executionPrice = tradeBar.Open;

            // Mark this as an executed trade - buy 100 shares @ 10.00
            testTrade.MarkExecuted(tradeBar.BarDateTime, executionPrice);
            testPortfolio.AddExecutedTrade(testTrade);

            decimal commission = TradingEnvironment.Instance.CommissionCharged(testTrade);

            // No do a sell of 100 at $1 higher
            testTrade = testTradeSell(testSec);
            testTrade.MarkExecuted(tradeBar.BarDateTime, 11.0m);
            testPortfolio.AddExecutedTrade(testTrade);

            commission += TradingEnvironment.Instance.CommissionCharged(testTrade);
            // Everything should be zeroed out, less $2 commission

            Assert.AreEqual(1, testPortfolio.Positions.Count);

            var closePos = testPortfolio.GetPositions(PositionStatus.Closed, date);

            Assert.IsTrue(closePos.Count == 1);
            Assert.IsTrue(closePos[0].IsOpen(date) == false);
            Assert.IsTrue(closePos[0].PositionDirection == PositionDirection.LongPosition);

            var openPos = testPortfolio.GetPositions(PositionStatus.Open, date);
            Assert.IsTrue(openPos.Count == 0);

            DateTime AsOf = new DateTime(2019, 11, 18);

            Assert.AreEqual(-2.0m, commission);

            // We should have made $100 minus $2 commission
            Assert.AreEqual(balance + 100.0m + commission, testPortfolio.TotalCashValue(AsOf));

            Assert.AreEqual(100.0m, testPortfolio.TotalCashPurchasesAndProceeds(AsOf));

            Assert.AreEqual(-2.0m, testPortfolio.TotalCommissions(AsOf));

            Assert.AreEqual(0m, testPortfolio.StockValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0m, testPortfolio.LongStockValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0m, testPortfolio.ShortStockValue(AsOf, TimeOfDay.MarketEndOfDay));

            //Assert.AreEqual(0, testPortfolio.LongOptionValue(asof));
            //Assert.AreEqual(0, testPortfolio.ShortOptionValue(asof));
            //Assert.AreEqual(0, testPortfolio.BondValue(asof));
            //Assert.AreEqual(0, testPortfolio.FundValue(asof));
            //Assert.AreEqual(0, testPortfolio.EuroAndAsianOptionsValue(asof));

            Assert.AreEqual(balance + 100.0m + commission, testPortfolio.EquityWithLoanValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0.0m, testPortfolio.GrossPositionValue(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(balance + 100.0m + commission, testPortfolio.NetLiquidationValue(AsOf, TimeOfDay.MarketEndOfDay));

            //Assert.AreEqual(0, testPortfolio.FuturesOptionsValue(asof));

            Assert.AreEqual(0m, testPortfolio.BrokerMaintenanceMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0.0m, testPortfolio.RegTMaintenanceMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0.0m, testPortfolio.BrokerInitialMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(-50.0m, testPortfolio.RegTInitialMarginRequirement(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(balance + 100.0m + commission, testPortfolio.ExcessLiquidity(AsOf, TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(balance + 100.0m + commission, testPortfolio.SpecialMemorandumAccountBalance(AsOf, TimeOfDay.MarketEndOfDay));

        }

        [TestMethod]
        public void MultiTradeTest2()
        {
            PortfolioSetup testSetupParams = new PortfolioSetup(PortfolioDirection.LongShort, PortfolioMarginType.RegTMargin, 10000, new DateTime(2019, 11, 18));
            Portfolio testPortfolio = new Portfolio(testSetupParams);
            var testSec = testSecurity1();
            var testTrade = testTradeBuy(testSec);
            var balance = testSetupParams.InitialCashBalance;

            // Valid monday with full trading week
            DateTime date = new DateTime(2019, 11, 18);

            var tradeBar = testSec.GetPriceBar(date, true);
            tradeBar.SetPriceValues(10m, 15m, 5m, 10m);

            var executionPrice = tradeBar.Open;

            // Mark this as an executed trade - buy 100 shares @ 10.00

            testTrade.MarkExecuted(tradeBar.BarDateTime, executionPrice);
            testPortfolio.AddExecutedTrade(testTrade);

            decimal commission = TradingEnvironment.Instance.CommissionCharged(testTrade);

            // Execute another 100 shates @ $11.00
            testTrade = testTradeBuy(testSec);
            testTrade.MarkExecuted(tradeBar.BarDateTime, 11.0m);
            testPortfolio.AddExecutedTrade(testTrade);

            Assert.AreEqual(1, testPortfolio.GetPositions(PositionStatus.Open, date).Count);

            var pos = testPortfolio.GetPosition(testSec, date);

            Assert.AreEqual(2, pos.ExecutedTrades.Count);
            Assert.AreEqual(10.5m, pos.AverageCost(date));
            Assert.AreEqual(200, pos.Size(date));
            Assert.AreEqual(2000.0m, pos.GrossPositionValue(date, TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(-100.0m, pos.TotalUnrealizedPnL(date, TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(-2100.0m, pos.NetCashImpact(date));
            Assert.AreEqual(-2.0m, pos.TotalCommissionPaid(date));

        }

        [TestMethod]
        public void MultiTradeTest3()
        {
            PortfolioSetup testSetupParams = new PortfolioSetup(PortfolioDirection.LongShort, PortfolioMarginType.RegTMargin, 10000, new DateTime(2019, 11, 18));
            Portfolio testPortfolio = new Portfolio(testSetupParams);
            var testSec = testSecurity1();
            var testTrade = testTradeBuy(testSec);
            var balance = testSetupParams.InitialCashBalance;

            // Valid monday with full trading week
            DateTime date = new DateTime(2019, 11, 18);

            var tradeBar = testSec.GetPriceBar(date, true);
            tradeBar.SetPriceValues(10m, 15m, 5m, 12m);

            var executionPrice = tradeBar.Open;

            // Mark this as an executed trade - buy 100 shares @ 10.00
            testTrade.MarkExecuted(tradeBar.BarDateTime, 10.0m);
            testPortfolio.AddExecutedTrade(testTrade);

            decimal commission = TradingEnvironment.Instance.CommissionCharged(testTrade);

            // Execute another 100 shares @ $11.00
            testTrade = testTradeBuy(testSec);
            testTrade.MarkExecuted(tradeBar.BarDateTime, 11.0m);
            testPortfolio.AddExecutedTrade(testTrade);

            // Execute another 50 shares @ $12.00
            testTrade = testTradeBuy(testSec);
            testTrade.Quantity = 50;
            testTrade.MarkExecuted(tradeBar.BarDateTime, 12.0m);
            testPortfolio.AddExecutedTrade(testTrade);

            // Execute another 50 shares @ $14.00
            testTrade = testTradeBuy(testSec);
            testTrade.Quantity = 50;
            testTrade.MarkExecuted(tradeBar.BarDateTime, 14.0m);
            testPortfolio.AddExecutedTrade(testTrade);

            var avgCostExp = ((100 * 10.0m) + (100 * 11.0m) + (50 * 12.0m) + (50 * 14.0m)) / 300;
            var pos = testPortfolio.GetPosition(testSec, date);

            Assert.AreEqual(avgCostExp.ToDouble(), pos.AverageCost(date).ToDouble(), .001);

            // Sell some shares - average cost should not change
            testTrade = testTradeSell(testSec);
            testTrade.MarkExecuted(tradeBar.BarDateTime, 13.5m);
            testPortfolio.AddExecutedTrade(testTrade);

            Assert.AreEqual(avgCostExp.ToDouble(), pos.AverageCost(date).ToDouble(), .001);

            // Buy more

            // Execute another 50 shares @ $12.50
            testTrade = testTradeBuy(testSec);
            testTrade.Quantity = 50;
            testTrade.MarkExecuted(tradeBar.BarDateTime, 12.5m);
            testPortfolio.AddExecutedTrade(testTrade);

            avgCostExp = ((avgCostExp * 200) + (50 * 12.50m)) / 250;

            Assert.AreEqual(avgCostExp.ToDouble(), pos.AverageCost(date).ToDouble(), .001);

        }

        [TestMethod]
        public void MultiTradeTest4()
        {
            PortfolioSetup testSetupParams = new PortfolioSetup(PortfolioDirection.LongShort, PortfolioMarginType.RegTMargin, 10000, new DateTime(2019, 11, 18));
            Portfolio testPortfolio = new Portfolio(testSetupParams);
            var testSec = testSecurity1();
            var testTrade = testTradeSell(testSec);
            var balance = testSetupParams.InitialCashBalance;

            // Valid monday with full trading week
            DateTime date = new DateTime(2019, 11, 18);

            var tradeBar = testSec.GetPriceBar(date, true);
            tradeBar.SetPriceValues(10m, 15m, 5m, 12m);

            var executionPrice = tradeBar.Open;

            // Mark this as an executed trade - buy 100 shares @ 10.00
            testTrade.TradeDate = tradeBar.BarDateTime;
            testTrade.MarkExecuted(tradeBar.BarDateTime, 10.0m);
            testPortfolio.AddExecutedTrade(testTrade);

            decimal commission = TradingEnvironment.Instance.CommissionCharged(testTrade);

            // Execute another 100 shares @ $9.00
            testTrade = testTradeSell(testSec);
            testTrade.MarkExecuted(tradeBar.BarDateTime, 9.0m);
            testPortfolio.AddExecutedTrade(testTrade);

            // Execute another 50 shares @ $8.00
            testTrade = testTradeSell(testSec);
            testTrade.Quantity = 50;
            testTrade.MarkExecuted(tradeBar.BarDateTime, 8.0m);
            testPortfolio.AddExecutedTrade(testTrade);

            // Execute another 50 shares @ 6.00
            testTrade = testTradeSell(testSec);
            testTrade.Quantity = 50;
            testTrade.MarkExecuted(tradeBar.BarDateTime, 6.0m);
            testPortfolio.AddExecutedTrade(testTrade);

            var avgCostExp = ((100 * 10.0m) + (100 * 9.0m) + (50 * 8.0m) + (50 * 6.0m)) / 300;
            var pos = testPortfolio.GetPosition(testSec, date);

            Assert.AreEqual(avgCostExp.ToDouble(), pos.AverageCost(date).ToDouble(), .001);

            // Buy some shares - average cost should not change
            testTrade = testTradeBuy(testSec);
            testTrade.MarkExecuted(tradeBar.BarDateTime, 7.5m);
            testPortfolio.AddExecutedTrade(testTrade);

            Assert.AreEqual(avgCostExp.ToDouble(), pos.AverageCost(date).ToDouble(), .001);

            // Buy more

            // Execute another 50 shares @ $6.5
            testTrade = testTradeSell(testSec);
            testTrade.Quantity = 50;
            testTrade.MarkExecuted(tradeBar.BarDateTime, 6.5m);
            testPortfolio.AddExecutedTrade(testTrade);

            avgCostExp = ((avgCostExp * 200) + (50 * 6.50m)) / 250;

            Assert.AreEqual(avgCostExp.ToDouble(), pos.AverageCost(date).ToDouble(), .001);
        }

        [TestMethod]
        public void RegTTest()
        {
            // Test scenario outlined at IBKR https://www.interactivebrokers.com/en/index.php?f=24862

            PortfolioSetup testSetupParams = new PortfolioSetup(PortfolioDirection.LongShort, PortfolioMarginType.RegTMargin, 10000, new DateTime(2019, 11, 18));
            Portfolio testPortfolio = new Portfolio(testSetupParams);

            TradingEnvironment.Instance.NegateCommissionForTesting = true;

            var testSecXYZ = new Security("XYZ", SecurityType.USCommonEquity);
            var testSecABC = new Security("ABC", SecurityType.USCommonEquity);

            DateTime[] day = new DateTime[]
            {
                new DateTime(2019, 11, 15),
                new DateTime(2019, 11, 18), // Day 1
                new DateTime(2019, 11, 19),
                new DateTime(2019, 11, 20),
                new DateTime(2019, 11, 21),
                new DateTime(2019, 11, 22)  // Day 5
            };

            // Setup stock as described
            testSecXYZ.GetPriceBar(day[1], true).SetPriceValues(40m, 40m, 40m, 40m);
            testSecXYZ.GetPriceBar(day[2], true).SetPriceValues(40m, 40m, 40m, 40m);
            testSecXYZ.GetPriceBar(day[3], true).SetPriceValues(40m, 45m, 35m, 40m);
            testSecXYZ.GetPriceBar(day[4], true).SetPriceValues(45m, 45m, 45m, 45m);
            testSecXYZ.GetPriceBar(day[5], true).SetPriceValues(40m, 40m, 40m, 40m);

            testSecABC.GetPriceBar(day[1], true).SetPriceValues(101m, 101m, 101m, 101m);
            testSecABC.GetPriceBar(day[2], true).SetPriceValues(101m, 101m, 101m, 101m);
            testSecABC.GetPriceBar(day[3], true).SetPriceValues(101m, 101m, 101m, 101m);
            testSecABC.GetPriceBar(day[4], true).SetPriceValues(101m, 101m, 101m, 101m);
            testSecABC.GetPriceBar(day[5], true).SetPriceValues(101m, 101m, 75m, 100m);

            // Day 1
            /*
               Deposit $10,000.00 Cash in Margin Account.
               After the deposit, account values look like this:
               Cash	$10,000.00	(Initial deposit)
               Securities Market Value	$0.00	(No positions held)
               Equity with Loan Value (ELV1)	$10,000.00	
               Initial Margin	$0.00	IM = 25% * Stock Value
               Maintenance Margin (MM)	$0.00	MM = 25% * Stock Value
               Available Funds	$10,000.00	ELV - IM
               Excess Liquidity	$10,000.00	ELV - MM          
               
               Reg T Margin	$0.00	Reg T Margin = 50% * Stock Value
               SMA2	$10,000.00	SMA >= 0
               SMA Requirement Satisfied, NO liquidation
             */

            Assert.AreEqual(10000.00m, testPortfolio.TotalCashValue(day[1]));
            Assert.AreEqual(0.0m, testPortfolio.SecuritiesMarketValue(day[1], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(10000.00m, testPortfolio.EquityWithLoanValue(day[1], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(0.0m, testPortfolio.BrokerInitialMarginRequirement(day[1], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(0.0m, testPortfolio.BrokerMaintenanceMarginRequirement(day[1], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(10000.0m, testPortfolio.AvailableFunds(day[1], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(10000.0m, testPortfolio.ExcessLiquidity(day[1], TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(10000.0m, testPortfolio.SpecialMemorandumAccountBalance(day[1], TimeOfDay.MarketEndOfDay));

            // Day 2
            /*
               Customer BUYS 500 shares of XYZ stock at $40.00/share.
               Total Amount = $20,000.00. After the trade, account values look like this:
               Cash	($10,000.00)	
               Securities Market Value	$20,000.00	
               Equity with Loan Value (ELV1)	$10,000.00	
               Initial Margin	$5,000.00	IM = 25% * Stock Value
               Maintenance Margin (MM)	$5,000.00	MM = 25% * Stock Value
               Available Funds	$5,000.00	ELV-IM
               Available Funds were >=0 at the time of the trade, so the trade was submitted.
               Excess Liquidity	$5,000.00	ELV - MM             
             */

            var trade1 = new Trade(testSecXYZ, TradeActionBuySell.Buy, 500, TradeType.Limit, 40.0m)
            {
                TradeStatus = TradeStatus.Pending
            };

            trade1.MarkExecuted(day[2], 40.0m);
            testPortfolio.AddExecutedTrade(trade1);

            Assert.AreEqual(-10000.00m, testPortfolio.TotalCashValue(day[2]));
            Assert.AreEqual(20000.0m, testPortfolio.SecuritiesMarketValue(day[2], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(10000.00m, testPortfolio.EquityWithLoanValue(day[2], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(5000.0m, testPortfolio.BrokerInitialMarginRequirement(day[2], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(5000.0m, testPortfolio.BrokerMaintenanceMarginRequirement(day[2], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(5000.0m, testPortfolio.AvailableFunds(day[2], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(5000.0m, testPortfolio.ExcessLiquidity(day[2], TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0.0m, testPortfolio.SpecialMemorandumAccountBalance(day[2], TimeOfDay.MarketEndOfDay));

            // Day 3 - 1
            /*
              First, the price of XYZ rises to 45.00/share.
              Account values now look like this:  
              Cash	($10,000.00)	
              Securities Market Value	$22,500.00	
              Equity with Loan Value (ELV1)	$12,500.00	
              Initial Margin	$5,625.00	IM = 25% * Stock Value
              Maintenance Margin (MM)	$5,625.00	MM = 25% * Stock Value
              Available Funds	$6,875.00	ELVIM
              Excess Liquidity	$6,875.00	ELV - MM
              Excess Liquidity >=0, so NO LIQUIDATION occurs.
             */

            // Our methods calculate EOD, so set price bar accordingly for this intraday scenario
            testSecXYZ.GetPriceBar(day[3]).SetPriceValues(45m, 45m, 45m, 45m);

            Assert.AreEqual(-10000.00m, testPortfolio.TotalCashValue(day[3]));
            Assert.AreEqual(22500, testPortfolio.SecuritiesMarketValue(day[3], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(12500.0m, testPortfolio.EquityWithLoanValue(day[3], TimeOfDay.MarketEndOfDay));

            // IBKR initial and maintenance margin is the same; our methods calculate initial based on the execution price for use in trade approval
            // Assert.AreEqual(5625.0m, testPortfolio.BrokerInitialMarginRequirement(day[3]));

            Assert.AreEqual(5625.0m, testPortfolio.BrokerMaintenanceMarginRequirement(day[3], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(6875.0m, testPortfolio.AvailableFunds(day[3], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(6875.0m, testPortfolio.ExcessLiquidity(day[3], TimeOfDay.MarketEndOfDay));

            //var log = testPortfolio.ToStringAllAccounting(day[3]);
            //log.AddRange(testPortfolio.ToStringAllActivity(day[3]));

            //OutputToTextFile(log);

            // Day 3 - 2
            /*
              Then the price of XYZ falls to $35.00/share.
              Account values now look like this:
              Cash	($10,000.00)	
              Securities Market Value	$17,500.00	
              Equity with Loan Value (ELV1)	$7,500.00	
              Initial Margin	$4,375.00	IM = 25% * Stock Value
              Maintenance Margin (MM)	$4,375.00	MM = 25% * Stock Value
              Available Funds	$3,125.00	ELVIM
              Excess Liquidity	$3,125.00	ELV - MM
              Reg T Margin	$8,750.00	Reg T Margin = 50% * Stock Value
              SMA2	$0.00
             */

            testSecXYZ.GetPriceBar(day[3]).SetPriceValues(35m, 35m, 35m, 35m);

            Assert.AreEqual(-10000.00m, testPortfolio.TotalCashValue(day[3]));
            Assert.AreEqual(17500.0m, testPortfolio.SecuritiesMarketValue(day[3], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(7500.0m, testPortfolio.EquityWithLoanValue(day[3], TimeOfDay.MarketEndOfDay));

            // Assert.AreEqual(5625.0m, testPortfolio.BrokerInitialMarginRequirement(day[3]));

            Assert.AreEqual(4375.0m, testPortfolio.BrokerMaintenanceMarginRequirement(day[3], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(3125.0m, testPortfolio.AvailableFunds(day[3], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(3125.0m, testPortfolio.ExcessLiquidity(day[3], TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(0.0m, testPortfolio.SpecialMemorandumAccountBalance(day[3], TimeOfDay.MarketEndOfDay));

            // Day 4
            /*
              Customer SELLS 500 shares of XYZ at $45.00/share.
              Total Amount = $22,500.00. After the trade, account values look like this:
              Cash	$12,500.00	
              Securities Market Value	$0.00	Positions no longer held.
              Equity with Loan Value (ELV1)	$12,500.00	
              Initial Margin	$0.00	IM = 25% * Stock Value
              Maintenance Margin (MM)	$0.00	MM = 25% * Stock Value
              Available Funds	$12,500.00	ELV-IM
              Excess Liquidity	$12,500.00	ELV - MM
              Reg T Margin	$0.00	Reg T Margin = 50% * Stock Value
              SMA2	$12,500.00
             */

            var trade2 = new Trade(testSecXYZ, TradeActionBuySell.Sell, 500, TradeType.Limit, 45.00m)
            {
                TradeStatus = TradeStatus.Pending
            };

            trade2.MarkExecuted(day[4], 45.0m);
            testPortfolio.AddExecutedTrade(trade2);

            Assert.AreEqual(12500.0m, testPortfolio.TotalCashValue(day[4]));
            Assert.AreEqual(0.0m, testPortfolio.SecuritiesMarketValue(day[4], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(12500.0m, testPortfolio.EquityWithLoanValue(day[4], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(0.0m, testPortfolio.BrokerInitialMarginRequirement(day[4], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(0.0m, testPortfolio.BrokerMaintenanceMarginRequirement(day[4], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(12500.0m, testPortfolio.AvailableFunds(day[4], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(12500.0m, testPortfolio.ExcessLiquidity(day[4], TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(12500.0m, testPortfolio.SpecialMemorandumAccountBalance(day[4], TimeOfDay.MarketEndOfDay));

            // Day 5 - 1
            /*
              Customer attempts to BUY 500 shares of ABC stock at $101.00/share.
              Total Amount = $50,500.00. Account values at the time of the attempted trade would look like this:
              Cash	$12,500.00	
              Securities Market Value	$0.00	
              Equity with Loan Value (ELV1)	$12,500.00	
              Initial Margin	$12,625.00	IM = 25% * Stock Value
              Maintenance Margin (MM)	$12,625.00	MM = 25% * Stock Value
              Available Funds	($125.00)	ELV-IM
              Available Funds <=0 so the trade is Rejected.
              Excess Liquidity	($125.00)	ELV - MM
             */

            var trade3 = new Trade(testSecABC, TradeActionBuySell.Buy, 500, TradeType.Limit, 101.00m)
            {
                TradeStatus = TradeStatus.Pending
            };

            var copyPort = testPortfolio.Copy();
            var copyTrade = trade3.Copy();
            copyTrade.MarkExecuted(day[5], 101.00m);
            copyPort.AddExecutedTrade(copyTrade);

            // Post-trade available funds are negative, so this trade would be rejected
                        
            Assert.AreEqual(-125.0m, copyPort.AvailableFunds(day[5], TimeOfDay.MarketOpen));

            // Test rule implementation
            var rule = new TradeApprovalRule_2("Rule2");
            Assert.IsFalse(rule.Run(trade3, testPortfolio, day[5], TimeOfDay.MarketOpen));


            copyTrade = null;
            copyPort = null;

            // Day 5 - 2
            /*
              Later on Day 5, the customer buys some stock.
              Customer BUYS 300 shares of ABC stock at $100.00/share.
              Total Amount = $30,000.00. After the trade, account values look like this:
              Cash	($17,500.00)	
              Securities Market Value	$30,000.00	
              Equity with Loan Value (ELV1)	$12,500.00	
              Initial Margin	$7,500.00	IM = 25% * Stock Value
              Maintenance Margin (MM)	$7,500.00	MM = 25% * Stock Value
              Available Funds	$5,000.00	ELVIM
              Excess Liquidity	$5,000.00	ELV - MM
              Reg T Margin	$15,000.00	Reg T Margin = 50% * Stock Value
              SMA2	-$2,500.00
             */

            testSecABC.GetPriceBar(day[5]).SetPriceValues(100m, 100m, 100m, 100m);

            var trade4 = new Trade(testSecABC, TradeActionBuySell.Buy, 300, TradeType.Limit, 100.00m)
            {
                TradeStatus = TradeStatus.Pending
            };

            trade4.MarkExecuted(day[5], 100.0m);
            testPortfolio.AddExecutedTrade(trade4);

            Assert.AreEqual(-17500.0m, testPortfolio.TotalCashValue(day[5]));
            Assert.AreEqual(30000.0m, testPortfolio.SecuritiesMarketValue(day[5], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(12500.0m, testPortfolio.EquityWithLoanValue(day[5], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(7500.0m, testPortfolio.BrokerInitialMarginRequirement(day[5], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(7500.0m, testPortfolio.BrokerMaintenanceMarginRequirement(day[5], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(5000.0m, testPortfolio.AvailableFunds(day[5], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(5000.0m, testPortfolio.ExcessLiquidity(day[5], TimeOfDay.MarketEndOfDay));

            Assert.AreEqual(-2500.0m, testPortfolio.SpecialMemorandumAccountBalance(day[5], TimeOfDay.MarketEndOfDay));

            // Day 5 - 2 alternate
            /*
              Consider an alternate Day 5 scenario in which the price of ABC stock drops.
              Price of ABC stock drops to $75.00/share.
              Account values would now look like this:
              Cash	($17,500.00)	
              Securities Market Value	$22,500.00	
              Equity with Loan Value (ELV1)	$5,000.00	
              Initial Margin	$5,625.00	IM = 25% * Stock Value
              Maintenance Margin (MM)	$5,625.00	MM = 25% * Stock Value
              Available Funds	($625.00)	ELVIM
              Excess Liquidity	($625.00)
             */

            testSecABC.GetPriceBar(day[5]).SetPriceValues(75m, 75m, 75m, 75m);

            Assert.AreEqual(-17500.0m, testPortfolio.TotalCashValue(day[5]));
            Assert.AreEqual(22500.0m, testPortfolio.SecuritiesMarketValue(day[5], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(5000.0m, testPortfolio.EquityWithLoanValue(day[5], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(5625.0m, testPortfolio.BrokerInitialMarginRequirement(day[5], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(5625.0m, testPortfolio.BrokerMaintenanceMarginRequirement(day[5], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(-625.0m, testPortfolio.AvailableFunds(day[5], TimeOfDay.MarketEndOfDay));
            Assert.AreEqual(-625.0m, testPortfolio.ExcessLiquidity(day[5], TimeOfDay.MarketEndOfDay));

            TradingEnvironment.Instance.NegateCommissionForTesting = false;
        }

        [TestMethod]
        public void WhatIfTest1()
        {
            PortfolioSetup testSetupParams = new PortfolioSetup(PortfolioDirection.LongShort, PortfolioMarginType.RegTMargin, 10000, new DateTime(2019, 11, 18));
            Portfolio testPortfolio = new Portfolio(testSetupParams);
            var testSec = testSecurity1();

            var testTrade1 = testTradeBuy(testSec);
            var testTrade2 = testTradeBuy(testSec);

            var balance = testSetupParams.InitialCashBalance;

            // Valid monday with full trading week
            DateTime date = new DateTime(2019, 11, 18);

            var tradeBar = testSec.GetPriceBar(date, true);
            tradeBar.SetPriceValues(10m, 15m, 5m, 12m);

            testTrade1.MarkExecuted(date, 10.0m);
            testPortfolio.AddExecutedTrade(testTrade1);

            var portCopy = testPortfolio.Copy();

            testTrade2.MarkExecuted(date, 15.0m);
            portCopy.AddExecutedTrade(testTrade2);

            Assert.AreNotEqual(testPortfolio.StockValue(date, TimeOfDay.MarketEndOfDay), portCopy.StockValue(date, TimeOfDay.MarketEndOfDay));

            //portCopy.ToStringAllActivity(date).ForEach(x => Console.WriteLine(x));

            //var log = portCopy.ToStringAllAccounting(date);
            //log.AddRange(portCopy.ToStringAllActivity(date));

            //OutputToTextFile(log);

        }

    }


}
