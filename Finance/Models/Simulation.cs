using Finance.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static Finance.Logger;

namespace Finance
{
    public partial class Simulation : INotifyPropertyChanged
    {
        #region Events

        public event SimulationStatusEventHandler SimulationStatusChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnSimulationStatusChanged(Simulation simulation)
        {
            SimulationStatusChanged?.Invoke(this, new SimulationStatusEventArgs(simulation));
        }
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public string Name { get; set; }

        public PortfolioManager PortfolioManager { get; }
        public PortfolioSetup PortfolioSetup { get; }
        public StrategyManager StrategyManager { get; }
        public RiskManager RiskManager { get; }

        public SimulationSettings Settings { get; private set; }

        public (DateTime start, DateTime end) SimulationTimeSpan
        {
            get
            {
                return (Settings.SimulationStartDate, Settings.SimulationEndDate);
            }
        }

        private SimulationStatus _SimulationStatus = SimulationStatus.NotStarted;
        public SimulationStatus SimulationStatus
        {
            get => _SimulationStatus;
            set
            {
                _SimulationStatus = value;
                OnSimulationStatusChanged(this);
                NotifyPropertyChanged();
            }
        }

        public bool Complete { get; private set; } = false;
        public SimulationResults Results
        {
            get
            {
                if (Complete)
                    return new SimulationResults(PortfolioManager.Portfolio, SimulationTimeSpan);
                else
                    return null;
            }
        }

        public string SimulationNotes { get; set; } = string.Empty;

        public Simulation(PortfolioSetup portfolioSetup, StrategyManager strategyManager, RiskManager riskManager, string name, SimulationSettings settings = null)
        {
            Name = name;
            PortfolioSetup = portfolioSetup;
            StrategyManager = strategyManager;
            RiskManager = riskManager;

            Settings = settings ?? new SimulationSettings(this);
            if (settings != null)
                Settings.Simulation = this;

            PortfolioManager = new PortfolioManager(PortfolioSetup, StrategyManager, RiskManager, Name);
        }

        public bool Run()
        {
            SimulationStatus = SimulationStatus.Running;
            Log(new LogMessage(Name, $"Beginning Simulation  '{Name}'", LogMessageType.Production));

            try
            {
                DateTime currentDate = SimulationTimeSpan.start;
                PortfolioManager.SetStartDate(Calendar.PriorTradingDay(SimulationTimeSpan.start));

                //
                // Execute simulation loop
                //
                while (currentDate < SimulationTimeSpan.end)
                {
                    PortfolioManager.ExecuteNextDay();
                    currentDate = PortfolioManager.CurrentSimulationDate;
                }

                Complete = true;
                SimulationStatus = SimulationStatus.Complete;

                return Complete;
            }
            catch (TradingSystemException ex)
            {
                Log(new LogMessage(Name, $"{ex.GetType()} {ex.message}", LogMessageType.SystemError));
                SimulationStatus = SimulationStatus.Error;
                return false;
            }
        }
        public Simulation Copy(string NewName)
        {
            var ret = new Simulation(
                PortfolioSetup.Copy(),
                StrategyManager.Copy(),
                RiskManager.Copy(),
                NewName,
                this.Settings.Copy());

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
            return 53900726 + EqualityComparer<string>.Default.GetHashCode(Name);
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
