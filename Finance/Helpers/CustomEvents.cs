using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{

    public class DataManagerStatusChangeEventArgs : EventArgs
    {
        public bool DatabaseConnected;
        public bool DataproviderConnected;
    }

    public class LogEventArgs : EventArgs
    {
        public LogMessage message { get; }

        public LogEventArgs(LogMessage message)
        {
            this.message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }

    #region Simulation Status

    //
    // Used to signal a change in status of simulation objects
    //

    public delegate void SimulationStatusEventHandler(object sender, SimulationStatusEventArgs e);
    public class SimulationStatusEventArgs : EventArgs
    {
        public Simulation Simulation { get; }
        public SimulationStatus Status => Simulation.SimulationStatus;

        public SimulationStatusEventArgs(Simulation simulation)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        }
    }

    #endregion

    #region Security Data Response

    //
    // Used to pass security information during retrieval and storage operations
    //

    public delegate void SecurityResponseDataHandler(object sender, SecurityDataResponseEventArgs e);
    public class SecurityDataResponseEventArgs : EventArgs
    {
        public Security security;
        public EventFlag flag = EventFlag.NotSet;
        public SecurityDataResponseEventArgs(Security security, EventFlag flag)
        {
            this.security = security ?? throw new ArgumentNullException(nameof(security));
            this.flag = flag;
        }
    }

    #endregion

}
