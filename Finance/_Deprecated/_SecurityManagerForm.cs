using Finance.Data;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Finance.Helpers;

/*
 *  DEPRECATED
 * 
 */
namespace Finance
{
    //public class SecurityManagerForm : CustomForm
    //{
    //    public override Size _defaultSize => new Size(400, 1000);

    //    public SecurityManagerForm() : base("SecurityManager", true, false)
    //    {
    //        if (DataManager.Instance.ProviderConnected == false)
    //            throw new DataproviderConnectionException();

    //        this.InitializeMe();

    //        Refresh();
    //        BringToFront();
    //    }

    //    [Initializer]
    //    private void InitializeStyles()
    //    {
    //        FormBorderStyle = FormBorderStyle.FixedSingle;
    //        this.ControlAdded += (s, e) =>
    //        {
    //            //
    //            // Resize form to show all added controls
    //            //
    //            Size _sizeNonClient = Size.Subtract(Size, ClientSize);
    //            foreach (Control ctrl in Controls)
    //            {
    //                if (ctrl.Right > ClientRectangle.Width)
    //                    Width = ctrl.Right + _sizeNonClient.Width;
    //                if (ctrl.Bottom > ClientRectangle.Height)
    //                    Height = ctrl.Bottom + _sizeNonClient.Height;
    //            }

    //            Refresh();
    //        };
    //    }

    //    #region Security Display and Selection

    //    ExpandoPanel pnlSecurities;
    //    SecurityListBox lbSecurityListBox;
    //    SecurityInfoPanel pnlSecurityInfo;

    //    [Initializer]
    //    private void InitializeSecuritiesDisplay()
    //    {
    //        //
    //        // Securities Panel which contains selector listbox
    //        //
    //        pnlSecurities = new ExpandoPanel
    //        {
    //            Location = new Point(5, 5)
    //        };
    //        Controls.Add(pnlSecurities);

    //        //
    //        // Securities Selector List Box 
    //        //
    //        lbSecurityListBox = new SecurityListBox();
    //        lbSecurityListBox.Height = 300;
    //        lbSecurityListBox.SelectedValueChanged += (s, e) =>
    //        {
    //            pnlSecurityInfo.Load(lbSecurityListBox.SelectedSecurity);
    //        };
    //        pnlSecurities.Controls.Add(lbSecurityListBox);

    //        //
    //        // Security Info Panel
    //        //
    //        pnlSecurityInfo = new SecurityInfoPanel(null);
    //        pnlSecurityInfo.DockTo(lbSecurityListBox, ControlEdge.Right, 5);
    //        pnlSecurities.Controls.Add(pnlSecurityInfo);
    //    }

    //    private Security GetSelectedSecurity()
    //    {
    //        if (lbSecurityListBox.SelectedIndex != -1)
    //            return lbSecurityListBox.SelectedSecurity;
    //        return null;
    //    }

    //    #endregion
    //    #region Database Control Area       

    //    ExpandoPanel pnlDatabaseControls;
    //    GroupBox grpDatabaseInfo;
    //    Label lblDatabaseInfo;

    //    Button btnUpdateAllSecurities;
    //    Button btnImportSymbols;
    //    Button btnCleanSymbols;
    //    Button btnCancelRequests;
    //    Button btnDeleteSecurity;

    //    [Initializer]
    //    private void InitializeDatabaseInfo()
    //    {
    //        pnlDatabaseControls = new ExpandoPanel();
    //        grpDatabaseInfo = new GroupBox();
    //        lblDatabaseInfo = new Label();

    //        //
    //        // Control Panel
    //        //
    //        pnlDatabaseControls.Width = pnlSecurities.Width;
    //        pnlDatabaseControls.DockTo(pnlSecurities, ControlEdge.Bottom, 5);

    //        //
    //        // grpDatabaseInfo
    //        //
    //        grpDatabaseInfo.Name = "grpDatabaseInfo";
    //        grpDatabaseInfo.Text = "Database";
    //        grpDatabaseInfo.Width = 175;
    //        grpDatabaseInfo.Height = 100;
    //        grpDatabaseInfo.Location = new Point(2, 2);
    //        pnlDatabaseControls.Controls.Add(grpDatabaseInfo);

    //        //
    //        // Database Info contents
    //        //
    //        Action RefreshDatabaseInfo = new Action(() =>
    //        {
    //            // Write number of securities and number of out-of-date securities to label
    //            int numSecurities = DataManager.Instance.GetAllSecurities().Count;
    //            int numOutOfDate = (from sec in DataManager.Instance.GetAllSecurities()
    //                                where !sec.DataUpToDate
    //                                select sec).Count();

    //            string text = string.Format($"Security Count: {numSecurities:###,###}{Environment.NewLine}" +
    //                $"Out-Of-Date:    {numOutOfDate:###,##0}");

    //            lblDatabaseInfo.Text = text;
    //            Refresh();
    //        });
    //        lblDatabaseInfo.Name = "lblDatabaseInfo";
    //        lblDatabaseInfo.Font = Helpers.SystemFont(8);
    //        lblDatabaseInfo.Width = grpDatabaseInfo.Width - 20;
    //        lblDatabaseInfo.Location = new Point(10, 20);
    //        DataManager.Instance.SecurityListLoaded += (s, e) =>
    //        {
    //            if (Visible) Invoke(RefreshDatabaseInfo);
    //        };
    //        grpDatabaseInfo.Controls.Add(lblDatabaseInfo);

    //        RefreshDatabaseInfo();
    //    }

    //    [Initializer]
    //    private void InitializeDataMaintControls()
    //    {
    //        Size _btnSize = new Size(175, 25);

    //        //
    //        // btnUpdateAllSeurities
    //        //
    //        btnUpdateAllSecurities = new Button
    //        {
    //            Name = "btnUpdateAllSecurities",
    //            Text = "Update All",
    //            BackColor = Color.Orange,
    //            Size = _btnSize
    //        };
    //        btnUpdateAllSecurities.Click += (s, e) =>
    //        {
    //            if (DataManager.Instance.PendingDataProviderRequestCount > 0)
    //            {
    //                MessageBox.Show("Please wait until Data Manager is ready");
    //                return;
    //            }

    //            int OutOfDateCount = (from sec in DataManager.Instance.GetAllSecurities()
    //                                  where sec.DataUpToDate == false
    //                                  select sec).Count();

    //            if (OutOfDateCount == 0)
    //            {
    //                MessageBox.Show("All Seurities are up to date", "Updated", MessageBoxButtons.OK);
    //                return;
    //            }

    //            if (MessageBox.Show($"Update {OutOfDateCount} securities? This will take a while", "Update?", MessageBoxButtons.OKCancel) != DialogResult.OK)
    //                return;

    //            btnUpdateAllSecurities.Enabled = false;
    //            DataManager.Instance.UpdateAll(DateTime.Today);
    //        };
    //        DataManager.Instance.SecurityDataResponse += (s, e) =>
    //        {
    //            if (DataManager.Instance.PendingDataProviderRequestCount == 0)
    //            {
    //                if (InvokeRequired)
    //                {
    //                    Invoke(new Action(() =>
    //                    {
    //                        btnUpdateAllSecurities.Enabled = true;
    //                    }));
    //                }
    //                else
    //                    btnUpdateAllSecurities.Enabled = true;
    //            }
    //        };

    //        //
    //        // btnImportSymbols
    //        //
    //        btnImportSymbols = new Button
    //        {
    //            Name = "bntImportSymbols",
    //            Text = "Import Symbols",
    //            Size = _btnSize
    //        };
    //        btnImportSymbols.Click += (s, e) =>
    //        {
    //            if (DataManager.Instance.PendingDataProviderRequestCount > 0)
    //            {
    //                MessageBox.Show("Please wait until Data Manager is ready");
    //                return;
    //            }

    //            string filePath;
    //            using (OpenFileDialog dialog = new OpenFileDialog())
    //            {
    //                if (dialog.ShowDialog() == DialogResult.OK)
    //                {
    //                    filePath = dialog.FileName;
    //                    new Task(() =>
    //                    {
    //                        try
    //                        {
    //                            DataManager.Instance.LoadSymbols(ReadSymbols(filePath));
    //                        }
    //                        catch (FileLoadException)
    //                        {
    //                            Logger.Log(new LogMessage("SecurityManagerUI", "Could not open symbol file, invalid name", LogMessageType.SystemError));
    //                            MessageBox.Show("Invalid File Selected", "Error", MessageBoxButtons.OK);
    //                        }
    //                    }).Start();
    //                }
    //            }
    //        };

    //        //
    //        // btnCleanSymbols
    //        //
    //        btnCleanSymbols = new Button
    //        {
    //            Name = "btnCleanSymbols",
    //            Text = "Clean Symbols",
    //            Size = _btnSize
    //        };
    //        btnCleanSymbols.Click += (s, e) =>
    //        {
    //            if (DataManager.Instance.PendingDataProviderRequestCount > 0)
    //            {
    //                MessageBox.Show("Please wait until Data Manager is ready");
    //                return;
    //            }

    //            new Task(() => { DataManager.Instance.CleanSymbols(); }).Start();
    //        };

    //        //
    //        // btnCancelRequests
    //        //
    //        btnCancelRequests = new Button()
    //        {
    //            Name = "btnCancelRequests",
    //            Text = "Cancel Open Requests",
    //            Size = _btnSize
    //        };
    //        btnCancelRequests.Click += (s, e) =>
    //        {
    //            DataManager.Instance.CancelAllRequests();
    //        };

    //        //
    //        // btnDeleteSecurity
    //        //
    //        btnDeleteSecurity = new Button()
    //        {
    //            Name = "btnDeleteSecurity",
    //            Text = "Delete Security",
    //            Size = _btnSize,
    //            BackColor = Color.PaleVioletRed
    //        };
    //        btnDeleteSecurity.Click += (s, e) =>
    //        {
    //            if (GetSelectedSecurity() == null)
    //                return;

    //            if (MessageBox.Show($"Delete {GetSelectedSecurity().Ticker} from database?", "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
    //            {
    //                DataManager.Instance.DeleteSecurity(GetSelectedSecurity());
    //                lbSecurityListBox.UpdateList();
    //            }
    //        };

    //        //
    //        // Add and arrange
    //        //
    //        btnUpdateAllSecurities.DockTo(grpDatabaseInfo, ControlEdge.Bottom, 5);
    //        btnImportSymbols.DockTo(btnUpdateAllSecurities, ControlEdge.Bottom, 5);
    //        btnCleanSymbols.DockTo(btnImportSymbols, ControlEdge.Bottom, 5);
    //        btnCancelRequests.DockTo(btnCleanSymbols, ControlEdge.Bottom, 5);
    //        btnDeleteSecurity.DockTo(btnCancelRequests, ControlEdge.Bottom, 5);

    //        pnlDatabaseControls.Controls.Add(btnUpdateAllSecurities);
    //        pnlDatabaseControls.Controls.Add(btnImportSymbols);
    //        pnlDatabaseControls.Controls.Add(btnCleanSymbols);
    //        pnlDatabaseControls.Controls.Add(btnCancelRequests);
    //        pnlDatabaseControls.Controls.Add(btnDeleteSecurity);

    //        pnlDatabaseControls.DockTo(pnlSecurities, ControlEdge.Bottom, 5);
    //        Controls.Add(pnlDatabaseControls);
    //    }

    //    #endregion

    //    public override void Refresh()
    //    {
    //        if (InvokeRequired)
    //        {
    //            Invoke(new Action(() => { Refresh(); }));
    //        }
    //        else
    //            base.Refresh();
    //    }


    //}
}
