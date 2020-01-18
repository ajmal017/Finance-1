using Finance.Data;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static Finance.Logger;

namespace Finance
{
    public class SimulationManager
    {
        #region Events

        public delegate void SimulationStatusEventHandler(object sender, SimulationStatusEventArgs e);
        public event SimulationStatusEventHandler SimulationStatusChanged;
        private void OnSimulationStatusChanged(Simulation simulation)
        {
            SimulationStatusChanged?.Invoke(this, new SimulationStatusEventArgs(simulation));
        }

        #endregion
        #region Status Indicator

        StatusLabelControlManager StatusIndicatorControlManager { get; } = null;
        private void SetStatusIndicator(ProcessStatus processStatus)
        {
            lastStatus = processStatus;

            switch (processStatus)
            {
                case ProcessStatus.ErrorState:
                    StatusIndicatorControlManager?.SetStatus("Error State", Color.PaleVioletRed);
                    break;
                case ProcessStatus.Ready:
                    if (Simulations.Any(x => x.SimulationStatus == SimulationStatus.Running))
                        SetStatusIndicator(ProcessStatus.Working);
                    else
                        StatusIndicatorControlManager?.SetStatus("Ready", Color.LightGreen);
                    break;
                case ProcessStatus.Working:
                    StatusIndicatorControlManager?.SetStatus($"Running {Simulations.Where(x => x.SimulationStatus == SimulationStatus.Running).Count()} Simulations...", Color.Orange);

                    if (StatusTimer == null)
                    {
                        StatusTimer = new System.Timers.Timer(500);
                        StatusTimer.Elapsed += (s, e) =>
                        {
                            if (Simulations.Any(x => x.SimulationStatus == SimulationStatus.Running))
                            {
                                SetStatusIndicator(ProcessStatus.Working);
                            }
                            else
                            {
                                SetStatusIndicator(ProcessStatus.Ready);
                                StatusTimer.Stop();
                                return;
                            }
                        };
                    }
                    StatusTimer.Start();
                    break;
                case ProcessStatus.Offline:
                    StatusIndicatorControlManager?.SetStatus("Offline", Color.PaleVioletRed);
                    break;
                default:
                    break;
            }
        }
        public Control StatusIndicator
        {
            get
            {
                var ret = StatusIndicatorControlManager.IssueControl();
                SetStatusIndicator(lastStatus);
                return ret;
            }
        }

        private System.Timers.Timer StatusTimer = null;
        ProcessStatus lastStatus { get; set; }

        #endregion

        public BindingList<Simulation> Simulations { get; private set; } = new BindingList<Simulation>();
        public int SimulationCount { get => Simulations.Count; }

        public SimulationManager()
        {
            StatusIndicatorControlManager = new StatusLabelControlManager("SimulationManager");
            SetStatusIndicator(ProcessStatus.Ready);
        }

        public Simulation CreateSimulation(string Name, IEnvironment environment, DataManager dataManager)
        {
            var ret = Simulations.AddAndReturn(
                new Simulation(environment, dataManager, PortfolioSetup.Default(), StrategyManager.Default(), new RiskManager(), Name));

            ret.SimulationStatusChanged += (s, e) => OnSimulationStatusChanged(e.Simulation);

            return ret;
        }
        public Simulation CreateSimulation(Simulation simulation)
        {
            string baseName;
            Simulation ret = null;

            // Rename sequentially
            if (int.TryParse(simulation.Name.Split(null).Last(), out int CopyNumber))
            {
                baseName = simulation.Name.TrimEnd(new[] { char.Parse(CopyNumber.ToString()), ' ' });
                while (Simulations.Any(x => x.Name == string.Format($"{baseName} {CopyNumber}")))
                    CopyNumber += 1;
                ret = Simulations.AddAndReturn(simulation.Copy(string.Format($"{baseName} {CopyNumber}")));
            }
            else
            {
                CopyNumber = 1;
                ret = Simulations.AddAndReturn(simulation.Copy(string.Format($"{simulation.Name} {CopyNumber}")));
            }

            ret.SimulationStatusChanged += (s, e) => OnSimulationStatusChanged(e.Simulation);

            return ret;
        }
        public Simulation GetSimulation(string Name)
        {
            return Simulations.Where(x => x.Name == Name).SingleOrDefault();
        }
        public void RemoveSimulation(string Name)
        {
            int index = Simulations.ToList().FindIndex(x => x.Name == Name);

            if (index != -1)
                Simulations.RemoveAt(index);
        }
        public void RemoveSimulation(Simulation simulation)
        {
            Simulations.Remove(simulation);
        }
        public void Run(Simulation simulation, DateTime startDate, DateTime endDate)
        {
            Log(new LogMessage("SimulationManager", $"Run simulation -> '{simulation.Name}'", LogMessageType.Production));

            //
            // Run on new thread
            //
            new Thread(() => simulation.Run(startDate, endDate)).Start();

            SetStatusIndicator(ProcessStatus.Working);
        }
        public void RunAll(DateTime startDate, DateTime endDate)
        {
            Log(new LogMessage("SimulationManager", $"Run all pending simulations ({Simulations.Where(x => x.SimulationStatus == SimulationStatus.NotStarted).Count()})", LogMessageType.Production));

            foreach (var simulation in Simulations)
            {
                if (simulation.SimulationStatus != SimulationStatus.NotStarted)
                    continue;

                Run(simulation, startDate, endDate);
            }
        }
    }
}