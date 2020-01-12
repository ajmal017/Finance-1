using Finance.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    public abstract partial class TradeStrategyBase
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public List<Signal> GenerateSignals(List<Security> SecurityList, DateTime AsOf)
        {
            ConcurrentBag<Signal> ret = new ConcurrentBag<Signal>();

            Parallel.ForEach(SecurityList, sec =>
             {
                 if (!(sec.GetPriceBar(AsOf, false) == null))
                 {
                     var signal = GenerateSignal(sec, AsOf);
                     if (signal != null)
                         ret.Add(signal);
                 }
             });

            return ret.ToList();
        }

        protected abstract Signal GenerateSignal(Security security, DateTime AsOf);
        public abstract TradeStrategyBase Copy();
    }
}
