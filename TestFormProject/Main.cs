using Finance;
using Finance.UI;
using System.Drawing;
using System.Windows.Forms;
using static Finance.Logger;

namespace TestFormProject
{
    public partial class Main : Form
    {
        #region Interface Screens

        SecurityManagerUI secManagerUI;
        SimulationManagerUI simManagerUI;
        LogOutputUI logOutputUI;

        #endregion

        #region Form Controls

        MenuStrip menuStrip;
        ToolStripMenuItem menuData;
        ToolStripMenuItem menuSimulations;
        ToolStripMenuItem menuConnect;
        ToolStripMenuItem menuReconnect;
        ToolStripMenuItem menuShowDataManager;
        ToolStripMenuItem menuShowSimulationManager;

        #endregion

        public MasterController MasterController { get; private set; } = new MasterController();

        public Main()
        {
            InitializeComponent();

            // Main form properties
            Text = "Main";
            FormBorderStyle = FormBorderStyle.FixedSingle;

            // Initialize components
            this.InitializeMe();

            FormClosing += (s, e) =>
            {
                if (logOutputUI != null)
                    logOutputUI.Close();
            };
        }

        #region Initializers

        [Initializer]
        private void InitializeMenuStrip()
        {

            // Menu Strip
            menuStrip = new MenuStrip() { Name = "menuStrip" };
            Controls.Add(menuStrip);

            // Data Menu
            menuData = new ToolStripMenuItem("Data");
            menuStrip.Items.Add(menuData);

            // Simulations Menu
            menuSimulations = new ToolStripMenuItem("Simulations");
            menuStrip.Items.Add(menuSimulations);

            #region Data -> Connect

            menuConnect = new ToolStripMenuItem("Connect");
            menuConnect.Click += (s, e) =>
            {
                Log(new LogMessage(Name, "User clicked CONNECT", LogMessageType.Debug));

                if (!MasterController.Initialized)
                {
                    MessageBox.Show("Cannot connect: Master Controller not initialized");
                    return;
                }

                menuConnect.Enabled = false;
                MasterController.ConnectDataProvider();
            };
            MasterController.DataManagerStatusChange += (s, e) =>
            {
                if (!e.DataproviderConnected)
                    menuConnect.Enabled = true;
                else
                    menuConnect.Enabled = false;

                Log(new LogMessage(Name, $"Connection Status Changed to {(e.DataproviderConnected ? "CONNECTED" : "DISCONNECTED")}", LogMessageType.Production));
            };
            menuData.DropDownItems.Add(menuConnect);

            #endregion

            #region Data -> Reconnect

            menuReconnect = new ToolStripMenuItem("Reconnect")
            {
                Enabled = false
            };
            menuReconnect.Click += (s, e) =>
            {
                Log(new LogMessage(Name, "User clicked RECONNECT", LogMessageType.Debug));

                if (!MasterController.Initialized)
                {
                    MessageBox.Show("Cannot connect: Master Controller not initialized");
                    return;
                }

                menuReconnect.Enabled = false;
                MasterController.ReconnectDataProvider();
            };
            MasterController.DataManagerStatusChange += (s, e) =>
            {
                if (!e.DataproviderConnected)
                    menuReconnect.Enabled = false;
                else
                    menuReconnect.Enabled = true;

                Log(new LogMessage(Name, $"Connection Status Changed to {(e.DataproviderConnected ? "CONNECTED" : "DISCONNECTED")}", LogMessageType.Production));
            };
            menuData.DropDownItems.Add(menuReconnect);

            #endregion

            menuData.DropDownItems.Add(new ToolStripSeparator());

            #region Data -> Security Manager

            menuShowDataManager = new ToolStripMenuItem("Data Manager") { Enabled = false };
            menuShowDataManager.Click += (s, e) =>
            {
                Log(new LogMessage(Name, "User opened Security Manager window", LogMessageType.Debug));

                if (secManagerUI == null || secManagerUI.IsDisposed)
                    secManagerUI = new SecurityManagerUI(MasterController.DataManager);

                Cursor = Cursors.WaitCursor;
                secManagerUI.StartPosition = FormStartPosition.Manual;
                var startPosition = Location;
                startPosition.Offset(0, Height - 8);
                secManagerUI.Location = startPosition;

                secManagerUI.Show();
                Cursor = Cursors.Default;
            };
            MasterController.DataManagerStatusChange += (s, e) =>
            {
                if (e.DatabaseConnected && e.DataproviderConnected)
                    menuShowDataManager.Enabled = true;
                else
                    menuShowDataManager.Enabled = false;

                Log(new LogMessage(Name, $"Data Manager Status Changed: Database: {(e.DatabaseConnected ? "YES" : "NO")} " +
                    $"Provider: {(e.DataproviderConnected ? "YES" : "NO")}", LogMessageType.Production));
            };
            menuData.DropDownItems.Add(menuShowDataManager);

            #endregion

            #region Simulations -> Simulation Manager

            menuShowSimulationManager = new ToolStripMenuItem("Simulation Manager") { Enabled = false };
            MasterController.DataManagerStatusChange += (s, e) =>
            {
                //
                // Disable menu item if the data manager is disconnected
                //

                if (e.DatabaseConnected && e.DataproviderConnected)
                    menuShowSimulationManager.Enabled = true;
                else
                    menuShowSimulationManager.Enabled = false;
            };
            menuShowSimulationManager.Click += (s, e) =>
            {
                //
                // Launch the simulation manager
                //

                Log(new LogMessage(Name, "User opened Simulation Manager window", LogMessageType.Debug));

                if (simManagerUI == null || simManagerUI.IsDisposed)
                    simManagerUI = new SimulationManagerUI(MasterController.DataManager, MasterController.SimulationManager);

                simManagerUI.Show();
            };
            menuSimulations.DropDownItems.Add(menuShowSimulationManager);

            #endregion
        }

        [Initializer]
        private void InitializeStatusDisplay()
        {
            //
            // Status Display Panel which will contain status indicator controls
            //
            pnlStatusDisplay = new Panel
            {
                Name = "pnlStatusDisplay",
                Dock = DockStyle.Fill
            };
            pnlStatusDisplay.ControlAdded += (s, e) =>
            {
                //
                // Panel and form will resize as status indicator controls are added
                //
                int offset = 0;
                foreach (Control ctrl in pnlStatusDisplay.Controls)
                {
                    ctrl.Height = 30;
                    ctrl.Location = new Point(0, offset);
                    ctrl.Width = pnlStatusDisplay.Width;
                    offset += ctrl.Height;
                }

                ClientSize = new Size(ClientSize.Width, menuStrip.Height + offset);
            };
            Controls.Add(pnlStatusDisplay);
            pnlStatusDisplay.BringToFront();

            //
            // Add Status Indicator Controls
            //
            pnlStatusDisplay.Controls.Add(MasterController.DataManager.StatusIndicator);
            pnlStatusDisplay.Controls.Add(MasterController.DataManager.DataProvider.StatusIndicator);
            pnlStatusDisplay.Controls.Add(MasterController.SimulationManager.StatusIndicator);
            pnlStatusDisplay.Controls.Add(MasterController.DailyUpdateStatusIndicator);
        }

        [Initializer]
        private void InitializeLogger()
        {
            logOutputUI = new LogOutputUI();
            logOutputUI.Show();
        }

        #endregion


    }
}
