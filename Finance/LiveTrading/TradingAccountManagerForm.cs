using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Finance;
using Finance.LiveTrading;
using static Finance.Helpers;

namespace Finance.LiveTrading
{
    public class TradingAccountManagerForm : Form, IPersistLayout
    {
        private static TradingAccountManagerForm _Instance { get; set; }
        public static TradingAccountManagerForm Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new TradingAccountManagerForm();
                return _Instance;
            }
        }

        private TradingAccountManager Manager { get; }

        public bool Sizeable => false;

        private MenuStrip menuStrip1;
        private ToolStripMenuItem accountToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblProviderConnection;
        private TabControl tabMainPanel;
        private TabPage tabAccount;
        private ToolStripDropDownButton btnAccountSelect;
        private ToolStripComboBox boxAccountSelect;
        private LiveAccountSummaryPanel pnlAccountDisplay;
        private TabPage tabPositions;
        private Panel pnlPositionsMain;
        private Panel pnlPositionsSummaryMain;
        private LogOutputForm tradeLogOutputForm;
        private ToolStripMenuItem menuShowLog;
        private ToolStripMenuItem tradingToolStripMenuItem;
        private ToolStripMenuItem menuLiveQuoteWindow;
        private PositionsSummaryPanel pnlPositionSummary;

        private TradingAccountManagerForm()
        {
            InitializeLogger();
            InitializeComponent();

            this.FormClosing += (s, e) =>
            {
                this.Hide();
                e.Cancel = true;
            };
            this.Shown += (s, e) => LoadLayout();
            this.ResizeEnd += (s, e) => SaveLayout();

            Manager = TradingAccountManager.Instance;

            LiveQuoteForm.Instance.Show();

            this.InitializeMe();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TradingAccountManagerForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.accountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuShowLog = new System.Windows.Forms.ToolStripMenuItem();
            this.tradingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLiveQuoteWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblProviderConnection = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnAccountSelect = new System.Windows.Forms.ToolStripDropDownButton();
            this.boxAccountSelect = new System.Windows.Forms.ToolStripComboBox();
            this.tabMainPanel = new System.Windows.Forms.TabControl();
            this.tabAccount = new System.Windows.Forms.TabPage();
            this.pnlAccountDisplay = new Finance.LiveAccountSummaryPanel();
            this.tabPositions = new System.Windows.Forms.TabPage();
            this.pnlPositionsMain = new System.Windows.Forms.Panel();
            this.pnlPositionsSummaryMain = new System.Windows.Forms.Panel();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.tabMainPanel.SuspendLayout();
            this.tabAccount.SuspendLayout();
            this.tabPositions.SuspendLayout();
            this.pnlPositionsMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.accountToolStripMenuItem,
            this.tradingToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(773, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // accountToolStripMenuItem
            // 
            this.accountToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuShowLog});
            this.accountToolStripMenuItem.Name = "accountToolStripMenuItem";
            this.accountToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            this.accountToolStripMenuItem.Text = "Tools";
            // 
            // menuShowLog
            // 
            this.menuShowLog.Name = "menuShowLog";
            this.menuShowLog.Size = new System.Drawing.Size(126, 22);
            this.menuShowLog.Text = "Show Log";
            // 
            // tradingToolStripMenuItem
            // 
            this.tradingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuLiveQuoteWindow});
            this.tradingToolStripMenuItem.Name = "tradingToolStripMenuItem";
            this.tradingToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.tradingToolStripMenuItem.Text = "Trading";
            // 
            // menuLiveQuoteWindow
            // 
            this.menuLiveQuoteWindow.Name = "menuLiveQuoteWindow";
            this.menuLiveQuoteWindow.Size = new System.Drawing.Size(180, 22);
            this.menuLiveQuoteWindow.Text = "Live Quote Window";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblProviderConnection,
            this.btnAccountSelect});
            this.statusStrip1.Location = new System.Drawing.Point(0, 515);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(773, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblProviderConnection
            // 
            this.lblProviderConnection.Name = "lblProviderConnection";
            this.lblProviderConnection.Size = new System.Drawing.Size(54, 17);
            this.lblProviderConnection.Text = "Provider:";
            // 
            // btnAccountSelect
            // 
            this.btnAccountSelect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnAccountSelect.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.boxAccountSelect});
            this.btnAccountSelect.Image = ((System.Drawing.Image)(resources.GetObject("btnAccountSelect.Image")));
            this.btnAccountSelect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAccountSelect.Name = "btnAccountSelect";
            this.btnAccountSelect.Size = new System.Drawing.Size(65, 20);
            this.btnAccountSelect.Text = "Account";
            // 
            // boxAccountSelect
            // 
            this.boxAccountSelect.Name = "boxAccountSelect";
            this.boxAccountSelect.Size = new System.Drawing.Size(121, 23);
            // 
            // tabMainPanel
            // 
            this.tabMainPanel.Controls.Add(this.tabAccount);
            this.tabMainPanel.Controls.Add(this.tabPositions);
            this.tabMainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMainPanel.Location = new System.Drawing.Point(0, 24);
            this.tabMainPanel.Name = "tabMainPanel";
            this.tabMainPanel.SelectedIndex = 0;
            this.tabMainPanel.Size = new System.Drawing.Size(773, 491);
            this.tabMainPanel.TabIndex = 2;
            // 
            // tabAccount
            // 
            this.tabAccount.Controls.Add(this.pnlAccountDisplay);
            this.tabAccount.Location = new System.Drawing.Point(4, 22);
            this.tabAccount.Name = "tabAccount";
            this.tabAccount.Size = new System.Drawing.Size(765, 465);
            this.tabAccount.TabIndex = 0;
            this.tabAccount.Text = "Account";
            this.tabAccount.UseVisualStyleBackColor = true;
            // 
            // pnlAccountDisplay
            // 
            this.pnlAccountDisplay.Location = new System.Drawing.Point(8, 3);
            this.pnlAccountDisplay.Name = "pnlAccountDisplay";
            this.pnlAccountDisplay.Size = new System.Drawing.Size(300, 459);
            this.pnlAccountDisplay.TabIndex = 0;
            // 
            // tabPositions
            // 
            this.tabPositions.Controls.Add(this.pnlPositionsMain);
            this.tabPositions.Location = new System.Drawing.Point(4, 22);
            this.tabPositions.Name = "tabPositions";
            this.tabPositions.Padding = new System.Windows.Forms.Padding(3);
            this.tabPositions.Size = new System.Drawing.Size(765, 465);
            this.tabPositions.TabIndex = 1;
            this.tabPositions.Text = "Positions";
            this.tabPositions.UseVisualStyleBackColor = true;
            // 
            // pnlPositionsMain
            // 
            this.pnlPositionsMain.Controls.Add(this.pnlPositionsSummaryMain);
            this.pnlPositionsMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPositionsMain.Location = new System.Drawing.Point(3, 3);
            this.pnlPositionsMain.Name = "pnlPositionsMain";
            this.pnlPositionsMain.Size = new System.Drawing.Size(759, 459);
            this.pnlPositionsMain.TabIndex = 0;
            // 
            // pnlPositionsSummaryMain
            // 
            this.pnlPositionsSummaryMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlPositionsSummaryMain.Location = new System.Drawing.Point(0, 0);
            this.pnlPositionsSummaryMain.Name = "pnlPositionsSummaryMain";
            this.pnlPositionsSummaryMain.Size = new System.Drawing.Size(759, 212);
            this.pnlPositionsSummaryMain.TabIndex = 0;
            // 
            // TradingAccountManagerForm
            // 
            this.ClientSize = new System.Drawing.Size(773, 537);
            this.Controls.Add(this.tabMainPanel);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "TradingAccountManagerForm";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabMainPanel.ResumeLayout(false);
            this.tabAccount.ResumeLayout(false);
            this.tabPositions.ResumeLayout(false);
            this.pnlPositionsMain.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void InitializeLogger()
        {
            if (tradeLogOutputForm != null && !tradeLogOutputForm.IsDisposed)
            {
                tradeLogOutputForm.Show();
                return;
            }

            LogMessageType[] msgs = new LogMessageType[]
            {
                 LogMessageType.TradingError, LogMessageType.TradingNotification, LogMessageType.TradingSystemMessage
            };
            tradeLogOutputForm = new LogOutputForm(msgs.ToList(), "Trading Log");
            tradeLogOutputForm.Show();
        }

        [Initializer]
        private void InitializeHandlers()
        {
            //
            // menuShowLog
            //
            menuShowLog.Click += (s, e) =>
            {
                InitializeLogger();
            };

            //
            // menuTradeEntryWindow
            //
            menuLiveQuoteWindow.Click += (s, e) =>
            {
                LiveQuoteForm.Instance.Show();
            };

        }

        [Initializer]
        private void InitializeControls()
        {
            //
            // lblProviderConnection
            //
            Manager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Connected")
                    UpdateConnectionStatusLabel(Manager.Connected);
            };
            UpdateConnectionStatusLabel(Manager.Connected);

            //
            // Account Selector
            //
            Manager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "ProviderAccountNumbers")
                {
                    UpdateAccountSelector(Manager.ProviderAccountNumbers);
                }
            };
            boxAccountSelect.ComboBox.SelectedValueChanged += (s, e) =>
            {
                Manager.SelectActiveAccount(boxAccountSelect.ComboBox.SelectedItem as string);
            };
            Manager.ActiveAccountChanged += (s, e) =>
            {
                Invoke(new Action(() =>
                {
                    btnAccountSelect.Text = e.Account.AccountId;
                    Refresh();
                }));
            };
            Manager.ActiveAccountChanged += (s, e) =>
            {
                Invoke(new Action(() =>
                {
                    pnlAccountDisplay.LoadAccount(e.Account);
                }));
            };

            //
            // Position Summary Panel
            //
            pnlPositionSummary = new PositionsSummaryPanel()
            {
                Dock = DockStyle.Fill
            };
            pnlPositionsSummaryMain.Controls.Add(pnlPositionSummary);
            Manager.ActiveAccountChanged += (s, e) =>
            {
                Invoke(new Action(() => { UpdatePositionSummary(); }));
            };
            pnlPositionSummary.SelectedPositionChanged += (s, e) =>
            {
                Manager.SetActivePosition(e.Position);
            };

        }

        private void UpdateConnectionStatusLabel(bool connected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    UpdateConnectionStatusLabel(connected);
                }));
                return;
            }

            switch (Manager.Connected)
            {
                case true:
                    lblProviderConnection.Text = $"Provider: CONNECTED";
                    lblProviderConnection.BackColor = Color.Green;
                    break;
                case false:
                    lblProviderConnection.Text = $"Provider: DISCONNECTED";
                    lblProviderConnection.BackColor = Color.Red;
                    break;
            }
        }
        private void UpdateAccountSelector(List<string> accountIds)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    UpdateAccountSelector(accountIds);
                }));
                return;
            }

            string selectedAccount = string.Empty;
            if (boxAccountSelect.ComboBox.SelectedIndex > -1)
                selectedAccount = boxAccountSelect.ComboBox.SelectedItem as string;

            boxAccountSelect.ComboBox.Items.Clear();

            if (accountIds.Count == 0)
                return;

            foreach (var id in accountIds)
                boxAccountSelect.ComboBox.Items.Add(id);

            Refresh();

            if (selectedAccount != string.Empty)
                boxAccountSelect.SelectedItem = selectedAccount;
            else
                boxAccountSelect.SelectedIndex = 0;

            btnAccountSelect.Text = boxAccountSelect.SelectedItem as string;

        }
        private void UpdatePositionSummary()
        {
            pnlPositionSummary.LoadAccount(Manager.ActiveAccount);
        }

        public void SaveLayout()
        {
            Settings.Instance.SaveFormLayout(this);
        }
        public void LoadLayout()
        {
            Settings.Instance.LoadFormLayout(this);
        }
    }
}
