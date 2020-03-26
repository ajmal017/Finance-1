using Finance;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Helpers;
using Finance.TradeStrategies;

namespace Finance
{
    /// <summary>
    /// Maintains one or more TradeStrategy objects.  Filters and applies to price data to generate trade entry signals
    /// </summary>
    public class StrategyManager
    {
        #region Events

        public event EventHandler StrategyChanged;
        private void OnStrategyChanged()
        {
            StrategyChanged?.Invoke(this, new EventArgs());
        }

        #endregion

        public TradeStrategyBase ActiveTradeStrategy { get; private set; }
        public List<TradeStrategyBase> AllTradeStrategies { get; private set; }
        public void SetStrategy(TradeStrategyBase tradeStrategy)
        {
            // Set by name to ensure that the ActiveStrategy is referencing local object

            ActiveTradeStrategy = AllTradeStrategies.Find(x => x.Name == tradeStrategy.Name);
            OnStrategyChanged();
        }

        private StrategyManager()
        {
        }
        public static StrategyManager Default()
        {
            var ret = new StrategyManager();
            ret.AllTradeStrategies = Helpers.AllTradeStrategies();
            ret.SetStrategy(ret.AllTradeStrategies.FirstOrDefault());
            return ret;
        }
        public StrategyManager Copy()
        {
            var ret = new StrategyManager();
 
            ret.AllTradeStrategies = new List<TradeStrategyBase>();
            foreach(var strategy in this.AllTradeStrategies)
            {
                ret.AllTradeStrategies.Add(strategy.Copy());
            }
            ret.SetStrategy(this.ActiveTradeStrategy);

            return ret;
        }

        public List<Signal> GenerateSignals(List<Security> securities, DateTime AsOf)
        {
            var ret = ActiveTradeStrategy.GenerateSignals(securities, AsOf);
            SignalHistory.AddRange(ret);
            return ret;
        }

        private List<Signal> SignalHistory { get; } = new List<Signal>();
        public List<Signal> GetSignalHistory(Security security)
        {
            return (from sig in SignalHistory where sig.Security == security select sig).ToList();
        }
        public List<Signal> GetSignalHistory()
        {
            return (from sig in SignalHistory select sig).ToList();
        }

    }
}
