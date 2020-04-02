using Finance;
using Finance.Data;
using Finance.LiveTrading;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using static Finance.Logger;
using static Finance.Helpers;

namespace TestFormProject
{
    public partial class Main : Form, IPersistLayout
    {

        #region Child Forms

        LogOutputForm SystemLogForm;

        #endregion

        #region Form Controls

        #endregion

        public bool Sizeable => false;

        public Main()
        {
            InitializeComponent();
            this.InitializeMe();
        }

        #region Initializers

        [Initializer]
        private void InitializeStyles()
        {
            Text = "Main";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            TopMost = true;

            this.Shown += (s, e) => LoadLayout();
            this.ResizeEnd += (s, e) => SaveLayout();
            this.FormClosing += (s, e) =>
            {
                if (SystemLogForm != null)
                    SystemLogForm.Close();
            };

            bool focusFlag = true;
            this.Activated += (s, e) =>
            {
                if (focusFlag)
                {
                    foreach (Form form in Application.OpenForms)
                    {
                        if (form == this) continue;
                        form.BringToFront();
                    }
                    focusFlag = false;
                    this.Focus();
                }
                else
                    focusFlag = true;
            };
        }
        [Initializer]
        private void InitializeEventManager()
        {
            EventManager.Instance.Initialize();
        }
        [Initializer]
        private void InitializeMenuStrip()
        {

            menuShowSecurityManager.Click += (s, e) => SecurityManagerForm.Instance.Show();

            menuShowSimulationManager.Click += (s, e) => SimulationManagerForm.Instance.Show();

            menuShowSettings.Click += (s, e) => SettingsManagerForm.Instance.Show();

            menuShowTradeManager.Click += (s, e) => LiveTradingManagerForm.Instance.Show();

            menuCalculator.Click += (s, e) => Process.Start("calc");

        }
        [Initializer]
        private void InitializeLogger()
        {
            //
            // Define the message group displayed in this logger
            //
            LogMessageType[] msgs = new LogMessageType[]
            {
                 LogMessageType.Debug, LogMessageType.Production, LogMessageType.SecurityError, LogMessageType.SystemError
            };
            SystemLogForm = new LogOutputForm(msgs.ToList(), "System Log");
            SystemLogForm.Show();
        }
        [Initializer]
        private void InitializeProviderMonitors()
        {
            pnlProviderMonitors.Controls.Add(new ProviderStatusPanel(RefDataManager.Instance));
            pnlProviderMonitors.Controls.Add(new ProviderStatusPanel(LiveDataProvider.Instance));
            pnlProviderMonitors.Controls.Add(new ProviderStatusPanel(LiveTradingProvider.Instance));
        }
        [Initializer]
        private void InitializeSystemModeIndicator()
        {
            //
            // Initialize window banner indicting system mode (testing or production)
            //
            foreach (Screen screen in Screen.AllScreens)
                new SystemModeIndicatorForm(screen).Show();
        }
        [Initializer]
        private void InitializeDefaultWindows()
        {
            //
            // World Clock
            //
            SystemClock.Instance.Show();

            //
            // Trading Manager
            //
            LiveTradingManagerForm.Instance.Show();

            //
            // Live Security Quote Screen
            //
            LiveQuoteForm.Instance.Show();

            //
            // Trade Entry Form
            //
            LiveTradeEntryForm.Instance.Show();

            //
            // Single Security Indicators
            //
            SingleSecurityIndicatorForm.Instance.Show();

            //
            // SCRAM
            //
            SCRAM.Instance.Show();
        }

        public void SaveLayout()
        {
            Settings.Instance.SaveFormLayout(this);
        }
        public void LoadLayout()
        {
            Settings.Instance.LoadFormLayout(this);
        }

        #endregion

    }
}
