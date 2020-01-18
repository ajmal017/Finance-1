using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance;

namespace Finance
{
    public partial class Signal
    {
        public Security Security { get; set; }
        public DateTime SignalDate { get; set; }
        public TradeActionBuySell SignalAction { get; set; }

        public Signal(Security security, DateTime signalDate, TradeActionBuySell signalAction)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            SignalDate = signalDate;
            SignalAction = signalAction;
        }
    }
}
