using Finance;
using Finance.Data;
using IBApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using static Finance.Helpers;


namespace Finance_UnitTests
{
    [TestClass]
    public class InteractiveBrokersTests
    {

        int Port = 4002;

        bool IbLock = false;

        [TestMethod]
        public void TestConnect()
        {
            IDataProvider data = new IbkrDataProvider(Port);

            data.Connect();

            int timeout = 30;
            while (!data.Connected)
            {
                Thread.Sleep(100);
                if (--timeout <= 0)
                    break;
            }

            Assert.IsTrue(data.Connected);

            data.Disconnect();
        }

        [TestMethod]
        public void TestDisconnect()
        {
            IDataProvider data = new IbkrDataProvider(Port);

            data.Connect();

            int timeout = 30;
            while (!data.Connected)
            {
                Thread.Sleep(100);
                if (--timeout <= 0)
                    break;
            }

            Assert.IsTrue(data.Connected);

            data.Disconnect();

            Assert.IsFalse(data.Connected);
        }

        /// <summary>
        /// Request a single bar of data and verify that it is received
        /// </summary>
        [TestMethod]
        public void TestGetData1_RunIndependently()
        {
            while (IbLock)
                Thread.Sleep(500);
            IbLock = true;


            IDataProvider data = new IbkrDataProvider(Port);

            data.Connect();

            int timeout = 30;
            while (!data.Connected)
            {
                Thread.Sleep(100);
                if (--timeout <= 0)
                    break;
            }

            Assert.IsTrue(data.Connected);

            bool requestComplete = false;
            DateTime requestDate = new DateTime(2019, 11, 20);

            data.OnSecurityDataResponse += (s, e) =>
            {
                Assert.AreEqual(1, e.security.PriceBarData.Count);
                Assert.AreEqual(requestDate, e.security.PriceBarData[0].BarDateTime);
                requestComplete = true;
                data.Disconnect();
                IbLock = false;
            };


            Security reqSec = new Security("F");
            data.RequestPriceData(reqSec, requestDate, requestDate);

            timeout = 100;
            while (!requestComplete)
            {
                Thread.Sleep(100);
                Assert.IsTrue(--timeout > 0);
            }

            data.Disconnect();
            IbLock = false;
        }

        /// <summary>
        /// Request multiple bars of data and verify that they are received
        /// </summary>
        [TestMethod]
        public void TestGetData2_RunIndependently()
        {
            while (IbLock)
                Thread.Sleep(500);
            IbLock = true;

            IDataProvider data = new IbkrDataProvider(Port);

            data.Connect();

            int timeout = 30;
            while (!data.Connected)
            {
                Thread.Sleep(100);
                if (--timeout <= 0)
                    break;
            }

            Assert.IsTrue(data.Connected);

            bool requestComplete = false;

            data.OnSecurityDataResponse += (s, e) =>
            {
                requestComplete = true;
                Assert.AreEqual(5, e.security.PriceBarData.Count);
                data.Disconnect();
                IbLock = false;
            };

            DateTime requestDateStart = new DateTime(2019, 11, 18);
            DateTime requestDateEnd = new DateTime(2019, 11, 22);

            Security reqSec = new Security("AAPL");
            data.RequestPriceData(reqSec, requestDateStart, requestDateEnd);

            timeout = 50;
            while (!requestComplete)
            {
                Thread.Sleep(100);
                Assert.IsTrue(--timeout > 0);
            }

            data.Disconnect();
            IbLock = false;
        }

        /// <summary>
        /// Request multiple bars from multiple securities and verify that they are received
        /// </summary>
        [TestMethod] 
        public void TestGetData3_RunIndependently()
        {
            while (IbLock)
                Thread.Sleep(500);
            IbLock = true;

            IDataProvider data = new IbkrDataProvider(Port);

            data.Connect();

            int timeout = 30;
            while (!data.Connected)
            {
                Thread.Sleep(100);
                if (--timeout <= 0)
                    break;
            }

            Assert.IsTrue(data.Connected);

            bool requestComplete = false;

            int receivedCount = 0;

            DateTime requestDateStart = new DateTime(2019, 11, 18);
            DateTime requestDateEnd = new DateTime(2019, 11, 22);

            Security reqSec1 = new Security("AMZN");
            Security reqSec2 = new Security("AAPL");
            Security reqSec3 = new Security("MAR");
            Security reqSec4 = new Security("F");
            Security reqSec5 = new Security("DIS");

            data.OnSecurityDataResponse += (s, e) =>
            {
                switch (e.security.Ticker)
                {
                    case "AMZN":
                        if (e.security.PriceBarData.Count == 5)
                            receivedCount += 1;

                        break;
                    case "AAPL":
                        if (e.security.PriceBarData.Count == 5)
                            receivedCount += 1;

                        break;
                    case "MAR":
                        if (e.security.PriceBarData.Count == 5)
                            receivedCount += 1;
                        break;
                    case "F":
                        if (e.security.PriceBarData.Count == 5)
                            receivedCount += 1;
                        break;
                    case "DIS":
                        if (e.security.PriceBarData.Count == 5)
                            receivedCount += 1;
                        break;
                }

                if (receivedCount == 3)
                {
                    requestComplete = true;
                    data.Disconnect();
                    IbLock = false;
                }
            };

            data.RequestPriceData(reqSec1, requestDateStart, requestDateEnd);
            data.RequestPriceData(reqSec2, requestDateStart, requestDateEnd);
            data.RequestPriceData(reqSec3, requestDateStart, requestDateEnd);
            data.RequestPriceData(reqSec4, requestDateStart, requestDateEnd);
            data.RequestPriceData(reqSec5, requestDateStart, requestDateEnd);

            timeout = 50;
            while (!requestComplete)
            {
                Thread.Sleep(100);
                Assert.IsTrue(--timeout > 0);
            }

            data.Disconnect();
            IbLock = false;

        }

        /// <summary>
        /// Request the earliest price bar available for a security
        /// </summary>
        // [TestMethod] Long running
        public void TestGetData4_RunIndependently()
        {
            while (IbLock)
                Thread.Sleep(500);
            IbLock = true;

            IDataProvider data = new IbkrDataProvider(Port);

            data.Connect();

            int timeout = 30;
            while (!data.Connected)
            {
                Thread.Sleep(100);
                if (--timeout <= 0)
                    break;
            }

            Assert.IsTrue(data.Connected);

            bool requestComplete = false;

            DateTime requestDateEnd = new DateTime(2019, 11, 22);

            Security reqSec1 = new Security("AMZN");

            data.OnSecurityDataResponse += (s, e) =>
            {
                Console.WriteLine($"Received {e.security.PriceBarData.Count} bars. " +
                    $"First DateTime: {e.security.GetPriceBars()[0].BarDateTime}, " +
                    $"last DateTime {e.security.GetPriceBars()[e.security.PriceBarData.Count - 1].BarDateTime}");

                requestComplete = true;                
            };

            data.RequestPriceData(reqSec1, requestDateEnd);

            timeout = 100;
            while (!requestComplete)
            {
                Thread.Sleep(100);
            }

            Assert.IsTrue(true);
            data.Disconnect();
            IbLock = false;
        }

        /// <summary>
        /// Test update of a security to the database
        /// </summary>
        [TestMethod]
        public void DataManagerTest2_RunIndependently()
        {

            IDataProvider dataProvider = new IbkrDataProvider(Port);
            PriceDatabase database = new PriceDatabase();

            DataManager manager = new DataManager(dataProvider, database);

            Security testSec = manager.GetSecurity("TWTR");

            bool complete = false;

            manager.SecurityDataResponse += (s, e) =>
            {
                Console.WriteLine($"DataManager received data for {e.security.Ticker}");
                complete = true;
            };

            manager.UpdateSecurity(testSec, DateTime.Today);

            int timeout = 1000;
            while (!complete)
            {
                Thread.Sleep(100);
                Assert.IsTrue(--timeout > 0);
            }

            // Requires manual cancel but works

            manager.CloseDataConnection();

        }

        /// <summary>
        /// Reads a set of symbols from symbol list and attempts to populate the database with all available data
        /// </summary>
       //  [TestMethod] long Running
        public void DatamanagerTest3_RunIndependently()
        {
            IDataProvider dataProvider = new IbkrDataProvider(Port);
            PriceDatabase database = new PriceDatabase();

            DataManager manager = new DataManager(dataProvider, database);

            Dictionary<string, string> symbols = Helpers.ReadSymbols(GetSymbolLists()[0]);

            bool complete = false;

            int limit = 5;

            int limit2 = limit;
            manager.SecurityDataResponse += (s, e) =>
            {
                Console.WriteLine($"DataManager received data for {e.security.Ticker}");
                if (--limit2 == 0)
                    complete = true;

            };

            foreach (var symbol in symbols)
            {
                var sec = manager.GetSecurity(symbol.Key);
                manager.UpdateSecurity(sec, DateTime.Today);
                if (--limit <= 0)
                    break;
            }

            while (!complete)
                Thread.Sleep(100);

            // Requires manual cancel but works

            manager.CloseDataConnection();
        }

     
    }
}
