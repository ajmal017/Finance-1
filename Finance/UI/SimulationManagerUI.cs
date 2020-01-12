using Finance;
using Finance.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Finance.Helpers;

namespace Finance.UI
{
    public partial class SimulationManagerUI : Form
    {
        SimulationManager SimulationManager { get; }
        DataManager DataManager { get; }
        IEnvironment Environment { get; }

        Size _formSize = new Size(1000, 750);

        public SimulationManagerUI(DataManager dataManager, SimulationManager simulationManager)
        {
            DataManager = dataManager;
            Environment = new IbkrEnvironment();
            SimulationManager = simulationManager;

            InitializeComponent();

            FormBorderStyle = FormBorderStyle.FixedSingle;
            Text = "Simulation Manager";
            Size = _formSize;

            this.InitializeMe();
        }

        #region Layout

        Panel pnlLeft;
        Panel pnlRight;

        [Initializer]
        private void InitializeLayout()
        {
            //
            // Left side panel
            //
            pnlLeft = new Panel
            {
                Size = new Size(ClientRectangle.Width / 2, ClientRectangle.Height),
                Location = ClientRectangle.Location
            };

            //
            // Right side panel
            //
            pnlRight = new Panel
            {
                Size = new Size(ClientRectangle.Width / 2, ClientRectangle.Height)
            };
            pnlRight.DockTo(pnlLeft, DockSide.Right);

            Controls.AddRange(new Control[] { pnlLeft, pnlRight });
        }

        #endregion
        #region Add/Copy/Delete Controls

        Panel pnlNewSimControls;
        TextBox txtNewSimulation;
        Label lblAddSimulation;
        Button btnCopySimulation;
        Button btnDeleteSimulation;

        [Initializer]
        private void InitializeSimManagementControls()
        {
            pnlNewSimControls = new Panel();
            lblAddSimulation = new Label();
            txtNewSimulation = new TextBox();
            btnCopySimulation = new Button();
            btnDeleteSimulation = new Button();

            Size _btnSize = new Size(85, 20);

            //
            // Panel
            //
            pnlNewSimControls.Name = "pnlNewSimControls";
            pnlNewSimControls.Size = new Size(200, 75);
            pnlNewSimControls.BorderStyle = BorderStyle.FixedSingle;
            //
            // lblAddSimulation
            //
            lblAddSimulation.Name = "lblAddSimulation";
            lblAddSimulation.Width = 175;
            lblAddSimulation.Height = 16;
            lblAddSimulation.Text = "Add Simulation";

            //
            // txtNewSimulation
            //
            txtNewSimulation.Name = "txtNewSimulation";
            txtNewSimulation.Width = 175;
            txtNewSimulation.KeyUp += (s, e) =>
            {
                if (e.KeyCode != Keys.Return)
                    return;

                // Create a simulation with the name entered
                if (txtNewSimulation.Text.Length == 0)
                    return;

                CreateNewSimulation(txtNewSimulation.Text);
                txtNewSimulation.Clear();
            };
            //
            // btnCopySimulation
            //
            btnCopySimulation.Name = "btnCopySimulation";
            btnCopySimulation.Text = "Copy Simulation";
            btnCopySimulation.Size = _btnSize;
            btnCopySimulation.Enabled = false;
            btnCopySimulation.Click += (s, e) =>
            {
                var sim = GetSelectedSimulation();
                if (sim == null)
                    return;

                CopySimulation(sim);
            };
            SimulationManager.Simulations.ListChanged += (s, e) =>
            {
                if (SimulationManager.SimulationCount > 0)
                    btnCopySimulation.Enabled = true;
                else
                    btnCopySimulation.Enabled = false;
            };
            //
            // btnDeleteSimulation
            //
            btnDeleteSimulation.Name = "btnDeleteSimulation";
            btnDeleteSimulation.Text = "Delete Simulation";
            btnDeleteSimulation.Size = _btnSize;
            btnDeleteSimulation.Enabled = false;
            btnDeleteSimulation.Click += (s, e) =>
            {
                Simulation simulation = GetSelectedSimulation();
                if (simulation == null)
                    return;

                DeleteSimulation(simulation);
            };
            SimulationManager.Simulations.ListChanged += (s, e) =>
            {
                if (SimulationManager.SimulationCount > 0)
                    btnDeleteSimulation.Enabled = true;
                else
                    btnDeleteSimulation.Enabled = false;
            };
            //
            // Add and Arrange
            //
            pnlNewSimControls.Controls.AddRange(new Control[]
            {
                lblAddSimulation,
                txtNewSimulation,
                btnCopySimulation,
                btnDeleteSimulation
            });
            lblAddSimulation.Location = new Point(4, 4);
            txtNewSimulation.Location = new Point(lblAddSimulation.Left, lblAddSimulation.Bottom);
            btnCopySimulation.Location = new Point(txtNewSimulation.Left, txtNewSimulation.Bottom + 5);
            btnDeleteSimulation.Location = new Point(btnCopySimulation.Right + 5, btnCopySimulation.Top);

            pnlLeft.Controls.Add(pnlNewSimControls);
            pnlNewSimControls.Location = new Point(5, 5);
        }

        #endregion
        #region Date Picker Controls

        Panel pnlDateTimePickerPanel;
        Label lblTimeSpan;
        Label lblStartDate;
        Label lblEndDate;
        DateTimePicker dtpStart;
        DateTimePicker dtpEnd;

        [Initializer]
        private void InitializeDatePickerControls()
        {

            pnlDateTimePickerPanel = new Panel();
            lblTimeSpan = new Label();
            lblStartDate = new Label();
            lblEndDate = new Label();
            dtpStart = new DateTimePicker();
            dtpEnd = new DateTimePicker();

            //
            // Panel
            //
            pnlDateTimePickerPanel.Name = "pnlDateTimePicker";
            pnlDateTimePickerPanel.Size = new Size(200, 75);
            pnlDateTimePickerPanel.BorderStyle = BorderStyle.FixedSingle;
            //
            // lblTimeSpan
            //
            lblTimeSpan.Name = "lblTimeSpan";
            lblTimeSpan.Text = "Simulation Duration";
            lblTimeSpan.Size = new Size(150, 20);
            //
            // lblStart
            //
            lblStartDate.Name = "lblStartDate";
            lblStartDate.Text = "Start Date";
            lblStartDate.Size = new Size(35, 20);
            //
            // lblEnd
            //
            lblEndDate.Name = "lblEndDate";
            lblEndDate.Text = "End Date";
            lblEndDate.Size = new Size(35, 20);
            //
            // dtpStart
            //
            dtpStart.Name = "dtpStart";
            dtpStart.Format = DateTimePickerFormat.Short;
            dtpStart.Size = new Size(150, 20);
            Func<DateTime> startDate = () =>
            {
                // Default start date should be one year ago on a valid trading day
                var d = DateTime.Today.AddYears(-1);
                while (!Calendar.IsTradingDay(d))
                    d = Calendar.PriorTradingDay(d);
                return d;
            };
            dtpStart.Value = startDate();
            dtpStart.ValueChanged += (s, e) =>
            {
                // Must start on a trading day
                if (!Calendar.IsTradingDay(dtpStart.Value))
                    dtpStart.Value = Calendar.NextTradingDay(dtpStart.Value);

                // Make sure the end date is valid based on start date
                if (dtpEnd.Value <= dtpStart.Value)
                    dtpEnd.Value = Calendar.NextTradingDay(dtpStart.Value);
            };
            //
            // dtpEnd
            //
            dtpEnd.Name = "dtpEnd";
            dtpEnd.Format = DateTimePickerFormat.Short;
            dtpEnd.Size = new Size(150, 20);
            Func<DateTime> endDate = () =>
            {
                // Default end date is the last valid trading day
                var d = Calendar.PriorTradingDay(DateTime.Today);
                return d;
            };
            dtpEnd.Value = dtpEnd.MaxDate = endDate();
            dtpEnd.ValueChanged += (s, e) =>
            {
                if (dtpEnd.Value <= dtpStart.Value)
                    dtpEnd.Value = Calendar.NextTradingDay(dtpStart.Value);
            };
            //
            // Add and Arrange
            //
            pnlDateTimePickerPanel.Controls.AddRange(new Control[]
            {
                lblTimeSpan,
                lblStartDate,
                lblEndDate,
                dtpStart,
                dtpEnd
            });
            lblTimeSpan.Location = new Point(4, 4);
            lblStartDate.DockTo(lblTimeSpan, DockSide.Bottom, 2);
            lblEndDate.DockTo(lblStartDate, DockSide.Bottom, 2);
            dtpStart.DockTo(lblStartDate, DockSide.Right, 2);
            dtpEnd.DockTo(lblEndDate, DockSide.Right, 2);

            pnlLeft.Controls.Add(pnlDateTimePickerPanel);
            pnlDateTimePickerPanel.DockTo(pnlNewSimControls, DockSide.Right, 5);
        }

        #endregion
        #region Parameter Control Tabs

        TabControl tabParameterControl;
        TabPage tbpRiskManager;
        TabPage tbpStrategyManager;
        TabPage tbpStrategy;

        [Initializer]
        private void InitializeParameterControlTabs()
        {
            tabParameterControl = new TabControl();
            tbpRiskManager = new TabPage();
            tbpStrategyManager = new TabPage();
            tbpStrategy = new TabPage();

            //
            // tabParameterControl
            //
            tabParameterControl.Name = "tabParameterControl";
            tabParameterControl.Size = new Size(pnlLeft.Width - 10, pnlLeft.Height / 2);
            tabParameterControl.SelectedIndexChanged += (s, e) =>
            {
                DrawTabPage();
            };

            //
            // Risk Manager Tab Page
            //
            tbpRiskManager.Name = "tbpRiskManager";
            tbpRiskManager.Text = "Risk Manager";

            //
            // Strategy Manager Tab Page
            //
            tbpStrategyManager.Name = "tbpStrategyManager";
            tbpStrategyManager.Text = "Strategy Manager";

            //
            // Active Strategy Tab Page
            //
            tbpStrategy.Name = "tbpStrategy";
            tbpStrategy.Text = "Strategy";

            //
            // Add and Arrange
            //
            tabParameterControl.TabPages.AddRange(new TabPage[]
            {
                tbpRiskManager,
                tbpStrategyManager,
                tbpStrategy
            });
            pnlLeft.Controls.Add(tabParameterControl);
            tabParameterControl.DockTo(pnlNewSimControls, DockSide.Bottom, 10);

        }

        private void DrawTabPage()
        {
            if (tabParameterControl.SelectedIndex != -1)
                DrawTabPage_ByName(tabParameterControl.SelectedTab);
        }
        private void DrawTabPage_ByName(TabPage page)
        {
            // Null check the selected simulation
            page.Controls.Clear();
            Simulation simulation = GetSelectedSimulation();
            if (simulation == null) return;

            // Set the Page Tag as a reference to the currently loaded simulation
            page.Tag = simulation;

            // Call appropriate method to draw page
            switch (page.Text)
            {
                case "Risk Manager":
                    DrawTabPage_RiskManager(page);
                    break;
                case "Strategy Manager":
                    DrawTabPage_StrategyManager(page);
                    break;
                case "Strategy":
                    DrawTabPage_Strategy(page);
                    break;
                default:
                    throw new UnknownErrorException();
            }
        }
        private void DrawTabPage_RiskManager(TabPage page)
        {
            FlowLayoutPanel pnlTabPagePanel;
            RiskManager riskManagerReference;

            //
            // pnlTabPagePanel holds all contents of the TabPage.  Disable all controls if the simulation is active or complete
            //
            pnlTabPagePanel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                Enabled = (page.Tag as Simulation).SimulationStatus == SimulationStatus.NotStarted
            };
            page.Controls.Add(pnlTabPagePanel);

            //
            // Set the reference to the active Risk Manager and get a list of ParameterAttribute properties
            //
            riskManagerReference = (page.Tag as Simulation).RiskManager;
            var parameterProperties = riskManagerReference.GetAvailableParameterList();

            //
            // Create a ParameterDisplayUpdatePanel for each available parameter and add to the page
            //
            foreach (var parameterProperty in parameterProperties)
            {
                pnlTabPagePanel.Controls.Add(new ParameterDisplayUpdatePanel(parameterProperty, riskManagerReference));
            }

        }
        private void DrawTabPage_StrategyManager(TabPage page)
        {
            FlowLayoutPanel pnlTabPagePanel;
            StrategyManager strategyManagerReference;

            //
            // pnlTabPagePanel holds all contents of the TabPage.  Disable all controls if the simulation is active or complete
            //
            pnlTabPagePanel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                Enabled = (page.Tag as Simulation).SimulationStatus == SimulationStatus.NotStarted
            };
            page.Controls.Add(pnlTabPagePanel);

            //
            // Set the reference to the active Risk Manager and get a list of ParameterAttribute properties
            //
            strategyManagerReference = (page.Tag as Simulation).StrategyManager;
            var parameterProperties = strategyManagerReference.GetAvailableParameterList();

            //
            // Create a ParameterDisplayUpdatePanel for each available parameter and add to the page
            //
            foreach (var parameterProperty in parameterProperties)
            {
                pnlTabPagePanel.Controls.Add(new ParameterDisplayUpdatePanel(parameterProperty, strategyManagerReference));
            }

            //
            // Create a StrategyDisplayUpdatePanel for each strategy available to the manager and add to the page
            //
            foreach (var strategy in strategyManagerReference.AllTradeStrategies)
            {
                pnlTabPagePanel.Controls.Add(new StrategyDisplayUpdatePanel(strategy, strategyManagerReference));
            }

        }
        private void DrawTabPage_Strategy(TabPage page)
        {
            FlowLayoutPanel pnlTabPagePanel;
            TradeStrategyBase activeTradeStrategyReference;

            //
            // pnlTabPagePanel holds all contents of the TabPage.  Disable all controls if the simulation is active or complete
            //
            pnlTabPagePanel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                Enabled = (page.Tag as Simulation).SimulationStatus == SimulationStatus.NotStarted
            };
            page.Controls.Add(pnlTabPagePanel);

            //
            // Set the reference to the active Risk Manager and get a list of ParameterAttribute properties
            //
            activeTradeStrategyReference = (page.Tag as Simulation).StrategyManager.ActiveTradeStrategy;
            var parameterProperties = activeTradeStrategyReference.GetAvailableParameterList();

            //
            // Create a ParameterDisplayUpdatePanel for each available parameter and add to the page
            //
            foreach (var parameterProperty in parameterProperties)
            {
                pnlTabPagePanel.Controls.Add(new ParameterDisplayUpdatePanel(parameterProperty, activeTradeStrategyReference));
            }

        }

        #endregion
        #region Simulation Data Grid Control

        DataGridView dgvSimultionDataGrid;
        System.Windows.Forms.Timer tmrDateGridRefreshTimer;

        [Initializer]
        private void InitializeSimulationDataGrid()
        {
            int _buffer = 5;
            Size _dgvSize = new Size(pnlRight.Width - (2 * _buffer),
                (pnlRight.Height / 3) * 2 - (2 * _buffer));

            //
            // Data Grid
            //
            dgvSimultionDataGrid = new DataGridView
            {
                AutoGenerateColumns = false
            };
            dgvSimultionDataGrid.Columns.Clear();
            dgvSimultionDataGrid.ColumnCount = 2;
            dgvSimultionDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSimultionDataGrid.MultiSelect = false;
            dgvSimultionDataGrid.RowHeadersVisible = false;
            dgvSimultionDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSimultionDataGrid.SelectionChanged += (s, e) =>
            {
                SetControls();
                DrawTabPage();
            };
            dgvSimultionDataGrid.DataSource = SimulationManager.Simulations;

            //
            // Col 1 - Name
            //
            dgvSimultionDataGrid.Columns[0].Name = "colSimName";
            dgvSimultionDataGrid.Columns[0].DataPropertyName = "Name";
            dgvSimultionDataGrid.Columns[0].HeaderText = "Simulation Name";

            //
            // Col 2 - Status
            //
            dgvSimultionDataGrid.Columns[1].Name = "colStatus";
            dgvSimultionDataGrid.Columns[1].DataPropertyName = "SimulationStatus";
            dgvSimultionDataGrid.Columns[1].HeaderText = "Status";

            //
            // Add and arrange
            //            
            pnlRight.Controls.Add(dgvSimultionDataGrid);
            dgvSimultionDataGrid.Size = _dgvSize;
            dgvSimultionDataGrid.Location = new Point(_buffer, _buffer);

            //
            // Refresh timer
            //

            tmrDateGridRefreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 500
            };
            tmrDateGridRefreshTimer.Tick += (s, e) =>
            {
                RefreshGridView();
            };
            tmrDateGridRefreshTimer.Start();
        }

        private void RefreshGridView()
        {
            if (InvokeRequired)
                Invoke(new Action(() => { RefreshGridView(); }));
            else
            {
                dgvSimultionDataGrid.Refresh();
                SetControls();
            }
        }
        private Simulation GetSelectedSimulation()
        {
            if (dgvSimultionDataGrid.SelectedCells.Count == 0)
                return null;

            return SimulationManager.GetSimulation(dgvSimultionDataGrid.SelectedCells[0].Value.ToString());
        }
        private void SetControls()
        {
            Simulation simulation = GetSelectedSimulation();
            if (simulation == null)
            {
                btnRunSingleSim.Enabled = false;
                btnRunAllSims.Enabled = false;
                btnCopySimulation.Enabled = false;
                btnDeleteSimulation.Enabled = false;
                btnViewResults.Enabled = false;
            }
            else
            {
                btnRunSingleSim.Enabled = (simulation.SimulationStatus == SimulationStatus.NotStarted);
                btnViewResults.Enabled = (simulation.SimulationStatus == SimulationStatus.Complete);
                btnRunAllSims.Enabled = true;
                btnCopySimulation.Enabled = true;
                btnDeleteSimulation.Enabled = true;
            }
        }

        #endregion
        #region Run/View Results

        Button btnRunSingleSim;
        Button btnRunAllSims;
        Button btnViewResults;

        [Initializer]
        private void InitializeRunButtons()
        {
            btnRunSingleSim = new Button();
            btnRunAllSims = new Button();
            btnViewResults = new Button();

            Size _btnSize = new Size(125, 50);

            //
            // btnRunSingleSim
            //
            btnRunSingleSim.Name = "bntRunSingleSim";
            btnRunSingleSim.Size = _btnSize;
            btnRunSingleSim.Text = "Run Simulation";
            btnRunSingleSim.Enabled = false;
            btnRunSingleSim.Click += (s, e) =>
            {
                var sim = GetSelectedSimulation();
                if (sim == null)
                    return;
                RunSimulation(sim);
            };

            //
            // btnRunAllSims
            //
            btnRunAllSims.Name = "bntRunAllSims";
            btnRunAllSims.Size = _btnSize;
            btnRunAllSims.Text = "Run All";
            btnRunAllSims.Enabled = false;
            btnRunAllSims.Click += (s, e) =>
            {
                RunAllSimulations();
            };

            //
            // btnViewResults
            //
            btnViewResults.Name = "btnViewResults";
            btnViewResults.Size = _btnSize;
            btnViewResults.Text = "View Results";
            btnViewResults.Enabled = false;
            btnViewResults.Click += (s, e) =>
            {
                if (GetSelectedSimulation() == null)
                    return;
                ViewResults(GetSelectedSimulation());
            };

            pnlRight.Controls.Add(btnRunSingleSim);
            pnlRight.Controls.Add(btnRunAllSims);
            pnlRight.Controls.Add(btnViewResults);

            btnRunSingleSim.DockTo(dgvSimultionDataGrid, DockSide.Bottom, 5);
            btnRunAllSims.DockTo(btnRunSingleSim, DockSide.Right, 5);
            btnViewResults.DockTo(btnRunAllSims, DockSide.Right, 5);
        }

        #endregion
        #region Simulation Management

        private void CreateNewSimulation(string Name)
        {
            Cursor.Current = Cursors.WaitCursor;
            SimulationManager.CreateSimulation(Name, Environment, DataManager);
            var i = dgvSimultionDataGrid.Rows.GetLastRow(DataGridViewElementStates.Displayed);
            dgvSimultionDataGrid.Rows[i].Selected = true;
            Cursor.Current = Cursors.Default;
        }
        private void CopySimulation(Simulation simulation)
        {
            if (simulation == null)
                return;

            Cursor.Current = Cursors.WaitCursor;
            SimulationManager.CreateSimulation(simulation);
            var i = dgvSimultionDataGrid.Rows.GetLastRow(DataGridViewElementStates.Displayed);
            dgvSimultionDataGrid.Rows[i].Selected = true;
            Cursor.Current = Cursors.Default;
        }
        private void DeleteSimulation(Simulation simulation)
        {
            if (simulation == null)
                return;

            Cursor.Current = Cursors.WaitCursor;
            if (MessageBox.Show($"Delete {simulation.Name}?", "Delete?", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                SimulationManager.RemoveSimulation(simulation);
            }
            Cursor.Current = Cursors.Default;
        }
        private void RunSimulation(Simulation simulation)
        {
            SimulationManager.Run(simulation, dtpStart.Value, dtpEnd.Value);
        }
        private void RunAllSimulations()
        {
            SimulationManager.RunAll(dtpStart.Value, dtpEnd.Value);
        }

        #endregion
        #region Results Management

        private void ViewResults(Simulation simulation)
        {
            MessageBox.Show("Show results here");
        }

        #endregion        
    }
}
