using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Finance
{
    public class ChartViewerForm : Form
    {
        private Size _defaultSize = new Size(1000, 750);

        public Security Security { get; private set; }
        public PriceBarSize PriceBarSize => ChartPanel.PriceBarSize;

        private SecurityChartPanel ChartPanel;

        public ChartViewerForm()
        {
            this.InitializeMe();
        }
        [Initializer]
        private void InitializeStyles()
        {
            Size = _defaultSize;
        }
        [Initializer]
        private void InitializeChartPanel()
        {
            ChartPanel = new SecurityChartPanel();
            this.Controls.Add(ChartPanel);
        }
        public void LoadSecurity(Security security, PriceBarSize priceBarSize, int swingpointBarCount)
        {
            this.Security = security;

            ChartPanel.LoadSecurity(this.Security, priceBarSize, swingpointBarCount);
        }
    }
    public class ResultViewerFormNew : Form
    {
        private Size _defaultSize = new Size(1000, 750);

        public Simulation Simulation { get; private set; }

        private SimResultsChartPanel ChartPanel;

        public ResultViewerFormNew()
        {
            this.InitializeMe();
        }
        [Initializer]
        private void InitializeStyles()
        {
            Size = _defaultSize;
        }
        [Initializer]
        private void InitializeChartPanel()
        {
            ChartPanel = new SimResultsChartPanel();
            this.Controls.Add(ChartPanel);
        }
        public void LoadSimulation(Simulation simulation)
        {
            ChartPanel.LoadSimulation(simulation);
        }

    }
}
