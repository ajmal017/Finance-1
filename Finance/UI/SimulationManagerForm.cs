using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Finance.TradeStrategies;
using Finance.PositioningStrategies;
using static Finance.Helpers;

namespace Finance
{
    public class SimulationManagerForm : Form, IPersistLayout
    {
        private static SimulationManagerForm _Instance { get; set; }
        public static SimulationManagerForm Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new SimulationManagerForm();
                return _Instance;
            }
        }

        #region Events

        public event SelectedSimulationChangedEventHandler SelectedSimulationChanged;
        private void OnSelectedSimulationChanged()
        {
            this.SelectedSimulationChanged?.Invoke(this, new SelectedSimulationEventArgs(this.SelectedSimulation));
        }

        #endregion

        #region Controls

        private ToolStripMenuItem toolsToolStripMenuItem;
        private Panel pnlMain;
        private Panel pnlSimList;
        private Panel pnlSimResults;
        private Panel pnlSimSettings;
        private Panel pnlTradingStrategySelect;
        private Panel pnlAddRemoveCopy;
        private Button btnAddSim;
        private TextBox txtAddSimName;
        private Button btnCopyCurrentSim;
        private Button btnDeleteCurrentSim;
        private DataGridView gridSimulationList;
        private Label label1;
        private ComboBox boxSelectedTradingStrategy;
        private Label lblTradingStrategyDescription;
        private Panel panel1;
        private Button btnRunSelectedSim;
        private MenuStrip menuStrip1;
        private Button btnViewResultsChart;
        private Panel pnlTinyChart;
        private Panel pnlPositioningStrategySelect;
        private Label lblPositioningStrategyDescription;
        private Label label3;
        private ComboBox boxSelectedPositioningStrategy;
        private Label label2;
        private Panel pnlSectorResultsChart;
        private Label label4;
        private SimulationSettingsManagerPanel pnlSimulationSettingsManager;

        #endregion

        public bool Sizeable => false;

        private SimulationManagerForm()
        {
            Name = "SimManager";

            this.Shown += (s, e) => LoadLayout();
            this.ResizeEnd += (s, e) => SaveLayout();
            this.FormClosing += (s, e) =>
            {
                this.Hide();
                e.Cancel = true;
            };

            InitializeComponent();
            this.InitializeMe();
        }

        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnRunSelectedSim = new System.Windows.Forms.Button();
            this.pnlSimList = new System.Windows.Forms.Panel();
            this.gridSimulationList = new System.Windows.Forms.DataGridView();
            this.pnlSimResults = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.pnlSectorResultsChart = new System.Windows.Forms.Panel();
            this.pnlTinyChart = new System.Windows.Forms.Panel();
            this.btnViewResultsChart = new System.Windows.Forms.Button();
            this.pnlSimSettings = new System.Windows.Forms.Panel();
            this.pnlPositioningStrategySelect = new System.Windows.Forms.Panel();
            this.lblPositioningStrategyDescription = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.boxSelectedPositioningStrategy = new System.Windows.Forms.ComboBox();
            this.pnlTradingStrategySelect = new System.Windows.Forms.Panel();
            this.lblTradingStrategyDescription = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.boxSelectedTradingStrategy = new System.Windows.Forms.ComboBox();
            this.pnlAddRemoveCopy = new System.Windows.Forms.Panel();
            this.btnAddSim = new System.Windows.Forms.Button();
            this.btnCopyCurrentSim = new System.Windows.Forms.Button();
            this.btnDeleteCurrentSim = new System.Windows.Forms.Button();
            this.txtAddSimName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.panel1.SuspendLayout();
            this.pnlSimList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSimulationList)).BeginInit();
            this.pnlSimResults.SuspendLayout();
            this.pnlPositioningStrategySelect.SuspendLayout();
            this.pnlTradingStrategySelect.SuspendLayout();
            this.pnlAddRemoveCopy.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1657, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // pnlMain
            // 
            this.pnlMain.Controls.Add(this.panel1);
            this.pnlMain.Controls.Add(this.pnlSimList);
            this.pnlMain.Controls.Add(this.pnlSimResults);
            this.pnlMain.Controls.Add(this.btnViewResultsChart);
            this.pnlMain.Controls.Add(this.pnlSimSettings);
            this.pnlMain.Controls.Add(this.pnlPositioningStrategySelect);
            this.pnlMain.Controls.Add(this.pnlTradingStrategySelect);
            this.pnlMain.Controls.Add(this.pnlAddRemoveCopy);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(0, 24);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(1657, 718);
            this.pnlMain.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnRunSelectedSim);
            this.panel1.Location = new System.Drawing.Point(5, 527);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(253, 38);
            this.panel1.TabIndex = 0;
            // 
            // btnRunSelectedSim
            // 
            this.btnRunSelectedSim.BackColor = System.Drawing.Color.YellowGreen;
            this.btnRunSelectedSim.Location = new System.Drawing.Point(4, 3);
            this.btnRunSelectedSim.Name = "btnRunSelectedSim";
            this.btnRunSelectedSim.Size = new System.Drawing.Size(75, 30);
            this.btnRunSelectedSim.TabIndex = 1;
            this.btnRunSelectedSim.Text = "Run";
            this.btnRunSelectedSim.UseVisualStyleBackColor = false;
            // 
            // pnlSimList
            // 
            this.pnlSimList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlSimList.Controls.Add(this.gridSimulationList);
            this.pnlSimList.Location = new System.Drawing.Point(4, 79);
            this.pnlSimList.Name = "pnlSimList";
            this.pnlSimList.Size = new System.Drawing.Size(253, 442);
            this.pnlSimList.TabIndex = 0;
            // 
            // gridSimulationList
            // 
            this.gridSimulationList.AllowUserToAddRows = false;
            this.gridSimulationList.AllowUserToDeleteRows = false;
            this.gridSimulationList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSimulationList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSimulationList.Location = new System.Drawing.Point(0, 0);
            this.gridSimulationList.Name = "gridSimulationList";
            this.gridSimulationList.ReadOnly = true;
            this.gridSimulationList.Size = new System.Drawing.Size(251, 440);
            this.gridSimulationList.TabIndex = 0;
            // 
            // pnlSimResults
            // 
            this.pnlSimResults.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlSimResults.Controls.Add(this.label4);
            this.pnlSimResults.Controls.Add(this.label2);
            this.pnlSimResults.Controls.Add(this.pnlSectorResultsChart);
            this.pnlSimResults.Controls.Add(this.pnlTinyChart);
            this.pnlSimResults.Location = new System.Drawing.Point(690, 4);
            this.pnlSimResults.Name = "pnlSimResults";
            this.pnlSimResults.Size = new System.Drawing.Size(955, 702);
            this.pnlSimResults.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 4);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Account Equity";
            // 
            // pnlSectorResultsChart
            // 
            this.pnlSectorResultsChart.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlSectorResultsChart.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.pnlSectorResultsChart.Location = new System.Drawing.Point(4, 284);
            this.pnlSectorResultsChart.Name = "pnlSectorResultsChart";
            this.pnlSectorResultsChart.Size = new System.Drawing.Size(946, 413);
            this.pnlSectorResultsChart.TabIndex = 1;
            // 
            // pnlTinyChart
            // 
            this.pnlTinyChart.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlTinyChart.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.pnlTinyChart.Location = new System.Drawing.Point(4, 21);
            this.pnlTinyChart.Name = "pnlTinyChart";
            this.pnlTinyChart.Size = new System.Drawing.Size(360, 238);
            this.pnlTinyChart.TabIndex = 1;
            // 
            // btnViewResultsChart
            // 
            this.btnViewResultsChart.Location = new System.Drawing.Point(577, 571);
            this.btnViewResultsChart.Name = "btnViewResultsChart";
            this.btnViewResultsChart.Size = new System.Drawing.Size(107, 23);
            this.btnViewResultsChart.TabIndex = 0;
            this.btnViewResultsChart.Text = "View Results";
            this.btnViewResultsChart.UseVisualStyleBackColor = true;
            // 
            // pnlSimSettings
            // 
            this.pnlSimSettings.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlSimSettings.Location = new System.Drawing.Point(263, 155);
            this.pnlSimSettings.Name = "pnlSimSettings";
            this.pnlSimSettings.Size = new System.Drawing.Size(421, 410);
            this.pnlSimSettings.TabIndex = 0;
            // 
            // pnlPositioningStrategySelect
            // 
            this.pnlPositioningStrategySelect.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPositioningStrategySelect.Controls.Add(this.lblPositioningStrategyDescription);
            this.pnlPositioningStrategySelect.Controls.Add(this.label3);
            this.pnlPositioningStrategySelect.Controls.Add(this.boxSelectedPositioningStrategy);
            this.pnlPositioningStrategySelect.Location = new System.Drawing.Point(263, 80);
            this.pnlPositioningStrategySelect.Name = "pnlPositioningStrategySelect";
            this.pnlPositioningStrategySelect.Size = new System.Drawing.Size(421, 69);
            this.pnlPositioningStrategySelect.TabIndex = 0;
            // 
            // lblPositioningStrategyDescription
            // 
            this.lblPositioningStrategyDescription.AutoSize = true;
            this.lblPositioningStrategyDescription.Location = new System.Drawing.Point(9, 40);
            this.lblPositioningStrategyDescription.Name = "lblPositioningStrategyDescription";
            this.lblPositioningStrategyDescription.Size = new System.Drawing.Size(0, 13);
            this.lblPositioningStrategyDescription.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Positioning Strategy";
            // 
            // boxSelectedPositioningStrategy
            // 
            this.boxSelectedPositioningStrategy.FormattingEnabled = true;
            this.boxSelectedPositioningStrategy.Location = new System.Drawing.Point(110, 7);
            this.boxSelectedPositioningStrategy.Name = "boxSelectedPositioningStrategy";
            this.boxSelectedPositioningStrategy.Size = new System.Drawing.Size(194, 21);
            this.boxSelectedPositioningStrategy.TabIndex = 0;
            // 
            // pnlTradingStrategySelect
            // 
            this.pnlTradingStrategySelect.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlTradingStrategySelect.Controls.Add(this.lblTradingStrategyDescription);
            this.pnlTradingStrategySelect.Controls.Add(this.label1);
            this.pnlTradingStrategySelect.Controls.Add(this.boxSelectedTradingStrategy);
            this.pnlTradingStrategySelect.Location = new System.Drawing.Point(263, 4);
            this.pnlTradingStrategySelect.Name = "pnlTradingStrategySelect";
            this.pnlTradingStrategySelect.Size = new System.Drawing.Size(421, 69);
            this.pnlTradingStrategySelect.TabIndex = 0;
            // 
            // lblTradingStrategyDescription
            // 
            this.lblTradingStrategyDescription.AutoSize = true;
            this.lblTradingStrategyDescription.Location = new System.Drawing.Point(9, 40);
            this.lblTradingStrategyDescription.Name = "lblTradingStrategyDescription";
            this.lblTradingStrategyDescription.Size = new System.Drawing.Size(0, 13);
            this.lblTradingStrategyDescription.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Trading Strategy";
            // 
            // boxSelectedTradingStrategy
            // 
            this.boxSelectedTradingStrategy.FormattingEnabled = true;
            this.boxSelectedTradingStrategy.Location = new System.Drawing.Point(110, 7);
            this.boxSelectedTradingStrategy.Name = "boxSelectedTradingStrategy";
            this.boxSelectedTradingStrategy.Size = new System.Drawing.Size(194, 21);
            this.boxSelectedTradingStrategy.TabIndex = 0;
            // 
            // pnlAddRemoveCopy
            // 
            this.pnlAddRemoveCopy.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlAddRemoveCopy.Controls.Add(this.btnAddSim);
            this.pnlAddRemoveCopy.Controls.Add(this.btnCopyCurrentSim);
            this.pnlAddRemoveCopy.Controls.Add(this.btnDeleteCurrentSim);
            this.pnlAddRemoveCopy.Controls.Add(this.txtAddSimName);
            this.pnlAddRemoveCopy.Location = new System.Drawing.Point(4, 4);
            this.pnlAddRemoveCopy.Name = "pnlAddRemoveCopy";
            this.pnlAddRemoveCopy.Size = new System.Drawing.Size(253, 69);
            this.pnlAddRemoveCopy.TabIndex = 0;
            // 
            // btnAddSim
            // 
            this.btnAddSim.Location = new System.Drawing.Point(4, 30);
            this.btnAddSim.Name = "btnAddSim";
            this.btnAddSim.Size = new System.Drawing.Size(75, 31);
            this.btnAddSim.TabIndex = 1;
            this.btnAddSim.Text = "Create";
            this.btnAddSim.UseVisualStyleBackColor = true;
            // 
            // btnCopyCurrentSim
            // 
            this.btnCopyCurrentSim.BackColor = System.Drawing.SystemColors.ControlLight;
            this.btnCopyCurrentSim.Location = new System.Drawing.Point(85, 30);
            this.btnCopyCurrentSim.Name = "btnCopyCurrentSim";
            this.btnCopyCurrentSim.Size = new System.Drawing.Size(75, 31);
            this.btnCopyCurrentSim.TabIndex = 1;
            this.btnCopyCurrentSim.Text = "Copy";
            this.btnCopyCurrentSim.UseVisualStyleBackColor = false;
            // 
            // btnDeleteCurrentSim
            // 
            this.btnDeleteCurrentSim.BackColor = System.Drawing.Color.DarkOrange;
            this.btnDeleteCurrentSim.Location = new System.Drawing.Point(166, 30);
            this.btnDeleteCurrentSim.Name = "btnDeleteCurrentSim";
            this.btnDeleteCurrentSim.Size = new System.Drawing.Size(75, 31);
            this.btnDeleteCurrentSim.TabIndex = 1;
            this.btnDeleteCurrentSim.Text = "Delete";
            this.btnDeleteCurrentSim.UseVisualStyleBackColor = false;
            // 
            // txtAddSimName
            // 
            this.txtAddSimName.Location = new System.Drawing.Point(4, 4);
            this.txtAddSimName.Name = "txtAddSimName";
            this.txtAddSimName.Size = new System.Drawing.Size(156, 20);
            this.txtAddSimName.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 265);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(101, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Sector Performance";
            // 
            // SimulationManagerFormNew
            // 
            this.ClientSize = new System.Drawing.Size(1657, 742);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "SimulationManagerFormNew";
            this.Text = "Simulation Manager";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.pnlMain.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.pnlSimList.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSimulationList)).EndInit();
            this.pnlSimResults.ResumeLayout(false);
            this.pnlSimResults.PerformLayout();
            this.pnlPositioningStrategySelect.ResumeLayout(false);
            this.pnlPositioningStrategySelect.PerformLayout();
            this.pnlTradingStrategySelect.ResumeLayout(false);
            this.pnlTradingStrategySelect.PerformLayout();
            this.pnlAddRemoveCopy.ResumeLayout(false);
            this.pnlAddRemoveCopy.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #region Simulation Management

        private Simulation _SelectedSimulation { get; set; }
        public Simulation SelectedSimulation
        {
            get
            {
                return _SelectedSimulation;
            }
            set
            {
                _SelectedSimulation = value;
                OnSelectedSimulationChanged();
            }
        }

        [Initializer]
        private void InitializeSimulationControls()
        {
            //
            // Create a new sim when the user presses enter in the txt box
            //
            txtAddSimName.KeyUp += (s, e) =>
            {
                if (e.KeyCode != Keys.Return)
                    return;

                // Create a simulation with the name entered
                if (txtAddSimName.Text.Length == 0)
                    return;

                CreateNewSimulation(txtAddSimName.Text);
                txtAddSimName.Clear();
            };

            //
            // Create a new sim when the user presses the Create button 
            //
            btnAddSim.Click += (s, e) =>
            {
                // Create a simulation with the name entered
                if (txtAddSimName.Text.Length == 0)
                    return;

                CreateNewSimulation(txtAddSimName.Text);
                txtAddSimName.Clear();
            };

            //
            // Copy the currently selected simulation
            //
            btnCopyCurrentSim.Click += (s, e) =>
            {
                if (SelectedSimulation == null)
                    return;

                CopySimulation(SelectedSimulation);
            };
            this.SelectedSimulationChanged += (s, e) =>
            {
                Invoke(new Action(() =>
                {
                    btnCopyCurrentSim.Enabled = (SelectedSimulation != null);
                    btnCopyCurrentSim.Refresh();
                }));
            };

            //
            // Delete the currently selected simulation
            //
            btnDeleteCurrentSim.Click += (s, e) =>
            {
                if (SelectedSimulation == null)
                    return;

                DeleteSimulation(SelectedSimulation);
            };
            this.SelectedSimulationChanged += (s, e) =>
            {
                btnDeleteCurrentSim.Enabled = (SelectedSimulation != null);
                btnDeleteCurrentSim.Refresh();
            };

            //
            // Run simulation
            //
            btnRunSelectedSim.Click += (s, e) =>
            {
                if (SelectedSimulation == null)
                    return;

                RunSimulation(SelectedSimulation);
                btnRunSelectedSim.Enabled = false;
            };
            this.SelectedSimulationChanged += (s, e) =>
            {
                this.Invoke(new Action(() =>
                {
                    if (SelectedSimulation == null || SelectedSimulation.SimulationStatus != SimulationStatus.NotStarted)
                    {
                        btnRunSelectedSim.Enabled = false;
                    }
                    else
                    {
                        btnRunSelectedSim.Enabled = true;
                    }
                }));
            };

        }

        public void SaveLayout()
        {
            Settings.Instance.SaveFormLayout(this);
        }
        public void LoadLayout()
        {
            Settings.Instance.LoadFormLayout(this);
        }

        private void CreateNewSimulation(string name)
        {
            SimulationManager.Instance.CreateSimulation(name);
        }
        private void CopySimulation(Simulation simulation)
        {
            SimulationManager.Instance.CreateSimulation(simulation);
        }
        private void DeleteSimulation(string name)
        {
            SimulationManager.Instance.RemoveSimulation(name);
        }
        private void DeleteSimulation(Simulation simulation)
        {
            SimulationManager.Instance.RemoveSimulation(simulation);
        }
        private void RunSimulation(Simulation simulation)
        {
            if (simulation.SimulationStatus == SimulationStatus.NotStarted)
                SimulationManager.Instance.Run(simulation);
        }

        #endregion

        #region Simulation Grid View

        [Initializer]
        private void InitializeGridView()
        {
            gridSimulationList.AutoGenerateColumns = false;
            gridSimulationList.Columns.Clear();
            gridSimulationList.ColumnCount = 2;
            gridSimulationList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridSimulationList.MultiSelect = false;
            gridSimulationList.RowHeadersVisible = false;
            gridSimulationList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            //
            // Col 1 - Name
            //
            gridSimulationList.Columns[0].Name = "colSimName";
            gridSimulationList.Columns[0].DataPropertyName = "Name";
            gridSimulationList.Columns[0].HeaderText = "Simulation Name";

            //
            // Col 2 - Status
            //
            gridSimulationList.Columns[1].Name = "colStatus";
            gridSimulationList.Columns[1].DataPropertyName = "SimulationStatus";
            gridSimulationList.Columns[1].HeaderText = "Status";

            gridSimulationList.DataSource = SimulationManager.Instance.Simulations;

            //
            // Update selected simulation
            //
            gridSimulationList.SelectionChanged += (s, e) =>
            {
                if (gridSimulationList.SelectedCells.Count == 0)
                    SelectedSimulation = null;
                else
                    SelectedSimulation = SimulationManager.Instance.GetSimulation(gridSimulationList.SelectedCells[0].Value.ToString());
            };

            //
            // Update status display
            //
            SimulationManager.Instance.SimulationStatusChanged += (s, e) =>
            {
                //gridSimulationList.Refresh();
            };
        }

        #endregion

        #region Strategy Selection Area

        [Initializer]
        private void InitializeStrategySelection()
        {
            bool SimChangeSuspend = false;
            this.SelectedSimulationChanged += (s, e) =>
            {
                SimChangeSuspend = true;
                if (SelectedSimulation == null)
                {
                    boxSelectedTradingStrategy.DataSource = null;
                    boxSelectedTradingStrategy.Enabled = false;
                    SimChangeSuspend = false;
                    return;
                }
                else
                {
                    boxSelectedTradingStrategy.DataSource = SelectedSimulation.StrategyManager.AllTradeStrategies;
                    boxSelectedTradingStrategy.DisplayMember = "Name";
                    boxSelectedTradingStrategy.SelectedIndex = SelectedSimulation.StrategyManager.AllTradeStrategies.IndexOf(SelectedSimulation.StrategyManager.ActiveTradeStrategy);
                    boxSelectedTradingStrategy.Refresh();
                    UpdateStrategyInfoPanel();
                    SimChangeSuspend = false;
                    boxSelectedTradingStrategy.Enabled = (SelectedSimulation.SimulationStatus == SimulationStatus.NotStarted);
                }
            };
            SimulationManager.Instance.SimulationStatusChanged += (s, e) =>
            {
                if (SelectedSimulation.SimulationStatus != SimulationStatus.NotStarted)
                {
                    Invoke(new Action(() => boxSelectedTradingStrategy.Enabled = false));
                }
            };

            boxSelectedTradingStrategy.SelectedValueChanged += (s, e) =>
            {
                if (SelectedSimulation == null || SimChangeSuspend)
                    return;

                SelectedSimulation.StrategyManager.SetStrategy(boxSelectedTradingStrategy.SelectedValue as TradeStrategyBase);
                UpdateStrategyInfoPanel();
            };

            lblTradingStrategyDescription.Width = pnlTradingStrategySelect.Width - 20;
        }

        [Initializer]
        private void InitializePositioningSelection()
        {
            bool SimChangeSuspend = false;
            this.SelectedSimulationChanged += (s, e) =>
            {
                SimChangeSuspend = true;
                if (SelectedSimulation == null)
                {
                    boxSelectedPositioningStrategy.DataSource = null;
                    boxSelectedPositioningStrategy.Enabled = false;
                    SimChangeSuspend = false;
                    return;
                }
                else
                {
                    boxSelectedPositioningStrategy.DataSource = SelectedSimulation.RiskManager.AllPositioningAndStoplossMethods;
                    boxSelectedPositioningStrategy.DisplayMember = "Name";
                    boxSelectedPositioningStrategy.SelectedIndex = SelectedSimulation.RiskManager.AllPositioningAndStoplossMethods.IndexOf(SelectedSimulation.RiskManager.ActivePositioningStrategy);
                    boxSelectedPositioningStrategy.Refresh();
                    UpdatePositioningInfoPanel();
                    SimChangeSuspend = false;
                    boxSelectedPositioningStrategy.Enabled = (SelectedSimulation.SimulationStatus == SimulationStatus.NotStarted);
                }
            };
            SimulationManager.Instance.SimulationStatusChanged += (s, e) =>
            {
                if (SelectedSimulation.SimulationStatus != SimulationStatus.NotStarted)
                {
                    Invoke(new Action(() => boxSelectedPositioningStrategy.Enabled = false));
                }
            };

            boxSelectedPositioningStrategy.SelectedValueChanged += (s, e) =>
            {
                if (SelectedSimulation == null || SimChangeSuspend)
                    return;

                SelectedSimulation.RiskManager.SetPositioningAndStoplossMethod(boxSelectedPositioningStrategy.SelectedValue as PositioningStrategyBase);
                UpdateStrategyInfoPanel();
            };

            lblTradingStrategyDescription.Width = pnlTradingStrategySelect.Width - 20;
        }

        private void UpdateStrategyInfoPanel()
        {
            Invoke(new Action(() =>
            {
                if (SelectedSimulation == null)
                {
                    lblTradingStrategyDescription.Text = "";
                    return;
                }
                lblTradingStrategyDescription.Text = SelectedSimulation.StrategyManager.ActiveTradeStrategy.Description;
                lblTradingStrategyDescription.Refresh();
            }));
        }
        private void UpdatePositioningInfoPanel()
        {
            Invoke(new Action(() =>
            {
                if (SelectedSimulation == null)
                {
                    lblPositioningStrategyDescription.Text = "";
                    return;
                }
                lblPositioningStrategyDescription.Text = SelectedSimulation.RiskManager.ActivePositioningStrategy.Description;
                lblPositioningStrategyDescription.Refresh();
            }));
        }

        #endregion

        #region Parameters Adjustment Panel

        [Initializer]
        private void InitializeParameterAdjustment()
        {
            pnlSimulationSettingsManager = new SimulationSettingsManagerPanel()
            {
                Dock = DockStyle.Fill
            };
            pnlSimSettings.Controls.Add(pnlSimulationSettingsManager);
            this.SelectedSimulationChanged += (s, e) => UpdateParameterSettingsDisplay();
            SimulationManager.Instance.SimulationStatusChanged += (s, e) =>
            {
                if (SelectedSimulation.SimulationStatus != SimulationStatus.NotStarted)
                {
                    pnlSimulationSettingsManager.Lock(true);
                }
            };
        }

        private void UpdateParameterSettingsDisplay()
        {
            if (SelectedSimulation == null)
            {
                pnlSimulationSettingsManager.Enabled = false;
                pnlSimulationSettingsManager.Hide();
            }
            else
            {
                pnlSimulationSettingsManager.Enabled = true;
                pnlSimulationSettingsManager.Show();
                pnlSimulationSettingsManager.LoadSettings(SelectedSimulation.Settings);
                pnlSimulationSettingsManager.Refresh();

                if (SelectedSimulation.SimulationStatus != SimulationStatus.NotStarted)
                {
                    pnlSimulationSettingsManager.Lock(true);
                }
            }
        }

        #endregion

        #region Simulation Results Management

        TinyFinanceSimResultsChart chartEquityResultView;
        TinySectorResultsChart chartSectorResultView;

        [Initializer]
        private void InitializeResultsControls()
        {
            //
            // View results button
            //
            btnViewResultsChart.Enabled = false;
            this.SelectedSimulationChanged += (s, e) =>
            {
                btnViewResultsChart.Enabled = (SelectedSimulation == null) ? false
                : (SelectedSimulation.SimulationStatus == SimulationStatus.Complete);
            };
            SimulationManager.Instance.SimulationStatusChanged += (s, e) =>
            {
                this.Invoke(new Action(() =>
                {
                    if (SelectedSimulation?.SimulationStatus == SimulationStatus.Complete)
                        btnViewResultsChart.Enabled = true;
                }));
            };
            btnViewResultsChart.Click += (s, e) =>
            {
                if (SelectedSimulation == null)
                    return;

                ResultViewerFormNew simResultViewer = new ResultViewerFormNew();
                simResultViewer.LoadSimulation(SelectedSimulation);
                simResultViewer.Show();
            };

            //
            // Equity chart
            //
            chartEquityResultView = new TinyFinanceSimResultsChart()
            {
                Dock = DockStyle.Fill
            };
            pnlTinyChart.Controls.Add(chartEquityResultView);

            this.SelectedSimulationChanged += (s, e) =>
            {
                if (SelectedSimulation?.SimulationStatus == SimulationStatus.Complete)
                {
                    chartEquityResultView.LoadSimulation(SelectedSimulation);
                    chartEquityResultView.Show();
                }
                else
                {
                    chartEquityResultView.Hide();
                }
            };
            SimulationManager.Instance.SimulationStatusChanged += (s, e) =>
            {
                Invoke(new Action(() =>
                {
                    if (SelectedSimulation?.SimulationStatus == SimulationStatus.Complete)
                    {
                        chartEquityResultView.LoadSimulation(SelectedSimulation);
                        chartEquityResultView.Show();
                    }
                    else
                    {
                        chartEquityResultView.Hide();
                    }
                }));
            };

            //
            // Sector chart
            //
            chartSectorResultView = new TinySectorResultsChart()
            {
                Dock = DockStyle.Fill
            };
            pnlSectorResultsChart.Controls.Add(chartSectorResultView);

            this.SelectedSimulationChanged += (s, e) =>
            {
                if (SelectedSimulation?.SimulationStatus == SimulationStatus.Complete)
                {
                    chartSectorResultView.LoadSimulation(SelectedSimulation);
                    chartSectorResultView.Show();
                }
                else
                {
                    chartSectorResultView.Hide();
                }
            };
            SimulationManager.Instance.SimulationStatusChanged += (s, e) =>
            {
                Invoke(new Action(() =>
                {
                    if (SelectedSimulation?.SimulationStatus == SimulationStatus.Complete)
                    {
                        chartSectorResultView.LoadSimulation(SelectedSimulation);
                        chartSectorResultView.Show();
                    }
                    else
                    {
                        chartSectorResultView.Hide();
                    }
                }));
            };

        }

        #endregion
    }
}