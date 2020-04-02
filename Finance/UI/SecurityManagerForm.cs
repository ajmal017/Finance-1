using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Finance;
using Finance.Data;
using static Finance.Logger;
using static Finance.Helpers;

namespace Finance
{
    public class SecurityManagerForm : Form, IPersistLayout
    {
        private static SecurityManagerForm _Instance { get; set; }
        public static SecurityManagerForm Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new SecurityManagerForm();
                }
                return _Instance;
            }
        }

        #region Events

        public event SelectedSecurityChangedEventHandler SelectedSecurityChanged;
        protected void OnSelectedSecurityChanged()
        {
            SelectedSecurityChanged?.Invoke(this, new SelectedSecurityEventArgs(this.SelectedSecurity));
        }

        #endregion

        public Security SelectedSecurity => securityListGrid1.SelectedSecurity;
        public bool Sizeable => false;

        private SecurityInfoPanelNew securityInfoPanelNew1;
        private TextBox txtAddSymbol;
        private Button btnAddSymbol;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem menuRequestProviderSymbols;
        private ToolStripMenuItem menuUpdateCompanyInfo;
        private ToolStripMenuItem toolsToolStripMenuItem1;
        private ToolStripMenuItem menuRemoveExclusions;
        private ToolStripMenuItem menuCleanZeroValues;
        private ToolStripMenuItem menuRequestSectors;
        private ToolStripMenuItem indicesToolStripMenuItem;
        private ToolStripMenuItem menuUpdateIndices;
        private ToolStripMenuItem menuTrendMonitor;
        private SecurityListGrid securityListGrid1;
        private ToolStripMenuItem btnUpdateAllSecurities;
        private ToolStripSeparator toolStripSeparator1;
        private SecurityFilterBox securityFilterBox1;
        private ToolStripMenuItem menuRequestMissingData;
        private ToolStripMenuItem menuReloadSecurityList;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem menuRemoveDuplicates;

        private SecurityManagerForm()
        {
            InitializeComponent();
            this.InitializeMe();

            this.Shown += (s, e) =>
            {
                LoadLayout();
                securityListGrid1.LoadSecurityList();
                securityFilterBox1.LoadFilterValues();
            };
            this.ResizeEnd += (s, e) => SaveLayout();
            this.FormClosing += (s, e) =>
            {
                this.Hide();
                e.Cancel = true;
            };
        }

        private void InitializeComponent()
        {
            this.txtAddSymbol = new System.Windows.Forms.TextBox();
            this.btnAddSymbol = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnUpdateAllSecurities = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuRequestProviderSymbols = new System.Windows.Forms.ToolStripMenuItem();
            this.menuUpdateCompanyInfo = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRequestSectors = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.menuReloadSecurityList = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuRemoveExclusions = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCleanZeroValues = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRemoveDuplicates = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRequestMissingData = new System.Windows.Forms.ToolStripMenuItem();
            this.indicesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuUpdateIndices = new System.Windows.Forms.ToolStripMenuItem();
            this.menuTrendMonitor = new System.Windows.Forms.ToolStripMenuItem();
            this.securityFilterBox1 = new Finance.SecurityFilterBox();
            this.securityListGrid1 = new Finance.SecurityListGrid();
            this.securityInfoPanelNew1 = new Finance.SecurityInfoPanelNew();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtAddSymbol
            // 
            this.txtAddSymbol.Location = new System.Drawing.Point(13, 364);
            this.txtAddSymbol.Name = "txtAddSymbol";
            this.txtAddSymbol.Size = new System.Drawing.Size(83, 20);
            this.txtAddSymbol.TabIndex = 6;
            // 
            // btnAddSymbol
            // 
            this.btnAddSymbol.Location = new System.Drawing.Point(102, 364);
            this.btnAddSymbol.Name = "btnAddSymbol";
            this.btnAddSymbol.Size = new System.Drawing.Size(74, 20);
            this.btnAddSymbol.TabIndex = 7;
            this.btnAddSymbol.Text = "Add Symbol";
            this.btnAddSymbol.UseVisualStyleBackColor = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolsToolStripMenuItem,
            this.toolsToolStripMenuItem1,
            this.indicesToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1469, 24);
            this.menuStrip1.TabIndex = 9;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnUpdateAllSecurities,
            this.toolStripSeparator1,
            this.menuRequestProviderSymbols,
            this.menuUpdateCompanyInfo,
            this.menuRequestSectors});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(63, 20);
            this.toolsToolStripMenuItem.Text = "Provider";
            // 
            // btnUpdateAllSecurities
            // 
            this.btnUpdateAllSecurities.Name = "btnUpdateAllSecurities";
            this.btnUpdateAllSecurities.Size = new System.Drawing.Size(211, 22);
            this.btnUpdateAllSecurities.Text = "Update All Securities";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(208, 6);
            // 
            // menuRequestProviderSymbols
            // 
            this.menuRequestProviderSymbols.Name = "menuRequestProviderSymbols";
            this.menuRequestProviderSymbols.Size = new System.Drawing.Size(211, 22);
            this.menuRequestProviderSymbols.Text = "Request Provider Symbols";
            // 
            // menuUpdateCompanyInfo
            // 
            this.menuUpdateCompanyInfo.Name = "menuUpdateCompanyInfo";
            this.menuUpdateCompanyInfo.Size = new System.Drawing.Size(211, 22);
            this.menuUpdateCompanyInfo.Text = "Request Company Data";
            // 
            // menuRequestSectors
            // 
            this.menuRequestSectors.Name = "menuRequestSectors";
            this.menuRequestSectors.Size = new System.Drawing.Size(211, 22);
            this.menuRequestSectors.Text = "Request Sector List";
            // 
            // toolsToolStripMenuItem1
            // 
            this.toolsToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuReloadSecurityList,
            this.toolStripSeparator2,
            this.menuRemoveExclusions,
            this.menuCleanZeroValues,
            this.menuRemoveDuplicates,
            this.menuRequestMissingData});
            this.toolsToolStripMenuItem1.Name = "toolsToolStripMenuItem1";
            this.toolsToolStripMenuItem1.Size = new System.Drawing.Size(47, 20);
            this.toolsToolStripMenuItem1.Text = "Tools";
            // 
            // menuReloadSecurityList
            // 
            this.menuReloadSecurityList.Name = "menuReloadSecurityList";
            this.menuReloadSecurityList.Size = new System.Drawing.Size(195, 22);
            this.menuReloadSecurityList.Text = "Reload Security List";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(192, 6);
            // 
            // menuRemoveExclusions
            // 
            this.menuRemoveExclusions.Name = "menuRemoveExclusions";
            this.menuRemoveExclusions.Size = new System.Drawing.Size(195, 22);
            this.menuRemoveExclusions.Text = "Remove All Exclusions";
            // 
            // menuCleanZeroValues
            // 
            this.menuCleanZeroValues.Name = "menuCleanZeroValues";
            this.menuCleanZeroValues.Size = new System.Drawing.Size(195, 22);
            this.menuCleanZeroValues.Text = "Clean Zero Values";
            // 
            // menuRemoveDuplicates
            // 
            this.menuRemoveDuplicates.Name = "menuRemoveDuplicates";
            this.menuRemoveDuplicates.Size = new System.Drawing.Size(195, 22);
            this.menuRemoveDuplicates.Text = "Remove Duplicate Bars";
            // 
            // menuRequestMissingData
            // 
            this.menuRequestMissingData.Name = "menuRequestMissingData";
            this.menuRequestMissingData.Size = new System.Drawing.Size(195, 22);
            this.menuRequestMissingData.Text = "Request Missing Data";
            // 
            // indicesToolStripMenuItem
            // 
            this.indicesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuUpdateIndices,
            this.menuTrendMonitor});
            this.indicesToolStripMenuItem.Name = "indicesToolStripMenuItem";
            this.indicesToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.indicesToolStripMenuItem.Text = "Indices";
            // 
            // menuUpdateIndices
            // 
            this.menuUpdateIndices.Name = "menuUpdateIndices";
            this.menuUpdateIndices.Size = new System.Drawing.Size(152, 22);
            this.menuUpdateIndices.Text = "Update Indices";
            // 
            // menuTrendMonitor
            // 
            this.menuTrendMonitor.Name = "menuTrendMonitor";
            this.menuTrendMonitor.Size = new System.Drawing.Size(152, 22);
            this.menuTrendMonitor.Text = "Trend Monitor";
            // 
            // securityFilterBox1
            // 
            this.securityFilterBox1.Location = new System.Drawing.Point(13, 390);
            this.securityFilterBox1.Name = "securityFilterBox1";
            this.securityFilterBox1.Size = new System.Drawing.Size(953, 491);
            this.securityFilterBox1.TabIndex = 11;
            // 
            // securityListGrid1
            // 
            this.securityListGrid1.Location = new System.Drawing.Point(12, 27);
            this.securityListGrid1.Name = "securityListGrid1";
            this.securityListGrid1.Size = new System.Drawing.Size(1445, 325);
            this.securityListGrid1.TabIndex = 10;
            // 
            // securityInfoPanelNew1
            // 
            this.securityInfoPanelNew1.Location = new System.Drawing.Point(972, 364);
            this.securityInfoPanelNew1.Name = "securityInfoPanelNew1";
            this.securityInfoPanelNew1.ShowControls = true;
            this.securityInfoPanelNew1.Size = new System.Drawing.Size(485, 380);
            this.securityInfoPanelNew1.TabIndex = 4;
            // 
            // SecurityManagerForm
            // 
            this.ClientSize = new System.Drawing.Size(1469, 893);
            this.Controls.Add(this.securityFilterBox1);
            this.Controls.Add(this.securityListGrid1);
            this.Controls.Add(this.btnAddSymbol);
            this.Controls.Add(this.txtAddSymbol);
            this.Controls.Add(this.securityInfoPanelNew1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "SecurityManagerForm";
            this.Text = " ";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        [Initializer]
        private void InitializeHandlers()
        {
            RefDataManager.Instance.SecurityListLoaded += (s, e) =>
            {
                if (this.Created)
                {
                    Invoke(new Action(() =>
                    {
                        securityListGrid1.LoadSecurityList();
                        securityFilterBox1.LoadFilterValues();
                    }));
                }
            };
            securityListGrid1.SelectedSecurityChanged += (s, e) =>
                {
                    securityInfoPanelNew1.LoadSecurity(this.SelectedSecurity);
                    this.OnSelectedSecurityChanged();
                };
            securityFilterBox1.SelectedFiltersChanged += (s, e) =>
                    {
                        Invoke(new Action(() =>
                        {
                            securityListGrid1.FilterSecurityList(securityFilterBox1.ActiveFilters);
                        }));
                    };
        }

        [Initializer]
        private void InitializeControls()
        {
            //
            // btnUpdateAllSecurities
            //
            btnUpdateAllSecurities.BackColor = Color.Orange;
            btnUpdateAllSecurities.Click += (s, e) =>
            {
                if (RefDataManager.Instance.Status == ControlStatus.Ready)
                    RefDataManager.Instance.UpdateAllPriceData(DateTime.Today);
                else
                    Logger.Log(new LogMessage("SecMgr", "RefDataProvider not ready", LogMessageType.SystemError));
            };

            //
            // menuRequestProviderSymbols
            //
            menuRequestProviderSymbols.Click += (s, e) =>
            {
                if (RefDataManager.Instance.Status == ControlStatus.Ready)
                    RefDataManager.Instance.RequestProviderSupportedSymbols();
                else
                    Logger.Log(new LogMessage("SecMgr", "RefDataProvider not ready", LogMessageType.SystemError));
            };

            //
            // btnAddSymbol
            //
            btnAddSymbol.Click += (s, e) =>
            {
                if (RefDataManager.Instance.Status == ControlStatus.Ready)
                {
                    var symbol = txtAddSymbol.Text.ToUpper();
                    txtAddSymbol.Clear();
                    new Task(() => RefDataManager.Instance.LoadSymbol(symbol)).Start();
                }
                else
                    Logger.Log(new LogMessage("SecMgr", "RefDataProvider not ready", LogMessageType.SystemError));
            };

            //
            // menuUpdateCompanyInfo
            //
            menuUpdateCompanyInfo.Click += (s, e) =>
            {
                if (RefDataManager.Instance.Status == ControlStatus.Ready)
                    RefDataManager.Instance.RequestCompanyInfoAll();
                else
                    Logger.Log(new LogMessage("SecMgr", "RefDataProvider not ready", LogMessageType.SystemError));
            };

            //
            // menuUpdateCompanyInfo
            //
            menuRequestSectors.Click += (s, e) =>
            {
                if (RefDataManager.Instance.Status == ControlStatus.Ready)
                    RefDataManager.Instance.RequestProviderSectors();
                else
                    Logger.Log(new LogMessage("SecMgr", "RefDataProvider not ready", LogMessageType.SystemError));
            };

            //
            // menuRemoveExclusions
            //
            menuRemoveExclusions.Click += (s, e) =>
            {
                if (RefDataManager.Instance.Status == ControlStatus.Ready)
                    if (MessageBox.Show("Remove all applied exclusions?", "Remove Exclusions", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        RefDataManager.Instance.RemoveAllExclusions();
                    }
                    else
                        Logger.Log(new LogMessage("SecMgr", "RefDataProvider not ready", LogMessageType.SystemError));
            };

            //
            // menuCleanZeroValues
            //
            menuCleanZeroValues.Click += (s, e) =>
            {
                if (RefDataManager.Instance.Status == ControlStatus.Ready)
                    RefDataManager.Instance.CleanZeroValues();
                else
                    Logger.Log(new LogMessage("SecMgr", "RefDataProvider not ready", LogMessageType.SystemError));
            };

            //
            // menuUpdateIndices
            //
            menuUpdateIndices.Click += (s, e) =>
            {
                IndexManager.Instance.UpdateAllIndices();
            };

            //
            // menuUpdateIndices
            //
            menuTrendMonitor.Click += (s, e) =>
            {
                MarketTrendMonitorForm.Instance.Show();
            };

            //
            // menuRequestMissingData
            //
            menuRequestMissingData.Click += (s, e) =>
            {
                if (RefDataManager.Instance.Status == ControlStatus.Ready)
                    RefDataManager.Instance.UpdateAllMissingPriceData();
                else
                    Logger.Log(new LogMessage("SecMgr", "RefDataProvider not ready", LogMessageType.SystemError));
            };

            //
            // menuReloadSecurityList
            //
            menuReloadSecurityList.Click += (s, e) =>
            {
                if (RefDataManager.Instance.Status == ControlStatus.Ready)
                    RefDataManager.Instance.LoadSecurityList();
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
    }
}
