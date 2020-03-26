using IBApi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance.Data
{
    public sealed class IbkrDataProviderRequest : RefDataProviderRequest
    {
        // Ordered list of request types to IBKR Gateway
        public List<IbkrDataProviderRequestType> Requests { get; private set; } = new List<IbkrDataProviderRequestType>();

        public Contract Contract
        {
            get
            {
                return new Contract()
                {
                    Currency = "USD",
                    Symbol = Security.Ticker,
                    SecType = "STK",
                    Exchange = "SMART",
                    PrimaryExch = Security.Exchange
                };
            }
        }

        public bool PartitionedRequest { get; set; } = false;

        public (string start, string end) IbkrPriceDataRequestRange => (PriceDataRequestRange.start.ToIbkrFormat(), PriceDataRequestRange.end.ToIbkrFormat());
        public string IbkrRequestDuration => ToIbkrDuration(PriceDataRequestRange.start, PriceDataRequestRange.end);

        public static IbkrDataProviderRequest GetRequest(RefDataProviderRequest request)
        {
            IbkrDataProviderRequest ret = new IbkrDataProviderRequest();
            request.CopyTo(ret);
            return ret;
        }
        public bool RequiresPartitioning => PriceDataRequestSpan.TotalDays > 365;

        public void AddRequestType(IbkrDataProviderRequestType requestType)
        {
            if (!Requests.Contains(requestType))
                Requests.Add(requestType);
        }
        public bool NextRequestType(out IbkrDataProviderRequestType requestType)
        {
            if (Requests.Count == 0)
            {
                requestType = IbkrDataProviderRequestType.NotSet;
                return false;
            }

            requestType = Requests[0];
            Requests.RemoveAt(0);
            return true;
        }

        public new IbkrDataProviderRequest Copy()
        {
            IbkrDataProviderRequest ret = base.Copy() as IbkrDataProviderRequest;
            ret.Requests = new List<IbkrDataProviderRequestType>(Requests);
            return ret;
        }
    }

    public partial class IbkrDataProvider : RefDataProvider
    {
        private static EClientSocket clientSocket;
        public readonly EReaderSignal signal;
        public int Port { get; }
        public int ClientId { get; }

        public override string Name => "IBKR Reference Data";

        ConcurrentBag<IbkrDataProviderRequest> PendingRequests = new ConcurrentBag<IbkrDataProviderRequest>();
        static double RequestInterval = 1000;
        System.Timers.Timer RequestTimer = new System.Timers.Timer(RequestInterval);

        public IbkrDataProvider(int port, int clientId)
        {
            Port = port;
            this.ClientId = clientId;
            signal = new EReaderMonitorSignal();
            clientSocket = new EClientSocket(this, signal);

            RequestTimer.Elapsed += RequestTimerTick;
        }

        private bool DataConnectionReady { get; set; } = false;

        public override void Connect()
        {
            if (!Connected)
            {
                Log(new LogMessage(ToString(), $"Connecting to IBKR Gateway on port {Port}", LogMessageType.Production));
                clientSocket?.eConnect("localhost", Port, ClientId);
            }
        }
        public override void Disconnect()
        {
            if (Connected)
            {
                Log(new LogMessage(ToString(), "Disconnecting from IBKR Gateway", LogMessageType.Production));
                clientSocket?.eDisconnect();
            }
        }

        public override void SubmitRequest(RefDataProviderRequest request)
        {
            var newRequest = FormatNewRequest(request);

            newRequest.SetNextRequestId();
            PushEnd(PendingRequests, newRequest);
            RequestTimer.Start();
        }
        public override void CancelAllRequests(string cancelMessage)
        {
            while (PendingRequests.Count > 0)
            {
                var request = Pop(ref PendingRequests);
                request.MarkCancelled(cancelMessage);
                OnDataProviderResponse(request);
            }
        }
        public override void GetProviderSupportedSymbols()
        {
            Log(new LogMessage(ToString(), $"{GetCurrentMethod()} not supported by IBKR Provider", LogMessageType.SystemError));
            Console.WriteLine($"{GetCurrentMethod()} not supported by IBKR");
        }
        public override void GetProviderSectors()
        {
            Log(new LogMessage(ToString(), $"{GetCurrentMethod()} not supported by IBKR Provider", LogMessageType.SystemError));
            Console.WriteLine($"{GetCurrentMethod()} not supported by IBKR");
        }
        public override void SubmitBatchRequest(List<RefDataProviderRequest> requests)
        {
            Log(new LogMessage(ToString(), $"{GetCurrentMethod()} not supported by IBKR Provider", LogMessageType.SystemError));
            Console.WriteLine($"{GetCurrentMethod()} not supported by IBKR");
        }

        private IbkrDataProviderRequest FormatNewRequest(RefDataProviderRequest request)
        {
            // Convert generic request into IBKR formatted request
            var ret = IbkrDataProviderRequest.GetRequest(request);

            switch (request.RequestType)
            {
                case DataProviderRequestType.SecurityContractData:
                    ret.AddRequestType(IbkrDataProviderRequestType.SecurityExchangeName);
                    break;
                case DataProviderRequestType.SecurityPriceData:
                case DataProviderRequestType.SecurityVolumeData:
                    if (ret.Security.Exchange == "UNK")
                        ret.AddRequestType(IbkrDataProviderRequestType.SecurityExchangeName);
                    if (ret.PriceDataRequestRange.start == DateTime.MinValue)
                        ret.AddRequestType(IbkrDataProviderRequestType.SecurityFirstAvailableDate);
                    ret.AddRequestType(IbkrDataProviderRequestType.SecurityHistoricalData);
                    break;
            }

            return ret;
        }
        private (IbkrDataProviderRequest yearsRequest, IbkrDataProviderRequest daysRequest) PartitionLargeRequest(IbkrDataProviderRequest request)
        {
            /*
             *  IBKR requests greater than 1 year must be split into 2 requests - one for the years and one for the remainder days             
             */

            Log(new LogMessage(ToString(), $"Partitioning request [{request.RequestID}] for {request.Security.Ticker}", LogMessageType.Debug));

            request.Requests.Insert(0, IbkrDataProviderRequestType.SecurityHistoricalData);

            IbkrDataProviderRequest yearsRequest = request.Copy();
            yearsRequest.PartitionedRequest = true;
            IbkrDataProviderRequest daysRequest = request.Copy();
            daysRequest.PartitionedRequest = true;

            var partitions = request.PriceDataRequestSpan.ToIbkrPartition();

            // Modify the Years request date range to request from the original End to End-Years
            yearsRequest.PriceDataRequestRange = (request.PriceDataRequestRange.end.AddYears(-partitions.wholeYears), request.PriceDataRequestRange.end);
            // Modify the Days request date range to request fron the original Start to Start+Days (plus buffer)
            daysRequest.PriceDataRequestRange = (request.PriceDataRequestRange.start, request.PriceDataRequestRange.start.AddDays(partitions.daysRemainder + 14));

            yearsRequest.MarkPending();
            daysRequest.MarkPending();

            return (yearsRequest, daysRequest);
        }

        private void RequestTimerTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!DataConnectionReady)
            {
                RequestTimer.Stop();
                Log(new LogMessage(ToString(), "Halted data provider request; data connection not ready", LogMessageType.SystemError));

                Status = ControlStatus.Offline;
                return;
            }

            if (PendingRequests.Where(x => x.RequestStatus == DataProviderRequestStatus.Pending).Count() > 0)
            {
                Status = ControlStatus.Working;
                ProcessRequestQueue();
            }
            else
                RequestTimer.Stop();
        }
        void ProcessRequestQueue()
        {

            // IbkrDataProviderRequest request = PendingRequests.PeekFirstPending();

            if (!TryPopFirstPending<IbkrDataProviderRequest>(ref PendingRequests, out var request))
            {
                RequestTimer.Stop();
                return;
            }
            request.MarkWorking();

            //
            // Confirm valid request
            //
            if (!request.NextRequestType(out var requestType) || requestType == IbkrDataProviderRequestType.NotSet)
            {
                Log(new LogMessage($"{this}.ProcessRequestQueue", "Invalid request in queue", LogMessageType.Debug));
                request.MarkError(DataProviderErrorType.InvalidRequest, "Invalid Request Type");
                OnDataProviderResponse(request);
                return;
            }

            //
            // Submit request to IBKR
            //
            switch (requestType)
            {
                case IbkrDataProviderRequestType.SecurityExchangeName:
                    {
                        Log(new LogMessage(ToString(),
                            $"Submitting request for: [{request.RequestID}] {request.Security.Ticker} primary exchange",
                            LogMessageType.Debug));

                        //
                        // Submit Request
                        //
                        request.MarkSubmitted(DateTime.Now);
                        PushEnd(PendingRequests, request);

                        clientSocket.reqMatchingSymbols(
                            request.RequestID,
                            request.Security.Ticker
                            );

                        break;
                    }
                case IbkrDataProviderRequestType.SecurityHistoricalData:
                    {
                        /*
                         *  Prior to submitting the request, partition requests that are 365 days in length
                         */
                        if (!request.PartitionedRequest && request.RequiresPartitioning)
                        {
                            // Pop request, generate 2 sub requests, push subrequests to front of queue
                            //request = PopAt(ref PendingRequests, request.RequestID);
                            var subRequests = PartitionLargeRequest(request);

                            request.MarkCancelled("Handled by subrequests");

                            subRequests.yearsRequest.SetNextRequestId();
                            subRequests.daysRequest.SetNextRequestId();

                            Push(ref PendingRequests, subRequests.yearsRequest);
                            Push(ref PendingRequests, subRequests.daysRequest);
                            break;
                        }


                        Log(new LogMessage(ToString(),
                            $"Submitting request for: [{request.RequestID}] {request.Security.Ticker} " +
                            $"price data from {request.IbkrPriceDataRequestRange.start} to {request.IbkrPriceDataRequestRange.end} " +
                            $"on {request.Contract.PrimaryExch} exchange",
                            LogMessageType.Debug));

                        //
                        // Submit Request
                        //
                        request.MarkSubmitted(DateTime.Now);
                        PushEnd(PendingRequests, request);

                        clientSocket.reqHistoricalData(
                            request.RequestID,
                            request.Contract,
                            request.IbkrPriceDataRequestRange.end,
                            request.IbkrRequestDuration,
                            "1 day",
                            "TRADES",
                            1, 1, false, null
                            );

                        break;
                    }
                case IbkrDataProviderRequestType.SecurityFirstAvailableDate:
                    {
                        Log(new LogMessage(ToString(),
                            $"Submitting request for: [{request.RequestID}] {request.Security.Ticker} " +
                            $"head timestamp on {request.Contract.PrimaryExch} exchange",
                            LogMessageType.Debug));

                        //
                        // Submit Request
                        //

                        PushEnd(PendingRequests, request);
                        request.MarkSubmitted(DateTime.Now);

                        clientSocket.reqHeadTimestamp(
                            request.RequestID,
                            request.Contract,
                            "TRADES",
                            1, 1
                            );

                        break;
                    }
            }

        }
        void HandleRequestReply(int requestId)
        {
            var request = PendingRequests.PeekAt(requestId);

            if (request.RequestStatus == DataProviderRequestStatus.ErrorResponse)
            {
                request = PopAt(ref PendingRequests, request.RequestID);
                OnDataProviderResponse(request);
                return;
            }

            if (!request.NextRequestType(out var nextRequestType))
            {
                request = PopAt(ref PendingRequests, request.RequestID);
                OnDataProviderResponse(request);
                return;
            }

            switch (nextRequestType)
            {
                case IbkrDataProviderRequestType.SecurityExchangeName:
                case IbkrDataProviderRequestType.SecurityFirstAvailableDate:
                case IbkrDataProviderRequestType.SecurityHistoricalData:
                    var popRequest = PopAt(ref PendingRequests, requestId);
                    popRequest.SetNextRequestId();
                    popRequest.MarkPending();
                    popRequest.Requests.Insert(0, nextRequestType);
                    Push(ref PendingRequests, popRequest);
                    RequestTimer.Start();
                    break;
            }
        }
    }

    /// <summary>
    /// Implemented EWrapper methods
    /// </summary>
    public partial class IbkrDataProvider : EWrapper
    {
        #region Connection Management

        /// <summary>
        /// Indicates that the data service is connected to the IBKR gateway
        /// </summary>
        public void connectAck()
        {
            try
            {
                if (Connected == clientSocket.IsConnected())
                    return;

                Connected = DataConnectionReady = clientSocket.IsConnected();

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

                Status = ControlStatus.Ready;
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString(), ex.Message, LogMessageType.SystemError));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return;
            }
        }

        /// <summary>
        /// Raised when the connection to the IBKR gateway is closed
        /// </summary>
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
        #region Error Handling

        public void error(Exception e)
        {
            Log(new LogMessage("IBKR Msg 1", e.Message));
        }
        public void error(string str)
        {
            Log(new LogMessage("IBKR Msg 2", str));
        }
        public void error(int id, int errorCode, string errorMsg)
        {
            // Receive an error with a code and corresponding message and direct to appropriate handler

            Log(new LogMessage("IBKR Msg 3", $"ID: [{id}] Code: [{errorCode}] Msg: {errorMsg}"));

            switch (errorCode)
            {
                case 162:
                    {
                        if (errorMsg.Contains("Request Timed Out"))
                        {
                            var request = PendingRequests.PeekAt(id);
                            request.MarkError(DataProviderErrorType.ConnectionError, "Request Timed Out");
                            HandleRequestReply(id);
                        }
                        else if (errorMsg.Contains("Trading TWS session is connected from a different IP address"))
                        {
                            var request = PendingRequests.PeekAt(id);
                            request.MarkError(DataProviderErrorType.ConnectionError, "TWS Session Error");
                            HandleRequestReply(id);
                        }
                        else
                        {
                            var request = PendingRequests.PeekAt(id);
                            request.MarkError(DataProviderErrorType.InvalidSecurity, "No historical data available for this Security");
                            HandleRequestReply(id);
                        }
                        break;
                    }
                case 1100:
                    {
                        // Connection lost
                        Connected = false;
                        DataConnectionReady = false;
                    }
                    break;
                case 1101:
                case 1102:
                    {
                        // Connection restored (data lost, data not lost)
                        Connected = true;
                        DataConnectionReady = true;
                    }
                    break;
                case 2110:
                    {
                        // Connectivity broke, will be restored automatically
                        Connected = false;
                        DataConnectionReady = false;
                    }
                    break;
                case 504:
                    {
                        // Not connected
                        Connected = false;
                        DataConnectionReady = false;
                    }
                    break;
                case 2129:
                    {
                        // Information for product is informational only - remove from database
                        var request = PendingRequests.PeekAt(id);
                        request.MarkError(DataProviderErrorType.InvalidSecurity, "Informational data only");
                        HandleRequestReply(id);
                    }
                    break;
                case 2105:
                    {
                        // Historical data connection offline
                        DataConnectionReady = false;
                        Connected = false;
                        Log(new LogMessage(ToString(), "*** Lost Data Provider Connection ***", LogMessageType.Production));
                    }
                    break;
                case 2106:
                case 2107:
                    {
                        // Historical data connection online
                        DataConnectionReady = true;
                        Status = ControlStatus.Working;
                        RequestTimer.Start();
                        Log(new LogMessage(ToString(), "*** Restored Data Provider Connection ***", LogMessageType.Production));
                    }
                    break;
                case 2158:
                    {
                        // Sec-def data farm connection is OK
                        Log(new LogMessage(ToString(), $"{errorMsg}", LogMessageType.Production));
                    }
                    break;
                default:
                    {
                        Log(new LogMessage(ToString(), $"Unhandled TWS Data System Message ID {errorCode}: {errorMsg}", LogMessageType.SystemError));

                        if (TryPopAt(ref PendingRequests, id, out var request))
                        {
                            request.MarkError(DataProviderErrorType.ConnectionError, "Unhandled TWS Error");
                            HandleRequestReply(id);
                        }
                    }
                    break;
            }
        }

        public void fundamentalData(int reqId, string data)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region Historical Data Request Handling

        /// <summary>
        /// Returns the first databar timestamp for a selected security
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="headTimestamp"></param>
        public void headTimestamp(int reqId, string headTimestamp)
        {
            try
            {
                var request = PendingRequests.PeekAt(reqId);

                Log(new LogMessage(ToString(), $"Received head timestamp data for [{request.RequestID}] {request.Security.Ticker} of {headTimestamp}", LogMessageType.Debug));

                // Update start date and adjust based on global limit
                DateTime adjustedStartDate = DateTimeMax(headTimestamp.FromIbkrFormat(), Settings.Instance.DataRequestStartLimit);

                request.PriceDataRequestRange = (adjustedStartDate, request.PriceDataRequestRange.end);
                request.MarkComplete();

                HandleRequestReply(reqId);
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString(), ex.Message, LogMessageType.SystemError));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");

                PendingRequests.PeekAt(reqId).MarkError(DataProviderErrorType.SystemError, "Request error in headTimestamp");
                HandleRequestReply(reqId);
            }
        }

        /// <summary>
        /// Returns a bar of data for a specific security request.  This is called for each received bar.
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="bar"></param>
        public void historicalData(int reqId, Bar bar)
        {
            try
            {
                var request = PendingRequests.PeekAt(reqId);

                // Make sure this is a valid trading day, since sometimes we get values for market closure days
                if (!Calendar.IsTradingDay(bar.Time.FromIbkrFormat()))
                    return;

                // Assign the price data to a PriceBar retrieved from the Security object
                var newBar = request.Security.GetPriceBar(bar.Time.FromIbkrFormat(), PriceBarSize.Daily, true);

                newBar.SetPriceValues(bar.Open.ToDecimal(), bar.High.ToDecimal(), bar.Low.ToDecimal(), bar.Close.ToDecimal(), bar.Volume);

                newBar.ToUpdate = true;
                newBar.PriceDataProvider = DataProviderType.InteractiveBrokers;

                request.MarkPartialResponse();
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString() + "historicalData bar", ex.Message, LogMessageType.SystemError));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");

                PendingRequests.PeekAt(reqId).MarkError(DataProviderErrorType.SystemError, "Request error in historicalData");
                HandleRequestReply(reqId);
            }
        }

        /// <summary>
        /// Indicates that all data has been returned for a specific request, sends to event.
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void historicalDataEnd(int reqId, string start, string end)
        {
            try
            {
                var request = PendingRequests.PeekAt(reqId);
                request.MarkComplete();
                HandleRequestReply(reqId);
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString() + "historicalData end", ex.Message, LogMessageType.SystemError));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");

                PendingRequests.PeekAt(reqId).MarkError(DataProviderErrorType.SystemError, "Request error in historicalDataEnd");
                HandleRequestReply(reqId);
            }
        }

        #endregion
        #region Misc

        public void nextValidId(int orderId)
        {
            // Not used yet
            Console.WriteLine("###" + System.Reflection.MethodInfo.GetCurrentMethod().Name);
        }
        public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
        {
            try
            {
                var request = PendingRequests.PeekAt(reqId);

                //
                // Filter the returned contracts to get the single US Stock matching the ticker exactly
                //
                ContractDescription contract = (from des in contractDescriptions
                                                where des.Contract.Symbol == request.Security.Ticker &&
                                                des.Contract.SecType == "STK" &&
                                                des.Contract.Currency == "USD"
                                                select des).FirstOrDefault();

                if (contract == null)
                {
                    request.MarkError(DataProviderErrorType.InvalidSecurity, "No contract description returned");
                    HandleRequestReply(reqId);
                    return;
                }

                Log(new LogMessage(ToString() + "symbols", $"Received primary exchange for [{request.RequestID}] {request.Security.Ticker} of {contract.Contract.PrimaryExch}"));

                //
                // Set the exchange name
                //
                if (contract.Contract.PrimaryExch.Contains("NASDAQ"))
                {
                    // This is a specific point in the IBKR API
                    request.Security.Exchange = request.Contract.PrimaryExch = "ISLAND";
                    request.MarkComplete();
                    HandleRequestReply(reqId);
                }
                else if (contract.Contract.PrimaryExch.Contains("PINK"))
                {
                    // Remove symbol, it is a pink slip
                    request.MarkError(DataProviderErrorType.InvalidSecurity, "Security is a pink sheet");
                    HandleRequestReply(reqId);
                }
                else
                {
                    request.Security.Exchange = contract.Contract.PrimaryExch;
                    request.MarkComplete();
                    HandleRequestReply(reqId);
                }

            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString() + "exchange name", ex.Message, LogMessageType.SystemError));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");

                PendingRequests.PeekAt(reqId).MarkError(DataProviderErrorType.SystemError, "Request error in symbolSamples");
                HandleRequestReply(reqId);
            }
        }

        #endregion
    }

    /// <summary>
    /// Non-Implemented EWrapper methods
    /// </summary>
    public partial class IbkrDataProvider : EWrapper
    {
        public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
        {
            throw new NotImplementedException();
        }
        public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
        {
            throw new NotImplementedException();
        }
        public void tickSize(int tickerId, int field, int size)
        {
            throw new NotImplementedException();
        }
        public void tickString(int tickerId, int field, string value)
        {
            throw new NotImplementedException();
        }
        public void marketDataType(int reqId, int marketDataType)
        {
            throw new NotImplementedException();
        }
        public void accountDownloadEnd(string account)
        {
            throw new NotImplementedException();
        }
        public void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            throw new NotImplementedException();
        }
        public void accountSummaryEnd(int reqId)
        {
            throw new NotImplementedException();
        }
        public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency)
        {
            throw new NotImplementedException();
        }
        public void accountUpdateMultiEnd(int requestId)
        {
            throw new NotImplementedException();
        }
        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            throw new NotImplementedException();
        }
        public void commissionReport(CommissionReport commissionReport)
        {
            throw new NotImplementedException();
        }
        public void completedOrder(Contract contract, Order order, OrderState orderState)
        {
            throw new NotImplementedException();
        }
        public void completedOrdersEnd()
        {
            throw new NotImplementedException();
        }
        public void contractDetails(int reqId, ContractDetails contractDetails)
        {
            throw new NotImplementedException();
        }
        public void contractDetailsEnd(int reqId)
        {
            throw new NotImplementedException();
        }
        public void currentTime(long time)
        {
            throw new NotImplementedException();
        }
        public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract)
        {
            throw new NotImplementedException();
        }
        public void displayGroupList(int reqId, string groups)
        {
            throw new NotImplementedException();
        }
        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            throw new NotImplementedException();
        }
        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            throw new NotImplementedException();
        }
        public void execDetailsEnd(int reqId)
        {
            throw new NotImplementedException();
        }
        public void familyCodes(FamilyCode[] familyCodes)
        {
            throw new NotImplementedException();
        }
        public void histogramData(int reqId, HistogramEntry[] data)
        {
            throw new NotImplementedException();
        }
        public void historicalDataUpdate(int reqId, Bar bar)
        {
            throw new NotImplementedException();
        }
        public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
        {
            throw new NotImplementedException();
        }
        public void historicalNewsEnd(int requestId, bool hasMore)
        {
            throw new NotImplementedException();
        }
        public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
        {
            throw new NotImplementedException();
        }
        public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
        {
            throw new NotImplementedException();
        }
        public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
        {
            throw new NotImplementedException();
        }
        public void managedAccounts(string accountsList)
        {
            Console.WriteLine("###" + System.Reflection.MethodInfo.GetCurrentMethod().Name);
            //throw new NotImplementedException();
        }
        public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
        {
            throw new NotImplementedException();
        }
        public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
        {
            throw new NotImplementedException();
        }
        public void newsArticle(int requestId, int articleType, string articleText)
        {
            throw new NotImplementedException();
        }
        public void newsProviders(NewsProvider[] newsProviders)
        {
            throw new NotImplementedException();
        }
        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            throw new NotImplementedException();
        }
        public void openOrderEnd()
        {
            throw new NotImplementedException();
        }
        public void orderBound(long orderId, int apiClientId, int apiOrderId)
        {
            throw new NotImplementedException();
        }
        public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            throw new NotImplementedException();
        }
        public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
        {
            throw new NotImplementedException();
        }
        public void pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
        {
            throw new NotImplementedException();
        }
        public void position(string account, Contract contract, double pos, double avgCost)
        {
            throw new NotImplementedException();
        }
        public void positionEnd()
        {
            throw new NotImplementedException();
        }
        public void positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost)
        {
            throw new NotImplementedException();
        }
        public void positionMultiEnd(int requestId)
        {
            throw new NotImplementedException();
        }
        public void realtimeBar(int reqId, long date, double open, double high, double low, double close, long volume, double WAP, int count)
        {
            throw new NotImplementedException();
        }
        public void receiveFA(int faDataType, string faXmlData)
        {
            throw new NotImplementedException();
        }
        public void rerouteMktDataReq(int reqId, int conId, string exchange)
        {
            throw new NotImplementedException();
        }
        public void rerouteMktDepthReq(int reqId, int conId, string exchange)
        {
            throw new NotImplementedException();
        }
        public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            throw new NotImplementedException();
        }
        public void scannerDataEnd(int reqId)
        {
            throw new NotImplementedException();
        }
        public void scannerParameters(string xml)
        {
            throw new NotImplementedException();
        }
        public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            throw new NotImplementedException();
        }
        public void securityDefinitionOptionParameterEnd(int reqId)
        {
            throw new NotImplementedException();
        }
        public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
        {
            throw new NotImplementedException();
        }
        public void softDollarTiers(int reqId, SoftDollarTier[] tiers)
        {
            throw new NotImplementedException();
        }
        public void tickByTickAllLast(int reqId, int tickType, long time, double price, int size, TickAttribLast tickAttriblast, string exchange, string specialConditions)
        {
            throw new NotImplementedException();
        }
        public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize, TickAttribBidAsk tickAttribBidAsk)
        {
            throw new NotImplementedException();
        }
        public void tickByTickMidPoint(int reqId, long time, double midPoint)
        {
            throw new NotImplementedException();
        }
        public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
        {
            throw new NotImplementedException();
        }
        public void tickGeneric(int tickerId, int field, double value)
        {
            throw new NotImplementedException();
        }
        public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
        {
            throw new NotImplementedException();
        }
        public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            throw new NotImplementedException();
        }
        public void tickSnapshotEnd(int tickerId)
        {
            throw new NotImplementedException();
        }
        public void updateAccountTime(string timestamp)
        {
            throw new NotImplementedException();
        }
        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            throw new NotImplementedException();
        }
        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            throw new NotImplementedException();
        }
        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size, bool isSmartDepth)
        {
            throw new NotImplementedException();
        }
        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            Log(new LogMessage(GetCurrentMethod(), $"{message} [{origExchange}]", LogMessageType.Production));
        }
        public void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName)
        {
            throw new NotImplementedException();
        }
        public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            throw new NotImplementedException();
        }
        public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            throw new NotImplementedException();
        }
        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            throw new NotImplementedException();
        }
        public void verifyMessageAPI(string apiData)
        {
            throw new NotImplementedException();
        }
    }
}
