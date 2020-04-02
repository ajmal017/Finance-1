using Finance;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance.Data
{

    /// <summary>
    /// Manages a Database object and an IDataProvider to update and deliver pricing data.
    /// Acts as the interface between the PortfolioManager and the back-end data storage.
    /// </summary>
    public class RefDataManager : IProviderStatus
    {
        private static RefDataManager _Instance { get; set; }
        public static RefDataManager Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new RefDataManager();
                return _Instance;
            }
        }

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler SecurityListLoaded;
        private void OnSecurityListLoaded()
        {
            SecurityListLoaded?.Invoke(this, new EventArgs());
        }

        public event SecurityDataUpdateEventHandler SecurityDataChanged;
        private void OnSecurityDataChanged()
        {
            SecurityDataChanged?.Invoke(this, new SecurityDataUpdateEventArgs());
        }
        private void OnSecurityDataChanged(Security security)
        {
            SecurityDataChanged?.Invoke(this, new SecurityDataUpdateEventArgs(security));
        }
        private void OnSecurityDataChanged(List<Security> securities)
        {
            SecurityDataChanged?.Invoke(this, new SecurityDataUpdateEventArgs(securities));
        }

        #endregion
        #region Status Indicator

        public string Name => DataProvider.Name;

        private ControlStatus _Status { get; set; } = ControlStatus.Offline;
        public ControlStatus Status
        {
            get
            {
                if (_Status == ControlStatus.ErrorState || DataProvider.Status == ControlStatus.ErrorState)
                    return ControlStatus.ErrorState;
                if (_Status == ControlStatus.Working || DataProvider.Status == ControlStatus.Working)
                    return ControlStatus.Working;

                return (ControlStatus)Math.Min(_Status.ToInt(), DataProvider.Status.ToInt());
            }
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    OnPropertyChanged("Status");
                }
            }
        }

        public bool Connected => DataProvider.Connected;

        public string StatusMessage
        {
            get => _StatusMessage();
            set { }
        }
        private string _StatusMessage()
        {
            switch (Status)
            {
                case ControlStatus.Ready:
                    return "Ready";
                case ControlStatus.Working:
                    if (_Status == ControlStatus.Working)
                        return "Database working";
                    if (DataProvider.Status == ControlStatus.Working)
                        return "Provider Working";
                    return "ERR";
                case ControlStatus.Offline:
                    return "Provider Offline";
                case ControlStatus.ErrorState:
                    if (_Status == ControlStatus.ErrorState)
                        return "Database Error";
                    if (DataProvider.Status == ControlStatus.ErrorState)
                        return "Provider Error";
                    return "ERR";
                default:
                    return "Status unknown";
            }
        }

        public string StatusMessage2
        {
            get => DataProvider.StatusMessage2;
            set { }
        }

        public void Connect()
        {
            //
            // Connect provider
            //
            DataProvider.Connect();

            //
            // Load Securities from database
            //
            if (ReloadSecuritiesList)
                new Thread(() => LoadSecurityList()).Start();
        }

        private bool _Busy => _Status == ControlStatus.Working;
        public bool Busy
        {
            get => this._Busy | DataProvider.Busy;
            set
            {
                if (value)
                    Status = ControlStatus.Working;
                else
                    Status = ControlStatus.Ready;
            }
        }

        #endregion

        private RefDataProvider DataProvider
        {
            get
            {
                var provider = RefDataProvider.Instance(this);
                if (RefDataProvider.NeedsInitialize)
                    InitializeNewDataProvider(provider);

                return provider;
            }
        }
        private PriceDatabase PriceDatabase
        {
            get
            {
                return PriceDatabase.Instance;
            }
        }

        // Stored reference to all security data in database, loaded every time database data is updated
        private List<Security> AllSecuritiesList { get; set; } = new List<Security>();
        private bool ReloadSecuritiesList = true;

        public bool ProviderConnected => DataProvider == null ? false : DataProvider.Connected;
        public bool DatabaseConnected => !(PriceDatabase == null);

        public RefDataManager()
        {
            this.InitializeMe();
            Log(new LogMessage(ToString(), "Data Manager initialized", LogMessageType.Production));
        }

        [Initializer]
        private void InitializeDatabaseConnection()
        {
            if (PriceDatabase.Instance == null)
            {
                Status = ControlStatus.ErrorState;
                return;
            }

            Status = ControlStatus.Ready;

            if (Settings.Instance.AutoLoadSecuritiesOnStart)
                new Thread(() => LoadSecurityList()).Start();

        }

        private void InitializeNewDataProvider(RefDataProvider provider)
        {
            // Assign event handlers
            provider.DataProviderRequestResponse += DataProviderResponseHandler;
            provider.DataProviderSupportedSymbolsResponse += DataProviderSupportedSymbolsResponseHandler;
            provider.DataProviderSectorsResponse += DataProviderSectorListResponseHandler;
            provider.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);

            RefDataProvider.NeedsInitialize = false;
        }
        public void ResetDataProviderConnection(int waitMsReconnect = 1000)
        {
            DataProvider.Disconnect();
            Thread.Sleep(waitMsReconnect);
            DataProvider.Connect();
        }

        #region Database Calls

        public Security GetSecurity(string ticker, bool create = true)
        {
            if (AllSecuritiesList.Find(x => x.Ticker == ticker) != null)
            {
                return AllSecuritiesList.Find(x => x.Ticker == ticker);
            }

            return PriceDatabase.GetSecurity(ticker, create, false);
        }
        public void SetSecurity(Security security, bool RequiresReload = true, bool Overwrite = false)
        {
            Busy = true;
            ReloadSecuritiesList = RequiresReload;
            PriceDatabase.SetSecurity(security, Overwrite);
            Busy = false;

            OnSecurityDataChanged(security);
        }
        public void SetSecurities(List<Security> securities, bool RequiresReload = true, bool Overwrite = false)
        {
            Busy = true;
            Log(new LogMessage(ToString(), $"Sending {securities.Count} to database for saving", LogMessageType.Production));
            ReloadSecuritiesList = RequiresReload;
            securities.ForEach(sec => PriceDatabase.SetSecurity(sec, Overwrite));

            Busy = false;
            OnSecurityDataChanged(securities);
        }

        public void DeleteSecurity(Security security)
        {
            ReloadSecuritiesList = true;
            PriceDatabase.RemoveSecurity(security);

            OnSecurityDataChanged(security);
        }

        public List<string> GetAllTickers()
        {
            return PriceDatabase.AllTickers();
        }
        public List<Security> GetAllSecurities()
        {
            if (ReloadSecuritiesList)
            {
                LoadSecurityList();
                return AllSecuritiesList;
            }
            else
                return AllSecuritiesList;
        }
        public void LoadSecurityList()
        {
            Busy = true;
            ReloadSecuritiesList = false;

            Log(new LogMessage(ToString(), "Reload Security List [START]", LogMessageType.Debug));

            AllSecuritiesList.Clear();
            AllSecuritiesList.AddRange(PriceDatabase.AllSecurities());
            SecurityLoadOperations();

            Log(new LogMessage(ToString(), "Reload Security List [END]", LogMessageType.Debug));

            Busy = false;

            OnSecurityListLoaded();
        }

        public void LoadSymbol(string ticker)
        {
            Log(new LogMessage("DataManager", $"Loading {ticker}", LogMessageType.Debug));

            Security toSave = GetSecurity(ticker, true);
            ReloadSecuritiesList = true;
            GetAllSecurities();
        }

        public void RemoveAllExclusions()
        {
            foreach (Security security in GetAllSecurities())
            {
                security.Excluded = false;
            }
            SetSecurities(GetAllSecurities(), false);
        }
        public void CleanZeroValues()
        {
            foreach (Security security in GetAllSecurities())
            {
                bool modified = false;

                foreach (PriceBar bar in security.DailyPriceBarData)
                {
                    var d = bar.BarDateTime;
                    if (bar.Open == 0m &&
                    bar.High == 0m &&
                    bar.Low == 0m)
                    {
                        bar.SetPriceValues(bar.Close, bar.Close, bar.Close, bar.Close, bar.Volume);
                        modified = true;
                    }
                }

                if (modified)
                    SetSecurity(security, false, true);
            }

            OnSecurityListLoaded();
        }

        //
        // Methods to execute upon security list load (sets some dynamic values and filters bad data
        //
        private void SecurityLoadOperations()
        {
            Log(new LogMessage("DataManager", "Executing post-load operations...", LogMessageType.Production));

            ScanForMissingData();
            ScanForZeroVolumeDays();

            foreach (Security security in AllSecuritiesList)
            {
                if (security.Modified)
                    SetSecurity(security, false);
            }

            Log(new LogMessage("DataManager", "Completed post-load operations", LogMessageType.Production));
        }
        private void ScanForMissingData()
        {

            foreach (Security security in AllSecuritiesList)
            {
                int priceBarCorrectCount = Calendar.TradingDayCount(security.GetFirstBar(PriceBarSize.Daily).BarDateTime, Calendar.PriorTradingDay(DateTime.Today));
                var secCount = security.GetPriceBars(security.GetFirstBar(PriceBarSize.Daily).BarDateTime, Calendar.PriorTradingDay(DateTime.Today), PriceBarSize.Daily, true).Count;

                // Check for missing data
                if (secCount != priceBarCorrectCount)
                {
                    if (!security.MissingData)
                    {
                        security.MissingData = true;
                        security.Modified = true;
                    }
                }
                else
                {
                    if (security.MissingData)
                    {
                        security.MissingData = false;
                        security.Modified = true;
                    }
                }
            }
        }
        private void ScanForZeroVolumeDays()
        {
            foreach (Security security in AllSecuritiesList)
            {
                if (ZeroVolumeDays(security, 360))
                {
                    if (!security.ZeroVolume)
                    {
                        security.ZeroVolume = true;
                        security.Modified = true;
                    }
                }
                else
                {
                    if (security.ZeroVolume)
                    {
                        security.ZeroVolume = false;
                        security.Modified = true;
                    }
                }
            }
        }
        private bool ZeroVolumeDays(Security security, int lookback)
        {
            var bars = security.GetLastBar(PriceBarSize.Daily).PriorBars(lookback);
            if (bars.Any(x => x.Volume == 0))
                return true;

            return false;
        }
        private void SetTechnical()
        {
            foreach (Security security in AllSecuritiesList)
            {
                Candlesticks.SetCandlestickPatterns(security);
                Technicals.SetTechnicals(security);
            }
        }

        #endregion

        #region Data Provider Calls

        private void UpdateSecurityPriceDataBatch(List<Security> securities, DateTime updateTo)
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            new Task(() =>
            {

                List<RefDataProviderRequest> requests = new List<RefDataProviderRequest>();

                foreach (Security security in securities)
                {
                    if (security.DataUpToDate)
                        continue;

                    if (security.DailyPriceBarData.Count == 0)
                    {
                        requests.Add(RefDataProviderRequest.GetPriceDataRequest(security, DateTime.MinValue, updateTo));
                    }
                    else if (security.DailyPriceBarData.Count > 0)
                    {
                        requests.Add(RefDataProviderRequest.GetPriceDataRequest(security, updateTo));
                    }
                }

                Busy = true;
                DataProvider.SubmitBatchRequest(requests);
            }).Start();
        }
        public void UpdateSecurityPriceData(Security security, DateTime updateTo)
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            if (security.DailyPriceBarData.Count == 0)
            {
                var request = RefDataProviderRequest.GetPriceDataRequest(security, DateTime.MinValue, updateTo);

                Busy = true;
                new Task(() => DataProvider.SubmitRequest(request)).Start();
            }
            else if (security.DailyPriceBarData.Count > 0)
            {
                var request = RefDataProviderRequest.GetPriceDataRequest(security, updateTo);

                Busy = true;
                new Task(() => DataProvider.SubmitRequest(request)).Start();
            }
        }
        public void UpdateSecurityPriceData(Security security, DateTime startDate, DateTime endDate)
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            RefDataProviderRequest request = RefDataProviderRequest.GetPriceDataRequest(security, startDate, endDate);

            Busy = true;
            new Task(() => DataProvider.SubmitRequest(request)).Start();
        }

        public void UpdateAllPriceData(DateTime updateTo)
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            Log(new LogMessage(ToString(), $"*** Begin update all securities in database to {updateTo.ToString("yyyyMMdd")} ***", LogMessageType.Production));

            UpdateSecurityPriceDataBatch(GetAllSecurities(), updateTo);

            //ReloadSecuritiesList = true;
            //OnSecurityListLoaded();
        }
        public void UpdateAllMissingPriceData()
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            new Task(() =>
            {
                Log(new LogMessage(ToString(), $"*** Begin update missing price data ***", LogMessageType.Production));

                DateTime startDate = Settings.Instance.DataRequestStartLimit;
                if (!Calendar.IsTradingDay(startDate))
                    startDate = Calendar.NextTradingDay(startDate);
                DateTime endDate = Calendar.PriorTradingDay(DateTime.Today);

                foreach (Security security in GetAllSecurities())
                {
                    for (DateTime currentDate = startDate;
                        currentDate <= endDate;
                        currentDate = Calendar.NextTradingDay(currentDate))
                    {
                        if (!security.HasBar(currentDate, PriceBarSize.Daily))
                        {
                            Log(new LogMessage("Missing Data", $"Requesting missing data for {security.Ticker}, {currentDate:yyyyMMdd}", LogMessageType.Production));
                            UpdateSecurityPriceData(security, currentDate, currentDate);
                            Thread.Sleep(10);
                        }
                    }
                }

            }).Start();

        }

        public void RequestContractData(Security security)
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            RefDataProviderRequest request = RefDataProviderRequest.GetContractDataRequest(security);
            Busy = true;
            new Task(() => DataProvider.SubmitRequest(request)).Start(); ;
        }
        public void RequestContractDataAll()
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            new Task(() =>
            {
                Log(new LogMessage(ToString(), $"*** Request contract data all UNK securities ***", LogMessageType.Production));
                foreach (Security security in GetAllSecurities())
                {
                    if (security.Exchange != "UNK")
                        continue;

                    Busy = true;
                    RequestContractData(security);
                }

                ReloadSecuritiesList = true;
                OnSecurityListLoaded();

            }).Start();
        }

        public void RequestCompanyInfo(Security security)
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            RefDataProviderRequest request = RefDataProviderRequest.GetCompanyInfoRequest(security);
            Busy = true;
            new Task(() => DataProvider.SubmitRequest(request)).Start();
        }
        public void RequestCompanyInfoAll()
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            new Task(() =>
            {
                Log(new LogMessage(ToString(), $"*** Request company info all NOT_SET securities ***", LogMessageType.Production));
                foreach (Security security in GetAllSecurities())
                {
                    if (security.Sector == "NOT_SET" ||
                    security.Industry == "NOT_SET" ||
                    security.SicCode == 0 ||
                    security.SecurityType == SecurityType.Unknown)
                    {
                        Busy = true;
                        RequestCompanyInfo(security);
                    }
                }

            }).Start();
        }

        public void RequestProviderSupportedSymbols()
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            new Task(() => DataProvider.GetProviderSupportedSymbols()).Start();
        }
        public void RequestProviderSectors()
        {
            if (!DataProvider.Connected)
            {
                Log(new LogMessage(ToString(), "Data Provider Not Connected", LogMessageType.SystemError));
                return;
            }

            new Task(() => DataProvider.GetProviderSectors()).Start();
        }

        private void DataProviderResponseHandler(object sender, DataProviderResponseEventArgs e)
        {
            if (e.HasError)
                ErrorRequestHandler(e);
            else if (e.Request.RequestStatus == DataProviderRequestStatus.CompleteResponse)
                CompleteRequestHandler(e);
            else if (e.Request.RequestStatus == DataProviderRequestStatus.Cancelled)
                CancelledRequestHandler(e);
            else
                InvalidStateRequestHandler(e);

            Busy = DataProvider.Busy;
        }
        private void ErrorRequestHandler(DataProviderResponseEventArgs e)
        {
            switch (e.Request.DataProviderErrorType)
            {
                case DataProviderErrorType.NoError:
                case DataProviderErrorType.ConnectionError:
                case DataProviderErrorType.Cancelled:
                case DataProviderErrorType.InvalidRequest:
                case DataProviderErrorType.SystemError:
                    // Do something???
                    break;
                case DataProviderErrorType.InvalidSecurity:
                    // Remove security from database
                    if (Settings.Instance.AutoDeleteInvalidSecurities)
                    {
                        Log(new LogMessage(ToString(), $"Symbol {e.Request.Security.Ticker} invalid - remove from database", LogMessageType.SecurityError));
                        DeleteSecurity(e.Request.Security);
                    }
                    else
                        Log(new LogMessage(ToString(), $"Symbol {e.Request.Security.Ticker} invalid", LogMessageType.SecurityError));

                    break;
            }
        }
        private void CompleteRequestHandler(DataProviderResponseEventArgs e)
        {
            if (e.Request.RequestStatus != DataProviderRequestStatus.CompleteResponse)
                throw new InvalidDataRequestException();

            Log(new LogMessage(ToString(), $"Saving data for {e.Request.Security.Ticker} to database", LogMessageType.Production));
            SetSecurity(e.Request.Security, false);
        }
        private void CancelledRequestHandler(DataProviderResponseEventArgs e)
        {
            Log(new LogMessage(ToString(), $"Request cancelled for {e.Request.Security.Ticker}", LogMessageType.Production));
        }
        private void InvalidStateRequestHandler(DataProviderResponseEventArgs e)
        {
            Log(new LogMessage(ToString(), $"Invalid request state returned to DataManager [{e.Request.Security.Ticker}] [{e.Request.RequestStatus.Description()}]", LogMessageType.SecurityError));
        }
        private void DataProviderSupportedSymbolsResponseHandler(object sender, DataProviderSupportedSymbolsEventArgs e)
        {
            Busy = true;

            if (e.Symbols == null)
            {
                Log(new LogMessage(ToString(), $"Symbol request from {nameof(DataProvider.Instance)} returned no values", LogMessageType.Production));
                return;
            }

            //
            // Process symbols
            //
            List<Security> toSave = new List<Security>();
            var symbolList = e.Symbols;

            if (Settings.Instance.ApplicationMode == ApplicationMode.Testing)
            {
                symbolList = symbolList.Take(Settings.Instance.TestingModeSecurityCount).ToList();
            }

            foreach (var symbol in symbolList)
            {
                var sec = toSave.AddAndReturn(GetSecurity(symbol.Ticker, true));
                sec.LongName = symbol.LongName;
                sec.Exchange = symbol.Exchange;
            }

            if (toSave.Count > 0)
            {
                Log(new LogMessage("DataManager", $"Saving {toSave.Count} Symbols to DB", LogMessageType.Debug));
                SetSecurities(toSave);
                ReloadSecuritiesList = true;
                GetAllSecurities();
            }

            Busy = false;
        }
        private void DataProviderSectorListResponseHandler(object sender, DataProviderSectorsEventArgs e)
        {
            if (e.Response.SectorNames.Count > 0)
            {
                Settings.Instance.MarketSectors = e.Response.SectorNames;
                Log(new LogMessage(ToString(), $"Processed Data Provider sector list ({e.Response.SectorNames.Count} items)", LogMessageType.Production));
            }

            foreach (var item in Settings.Instance.MarketSectors)
            {
                Console.WriteLine(item);
            }
        }

        #endregion

    }
}
