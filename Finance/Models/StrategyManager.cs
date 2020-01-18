using Finance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    /// <summary>
    /// Maintains one or more TradeStrategy objects.  Filters and applies to price data to generate trade entry signals
    /// </summary>
    public partial class StrategyManager
    {
        #region Events

        public event EventHandler StrategyChanged;
        private void OnStrategyChanged()
        {
            StrategyChanged?.Invoke(this, new EventArgs());
        }

        #endregion

        [TradeStrategyFilter("Minimum Share Price", "Lower bound of share prices considered in strategy")]
        public decimal Minimum_Security_Price { get; set; } = 0.00m;

        [TradeStrategyFilter("Maximum Share Price", "Upper bound of share prices considered in strategy")]
        public decimal Maximum_Security_Price { get; set; } = 9999.00m;

        public TradeStrategyBase ActiveTradeStrategy { get; set; }
        public List<TradeStrategyBase> AllTradeStrategies { get; set; }
        public void SetStrategy(TradeStrategyBase tradeStrategy)
        {
            ActiveTradeStrategy = tradeStrategy;
            OnStrategyChanged();
        }

        public StrategyManager()
        {
            AllTradeStrategies = Helpers.AllTradeStrategies();
            SetStrategy(AllTradeStrategies.FirstOrDefault());
        }
        public static StrategyManager Default()
        {
            return new StrategyManager();
        }
        public StrategyManager Copy()
        {
            var ret = new StrategyManager()
            {
                Minimum_Security_Price = Minimum_Security_Price,
                Maximum_Security_Price = Maximum_Security_Price,
                AllTradeStrategies = AllTradeStrategies,
                ActiveTradeStrategy = ActiveTradeStrategy
            };

            return ret;
        }
        public List<Signal> GenerateSignals(List<Security> Securities, DateTime AsOf)
        {
            var ret = ActiveTradeStrategy.GenerateSignals(Securities, AsOf);
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
