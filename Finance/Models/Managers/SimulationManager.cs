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

        private static SimulationManager _Instance { get; set; }
        public static SimulationManager Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new SimulationManager();
                return _Instance;
            }
        }

        #region Events

        public event SimulationStatusEventHandler SimulationStatusChanged;
        private void OnSimulationStatusChanged(Simulation simulation)
        {
            SimulationStatusChanged?.Invoke(this, new SimulationStatusEventArgs(simulation));
        }

        #endregion
     
        public BindingList<Simulation> Simulations { get; private set; } = new BindingList<Simulation>();
        public int SimulationCount { get => Simulations.Count; }

        private SimulationManager()
        {
        }

        public Simulation CreateSimulation(string Name)
        {
            var ret = Simulations.AddAndReturn(
                new Simulation(PortfolioSetup.Default(), StrategyManager.Default(), RiskManager.Default(), Name));

            ret.SimulationStatusChanged += (s, e) => OnSimulationStatusChanged(e.Simulation);

            return ret;
        }
        public Simulation CreateSimulation(Simulation simulation)
        {
            string baseName;
            Simulation ret = null;
            int CopyNumber = 1;

            if (simulation.Name.Split(null).Last().Contains("("))
            {
                baseName = simulation.Name.Replace(simulation.Name.Split(null).Last(), "").Trim();
                CopyNumber = int.Parse(simulation.Name.Split(null).Last().Trim('(', ')')) + 1;

                while (GetSimulation(string.Format($"{baseName} ({CopyNumber})")) != null)
                    CopyNumber += 1;

                ret = Simulations.AddAndReturn(simulation.Copy(string.Format($"{baseName} ({CopyNumber})")));
            }
            else
            {
                while (GetSimulation(string.Format($"{simulation.Name} ({CopyNumber})")) != null)
                    CopyNumber += 1;

                ret = Simulations.AddAndReturn(simulation.Copy(string.Format($"{simulation.Name} ({CopyNumber})")));
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
        public void Run(Simulation simulation)
        {
            Log(new LogMessage("SimulationManager", $"Run simulation -> '{simulation.Name}'", LogMessageType.Production));

            //
            // Run on new thread
            //
            new Thread(() => simulation.Run()).Start();
        }
        public void RunAll()
        {
            Log(new LogMessage("SimulationManager", $"Run all pending simulations ({Simulations.Where(x => x.SimulationStatus == SimulationStatus.NotStarted).Count()})", LogMessageType.Production));

            foreach (var simulation in Simulations)
            {
                if (simulation.SimulationStatus != SimulationStatus.NotStarted)
                    continue;

                Run(simulation);
            }
        }
    }
}