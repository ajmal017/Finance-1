using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using Finance;
using Finance.Data;

namespace Finance
{
    public class SecurityInfoPanelNew : UserControl
    {
        private Button btnUpdateSecurity;
        private Button btnViewSecurity;
        private GroupBox grpMain;
        private Button btnDeleteSecurity;
        private DateTimePicker dtpUpdateSpecificDate;
        private Button btnUpdateSpecificDate;
        private Button btnExcludeSecurity;
        private Button btnFavorite;
        private ChartViewerForm formSecurityViewer;

        public Security Security { get; private set; }
        public bool ShowControls { get; set; } = true;

        public SecurityInfoPanelNew()
        {
            this.InitializeMe();

            ShowInfo(null);
        }

        [Initializer]
        private void InitializeComponent()
        {
            this.grpMain = new System.Windows.Forms.GroupBox();
            this.btnUpdateSecurity = new System.Windows.Forms.Button();
            this.btnViewSecurity = new System.Windows.Forms.Button();
            this.btnDeleteSecurity = new System.Windows.Forms.Button();
            this.dtpUpdateSpecificDate = new System.Windows.Forms.DateTimePicker();
            this.btnUpdateSpecificDate = new System.Windows.Forms.Button();
            this.btnExcludeSecurity = new System.Windows.Forms.Button();
            this.btnFavorite = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // grpMain
            // 
            this.grpMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpMain.Location = new System.Drawing.Point(3, 3);
            this.grpMain.Name = "grpMain";
            this.grpMain.Size = new System.Drawing.Size(410, 234);
            this.grpMain.TabIndex = 0;
            this.grpMain.TabStop = false;
            this.grpMain.Text = "Security Info";
            // 
            // btnUpdateSecurity
            // 
            this.btnUpdateSecurity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnUpdateSecurity.Location = new System.Drawing.Point(3, 240);
            this.btnUpdateSecurity.Name = "btnUpdateSecurity";
            this.btnUpdateSecurity.Size = new System.Drawing.Size(98, 23);
            this.btnUpdateSecurity.TabIndex = 0;
            this.btnUpdateSecurity.Text = "Update Security";
            this.btnUpdateSecurity.UseVisualStyleBackColor = true;
            // 
            // btnViewSecurity
            // 
            this.btnViewSecurity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnViewSecurity.Location = new System.Drawing.Point(107, 240);
            this.btnViewSecurity.Name = "btnViewSecurity";
            this.btnViewSecurity.Size = new System.Drawing.Size(98, 23);
            this.btnViewSecurity.TabIndex = 0;
            this.btnViewSecurity.Text = "View Chart";
            this.btnViewSecurity.UseVisualStyleBackColor = true;
            // 
            // btnDeleteSecurity
            // 
            this.btnDeleteSecurity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteSecurity.Location = new System.Drawing.Point(211, 240);
            this.btnDeleteSecurity.Name = "btnDeleteSecurity";
            this.btnDeleteSecurity.Size = new System.Drawing.Size(98, 23);
            this.btnDeleteSecurity.TabIndex = 0;
            this.btnDeleteSecurity.Text = "Delete Security";
            this.btnDeleteSecurity.UseVisualStyleBackColor = true;
            // 
            // dtpUpdateSpecificDate
            // 
            this.dtpUpdateSpecificDate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.dtpUpdateSpecificDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpUpdateSpecificDate.Location = new System.Drawing.Point(107, 266);
            this.dtpUpdateSpecificDate.Name = "dtpUpdateSpecificDate";
            this.dtpUpdateSpecificDate.Size = new System.Drawing.Size(97, 20);
            this.dtpUpdateSpecificDate.TabIndex = 1;
            // 
            // btnUpdateSpecificDate
            // 
            this.btnUpdateSpecificDate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnUpdateSpecificDate.Location = new System.Drawing.Point(3, 264);
            this.btnUpdateSpecificDate.Name = "btnUpdateSpecificDate";
            this.btnUpdateSpecificDate.Size = new System.Drawing.Size(98, 23);
            this.btnUpdateSpecificDate.TabIndex = 0;
            this.btnUpdateSpecificDate.Text = "Get Single Bar";
            this.btnUpdateSpecificDate.UseVisualStyleBackColor = true;
            // 
            // btnExcludeSecurity
            // 
            this.btnExcludeSecurity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExcludeSecurity.Location = new System.Drawing.Point(211, 264);
            this.btnExcludeSecurity.Name = "btnExcludeSecurity";
            this.btnExcludeSecurity.Size = new System.Drawing.Size(98, 23);
            this.btnExcludeSecurity.TabIndex = 0;
            this.btnExcludeSecurity.Text = "Exclude Security";
            this.btnExcludeSecurity.UseVisualStyleBackColor = true;
            // 
            // btnFavorite
            // 
            this.btnFavorite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnFavorite.Location = new System.Drawing.Point(315, 240);
            this.btnFavorite.Name = "btnFavorite";
            this.btnFavorite.Size = new System.Drawing.Size(98, 23);
            this.btnFavorite.TabIndex = 0;
            this.btnFavorite.Text = "Favorite";
            this.btnFavorite.UseVisualStyleBackColor = true;
            // 
            // SecurityInfoPanelNew
            // 
            this.Controls.Add(this.dtpUpdateSpecificDate);
            this.Controls.Add(this.btnExcludeSecurity);
            this.Controls.Add(this.btnFavorite);
            this.Controls.Add(this.btnDeleteSecurity);
            this.Controls.Add(this.btnViewSecurity);
            this.Controls.Add(this.btnUpdateSpecificDate);
            this.Controls.Add(this.btnUpdateSecurity);
            this.Controls.Add(this.grpMain);
            this.Name = "SecurityInfoPanelNew";
            this.Size = new System.Drawing.Size(419, 289);
            this.ResumeLayout(false);

        }
        [Initializer]
        private void InitializeHandlers()
        {
            btnUpdateSecurity.Enabled = btnUpdateSecurity.Enabled = RefDataManager.Instance.Connected;
            RefDataManager.Instance.PropertyChanged += (s, e) =>
            {
                if (!this.Created)
                    return;

                if (e.PropertyName == "Connected")
                    Invoke(new Action(() => btnUpdateSecurity.Enabled = RefDataManager.Instance.Connected));
            };

            this.btnUpdateSecurity.Click += (s, e) => UpdateSecurity();
            this.btnUpdateSpecificDate.Click += (s, e) => UpdateSpecificDate();
            this.btnViewSecurity.Click += (s, e) => ShowSecurityChart();
            this.btnDeleteSecurity.Click += (s, e) => DeleteSecurity();
            this.btnExcludeSecurity.Click += (s, e) => ExcludeSecurity();
            this.btnFavorite.Click += (s, e) => FavoriteSecurity();

            RefDataManager.Instance.SecurityDataChanged += (s, e) =>
            {
                if (e.TryGetSecurity(this.Security?.Ticker, out Security sec))
                    LoadSecurity(sec);
            };
        }

        public void LoadSecurity(Security security)
        {
            this.Security = security;

            ShowInfo(security);
            ShowHideUpdateSecurityButton();
            ShowHideViewSecurityButton();
            ShowHideDeleteSecurityButton();
            ShowHideExcludeSecurityButton();
            ShowHideFavoriteSecurityButton();

            if (Security != null && formSecurityViewer != null && formSecurityViewer.Visible)
                formSecurityViewer.LoadSecurity(this.Security, formSecurityViewer.PriceBarSize, Settings.Instance.DefaultSwingpointBarCount);
        }
        private void ShowInfo(Security security)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { ShowInfo(security); }));
                return;
            }

            //
            // Draw No Data if Security Null
            //
            grpMain.Controls.Clear();

            if (security == null)
            {
                grpMain.Controls.Add(new Label()
                {
                    Text = "No Data",
                    Font = Helpers.SystemFont(24),
                    ForeColor = Color.FromArgb(128, 255, 255, 255),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                });
                return;
            }

            //
            // Get a list of all UI Output methods in Security (marked with Attribute)
            //
            var methods = (from method in security.GetType().GetMethods()
                           where Attribute.IsDefined(method, typeof(UiDisplayTextAttribute))
                           select method).OrderBy(x => (x.GetCustomAttribute<UiDisplayTextAttribute>()).Order);
            //
            // Add a label for each UI output method
            //
            foreach (var field in methods)
            {
                grpMain.Controls.Add(new Label()
                {
                    Text = field.Invoke(security, null) as string,
                    Width = (int)(grpMain.Width * .90),
                    Font = Helpers.SystemFont(8),
                    AutoEllipsis = true
                });
            }

            //
            // Arrange labels
            //
            grpMain.Controls[0].Location = new Point(5, 20);
            for (int i = 1; i < grpMain.Controls.Count; i++)
                grpMain.Controls[i].DockTo(grpMain.Controls[i - 1], ControlEdge.Bottom);

        }

        private void ShowHideUpdateSecurityButton()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowHideUpdateSecurityButton()));
                return;
            }

            if (!ShowControls)
            {
                btnUpdateSecurity.Enabled = false;
                btnUpdateSecurity.Hide();
                return;
            }

            if (Security == null || Security.DataUpToDate)
            {
                btnUpdateSecurity.Enabled = false;
                btnUpdateSecurity.Text = "Up to Date";
                btnUpdateSecurity.BackColor = Button.DefaultBackColor;
            }
            else if (!RefDataManager.Instance.ProviderConnected)
            {
                btnUpdateSecurity.Enabled = false;
                btnUpdateSecurity.Text = "No Data Connection";
                btnUpdateSecurity.BackColor = Button.DefaultBackColor;
            }
            else
            {
                btnUpdateSecurity.Text = "Update Security";
                btnUpdateSecurity.Enabled = true;
                btnUpdateSecurity.BackColor = Color.Orange;
            }
        }
        private void ShowHideViewSecurityButton()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowHideViewSecurityButton()));
                return;
            }

            if (!ShowControls)
            {
                btnViewSecurity.Enabled = false;
                btnViewSecurity.Hide();
                return;
            }

            if (Security == null)
            {
                btnViewSecurity.Enabled = false;
            }
            else
            {
                btnViewSecurity.Enabled = true;
            }
        }
        private void ShowHideDeleteSecurityButton()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowHideDeleteSecurityButton()));
                return;
            }

            if (!ShowControls)
            {
                btnDeleteSecurity.Enabled = false;
                btnDeleteSecurity.Hide();
                return;
            }

            if (Security == null)
                btnDeleteSecurity.Enabled = false;
            else
                btnDeleteSecurity.Enabled = true;
        }
        private void ShowHideExcludeSecurityButton()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowHideExcludeSecurityButton()));
                return;
            }

            if (!ShowControls)
            {
                btnExcludeSecurity.Enabled = false;
                btnExcludeSecurity.Hide();
                return;
            }

            if (Security == null)
            {
                btnExcludeSecurity.Enabled = false;
                return;
            }
            else if (Security.Excluded == false)
            {
                btnExcludeSecurity.Enabled = true;
                btnExcludeSecurity.Text = "Included";
                btnExcludeSecurity.BackColor = Color.LawnGreen;
            }
            else
            {
                btnExcludeSecurity.Enabled = true;
                btnExcludeSecurity.Text = "Excluded";
                btnExcludeSecurity.BackColor = Color.Red;
            }
        }
        private void ShowHideFavoriteSecurityButton()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowHideFavoriteSecurityButton()));
                return;
            }

            if (!ShowControls)
            {
                btnFavorite.Enabled = false;
                btnFavorite.Hide();
                return;
            }

            if (Security == null)
            {
                btnFavorite.Enabled = false;
                return;
            }
            else if (Security.GetCustomFlag(CustomSecurityTag.Favorite))
            {
                btnFavorite.Enabled = true;
                btnFavorite.Text = "Unfavorite";
                btnFavorite.BackColor = Color.Yellow;
            }
            else
            {
                btnFavorite.Enabled = true;
                btnFavorite.Text = "Favorite";
                btnFavorite.BackColor = Color.LightBlue;
            }
        }

        private void UpdateSecurity()
        {
            if (this.Security == null || Security.DataUpToDate)
                return;

            RefDataManager.Instance.UpdateSecurityPriceData(Security, DateTime.Today);
            btnUpdateSecurity.Enabled = false;
            btnUpdateSecurity.Text = "Updating...";
        }
        private void UpdateSpecificDate()
        {
            if (this.Security == null)
                return;

            RefDataManager.Instance.UpdateSecurityPriceData(this.Security, dtpUpdateSpecificDate.Value, dtpUpdateSpecificDate.Value);

        }
        private void DeleteSecurity()
        {
            if (this.Security == null)
                return;

            if (MessageBox.Show($"Delete {Security.Ticker} from database?", "Confirm Delete", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                RefDataManager.Instance.DeleteSecurity(this.Security);
                btnDeleteSecurity.Enabled = false;
            }
        }
        private void ExcludeSecurity()
        {
            Security.Excluded = !Security.Excluded;
            RefDataManager.Instance.SetSecurity(Security, false);

            if (Security.Excluded == false)
            {
                btnExcludeSecurity.Enabled = true;
                btnExcludeSecurity.Text = "Included";
                btnExcludeSecurity.BackColor = Color.LawnGreen;
            }
            else
            {
                btnExcludeSecurity.Enabled = true;
                btnExcludeSecurity.Text = "Excluded";
                btnExcludeSecurity.BackColor = Color.Red;
            }

        }
        private void ShowSecurityChart()
        {
            if (Security == null)
                return;

            if (formSecurityViewer == null || formSecurityViewer.IsDisposed)
                formSecurityViewer = new ChartViewerForm();

            formSecurityViewer.LoadSecurity(this.Security, PriceBarSize.Daily, Settings.Instance.DefaultSwingpointBarCount);
            formSecurityViewer.Show();
        }
        private void FavoriteSecurity()
        {
            Security.SetCustomTag(CustomSecurityTag.Favorite);
            RefDataManager.Instance.SetSecurity(Security, false);

            if (Security.GetCustomFlag(CustomSecurityTag.Favorite))
            {
                btnFavorite.Text = "Unfavorite";
                btnFavorite.BackColor = Color.Yellow;
            }
            else
            {
                btnFavorite.Text = "Favorite";
                btnFavorite.BackColor = Color.Green;
            }
        }
    }
}
