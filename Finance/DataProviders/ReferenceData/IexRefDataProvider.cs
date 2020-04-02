using IBApi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Finance.Helpers;
using static Finance.Logger;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Reflection;

namespace Finance.Data
{
    public class IexHistoricalDataResponse
    {
        public DateTime date { get; set; }
        public decimal open { get; set; }
        public decimal high { get; set; }
        public decimal low { get; set; }
        public decimal close { get; set; }
        public long volume { get; set; }
        public decimal uOpen { get; set; }
        public decimal uHigh { get; set; }
        public decimal uLow { get; set; }
        public decimal uClose { get; set; }
        public long uVolume { get; set; }
        public decimal change { get; set; }
        public decimal changePercent { get; set; }
        public string label { get; set; }
        public decimal changeOverTime { get; set; }
    }
    public class IexContractDataResponse
    {
        public string symbol { get; set; }
        public string securityName { get; set; }
        public string securityType { get; set; }
        public string region { get; set; }
        public string exchange { get; set; }
    }
    public class IexSupportedSymbolsResponse : RefDataProviderSupportedSymbolsResponse
    {
        public string symbol
        {
            get => Ticker;
            set => Ticker = value;
        }
        public string exchange
        {
            get => Exchange;
            set => Exchange = value;
        }
        public string name
        {
            get => LongName;
            set => LongName = value;
        }
        public string date { get; set; }
        public bool isEnabled { get; set; }
        public string type { get; set; }
        public string region { get; set; }
        public string currency { get; set; }
        public string iexId { get; set; }
        public string figi { get; set; }
        public string cik { get; set; }
    }
    public class IexCompanyInfoResponse
    {
        public string symbol { get; set; }
        public string companyName { get; set; }
        public string exchange { get; set; }
        public string industry { get; set; }
        public string issueType { get; set; }
        public string sector { get; set; }
        public string primarySicCode { get; set; }
        public string[] tags { get; set; }
    }
    public class IexSectorListResponse : RefDataProviderSectorListResponse { }

    public class IexDataProviderRequest : RefDataProviderRequest
    {
        public bool RequestComplete { get; set; } = false;
        public List<string> RequestParameters = new List<string>();

        public bool HasRequestForDate(DateTime date)
        {
            return date.IsBetween(this.PriceDataRequestRange.start, this.PriceDataRequestRange.end, true);
        }

        public static IexDataProviderRequest GetRequest(RefDataProviderRequest request)
        {
            IexDataProviderRequest ret = new IexDataProviderRequest();
            request.CopyTo(ret);
            ret.GenerateParams();
            return ret;
        }
        private void GenerateParams()
        {
            switch (this.RequestType)
            {
                case DataProviderRequestType.SecurityContractData:
                    GenerateContractDataRequestParams();
                    break;
                case DataProviderRequestType.SecurityPriceData:
                case DataProviderRequestType.SecurityVolumeData:
                    GenerateHistoricalDataRequestParams();
                    break;
                case DataProviderRequestType.SecurityCompanyInfo:
                    GenerateCompanyInfoRequestParams();
                    break;
                default:
                    throw new NotImplementedException("Request type not supported");
            }
        }
        private void GenerateHistoricalDataRequestParams()
        {
            if (PriceDataRequestRange.start < Settings.Instance.DataRequestStartLimit)
                PriceDataRequestRange = (Settings.Instance.DataRequestStartLimit, PriceDataRequestRange.end);

            // Request daily data for each date
            for (DateTime d = PriceDataRequestRange.start; d <= PriceDataRequestRange.end; d = Calendar.NextTradingDay(d))
            {
                RequestParameters.Add(IexRequestString(Security, false, d));
            }

        }
        private void GenerateContractDataRequestParams()
        {
            RequestParameters.Add($@"search/{Security.Ticker}");
        }
        private void GenerateCompanyInfoRequestParams()
        {
            RequestParameters.Add($@"stock/{Security.Ticker}/company");
        }

        public static string IexRequestString(Security security, bool maxData = true, DateTime? requestDate = null)
        {
            if (!maxData && !requestDate.HasValue)
                return "ERR";


            StringBuilder sb = new StringBuilder();

            sb.Append($@"{security.Ticker}/chart/");

            if (maxData)
            {
                sb.Append($@"max?chartByDay=true");
            }
            else
            {
                sb.Append($@"date/{requestDate.Value.ToString("yyyyMMdd")}?chartByDay=true");
            }

            return sb.ToString();
        }
    }

    public class IexDataProvider : RefDataProvider
    {
        public override string Name => "IEX Cloud Ref Data";
        private IexClient iexClient;

        public IexDataProvider()
        {
        }

        public override void Connect()
        {
            iexClient = new IexClient();
            Connected = true;

            StatusMessage2 = MessageCountStatusString();
            Settings.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IexMessageCount")
                    StatusMessage2 = MessageCountStatusString();
            };
        }
        public override void Disconnect()
        {
            iexClient = null;
            Connected = false;
        }
        private string MessageCountStatusString()
        {
            decimal percentUsed = Settings.Instance.IexMessageCount.ToDecimal() / Settings.Instance.IexMessageCountLimit.ToDecimal();
            return string.Format($@"IEX Msgs used: {Settings.Instance.IexMessageCount:###,###,##0} / {Settings.Instance.IexMessageCountLimit:###,###,###} ({percentUsed*100:0.00}%)");
        }

        public override void SubmitRequest(RefDataProviderRequest request)
        {
            IexDataProviderRequest newRequest = IexDataProviderRequest.GetRequest(request);

            if (request.Security.Exchange == "UNK" &&
                newRequest.RequestType != DataProviderRequestType.SecurityContractData &&
                Settings.Instance.IexCloudMode == IexCloudMode.Production)
            {
                // Format a contract data request along with this request
                var contractRequest = RefDataProviderRequest.GetContractDataRequest(request.Security);
                new Task(() => SubmitRequestAsync(IexDataProviderRequest.GetRequest(contractRequest))).Start();
            }

            SubmitRequestAsync(newRequest);
        }
        public override void SubmitBatchRequest(List<RefDataProviderRequest> requests)
        {
            List<IexDataProviderRequest> newRequests = new List<IexDataProviderRequest>();

            requests.ForEach(req => newRequests.Add(IexDataProviderRequest.GetRequest(req)));
            SubmitBatchRequest(newRequests);
        }
        public override void CancelAllRequests(string cancelMessage)
        {
            throw new NotImplementedException();
        }
        private async void SubmitRequestAsync(IexDataProviderRequest request)
        {
            request.MarkSubmitted(DateTime.Now);
            switch (request.RequestType)
            {
                case DataProviderRequestType.SecurityContractData:
                    {
                        Log(new LogMessage(ToString(),
                            $"Submitting request for: {request.Security.Ticker} contract data",
                            LogMessageType.Debug));

                        if (await iexClient.SubmitContractDataRequestAsync(request))
                        {
                            if (request.RequestComplete)
                                OnDataProviderResponse(request);
                        }
                        break;
                    }
                case DataProviderRequestType.SecurityPriceData:
                case DataProviderRequestType.SecurityVolumeData:
                    {
                        Log(new LogMessage(ToString(),
                          $"Submitting request for: {request.Security.Ticker} " +
                          $"price data from {request.PriceDataRequestRange.start:yyyyMMdd} to {request.PriceDataRequestRange.end:yyyyMMdd} ",
                          LogMessageType.Debug));

                        if (iexClient.SubmitHistoricalDataRequest(request))
                        {
                            OnDataProviderResponse(request);
                        }
                        break;
                    }
                case DataProviderRequestType.SecurityCompanyInfo:
                    {
                        Log(new LogMessage(ToString(),
                          $"Submitting request for: {request.Security.Ticker} company info",
                          LogMessageType.Debug));

                        if (await iexClient.SubmitCompanyInfoRequestAsync(request))
                        {
                            OnDataProviderResponse(request);
                        }
                        break;
                    }
                default:
                    throw new NotImplementedException("Invalid request type");
            }
        }
        private async void SubmitBatchRequest(List<IexDataProviderRequest> requests)
        {
            DataProviderRequestType allType = DataProviderRequestType.NotSet;
            foreach (DataProviderRequestType requestType in Enum.GetValues(typeof(DataProviderRequestType)))
            {
                if (requests.TrueForAll(r => r.RequestType == requestType))
                {
                    allType = requestType;
                    break;
                }
            }
            if (allType == DataProviderRequestType.NotSet)
                throw new InvalidDataRequestException() { message = "Invalid batch request" };

            requests.ForEach(r => r.MarkSubmitted(DateTime.Now));

            List<IexDataProviderRequest> requestGroup = new List<IexDataProviderRequest>();

            while (requests.Count > 0)
            {
                requestGroup.Clear();
                requestGroup.AddRange(requests.Take(Settings.Instance.IexBatchSymbolLimit));
                requests.RemoveRange(0, requestGroup.Count);

                DateTime earliestRequestDate = (from request in requestGroup select request.PriceDataRequestRange.start).Min();
                DateTime latestRequestDate = (from request in requestGroup select request.PriceDataRequestRange.end).Max();

                switch (allType)
                {
                    case DataProviderRequestType.SecurityContractData:
                        {
                            throw new NotImplementedException();
                        }
                    case DataProviderRequestType.SecurityPriceData:
                    case DataProviderRequestType.SecurityVolumeData:
                        {
                            Log(new LogMessage(ToString(),
                              $"Submitting batch request for price data from {earliestRequestDate:yyyyMMdd} to {latestRequestDate:yyyyMMdd} ",
                              LogMessageType.Debug));
                            try
                            {

                                if (await iexClient.SubmitHistoricalBatchDataRequest(requestGroup))
                                {
                                    foreach (var request in requestGroup)
                                        OnDataProviderResponse(request);
                                }
                                break;
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    case DataProviderRequestType.SecurityCompanyInfo:
                        {
                            throw new NotImplementedException();
                        }
                    default:
                        throw new NotImplementedException("Invalid request type");
                }
            }

        }

        public override void GetProviderSupportedSymbols()
        {
            new Task(() => GetProviderSupportedSymbolsAsync()).Start();
        }
        private async void GetProviderSupportedSymbolsAsync()
        {
            List<IexSupportedSymbolsResponse> response = await iexClient.RequestSymbols();
            if (response != null)
            {
                // Remove all non common-stock or ETF entries and non-USD entries
                response.RemoveAll(x => (x.type.ToUpper() != "CS" && x.type.ToUpper() != "ET") || x.currency.ToUpper() != "USD");

                List<RefDataProviderSupportedSymbolsResponse> ret = new List<RefDataProviderSupportedSymbolsResponse>(response);
                OnDataProviderSupportedSymbolsResponse(ret);
            }
        }

        public override void GetProviderSectors()
        {
            new Task(() => GetProviderSectorsAsync()).Start();
        }
        private async void GetProviderSectorsAsync()
        {
            IexSectorListResponse response = await iexClient.RequestSectors();
            if (response != null)
            {
                OnDataProviderSectorsResponse(response);
            }
        }
    }

    public sealed class IexClient
    {
        public const string SandboxBaseURL = @"https://sandbox.iexapis.com";
        public const string ProductionBaseURL = @"https://cloud.iexapis.com";
        public const string Version = "stable";

        private int RequestThrottleMs { get; set; } = 10;

        private HttpClient httpClient;

        public string BaseURL
        {
            get
            {
                switch (Settings.Instance.IexCloudMode)
                {
                    case IexCloudMode.Sandbox:
                        return SandboxBaseURL;
                    case IexCloudMode.Production:
                        return ProductionBaseURL;
                    default:
                        throw new IEXDataProviderException() { message = "Invalid IEX Cloud Mode" };
                }
            }
        }
        private string PublishedToken
        {
            get
            {
                switch (Settings.Instance.IexCloudMode)
                {
                    case IexCloudMode.Sandbox:
                        return Settings.Instance.IexPublishableTokenSandbox;
                    case IexCloudMode.Production:
                        return Settings.Instance.IexPublishableTokenProduction;
                    default:
                        throw new IEXDataProviderException() { message = "Invalid IEX Cloud Mode" };
                }
            }
        }
        private string SecretToken
        {
            get
            {
                switch (Settings.Instance.IexCloudMode)
                {
                    case IexCloudMode.Sandbox:
                        return Settings.Instance.IexSecretTokenSandbox;
                    case IexCloudMode.Production:
                        return Settings.Instance.IexSecretTokenProduction;
                    default:
                        throw new IEXDataProviderException() { message = "Invalid IEX Cloud Mode" };
                }
            }
        }

        public IexClient()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(BaseURL);
            httpClient.Timeout = TimeSpan.FromMilliseconds(-1);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        #region Contract Data Request

        public async Task<bool> SubmitContractDataRequestAsync(IexDataProviderRequest request)
        {
            if (request.RequestParameters.Count != 1)
                throw new IEXDataProviderException() { message = "Request parameter count mismatch for contract data search" };

            var reqString = GetcontractDataRequestString(request.RequestParameters.Single());
            var httpResponse = httpClient.GetAsync(reqString).Result;
            if (httpResponse.IsSuccessStatusCode)
            {
                var data = await httpResponse.Content.ReadAsStringAsync();
                if (data == null)
                    Console.WriteLine();
                ProcessContractDataResponse(request, data);
                ProcessMessageCount(httpResponse);
            }
            else
            {
                ProcessErrorCode(httpResponse, request);
                return true;
            }

            Thread.Sleep(RequestThrottleMs); // IEX throttle, just to be safe

            return true;
        }
        private void ProcessContractDataResponse(IexDataProviderRequest request, string data)
        {
            List<IexContractDataResponse> ret = new List<IexContractDataResponse>();

            try
            {
                //
                // Process from JSON response to POCO
                //
                JArray jArray = JArray.Parse(data);
                foreach (var entry in jArray)
                {
                    var newItem = ret.AddAndReturn(new IexContractDataResponse());
                    foreach (PropertyInfo property in typeof(IexContractDataResponse).GetTypeInfo().GetProperties())
                    {
                        property.SetValue(newItem, Convert.ChangeType(entry[property.Name], property.PropertyType));
                    }
                }

                //
                // Process POCO into Security object
                //
                ProcessContractData(request, ret);
            }
            catch (Exception ex)
            {
                Log(new LogMessage("IEX Process Response", $"{ex.Message}", LogMessageType.SecurityError));
                throw ex;
            }
        }
        private void ProcessContractData(IexDataProviderRequest request, List<IexContractDataResponse> iexContractDataList)
        {
            Security security = request.Security;

            var matchingData = iexContractDataList.Find(x => (x.securityType == "cs" || x.securityType == "et") && x.symbol == security.Ticker);

            security.Exchange = matchingData.exchange;
            security.LongName = matchingData.securityName;

            request.RequestComplete = true;
            request.MarkComplete();
        }
        private string GetcontractDataRequestString(string paramString)
        {
            return string.Format($@"{BaseURL}/{Version}/{paramString}?token={SecretToken}");
        }

        #endregion#region Contract Data Request
        #region Historical Data Request

        public async Task<bool> SubmitHistoricalBatchDataRequest(List<IexDataProviderRequest> requests)
        {
            if (requests.Count > 100)
                throw new InvalidDataRequestException() { message = "Batch requests limited to 100 symbols each" };

            List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();

            DateTime earliestRequestDate = (from request in requests select request.PriceDataRequestRange.start).Min();
            DateTime latestRequestDate = (from request in requests select request.PriceDataRequestRange.end).Max();

            List<string> batchRequestStrings = new List<string>();

            for (DateTime requestDate = earliestRequestDate; requestDate <= latestRequestDate; requestDate = Calendar.NextTradingDay(requestDate))
            {
                batchRequestStrings.Add(GetHistoricalDataBatchRequestString(requests, requestDate));
            }

            foreach (string reqString in batchRequestStrings)
            {
                //tasks.Add(httpClient.GetAsync(reqString));

                var result = httpClient.GetAsync(reqString).Result;
                //Thread.Sleep(RequestThrottleMs); // IEX throttle, just to be safe               

                if (result.IsSuccessStatusCode)
                {
                    var data = await result.Content.ReadAsStringAsync();
                    if (data == null)
                        Console.WriteLine();
                    ProcessMessageCount(result);
                    ProcessHistoricalPriceDataBatchResponse(requests, data);
                }
                else
                {
                    //ProcessBatchErrorCode(requests, requests);
                    Console.WriteLine();
                }
            }
            return true;
        }
        public bool SubmitHistoricalDataRequest(IexDataProviderRequest request)
        {

            List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();

            foreach (string paramString in request.RequestParameters)
            {
                var reqString = GetHistoricalDataRequestString(paramString);
                tasks.Add(httpClient.GetAsync(reqString));
                //Thread.Sleep(RequestThrottleMs); // IEX throttle, just to be safe               
            }

            Task.WaitAll(tasks.ToArray(), -1);

            List<Task<string>> results = new List<Task<string>>();
            tasks.ForEach(t =>
            {
                if (t.Result.IsSuccessStatusCode)
                {
                    results.Add(t.Result.Content.ReadAsStringAsync());
                    ProcessMessageCount(t.Result);
                }
                else
                {
                    ProcessErrorCode(t.Result, request);
                }
            });

            Task.WaitAll(results.ToArray(), -1);

            results.ForEach(r =>
            {
                string data = r.Result;
                ProcessHistoricalPriceDataResponse(request, data);
            });

            return true;
        }

        private void ProcessHistoricalPriceDataResponse(IexDataProviderRequest request, string data)
        {
            List<IexHistoricalDataResponse> ret = new List<IexHistoricalDataResponse>();

            try
            {
                //
                // Process from JSON response to POCO
                //
                JArray jArray = JArray.Parse(data);
                foreach (var entry in jArray)
                {
                    var newItem = ret.AddAndReturn(new IexHistoricalDataResponse());
                    foreach (PropertyInfo property in typeof(IexHistoricalDataResponse).GetTypeInfo().GetProperties())
                    {
                        property.SetValue(newItem, Convert.ChangeType(entry[property.Name], property.PropertyType));
                    }
                }

                //
                // Process POCO into Security object
                //
                ProcessHistoricalPriceData(request, ret);
            }
            catch (Exception ex)
            {
                Log(new LogMessage("IEX Process Response", $"{ex.Message}", LogMessageType.SecurityError));
                throw ex;
            }
        }
        private void ProcessHistoricalPriceData(IexDataProviderRequest request, List<IexHistoricalDataResponse> iexPriceDataList)
        {
            try
            {
                Security security = request.Security;

                foreach (IexHistoricalDataResponse iexObject in iexPriceDataList)
                {
                    var newBar = security.GetPriceBar(iexObject.date, PriceBarSize.Daily, true);

                    // If there is low/no volume, we will get a return with just the close price (ie, prices didn't move).
                    if (iexObject.open == 0 && iexObject.high == 0 && iexObject.low == 0)
                        newBar.SetPriceValues(iexObject.close, iexObject.close, iexObject.close, iexObject.close, iexObject.volume);
                    else
                        newBar.SetPriceValues(iexObject.open, iexObject.high, iexObject.low, iexObject.close, iexObject.volume);

                    newBar.ToUpdate = true;
                    newBar.PriceDataProvider = DataProviderType.IEXCloud;
                }

                request.RequestComplete = true;
                request.MarkComplete();
            }
            catch (Exception ex)
            {
                request.MarkError(DataProviderErrorType.SystemError, ex.Message);
                //throw;
            }
        }

        private void ProcessHistoricalPriceDataBatchResponse(List<IexDataProviderRequest> requests, string data)
        {
            List<IexHistoricalDataResponse> ret = new List<IexHistoricalDataResponse>();

            try
            {
                JObject jObj = JObject.Parse(data);
                foreach (var entry in jObj)
                {
                    JArray arr = (entry.Value["chart"] as JArray);
                    foreach (var entry2 in arr)
                    {
                        var newItem = ret.AddAndReturn(new IexHistoricalDataResponse());
                        foreach (PropertyInfo property in typeof(IexHistoricalDataResponse).GetTypeInfo().GetProperties())
                        {
                            property.SetValue(newItem, Convert.ChangeType(entry2[property.Name], property.PropertyType));
                        }
                    }
                    var req = requests.Find(x => x.Security.Ticker == entry.Key);

                    if (req == null)
                    {
                        Log(new LogMessage(ToString(), $"Could not process request response for {entry.Key}", LogMessageType.SystemError));
                        continue;
                    }
                    else
                        ProcessHistoricalPriceData(req, ret);
                }

            }
            catch (Exception ex)
            {
                Log(new LogMessage("IEX Process Response", $"{ex.Message}", LogMessageType.SecurityError));
                //throw ex;
            }
        }

        private string GetHistoricalDataRequestString(string paramString)
        {
            return string.Format($@"{BaseURL}/{Version}/stock/{paramString}&token={SecretToken}");
        }
        private string GetHistoricalDataBatchRequestString(List<IexDataProviderRequest> requests, DateTime requestDate)
        {
            string symbolList = BatchSymbolList(requests.Where(r => r.HasRequestForDate(requestDate)).ToList());
            return string.Format($@"{BaseURL}/{Version}/stock/market/batch/?symbols={symbolList}&types=chart&range=date&exactDate={requestDate.ToString("yyyyMMdd")}&chartByDay=true&chartIEXOnly=true&token={SecretToken}");
        }
        private string BatchSymbolList(List<IexDataProviderRequest> requests)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var request in requests)
                sb.Append($"{request.Security.Ticker},");

            return sb.ToString().TrimEnd(',');
        }

        #endregion
        #region Reference Data Requests

        //
        // All IEX Supported Symbols
        //
        public async Task<List<IexSupportedSymbolsResponse>> RequestSymbols()
        {
            var httpResponse = httpClient.GetAsync(GetSymbolListRequestString()).Result;
            if (httpResponse.IsSuccessStatusCode)
            {
                var data = await httpResponse.Content.ReadAsStringAsync();
                if (data == null)
                    Console.WriteLine();
                var ret = ProcessSymbolListResponse(data);
                ProcessMessageCount(httpResponse);
                return ret;
            }
            return null;
        }
        private string GetSymbolListRequestString()
        {
            return string.Format($@"{BaseURL}/{Version}/ref-data/symbols?token={SecretToken}");
        }
        private List<IexSupportedSymbolsResponse> ProcessSymbolListResponse(string data)
        {
            List<IexSupportedSymbolsResponse> ret = new List<IexSupportedSymbolsResponse>();

            try
            {
                //
                // Process from JSON response to POCO
                //
                JArray jArray = JArray.Parse(data);
                foreach (var entry in jArray)
                {
                    var newItem = ret.AddAndReturn(new IexSupportedSymbolsResponse());
                    foreach (PropertyInfo property in typeof(IexSupportedSymbolsResponse).GetTypeInfo().GetProperties())
                    {
                        if (entry[property.Name] != null)
                            property.SetValue(newItem, Convert.ChangeType(entry[property.Name], property.PropertyType));
                    }
                }

                return ret;
            }
            catch (Exception ex)
            {
                Log(new LogMessage("IEX Process Response", $"{ex.Message}", LogMessageType.SecurityError));
                throw ex;
            }
        }

        //
        // Company information
        //
        public async Task<bool> SubmitCompanyInfoRequestAsync(IexDataProviderRequest request)
        {
            Thread.Sleep(RequestThrottleMs); // IEX throttle, just to be safe

            if (request.RequestParameters.Count != 1)
                throw new IEXDataProviderException() { message = "Request parameter count mismatch for company info search" };

            var reqString = GetCompanyInfoRequestString(request.RequestParameters.Single());
            var httpResponse = httpClient.GetAsync(reqString).Result;
            if (httpResponse.IsSuccessStatusCode)
            {
                var data = await httpResponse.Content.ReadAsStringAsync();
                if (data == null)
                    Console.WriteLine();
                ProcessCompanyInfoResponse(request, data);
                ProcessMessageCount(httpResponse);
            }
            else
            {
                ProcessErrorCode(httpResponse, request);
                return true;
            }



            return true;
        }
        private void ProcessCompanyInfoResponse(IexDataProviderRequest request, string data)
        {


            IexCompanyInfoResponse ret = null;

            try
            {
                //
                // Process from JSON response to POCO
                //
                JObject jObject = JObject.Parse(data);

                ret = new IexCompanyInfoResponse();
                foreach (PropertyInfo property in typeof(IexCompanyInfoResponse).GetTypeInfo().GetProperties())
                {
                    if (data.Contains("BWMX"))
                        Console.WriteLine(property.Name);

                    if (jObject[property.Name].Type == JTokenType.Array)
                    {
                        // Tags is the only array in this object
                        JToken val = jObject[property.Name];
                        if (!val.HasValues)
                        {
                            property.SetValue(ret, new string[] { "" });
                        }
                        else
                        {
                            var tags = (val?.ToObject<string[]>());
                            property.SetValue(ret, tags);
                        }
                    }
                    else
                    {
                        var value = jObject[property.Name]?.ToObject(property.PropertyType);
                        if (value == null)
                            property.SetValue(ret, "0");
                        else
                            property.SetValue(ret, value);
                    }
                }

                //
                // Process POCO into Security object
                //
                ProcessCompanyInfo(request, ret);
            }
            catch (Exception ex)
            {
                Log(new LogMessage("IEX Process Response", $"{ex.Message}", LogMessageType.SecurityError));
                throw ex;
            }
        }
        private void ProcessCompanyInfo(IexDataProviderRequest request, IexCompanyInfoResponse iexCompanyInfo)
        {
            Security security = request.Security;

            /*
             *  Add info to security
             */
            security.Industry = iexCompanyInfo.industry;
            security.Sector = iexCompanyInfo.sector;
            security.SicCode = Int32.Parse(iexCompanyInfo.primarySicCode);
            security.SecurityType = iexCompanyInfo.issueType.FromIexCode();

            request.RequestComplete = true;
            request.MarkComplete();
        }
        private string GetCompanyInfoRequestString(string paramString)
        {
            return string.Format($@"{BaseURL}/{Version}/{paramString}?token={SecretToken}");
        }

        //
        // Available sector tags
        //
        public async Task<IexSectorListResponse> RequestSectors()
        {
            var httpResponse = httpClient.GetAsync(GetSectorListRequestString()).Result;
            if (httpResponse.IsSuccessStatusCode)
            {
                var data = await httpResponse.Content.ReadAsStringAsync();
                var ret = ProcessSectorListResponse(data);
                ProcessMessageCount(httpResponse);
                return ret;
            }
            return null;
        }
        private string GetSectorListRequestString()
        {
            return string.Format($@"{BaseURL}/{Version}/ref-data/sectors?token={SecretToken}");
        }
        private IexSectorListResponse ProcessSectorListResponse(string data)
        {
            IexSectorListResponse ret = new IexSectorListResponse();

            try
            {
                //
                // Process from JSON response to POCO
                //
                JArray jArray = JArray.Parse(data);
                foreach (var entry in jArray)
                {
                    ret.SectorNames.Add((string)entry["name"]);
                }

                return ret;
            }
            catch (Exception ex)
            {
                Log(new LogMessage("IEX Process Response", $"{ex.Message}", LogMessageType.SystemError));
                throw ex;
            }
        }

        #endregion

        private void ProcessMessageCount(HttpResponseMessage message)
        {
            if (Int32.TryParse(message.Headers.GetValues("iexcloud-messages-used").FirstOrDefault(), out int msgCount))
            {
                Settings.Instance.IexMessageCount += msgCount;
            }
        }
        private void ProcessErrorCode(HttpResponseMessage httpResponse, IexDataProviderRequest request)
        {
            request.MarkError(DataProviderErrorType.InvalidRequest, httpResponse.ReasonPhrase);
            Log(new LogMessage("IEX REST Error", $"[{httpResponse.StatusCode.ToInt()}] {httpResponse.ReasonPhrase}", LogMessageType.SystemError));
        }
        private void ProcessBatchErrorCode(HttpResponseMessage httpResponse, List<IexDataProviderRequest> requests)
        {
            //request.MarkError(DataProviderErrorType.InvalidRequest, httpResponse.ReasonPhrase);
            Log(new LogMessage("IEX REST Error", $"[{httpResponse.StatusCode.ToInt()}] {httpResponse.ReasonPhrase}", LogMessageType.SystemError));
        }
    }
}
