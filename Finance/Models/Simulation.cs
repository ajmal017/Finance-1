using Finance.Data;
using System;
using System.Collections.Generic;
using static Finance.Logger;

namespace Finance
{
    public partial class Simulation
    {

        #region Events

        public event SimulationStatusEventHandler SimulationStatusChanged;
        private void OnSimulationStatusChanged(Simulation simulation)
        {
            SimulationStatusChanged?.Invoke(this, new SimulationStatusEventArgs(simulation));
        }

        #endregion

        public string Name { get; set; }

        public PortfolioManager PortfolioManager { get; }
        public PortfolioSetup PortfolioSetup { get; }
        public StrategyManager StrategyManager { get; }
        public RiskManager RiskManager { get; }
        private IEnvironment Environment { get; }
        private DataManager DataManager { get; }

        public Tuple<DateTime, DateTime> SimulationTimeSpan { get; private set; }

        private SimulationStatus _SimulationStatus = SimulationStatus.NotStarted;
        public SimulationStatus SimulationStatus
        {
            get => _SimulationStatus;
            set
            {
                _SimulationStatus = value;
                OnSimulationStatusChanged(this);
            }
        }

        public bool Complete { get; private set; } = false;
        public SimulationResults Results { get; private set; }

        public Simulation(
            IEnvironment environment,
            DataManager dataManager,
            PortfolioSetup portfolioSetup,
            StrategyManager strategyManager,
            RiskManager riskManager,
            string name)
        {
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            DataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            PortfolioSetup = portfolioSetup ?? throw new ArgumentNullException(nameof(portfolioSetup));

            Name = name;

            StrategyManager = strategyManager.Copy();
            RiskManager = riskManager.Copy();

            PortfolioManager = new PortfolioManager(Environment, PortfolioSetup, DataManager, StrategyManager, RiskManager, Name);
        }

        public bool Run(DateTime startDate, DateTime endDate)
        {
            Log(new LogMessage(Name, $"Beginning Simulation  '{Name}'", LogMessageType.Production));
            SimulationStatus = SimulationStatus.Running;

            try
            {
                SimulationTimeSpan = new Tuple<DateTime, DateTime>(startDate, endDate);
                DateTime currentDate = Calendar.PriorTradingDay(startDate);
                PortfolioManager.SetStartDate(currentDate);

                //
                // Execute simulation loop
                //
                while (currentDate < endDate)
                {
                    PortfolioManager.ExecuteNextDay();
                    currentDate = PortfolioManager.CurrentSimulationDate;
                }

                Complete = true;
                Results = new SimulationResults(PortfolioManager.Portfolio, SimulationTimeSpan);
                SimulationStatus = SimulationStatus.Complete;
                return Complete;
            }
            catch (TradingSystemException ex)
            {
                Log(new LogMessage(Name, ex.message, LogMessageType.Error));
                return false;
            }
        }
        public Simulation Copy(string NewName)
        {
            var ret = new Simulation(
                Environment,
                DataManager,
                PortfolioSetup.Copy(),
                StrategyManager.Copy(),
                RiskManager.Copy(),
                NewName);

            return ret;
        }
    }
    public partial class Simulation : IEquatable<Simulation>
    {
        public override bool Equals(object obj)
        {
            return Equals(obj as Simulation);
        }
        public bool Equals(Simulation other)
        {
            return other != null &&
                   Name == other.Name;
        }
        public override int GetHashCode()
        {
            return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
        }
        public static bool operator ==(Simulation simulation1, Simulation simulation2)
        {
            return EqualityComparer<Simulation>.Default.Equals(simulation1, simulation2);
        }
        public static bool operator !=(Simulation simulation1, Simulation simulation2)
        {
            return !(simulation1 == simulation2);
        }
    }
}
