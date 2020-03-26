using System;
using System.Collections.Generic;
using IBApi;
using System.Threading;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance.Data
{
    public class IbkrLiveDataProvider : LiveDataProvider, EWrapper
    {

        public override string Name => "IBKR Live Data";

        #region IBKR

        private static EClientSocket clientSocket;
        public readonly EReaderSignal signal;
        public int Port { get; }
        public int ClientId { get; }

        #endregion

        public IbkrLiveDataProvider(int port, int clientId)
        {
            Port = port;
            ClientId = clientId;
            signal = new EReaderMonitorSignal();
            clientSocket = new EClientSocket(this, signal);
        }

        protected (int reqId, Security security)? ActiveStreamingQuote = null;

        #region Connection Management

        public override void Connect()
        {
            if (!Connected)
            {
                Log(new LogMessage(ToString(), $"Connecting to IBKR Gateway on port {Port} (Live Data)", LogMessageType.Production));
                clientSocket?.eConnect("localhost", Port, ClientId);
            }
        }
        public override void Disconnect()
        {
            if (Connected)
            {
                Log(new LogMessage(ToString(), "Disconnecting from IBKR Gateway (Live Data)", LogMessageType.Production));
                clientSocket?.eDisconnect();
            }
        }

        #endregion

        #region ID Numbers

        //
        // ID used for identifying client requests (internal)
        //
        private int _NextRequestId { get; set; } = 40000;
        private int NextRequestId
        {
            get
            {
                _NextRequestId += 5;
                return _NextRequestId;
            }
        }

        private int SecurityId(int reqId)
        {
            return ((reqId / 5)) * 5;
        }
        private int GetActiveReqId(Security security)
        {
            if (ActiveStreamingQuote?.security != security)
                return -1;
            else
                return ActiveStreamingQuote.Value.reqId;
        }
        protected Security GetRequestSecurity(int reqId)
        {
            //var security = ActiveStreamingQuotes.SingleOrDefault(x => x.Value.reqId == SecurityId(reqId))?.security;
            if (ActiveStreamingQuote == null || (reqId - (reqId % 5) != ActiveStreamingQuote.Value.reqId))
                return null;
            else
            {
                return ActiveStreamingQuote?.security;
            }
        }

        private enum IbkrLiveDataRequestType
        {
            LastClose = 0,
            TodayIntradayMinutes = 1,
            StreamIntradayMinutesUpdates = 2,
            StreamBidAskTicks = 3,
            StreamLastTrades = 4
        }

        #endregion

        public override void RequestStreamingQuotes(Security security)
        {
            if (security == null)
                return;

            if (ActiveStreamingQuote?.security == security)
                return;

            Console.WriteLine("Submitting RTB request");

            if (ActiveStreamingQuote.HasValue)
                CancelStreamingQuotes(ActiveStreamingQuote?.security);

            var reqId = NextRequestId;

            ActiveStreamingQuote = (reqId, security);

            var lastCloseDate = DateTime.Today;
            if (!Calendar.IsTradingDay(lastCloseDate))
                lastCloseDate = Calendar.PriorTradingDay(lastCloseDate).AddHours(23);

            // Request historical data for last close
            clientSocket.reqHistoricalData(reqId + IbkrLiveDataRequestType.LastClose.ToInt(),
                security.GetContract(), lastCloseDate.ToIbkrFormat(), "1 D", "1 day", "TRADES", 1, 2, false, null);

            // Request historical data for intraday minutes
            clientSocket.reqHistoricalData(reqId + IbkrLiveDataRequestType.TodayIntradayMinutes.ToInt(),
                security.GetContract(), DateTime.Now.ToIbkrFormat(), "1 D", "1 min", "TRADES", 1, 2, false, null);

            // Request intraday minute updates
            clientSocket.reqHistoricalData(reqId + IbkrLiveDataRequestType.StreamIntradayMinutesUpdates.ToInt(),
                security.GetContract(), "", "1 D", "1 min", "TRADES", 1, 2, true, null);

            // Request streaming bid and ask
            clientSocket.reqTickByTickData(reqId + IbkrLiveDataRequestType.StreamBidAskTicks.ToInt(),
                security.GetContract(), "BidAsk", 1, true);

            // Request streaming last trades
            clientSocket.reqTickByTickData(reqId + IbkrLiveDataRequestType.StreamLastTrades.ToInt(),
                security.GetContract(), "Last", 1, true);
        }
        public override void CancelStreamingQuotes(Security security)
        {
            if (security == null)
                return;

            int reqId = GetActiveReqId(security);

            for (int i = reqId; i < reqId + 5; i++)
            {
                clientSocket.cancelTickByTickData(i);
                Thread.Sleep(20);
                clientSocket.cancelHistoricalData(i);
                Thread.Sleep(20);
                clientSocket.cancelMktData(i);
                Thread.Sleep(20);
            }
        }
        private void CancelAllStreamingQuotes()
        {
            CancelStreamingQuotes(ActiveStreamingQuote?.security);
            ActiveStreamingQuote = null;
        }

        #region System

        public void error(Exception e)
        {
            Log(new LogMessage(ToString(), e.Message, LogMessageType.TradingError));
        }
        public void error(string str)
        {
            Log(new LogMessage(ToString(), str, LogMessageType.TradingError));
        }
        public void error(int id, int errorCode, string errorMsg)
        {
            if (id == -1)
            {
                // Not an error, a notification

                Log(new LogMessage(ToString(), $"{errorCode}: {errorMsg}", LogMessageType.TradingSystemMessage));
            }
            else
            {
                // Handle
                switch (errorCode)
                {
                    case 162: // Historical data cancelled
                    case 300:
                    case 366:
                        // Ignore
                        break;
                    default:
                        Log(new LogMessage(ToString(), $"Unhandled TWS Trading System Message ID {errorCode}: {errorMsg}", LogMessageType.TradingError));
                        break;
                }
            }
        }

        public void connectAck()
        {
            try
            {
                if (Connected == clientSocket.IsConnected())
                    return;

                Connected = clientSocket.IsConnected();

                if (!Connected)
                    return;

                //Create a reader to consume messages from the TWS. The EReader will consume the incoming messages and put them in a queue
                EReader reader = new EReader(clientSocket, signal);
                reader.Start();

                //Once the messages are in the queue, an additional thread can be created to fetch them
                new Thread(() =>
                {
                    try
                    {
                        while (clientSocket.IsConnected())
                        {
                            signal.waitForSignal();
                            reader.processMsgs();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception in reader thread: {ex.Message}");
                    }
                })
                {
                    IsBackground = true
                }.Start();

            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString(), ex.Message, LogMessageType.SystemError));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return;
            }
        }
        public void connectionClosed()
        {
            try
            {
                Connected = clientSocket.IsConnected();
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString(), ex.Message, LogMessageType.SystemError));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return;
            }
        }

        #endregion

        #region Streaming Data

        public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize, TickAttribBidAsk tickAttribBidAsk)
        {
            var security = GetRequestSecurity(reqId);

            if (security == null)
                return;

            DateTime dt = time.FromIbkrTimeFormat();

            security.LastBid = bidPrice.ToDecimal();
            security.LastAsk = askPrice.ToDecimal();

            OnLiveQuoteReceived(security, LiveQuoteType.Bid, dt, bidPrice.ToDecimal(), bidSize);
            OnLiveQuoteReceived(security, LiveQuoteType.Ask, dt, askPrice.ToDecimal(), askSize);
        }
        public void tickByTickAllLast(int reqId, int tickType, long time, double price, int size, TickAttribLast tickAttriblast, string exchange, string specialConditions)
        {
            var security = GetRequestSecurity(reqId);

            if (security == null)
                return;

            DateTime dt = time.FromIbkrTimeFormat();

            OnLiveQuoteReceived(security, LiveQuoteType.Trade, dt, price.ToDecimal(), size);
        }
        public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
        {
            foreach (var tick in ticks)
            {
                tickByTickBidAsk(reqId, tick.Time, tick.PriceBid, tick.PriceAsk, (int)tick.SizeBid, (int)tick.SizeAsk, tick.TickAttribBidAsk);
            }
        }
        public void historicalData(int reqId, Bar bar)
        {
            var security = GetRequestSecurity(reqId);

            if (security == null)
                return;

            if (reqId % 5 == 0)
            {
                // Last Close Bar
                var Close = bar.Close;
                var Time = bar.Time.FromIbkrFormat();
                OnLiveQuoteReceived(security, LiveQuoteType.Open, Time, Close.ToDecimal(), 0);
            }
            else if (reqId % 5 == 1)
            {
                // Intraday minute bar populate
                DateTime d = long.Parse(bar.Time).FromIbkrTimeFormat();

                if (d.Day != DateTime.Today.Day)
                    return;

                TimeSpan dt = long.Parse(bar.Time).FromIbkrTimeFormat().TimeOfDay;
                security.AddIntradayTick(dt, bar.Close.ToDecimal(), true);
            }

        }
        public void historicalDataEnd(int reqId, string start, string end)
        {
            if (reqId % 5 == 1)
            {
                // End of intraday minute populate

                var security = GetRequestSecurity(reqId);

                if (security == null)
                    return;

                security.IntradayTickInitialPopulateComplete();
            }
        }
        public void historicalDataUpdate(int reqId, Bar bar)
        {
            if (reqId % 5 == 2)
            {
                var security = GetRequestSecurity(reqId);
                // Intraday minute bar update

                if (security == null)
                    return;

                var dt = long.Parse(bar.Time).FromIbkrTimeFormat();
                security.AddIntradayTick(dt.TimeOfDay, bar.Close.ToDecimal(), false);
            }
        }

        #endregion

        #region Not Used

        public void currentTime(long time)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void tickSize(int tickerId, int field, int size)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void tickString(int tickerId, int field, string value)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void tickGeneric(int tickerId, int field, double value)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void tickSnapshotEnd(int tickerId)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void managedAccounts(string accountsList)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void accountSummaryEnd(int reqId)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void updateAccountTime(string timestamp)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void accountDownloadEnd(string account)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void openOrderEnd()
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void contractDetails(int reqId, ContractDetails contractDetails)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void contractDetailsEnd(int reqId)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void execDetailsEnd(int reqId)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void commissionReport(CommissionReport commissionReport)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void fundamentalData(int reqId, string data)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void marketDataType(int reqId, int marketDataType)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size, bool isSmartDepth)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void position(string account, Contract contract, double pos, double avgCost)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void positionEnd()
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void realtimeBar(int reqId, long date, double open, double high, double low, double close, long volume, double WAP, int count)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void scannerParameters(string xml)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void scannerDataEnd(int reqId)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void receiveFA(int faDataType, string faXmlData)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void verifyMessageAPI(string apiData)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void displayGroupList(int reqId, string groups)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void positionMultiEnd(int requestId)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void accountUpdateMultiEnd(int requestId)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void securityDefinitionOptionParameterEnd(int reqId)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void softDollarTiers(int reqId, SoftDollarTier[] tiers)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void familyCodes(FamilyCode[] familyCodes)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void newsProviders(NewsProvider[] newsProviders)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void newsArticle(int requestId, int articleType, string articleText)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void historicalNewsEnd(int requestId, bool hasMore)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void headTimestamp(int reqId, string headTimestamp)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void histogramData(int reqId, HistogramEntry[] data)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void rerouteMktDataReq(int reqId, int conId, string exchange)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void rerouteMktDepthReq(int reqId, int conId, string exchange)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void tickByTickMidPoint(int reqId, long time, double midPoint)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void orderBound(long orderId, int apiClientId, int apiOrderId)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void completedOrder(Contract contract, Order order, OrderState orderState)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void completedOrdersEnd()
        {
            Console.WriteLine(GetCurrentMethod());
        }

        public void nextValidId(int orderId)
        {
            Console.WriteLine(GetCurrentMethod());
        }

        #endregion
    }
}
