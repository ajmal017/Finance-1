using Finance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using static Finance.Helpers;

namespace Finance_UnitTests
{
    [TestClass]
    public class HelperTests
    {
        [TestMethod]
        public void Test_DateIsBetween()
        {

            DateTime testDate = new DateTime(2019, 1, 1);
            DateTime earlyDate = new DateTime(2018, 1, 1);
            DateTime lateDate = new DateTime(2020, 1, 1);

            Assert.IsTrue(testDate.IsBetween(earlyDate, lateDate));

            earlyDate = earlyDate.AddDays(360);
            lateDate = lateDate.AddDays(-360);

            Assert.IsTrue(testDate.IsBetween(earlyDate, lateDate));

            earlyDate = lateDate.AddDays(-2);

            Assert.IsFalse(testDate.IsBetween(earlyDate, lateDate));
        }

        [TestMethod]
        public void IbkrFormatTimeTest()
        {

            DateTime dt1 = DateTime.Today;
            DateTime dt2 = dt1.AddDays(1);

            Assert.AreEqual("2 D", Helpers.ToIbkrDuration(dt1, dt2));

            dt2 = dt1.AddDays(10);

            Assert.AreEqual("11 D", Helpers.ToIbkrDuration(dt1, dt2));

            dt2 = dt1.AddDays(-1);

            Assert.ThrowsException<InvalidDateOrderException>(() => Helpers.ToIbkrDuration(dt1, dt2));

        }

        [TestMethod]
        public void TestEnumToInt()
        {
            TradeActionBuySell enum1 = TradeActionBuySell.Buy;
            PositionDirection enum2 = PositionDirection.LongPosition;

            Assert.AreEqual(enum1.ToInt(), enum2.ToInt());
        }        
    }
}
