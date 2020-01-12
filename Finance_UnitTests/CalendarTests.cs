using Finance;
using static Finance.Helpers;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Finance_UnitTests
{
    [TestClass]
    public class CalendarTests
    {
        [TestMethod]
        public void Test_CalendarTradingHolidays()
        {

            Assert.IsFalse(Calendar.IsTradingDay(new DateTime(2019, 12, 25)));
            Assert.IsFalse(Calendar.IsTradingDay(new DateTime(2019, 11, 28)));
            Assert.IsFalse(Calendar.IsTradingDay(new DateTime(2020, 4, 10)));
            Assert.IsFalse(Calendar.IsTradingDay(new DateTime(2021, 5, 31)));
            Assert.IsFalse(Calendar.IsTradingDay(new DateTime(2019, 11, 2)));
            Assert.IsFalse(Calendar.IsTradingDay(new DateTime(2020, 7, 3)));
            Assert.IsFalse(Calendar.IsTradingDay(new DateTime(2021, 7, 5)));

            Assert.IsTrue(Calendar.IsTradingDay(new DateTime(2019, 1, 4)));

            Assert.IsTrue(Calendar.IsTradingDay(new DateTime(2019, 10, 31)));
            Assert.IsTrue(Calendar.IsTradingDay(new DateTime(2021,12,27)));



        }
    }
}
