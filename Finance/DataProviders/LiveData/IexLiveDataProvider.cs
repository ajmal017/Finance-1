using System;
using System.Collections.Generic;
using System.Threading;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance.Data
{
    public class IexLiveDataProvider : LiveDataProvider
    {
        public override string Name => "IEX Cloud Live Data";
        private IexClient iexClient;

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
        public override void RequestSnapshotQuotes(Security security)
        {
            throw new NotImplementedException();
        }
        public override void RequestStreamingQuotes(Security security)
        {
            throw new NotImplementedException();
        }
        public override void CancelStreamingQuotes(Security security = null)
        {
            throw new NotImplementedException();
        }

        private string MessageCountStatusString()
        {
            decimal percentUsed = Settings.Instance.IexMessageCount.ToDecimal() / Settings.Instance.IexMessageCountLimit.ToDecimal();
            return string.Format($@"IEX Msgs used: {Settings.Instance.IexMessageCount:###,###,##0} / {Settings.Instance.IexMessageCountLimit:###,###,###} ({percentUsed * 100:0.00}%)");
        }
    }
}
