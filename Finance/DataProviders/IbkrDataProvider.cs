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
    public partial class IbkrDataProvider : DataProvider
    {

        #region IBKR client objects

        private static EClientSocket clientSocket;
        public readonly EReaderSignal signal;
        public int Port { get; }

        #endregion

        public IbkrDataProvider(int port)
        {
            Name = "Interactive Brokers";
            Port = port;
            signal = new EReaderMonitorSignal();
            clientSocket = new EClientSocket(this, signal);

            statusIndicatorControlManager = new StatusLabelControlManager(Name);

            RequestTimer.Elapsed += RequestQueueAction;
            Log(new LogMessage(ToString(), "Created Instance", LogMessageType.Debug));
        }

        public override void Connect()
        {
            if (!Connected)
            {
                Log(new LogMessage(ToString(), $"Connecting to IBKR Gateway on port {Port}", LogMessageType.Production));
                clientSocket?.eConnect("localhost", Port, 0);
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

        protected override void SetStatusIndicator(ProcessStatus processStatus)
        {
            switch (processStatus)
            {
                case ProcessStatus.ErrorState:
                    statusIndicatorControlManager.SetStatus("Error State", System.Drawing.Color.PaleVioletRed);
                    break;
                case ProcessStatus.Ready:
                    StatusTimer?.Stop();
                    if (Connected)
                        statusIndicatorControlManager.SetStatus("Ready", System.Drawing.Color.LightGreen);
                    else
                        SetStatusIndicator(ProcessStatus.Offline);
                    break;
                case ProcessStatus.Working:
                    {
                        int pendingRequests = PendingRequestQueue.Where(x => x.Value.Complete == false).Count();
                        if (pendingRequests == 0)
                            SetStatusIndicator(ProcessStatus.Ready);
                        else
                        {
                            statusIndicatorControlManager.SetStatus($"Working ... {pendingRequests} Requests Pending", System.Drawing.Color.Orange);
                            if (StatusTimer == null)
                            {
                                StatusTimer = new System.Timers.Timer(500);
                                StatusTimer.Elapsed += (s, e) => { SetStatusIndicator(processStatus); };
                            }
                            StatusTimer.Start();
                        }
                    }
                    break;
                case ProcessStatus.Offline:
                    statusIndicatorControlManager.SetStatus("Offline", System.Drawing.Color.PaleVioletRed);
                    break;
                default:
                    break;
            }
        }
        private System.Timers.Timer StatusTimer = null;

        private TimeSpan _ServerTimeOffset { get; set; } = new TimeSpan(0);
        public override TimeSpan ServerTimeOffset
        {
            get
            {
                return _ServerTimeOffset;
            }
        }

        public override void RequestPriceData(Security security, DateTime startDate, DateTime endDate)
        {
            var str = new StringBuilder();
            var request = new Request(NextDataRequestId, security)
            {
                StartDate = startDate,
                EndDate = endDate
            };

            str.Append($"Price Data Request for {security.Ticker}: [ReqID {request.ReqId}]:{startDate.ToString("yyyyMMdd")} to {endDate.ToString("yyyyMMdd")}: Exchange {security.Exchange}");

            if (security.Exchange == "UNK")
            {
                // Requires exchange info first
                request.Requests.Add(DataRequestType.ExchangeName);
                str.Append($": (Request exchange)");
            }

            if (startDate == DateTime.MinValue)
            {
                // Request first available bar date
                request.Requests.Add(DataRequestType.FirstAvailableDate);
                str.Append($": (Request Head Timestamp)");
            }
            // Append historical data request
            request.Requests.Add(DataRequestType.HistoricalData);

            // TODO: Maybe run into problems here
            // Partition a large request if required
            if (request.Requests.FirstOrDefault() == DataRequestType.HistoricalData &&
                (request.EndDate - request.StartDate).TotalDays >= 365)
            {
                PartitionLargeRequest(request);
                str.Append($" (Partition Large Request)");
            }

            //Log(new LogMessage(ToString(), str.ToString(), LogMessageType.Debug));

            request.Submit = true;
            PendingRequestQueue.TryAdd(request.ReqId, request);
            RequestTimer.Start();

        }
        public override void RequestPriceData(Security security, DateTime endDate)
        {
            try
            {
                RequestPriceData(security, DateTime.MinValue, endDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return;
            }
        }
        public override void RequestContractData(Security security)
        {
            var request = new Request(NextDataRequestId, security) { };

            request.Requests.Add(DataRequestType.ExchangeName);
            request.Submit = true;

            PendingRequestQueue.TryAdd(request.ReqId, request);
            RequestTimer.Start();
        }
        public override void CancelRequest(Security security)
        {
            var entry = PendingRequestQueue.Where(sec => sec.Value.Security == security).FirstOrDefault();

            if (entry.Value == null)
                return;

            Request request = entry.Value;
            request.Cancel();
        }
        public override void CancelAllRequests()
        {
            foreach (var entry in PendingRequestQueue)
            {
                entry.Value.Cancel();
            }
        }

        #region Request Management

        private bool DataConnectionReady { get; set; } = false;

        // Request Class used to organize IBKR requests
        private class Request
        {
            public int ReqId { get; set; }

            public bool Submit { get; set; } = false;
            public bool Complete { get; set; } = false;

            public Request nextRequest { get; set; } = null;

            public Contract Contract { get; set; }
            public Security Security { get; set; }
            public DateTime StartDate { get; set; } = DateTime.MinValue;
            public DateTime EndDate { get; set; } = DateTime.MinValue;

            // Use a list so we can chain request types and send to next method after receipt
            public List<DataRequestType> Requests { get; set; } = new List<DataRequestType>();

            public Request(int reqId, Security security)
            {
                ReqId = reqId;
                Security = security;

                Contract = new Contract()
                {
                    Currency = "USD",
                    Symbol = security.Ticker,
                    SecType = "STK",
                    Exchange = "SMART",
                    PrimaryExch = security.Exchange
                };

            }
            public void Cancel()
            {
                Submit = false;
                Complete = true;
                nextRequest = null;
                Requests = new List<DataRequestType>();
            }
        }

        int priorityRequestNext = 0;
        static double RequestInterval = 1000;
        System.Timers.Timer RequestTimer = new System.Timers.Timer(RequestInterval);
        ConcurrentDictionary<int, Request> PendingRequestQueue = new ConcurrentDictionary<int, Request>();

        void RequestQueueAction(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!DataConnectionReady)
            {
                // TODO: Expand this to auto time-out and cancel requests if the connection stays down
                RequestTimer.Stop();
                Log(new LogMessage(ToString(), "Halted seurity data reqeust due to loss of connection", LogMessageType.Error));
                SetStatusIndicator(ProcessStatus.Offline);
            }

            try
            {
                // Check to see if there are pending requests
                if (PendingRequestQueue.Where(x => x.Value.Submit == true).Count() > 0)
                {
                    SetStatusIndicator(ProcessStatus.Working);

                    Request request;

                    // Take the first waiting request, unless a subsequent request was tagged
                    if (priorityRequestNext == 0)
                        request = PendingRequestQueue.Where(x => x.Value.Submit == true).First().Value;
                    else
                        request = PendingRequestQueue[priorityRequestNext];

                    // Submit next request
                    switch (request.Requests.First())
                    {
                        case DataRequestType.NotSet:
                            {
                                Log(new LogMessage(ToString(), $"Invalid Request Action: [{request.ReqId}] {request.Security.Ticker} request type not set", LogMessageType.Debug));
                                throw new InvalidDataRequestException();
                            }
                        case DataRequestType.ExchangeName:
                            {
                                request.Submit = false;
                                request.Requests.RemoveAt(0);
                                Log(new LogMessage(ToString(), $"Submitting request for: [{request.ReqId}] {request.Security.Ticker} primary exchange", LogMessageType.Debug));

                                //
                                // Submit Request
                                //
                                clientSocket.reqMatchingSymbols(request.ReqId, request.Security.Ticker);
                                break;
                            }
                        case DataRequestType.HistoricalData:
                            {
                                request.Submit = false;
                                request.Requests.RemoveAt(0);

                                var adjustedEndDate = request.EndDate;
                                if (AfterHours(DateTime.Now))
                                    adjustedEndDate = adjustedEndDate.AddHours(23);

                                Log(new LogMessage(ToString(), $"Submitting request for: [{request.ReqId}] {request.Security.Ticker} " +
                                    $"price data from {request.StartDate.ToString("yyyyMMdd")} to {request.EndDate.ToString("yyyyMMdd")} " +
                                    $"on {request.Contract.PrimaryExch} exchange", LogMessageType.Debug));

                                //
                                // Submit Request
                                //
                                clientSocket.reqHistoricalData(request.ReqId, request.Contract,
                                    adjustedEndDate.ToIbkrFormat(),  // Add 23 hours to default time of 12 AM so the request retrieves the bar dated on the request date, only if the market has closed already
                                    ToIbkrDuration(request.StartDate, request.EndDate),
                                    "1 day", "TRADES", 1, 1, false, null);

                                break;
                            }
                        case DataRequestType.FirstAvailableDate:
                            {
                                request.Submit = false;
                                request.Requests.RemoveAt(0);

                                Log(new LogMessage(ToString(), $"Submitting request for: [{request.ReqId}] {request.Security.Ticker} " +
                                                                    $"head timestamp on {request.Contract.PrimaryExch} exchange", LogMessageType.Debug));

                                //
                                // Submit Request
                                //
                                clientSocket.reqHeadTimestamp(request.ReqId, request.Contract, "TRADES", 1, 1);
                                break;
                            }
                        default:
                            throw new InvalidDataRequestException();
                    }
                    RequestTimer.Stop();
                }
                else
                {
                    RequestTimer.Stop();
                    SetStatusIndicator(ProcessStatus.Working);
                }
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString(), ex.Message, LogMessageType.Error));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                SetStatusIndicator(ProcessStatus.ErrorState);
                return;
            }
        }
        void PartitionLargeRequest(Request request)
        {
            Log(new LogMessage(ToString(), $"Partitioning request [{request.ReqId}] for {request.Security.Ticker}", LogMessageType.Debug));
            try
            {
                // This method splits a request into a years and days portion, with the days portion being < 365 total

                if (request.Requests.SingleOrDefault() != DataRequestType.HistoricalData)
                    throw new InvalidDataRequestException() { message = "Can only split valid Historical Data Requests" };

                // Compute whole years difference in dates
                TimeSpan span = (request.EndDate - request.StartDate);
                int years = ((int)span.TotalDays) / 365;

                // The breakpoints in the two requests -- add some cusion so no days are skipped. 
                DateTime yearsRequestStartDate = request.EndDate.AddYears(-years);
                DateTime daysRequestEndDate = request.EndDate.AddYears(-years).AddDays(14);

                // Request days portion to be 3executed after this request is complete
                request.nextRequest = new Request(NextDataRequestId, request.Security)
                {
                    StartDate = request.StartDate,
                    EndDate = daysRequestEndDate,
                    Requests = new List<DataRequestType>() { DataRequestType.HistoricalData },
                    Submit = false
                };

                // Set original request to request years portion
                request.StartDate = yearsRequestStartDate;
                request.Submit = true;                

                // Submit to queue
                PendingRequestQueue.TryAdd(request.ReqId, request);

                // If the request queue manager is not already in the process of submitting, start it.
                RequestTimer.Start();
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString(), ex.Message, LogMessageType.Error));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return;
            }
        }
        void HandleRequestReply(Request request, EventFlag flag)
        {
            switch (flag)
            {
                case EventFlag.NotSet:
                    break;
                case EventFlag.BadSymbol:
                    Log(new LogMessage(ToString() + " RequestReply", $"Bad/No data received for [{request.ReqId}] {request.Security.Ticker}", LogMessageType.Debug));
                    request.Complete = true;
                    SecurityDataResponse(request.Security, flag);
                    RequestTimer.Start();
                    return;
                case EventFlag.RequestError:
                    Log(new LogMessage(ToString(), $"Could not complete request for {request.Security.Ticker}", LogMessageType.Error));
                    request.Complete = true;
                    SecurityDataResponse(request.Security, flag);
                    RequestTimer.Start();
                    return;
                default:
                    break;
            }

            if (request.Requests.Count > 0)
            {
                PendingRequestQueue.TryRemove(request.ReqId, out Request nextRequest);
                nextRequest.ReqId = NextDataRequestId;
                nextRequest.Submit = true;
                PendingRequestQueue.TryAdd(nextRequest.ReqId, nextRequest);
                priorityRequestNext = nextRequest.ReqId;
                RequestTimer.Start();
            }
            else if (request.nextRequest != null)
            {
                // Forward child request
                request.Complete = true;
                request.nextRequest.Submit = true;
                PendingRequestQueue.TryAdd(request.nextRequest.ReqId, request.nextRequest);
                priorityRequestNext = request.nextRequest.ReqId;
                RequestTimer.Start();
            }
            else
            {
                // Send up
                request.Complete = true;
                SecurityDataResponse(request.Security, flag);
                priorityRequestNext = 0;
                RequestTimer.Start();
            }
        }

        #endregion
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
                Connected = clientSocket.IsConnected();

                if (!Connected)
                    return;

                var time = (clientSocket.ServerTime.Split(null)[0] + " " + clientSocket.ServerTime.Split(null)[1]).FromIbkrFormat();
                _ServerTimeOffset = time - DateTime.Now;

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
                            Log(new LogMessage("EReader", "Received message", LogMessageType.Debug));
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

                Console.WriteLine("Current Server Time: " + clientSocket.ServerTime);
                ConnectionStatusChanged();

                SetStatusIndicator(ProcessStatus.Ready);

            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString(), ex.Message, LogMessageType.Error));
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
                ConnectionStatusChanged();
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString(), ex.Message, LogMessageType.Error));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return;
            }
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Error 1
        /// </summary>
        /// <param name="e"></param>
        public void error(Exception e)
        {
            Log(new LogMessage("IBKR Msg 1", e.Message));

            Console.WriteLine($"IBKR DataProvider error message (Func 1): {e.Message}");
        }

        /// <summary>
        /// Error 2
        /// </summary>
        /// <param name="str"></param>
        public void error(string str)
        {
            Log(new LogMessage("IBKR Msg 2", str));
            Console.WriteLine($"IBKR DataProvider error message (Func 2): {str}");
        }

        /// <summary>
        /// Error3
        /// </summary>
        /// <param name="id"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMsg"></param>
        public void error(int id, int errorCode, string errorMsg)
        {
            // Receive an error with a code and corresponding message and direct to appropriate handler

            Log(new LogMessage("IBKR Msg 3", $"ID: [{id}] Code: [{errorCode}] Msg: {errorMsg}"));

            Console.WriteLine($"IBKR DataProvider error message (Func 3): {id} {errorCode} {errorMsg}");

            switch (errorCode)
            {
                case 162:
                    {
                        if (errorMsg.Contains("Request Timed Out"))
                        {
                            HandleRequestReply(PendingRequestQueue[id], EventFlag.RequestError);
                        }
                        else
                        {
                            // No historical data available from server
                            Request req = PendingRequestQueue[id];
                            HandleRequestReply(req, EventFlag.BadSymbol);
                        }
                        break;
                    }
                case 1100:
                    {
                        // Connection lost
                        Connected = false;
                        DataConnectionReady = false;
                        ConnectionStatusChanged();
                    }
                    break;
                case 1101:
                case 1102:
                    {
                        // Connection restored (data lost, data not lost)
                        Connected = true;
                        DataConnectionReady = true;
                        ConnectionStatusChanged();
                    }
                    break;
                case 2110:
                    {
                        // Connectivity broke, will be restored automatically
                        Connected = false;
                        DataConnectionReady = false;
                        ConnectionStatusChanged();
                    }
                    break;
                case 504:
                    {
                        // Not connected
                        Connected = false;
                        DataConnectionReady = false;
                        ConnectionStatusChanged();
                    }
                    break;
                case 2129:
                    {
                        // Information for product is informational only - remove from database
                    }
                    break;
                case 2105:
                    {
                        // Historical data connection offline
                        DataConnectionReady = false;
                        SetStatusIndicator(ProcessStatus.Offline);
                        Log(new LogMessage(ToString(), "!!! Lost Data Provider Connection", LogMessageType.Production));
                    }
                    break;
                case 2106:
                case 2107:
                    {
                        // Historical data connection online
                        DataConnectionReady = true;
                        SetStatusIndicator(ProcessStatus.Working);
                        RequestTimer.Start();
                        Log(new LogMessage(ToString(), "!!! Restored Data Provider Connection", LogMessageType.Production));
                    }
                    break;
                default:
                    {
                        Log(new LogMessage(ToString(), $"Unhandled TWS Message ID {errorCode}", LogMessageType.Error));
                    }
                    break;
            }
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
                Request request = PendingRequestQueue[reqId];

                // Set value for this callback
                request.StartDate = headTimestamp.FromIbkrFormat();

                Log(new LogMessage(ToString(), $"Received head timestamp data for [{request.ReqId}] {request.Security.Ticker} of {request.StartDate.ToString("yyyyMMdd")}", LogMessageType.Debug));

                if (request.StartDate < EarlyDateRequestLimit)
                    request.StartDate = EarlyDateRequestLimit;

                if ((request.EndDate - request.StartDate).TotalDays >= 365)
                {
                    PartitionLargeRequest(request);
                }

                // Send to handler
                HandleRequestReply(request, EventFlag.NotSet);

            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString(), ex.Message, LogMessageType.Error));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                HandleRequestReply(null, EventFlag.RequestError);
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
                Request request = PendingRequestQueue[reqId];

                // Make sure this is a valid trading day, since sometimes we get values for market closure days
                if (!Calendar.IsTradingDay(bar.Time.FromIbkrFormat()))
                    return;

                // Assign the price data to a PriceBar retrieved from the Security object
                var newBar = request.Security.GetPriceBar(bar.Time.FromIbkrFormat(), true);
                newBar.SetPriceValues(bar.Open.ToDecimal(), bar.High.ToDecimal(), bar.Low.ToDecimal(), bar.Close.ToDecimal(), bar.Volume);
                newBar.ToUpdate = true;
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString() + "historicalData bar", ex.Message, LogMessageType.Error));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return;
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
                Request request = PendingRequestQueue[reqId];
                HandleRequestReply(request, EventFlag.NotSet);
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString() + "historicalData end", ex.Message, LogMessageType.Error));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                HandleRequestReply(null, EventFlag.RequestError);
            }
        }

        #endregion

        #region Fundamental Data Request Handling

        /// <summary>
        /// Receives an XML-formatted text string response to reqFundamentalData
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="data"></param>
        public void fundamentalData(int reqId, string data)
        {
            try
            {
                // Not used in our implementation - use reqMktData with generic tick type instead
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                return;
            }
        }

        /// <summary>
        /// Called in response to mktData request but not used
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="marketDataType"></param>
        public void marketDataType(int reqId, int marketDataType)
        {
            //Console.WriteLine($"MarketDataType: {reqId}  {marketDataType}");
        }

        /// <summary>
        /// Called in response to mktData request but not used
        /// </summary>
        /// <param name="tickerId"></param>
        /// <param name="field"></param>
        /// <param name="price"></param>
        /// <param name="attribs"></param>
        public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
        {
            //Console.WriteLine($"tickPrice: {tickerId}  {field}  {price}  {attribs.ToString()}");
        }

        /// <summary>
        /// Called in response to mktData request but not used
        /// </summary>
        /// <param name="tickerId"></param>
        /// <param name="minTick"></param>
        /// <param name="bboExchange"></param>
        /// <param name="snapshotPermissions"></param>
        public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
        {
            //Console.WriteLine($"tickReqParams:  {tickerId} {minTick} {bboExchange} {snapshotPermissions}");
        }

        /// <summary>
        /// Called in response to mktData request but not used
        /// </summary>
        /// <param name="tickerId"></param>
        /// <param name="field"></param>
        /// <param name="size"></param>
        public void tickSize(int tickerId, int field, int size)
        {
            //Console.WriteLine($"tickSize: {tickerId}  {field}  {size}");
        }

        /// <summary>
        /// Called in response to fundamental data tick request only (for now)
        /// </summary>
        /// <param name="tickerId"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void tickString(int tickerId, int field, string value)
        {

            if (field == 47)
                Console.WriteLine("Fundamental Ratio received");
        }

        #endregion

        /// <summary>
        /// Returns the next valid Order ID per IBKR systems (Not used)
        /// </summary>
        /// <param name="orderId"></param>
        public void nextValidId(int orderId)
        {
            Console.WriteLine("###" + System.Reflection.MethodInfo.GetCurrentMethod().Name);
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Receives a contract description for exchange name and updates matching security in queue.
        /// Pushes request forward to original data request
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="contractDescriptions"></param>
        public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
        {
            try
            {
                Request request = PendingRequestQueue[reqId];

                var res = (from des in contractDescriptions
                           where des.Contract.Symbol == request.Security.Ticker &&
                           des.Contract.SecType == "STK" &&
                           des.Contract.Currency == "USD"
                           select des).FirstOrDefault();

                if (res == null)
                {
                    HandleRequestReply(request, EventFlag.BadSymbol);
                    return;
                }

                Log(new LogMessage(ToString() + "symbols", $"Received primary exchange for [{request.ReqId}] {request.Security.Ticker} of {res.Contract.PrimaryExch}"));

                // This is a specific point in the IBKR API
                if (res.Contract.PrimaryExch.Contains("NASDAQ"))
                {
                    request.Security.Exchange = request.Contract.PrimaryExch = "ISLAND";
                }
                else if (res.Contract.PrimaryExch.Contains("PINK"))
                {
                    // Remove symbol
                    HandleRequestReply(request, EventFlag.BadSymbol);
                    return;
                }
                else
                {
                    request.Security.Exchange = request.Contract.PrimaryExch = res.Contract.PrimaryExch;
                }

                HandleRequestReply(request, EventFlag.NotSet);
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString() + "exchange name", ex.Message, LogMessageType.Error));
                Console.WriteLine($"EXCEPTION:{GetCurrentMethod()}  {ex.Message}");
                HandleRequestReply(null, EventFlag.RequestError);
            }
        }

    }

    /// <summary>
    /// Non-Implemented EWrapper methods
    /// </summary>
    public partial class IbkrDataProvider : EWrapper
    {
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
            throw new NotImplementedException();
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
