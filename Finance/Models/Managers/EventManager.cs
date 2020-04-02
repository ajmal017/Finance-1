using Finance.Data;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance
{

    public class EventManager
    {
        private static EventManager _Instance { get; set; }
        public static EventManager Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new EventManager();
                return _Instance;
            }
        }

        private EventManager() { }

        public void Initialize()
        {
            this.InitializeMe();
        }

        System.Timers.Timer tmrSystemEventTimer;
        List<SystemEventAction> SystemEvents { get; set; }

        [Initializer]
        private void InitializeSystemTimer()
        {
            tmrSystemEventTimer = new System.Timers.Timer();
            SystemEvents = new List<SystemEventAction>();

            tmrSystemEventTimer.Interval = 1000;
            tmrSystemEventTimer.Elapsed += (s, e) =>
            {
                ExecuteSystemEvents(DateTime.Now);
            };

            tmrSystemEventTimer.Start();
        }
        [Initializer]
        private void SetSystemEvents()
        {
            foreach (MethodInfo method in typeof(SystemEvents).GetMethods())
            {
                if (!Attribute.IsDefined(method, typeof(SystemEventActionAttribute)))
                    continue;

                var attr = method.GetCustomAttribute<SystemEventActionAttribute>();
                Action methodAction = (Action)method.CreateDelegate(typeof(Action), null);
                SystemEvents.Add(new SystemEventAction(attr.DisplayName, attr.ExecutionTime, methodAction));
            }
        }

        private void ExecuteSystemEvents(DateTime eventTime)
        {
            SystemEvents.Where(x =>
                x.ExecutionTime.Hours == eventTime.TimeOfDay.Hours &&
                x.ExecutionTime.Minutes == eventTime.TimeOfDay.Minutes &&
                x.ExecutionTime.Seconds == eventTime.TimeOfDay.Seconds)
                .ToList().ForEach(x => x.TryExecute());
        }
        public void AddSystemEvent(SystemEventAction systemEventAction)
        {
            if (SystemEvents.Exists(x => x.EventName == systemEventAction.EventName))
                throw new TradingSystemException() { message = $"Event '{systemEventAction.EventName}' already exists" };

            SystemEvents.Add(systemEventAction);
        }
        public void RemoveSystemEvent(string Name)
        {
            SystemEvents.RemoveAll(x => x.EventName == Name);
        }
        public SystemEventAction NextScheduledEvent()
        {
            if (SystemEvents.Count == 0)
                return null;

            return SystemEvents.Where(x => x.ExecutionTime > DateTime.Now.TimeOfDay).FirstOrDefault() ?? SystemEvents.First();
        }

    }

    public static class SystemEvents
    {
        /*
         *  Please all system event actions here
         */

        [SystemEventAction("Daily Security Update", "DailySecurityUpdateTime")]
        public static void DailyDataUpdate()
        {
            //
            // Data update timer (fires once a day)
            //

            Log(new LogMessage("Daily Data Update", "Executing daily security price data update", LogMessageType.Production));

            if (!RefDataManager.Instance.ProviderConnected)
            {
                RefDataManager.Instance.ResetDataProviderConnection();
                Thread.Sleep(5000);
                Log(new LogMessage("System Event: DailyDataUpdate", $"Could not execute daily update at {DateTime.Now.ToString("hh:mm:ss.fff")}", LogMessageType.SystemError));
                return;
            }

            RefDataManager.Instance.UpdateAllPriceData(DateTime.Today);
        }
        [SystemEventAction("Daily Index Update", "DailyIndexUpdateTime")]
        public static void DailyIndexUpdate()
        {
            //
            // Index update timer (fires once a day)
            //

            Log(new LogMessage("Daily Index Update", "Executing daily index recalculation", LogMessageType.Production));

            if (!RefDataManager.Instance.ProviderConnected)
            {
                RefDataManager.Instance.ResetDataProviderConnection();
                Thread.Sleep(5000);
                Log(new LogMessage("System Event: DailyDataUpdate", $"Could not execute daily update at {DateTime.Now.ToString("hh:mm:ss.fff")}", LogMessageType.SystemError));
                return;
            }

            IndexManager.Instance.UpdateAllIndices();
        }
        [SystemEventAction("IBKR Gateway Reset", "DailyGatewayReconnect")]
        public static void DailyGatewayReconnect()
        {
            //
            // Reconnect to IBKR Gateway on daly reset
            //
            if (Settings.Instance.RefDataProvider != DataProviderType.InteractiveBrokers)
                return;

            Log(new LogMessage("Gateway Reconnect", "Executing IBKR Gateway Reconnect", LogMessageType.Production));

            RefDataManager.Instance.ResetDataProviderConnection(30000);
        }
    }
}
