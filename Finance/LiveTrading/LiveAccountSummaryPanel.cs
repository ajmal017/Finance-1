using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using Finance.LiveTrading;

namespace Finance
{
    public class LiveAccountSummaryPanel : Panel
    {
        public LiveAccount Account { get; private set; }

        private Panel pnlMain;
        private DataGridView accountGrid;

        public LiveAccountSummaryPanel()
        {
            InitializeComponent();
            this.InitializeMe();
        }

        private void InitializeComponent()
        {
            this.pnlMain = new System.Windows.Forms.Panel();
            this.accountGrid = new System.Windows.Forms.DataGridView();
            this.pnlMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.accountGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlMain
            // 
            this.pnlMain.Controls.Add(this.accountGrid);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(0, 0);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(589, 383);
            this.pnlMain.TabIndex = 0;
            // 
            // accountGrid
            // 
            this.accountGrid.AllowUserToAddRows = false;
            this.accountGrid.AllowUserToDeleteRows = false;
            this.accountGrid.AllowUserToResizeColumns = false;
            this.accountGrid.AllowUserToResizeRows = false;
            this.accountGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.accountGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.ColumnHeader;
            this.accountGrid.BackgroundColor = System.Drawing.SystemColors.ActiveBorder;
            this.accountGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.accountGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.accountGrid.Location = new System.Drawing.Point(3, 3);
            this.accountGrid.MultiSelect = false;
            this.accountGrid.Name = "accountGrid";
            this.accountGrid.ReadOnly = true;
            this.accountGrid.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.accountGrid.Size = new System.Drawing.Size(294, 377);
            this.accountGrid.TabIndex = 0;
            // 
            // LiveAccountSummaryPanel
            // 
            this.Controls.Add(this.pnlMain);
            this.Name = "LiveAccountSummaryPanel";
            this.Size = new System.Drawing.Size(589, 383);
            this.pnlMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.accountGrid)).EndInit();
            this.ResumeLayout(false);

        }

        [Initializer]
        private void InitializeAccountGrid()
        {
            var displayProperties = (from property in typeof(LiveAccount).GetTypeInfo()
                                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                     where property.GetCustomAttribute<AccountValueAttribute>() != null
                                     select property);

            accountGrid.Rows.Clear();
            accountGrid.Columns.Clear();
            accountGrid.AutoGenerateColumns = false;
            accountGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            accountGrid.AutoSize = false;

            accountGrid.Columns.Add("field", "");
            accountGrid.Columns.Add("value", "");

            accountGrid.RowHeadersVisible = false;
            accountGrid.ColumnHeadersVisible = false;

            accountGrid.Columns[0].Width = (int)(accountGrid.Width * .65);
            accountGrid.Columns[1].Width = (int)(accountGrid.Width * .35);

            int i = 0;
            foreach (var property in displayProperties)
            {
                var p = property.GetCustomAttribute<AccountValueAttribute>();
                if (p == null)
                    continue;

                accountGrid.Rows.Add();
                accountGrid.Rows[i].Cells["field"].Value = property.GetCustomAttribute<AccountValueAttribute>().Description;
                accountGrid.Rows[i].Cells["field"].Tag = property.Name;

                accountGrid.Rows[i].Cells["value"].Style.Format = property.GetCustomAttribute<AccountValueAttribute>().DisplayFormat;
                accountGrid.Rows[i].Cells["value"].Style.Alignment = DataGridViewContentAlignment.MiddleRight;

                i += 1;
            }

            accountGrid.Height = accountGrid.Rows.GetRowsHeight( DataGridViewElementStates.None);
        }

        public void LoadAccount(LiveAccount account)
        {
            if (account == null)
                return;

            this.Account = account;
            UpdateDisplayTable();

            Account.PropertyChanged += (s, e) => UpdateDisplayTable();
        }

        private void UpdateDisplayTable()
        {
            foreach (DataGridViewRow row in accountGrid.Rows)
            {
                string field = row.Cells["field"].Tag as string;

                var val = Account.GetType().GetProperty(field).GetValue(Account);

                row.Cells["value"].Value = val;
                row.Cells["value"].Style.ForeColor = (decimal)val < 0 ? Color.Red : Color.Black;

            }
        }
    }

}
