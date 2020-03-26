using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Finance;
using Finance.Data;
using static Finance.Helpers;

namespace Finance
{
    public class MarketTrendMonitorForm : Form, IPersistLayout
    {
        private Panel pnlMain;
        private DateTimePicker dateTrendIndexDate;
        private Button btnMinusDay;
        private Button btnPlusDay;
        private Button btnLatest;

        private List<SectorTrendPanel> SectorTrendPanels { get; set; } = new List<SectorTrendPanel>();
        Size _defaultChartPanelSize = new Size(200, 150);

        private static MarketTrendMonitorForm _Instance { get; set; }
        public static MarketTrendMonitorForm Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new MarketTrendMonitorForm();
                }
                return _Instance;
            }
        }

        public bool Sizeable => false;

        private MarketTrendMonitorForm()
        {
            InitializeComponent();

            this.FormClosing += (s, e) =>
            {
                this.Hide();
                e.Cancel = true;
            };
            this.Shown += (s, e) => LoadLayout();
            this.ResizeEnd += (s, e) => SaveLayout();

            this.InitializeMe();

            LoadTrends();
        }

        private void InitializeComponent()
        {
            this.pnlMain = new System.Windows.Forms.Panel();
            this.dateTrendIndexDate = new System.Windows.Forms.DateTimePicker();
            this.btnMinusDay = new System.Windows.Forms.Button();
            this.btnPlusDay = new System.Windows.Forms.Button();
            this.btnLatest = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // pnlMain
            // 
            this.pnlMain.Location = new System.Drawing.Point(13, 13);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(1400, 450);
            this.pnlMain.TabIndex = 2;
            // 
            // dateTrendIndexDate
            // 
            this.dateTrendIndexDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTrendIndexDate.Location = new System.Drawing.Point(13, 469);
            this.dateTrendIndexDate.Name = "dateTrendIndexDate";
            this.dateTrendIndexDate.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.dateTrendIndexDate.Size = new System.Drawing.Size(98, 20);
            this.dateTrendIndexDate.TabIndex = 3;
            // 
            // btnMinusDay
            // 
            this.btnMinusDay.Location = new System.Drawing.Point(13, 495);
            this.btnMinusDay.Name = "btnMinusDay";
            this.btnMinusDay.Size = new System.Drawing.Size(47, 42);
            this.btnMinusDay.TabIndex = 4;
            this.btnMinusDay.Text = "<=";
            this.btnMinusDay.UseVisualStyleBackColor = true;
            // 
            // btnPlusDay
            // 
            this.btnPlusDay.Location = new System.Drawing.Point(64, 495);
            this.btnPlusDay.Name = "btnPlusDay";
            this.btnPlusDay.Size = new System.Drawing.Size(47, 42);
            this.btnPlusDay.TabIndex = 4;
            this.btnPlusDay.Text = "=>";
            this.btnPlusDay.UseVisualStyleBackColor = true;
            // 
            // btnLatest
            // 
            this.btnLatest.Location = new System.Drawing.Point(13, 539);
            this.btnLatest.Name = "btnLatest";
            this.btnLatest.Size = new System.Drawing.Size(98, 23);
            this.btnLatest.TabIndex = 5;
            this.btnLatest.Text = "button1";
            this.btnLatest.UseVisualStyleBackColor = true;
            // 
            // MarketTrendMonitorForm
            // 
            this.ClientSize = new System.Drawing.Size(1423, 569);
            this.Controls.Add(this.btnLatest);
            this.Controls.Add(this.btnPlusDay);
            this.Controls.Add(this.btnMinusDay);
            this.Controls.Add(this.dateTrendIndexDate);
            this.Controls.Add(this.pnlMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "MarketTrendMonitorForm";
            this.ResumeLayout(false);

        }

        [Initializer]
        private void InitializeControls()
        {
            pnlMain.Size = new Size(_defaultChartPanelSize.Width * 7, _defaultChartPanelSize.Height * 3);
            pnlMain.BorderStyle = BorderStyle.FixedSingle;

            //
            // Date Picker
            //
            dateTrendIndexDate.MaxDate = Calendar.PriorTradingDay(DateTime.Today);
            dateTrendIndexDate.Value = Calendar.PriorTradingDay(DateTime.Today);
            dateTrendIndexDate.CloseUp += (s, e) =>
            {
                if (!Calendar.IsTradingDay(dateTrendIndexDate.Value))
                    dateTrendIndexDate.Value = Calendar.PriorTradingDay(dateTrendIndexDate.Value);

                LoadSectorTrendDate(dateTrendIndexDate.Value);
            };

            //
            // Less day
            //
            btnMinusDay.Click += (s, e) =>
            {
                dateTrendIndexDate.Value = Calendar.PriorTradingDay(dateTrendIndexDate.Value);
                LoadSectorTrendDate(dateTrendIndexDate.Value);
            };

            //
            // Add day
            //
            btnPlusDay.Click += (s, e) =>
            {
                if (Calendar.NextTradingDay(dateTrendIndexDate.Value) <= dateTrendIndexDate.MaxDate)
                    dateTrendIndexDate.Value = Calendar.NextTradingDay(dateTrendIndexDate.Value);
                else
                    return;

                LoadSectorTrendDate(dateTrendIndexDate.Value);
            };

            //
            // Latest day
            //
            btnLatest.Click += (s, e) =>
            {
                dateTrendIndexDate.Value = dateTrendIndexDate.MaxDate;
                LoadSectorTrendDate(dateTrendIndexDate.Value);
            };

        }

        private void LoadTrends()
        {
            var trends = IndexManager.Instance.GetAllTrendIndices(Settings.Instance.Sector_Trend_Bar_Size);

            pnlMain.Controls.Clear();

            foreach (var trend in trends)
            {
                if (trend.IndexEntries.Count > 0)
                {
                    AddSectorTrendPanel(trend);
                }
            }
        }
        private void AddSectorTrendPanel(TrendIndex trend)
        {
            var pnl = SectorTrendPanels.AddAndReturn(new SectorTrendPanel()
            {
                Size = _defaultChartPanelSize
            });
            int row = (SectorTrendPanels.Count - 1) / 7;
            int col = (SectorTrendPanels.Count - 1) % 7;
            DateTime trendDate = dateTrendIndexDate.Value;

            pnl.LoadTrendIndex(trend, trendDate);
            pnl.Location = new System.Drawing.Point(_defaultChartPanelSize.Width * col, _defaultChartPanelSize.Height * row);
            pnlMain.Controls.Add(pnl);
        }
        private void LoadSectorTrendDate(DateTime date)
        {
            foreach (var chart in SectorTrendPanels)
            {
                chart.LoadTrendDate(date);
            }
            Refresh();
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
