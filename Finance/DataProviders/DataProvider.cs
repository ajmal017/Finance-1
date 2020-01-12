using System;
using System.Windows.Forms;

namespace Finance.Data
{

    public abstract class DataProvider 
    {
        public string Name { get; set; }

        // Connection
        protected bool _Connected;
        public bool Connected { get => _Connected; set => _Connected = value; }

        public abstract void Connect();
        public abstract void Disconnect();

        public abstract TimeSpan ServerTimeOffset { get; }

        // Data callback event
        public event SecurityResponseDataHandler OnSecurityDataResponse;
        public void SecurityDataResponse(Security security, EventFlag flag)
        {
            OnSecurityDataResponse?.Invoke(this, new SecurityDataResponseEventArgs(security, flag));
        }

        // Connection Status Change event
        public event EventHandler OnConnectionStatusChanged;
        public void ConnectionStatusChanged()
        {
            OnConnectionStatusChanged?.Invoke(this, null);
        }

        // Status Indicator
        public StatusLabelControlManager statusIndicatorControlManager { get; protected set; }
        public Control StatusIndicator
        {
            get
            {
                return statusIndicatorControlManager.IssueControl();
            }
        }
        protected abstract void SetStatusIndicator(ProcessStatus processStatus);

        // Request ID and Queue
        private int _DataRequestId = 0;
        protected int NextDataRequestId
        {
            get
            {
                return ++_DataRequestId;
            }
        }

        // Sets an earliest date limit for request data to avoid long-running requests that generate too much data
        public DateTime EarlyDateRequestLimit { get; set; } = new DateTime(2000, 1, 1);

        // Request Methods
        public abstract void RequestPriceData(Security security, DateTime startDate, DateTime endDate);
        public abstract void RequestPriceData(Security security, DateTime endDate);
        public abstract void RequestContractData(Security security);
        public abstract void CancelRequest(Security security);
        public abstract void CancelAllRequests();

    }

}
