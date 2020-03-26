using Finance.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Finance.TradeStrategies;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    public class PortfolioManager
    {
        // Created internally
        public Portfolio Portfolio { get; }
        public RiskManager RiskManager { get; }
        public TradeManager TradeManager { get; }

        // Provided by initializing member
        public PortfolioSetup Setup { get; }
        public StrategyManager StrategyManager { get; }

        public DateTime CurrentSimulationDate { get; private set; }
        public List<Security> SecurityUniverse { get; }

        /// <summary>
        /// Initializes a new portfolio manager able to execute a time simulation
        /// </summary>
        /// <param name="environment">Provided trading environment</param>
        /// <param name="setup">Initial portfolio setup parameters</param>
        /// <param name="dataManager">Initialized and connected Data Manager</param>
        /// <param name="strategy">Strategy Manager loaded with one or more strategies</param>
        public PortfolioManager(PortfolioSetup setup, StrategyManager strategy, RiskManager riskManager, string Name = "Default Portfolio")
        {
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            StrategyManager = strategy ?? throw new ArgumentNullException(nameof(strategy));

            Portfolio = new Portfolio(setup, Name);
            TradeManager = new TradeManager(Portfolio);

            RiskManager = riskManager;
            RiskManager.Attach(Portfolio, TradeManager);

            //
            // TODO: Implement adjustable security filters
            //

            //SecurityUniverse = (from sec in DataManager.GetAllSecurities() where sec.DataUpToDate select sec).ToList();
            SecurityUniverse = RefDataManager.Instance.GetAllSecurities().Where(x => !x.Excluded).ToList();

            CurrentSimulationDate = setup.InceptionDate;
        }

        public void SetStartDate(DateTime date)
        {
            CurrentSimulationDate = date;
            Setup.InceptionDate = date;
            Portfolio.SetInceptionDate(date);
        }
        public void ExecuteNextDay()
        {
            // Increment the date
            CurrentSimulationDate = Calendar.NextTradingDay(CurrentSimulationDate);

            //
            // Market Open
            //

            // Process morning trades
            TradeManager.ProcessTradeQueue(CurrentSimulationDate, TimeOfDay.MarketOpen);

            //
            // Market Close
            //

            // Process end of day trades & stops
            TradeManager.ProcessTradeQueue(CurrentSimulationDate, TimeOfDay.MarketEndOfDay);

            // Update stoplosses
            RiskManager.UpdateStoplosses(CurrentSimulationDate);

            // Scale open positions
            //RiskManager.ScalePositions(CurrentSimulationDate);

            // Generate new signals
            var securityUniverse = RiskManager.GetSecurityUniverse();
            var signals = StrategyManager.GenerateSignals(securityUniverse, CurrentSimulationDate);

            // Send signals for processing
            RiskManager.ProcessSignals(signals, CurrentSimulationDate);

            // End of Day (EOD)
        }

    }

}
