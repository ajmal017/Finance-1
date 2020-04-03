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
        Font _defaultFont = SystemFont(8, FontStyle.Bold);

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

            positionsGrid.AutoGenerateColumns = false;
            positionsGrid.AutoSize = false;
            positionsGrid.ColumnHeadersVisible = true;
            positionsGrid.RowHeadersVisible = false;
            positionsGrid.AllowUserToResizeRows = false;
            positionsGrid.AllowUserToResizeColumns = false;
            positionsGrid.AllowUserToDeleteRows = false;
            positionsGrid.AllowUserToAddRows = false;
            positionsGrid.AllowUserToOrderColumns = false;

            //
            // Headers            
            //
            positionsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            positionsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            positionsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 0, 64);
            positionsGrid.DefaultCellStyle.SelectionForeColor = Color.Goldenrod;

            positionsGrid.Font = _defaultFont;

            var displayProperties = (from property in typeof(LivePosition).GetTypeInfo()
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                     where property.GetCustomAttribute<DisplayValueAttribute>() != null
                                     select property);

            //
            // Create columns from LivePosition display attributes
            //
            foreach (var property in displayProperties)
            {
                positionsGrid.Columns.Add(property.Name, property.GetCustomAttribute<DisplayValueAttribute>().Description);
                positionsGrid.Columns[property.Name].DataPropertyName = property.Name;


                if (property.Name == "CompanyName")
                    positionsGrid.Columns[property.Name].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                if (property.Name == "Size" || property.Name == "AverageCost")
                    positionsGrid.Columns[property.Name].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                positionsGrid.Columns[property.Name].DefaultCellStyle.Format =
                    property.GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;

                positionsGrid.Columns[property.Name].DefaultCellStyle.BackColor = Color.Black;
                positionsGrid.Columns[property.Name].DefaultCellStyle.ForeColor = Color.Goldenrod;
            }

            //
            // Handlers
            //
            positionsGrid.SelectionChanged += (s, e) =>
            {
                //OnSelectedPositionChanged();
            };

            positionsGrid.CellDoubleClick += (s, e) =>
            {
                OnSelectedPositionChanged();
            };

            positionsGrid.CellFormatting += PositionsGrid_CellFormatting;

            this.Controls.Add(positionsGrid);
        }

        private void PositionsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if ((decimal)positionsGrid.Rows[e.RowIndex].Cells["Size"].Value == 0)
            {
                e.CellStyle.ForeColor = Color.Gray;
                e.CellStyle.SelectionForeColor = Color.Gray;
            }

            if (e.ColumnIndex == positionsGrid.Columns["UnrlPNLDollars"].Index)
            {
                e.CellStyle.ForeColor = (decimal)e.Value < 0 ? Color.PaleVioletRed : Color.PaleGreen;
                e.CellStyle.SelectionForeColor = (decimal)e.Value < 0 ? Color.PaleVioletRed : Color.PaleGreen;
            }
            if (e.ColumnIndex == positionsGrid.Columns["UnrlPNLPercent"].Index)
            {
                e.CellStyle.ForeColor = (decimal)e.Value < 0 ? Color.PaleVioletRed : Color.PaleGreen;
                e.CellStyle.SelectionForeColor = (decimal)e.Value < 0 ? Color.PaleVioletRed : Color.PaleGreen;
            }

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
            Invoke(new Action(() =>
            {
                var bindSource = new BindingSource();
                var positions = Account.Portfolio.Positions;
                bindSource.DataSource = positions;
                positionsGrid.DataSource = bindSource;
            }));
        }

    }

}
