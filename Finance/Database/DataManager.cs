using Finance;
using System;
using System.Collections.Generic;
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
    public partial class DataManager
    {

        #region Events

        public event EventHandler DataProviderConnectionStatusChanged;
        private void OnDataProviderConnectionStatusChanged()
        {
            Log(new LogMessage(ToString(), "Data Provider Connection Status Changed", LogMessageType.Production));
            DataProviderConnectionStatusChanged?.Invoke(this, null);
        }

        public event SecurityResponseDataHandler SecurityDataResponse;
        private void OnSecurityDataResponse(Security security, EventFlag flag)
        {
            Log(new LogMessage(ToString(), $"Raising security data event on {security.Ticker} with FLAG [{Enum.GetName(typeof(EventFlag), flag)}]", LogMessageType.Production));
            SecurityDataResponse?.Invoke(this, new SecurityDataResponseEventArgs(security, flag));
        }

        public event EventHandler SecurityListLoaded;
        private void OnSecurityListLoaded()
        {
            SecurityListLoaded?.Invoke(this, new EventArgs());
        }

        #endregion

        // Data Objects
        public DataProvider DataProvider { get; private set; }
        public PriceDatabase PriceDatabase { get; private set; }

        // Stored reference to all security data in database, loaded every time database data is updated
        private List<Security> AllSecuritiesList { get; set; } = null;
        private bool ReloadSecuritiesList = true;

        // Current count of requests which are pending reply from the data provider
        public int PendingDataProviderRequestCount
        {
            get
            {
                return PendingRequestLog.Count;
            }
        }
        private Dictionary<Security, DateTime> PendingRequestLog = new Dictionary<Security, DateTime>();

        // Local-Server offset time for synchronization
        private TimeSpan LocalTimeOffsetFromServer { get; set; } = new TimeSpan(0);
        public DateTime ServerTime { get => DateTime.Now.Add(LocalTimeOffsetFromServer); }

        #region Indicator Controls

        public StatusLabelControlManager statusIndicatorControlManager { get; protected set; }
        public Control StatusIndicator
        {
            get
            {
                return statusIndicatorControlManager.IssueControl();
            }
        }
        private void SetStatusIndicator(ProcessStatus processStatus)
        {
            switch (processStatus)
            {
                case ProcessStatus.ErrorState:
                    statusIndicatorControlManager.SetStatus("Error State", System.Drawing.Color.PaleVioletRed);
                    break;
                case ProcessStatus.Ready:
                    {
                        if (DataProvider.Connected && PriceDatabase != null)
                            statusIndicatorControlManager.SetStatus("Ready", System.Drawing.Color.LightGreen);
                        else if (DataProvider.Connected && PriceDatabase == null)
                            statusIndicatorControlManager.SetStatus("Database Offline", System.Drawing.Color.PaleVioletRed);
                        else if (!DataProvider.Connected)
                            statusIndicatorControlManager.SetStatus("Data Provider Offline", System.Drawing.Color.PaleVioletRed);
                    }
                    break;
                case ProcessStatus.Working:
                    {
                        statusIndicatorControlManager.SetStatus($"Working", System.Drawing.Color.Orange);
                    }
                    break;
                case ProcessStatus.Offline:
                    statusIndicatorControlManager.SetStatus("Offline", System.Drawing.Color.Yellow);
                    break;
                default:
                    break;
            }
        }

        public SecurityListControlManager securityListControlManager { get; protected set; }
        public Control SecurityListBox
        {
            get
            {
                return securityListControlManager.IssueControl();
            }
        }
        private void UpdateListBoxControls()
        {
            securityListControlManager.UpdateLists();
        }

        #endregion

        public DataManager(DataProviderType providerType, int port = 0, string databaseConnectionString = "")
        {
            if (!_InitializeDatabaseConnection())
                throw new DataproviderConnectionException() { message = "Cannot connect to price database" };

            if (!_InitializeDataProvider(providerType, port))
                throw new DataproviderConnectionException() { message = "Cannot initialize data provider" };

            Log(new LogMessage(ToString(), "DataManager initialized", LogMessageType.Production));

            statusIndicatorControlManager = new StatusLabelControlManager("DataManager");
            securityListControlManager = new SecurityListControlManager(this, "DataManager");

            this.InitializeMe();

            SetStatusIndicator(ProcessStatus.Offline);
        }
        private bool _InitializeDatabaseConnection(string connectionString = "")
        {
            PriceDatabase = new PriceDatabase();
            return true;
        }
        private bool _InitializeDataProvider(DataProviderType providerType, int port = 0)
        {
            switch (providerType)
            {
                case DataProviderType.InteractiveBrokers:
                    {
                        if (port == 0)
                            throw new DataproviderConnectionException() { message = "IBKR Connection requires port specifier" };
                        DataProvider = new IbkrDataProvider(port);
                    }
                    break;
                default:
                    throw new DataproviderConnectionException() { message = "No data provider specified" };
            }

            // Assign event handlers
            DataProvider.OnSecurityDataResponse += DataProviderCallback;
            DataProvider.OnConnectionStatusChanged += (s, e) =>
            {
                OnDataProviderConnectionStatusChanged();
            };

            return true;
        }

        public void ConnectDataProvider(int timeoutSecs = 3)
        {
            SetStatusIndicator(ProcessStatus.Working);
            if (DataProvider == null)
                throw new DataproviderConnectionException() { message = "Data provider is not initialized" };

            Log(new LogMessage(ToString(), $"Attempting to connect to {DataProvider}", LogMessageType.Production));

            while (!DataProvider.Connected && timeoutSecs > 0)
            {
                DataProvider.Connect();
                Thread.Sleep(1000);
                timeoutSecs--;
            }

            if (!DataProvider.Connected)
            {
                Log(new LogMessage("DataManger", "Could not connect to Data Provider", LogMessageType.Error));
                OnDataProviderConnectionStatusChanged();
                return;
            }

            // Set local time offset interval if connected
            LocalTimeOffsetFromServer = DataProvider.ServerTimeOffset;

            SetStatusIndicator(ProcessStatus.Ready);
        }
        public void ResetDataProviderConnection()
        {
            CloseDataConnection();
            ConnectDataProvider();
        }
        public bool ProviderConnected
        {
            get
            {
                return DataProvider.Connected;
            }
        }
        public void CloseDataConnection()
        {
            DataProvider.Disconnect();
            SetStatusIndicator(ProcessStatus.Offline);
        }

        #region Database Calls

        public Security GetSecurity(string ticker, bool create = true)
        {
            return PriceDatabase.GetSecurity(ticker, create, false);
        }
        public void SetSecurity(Security security)
        {
            SetStatusIndicator(ProcessStatus.Working);
            ReloadSecuritiesList = true;
            PriceDatabase.SetSecurity(security, false);
            UpdateListBoxControls();
            SetStatusIndicator(ProcessStatus.Ready);
        }
        public void SetSecurities(List<Security> securities)
        {
            SetStatusIndicator(ProcessStatus.Working);
            Log(new LogMessage(ToString(), $"Sending {securities.Count} to database for saving", LogMessageType.Production));
            ReloadSecuritiesList = true;
            securities.ForEach(sec => PriceDatabase.SetSecurity(sec, false));
            UpdateListBoxControls();
            SetStatusIndicator(ProcessStatus.Ready);
        }
        public List<string> GetAllTickers()
        {
            return PriceDatabase.AllTickers;
        }
        public List<Security> GetAllSecurities()
        {
            if (ReloadSecuritiesList)
            {
                SetStatusIndicator(ProcessStatus.Working);
                Log(new LogMessage(ToString(), "Reload Security List [START]", LogMessageType.Debug));
                AllSecuritiesList = PriceDatabase.AllSecurities();
                Log(new LogMessage(ToString(), "Reload Security List [END]", LogMessageType.Debug));
                ReloadSecuritiesList = false;
                OnSecurityListLoaded();
                UpdateListBoxControls();
                SetStatusIndicator(ProcessStatus.Ready);
                return AllSecuritiesList;
            }
            else
                return AllSecuritiesList;
        }
        public void LoadSymbols(Dictionary<string, string> tickersAndNames)
        {
            bool _DEBUG_MODE = true;
            int _DEBUG_LIMIT = 50;

            Log(new LogMessage("DataManager", $"Loading {tickersAndNames.Count} Symbols", LogMessageType.Debug));

            List<Security> toSave = new List<Security>();

            foreach (var item in tickersAndNames)
            {
                if (_DEBUG_MODE && _DEBUG_LIMIT > 0)
                {
                    var sec = GetSecurity(item.Key, false);
                    if (sec == null)
                    {
                        sec = toSave.AddAndReturn(GetSecurity(item.Key, true));
                        sec.LongName = item.Value;
                        if (--_DEBUG_LIMIT == 0)
                            break;
                    }
                }
                else
                {
                    var sec = toSave.AddAndReturn(GetSecurity(item.Key, true));
                    sec.LongName = item.Value;
                }
            }

            if (toSave.Count > 0)
            {
                Log(new LogMessage("DataManager", $"Saving {toSave.Count} Symbols to DB", LogMessageType.Debug));
                SetSecurities(toSave);
                ReloadSecuritiesList = true;
                GetAllSecurities();
            }
        }

        #endregion
        #region Data Provider Calls

        System.Windows.Forms.Timer tmrRequestTimeout;
        TimeSpan tmsRequestTimeout = new TimeSpan(0, 5, 0);

        [Initializer]
        private void InitializeRequestTimeout()
        {
            //
            // Request Timeout timer (Disabled for now)
            //
            tmrRequestTimeout = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            tmrRequestTimeout.Tick += (s, e) =>
            {
                if (PendingDataProviderRequestCount > 0)
                    CancelStalledRequests();
            };
            tmrRequestTimeout.Enabled = false;
        }

        public void UpdateSecurity(Security security, DateTime UpdateTo, bool ForceUpdateAll = false)
        {
            PendingRequestLog.Add(security, DateTime.Now);

            if (ForceUpdateAll)
            {
                DataProvider.RequestPriceData(security, UpdateTo);
            }
            else if (security.PriceBarData.Count > 0)
            {
                Log(new LogMessage(ToString(), $"Request update to {security.Ticker} from {security.LastBar().BarDateTime.ToString("yyyyMMdd")} to {UpdateTo.ToString("yyyyMMdd")}", LogMessageType.Production));
                DataProvider.RequestPriceData(security, security.LastBar().BarDateTime, UpdateTo);
            }
            // Otherwise, send a requst for the earliest possible bar
            else
            {
                Log(new LogMessage(ToString(), $"Request update to {security.Ticker} from earliest available", LogMessageType.Production));
                DataProvider.RequestPriceData(security, UpdateTo);
            }
        }
        public void UpdateAll(DateTime UpdateTo)
        {
            new Task(() =>
            {
                int TestBreak = -1;

                SetStatusIndicator(ProcessStatus.Working);
                Log(new LogMessage(ToString(), $"***** Begin update all securities in database to {UpdateTo.ToString("yyyyMMdd")} *****", LogMessageType.Production));
                foreach (Security security in GetAllSecurities())
                {
                    if (security.DataUpToDate)
                        continue;

                    if (TestBreak-- == 0)
                        break;

                    PendingRequestLog.Add(security, DateTime.Now);

                    if (security.PriceBarData.Count > 0)
                        DataProvider.RequestPriceData(security, security.LastBar().BarDateTime, UpdateTo);
                    else
                        DataProvider.RequestPriceData(security, UpdateTo);
                }
            }).Start();
        }
        public void RequestContractData(List<Security> securities, bool All = false)
        {
            new Task(() =>
            {
                foreach (Security security in securities)
                {
                    // Skip security if we know exchange and 'All' option isn't selected
                    if (!All && security.Exchange != "UNK")
                        continue;

                    PendingRequestLog.Add(security, DateTime.Now);
                    DataProvider.RequestContractData(security);
                }
            }).Start();
        }
        public void CleanSymbols()
        {
            RequestContractData(AllSecuritiesList);
        }

        private void DataProviderCallback(object sender, SecurityDataResponseEventArgs e)
        {
            PendingRequestLog.Remove(e.security);
            switch (e.flag)
            {
                case EventFlag.NotSet:
                    {
                        // No problems
                        Log(new LogMessage(ToString(), $"Manager received callback data for {e.security.Ticker}: sending to database", LogMessageType.Production));
                        SetSecurity(e.security);
                        OnSecurityDataResponse(e.security, e.flag);
                    }
                    break;
                case EventFlag.BadSymbol:
                    {
                        // Delete symbol from database           
                        Log(new LogMessage(ToString(), $"Manager received callback data for {e.security.Ticker}: not a valid symbol, remove from database", LogMessageType.Production));
                        ReloadSecuritiesList = true;
                        PriceDatabase.RemoveSecurity(e.security);
                        OnSecurityDataResponse(e.security, e.flag);
                    }
                    break;
                case EventFlag.RequestError:
                    {
                        // Error
                        Log(new LogMessage(ToString(), $"Manager received callback data for {e.security.Ticker}: request error", LogMessageType.Production));
                    }
                    break;
                default:
                    break;
            }

            if (PendingDataProviderRequestCount == 0)
            {
                ReloadSecuritiesList = true;
                GetAllSecurities();
            }
            else
                SetStatusIndicator(ProcessStatus.Working);
        }
        private void CancelStalledRequests()
        {
            foreach (var request in PendingRequestLog.Where(x => (DateTime.Now - x.Value) > tmsRequestTimeout).ToList())
            {
                PendingRequestLog.Remove(request.Key);
                DataProvider.CancelRequest(request.Key);
                Log(new LogMessage("DataManager", $"Request for {request.Key.Ticker} timed out and was cancelled", LogMessageType.Error));
            }
        }

        #endregion

    }
}
