using Finance;
using Finance.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance
{

    public partial class MasterController
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

        System.Timers.Timer tmrSystemActions;

        TimeSpan dailyUpdateTime = new TimeSpan(20, 15, 0);

        private StatusLabelControlManager DailyUpdateIndicatorControlManager { get; } = new StatusLabelControlManager("MasterController");
        public Control DailyUpdateStatusIndicator
        {
            get
            {
                return DailyUpdateIndicatorControlManager.IssueControl();
            }
        }
        private void SetDailyUpdateIndicatorStatus()
        {
            TimeSpan timeSpan = dailyUpdateTime - DateTime.Now.TimeOfDay;

            Color col = SystemColors.Control;
            if (timeSpan.TotalMinutes < 90)
                col = Color.Yellow;
            if (timeSpan.TotalMinutes < 30)
                col = Color.Orange;

            if (timeSpan.Ticks > 0)
                DailyUpdateIndicatorControlManager.SetStatus($"System Update in {timeSpan.ToString(@"hh\:mm\:ss")}", col);
        }

        [Initializer]
        private void InitializeSystemTimer()
        {
            tmrSystemActions = new System.Timers.Timer();

            tmrSystemActions.Interval = 1000;

            //
            // System Actions
            //
            tmrSystemActions.Elapsed += (s,e) => ExecuteSystemEvents(DateTime.Now);
            
            tmrSystemActions.Start();
        }

        private void ExecuteSystemEvents(DateTime eventTime)
        {



        }

        #endregion

        #region System Event -> Daily Update
        
        private void DailyDataUpdate()
        {
            //
            // Data update timer (fires once a day)
            //

            Log(new LogMessage("DailyUpdateTimer", "Executing nightly security data update...", LogMessageType.Production));

            if (!DataManager.ProviderConnected)
            {
                Log(new LogMessage(ToString() + ".UpdateTimer", $"Could not execute daily update at {DateTime.Now.ToString("hh:mm:ss.fff")}", LogMessageType.Error));

                // Reset interval an try again in a minute              
                tmrDataUpdateTimer.Interval = (new TimeSpan(0, 1, 0)).TotalMilliseconds;
                DataManager.ConnectDataProvider();
                return;
            }

            // Reset interval for another 24 hours and execute actions
            tmrDataUpdateTimer.Interval = (new TimeSpan(24, 0, 0)).TotalMilliseconds;
            Log(new LogMessage(ToString(), "Sending request for daily data update", LogMessageType.Production));

            DataManager.UpdateAll(DateTime.Today);
        }

        #endregion

    }

}
