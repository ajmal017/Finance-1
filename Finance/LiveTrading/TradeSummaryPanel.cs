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
    public class TradeSummaryPanel : Panel
    {
        Font _defaultFont = SystemFont(8, FontStyle.Bold);

        #region Events

        public event LiveTradeEventHandler SelectedTradeChanged;
        private void OnSelectedTradeChanged()
        {
            SelectedTradeChanged?.Invoke(this, new LiveTradeEventArgs(this.SelectedTrade));
        }

        #endregion

        public LiveAccount Account { get; private set; }
        Size _defaultSize = new Size(400, 300);
        DataGridView tradeGrid = new DataGridView();

        private LiveTrade _SelectedTrade { get; set; }
        public LiveTrade SelectedTrade
        {
            get
            {
                if (tradeGrid.SelectedCells.Count == 0)
                    _SelectedTrade = null;
                else
                {
                    int tradeId = (int)tradeGrid.SelectedCells[0].Value;
                    _SelectedTrade = Account.Portfolio.GetTrade(tradeId);
                }
                return _SelectedTrade;
            }
        }

        public TradeSummaryPanel()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializePositionsGrid()
        {
            tradeGrid.Dock = DockStyle.Fill;
            tradeGrid.Name = "positionsGrid";

            tradeGrid.AutoGenerateColumns = false;
            tradeGrid.AutoSize = false;
            tradeGrid.ColumnHeadersVisible = true;
            tradeGrid.RowHeadersVisible = false;
            tradeGrid.AllowUserToResizeRows = false;
            tradeGrid.AllowUserToResizeColumns = false;
            tradeGrid.AllowUserToDeleteRows = false;
            tradeGrid.AllowUserToAddRows = false;
            tradeGrid.AllowUserToOrderColumns = false;

            //
            // Headers            
            //
            tradeGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            tradeGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            tradeGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 0, 64);
            tradeGrid.DefaultCellStyle.SelectionForeColor = Color.Goldenrod;

            tradeGrid.Font = _defaultFont;

            var displayProperties = (from property in typeof(LivePosition).GetTypeInfo()
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                     where property.GetCustomAttribute<DisplayValueAttribute>() != null
                                     select property);

            //
            // Create columns from LivePosition display attributes
            //
            foreach (var property in displayProperties)
            {
                tradeGrid.Columns.Add(property.Name, property.GetCustomAttribute<DisplayValueAttribute>().Description);
                tradeGrid.Columns[property.Name].DataPropertyName = property.Name;


                if (property.Name == "CompanyName")
                    tradeGrid.Columns[property.Name].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                if (property.Name == "Size" || property.Name == "AverageCost")
                    tradeGrid.Columns[property.Name].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                tradeGrid.Columns[property.Name].DefaultCellStyle.Format =
                    property.GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;

                tradeGrid.Columns[property.Name].DefaultCellStyle.BackColor = Color.Black;
                tradeGrid.Columns[property.Name].DefaultCellStyle.ForeColor = Color.Goldenrod;
            }

            //
            // Handlers
            //
            tradeGrid.SelectionChanged += (s, e) =>
            {
                OnSelectedTradeChanged();
            };

            tradeGrid.CellFormatting += PositionsGrid_CellFormatting;

            this.Controls.Add(tradeGrid);
        }

        private void PositionsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if ((decimal)tradeGrid.Rows[e.RowIndex].Cells["Size"].Value == 0)
            {
                e.CellStyle.ForeColor = Color.Gray;
                e.CellStyle.SelectionForeColor = Color.Gray;
            }

            if (e.ColumnIndex == tradeGrid.Columns["UnrlPNLDollars"].Index)
            {
                e.CellStyle.ForeColor = (decimal)e.Value < 0 ? Color.PaleVioletRed : Color.PaleGreen;
                e.CellStyle.SelectionForeColor = (decimal)e.Value < 0 ? Color.PaleVioletRed : Color.PaleGreen;
            }
            if (e.ColumnIndex == tradeGrid.Columns["UnrlPNLPercent"].Index)
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
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateDisplayTable()));
                return;
            }

            var bindSource = new BindingSource();
            var positions = Account.Portfolio.Positions;
            bindSource.DataSource = positions;
            tradeGrid.DataSource = bindSource;
        }
    }
}