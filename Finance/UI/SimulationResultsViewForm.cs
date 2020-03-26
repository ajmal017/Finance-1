using System;
using System.Drawing;
using System.Windows.Forms;

namespace Finance
{
    #region Simulation Results Viewer

    public class SimulationResultsViewForm : Form
    {
        public Size _defaultSize => new Size(1000, 750);

        public Simulation Simulation { get; }

        Panel pnlControls;

        Panel pnlMain;
        Panel pnlAccountViewTop;
        Panel pnlSecurityViewBottom;

        //AccountChart accountChart;
        FinanceSecurityChart securityChart;

        AccountingSeriesSelectPanel pnlSeriesSelect;
        AccountSummaryPanel pnlAccountSummary;
        PositionListPanel pnlPositionList;
        PositionSummaryPanel pnlPositionSummary;

        public SimulationResultsViewForm(Simulation simulation)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeForm()
        {
            Text = $"Simulation Result View for {Simulation.Name}";
            WindowState = FormWindowState.Maximized;

            //
            // pnlControls
            //
            pnlControls = new Panel()
            {
                Dock = DockStyle.Left,
                Width = 300,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlControls);

            //
            // pnlMain
            //
            pnlMain = new Panel()
            {
                Width = this.ClientRectangle.Width - pnlControls.Width,
                Height = this.ClientRectangle.Height,
            };
            pnlMain.DockTo(pnlControls, ControlEdge.Right);
            this.Controls.Add(pnlMain);

            //
            // pnlAccountViewTop
            //
            pnlAccountViewTop = new Panel()
            {
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlMain.Controls.Add(pnlAccountViewTop);

            //
            // pnlSecurityViewBottom
            //
            pnlSecurityViewBottom = new Panel()
            {
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlSecurityViewBottom.DockTo(pnlAccountViewTop, ControlEdge.Bottom);
            pnlMain.Controls.Add(pnlSecurityViewBottom);

            Action sizePanels = new Action(() =>
            {
                pnlMain.Width = this.ClientRectangle.Width - pnlControls.Width;
                pnlMain.Height = this.ClientRectangle.Height;

                pnlAccountViewTop.Width = pnlMain.Width;
                pnlAccountViewTop.Height = pnlMain.Height / 2;

                pnlSecurityViewBottom.Width = pnlMain.Width;
                pnlSecurityViewBottom.Height = pnlMain.Height / 2;
                pnlSecurityViewBottom.DockTo(pnlAccountViewTop, ControlEdge.Bottom);

            });
            this.SizeChanged += (s, e) => sizePanels();

        }

        [Initializer]
        private void InitializeControls()
        {
            //
            // Account value series selected
            //
            pnlSeriesSelect = new AccountingSeriesSelectPanel("Series")
            {
                Size = new Size(250, 200)
            };
            pnlSeriesSelect.SelectedValueChanged += (s, e) =>
            {
                //accountChart?.SetSeries(pnlSeriesSelect.SelectedValue);
            };
            pnlSeriesSelect.Location = new Point(2, 2);
            pnlControls.Controls.Add(pnlSeriesSelect);

            //
            // Account Summary Panel
            //
            pnlAccountSummary = new AccountSummaryPanel("Summary", Simulation)
            {
                Size = new Size(300, 400)
            };
            pnlAccountSummary.DockTo(pnlSeriesSelect, ControlEdge.Bottom, 5);
            pnlControls.Controls.Add(pnlAccountSummary);

            //
            // Position List Box
            //
            pnlPositionList = new PositionListPanel("Positions", Simulation)
            {
                Size = new Size(75, 200)
            };
            pnlPositionList.SelectedValueChanged += (s, e) =>
            {
                if (pnlPositionList.SelectedValue == null)
                    return;
            };
            pnlPositionList.DockTo(pnlAccountSummary, ControlEdge.Bottom, 5);
            pnlControls.Controls.Add(pnlPositionList);

            //
            // Position Summary Panel
            //
            pnlPositionSummary = new PositionSummaryPanel("Position Summary")
            {
                Size = new Size(200, 200)
            };
            pnlPositionList.SelectedValueChanged += (s, e) =>
            {
                if (pnlPositionList.SelectedValue == null)
                    return;

                var positions = Simulation.PortfolioManager.Portfolio.GetPositions(pnlPositionList.SelectedValue);
                pnlPositionSummary.LoadPosition(positions);
            };
            pnlPositionSummary.DockTo(pnlPositionList, ControlEdge.Right, 2);
            pnlControls.Controls.Add(pnlPositionSummary);

        }

        [Initializer]
        private void InitializeCharts()
        {
            //
            // accountchart
            //
            //accountChart = new AccountChart(Simulation)
            //{
            //    Dock = DockStyle.Fill
            //};
            //pnlAccountViewTop.Controls.Add(accountChart);

            //
            // securityChart
            //
            securityChart = new FinanceSecurityChart()
            {
                Dock = DockStyle.Fill
            };
            //accountChart.ViewChanged += (s, e) =>
            //{
            //    securityChart.SetView(accountChart.CurrentView.Item1, accountChart.CurrentView.Item2);
            //};
            pnlSecurityViewBottom.Controls.Add(securityChart);
        }
    }

    #endregion
};