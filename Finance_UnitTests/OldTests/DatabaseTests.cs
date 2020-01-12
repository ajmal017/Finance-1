using Finance;
using Finance.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using Finance.Data;
using static Finance.Helpers;

namespace Finance_UnitTests
{
    [TestClass] //Excluded for the time being
    public class DatabaseTests
    {

        [TestMethod]
        public void SecurityCreateTest()
        {

            PriceDatabase db = new PriceDatabase();

            var sec = db.GetSecurity("TEST1");

            var tickers = db.AllTickers;

            Assert.IsTrue(tickers.Count == 1);

            Assert.AreEqual("TEST1", tickers[0]);

        }

        [TestMethod]
        public void PriceDataUpdateTest()
        {

            PriceDatabase db = new PriceDatabase();

            var sec = db.GetSecurity("TEST2");

            // Assert that there are currently no price bars associated with this security
            Assert.IsTrue(db.PriceBarCount(sec.Ticker) == 0, "Fail 1");

            var date = new DateTime(2019, 11, 5);

            // Get a price bar with specific date (does not exist, will be created)
            var bar = sec.GetPriceBar(date);

            // Set price values and send to Set function to save in database
            bar.SetPriceValues(2.0m, 3.0m, 1.0m, 2.5m, 1000);
            db.SetSecurity(sec);

            // Assert that there is a single bar with associated ticker saved in the database
            Assert.IsTrue(db.PriceBarCount(sec.Ticker) == 1, "Fail 2");

            // Retrieve the security
            var checkSec = db.GetSecurity(sec.Ticker);

            // Assert that the security contains a single price bar
            Assert.IsTrue(checkSec.GetPriceBars().Count == 1, "Fail 3");

            // Check the bar values against input
            var checkBar = checkSec.PriceBarData[0];

            Assert.AreEqual(bar, checkBar, "Fail 4");

            // Add 2 more bars and update the database

            var barList = new List<PriceBar>
            {
                bar
            };

            bar = sec.GetPriceBar(date.AddDays(1));
            bar.SetPriceValues(2.1m, 3.1m, 1.1m, 2.6m, 2000);
            barList.Add(bar);

            bar = sec.GetPriceBar(date.AddDays(2));
            bar.SetPriceValues(2.2m, 3.2m, 1.2m, 2.7m, 3000);
            barList.Add(bar);

            db.SetSecurity(sec);

            // Assert that there are 3 bars with associated ticker saved in the database
            Assert.IsTrue(db.PriceBarCount(sec.Ticker) == 3, "Fail 5");

            // Retrieve the security
            checkSec = db.GetSecurity(sec.Ticker);

            // Assert that the security contains 3 price bars
            Assert.IsTrue(checkSec.GetPriceBars().Count == 3, "Fail 6");

            // Check the bar values against input
            var checkBars = checkSec.GetPriceBars();
            Assert.AreEqual(3, barList.Count, "Fail 7");

            foreach (var b in barList)
            {
                Assert.IsTrue(checkBars.Contains(b), "Fail 8");
            }

        }

    }
}
