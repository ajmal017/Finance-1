using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance;

namespace Finance
{
    public class Signal
    {
        public Security Security { get; set; }
        public PriceBarSize SignalBarSize { get; set; }
        public DateTime SignalDate { get; set; }
        public SignalAction SignalAction { get; set; }

        // Score between 0.0 and 1.0 to help with prioritization
        public double SignalStrength { get; set; } = 0.0;

        public Signal(Security security, PriceBarSize barsize, DateTime signalDate, SignalAction signalAction, double signalStrength = 0)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            SignalBarSize = barsize;
            SignalDate = signalDate;
            SignalAction = signalAction;
            SignalStrength = signalStrength;
        }
    }
}
