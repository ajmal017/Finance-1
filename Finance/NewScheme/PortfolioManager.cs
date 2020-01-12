using Finance.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    public partial class PortfolioManager
    {
        // Created internally
        public Portfolio Portfolio { get; }
        public RiskManagerBase RiskManager { get; }
        public TradeManager TradeManager { get; }

        // Provided by initializing member
        public IEnvironment Environment { get; }
        public PortfolioSetup Setup { get; }
        public DataManager DataManager { get; }
        public StrategyManager StrategyManager { get; }

        public List<Security> SecurityUniverse { get; set; }

        /// <summary>
        /// Initializes a new portfolio manager able to execute a time simulation
        /// </summary>
        /// <param name="environment">Provided trading environment</param>
        /// <param name="setup">Initial portfolio setup parameters</param>
        /// <param name="dataManager">Initialized and connected Data Manager</param>
        /// <param name="strategy">Strategy Manager loaded with one or more strategies</param>
        public PortfolioManager(IEnvironment environment, PortfolioSetup setup, DataManager dataManager, StrategyManager strategy, RiskManager riskManager, string Name = "Default Portfolio")
        {
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            DataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            StrategyManager = strategy ?? throw new ArgumentNullException(nameof(strategy));

            Portfolio = new Portfolio(environment, setup, Name);
            TradeManager = new TradeManager(Portfolio);

            RiskManager = riskManager;
            RiskManager.Attach(Portfolio, TradeManager);

            SecurityUniverse = (from sec in DataManager.GetAllSecurities() where sec.DataUpToDate select sec).ToList();

            CurrentDate = setup.InceptionDate;
        }

        public void SetStartDate(DateTime date)
        {
            CurrentDate = date;
            Setup.InceptionDate = date;
            Portfolio.SetInceptionDate(date);
        }

        public DateTime CurrentDate { get; set; }

        public void ExecuteNextDay()
        {
            // Increment the date
            CurrentDate = Calendar.NextTradingDay(CurrentDate);

            //
            // Market Open
            //

            // Process morning trades
            TradeManager.ProcessTradeQueue(CurrentDate, TimeOfDay.MarketOpen);
            
            //
            // Market Close
            //

            // Process end of day trades & stops
            TradeManager.ProcessTradeQueue(CurrentDate, TimeOfDay.MarketEndOfDay);

            // Update stoplosses
            RiskManager.UpdateStoplosses(CurrentDate);

            // Scale open positions
            RiskManager.ScalePositions(CurrentDate);

            // Generate new signals
            var signals = StrategyManager.GenerateSignals(SecurityUniverse, CurrentDate);

            // Send signals for processing
            RiskManager.ProcessSignals(signals, CurrentDate);

            // End of Day (EOD)
        }

    }

}
