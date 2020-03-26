using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using Finance.LiveTrading;
using static Finance.Helpers;
using System.Reflection;

namespace Finance.LiveTrading
{
    public class PositionsSummaryPanel : Panel
    {

        #region Events

        public event OpenPositionEventHandler SelectedPositionChanged;
        private void OnSelectedPositionChanged()
        {
            SelectedPositionChanged?.Invoke(this, new OpenPositionEventArgs(SelectedPosition));
        }

        #endregion

        public LiveAccount Account { get; private set; }
        Size _defaultSize = new Size(400, 300);
        DataGridView positionsGrid = new DataGridView();

        private LivePosition _SelectedPosition { get; set; }
        public LivePosition SelectedPosition
        {
            get
            {
                if (positionsGrid.SelectedCells.Count == 0)
                    _SelectedPosition = null;
                else
                {
                    string ticker = positionsGrid.SelectedCells[0].Value.ToString();
                    _SelectedPosition = Account.Portfolio.GetPosition<LivePosition>(ticker);
                }
                return _SelectedPosition;
            }
        }

        public PositionsSummaryPanel()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializePositionsGrid()
        {
            positionsGrid.Dock = DockStyle.Fill;
            positionsGrid.Name = "positionsGrid";

            var displayProperties = (from property in typeof(LivePosition).GetTypeInfo()
                                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                     where property.GetCustomAttribute<AccountValueAttribute>() != null
                                     select property);

            positionsGrid.AutoGenerateColumns = false;
            positionsGrid.AutoSize = false;
            positionsGrid.ColumnHeadersVisible = true;
            positionsGrid.RowHeadersVisible = false;
            positionsGrid.AllowUserToResizeRows = false;
            positionsGrid.AllowUserToResizeColumns = false;
            positionsGrid.AllowUserToDeleteRows = false;
            positionsGrid.AllowUserToAddRows = false;
            positionsGrid.AllowUserToOrderColumns = false;

            // Headers            
            positionsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            positionsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            positionsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 0, 64);
            positionsGrid.DefaultCellStyle.SelectionForeColor = Color.Goldenrod;

            positionsGrid.Font = SystemFont(12, FontStyle.Bold);

            // Handlers
            positionsGrid.SelectionChanged += (s, e) =>
            {
                OnSelectedPositionChanged();
            };

            foreach (var property in displayProperties)
            {
                positionsGrid.Columns.Add(property.Name, property.GetCustomAttribute<AccountValueAttribute>().Description);
                positionsGrid.Columns[property.Name].DataPropertyName = property.Name;

                if (property.Name == "CompanyName")
                    positionsGrid.Columns[property.Name].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                if (property.Name == "Size" || property.Name == "AverageCost")
                    positionsGrid.Columns[property.Name].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                positionsGrid.Columns[property.Name].DefaultCellStyle.Format =
                    property.GetCustomAttribute<AccountValueAttribute>().DisplayFormat;

                positionsGrid.Columns[property.Name].DefaultCellStyle.BackColor = Color.Black;
                positionsGrid.Columns[property.Name].DefaultCellStyle.ForeColor = Color.Goldenrod;
            }

            this.Controls.Add(positionsGrid);
        }

        public void LoadAccount(LiveAccount account)
        {
            if (account == null)
                return;

            this.Account = account;
            UpdateDisplayTable();

            Account.PositionChanged += (s, e) => UpdateDisplayTable();
        }

        private void UpdateDisplayTable()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateDisplayTable()));
                return;
            }

            var positions = Account.Portfolio.Positions;
            positionsGrid.DataSource = positions;
            Refresh();
        }

    }
}
