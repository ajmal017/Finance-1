using Finance.Data;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance
{
    public class MasterController
    {
        public DataManager DataManager { get; private set; }
        public SimulationManager SimulationManager { get; private set; }
        public IEnvironment Environment { get; private set; }

        public bool Initialized { get; private set; } = false;

        #region Events

        /// <summary>
        /// Raised whenever the status of the data manager changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void DataManagerStatusChangeEventHandler(object sender, DataManagerStatusChangeEventArgs e);
        public event DataManagerStatusChangeEventHandler DataManagerStatusChange;
        private void OnDataManagerStatusChange()
        {
            DataManagerStatusChange?.Invoke(this, new DataManagerStatusChangeEventArgs() { DataproviderConnected = DataManager.ProviderConnected, DatabaseConnected = true });
        }

        #endregion

        public MasterController()
        {
            InitializeDataManager(DataProviderType.InteractiveBrokers, 4002);
            SimulationManager = new SimulationManager();

            this.InitializeMe();

            Initialized = true;
        }

        #region Data Manager

        public void InitializeDataManager(DataProviderType providerType, int port = 0, string databaseConnectionString = "")
        {
            if (Initialized)
                return;

            DataManager = new DataManager(providerType, port, databaseConnectionString);
            DataManager.DataProviderConnectionStatusChanged += (s, e) =>
            {
                OnDataManagerStatusChange();
            };

            // Load securities in background
            new Thread(() => { DataManager.GetAllSecurities(); }).Start();

            Initialized = true;
        }
        public void ConnectDataProvider()
        {
            DataManager.ConnectDataProvider(3);
        }
        public void ReconnectDataProvider()
        {
            DataManager.ResetDataProviderConnection();
        }

        #endregion
        #region Event Timer

        #region Initialize Timer

        System.Timers.Timer tmrSystemEventTimer;
        List<SystemEventAction> systemEvents;

        private StatusLabelControlManager SystemEventIndicatorManager { get; } = new StatusLabelControlManager("System");
        public Control SystemEventStatusIndicator
        {
            get
            {
                return SystemEventIndicatorManager.IssueControl();
            }
        }
        private void setSystemEventStatusIndicator()
        {
            SystemEventAction nextEvent = NextScheduledEvent();
            SystemEventIndicatorManager.SetStatus($"Next System Event: {nextEvent.EventName} @ {nextEvent.ExecutionTime.ToString(@"hh\:mm\:ss")}", SystemColors.Control, false);
        }

        [Initializer]
        private void InitializeSystemTimer()
        {
            tmrSystemEventTimer = new System.Timers.Timer();
            systemEvents = new List<SystemEventAction>();

            tmrSystemEventTimer.Interval = 1000;
            tmrSystemEventTimer.Elapsed += (s, e) =>
            {
                ExecuteSystemEvents(DateTime.Now);
                setSystemEventStatusIndicator();
            };

            tmrSystemEventTimer.Start();
        }
        [Initializer]
        private void SetSystemEvents()
        {
            //
            // Add all system events manually until we create a UI for managing.  Add sorted by time for ease.
            //

            systemEvents.Add(new SystemEventAction("IBKR Gateway Reset", new TimeSpan(03, 0, 0), new Action(IbkrGatewayRestart)));
            systemEvents.Add(new SystemEventAction("Daily Update", new TimeSpan(20, 15, 0), new Action(DailyDataUpdate)));

        }

        private void ExecuteSystemEvents(DateTime eventTime)
        {
            systemEvents.Where(x =>
                x.ExecutionTime.Hours == eventTime.TimeOfDay.Hours &&
                x.ExecutionTime.Minutes == eventTime.TimeOfDay.Minutes &&
                x.ExecutionTime.Seconds == eventTime.TimeOfDay.Seconds)
                .ToList().ForEach(x => x.TryExecute());
        }
        public void AddSystemEvent(SystemEventAction systemEventAction)
        {
            if (systemEvents.Exists(x => x.EventName == systemEventAction.EventName))
                throw new TradingSystemException() { message = $"Event '{systemEventAction.EventName}' alreasy exists" };

            systemEvents.Add(systemEventAction);
        }
        public void RemoveSystemEvent(string Name)
        {
            systemEvents.RemoveAll(x => x.EventName == Name);
        }
        public SystemEventAction NextScheduledEvent()
        {
            return systemEvents.Where(x => x.ExecutionTime > DateTime.Now.TimeOfDay).FirstOrDefault() ?? systemEvents.First();
        }

        #endregion

        private void DailyDataUpdate()
        {
            //
            // Data update timer (fires once a day)
            //

            Log(new LogMessage("Daily Data Update", "Executing daily security price data update...", LogMessageType.Production));

            if (!DataManager.ProviderConnected)
            {
                DataManager.ResetDataProviderConnection();
                Thread.Sleep(5000);
                Log(new LogMessage(ToString() + ".UpdateTimer", $"Could not execute daily update at {DateTime.Now.ToString("hh:mm:ss.fff")}", LogMessageType.Error));
                return;
            }

            DataManager.UpdateAll(DateTime.Today);
        }
        private void IbkrGatewayRestart()
        {
            // IBKR Gateway will restart every morning at 05:00 Local.
            // Client will not detect logoff or reconnect automatically, 
            // so we need to terminate the connection and reconnect like 30 seconds later

            DataManager.CloseDataConnection();
            Thread.Sleep(30000);
            DataManager.ConnectDataProvider(5);
        }

        #endregion



    }
}
