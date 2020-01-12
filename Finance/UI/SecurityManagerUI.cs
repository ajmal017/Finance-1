using Finance;
using Finance.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Finance.Helpers;

namespace Finance.UI
{
    public partial class SecurityManagerUI : Form
    {

        private DataManager dataManager;

        public SecurityManagerUI(DataManager manager)
        {
            dataManager = manager ?? throw new ArgumentNullException(nameof(manager));

            if (manager.ProviderConnected == false)
            {
                throw new DataproviderConnectionException();
            }

            InitializeComponent();

            FormBorderStyle = FormBorderStyle.FixedSingle;

            // Call Initializer
            this.InitializeMe();

            new Task(() =>
            {
                //UpdateDatabaseMetrics();
            }).Start();

            //
            // Resize form to show all added controls
            //
            Size _sizeNonClient = Size.Subtract(Size, ClientSize);
            foreach (Control ctrl in Controls)
            {
                if (ctrl.Right > ClientRectangle.Width)
                    Width = ctrl.Right + _sizeNonClient.Width;
                if (ctrl.Bottom > ClientRectangle.Height)
                    Height = ctrl.Bottom + _sizeNonClient.Height;
            }
            Refresh();

            BringToFront();
        }

        #region Layout

        Panel pnlSecurities;

        [Initializer]
        private void InitializeLayout()
        {
            pnlSecurities = new Panel
            {
                Size = new Size(250, 250),
                Location = new Point(5, 5)
            };
            pnlSecurities.ControlAdded += (s, e) =>
            {
                if (e.Control.Right > pnlSecurities.Width)
                    pnlSecurities.Width = e.Control.Right;
                if (e.Control.Bottom > pnlSecurities.Height)
                    pnlSecurities.Height = e.Control.Bottom;
            };

            Controls.Add(pnlSecurities);
        }

        #endregion
        #region Security Info Display Area

        SecurityListBox lbSecurityListBox;
        SecurityInfoPanel pnlSecurityInfo;

        [Initializer]
        private void InitializeSecurityListBox()
        {
            lbSecurityListBox = dataManager.SecurityListBox as SecurityListBox;
            lbSecurityListBox.Height = pnlSecurities.Height;
            lbSecurityListBox.SelectedValueChanged += (s, e) =>
            {
                pnlSecurityInfo.Load(lbSecurityListBox.SelectedSecurity);
            };

            pnlSecurities.Controls.Add(lbSecurityListBox);
        }

        [Initializer]
        private void InitializeSecurityDataPanel()
        {
            pnlSecurityInfo = new SecurityInfoPanel(null, dataManager);

            pnlSecurityInfo.DockTo(lbSecurityListBox, DockSide.Right, 5);
            pnlSecurities.Controls.Add(pnlSecurityInfo);
        }

        private Security GetSelectedSecurity()
        {
            if (lbSecurityListBox.SelectedIndex != -1)
                return lbSecurityListBox.SelectedSecurity;
            return null;
        }

        #endregion
        #region Database Control Area       

        Panel pnlDatabaseControls;
        GroupBox grpDatabaseInfo;
        Label lblDatabaseInfo;
        Button btnUpdateAllSecurities;
        Button btnImportSymbols;
        Button btnCleanSymbols;

        [Initializer]
        private void InitializeDatabaseInfo()
        {
            grpDatabaseInfo = new GroupBox();
            pnlDatabaseControls = new Panel();
            lblDatabaseInfo = new Label();

            //
            // Control Panel
            //
            pnlDatabaseControls.Width = pnlSecurities.Width;
            pnlDatabaseControls.Height = 200;
            pnlDatabaseControls.DockTo(pnlSecurities, DockSide.Bottom, 5);
            Controls.Add(pnlDatabaseControls);

            //
            // grpDatabaseInfo
            //
            grpDatabaseInfo.Name = "grpDatabaseInfo";
            grpDatabaseInfo.Text = "Database";
            grpDatabaseInfo.Width = 175;
            grpDatabaseInfo.Height = 100;
            grpDatabaseInfo.Location = new Point(2, 2);
            pnlDatabaseControls.Controls.Add(grpDatabaseInfo);

            //
            // Database Info contents
            //
            Action RefreshDatabaseInfo = new Action(() =>
            {
                // Write number of securities and number of out-of-date securities to label
                int numSecurities = dataManager.GetAllSecurities().Count;
                int numOutOfDate = (from sec in dataManager.GetAllSecurities()
                                    where !sec.DataUpToDate
                                    select sec).Count();

                string text = string.Format($"Security Count: {numSecurities:###,###}{Environment.NewLine}" +
                    $"Out-Of-Date:    {numOutOfDate:###,###}");

                lblDatabaseInfo.Text = text;
                Refresh();
            });
            lblDatabaseInfo.Name = "lnlDatabaseInfo";
            lblDatabaseInfo.Font = Helpers.SystemFont(8);
            lblDatabaseInfo.Width = grpDatabaseInfo.Width - 20;
            lblDatabaseInfo.Location = new Point(10, 20);
            dataManager.SecurityListLoaded += (s, e) =>
            {
                if (Visible) Invoke(RefreshDatabaseInfo);
            };
            grpDatabaseInfo.Controls.Add(lblDatabaseInfo);
            RefreshDatabaseInfo();

        }

        [Initializer]
        private void InitializeDataMaintControls()
        {
            btnUpdateAllSecurities = new Button();
            btnImportSymbols = new Button();
            btnCleanSymbols = new Button();

            Size _btnSize = new Size(175, 25);

            //
            // btnUpdateAllSeurities
            //
            btnUpdateAllSecurities.Name = "btnUpdateAllSecurities";
            btnUpdateAllSecurities.Text = "Update All";
            btnUpdateAllSecurities.BackColor = Color.Orange;
            btnUpdateAllSecurities.Size = _btnSize;
            btnUpdateAllSecurities.Click += (s, e) =>
            {
                if (dataManager.PendingDataProviderRequestCount > 0)
                {
                    MessageBox.Show("Please wait until Data Manager is ready");
                    return;
                }

                int OutOfDateCount = (from sec in dataManager.GetAllSecurities()
                                      where sec.DataUpToDate == false
                                      select sec).Count();

                if (OutOfDateCount == 0)
                {
                    MessageBox.Show("All Seurities are up to date", "Updated", MessageBoxButtons.OK);
                    return;
                }

                if (MessageBox.Show($"Update {OutOfDateCount} securities? This will take a while", "Update?", MessageBoxButtons.OKCancel) != DialogResult.OK)
                    return;

                btnUpdateAllSecurities.Enabled = false;
                dataManager.UpdateAll(DateTime.Today);
            };
            dataManager.SecurityDataResponse += (s, e) =>
            {
                if (dataManager.PendingDataProviderRequestCount == 0)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            btnUpdateAllSecurities.Enabled = true;
                        }));
                    }
                    else
                        btnUpdateAllSecurities.Enabled = true;
                }
            };

            //
            // btnImportSymbols
            //
            btnImportSymbols.Name = "bntImportSymbols";
            btnImportSymbols.Text = "Import Symbols";
            btnImportSymbols.Size = _btnSize;
            btnImportSymbols.Click += (s, e) =>
            {
                if (dataManager.PendingDataProviderRequestCount > 0)
                {
                    MessageBox.Show("Please wait until Data Manager is ready");
                    return;
                }

                string filePath;
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        filePath = dialog.FileName;
                        new Task(() =>
                         {
                             try
                             {
                                 dataManager.LoadSymbols(ReadSymbols(filePath));
                             }
                             catch (FileLoadException)
                             {
                                 Logger.Log(new LogMessage("SecurityManagerUI", "Could not open symbol file, invalid name", LogMessageType.Error));
                                 MessageBox.Show("Invalid File Selected", "Error", MessageBoxButtons.OK);
                             }
                         }).Start();
                    }
                }
            };

            //
            // btnCleanSymbols
            //
            btnCleanSymbols.Name = "btnCleanSymbols";
            btnCleanSymbols.Text = "Clean Symbols";
            btnCleanSymbols.Size = _btnSize;
            btnCleanSymbols.Click += (s, e) =>
            {
                if (dataManager.PendingDataProviderRequestCount > 0)
                {
                    MessageBox.Show("Please wait until Data Manager is ready");
                    return;
                }

                new Task(() => { dataManager.CleanSymbols(); }).Start();
            };

            //
            // Add and arrange
            //
            btnUpdateAllSecurities.DockTo(grpDatabaseInfo, DockSide.Bottom, 5);
            btnImportSymbols.DockTo(btnUpdateAllSecurities, DockSide.Bottom, 5);
            btnCleanSymbols.DockTo(btnImportSymbols, DockSide.Bottom, 5);

            pnlDatabaseControls.Controls.Add(btnUpdateAllSecurities);
            pnlDatabaseControls.Controls.Add(btnImportSymbols);
            pnlDatabaseControls.Controls.Add(btnCleanSymbols);

            Controls.Add(pnlDatabaseControls);
            pnlDatabaseControls.DockTo(pnlSecurities, DockSide.Bottom, 5);

        }

        #endregion

        #region Security View

        Button btnViewSecurity;
        Form formSecurityView;
        SingleSecurityChart chrtSingleSecurity;

        [Initializer]
        private void InitializeSecurityView()
        {
            btnViewSecurity = new Button
            {

                //
                // btnViewSecurity
                //
                Name = "bntViewSecurity",
                Text = "Security Viewer",
                Size = new Size(175, 50)
            };
            btnViewSecurity.Click += (s, e) => ShowSecurityViewer();

            pnlDatabaseControls.Controls.Add(btnViewSecurity);
            btnViewSecurity.DockTo(grpDatabaseInfo, DockSide.Right, 10);
        }

        private void ShowSecurityViewer()
        {
            if (GetSelectedSecurity() == null)
                return;

            chrtSingleSecurity = new SingleSecurityChart(GetSelectedSecurity());

            if (formSecurityView == null || formSecurityView.IsDisposed)
            {
                formSecurityView = new Form()
                {
                    Size = chrtSingleSecurity.Size + new Size(20, 20),
                    FormBorderStyle = FormBorderStyle.FixedSingle
                };
            }

            formSecurityView.Controls.Clear();
            formSecurityView.Controls.Add(chrtSingleSecurity);
            chrtSingleSecurity.Location = new Point(10, 10);

            formSecurityView.Show();
        }

        #endregion

        public override void Refresh()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { Refresh(); }));
            }
            else
                base.Refresh();
        }


    }
}
