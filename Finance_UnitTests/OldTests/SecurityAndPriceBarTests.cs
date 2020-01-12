using Finance;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using static Finance.Calendar;

namespace Finance_UnitTests
{
    [TestClass]
    public class SecurityAndPriceBarTests
    {



        [TestMethod]
        public void SecurityTest1()
        {

            Security TestSecurity() => new Security
            {
                Ticker = "XYZ",
                Exchange = "Test Exchange",
                SecurityType = SecurityType.USCommonEquity
            };

            DateTime date = new DateTime(2019, 11, 18);

            Security testSec1 = TestSecurity();
            Security testSec2 = TestSecurity();

            Assert.AreEqual(testSec1, testSec2);

            Assert.IsTrue(testSec1 == testSec2);

            PriceBar testBar = testSec1.GetPriceBar(date, true);

            Assert.IsTrue(testSec1.PriceBarData.Count == 1);

        }

        [TestMethod]
        public void BasicTests()
        {

            var TestPriceBar = new PriceBar();

            decimal open, high, low, close;

            open = 1.0m;
            high = 2.0m;
            low = 0.50m;
            close = 1.5m;

            TestPriceBar.SetPriceValues(open, high, low, close);

            Assert.AreEqual(0.50m, TestPriceBar.Change);
            Assert.AreEqual(1.50m, TestPriceBar.Range);

            Assert.AreEqual(1.50m, TestPriceBar.TrueRange());

        }

        [TestMethod]
        public void ComparisonTests()
        {

            var TestBar1 = new PriceBar();
            var TestBar2 = new PriceBar();

            decimal open, high, low, close;

            open = 1.0m;
            high = 2.0m;
            low = 0.50m;
            close = 1.5m;

            TestBar1.SetPriceValues(open, high, low, close);
            TestBar2.SetPriceValues(open, high, low, close);

            Assert.IsTrue(TestBar1.Equals(TestBar2));

            DateTime barDate = DateTime.Today;

            TestBar1.BarDateTime = barDate;
            TestBar2.BarDateTime = barDate;

            Assert.IsTrue(TestBar1.Equals(TestBar2));

            var TestSec1 = new Security("XYZ");
            var TestSec2 = new Security("ABC");

            TestBar1.Security = TestSec1;
            TestBar2.Security = TestSec2;

            Assert.IsFalse(TestBar1.Equals(TestBar2));

        }

        [TestMethod]
        public void MultiBarsTest()
        {

            Security TestSec = new Security("XYZ");

            // Valid monday with full trading week
            DateTime FirstDay = new DateTime(2019, 11, 18);

            for (decimal i = 0; i < 3; i++)
            {
                var bar = TestSec.GetPriceBar(FirstDay.AddDays((int)i),true);
                bar.SetPriceValues(5.0m + i, 6.0m + i, 4.0m + i, 6.0m + i);
            }

            // Should have 3 price bars
            Assert.AreEqual(3, TestSec.PriceBarData.Count);

            var bars = TestSec.GetPriceBars();
            Assert.AreEqual(3, bars.Count);

            bars = TestSec.GetPriceBars(FirstDay, FirstDay.AddDays(2));
            Assert.AreEqual(3, bars.Count);

            // 2 bars
            bars = TestSec.GetPriceBars(FirstDay, FirstDay.AddDays(1));
            Assert.AreEqual(2, bars.Count);

            // Request invalid bar
            Assert.ThrowsException<InvalidTradingDateException>(() =>
            {
                // Try to get a bar that's on a Saturday
                TestSec.GetPriceBar(FirstDay.AddDays(5));
            });

            // Next and Prior function
            var firstBar = TestSec.GetPriceBar(FirstDay);
            var secondBar = firstBar.NextBar;
            Assert.AreEqual(FirstDay.AddDays(1), secondBar.BarDateTime);

            var thirdBar = secondBar.NextBar;
            Assert.AreEqual(FirstDay.AddDays(2), thirdBar.BarDateTime);

            secondBar = thirdBar.PriorBar;
            Assert.AreEqual(FirstDay.AddDays(1), secondBar.BarDateTime);

            Assert.AreEqual(thirdBar, firstBar.NextBar.NextBar);

            bars = firstBar.NextBars(true);
            Assert.AreEqual(3, bars.Count);

            bars = firstBar.NextBars(false);
            Assert.AreEqual(2, bars.Count);

            bars = thirdBar.PriorBars(2, true);
            Assert.AreEqual(2, bars.Count);

            bars = thirdBar.PriorBars(1, false);
            Assert.AreEqual(1, bars.Count);
            Assert.AreEqual(bars[0], secondBar);

            Assert.AreEqual(thirdBar, TestSec.LastBar());

        }

        [TestMethod]
        public void AtrTest1()
        {
            Security TestSec = new Security("XYZ");

            // Valid monday with full trading week
            DateTime date = new DateTime(2019, 11, 18);

            // Create a simple series which will support a 14-period ATR calculation
            for (decimal i = 0; i < 30; i++)
            {
                date = NextTradingDay(date, (int)i);
                var bar = TestSec.GetPriceBar(date, true);
                bar.SetPriceValues(5.0m + i, 6.0m + i, 4.0m + i, 6.0m + i);
            }

            // since all bars are the same +1, Range for all should be the same
            for (int i = 0; i < TestSec.PriceBarData.Count - 2; i++)
            {
                Assert.AreEqual(TestSec.PriceBarData[i].Range, TestSec.PriceBarData[i + 1].Range);
            }

            // True range should be the same for all bars after the first
            for (int i = 1; i < TestSec.PriceBarData.Count - 2; i++)
            {
                Assert.AreEqual(TestSec.PriceBarData[i].TrueRange(), TestSec.PriceBarData[i + 1].TrueRange());
            }

            // Average True Range at the end of the series should be the same as True Range
            int period = 14;
            var lastBar = TestSec.LastBar();
            var atr = lastBar.AverageTrueRange(period);
            Assert.AreEqual(2.0m, atr);

        }

        [TestMethod]
        public void AtrTest2()
        {
            Security TestSec = new Security("XYZ");

            // Valid monday with full trading week
            DateTime date = new DateTime(2019, 11, 18);

            Stopwatch sw = new Stopwatch();

            // Load Test Date
            var data = AtrTestValues();

            for (int i = 0; i < 30; i++)
            {
                date = NextTradingDay(date, i);
                var bar = TestSec.GetPriceBar(date, true);
                bar.SetPriceValues(data[i][0], data[i][1], data[i][2], data[i][3]);
            }

            int period = 14;

            sw.Start();
            // Check all days starting on day 16 - the first day (15) will be off slightly due to calculations used at the start of the series
            for (int i = 15; i < TestSec.PriceBarData.Count; i++)
            {
                decimal atr = TestSec.PriceBarData[i].AverageTrueRange(period);
                double delta = .005;

                Assert.AreEqual((double)atr, (double)KnownAtrValues()[i-1], delta);
            }
            sw.Stop();
            Console.WriteLine($"First calculation: {sw.ElapsedTicks}");

            sw.Restart();
            for (int i = 15; i < TestSec.PriceBarData.Count; i++)
            {
                decimal atr = TestSec.PriceBarData[i].AverageTrueRange(period);
                double delta = .005;

                Assert.AreEqual((double)atr, (double)KnownAtrValues()[i-1], delta);
            }
            sw.Stop();
            Console.WriteLine($"Second calculation: {sw.ElapsedTicks}");

        }

        /// <summary>
        /// Returns an array of price data with known ATR calculation on 14-day period
        /// </summary>
        /// <returns></returns>
        private static List<decimal[]> AtrTestValues()
        {
            return new List<decimal[]>()
            {
                new decimal[] {108.14m,109.54m,108.08m,109.48m},
                new decimal[] {109.63m,110.23m,109.21m,109.38m},
                new decimal[] {109.1m,109.37m,108.34m,109.22m},
                new decimal[] {109.23m,109.6m,109.02m,109.08m},
                new decimal[] {108.77m,109.69m,108.36m,109.36m},
                new decimal[] {108.86m,109.1m,107.85m,108.51m},
                new decimal[] {108.59m,109.32m,108.53m,108.85m},
                new decimal[] {108.57m,108.75m,107.68m,108.03m},
                new decimal[] {107.39m,107.88m,106.68m,107.57m},
                new decimal[] {107.41m,107.95m,106.31m,106.94m},
                new decimal[] {106.62m,107.44m,106.29m,106.82m},
                new decimal[] {105.8m,106.5m,105.5m,106m},
                new decimal[] {105.66m,106.57m,105.64m,106.1m},
                new decimal[] {106.14m,106.8m,105.62m,106.73m},
                new decimal[] {107.7m,108m,106.82m,107.73m},
                new decimal[] {107.9m,108.3m,107.51m,107.7m},
                new decimal[] {107.83m,108.76m,107.07m,108.36m},
                new decimal[] {107.25m,107.27m,105.24m,105.52m},
                new decimal[] {104.64m,105.72m,103.13m,103.13m},
                new decimal[] {102.65m,105.72m,102.53m,105.44m},
                new decimal[] {107.51m,108.79m,107.24m,107.95m},
                new decimal[] {108.73m,113.03m,108.6m,111.77m},
                new decimal[] {113.86m,115.73m,113.49m,115.57m},
                new decimal[] {115.12m,116.13m,114.04m,114.92m},
                new decimal[] {115.19m,116.18m,113.25m,113.58m},
                new decimal[] {113.05m,114.12m,112.51m,113.57m},
                new decimal[] {113.85m,113.99m,112.44m,113.55m},
                new decimal[] {114.35m,114.94m,114m,114.62m},
                new decimal[] {114.42m,114.79m,111.55m,112.71m},
                new decimal[] {111.64m,113.39m,111.55m,112.88m},
            };
        }

        private static decimal[] KnownAtrValues()
        {
            return new decimal[]
            {
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                0.00000m,
                1.18400m,
                1.15586m,
                1.19401m,
                1.33158m,
                1.42147m,
                1.54779m,
                1.67652m,
                1.91963m,
                2.06537m,
                2.06713m,
                2.12876m,
                2.09171m,
                2.05301m,
                2.00566m,
                2.09382m,
                2.07569m
            };
        }
    }
}
