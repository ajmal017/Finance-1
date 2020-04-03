using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms.DataVisualization.Charting;
using Finance;
using Finance.Data;
using static Finance.Helpers;
using static Finance.Logger;
using static Finance.Calendar;

namespace Finance
{
    public abstract class SingleStockIndicatorTile : Panel, INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event SelectedSecurityChangedEventHandler SecurityChanged;
        protected void OnSecurityChanged()
        {
            SecurityChanged?.Invoke(this, new SelectedSecurityEventArgs(this.Security));
        }

        #endregion

        Size _defaultSize = new Size(500, 400);
        Label lblTitle;
        protected Panel pnlGraphics;
        protected Panel pnlOptions;
        protected Panel pnlText;
        private Panel pnlNoData;

        protected ComboBox<ReturnFormat> cmbReturnFormat;

        protected abstract string IndicatorDescription { get; }
        protected ToolTip toolTip;

        public Security Security { get; protected set; }

        protected ReturnFormat ReturnFormat
        {
            get => cmbReturnFormat?.GetSelectedValue() ?? ReturnFormat.Percent;
            set => cmbReturnFormat.SetSelectedValue(value);
        }

        public SingleStockIndicatorTile()
        {
            InitializeBaseStyle();
            InitializePanels();
            InitializeReturnFormatSelector();
            InitializeToolTip();
        }

        private void InitializeBaseStyle()
        {
            Size = _defaultSize;
            MinimumSize = _defaultSize;
            MaximumSize = _defaultSize;
            this.BackColor = BackColor = Color.FromArgb(255, 48, 48, 48);
            BorderStyle = BorderStyle.FixedSingle;
        }
        private void InitializePanels()
        {
            pnlGraphics = new Panel()
            {
                Height = (int)(_defaultSize.Height / 7 * 4.5),
                Width = _defaultSize.Width,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(0, 20),
                BackColor = Color.Black
            };
            pnlOptions = new Panel()
            {
                Height = _defaultSize.Height / 7,
                Width = _defaultSize.Width,
                BackColor = Color.FromArgb(255, 64, 64, 64)
            };
            pnlOptions.DockTo(pnlGraphics, ControlEdge.Bottom, 0);
            pnlText = new Panel()
            {
                Height = _defaultSize.Height - pnlOptions.Bottom,
                Width = _defaultSize.Width,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(255, 48, 48, 48)
            };
            pnlText.DockTo(pnlOptions, ControlEdge.Bottom, 0);
            this.Controls.AddRange(new[] { pnlGraphics, pnlOptions, pnlText });

            //
            // No Data panel
            //
            pnlNoData = new Panel()
            {
                BackColor = Color.Black,
                Dock = DockStyle.Fill,
                Visible = false
            };
            Label lblNoData = new Label()
            {
                Text = "No Data",
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlNoData.Controls.Add(lblNoData);
            pnlGraphics.Controls.Add(pnlNoData);

            //
            // Title Label
            //
            lblTitle = new Label()
            {
                Font = new Font("Calibri", 10, FontStyle.Bold),
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                Width = _defaultSize.Width / 3 * 2,
                Location = new Point(0, 0),
                ForeColor = Color.White
            };
            this.Controls.Add(lblTitle);
        }
        private void InitializeReturnFormatSelector()
        {
            cmbReturnFormat = new ComboBox<ReturnFormat>()
            {
                Font = new Font("Calibri", 8, FontStyle.Bold),
                Width = 75,
                Location = new Point(_defaultSize.Width - 77, 2)
            };
            cmbReturnFormat.SelectedValueChanged += (s, e) =>
            {
                ReloadIndicator();
            };
            this.Controls.Add(cmbReturnFormat);
        }
        private void InitializeToolTip()
        {
            toolTip = new ToolTip()
            {
                AutoPopDelay = 5000,
                InitialDelay = 500,
                ReshowDelay = 0,
                ShowAlways = true
            };
            toolTip.SetToolTip(lblTitle, IndicatorDescription);
        }

        protected void SetTitle(string name)
        {
            this.Name = name;
            lblTitle.Text = this.Name;
            Refresh();
        }
        protected void DrawNoData()
        {
            pnlNoData.Show();
        }
        protected void HideNoData()
        {
            pnlNoData.Hide();
        }

        public void SetSecurity(Security security)
        {
            if (this.Security == security)
                return;

            this.Security = security;

            SetNewSecurityOptions();
            ReloadIndicator();

            OnSecurityChanged();
        }

        protected abstract void SetNewSecurityOptions();
        protected abstract void ReloadIndicator();
    }

    public abstract class SingleStockTrendChartIndicatorTile : SingleStockIndicatorTile
    {
        protected Chart chart;
        protected ChartArea chartArea;
        protected Series series;

        protected ComboBox<PriceBarSize> cmbBasePriceBarSize;
        protected ComboBox<TrendQualification> cmbBaseTrendType;
        protected ComboBox<PriceBarSize> cmbBackgroundPriceBarSize;
        protected ComboBox<TrendQualification> cmbBackgroundTrendType;

        protected PriceBarSize BarSize
        {
            get => cmbBasePriceBarSize?.GetSelectedValue() ?? PriceBarSize.Daily;
            set => cmbBasePriceBarSize.SetSelectedValue(value, true);
        }
        protected TrendQualification TrendType
        {
            get => cmbBaseTrendType?.GetSelectedValue() ?? TrendQualification.ConfirmedBullish;
            set => cmbBaseTrendType.SetSelectedValue(value, true);
        }
        protected PriceBarSize BackBarSize
        {
            get => cmbBackgroundPriceBarSize?.GetSelectedValue() ?? PriceBarSize.Weekly;
            set => cmbBackgroundPriceBarSize.SetSelectedValue(value, true);
        }
        protected TrendQualification BackTrendType
        {
            get => cmbBackgroundTrendType?.GetSelectedValue() ?? TrendQualification.NotSet;
            set => cmbBackgroundTrendType.SetSelectedValue(value, true);
        }

        public SingleStockTrendChartIndicatorTile() : base()
        {
            InitializeChartArea();
            InitializeChart();
            InitializeOptions();
            InitializeSeries();
        }

        protected abstract void InitializeChartArea();
        private void InitializeChart()
        {
            chart = new Chart()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(255, 16, 16, 16)
            };
            chart.ChartAreas.Clear();
            chart.ChartAreas.Add(chartArea);

            pnlGraphics.Controls.Add(chart);
        }

        private void InitializeOptions()
        {

            //
            // Primary label
            //
            Label lblPrimary = new Label()
            {
                Text = "Primary Trend",
                Font = new Font("Calibri", 8, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(2, 2),
                Width = 100,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlOptions.Controls.Add(lblPrimary);

            //
            // Price Bar Select
            //
            cmbBasePriceBarSize = new ComboBox<PriceBarSize>()
            {
                Font = new Font("Calibri", 8, FontStyle.Bold),
                Width = 75
            };
            cmbBasePriceBarSize.RemoveItem(PriceBarSize.Quarterly);
            cmbBasePriceBarSize.SelectedValueChanged += (s, e) =>
            {
                if (BackBarSize <= BarSize)
                {
                    BackBarSize = BarSize + 1;
                }
                ReloadIndicator();
            };
            cmbBasePriceBarSize.DockTo(lblPrimary, ControlEdge.Right, 2);
            pnlOptions.Controls.Add(cmbBasePriceBarSize);

            //
            // Trend Type Select
            //
            cmbBaseTrendType = new ComboBox<TrendQualification>()
            {
                Font = new Font("Calibri", 8, FontStyle.Bold),
                Width = 125
            };
            cmbBaseTrendType.DockTo(cmbBasePriceBarSize, ControlEdge.Right, 2);
            cmbBaseTrendType.RemoveItem(TrendQualification.NotSet);
            cmbBaseTrendType.SelectedValueChanged += (s, e) =>
            {
                ReloadIndicator();
            };
            pnlOptions.Controls.Add(cmbBaseTrendType);

            //
            // Background label
            //
            Label lblBackground = new Label()
            {
                Text = "Background Trend",
                Font = new Font("Calibri", 8, FontStyle.Bold),
                ForeColor = Color.White,
                Width = 100,
                TextAlign = ContentAlignment.MiddleRight
            };
            lblBackground.DockTo(lblPrimary, ControlEdge.Bottom, 0);
            pnlOptions.Controls.Add(lblBackground);

            //
            // Background Price Bar Select
            //
            cmbBackgroundPriceBarSize = new ComboBox<PriceBarSize>()
            {
                Location = new Point(2, 4),
                Font = new Font("Calibri", 8, FontStyle.Bold),
                Width = 75
            };
            cmbBackgroundPriceBarSize.DockTo(lblBackground, ControlEdge.Right, 2);
            cmbBackgroundPriceBarSize.SelectedValueChanged += (s, e) =>
            {
                if (BackBarSize <= BarSize)
                {
                    BackBarSize = BarSize + 1;
                }
                ReloadIndicator();
            };
            pnlOptions.Controls.Add(cmbBackgroundPriceBarSize);

            //
            // Background Trend Type Select
            //
            cmbBackgroundTrendType = new ComboBox<TrendQualification>()
            {
                Font = new Font("Calibri", 8, FontStyle.Bold),
                Width = 125
            };
            cmbBackgroundTrendType.DockTo(cmbBackgroundPriceBarSize, ControlEdge.Right, 2);
            cmbBackgroundTrendType.SelectedValueChanged += (s, e) =>
            {
                ReloadIndicator();
            };
            pnlOptions.Controls.Add(cmbBackgroundTrendType);

            SetDefaultOptions();

        }
        protected abstract void InitializeSeries();

        protected void SetDefaultOptions()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetDefaultOptions()));
                return;
            }

            BarSize = PriceBarSize.Daily;
            TrendType = TrendQualification.ConfirmedBullish;
            BackBarSize = PriceBarSize.Weekly;
            BackTrendType = TrendQualification.NotSet;
            ReturnFormat = ReturnFormat.ATR;
        }
        protected override void SetNewSecurityOptions()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetNewSecurityOptions()));
                return;
            }

            // Set the primary and background trends to current
            TrendType = Security.GetLastBar(BarSize).GetTrendType(Settings.Instance.DefaultSwingpointBarCount);
            BackTrendType = Security.GetLastBar(BackBarSize).GetTrendType(Settings.Instance.DefaultSwingpointBarCount);
        }
        protected override void ReloadIndicator()
        {
            if (Security == null || TrendType == TrendQualification.NotSet)
            {
                DrawNoData();
                return;
            }

            HideNoData();
            PopulateSeries();
            DrawChart();
            DrawInfo();
            DrawAdditional();

            Refresh();
        }

        private void DrawChart()
        {
            if (series.Points.Count == 0)
            {
                DrawNoData();
                return;
            }

            SetXRange();
            SetYFormat();
            chart.Update();
        }

        protected abstract void SetYFormat();
        protected abstract void SetXRange();
        protected abstract void DrawAdditional();
        protected abstract void DrawInfo();
        protected abstract void PopulateSeries();
    }

    public sealed class TrendAveragePerformanceTile : SingleStockTrendChartIndicatorTile
    {
        protected override string IndicatorDescription => "Displays an ordered set of next-day returns given the indicated primary and next-order trends.";

        public TrendAveragePerformanceTile()
        {
            SetTitle("Average Daily Return by Current Trend");
        }

        protected override void InitializeChartArea()
        {
            chartArea = new ChartArea()
            {
                Name = "primary",
                BackColor = Color.FromArgb(255, 16, 16, 16)
            };

            chartArea.Position.Width = 100;
            chartArea.Position.Height = 100;
            chartArea.Position.X = 0;
            chartArea.Position.Y = 0;

            chartArea.InnerPlotPosition.Width = 90;
            chartArea.InnerPlotPosition.Height = 94;
            chartArea.InnerPlotPosition.X = 10;
            chartArea.InnerPlotPosition.Y = 3;

            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Number;
            chartArea.AxisX.LabelStyle.Enabled = false;

            // X Grid (off)
            chartArea.AxisX.MajorGrid.Enabled = false;

            chartArea.AxisY.Interval = .001;
            chartArea.AxisY.IntervalType = DateTimeIntervalType.Number;

            // Y Grid (on)            
            chartArea.AxisY.MajorGrid.Enabled = true;
            chartArea.AxisY.MajorGrid.Interval = .01;
            chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(255, 96, 96, 96);
            chartArea.AxisY.LabelStyle.Format = "0.00%";
            chartArea.AxisY.LabelAutoFitStyle = LabelAutoFitStyles.DecreaseFont;

            chartArea.AxisY.MinorGrid.Enabled = true;
            chartArea.AxisY.MinorGrid.Interval = .0025;
            chartArea.AxisY.MinorGrid.LineColor = Color.FromArgb(255, 32, 32, 32);

            chartArea.AxisY.LabelStyle.ForeColor = Color.White;
            chartArea.AxisY.LabelStyle.Interval = .01;
        }
        protected override void InitializeSeries()
        {
            series = new Series()
            {
                ChartArea = "primary",
                ChartType = SeriesChartType.Column
            };

            chart.Series.Clear();
            chart.Series.Add(series);
        }

        protected override void PopulateSeries()
        {
            series.Points.Clear();

            Security.SetSwingPointsAndTrends(Settings.Instance.DefaultSwingpointBarCount, BarSize);
            Security.SetSwingPointsAndTrends(Settings.Instance.DefaultSwingpointBarCount, BackBarSize);

            // List of bars for the base trend
            var baseTrendBars = Security.GetPriceBars(BarSize).
                Where(x => x.GetTrendType(Settings.Instance.DefaultSwingpointBarCount) == TrendType).
                ToList();

            // List of bars for the background trend
            var backgroundTrendBars = Security.GetPriceBars(BackBarSize).ToList();

            var dataPts = new List<decimal>();

            foreach (PriceBar bar in baseTrendBars)
            {
                if (bar.NextBar == null)
                    continue;

                // For each bar, include it only if it's background bar matches the selected trend (or if NotSet for BG)
                var bgBar = Calendar.GetContainingBar(bar, backgroundTrendBars);
                if (BackTrendType != TrendQualification.NotSet &&
                    bgBar.GetTrendType(Settings.Instance.DefaultSwingpointBarCount) != BackTrendType)
                    continue;

                switch (ReturnFormat)
                {
                    case ReturnFormat.Percent:
                        dataPts.Add(bar.NextBar.PercentChange);
                        break;
                    case ReturnFormat.ATR:
                        dataPts.Add(bar.NextBar.AtrChange);
                        break;
                }
            }

            dataPts.Sort((x, y) => x.CompareTo(y));

            int i = 0;
            foreach (decimal dataPt in dataPts)
            {
                var pt = new DataPoint()
                {
                    XValue = ++i,
                    YValues = new[] { dataPt.ToDouble() },
                    Color = dataPt > 0 ? Color.Green : Color.Red,
                    IsValueShownAsLabel = false
                };
                series.Points.Add(pt);
            }
        }
        protected override void DrawInfo()
        {
            Font _lblFont = new Font("Consolas", 8, FontStyle.Bold);

            pnlText.Controls.Clear();

            // Number of datapoints
            Label lbl1 = new Label()
            {
                Text = $"{"Datapoints:",11}{series.Points.Count,8}",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = _lblFont,
                ForeColor = Color.White,
                Width = pnlText.Width / 3,
                Height = pnlText.Height / 5,
                Location = new Point(2, 2)
            };

            if (series.Points.Count == 0)
            {
                pnlText.Controls.Add(lbl1);
                return;
            }

            // Average return
            var avg = series.Points.Count > 0 ? series.Points.Average(x => x.YValues[0]) : 0;

            string lblStr = ReturnFormat == ReturnFormat.Percent ?
                 $"{$"Avg % Rtn:",11}{avg,8:0.00%}" :
                  $"{$"Avg ATR:",11}{avg,8:0.00}";

            Label lbl2 = new Label()
            {
                Text = lblStr,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = _lblFont,
                ForeColor = Color.White,
                Width = pnlText.Width / 3,
                Height = pnlText.Height / 5,
            };
            lbl2.DockTo(lbl1, ControlEdge.Bottom, 2);

            // Stdev
            var stdev = Stdev((from pt in series.Points select pt.YValues[0]).ToList());

            lblStr = ReturnFormat == ReturnFormat.Percent ?
                 $"{$"Std Dev:",11}{avg,8:0.00%}" :
                  $"{$"Std Dev:",11}{avg,8:0.00}";

            Label lbl3 = new Label()
            {
                Text = lblStr,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = _lblFont,
                ForeColor = Color.White,
                Width = pnlText.Width / 3,
                Height = pnlText.Height / 5,
            };
            lbl3.DockTo(lbl2, ControlEdge.Bottom, 2);

            // Number of +
            var posDays = series.Points.Where(x => x.YValues[0] > 0).Count();
            Label lbl4 = new Label()
            {
                Text = $"{"Gain Days:",12}{posDays,8}",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = _lblFont,
                ForeColor = Color.LawnGreen,
                Width = pnlText.Width / 3,
                Height = pnlText.Height / 5,
            };
            lbl4.DockTo(lbl1, ControlEdge.Right, 0);

            // Average +
            var posAvgset = series.Points.Where(x => x.YValues[0] > 0);

            double posAvg = 0;
            if (posAvgset.Count() > 0)
                posAvg = posAvgset.Average(x => x.YValues[0]);

            lblStr = ReturnFormat == ReturnFormat.Percent ?
                 $"{$"Avg %:",11}{avg,8:0.00%}" :
                  $"{$"Avg ATR:",11}{avg,8:0.00}";

            Label lbl5 = new Label()
            {
                Text = lblStr,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = _lblFont,
                ForeColor = Color.LawnGreen,
                Width = pnlText.Width / 3,
                Height = pnlText.Height / 5,
            };
            lbl5.DockTo(lbl4, ControlEdge.Bottom, 0);

            // Number of -
            var negDays = series.Points.Where(x => x.YValues[0] < 0).Count();
            Label lbl6 = new Label()
            {
                Text = $"{"Loss Days:",12}{negDays,8}",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = _lblFont,
                ForeColor = Color.PaleVioletRed,
                Width = pnlText.Width / 3,
                Height = pnlText.Height / 5,
            };
            lbl6.DockTo(lbl4, ControlEdge.Right, 2);

            // Average -
            var negAvgset = series.Points.Where(x => x.YValues[0] < 0);
            double negAvg = 0;
            if (negAvgset.Count() > 0)
                negAvg = negAvgset.Average(x => x.YValues[0]);

            Label lbl7 = new Label()
            {
                Text = lblStr,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = _lblFont,
                ForeColor = Color.PaleVioletRed,
                Width = pnlText.Width / 3,
                Height = pnlText.Height / 5,
            };
            lbl7.DockTo(lbl6, ControlEdge.Bottom, 0);

            pnlText.Controls.AddRange(new[] { lbl1, lbl2, lbl3, lbl4, lbl5, lbl6, lbl7 });
        }
        protected override void DrawAdditional()
        {
            DrawAverageReturnStripline();
        }

        protected override void SetYFormat()
        {
            switch (ReturnFormat)
            {
                case ReturnFormat.Percent:
                    {
                        chartArea.AxisY.MajorGrid.Interval = .01;
                        chartArea.AxisY.LabelStyle.Format = "0.00%";
                        chartArea.AxisY.MinorGrid.Interval = .0025;
                        chartArea.AxisY.LabelStyle.Interval = .01;
                    }
                    break;
                case ReturnFormat.ATR:
                    {
                        chartArea.AxisY.MajorGrid.Interval = .5;
                        chartArea.AxisY.LabelStyle.Format = "0.0";
                        chartArea.AxisY.MinorGrid.Interval = .5;
                        chartArea.AxisY.LabelStyle.Interval = .5;
                    }
                    break;
            }
        }
        protected override void SetXRange()
        {
            chartArea.AxisX.Minimum = 0;
            chartArea.AxisX.Maximum = series.Points.Count + 1;
        }
        private void DrawAverageReturnStripline()
        {
            if (series.Points.Count == 0)
                return;

            chartArea.AxisY.StripLines.Clear();

            var averageReturn = series.Points.Average(x => x.YValues[0]);
            var stdev = Stdev((from pt in series.Points select pt.YValues[0]).ToList());

            chartArea.AxisY.StripLines.Add(new StripLine()
            {
                Interval = 0,
                IntervalOffset = averageReturn,
                StripWidth = .0003,
                BackColor = Color.White
            });
            chartArea.AxisY.StripLines.Add(new StripLine()
            {
                Interval = 0,
                IntervalOffset = averageReturn - stdev,
                StripWidth = stdev * 2,
                BackColor = Color.FromArgb(64, 32, 0, 255)
            });
        }
    }
    public sealed class TrendNormalizedPerformanceTile : SingleStockTrendChartIndicatorTile
    {
        protected override string IndicatorDescription => "Displays distribution of next-day returns given the indicated primary and next-order trends.";

        Series slSeries;

        private decimal PercentBucketLimit => .03m;
        private decimal PercentBucketStep => 0.0025m;
        private List<decimal> PercentBuckets;

        private decimal ATRBucketLimit => 4.0m;
        private decimal ATRBucketStep => .25m;
        private List<decimal> ATRBuckets;

        public TrendNormalizedPerformanceTile()
        {
            SetTitle("Normalized Daily Return Distribution By Trend");
            InitializeBuckets();
        }

        protected override void InitializeChartArea()
        {
            chartArea = new ChartArea()
            {
                Name = "primary",
                BackColor = Color.FromArgb(255, 16, 16, 16)
            };

            chartArea.Position.Width = 100;
            chartArea.Position.Height = 100;
            chartArea.Position.X = 0;
            chartArea.Position.Y = 0;

            chartArea.InnerPlotPosition.Width = 100;
            chartArea.InnerPlotPosition.Height = 86;
            chartArea.InnerPlotPosition.X = 0;
            chartArea.InnerPlotPosition.Y = 4;

            // X Grid (on)
            chartArea.AxisX.MajorGrid.Enabled = true;
            chartArea.AxisX.MajorGrid.Interval = .01;
            chartArea.AxisX.MajorGrid.IntervalOffset = .0025;
            chartArea.AxisX.MajorGrid.LineColor = Color.Gray;
            chartArea.AxisX.Interval = PercentBucketStep.ToDouble();
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Number;
            chartArea.AxisX.LabelStyle.Enabled = true;
            chartArea.AxisX.LabelStyle.ForeColor = Color.White;
            chartArea.AxisX.LabelStyle.Format = "0%";
            chartArea.AxisX.LabelStyle.Interval = .01;
            chartArea.AxisX.LabelStyle.IntervalOffset = .0025;
            chartArea.AxisX.IsMarginVisible = true;

            chartArea.AxisY.IntervalType = DateTimeIntervalType.Number;
            chartArea.AxisY.Interval = 1;

            // Y Grid (on)                        
            chartArea.AxisY.MinorGrid.Enabled = true;
            chartArea.AxisY.LabelStyle.Enabled = false;
            chartArea.AxisY.LabelStyle.Interval = 1;
            chartArea.AxisY.MinorGrid.LineColor = Color.FromArgb(255, 32, 32, 32);
        }
        protected override void InitializeSeries()
        {
            series = new Series()
            {
                ChartArea = "primary",
                ChartType = SeriesChartType.Column
            };
            slSeries = new Series()
            {
                ChartArea = "primary",
                ChartType = SeriesChartType.Column
            };
            slSeries["PixelPointWidth"] = "2";

            chart.Series.Clear();
            chart.Series.Add(series);
            chart.Series.Add(slSeries);
        }

        private void InitializeBuckets()
        {
            PercentBuckets = new List<decimal>();
            for (decimal i = -PercentBucketLimit; i <= PercentBucketLimit; i += PercentBucketStep)
                PercentBuckets.Add(i);

            ATRBuckets = new List<decimal>();
            for (decimal i = -ATRBucketLimit; i <= ATRBucketLimit; i += ATRBucketStep)
                ATRBuckets.Add(i);
        }

        protected override void PopulateSeries()
        {
            series.Points.Clear();

            Security.SetSwingPointsAndTrends(Settings.Instance.DefaultSwingpointBarCount, BarSize);
            Security.SetSwingPointsAndTrends(Settings.Instance.DefaultSwingpointBarCount, BackBarSize);

            // List of bars for the base trend
            var baseTrendBars = Security.GetPriceBars(BarSize).
                Where(x => x.GetTrendType(Settings.Instance.DefaultSwingpointBarCount) == TrendType).
                ToList();

            // List of bars for the background trend
            var backgroundTrendBars = Security.GetPriceBars(BackBarSize).ToList();

            var dataPts = new List<decimal>();

            foreach (PriceBar bar in baseTrendBars)
            {
                if (bar.NextBar == null)
                    continue;

                // For each bar, include it only if it's background bar matches the selected trend (or if NotSet for BG)
                var bgBar = Calendar.GetContainingBar(bar, backgroundTrendBars);
                if (BackTrendType != TrendQualification.NotSet &&
                    bgBar.GetTrendType(Settings.Instance.DefaultSwingpointBarCount) != BackTrendType)
                    continue;

                switch (ReturnFormat)
                {
                    case ReturnFormat.Percent:
                        dataPts.Add(bar.NextBar.PercentChange);
                        break;
                    case ReturnFormat.ATR:
                        dataPts.Add(bar.NextBar.AtrChange);
                        break;
                }
            }

            dataPts.Sort((x, y) => x.CompareTo(y));

            foreach (decimal dataPt in dataPts)
            {
                var firstLargerVal = (ReturnFormat == ReturnFormat.Percent ? PercentBuckets : ATRBuckets).FirstOrDefault(x => x > dataPt);
                var thisValue = firstLargerVal - ((ReturnFormat == ReturnFormat.Percent ? PercentBucketStep : ATRBucketStep) / 2);

                var existingPt = series.Points.FirstOrDefault(pnt => pnt.XValue == thisValue.ToDouble());
                if (existingPt == null)
                {
                    var pt = new DataPoint()
                    {
                        XValue = thisValue.ToDouble(),
                        YValues = new[] { 1.0 },
                        Color = thisValue > 0 ? Color.Green : Color.Red,
                        IsValueShownAsLabel = false
                    };
                    series.Points.Add(pt);
                }
                else
                {
                    existingPt.YValues[0] += 1;
                }
            }
        }
        protected override void DrawInfo()
        {

        }
        protected override void DrawAdditional()
        {
            DrawStriplines();
        }
        protected override void SetYFormat()
        {
            switch (ReturnFormat)
            {
                case ReturnFormat.Percent:
                    {
                        chartArea.AxisY.MajorGrid.Interval = 1;
                        chartArea.AxisY.LabelStyle.Format = "0";
                        chartArea.AxisY.MinorGrid.Enabled = false;
                        chartArea.AxisY.LabelStyle.Interval = 1;
                    }
                    break;
                case ReturnFormat.ATR:
                    {
                        chartArea.AxisY.MajorGrid.Interval = 1;
                        chartArea.AxisY.LabelStyle.Format = "0";
                        chartArea.AxisY.MinorGrid.Enabled = false;
                        chartArea.AxisY.LabelStyle.Interval = 1;
                    }
                    break;
            }
        }
        protected override void SetXRange()
        {
            switch (ReturnFormat)
            {
                case ReturnFormat.Percent:
                    chartArea.AxisX.Minimum = (-PercentBucketLimit - (2 * PercentBucketStep)).ToDouble();
                    chartArea.AxisX.Maximum = (PercentBucketLimit + (2 * PercentBucketStep)).ToDouble();
                    break;
                case ReturnFormat.ATR:
                    chartArea.AxisX.Minimum = (-ATRBucketLimit - (2 * ATRBucketStep)).ToDouble();
                    chartArea.AxisX.Maximum = (ATRBucketLimit + (2 * ATRBucketStep)).ToDouble();
                    break;
            }

            switch (ReturnFormat)
            {
                case ReturnFormat.Percent:
                    {
                        chartArea.AxisX.MajorGrid.Interval = .01;
                        chartArea.AxisX.MajorGrid.IntervalOffset = 0;
                        chartArea.AxisX.Interval = PercentBucketStep.ToDouble();
                        chartArea.AxisX.LabelStyle.Format = "0.0%";
                        chartArea.AxisX.LabelStyle.Interval = .005;
                        chartArea.AxisX.LabelStyle.IntervalOffset = 0;
                    }
                    break;
                case ReturnFormat.ATR:
                    {
                        chartArea.AxisX.MajorGrid.Interval = .25;
                        chartArea.AxisX.MajorGrid.IntervalOffset = .5;
                        chartArea.AxisX.Interval = ATRBucketStep.ToDouble();
                        chartArea.AxisX.LabelStyle.Format = "0.0";
                        chartArea.AxisX.LabelStyle.Interval = 0.5;
                        chartArea.AxisX.LabelStyle.IntervalOffset = .5;
                    }
                    break;
            }
        }
        private void DrawStriplines()
        {
            if (series.Points.Count == 0)
                return;

            var ptSum = (from pt in series.Points select pt.YValues[0]).Sum();
            var avg = (from pt in series.Points select pt.XValue * pt.YValues[0]).Sum() / ptSum;

            slSeries.Points.Clear();

            slSeries.Points.Add(new DataPoint()
            {
                XValue = avg,
                YValues = new[] { series.Points.Max(x => x.YValues[0]) }
            });

            //
            // Zero
            //
            chartArea.AxisX.StripLines.Clear();
            chartArea.AxisX.StripLines.Add(new StripLine()
            {
                Interval = 0,
                IntervalOffset = (ReturnFormat == ReturnFormat.Percent ? -.0005 : -.05),
                StripWidth = (ReturnFormat == ReturnFormat.Percent ? .001 : .1),
                BackColor = Color.Purple
            });
        }
    }
    public sealed class SingleStockPerformanceOutlookTile : SingleStockTrendChartIndicatorTile
    {
        protected override string IndicatorDescription => "Displays average return over a 15 day period, by day";

        private int OutlookDays => 15;

        public SingleStockPerformanceOutlookTile()
        {
            SetTitle("15-day Average Change");
        }

        protected override void InitializeChartArea()
        {
            chartArea = new ChartArea()
            {
                Name = "primary",
                BackColor = Color.FromArgb(255, 16, 16, 16)
            };

            chartArea.Position.Width = 100;
            chartArea.Position.Height = 100;
            chartArea.Position.X = 0;
            chartArea.Position.Y = 0;

            chartArea.InnerPlotPosition.Width = 100;
            chartArea.InnerPlotPosition.Height = 86;
            chartArea.InnerPlotPosition.X = 0;
            chartArea.InnerPlotPosition.Y = 4;

            // X Grid (on)
            chartArea.AxisX.MajorGrid.Enabled = true;
            chartArea.AxisX.MajorGrid.Interval = 1;
            chartArea.AxisX.MajorGrid.IntervalOffset = .5;
            chartArea.AxisX.MajorGrid.LineColor = Color.Gray;
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Number;
            chartArea.AxisX.LabelStyle.Enabled = true;
            chartArea.AxisX.LabelStyle.ForeColor = Color.White;
            chartArea.AxisX.LabelStyle.Format = "0";
            chartArea.AxisX.LabelStyle.Interval = 1;
            chartArea.AxisX.LabelStyle.IntervalOffset = 0;
            chartArea.AxisX.IsMarginVisible = true;

            chartArea.AxisY.IntervalType = DateTimeIntervalType.Number;
            chartArea.AxisY.Interval = .0025;

            // Y Grid (on)                        
            chartArea.AxisY.MinorGrid.Enabled = false;
            chartArea.AxisY.LabelStyle.Enabled = false;
            chartArea.AxisY.LabelStyle.Interval = 1;
            chartArea.AxisY.MinorGrid.LineColor = Color.FromArgb(255, 32, 32, 32);
        }
        protected override void InitializeSeries()
        {
            series = new Series()
            {
                ChartArea = "primary",
                ChartType = SeriesChartType.Column
            };

            chart.Series.Clear();
            chart.Series.Add(series);
        }

        protected override void PopulateSeries()
        {
            series.Points.Clear();

            Security.SetSwingPointsAndTrends(Settings.Instance.DefaultSwingpointBarCount, BarSize);
            Security.SetSwingPointsAndTrends(Settings.Instance.DefaultSwingpointBarCount, BackBarSize);

            // List of bars for the base trend
            var baseTrendBars = Security.GetPriceBars(BarSize).
                Where(x => x.GetTrendType(Settings.Instance.DefaultSwingpointBarCount) == TrendType).
                ToList();

            // List of bars for the background trend
            var backgroundTrendBars = Security.GetPriceBars(BackBarSize).ToList();

            var dataPts = new decimal[OutlookDays].ToList();
            var dataPointCount = new int[OutlookDays].ToList();

            //
            // Generate one datapoint for each day from the base bar, as an average of that day's change
            //

            foreach (PriceBar bar in baseTrendBars)
            {
                // For each bar, include it only if it's background bar matches the selected trend (or if NotSet for BG)
                var bgBar = Calendar.GetContainingBar(bar, backgroundTrendBars);
                if (BackTrendType != TrendQualification.NotSet &&
                    bgBar.GetTrendType(Settings.Instance.DefaultSwingpointBarCount) != BackTrendType)
                    continue;

                int day = 0;
                PriceBar dayBar = bar;
                switch (ReturnFormat)
                {
                    case ReturnFormat.Percent:
                        {
                            while (dayBar.NextBar != null && day < OutlookDays)
                            {
                                dataPts[day] += dayBar.NextBar.PercentChange;
                                dataPointCount[day] += 1;
                                dayBar = dayBar.NextBar;
                                day += 1;
                            }
                        }
                        break;
                    case ReturnFormat.ATR:
                        {
                            while (dayBar.NextBar != null && day < OutlookDays)
                            {
                                dataPts[day] += dayBar.NextBar.AtrChange;
                                dataPointCount[day] += 1;
                                dayBar = dayBar.NextBar;
                                day += 1;
                            }
                        }
                        break;
                }
            }

            for (int k = 0; k < OutlookDays; k++)
                dataPts[k] /= (dataPointCount[k] > 0 ? dataPointCount[k] : 1);

            int d = 0;
            foreach (decimal dataPt in dataPts)
            {
                var pt = new DataPoint()
                {
                    XValue = ++d,
                    YValues = new[] { dataPt.ToDouble() },
                    Color = dataPt > 0 ? Color.Green : Color.Red,
                    IsValueShownAsLabel = true,
                    LabelForeColor = Color.White
                };
                series.Points.Add(pt);
            }
        }

        protected override void SetXRange()
        {
            chartArea.AxisX.Minimum = 0;
            chartArea.AxisX.Maximum = OutlookDays + 1;
        }
        protected override void SetYFormat()
        {
            switch (ReturnFormat)
            {
                case ReturnFormat.Percent:
                    foreach (DataPoint pt in series.Points)
                        pt.LabelFormat = "0.0%";
                    break;
                case ReturnFormat.ATR:
                    foreach (DataPoint pt in series.Points)
                        pt.LabelFormat = "0.0";
                    break;
            }
        }

        protected override void DrawAdditional()
        {
        }
        protected override void DrawInfo()
        {
        }
    }

}
