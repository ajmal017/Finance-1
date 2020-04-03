using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using Finance;
using static Finance.Helpers;

namespace Finance
{
    #region Chart Panels

    public abstract class ChartPanel : Panel
    {
        protected Panel pnlMain;
        protected Panel pnlControls;
        protected Panel pnlChart;

        protected abstract ChartBase PrimaryChart { get; set; }

        protected int _widthControlPanel = 300;

        [Initializer]
        private void InitializeLayout()
        {
            this.Dock = DockStyle.Fill;

            pnlMain = new Panel()
            {
                Dock = DockStyle.Fill
            };
            pnlControls = new FlowLayoutPanel()
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom,
                FlowDirection = FlowDirection.TopDown,
                BackColor = Color.White,
                Width = _widthControlPanel
            };
            pnlChart = new Panel()
            {
                Location = new Point(_widthControlPanel, 0),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom
            };

            pnlMain.Controls.AddRange(new[] { pnlControls, pnlChart });
            pnlMain.SizeChanged += (s, e) =>
            {
                pnlControls.Width = _widthControlPanel;
                pnlChart.Width = (pnlMain.Width - _widthControlPanel);
            };

            this.Controls.Add(pnlMain);
        }

        protected abstract void LoadPrimaryChart(ChartBase chart);
    }

    public class SecurityChartPanel : ChartPanel
    {

        #region Events

        private event ChartCursorChangedEventHandler PrimaryChartSelectedValueChanged;
        private void OnPrimaryChartSelectedValueChanged(object sender, ChartCursorChangedEventArgs e)
        {
            PrimaryChartSelectedValueChanged?.Invoke(sender, e);
        }

        private event EventHandler PrimaryChartViewChanged;
        private void OnPrimaryChartViewChanged(object sender, EventArgs e)
        {
            PrimaryChartViewChanged?.Invoke(sender, e);
        }

        private event EventHandler SecurityLoaded;
        private void OnSecurityLoaded()
        {
            SecurityLoaded?.Invoke(this.Security, null);
        }

        #endregion

        protected override ChartBase PrimaryChart { get; set; }

        public Security Security { get; private set; }
        public PriceBarSize PriceBarSize { get; private set; } = Settings.Instance.DefaultChartViewBarSize;
        public int SwingpointBarCount { get; private set; }

        #region Control Panel

        protected ChartTimeSpanChooserPanel pnlChartTimeSpanChooser;
        protected PriceBarInfoPanel pnlPriceBarInfo;
        protected SecurityTrendInfoPanel pnlSecurityTrendInfo;

        #endregion

        public SecurityChartPanel()
        {
            //_widthControlPanel = 50;
            this.InitializeMe();
            LoadPrimaryChart(new FinanceSecurityChart());
        }

        protected override void LoadPrimaryChart(ChartBase chart)
        {
            PrimaryChart = chart;
            PrimaryChart.Dock = DockStyle.Fill;
            pnlChart.Controls.Clear();
            pnlChart.Controls.Add(PrimaryChart);

            InitializeChartControls();
        }

        public void LoadSecurity(Security security, PriceBarSize priceBarSize, int swingpointBarCount)
        {
            if (security == this.Security && priceBarSize == this.PriceBarSize)
                return;

            this.Security = security;
            this.PriceBarSize = priceBarSize;
            this.SwingpointBarCount = swingpointBarCount;

            (PrimaryChart as FinanceSecurityChart).LoadSecurity(this.Security, this.PriceBarSize, this.SwingpointBarCount);

            OnSecurityLoaded();
        }

        #region Chart Controls

        private void InitializeChartControls()
        {
            //
            // Time Span Chooser
            //
            pnlChartTimeSpanChooser = new ChartTimeSpanChooserPanel();
            pnlChartTimeSpanChooser.Location = new Point(50, 0);

            pnlChart.Controls.Add(pnlChartTimeSpanChooser);
            pnlChartTimeSpanChooser.BringToFront();

            pnlChartTimeSpanChooser.SelectedValueChanged += (s, e) =>
            {
                var value = pnlChartTimeSpanChooser.SelectedValue;
                PrimaryChart.SetView(value.Start, value.End);
            };
            pnlChartTimeSpanChooser.SelectedBarSizeChanged += (s, e) =>
            {
                var value = pnlChartTimeSpanChooser.SelectedBarSize;
                LoadSecurity(this.Security, value, SwingpointBarCount);
            };
            PrimaryChart.ChartViewChanged += (s, e) =>
            {
                pnlChartTimeSpanChooser.SetValues(PrimaryChart.CurrentView.MinDate, PrimaryChart.CurrentView.MaxDate);
            };

            //
            // Price Bar Info
            //
            pnlPriceBarInfo = new PriceBarInfoPanel();
            pnlPriceBarInfo.DockTo(pnlChartTimeSpanChooser, ControlEdge.Right, 20);

            pnlChart.Controls.Add(pnlPriceBarInfo);
            pnlPriceBarInfo.BringToFront();

            PrimaryChart.SelectedValueChanged += (s, e) => pnlPriceBarInfo.Load((PrimaryChart as FinanceSecurityChart).SelectedValue);

            //
            // Trend Info
            //
            pnlSecurityTrendInfo = new SecurityTrendInfoPanel()
            {
                Size = new Size(pnlControls.Width, 20)
            };
            pnlControls.Controls.Add(pnlSecurityTrendInfo);

            this.SecurityLoaded += (s, e) => pnlSecurityTrendInfo.LoadSecurity(s as Security, this.PriceBarSize);
        }

        #endregion


    }
    public class SimResultsChartPanel : ChartPanel
    {

        #region Events

        private event ChartCursorChangedEventHandler PrimaryChartSelectedValueChanged;
        private void OnPrimaryChartSelectedValueChanged(object sender, ChartCursorChangedEventArgs e)
        {
            PrimaryChartSelectedValueChanged?.Invoke(sender, e);
        }
        private event EventHandler PrimaryChartViewChanged;
        private void OnPrimaryChartViewChanged(object sender, EventArgs e)
        {
            PrimaryChartViewChanged?.Invoke(sender, e);
        }

        #endregion

        protected override ChartBase PrimaryChart { get; set; }
        protected ChartBase SecondaryChart { get; set; }
        public Simulation Simulation { get; private set; }
        public int SwingPointBarCount { get; private set; }

        #region Control Panel

        protected ChartTimeSpanChooserPanel pnlChartTimeSpanChooser;
        protected PriceBarInfoPanel pnlPriceBarInfo;
        protected PositionListPanel pnlPositionChooser;
        protected Button btnReturnToAccountView;
        protected ComboBox boxSecondarySeriesSelector;

        #endregion

        public SimResultsChartPanel()
        {
            _widthControlPanel = 200;
            this.InitializeMe();

            LoadPrimaryChart(new FinanceSimResultsChart());
            LoadSecondaryChart(new FinanceSecurityChart());

            InitializeChartControls();
        }
        public void LoadSimulation(Simulation simulation)
        {
            if (simulation == this.Simulation)
                return;

            this.Simulation = simulation;

            var bc = simulation.Settings.ActiveStrategy.GetType().GetProperty("BarCount");
            if (bc != null)
            {
                this.SwingPointBarCount = (int)bc.GetValue(simulation.Settings.ActiveStrategy);
            }
            else
                this.SwingPointBarCount = Settings.Instance.DefaultSwingpointBarCount;

            (PrimaryChart as FinanceSimResultsChart).LoadSimulation(simulation);

            pnlPositionChooser.LoadSimulation(simulation);
        }
        private void LoadSecurity(Security security, PriceBarSize priceBarSize)
        {
            (SecondaryChart as FinanceSecurityChart).LoadSecurity(security, priceBarSize, SwingPointBarCount);
            (SecondaryChart as FinanceSecurityChart).LoadSignals(Simulation);
            (SecondaryChart as FinanceSecurityChart).LoadTrades(Simulation);

            LoadSecondarySeries(boxSecondarySeriesSelector.SelectedItem as string);

            (SecondaryChart as FinanceSecurityChart).SetView(PrimaryChart.CurrentView.MinDate, PrimaryChart.CurrentView.MaxDate);

            SecondaryChart.Visible = true;
            SecondaryChart.BringToFront();

            PrimaryChart.Visible = false;
            PrimaryChart.SendToBack();

            boxSecondarySeriesSelector.Enabled = true;

            pnlChartTimeSpanChooser.BringToFront();
            pnlPriceBarInfo.BringToFront();
        }

        protected override void LoadPrimaryChart(ChartBase chart)
        {
            PrimaryChart = chart;
            PrimaryChart.Dock = DockStyle.Fill;
            PrimaryChart.Visible = true;
            pnlChart.Controls.Clear();
            pnlChart.Controls.Add(PrimaryChart);

        }
        private void LoadSecondaryChart(ChartBase chart)
        {
            SecondaryChart = chart;
            SecondaryChart.Dock = DockStyle.Fill;
            SecondaryChart.Visible = false;
            pnlChart.Controls.Add(SecondaryChart);
            SecondaryChart.SendToBack();
        }
        private void LoadSecondarySeries(string series)
        {
            if (SecondaryChart.Visible == false)
                return;

            switch (series)
            {
                case "Volume":
                    (SecondaryChart as FinanceSecurityChart).LoadVolume();
                    (SecondaryChart as FinanceSecurityChart).ReloadSecondaryChart();
                    break;
                case "Positions":
                    (SecondaryChart as FinanceSecurityChart).LoadPositions(Simulation);
                    (SecondaryChart as FinanceSecurityChart).ReloadSecondaryChart();
                    break;
            }
        }

        #region Chart Controls

        private void InitializeChartControls()
        {
            //
            // Time Span Chooser
            //
            pnlChartTimeSpanChooser = new ChartTimeSpanChooserPanel();
            pnlChartTimeSpanChooser.Location = new Point(50, 0);

            pnlChart.Controls.Add(pnlChartTimeSpanChooser);
            pnlChartTimeSpanChooser.BringToFront();

            pnlChartTimeSpanChooser.SelectedValueChanged += (s, e) =>
            {
                var value = pnlChartTimeSpanChooser.SelectedValue;
                if (PrimaryChart.Visible)
                    PrimaryChart.SetView(value.Start, value.End);
                if (SecondaryChart.Visible)
                    SecondaryChart.SetView(value.Start, value.End);
            };

            PrimaryChart.ChartViewChanged += (s, e) => pnlChartTimeSpanChooser.SetValues(PrimaryChart.CurrentView.MinDate, PrimaryChart.CurrentView.MaxDate);
            SecondaryChart.ChartViewChanged += (s, e) => pnlChartTimeSpanChooser.SetValues(SecondaryChart.CurrentView.MinDate, SecondaryChart.CurrentView.MaxDate);

            pnlChartTimeSpanChooser.SelectedBarSizeChanged += (s, e) =>
            {
                var value = pnlChartTimeSpanChooser.SelectedBarSize;
                LoadSecurity(pnlPositionChooser.SelectedValue, value);
            };

            //
            // Price Bar Info
            //
            pnlPriceBarInfo = new PriceBarInfoPanel();
            pnlPriceBarInfo.DockTo(pnlChartTimeSpanChooser, ControlEdge.Right, 20);

            pnlChart.Controls.Add(pnlPriceBarInfo);
            pnlPriceBarInfo.BringToFront();

            SecondaryChart.VisibleChanged += (s, e) => pnlPriceBarInfo.Visible = SecondaryChart.Visible;
            SecondaryChart.SelectedValueChanged += (s, e) => pnlPriceBarInfo.Load((SecondaryChart as FinanceSecurityChart).SelectedValue);

            //
            // Position Chooser
            //
            pnlPositionChooser = new PositionListPanel("Position Chooser", null);
            pnlPositionChooser.Size = new Size(150, 150);

            bool firstLoad = true;
            pnlPositionChooser.SelectedValueChanged += (s, e) =>
            {
                if (firstLoad)
                {
                    firstLoad = false;
                    return;
                }
                LoadSecurity(pnlPositionChooser.SelectedValue, pnlChartTimeSpanChooser.SelectedBarSize);
            };
            pnlControls.Controls.Add(pnlPositionChooser);

            //
            // Return to Account View Button
            //
            btnReturnToAccountView = new Button()
            {
                Text = "Account Overview",
                Size = new Size(150, 50)
            };
            btnReturnToAccountView.Click += (s, e) =>
            {
                PrimaryChart.Visible = true;
                PrimaryChart.BringToFront();

                SecondaryChart.Visible = false;
                SecondaryChart.SendToBack();

                boxSecondarySeriesSelector.Enabled = false;

                pnlChartTimeSpanChooser.BringToFront();
            };
            pnlControls.Controls.Add(btnReturnToAccountView);

            //
            // Secondary Series Selector
            //
            boxSecondarySeriesSelector = new ComboBox()
            {
                Width = 150,
                Enabled = false
            };
            boxSecondarySeriesSelector.Items.AddRange(new[] { "Volume", "Positions" });
            boxSecondarySeriesSelector.SelectedItem = "Volume";
            boxSecondarySeriesSelector.SelectedValueChanged += (s, e) =>
            {
                LoadSecondarySeries(boxSecondarySeriesSelector.SelectedItem as string);
            };
            pnlControls.Controls.Add(boxSecondarySeriesSelector);
        }

        #endregion
    }

    #endregion
    #region Charts

    public abstract class ChartBase : Chart
    {
        #region Events

        public event EventHandler ChartViewChanged;
        private void OnChartViewChanged() => ChartViewChanged?.Invoke(this, null);

        public event ChartCursorChangedEventHandler SelectedValueChanged;
        private void OnSelectedValueChanged()
        {
            SelectedValueChanged?.Invoke(this, new ChartCursorChangedEventArgs(PrimaryChartArea.CursorX.Position, PrimaryChartArea.CursorY.Position));
        }

        #endregion

        public FinanceChartView CurrentView { get; protected set; }

        protected FinanceSeries PrimarySeries { get; set; }
        protected FinanceSeries SecondarySeries { get; set; }

        protected FinanceChartArea PrimaryChartArea { get; set; }
        protected FinanceChartArea SecondaryChartArea { get; set; }

        public bool AllowDragView { get; set; } = true;
        public bool ShowHorizontalCursor { get; set; } = true;
        public bool ShowVerticalCursor { get; set; } = true;

        protected bool NoData => (PrimarySeries == null);
        public (DateTime min, DateTime max) PrimarySeriesDateRange
        {
            get
            {
                if (PrimarySeries == null)
                    return (DateTime.MinValue, DateTime.MaxValue);
                return (PrimarySeries.MinXValue, PrimarySeries.MaxXValue);
            }
        }

        [Initializer]
        protected abstract void InitializeChartAreas();

        public void SetView(DateTime minView, DateTime maxView)
        {
            if (CurrentView == null)
                CurrentView = new FinanceChartView(minView, maxView);
            else
            {
                CurrentView.MinDate = minView;
                CurrentView.MaxDate = maxView;
            }

            RedrawAxes();
            Invalidate();
        }
        public void ReloadSecondaryChart()
        {
            SetSecondaryChartAreaY1Axis();
            Invalidate();
        }
        protected void ReloadChart()
        {
            if (NoData)
            {
                Invalidate();
                return;
            }

            if (PrimarySeries is FinanceAccountingSeries acctSer)
                CurrentView = new FinanceChartView(acctSer.MinXValue, acctSer.MaxXValue);
            else if (CurrentView == null)
                CurrentView = FinanceChartView.Default();

            RedrawAxes();
            SetCursors();
            Invalidate();
        }
        protected void RedrawAxes()
        {
            SetXAxis();
            SetPrimaryChartAreaY1Axis();
            SetSecondaryChartAreaY1Axis();
        }
        protected void SetXAxis()
        {
            PrimaryChartArea.AxisX.Minimum = CurrentView.MinDate.ToOADate();
            PrimaryChartArea.AxisX.Maximum = CurrentView.MaxDate.ToOADate();

            if (SecondaryChartArea != null)
            {
                SecondaryChartArea.AxisX.Minimum = CurrentView.MinDate.ToOADate();
                SecondaryChartArea.AxisX.Maximum = CurrentView.MaxDate.ToOADate();
            }

            OnChartViewChanged();
        }
        protected virtual void SetPrimaryChartAreaY1Axis()
        {
            var minAct = Math.Max(PrimarySeries.MinYValue(CurrentView.MinDate, CurrentView.MaxDate).ToDouble(), 0);
            var maxAct = PrimarySeries.MaxYValue(CurrentView.MinDate, CurrentView.MaxDate).ToDouble();

            var minAdj = RoundDownWholeNumberLargestPlace(minAct.ToDecimal()).ToDouble();
            var maxAdj = RoundUpWholeNumberLargestPlace(maxAct.ToDecimal()).ToDouble();

            if ((maxAdj / maxAct) > 1.40)
                maxAdj = RoundUpWholeNumber2ndLargestPlace(maxAct.ToDecimal()).ToDouble();
            else
            {
                while ((maxAdj / maxAct < 1.2))
                    maxAdj += 0.25;
            }

            if ((minAdj / minAct) < .60)
                minAdj = RoundDownWholeNumber2ndLargestPlace(minAct.ToDecimal()).ToDouble();
            else
            {
                while ((minAdj / minAct) > .80)
                    minAdj -= 0.25;
            }
                


            PrimaryChartArea.AxisY.Maximum = maxAdj;
            PrimaryChartArea.AxisY.Minimum = minAdj;

            PrimaryChartArea.AxisY.Interval = (maxAdj - minAdj) / 10;
        }
        protected virtual void SetSecondaryChartAreaY1Axis()
        {
            if (SecondaryChartArea == null)
                return;

            /*
             *  Scale the SecondaryChartArea Y axis - this will be either volume or position, so whole Ints
             */

            int minAct = Convert.ToInt32(SecondarySeries.MinYValue(CurrentView.MinDate, CurrentView.MaxDate));
            int maxAct = Convert.ToInt32(SecondarySeries.MaxYValue(CurrentView.MinDate, CurrentView.MaxDate));

            // Adjust the maximum to round to the largest place value, or second largest if there is too much whitespace
            int maxAdj = RoundUpWholeNumberLargestPlace(maxAct);
            if ((maxAdj / maxAct.ToDouble()) > 1.25)
                maxAdj = RoundUpWholeNumber2ndLargestPlace(maxAct);

            int minAdj = RoundDownWholeNumberLargestPlace(minAct);
            if ((minAdj / minAct.ToDouble()) > 1.25)
                minAdj = RoundDownWholeNumber2ndLargestPlace(minAct);


            SecondaryChartArea.AxisY.Maximum = maxAdj;
            SecondaryChartArea.AxisY.Minimum = minAdj;

            SecondaryChartArea.AxisY.Interval = (maxAdj - minAdj) / 10;
        }

        #region Cursor Management

        public (DateTime xValue, double yValue) CursorChartPoint
        {
            get
            {
                return (DateTime.FromOADate(PrimaryChartArea.CursorX.Position), PrimaryChartArea.CursorY.Position);
            }
        }

        private void SetCursors()
        {
            if (ShowVerticalCursor)
            {
                PrimaryChartArea.CursorX.IsUserEnabled = true;
                PrimaryChartArea.CursorX.LineColor = Color.Red;
                PrimaryChartArea.CursorX.IntervalType = DateTimeIntervalType.Days;
                PrimaryChartArea.CursorX.Interval = 1;

                if (SecondaryChartArea != null)
                {
                    SecondaryChartArea.CursorX.IsUserEnabled = true;
                    SecondaryChartArea.CursorX.LineColor = Color.Red;
                    SecondaryChartArea.CursorX.IntervalType = DateTimeIntervalType.Days;
                    SecondaryChartArea.CursorX.Interval = 1;
                }
            }
            if (ShowHorizontalCursor)
            {
                PrimaryChartArea.CursorY.IsUserEnabled = true;
                PrimaryChartArea.CursorY.LineColor = Color.Red;
                PrimaryChartArea.CursorY.IntervalType = DateTimeIntervalType.Number;
                PrimaryChartArea.CursorY.Interval = 0.01;

                if (SecondaryChartArea != null)
                {
                    SecondaryChartArea.CursorY.IsUserEnabled = true;
                    SecondaryChartArea.CursorY.LineColor = Color.Red;
                    SecondaryChartArea.CursorY.IntervalType = DateTimeIntervalType.Number;
                    SecondaryChartArea.CursorY.Interval = 1;
                }
            }

            this.CursorPositionChanging += (s, e) =>
            {
                if (e.ChartArea.Name == "primary" && e.Axis.AxisName == AxisName.X)
                    SetCursorSecondaryChartArea(e.NewPosition, 0);
                if (e.ChartArea.Name == "secondary" && e.Axis.AxisName == AxisName.X)
                    SetCursorPrimaryChartArea(e.NewPosition, 0);

                OnSelectedValueChanged();

            };


        }
        public void SetCursorPrimaryChartArea(double X, double Y)
        {
            PrimaryChartArea.CursorX.SetCursorPosition(X);

            var collection = PrimarySeries.Points.Where(pt => pt.XValue == X);
            var yValue = collection.FirstOrDefault();
            if (yValue == null)
                Y = 0;
            else
                Y = yValue.YValues.Max();

            PrimaryChartArea.CursorY.SetCursorPosition(Y);
        }
        public void SetCursorSecondaryChartArea(double X, double Y)
        {
            if (SecondaryChartArea == null)
                return;

            /*
             *  On the secondary chart, we want the Y cursor to lock to the value of the bar (volume or position size)
             */
            SecondaryChartArea.CursorX.SetCursorPosition(X);

            var collection = SecondarySeries.Points.Where(pt => pt.XValue == X);
            var yValue = collection.FirstOrDefault();
            if (yValue == null)
                Y = 0;
            else
                Y = yValue.YValues.Max();

            SecondaryChartArea.CursorY.SetCursorPosition(Y);
        }

        #endregion
        #region Drag View

        private int lastPositionX { get; set; }

        [Initializer]
        private void SetMouseDrag()
        {
            MouseMove += MoveTimeframeOnDrag;
        }
        private void MoveTimeframeOnDrag(object sender, MouseEventArgs e)
        {
            if (!AllowDragView)
                return;

            if (e.Button == MouseButtons.Left)
            {
                int deltaMove = (lastPositionX - e.X);
                SetDragView(deltaMove);
            }
            lastPositionX = e.X;
        }
        private void SetDragView(int deltaX)
        {
            double scaleFactor = 1.04;
            var percentMove = scaleFactor * (deltaX / ((PrimaryChartArea.InnerPlotPosition.Width / 100d) * Width));
            var daysDrag = (CurrentView.Span.TotalDays * percentMove);

            if (CurrentView.MinDate.AddDays(daysDrag) > PrimarySeries.MaxXValue)
                return;
            if (CurrentView.MaxDate.AddDays(daysDrag) < PrimarySeries.MinXValue)
                return;

            CurrentView.ShiftDays(daysDrag);
            RedrawAxes();
            Invalidate();
        }

        #endregion
        #region Paint Effects

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PaintNoData(e);
            PaintAxisXLabel(e);
            PaintAxisYLabel(e);
        }
        protected void PaintNoData(PaintEventArgs e)
        {
            if (!NoData)
                return;

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, 35, 35, 35)))
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.DrawString($"No Data", Helpers.SystemFont(72), brush, this.Width / 3, this.Height / 5 * 2, new StringFormat());
                return;
            }
        }
        protected void PaintAxisXLabel(PaintEventArgs e)
        {
            if (double.IsNaN(PrimaryChartArea.CursorX.Position))
                return;

            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                g.DrawString(
                    $"{DateTime.FromOADate(PrimaryChartArea.CursorX.Position):ddd MM/dd/yy}",
                    Helpers.SystemFont(12),
                    brush,
                    (float)PrimaryChartArea.AxisX.ValueToPixelPosition(PrimaryChartArea.CursorX.Position),
                    Convert.ToInt32(PrimaryChartArea.AxisY.ValueToPixelPosition(PrimaryChartArea.AxisY.Minimum)) - 20);

            }
        }
        protected void PaintAxisYLabel(PaintEventArgs e)
        {
            if (double.IsNaN(PrimaryChartArea.CursorY.Position) || SecondaryChartArea == null || double.IsNaN(SecondaryChartArea.CursorY.Position))
                return;

            Point pt2 = new Point(
                Convert.ToInt32(PrimaryChartArea.CursorX.Position),
                Convert.ToInt32(PrimaryChartArea.AxisY.ValueToPixelPosition(PrimaryChartArea.AxisY.Minimum)));

            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                g.DrawString(
                    $"{PrimaryChartArea.CursorY.Position.ToString(PrimaryChartArea.AxisYLabelFormat)}",
                    Helpers.SystemFont(12),
                    brush,
                    Convert.ToInt32(PrimaryChartArea.AxisX.ValueToPixelPosition(CurrentView.MinDate.ToOADate())),
                    (float)PrimaryChartArea.AxisY.ValueToPixelPosition(PrimaryChartArea.CursorY.Position));

                g.DrawString(
                    $"{SecondaryChartArea.CursorY.Position.ToString(SecondaryChartArea.AxisYLabelFormat)}",
                    Helpers.SystemFont(12),
                    brush,
                    (float)SecondaryChartArea.AxisX.ValueToPixelPosition(SecondaryChartArea.CursorX.Position),
                    Convert.ToInt32(SecondaryChartArea.AxisY.ValueToPixelPosition(SecondaryChartArea.CursorY.Position)) - 20);
            }
        }

        #endregion
    }

    public class FinanceChartArea : ChartArea
    {
        private string _AxisYLabelFormat { get; set; }
        public string AxisYLabelFormat
        {
            get
            {
                return _AxisYLabelFormat;
            }
            set
            {
                _AxisYLabelFormat = value;
                this.AxisY.LabelStyle.Format = value;
            }
        }

        public FinanceChartArea(string name) : base(name)
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyles()
        {
            this.BackColor = Color.Black;

            // Axis X Style
            this.AxisX.IntervalType = DateTimeIntervalType.Weeks;
            this.AxisX.IsStartedFromZero = false;
            this.AxisX.Interval = 1;
            this.AxisX.IntervalOffset = 1;
            this.AxisX.Title = "Date";
            this.AxisX.LabelStyle = new LabelStyle() { Format = "MM/dd/yy" };

            this.AxisX.MajorGrid.LineColor = Color.FromArgb(25, 25, 25);
            this.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            this.AxisX.MajorGrid.LineWidth = 1;
            this.AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Days;
            this.AxisX.MajorGrid.Interval = 1;
            this.AxisX.MajorGrid.IntervalOffset = -0.5;

            // Axis Y Style
            this.AxisY.IntervalType = DateTimeIntervalType.Number;
            this.AxisY.IsStartedFromZero = false;
            this.AxisY.LabelStyle.Format = AxisYLabelFormat;
            this.AxisY.MajorGrid.LineColor = Color.FromArgb(25, 25, 25);
            this.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            this.AxisY.MajorGrid.LineWidth = 1;
        }

        #region Striplines

        [Initializer]
        private void DrawStriplines()
        {
            this.AxisX.StripLines.Clear();

            DrawWeekendStriplines();
            DrawMonthStartStriplines();
            DrawYearStartStriplines();
            DrawHolidayStriplines();
        }
        private void DrawWeekendStriplines()
        {
            StripLine slWeekend = new StripLine()
            {
                IntervalOffset = -1.5,
                IntervalOffsetType = DateTimeIntervalType.Days,
                Interval = 1,
                IntervalType = DateTimeIntervalType.Weeks,
                StripWidth = 2,
                StripWidthType = DateTimeIntervalType.Days,
                BackColor = Color.FromArgb(5, 5, 5),
                BorderColor = Color.FromArgb(10, 10, 10)
            };
            this.AxisX.StripLines.Add(slWeekend);
        }
        private void DrawMonthStartStriplines()
        {
            StripLine slMonth = new StripLine()
            {
                IntervalOffset = -.5,
                IntervalOffsetType = DateTimeIntervalType.Days,
                Interval = 1,
                IntervalType = DateTimeIntervalType.Months,
                StripWidth = .10,
                StripWidthType = DateTimeIntervalType.Days,
                BackColor = Color.FromArgb(32, 32, 0),
                BorderColor = Color.FromArgb(32, 32, 0)
            };
            this.AxisX.StripLines.Add(slMonth);
        }
        private void DrawYearStartStriplines()
        {
            StripLine slYear = new StripLine()
            {
                IntervalOffset = -.5,
                IntervalOffsetType = DateTimeIntervalType.Days,
                Interval = 1,
                IntervalType = DateTimeIntervalType.Years,
                StripWidth = .10,
                StripWidthType = DateTimeIntervalType.Days,
                BackColor = Color.FromArgb(128, 0, 0),
                BorderColor = Color.FromArgb(128, 0, 0)
            };
            this.AxisX.StripLines.Add(slYear);
        }
        private void DrawHolidayStriplines()
        {
            foreach (DateTime holiday in Calendar.AllHolidays(DateTime.Today.AddYears(-10), DateTime.Today))
            {
                StripLine slHoliday = new StripLine()
                {
                    IntervalOffset = holiday.ToOADate() - 0.5,
                    IntervalType = DateTimeIntervalType.Auto,
                    StripWidth = 1,
                    StripWidthType = DateTimeIntervalType.Days,
                    BackColor = Color.FromArgb(25, 0, 25),
                    BorderColor = Color.FromArgb(10, 10, 10),
                };
                this.AxisX.StripLines.Add(slHoliday);
            }

        }

        #endregion
    }

    public class FinanceSecurityChart : ChartBase
    {
        public Security Security { get; private set; }
        public PriceBarSize PriceBarSize { get; private set; }
        public PriceBar SelectedValue
        {
            get
            {
                return PrimarySeries.ChartPointObject(PrimaryChartArea.CursorX.Position) as PriceBar;
            }
        }

        protected FinanceVolumeSeries VolumeSeries { get; set; }
        protected FinancePositionSeries PositionSeries { get; set; }
        protected FinanceSignalSeries SignalSeries { get; set; }
        protected FinanceTradeSeries TradeSeries { get; set; }

        protected FinanceSwingPointSeries SwingPointSeries { get; set; }
        protected FinanceTrendSeries TrendSeries { get; set; }

        public FinanceSecurityChart()
        {
            this.InitializeMe();
        }

        protected override void InitializeChartAreas()
        {
            ChartAreas.Clear();

            //
            // Primary Chart
            //
            PrimaryChartArea = new FinanceChartArea("primary");

            PrimaryChartArea.Position.Auto = false;
            PrimaryChartArea.Position.X = 50;
            PrimaryChartArea.Position.Y = 0;
            PrimaryChartArea.Position.Height = 70;
            PrimaryChartArea.Position.Width = 100;

            PrimaryChartArea.AxisX.IsMarginVisible = false;
            PrimaryChartArea.AxisX.MajorTickMark.Enabled = false;

            PrimaryChartArea.InnerPlotPosition.Auto = false;
            PrimaryChartArea.InnerPlotPosition.X = 5;
            PrimaryChartArea.InnerPlotPosition.Y = 5;
            PrimaryChartArea.InnerPlotPosition.Width = 90;
            PrimaryChartArea.InnerPlotPosition.Height = 90;

            PrimaryChartArea.AxisYLabelFormat = "$0.00";

            //
            // Secondary Chart
            //
            SecondaryChartArea = new FinanceChartArea("secondary");

            SecondaryChartArea.Position.Auto = false;
            SecondaryChartArea.Position.X = 50;
            SecondaryChartArea.Position.Y = 70;
            SecondaryChartArea.Position.Height = 30;
            SecondaryChartArea.Position.Width = 100;

            SecondaryChartArea.InnerPlotPosition.X = 5;
            SecondaryChartArea.InnerPlotPosition.Y = 5;
            SecondaryChartArea.InnerPlotPosition.Auto = false;
            SecondaryChartArea.InnerPlotPosition.Width = 90;
            SecondaryChartArea.InnerPlotPosition.Height = 80;

            SecondaryChartArea.AxisYLabelFormat = "0";

            ChartAreas.Add(PrimaryChartArea);
            ChartAreas.Add(SecondaryChartArea);
        }
        private void ClearAllSeries()
        {
            this.Series.Clear();

            PrimarySeries = null;
            VolumeSeries = null;
            PositionSeries = null;
            SignalSeries = null;
            SwingPointSeries = null;
            TrendSeries = null;
        }

        public void LoadSecurity(Security security, PriceBarSize priceBarSize, int swingpointBarCount)
        {
            ClearAllSeries();
            this.Security = security;
            this.PriceBarSize = priceBarSize;

            if (Security == null)
            {
                ReloadChart();
                return;
            }

            PrimarySeries = new FinanceSecuritySeries(Security, PriceBarSize);
            PrimarySeries.Name = "security";
            this.Series.Add(PrimarySeries);

            LoadSwingPoints(swingpointBarCount);
            LoadSwingPointTrends(swingpointBarCount);
            LoadVolume();
            ShowAtrBands();

            ReloadChart();

            Security.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Security.IntradayMinuteBars))
                {
                    Invoke(new Action(() =>
                    {
                        PrimarySeries.RefreshSeries();
                        if (SecondarySeries.Name == "volume")
                        {
                            SecondarySeries.RefreshSeries();
                        }
                    }
                    ));
                };
            };
        }

        private void LoadSwingPoints(int BarCount)
        {
            SwingPointSeries = new FinanceSwingPointSeries(Security, PriceBarSize, BarCount);
            SwingPointSeries.Name = "swingpoints";
            this.Series.Add(SwingPointSeries);
        }
        private void LoadSwingPointTrends(int BarCount)
        {
            TrendSeries = new FinanceTrendSeries(Security, PriceBarSize, BarCount);
            TrendSeries.Name = "trends";
            this.Series.Add(TrendSeries);
        }

        public void LoadSignals(Simulation simulation)
        {
            List<Signal> signals = simulation.PortfolioManager.StrategyManager.GetSignalHistory(Security);
            SignalSeries = new FinanceSignalSeries(signals);
            SignalSeries.Name = "signals";
            this.Series.Add(SignalSeries);
        }
        public void LoadTrades(Simulation simulation)
        {
            TradeSeries = new FinanceTradeSeries(Security, simulation);
            TradeSeries.Name = "trades";
            this.Series.Add(TradeSeries);
        }
        public void LoadVolume()
        {
            this.Series.Remove(SecondarySeries);
            VolumeSeries = new FinanceVolumeSeries(Security, PriceBarSize);
            VolumeSeries.Name = "volume";
            SecondarySeries = VolumeSeries;
            this.Series.Add(VolumeSeries);
            SecondaryChartArea.AxisYLabelFormat = "0";
        }
        public void LoadPositions(Simulation simulation)
        {
            this.Series.Remove(SecondarySeries);
            PositionSeries = new FinancePositionSeries(Security, simulation);
            PositionSeries.Name = "positions";
            SecondarySeries = PositionSeries;
            this.Series.Add(SecondarySeries);
            SecondaryChartArea.AxisYLabelFormat = "$0.00";
        }

        public void ShowAtrBands()
        {
            if (PriceBarSize != PriceBarSize.Daily)
                return;

            PrimaryChartArea.AxisY.StripLines.Clear();

            //
            // Add a stripline for 1 ATR from current close
            //
            var atrValue = Security.GetLastBar(PriceBarSize).AverageTrueRange();
            var lastClose = Security.IntradayBar?.Close ?? Security.GetLastBar(PriceBarSize).Close;

            //
            // Positive 1
            //
            PrimaryChartArea.AxisY.StripLines.Add(new StripLine()
            {
                Interval = 0,
                IntervalOffset = (lastClose).ToDouble(),
                StripWidth = atrValue.ToDouble(),
                BackColor = Color.FromArgb(32, 0, 0, 255),
                                BorderDashStyle = ChartDashStyle.Dot,
                BorderColor = Color.FromArgb(255, 64, 64, 64),
                BorderWidth = 1
            });
            //
            // Positive 2
            //
            PrimaryChartArea.AxisY.StripLines.Add(new StripLine()
            {
                Interval = 0,
                IntervalOffset = (lastClose + atrValue).ToDouble(),
                StripWidth = atrValue.ToDouble(),
                BackColor = Color.FromArgb(32, 0, 0, 128),
                BorderDashStyle = ChartDashStyle.Dot,
                BorderColor = Color.FromArgb(255,64,64,64),
                BorderWidth = 1
            });

            //
            // Negative 1
            //
            PrimaryChartArea.AxisY.StripLines.Add(new StripLine()
            {
                Interval = 0,
                IntervalOffset = (lastClose - atrValue).ToDouble(),
                StripWidth = atrValue.ToDouble(),
                BackColor = Color.FromArgb(32, 255, 0, 0),
                BorderDashStyle = ChartDashStyle.Dot,
                BorderColor = Color.FromArgb(255, 64, 64, 64),
                BorderWidth = 1
            });
            //
            // Negative 2
            //
            PrimaryChartArea.AxisY.StripLines.Add(new StripLine()
            {
                Interval = 0,
                IntervalOffset = (lastClose - (2 * atrValue)).ToDouble(),
                StripWidth = atrValue.ToDouble(),
                BackColor = Color.FromArgb(32, 128, 0, 0),
                BorderDashStyle = ChartDashStyle.Dot,
                BorderColor = Color.FromArgb(255, 64, 64, 64),
                BorderWidth = 1
            });
        }
    }
    public class FinanceSimResultsChart : ChartBase
    {
        public Simulation Simulation { get; private set; }

        public FinanceSimResultsChart()
        {
            this.InitializeMe();
        }

        protected override void InitializeChartAreas()
        {
            ChartAreas.Clear();

            //
            // Primary Chart
            //
            PrimaryChartArea = new FinanceChartArea("primary");

            PrimaryChartArea.Position.Auto = false;
            PrimaryChartArea.Position.X = 50;
            PrimaryChartArea.Position.Y = 0;
            PrimaryChartArea.Position.Height = 70;
            PrimaryChartArea.Position.Width = 100;

            PrimaryChartArea.AxisX.IsMarginVisible = false;
            PrimaryChartArea.AxisX.MajorTickMark.Enabled = false;

            PrimaryChartArea.InnerPlotPosition.Auto = false;
            PrimaryChartArea.InnerPlotPosition.X = 5;
            PrimaryChartArea.InnerPlotPosition.Y = 5;
            PrimaryChartArea.InnerPlotPosition.Width = 90;
            PrimaryChartArea.InnerPlotPosition.Height = 90;

            PrimaryChartArea.AxisYLabelFormat = "$0.00";

            //
            // Secondary Chart
            //
            SecondaryChartArea = new FinanceChartArea("secondary");

            SecondaryChartArea.Position.Auto = false;
            SecondaryChartArea.Position.X = 50;
            SecondaryChartArea.Position.Y = 70;
            SecondaryChartArea.Position.Height = 30;
            SecondaryChartArea.Position.Width = 100;

            SecondaryChartArea.InnerPlotPosition.X = 5;
            SecondaryChartArea.InnerPlotPosition.Y = 5;
            SecondaryChartArea.InnerPlotPosition.Auto = false;
            SecondaryChartArea.InnerPlotPosition.Width = 90;
            SecondaryChartArea.InnerPlotPosition.Height = 80;

            SecondaryChartArea.AxisYLabelFormat = "$0.00";

            ChartAreas.Add(PrimaryChartArea);
            ChartAreas.Add(SecondaryChartArea);
        }
        public void LoadSimulation(Simulation simulation)
        {
            this.Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.Series.Clear();

            if (Simulation == null)
            {
                PrimarySeries = null;
                SecondarySeries = null;
                ReloadChart();
                return;
            }

            PrimarySeries = new FinanceAccountingSeries(Simulation);
            SecondarySeries = new FinancePositionSeries(Simulation);

            this.Series.Add(PrimarySeries);
            this.Series.Add(SecondarySeries);

            ReloadChart();
        }
    }

    #endregion
    #region Components

    public class FinanceChartView
    {
        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
        public TimeSpan Span => (MaxDate - MinDate);

        public FinanceChartView(DateTime minDate, DateTime maxDate)
        {
            MinDate = minDate;
            MaxDate = maxDate;
        }
        public void ShiftDays(double days)
        {
            MinDate = MinDate.AddDays(days);
            MaxDate = MaxDate.AddDays(days);
        }

        public static FinanceChartView Default()
        {
            return new FinanceChartView(DateTime.Today.AddDays(15).AddYears(-1), DateTime.Today.AddDays(15));
        }
    }
    public class PriceBarInfoPanel : Panel
    {
        Size _defaultSize = new Size(750, 20);

        Label lblDisplayTxt;
        public PriceBar PriceBar { get; private set; }

        public PriceBarInfoPanel()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void SetStyle()
        {
            Size = _defaultSize;
            BackColor = Color.White;
            this.ControlAdded += (s, e) =>
            {
                if (e.Control.Right > this.Width)
                    this.Width = e.Control.Right;
            };

            lblDisplayTxt = new Label()
            {
                Dock = DockStyle.Fill,
                Font = Helpers.SystemFont(10),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White
            };

            this.Controls.Add(lblDisplayTxt);
        }

        public void Load(PriceBar priceBar)
        {
            if (priceBar == null) return;

            PriceBar = priceBar;

            string displayTxt = string.Format($"{priceBar.BarDateTime.ToShortDateString()} " +
                $"O: {priceBar.Open:0.00} " +
                $"H: {priceBar.High:0.00} " +
                $"L: {priceBar.Low:0.00} " +
                $"C: {priceBar.Close:0.00} " +
                $"CHG: {priceBar.Change:0.00} " +
                $"RNG: {priceBar.Range:0.00} " +
                $"VOL: {PriceBar.Volume}");

            lblDisplayTxt.Text = displayTxt;
        }

    }
    public class ChartTimeSpanChooserPanel : Panel
    {
        #region Events

        public event EventHandler SelectedValueChanged;
        private void OnSelectedValueChanged()
        {
            SelectedValue = (dtpStart.Value, dtpEnd.Value);
            this.SelectedValueChanged?.Invoke(this, new EventArgs());
        }
        public event EventHandler SelectedBarSizeChanged;
        private void OnSelectedBarSizeChanged()
        {
            this.SelectedBarSizeChanged?.Invoke(this, new EventArgs());
        }

        #endregion

        Size _defaultSize = new Size(100, 20);

        public (DateTime Start, DateTime End) SelectedValue { get; private set; }
        public PriceBarSize SelectedBarSize { get; private set; }

        DateTimePicker dtpStart;
        DateTimePicker dtpEnd;
        Label lbl5yr, lbl2yr, lbl1yr, lbl6mo, lbl3mo;
        ComboBox boxBarSize;

        public ChartTimeSpanChooserPanel()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyle()
        {
            this.Size = _defaultSize;
            this.ControlAdded += (s, e) =>
            {
                if (e.Control.Right > this.Width)
                    this.Width = e.Control.Right;
            };
        }
        [Initializer]
        private void InitializePickers()
        {
            dtpStart = new DateTimePicker()
            {
                Width = 75,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = @"MM/dd/yy"
            };
            dtpEnd = new DateTimePicker()
            {
                Width = 75,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = @"MM/dd/yy"
            };
            dtpStart.Leave += (s, e) => OnSelectedValueChanged();
            dtpEnd.Leave += (s, e) => OnSelectedValueChanged();

            dtpStart.Location = new Point(0, 0);
            dtpEnd.DockTo(dtpStart, ControlEdge.Right);

            this.Controls.AddRange(new[] { dtpStart, dtpEnd });
        }
        [Initializer]
        private void InitializeButtons()
        {
            lbl5yr = new Label()
            {
                Text = "5 Yr",
                Size = new Size(40, 20),
                BorderStyle = BorderStyle.FixedSingle,
                Font = SystemFont(8),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0),
                BackColor = Color.White
            };
            lbl5yr.Click += (s, e) => ButtonClick(lbl5yr.Text);
            lbl2yr = new Label()
            {
                Text = "2 Yr",
                Size = new Size(40, 20),
                BorderStyle = BorderStyle.FixedSingle,
                Font = SystemFont(8),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0),
                BackColor = Color.White
            };
            lbl2yr.Click += (s, e) => ButtonClick(lbl2yr.Text);
            lbl1yr = new Label()
            {
                Text = "1 Yr",
                Size = new Size(40, 20),
                BorderStyle = BorderStyle.FixedSingle,
                Font = SystemFont(8),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0),
                BackColor = Color.White
            };
            lbl1yr.Click += (s, e) => ButtonClick(lbl1yr.Text);
            lbl6mo = new Label()
            {
                Text = "6 Mo",
                Size = new Size(40, 20),
                BorderStyle = BorderStyle.FixedSingle,
                Font = SystemFont(8),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0),
                BackColor = Color.White
            };
            lbl6mo.Click += (s, e) => ButtonClick(lbl6mo.Text);
            lbl3mo = new Label()
            {
                Text = "3 Mo",
                Size = new Size(40, 20),
                BorderStyle = BorderStyle.FixedSingle,
                Font = SystemFont(8),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0),
                BackColor = Color.White
            };
            lbl3mo.Click += (s, e) => ButtonClick(lbl3mo.Text);

            lbl5yr.DockTo(dtpEnd, ControlEdge.Right);
            lbl2yr.DockTo(lbl5yr, ControlEdge.Right);
            lbl1yr.DockTo(lbl2yr, ControlEdge.Right);
            lbl6mo.DockTo(lbl1yr, ControlEdge.Right);
            lbl3mo.DockTo(lbl6mo, ControlEdge.Right);

            this.Controls.AddRange(new[] { lbl5yr, lbl2yr, lbl1yr, lbl6mo, lbl3mo });
        }
        [Initializer]
        private void InitializeBarSizeChooser()
        {
            boxBarSize = new ComboBox();
            boxBarSize.Items.AddRange(Enum.GetNames(typeof(PriceBarSize)));
            boxBarSize.SelectedIndex = 0;
            boxBarSize.Width = 75;
            boxBarSize.Font = SystemFont(8);
            boxBarSize.DockTo(lbl3mo, ControlEdge.Right, 0);

            boxBarSize.SelectedValueChanged += (s, e) =>
            {
                var value = (PriceBarSize)Enum.Parse(typeof(PriceBarSize), (boxBarSize.SelectedItem as string));
                if (SelectedBarSize != value)
                {
                    SelectedBarSize = value;
                    OnSelectedBarSizeChanged();
                }
            };

            this.Controls.Add(boxBarSize);
        }

        private void ButtonClick(string span)
        {
            switch (span)
            {
                case "5 Yr":
                    dtpStart.Value = dtpEnd.Value.AddYears(-5);
                    break;
                case "2 Yr":
                    dtpStart.Value = dtpEnd.Value.AddYears(-2);
                    break;
                case "1 Yr":
                    dtpStart.Value = dtpEnd.Value.AddYears(-1);
                    break;
                case "6 Mo":
                    dtpStart.Value = dtpEnd.Value.AddMonths(-6);
                    break;
                case "3 Mo":
                    dtpStart.Value = dtpEnd.Value.AddMonths(-3);
                    break;
            }

            OnSelectedValueChanged();
        }
        public void SetValues(DateTime start, DateTime end)
        {
            if (start >= end)
                return;

            dtpStart.Value = start;
            dtpEnd.Value = end;
        }
    }

    #endregion
    #region Series

    public abstract class FinanceSeries : Series
    {
        public DateTime MinXValue
        {
            get
            {
                if (Points.Count == 0)
                    return FinanceChartView.Default().MinDate;
                else
                {
                    return DateTime.FromOADate((from pt in Points select pt.XValue).Min());
                }
            }
        }
        public DateTime MaxXValue
        {
            get
            {
                if (Points.Count == 0)
                    return FinanceChartView.Default().MaxDate;
                else
                {
                    return DateTime.FromOADate((from pt in Points select pt.XValue).Max());
                }
            }
        }
        public decimal MinYValue()
        {
            if (Points.Count == 0)
                return 0;
            else
            {
                return (from pt in Points select pt.YValues.Min()).Min().ToDecimal();
            }
        }
        public decimal MinYValue(DateTime start, DateTime end)
        {
            if (Points.Count == 0)
                return 0;
            else
            {
                if (Points.Where(pt => DateTime.FromOADate(pt.XValue).IsBetween(start, end)).Count() == 0)
                    return 0;
                return (from pt in Points
                        where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
                        select pt.YValues.Min()).Min().ToDecimal();
            }
        }
        public decimal MaxYValue()
        {
            if (Points.Count == 0)
                return 1;
            else
            {
                return (from pt in Points select pt.YValues.Max()).Max().ToDecimal();
            }
        }
        public decimal MaxYValue(DateTime start, DateTime end)
        {
            if (Points.Count == 0)
                return 0;
            else
            {
                if (Points.Where(pt => DateTime.FromOADate(pt.XValue).IsBetween(start, end)).Count() == 0)
                    return 0;
                return (from pt in Points
                        where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
                        select pt.YValues.Max()).Max().ToDecimal();
            }
        }
        public object ChartPointObject(double XValue)
        {
            var ret = (from pt in Points where pt.XValue == XValue select pt).FirstOrDefault();
            if (ret != null)
                return ret.Tag;
            return ret;
        }

        protected abstract void SetStyles();
        protected abstract void BuildSeries();
        public abstract void RefreshSeries();
    }

    public class FinanceSecuritySeries : FinanceSeries
    {
        public Security Security { get; private set; }
        public PriceBarSize PriceBarSize { get; private set; }

        public FinanceSecuritySeries(Security security, PriceBarSize priceBarSize)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            PriceBarSize = priceBarSize;
            this.InitializeMe();
        }
        [Initializer]
        protected override void SetStyles()
        {
            ChartType = SeriesChartType.Candlestick;

            this["PriceUpColor"] = "Green";
            this["PriceDownColor"] = "Red";
        }
        [Initializer]
        protected override void BuildSeries()
        {
            Points.Clear();

            //
            // Add historical data
            //
            foreach (PriceBar bar in Security.GetPriceBars(PriceBarSize))
            {
                var pt = new DataPoint(this)
                {
                    XValue = bar.BarDateTime.ToOADate(),
                    YValues = bar.AsChartingValue(),
                    IsValueShownAsLabel = false,
                    Tag = bar,
                    Color = (bar.Change >= 0 ? Color.Green : Color.Red)
                };
                Points.Add(pt);
            }

            //
            // Add live intraday data
            //

            if (PriceBarSize != PriceBarSize.Daily)
                return;

            var intradayBar = Security.IntradayBar;
            if (intradayBar != null)
            {
                var pt = new DataPoint(this)
                {
                    XValue = intradayBar.BarDateTime.Date.ToOADate(),
                    YValues = intradayBar.AsChartingValue(),
                    IsValueShownAsLabel = false,
                    Tag = intradayBar,
                    Color = (intradayBar.Change >= 0 ? Color.Green : Color.Red)
                };
                Points.Add(pt);
            }
        }

        public override void RefreshSeries()
        {
            BuildSeries();
        }
    }
    public class FinanceVolumeSeries : FinanceSeries
    {
        public Security Security { get; private set; }
        public PriceBarSize PriceBarSize { get; private set; }

        public FinanceVolumeSeries(Security security, PriceBarSize priceBarSize)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            PriceBarSize = priceBarSize;
            this.InitializeMe();
        }

        [Initializer]
        protected override void SetStyles()
        {
            ChartType = SeriesChartType.Column;
            ChartArea = "secondary";
        }
        [Initializer]
        protected override void BuildSeries()
        {
            Points.Clear();

            foreach (PriceBar bar in Security.GetPriceBars(PriceBarSize))
            {
                var pt = new DataPoint(this)
                {
                    XValue = bar.BarDateTime.ToOADate(),
                    YValues = new[] { Convert.ToDouble(bar.Volume) },
                    IsValueShownAsLabel = false,
                    Tag = bar,
                    Color = Color.FromArgb(128, 255, 200, 0)
                };
                Points.Add(pt);
            }

            //
            // Add live intraday data
            //

            if (PriceBarSize != PriceBarSize.Daily)
                return;

            var intradayBar = Security.IntradayBar;
            if (intradayBar != null)
            {
                var pt = new DataPoint(this)
                {
                    XValue = intradayBar.BarDateTime.Date.ToOADate(),
                    YValues = new[] { Convert.ToDouble(intradayBar.Volume) },
                    IsValueShownAsLabel = false,
                    Tag = intradayBar,
                    Color = Color.FromArgb(128, 255, 200, 0)
                };
                Points.Add(pt);
            }
        }

        public override void RefreshSeries()
        {
            BuildSeries();
        }
    }
    public class FinanceSwingPointSeries : FinanceSeries
    {
        public Security Security { get; private set; }
        public PriceBarSize PriceBarSize { get; private set; }
        private int BarCount { get; set; }

        public FinanceSwingPointSeries(Security security, PriceBarSize priceBarSize, int barCount)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            PriceBarSize = priceBarSize;
            BarCount = barCount;
            this.InitializeMe();
        }

        [Initializer]
        protected override void SetStyles()
        {
            ChartType = SeriesChartType.Point;
            ChartArea = "primary";
            SmartLabelStyle.Enabled = false;
        }
        [Initializer]
        protected override void BuildSeries()
        {
            Points.Clear();

            Security.SetSwingPointsAndTrends(BarCount, PriceBarSize);

            foreach (PriceBar bar in Security.GetPriceBars(PriceBarSize))
            {
                if ((bar.GetSwingPointType(BarCount) & SwingPointType.SwingPointHigh) == SwingPointType.SwingPointHigh)
                {
                    var pt = new DataPoint(this)
                    {
                        XValue = bar.BarDateTime.ToOADate(),
                        YValues = new[] { Convert.ToDouble(bar.High) },
                        IsValueShownAsLabel = false,
                        Label = "SPH",
                        LabelForeColor = Color.White,
                        Tag = bar,
                        MarkerStyle = MarkerStyle.Circle,
                        MarkerColor = Color.Transparent,
                        Color = Color.Transparent,
                        MarkerBorderColor = Color.White,
                        MarkerBorderWidth = 1,
                        MarkerSize = 8
                    };
                    Points.Add(pt);
                }
                if ((bar.GetSwingPointType(BarCount) & SwingPointType.SwingPointLow) == SwingPointType.SwingPointLow)
                {
                    var pt = new DataPoint(this)
                    {
                        XValue = bar.BarDateTime.ToOADate(),
                        YValues = new[] { Convert.ToDouble(bar.Low) },
                        IsValueShownAsLabel = false,
                        Label = "SPL",
                        LabelForeColor = Color.White,
                        Tag = bar,
                        MarkerStyle = MarkerStyle.Circle,
                        MarkerColor = Color.Transparent,
                        Color = Color.Transparent,
                        MarkerBorderColor = Color.White,
                        MarkerBorderWidth = 1,
                        MarkerSize = 8
                    };
                    Points.Add(pt);
                }
                if (bar.GetSwingPointTestType(BarCount) == SwingPointTest.TestHigh)
                {
                    var pt = new DataPoint(this)
                    {
                        XValue = bar.BarDateTime.ToOADate(),
                        YValues = new[] { Convert.ToDouble(bar.High * 1.01m) },
                        IsValueShownAsLabel = false,
                        LabelAngle = -90,
                        Label = "TEST",
                        Font = new Font("Consolas", 7, FontStyle.Bold),
                        LabelForeColor = Color.LimeGreen,
                        Tag = bar,
                        MarkerStyle = MarkerStyle.None,
                        MarkerColor = Color.Transparent,
                        Color = Color.Transparent
                    };
                    Points.Add(pt);
                }
                if (bar.GetSwingPointTestType(BarCount) == SwingPointTest.TestLow)
                {
                    var pt = new DataPoint(this)
                    {
                        XValue = bar.BarDateTime.ToOADate(),
                        YValues = new[] { Convert.ToDouble(bar.Low * .99m) },
                        IsValueShownAsLabel = false,
                        LabelAngle = 90,
                        Label = "TEST",
                        Font = new Font("Consolas", 7, FontStyle.Bold),
                        LabelForeColor = Color.Pink,
                        Tag = bar,
                        MarkerStyle = MarkerStyle.None,
                        MarkerColor = Color.Transparent,
                        Color = Color.Transparent
                    };
                    Points.Add(pt);
                }
            }
        }

        public override void RefreshSeries()
        {
            throw new NotImplementedException();
        }
    }
    public class FinanceTrendSeries : FinanceSeries
    {
        public Security Security { get; private set; }
        public PriceBarSize PriceBarSize { get; set; }
        private int BarCount { get; set; }

        public FinanceTrendSeries(Security security, PriceBarSize priceBarSize, int barCount)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            PriceBarSize = priceBarSize;
            BarCount = barCount;
            this.InitializeMe();
        }

        [Initializer]
        protected override void SetStyles()
        {
            ChartType = SeriesChartType.RangeColumn;
            ChartArea = "primary";
        }
        [Initializer]
        protected override void BuildSeries()
        {

            Points.Clear();

            Security.SetSwingPointsAndTrends(BarCount, PriceBarSize);

            foreach (PriceBar bar in Security.GetPriceBars(PriceBarSize))
            {
                var pt = new DataPoint(this)
                {
                    XValue = bar.BarDateTime.ToOADate(),
                    //YValues = new[] { bar.Low.ToDouble() - 2, bar.High.ToDouble() + 2 },
                    //YValues = new[] { 0.0, bar.Low.ToDouble() *.90 },
                    IsValueShownAsLabel = false,
                    Color = Color.Transparent,
                    Tag = bar
                };
                var trend = bar.GetTrendType(BarCount);
                switch (trend)
                {
                    case TrendQualification.NotSet:
                        break;
                    case TrendQualification.AmbivalentSideways:
                        pt.BorderColor = Settings.Instance.ColorAmbivalentSidewaysTrend;
                        pt.BorderWidth = 1;
                        pt.BorderDashStyle = ChartDashStyle.Dash;
                        pt.YValues = new[] { bar.Low.ToDouble() * 0.90, bar.High.ToDouble() * 1.10 };
                        break;
                    case TrendQualification.SuspectSideways:
                        pt.Color = Settings.Instance.ColorSuspectSidewaysTrend;
                        pt.BorderWidth = 1;
                        pt.YValues = new[] { bar.Low.ToDouble() * 0.90, bar.High.ToDouble() * 1.10 };
                        break;
                    case TrendQualification.ConfirmedSideways:
                        pt.Color = Settings.Instance.ColorConfirmedSidewaysTrend;
                        pt.YValues = new[] { bar.Low.ToDouble() * 0.90, bar.High.ToDouble() * 1.10 };
                        break;
                    case TrendQualification.SuspectBullish:
                        pt.Color = Settings.Instance.ColorSuspectBullishTrend;
                        pt.BorderWidth = 1;
                        pt.YValues = new[] { 0.0, bar.Low.ToDouble() * .90 };
                        break;
                    case TrendQualification.ConfirmedBullish:
                        pt.Color = Settings.Instance.ColorConfirmedBullishTrend;
                        pt.YValues = new[] { 0.0, bar.Low.ToDouble() * .90 };
                        break;
                    case TrendQualification.SuspectBearish:
                        pt.Color = Settings.Instance.ColorSuspectBearishTrend;
                        pt.BorderWidth = 1;
                        pt.YValues = new[] { bar.High.ToDouble() * 1.10, 9999.0 };
                        break;
                    case TrendQualification.ConfirmedBearish:
                        pt.Color = Settings.Instance.ColorConfirmedBearishTrend;
                        pt.YValues = new[] { bar.High.ToDouble() * 1.10, 9999.0 };
                        break;
                    default:
                        break;
                }
                Points.Add(pt);
            }
        }

        public override void RefreshSeries()
        {
            throw new NotImplementedException();
        }
    }

    public class FinanceSignalSeries : FinanceSeries
    {
        public Security Security { get; }
        private List<Signal> Signals { get; }

        public FinanceSignalSeries(List<Signal> signals)
        {
            Signals = signals ?? throw new ArgumentNullException(nameof(signals));
            Security = signals.FirstOrDefault()?.Security;
            this.InitializeMe();
        }

        [Initializer]
        protected override void SetStyles()
        {
            ChartType = SeriesChartType.Point;
            ChartArea = "primary";
        }
        [Initializer]
        protected override void BuildSeries()
        {
            Points.Clear();

            foreach (Signal signal in Signals)
            {
                if (signal.SignalAction.ToInt() == TradeActionBuySell.None.ToInt())
                    continue;

                var pt = new DataPoint(this)
                {
                    XValue = signal.SignalDate.ToOADate(),
                    IsValueShownAsLabel = false,
                    Tag = signal,
                    MarkerSize = 7,
                    MarkerBorderColor = Color.DarkSlateGray,
                };

                switch (signal.SignalAction)
                {
                    case SignalAction.CloseIfOpen:
                        pt.Color = Color.Orange;
                        pt.MarkerStyle = MarkerStyle.Cross;
                        break;
                    case SignalAction.Sell:
                        pt.Color = Color.PaleVioletRed;
                        pt.MarkerStyle = MarkerStyle.Circle;
                        break;
                    case SignalAction.None:
                        break;
                    case SignalAction.Buy:
                        pt.Color = Color.LightGreen;
                        pt.MarkerStyle = MarkerStyle.Circle;
                        break;
                    default:
                        break;
                }

                //
                // Set Y value based on buy or sell
                //
                var bar = Security.GetPriceBar(signal.SignalDate, signal.SignalBarSize);
                switch (signal.SignalAction)
                {
                    case SignalAction.Buy:
                        pt.YValues = new[] { bar.Close.ToDouble() * 1.02 };
                        break;
                    case SignalAction.Sell:
                        pt.YValues = new[] { bar.Close.ToDouble() * 0.98 };
                        break;
                }

                Points.Add(pt);
            }
        }

        public override void RefreshSeries()
        {
            throw new NotImplementedException();
        }
    }
    public class FinancePositionSeries : FinanceSeries
    {
        public Security Security { get; }
        private Simulation Simulation { get; }

        public FinancePositionSeries(Security security, Simulation simulation)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.InitializeMe();
        }
        public FinancePositionSeries(Simulation simulation)
        {
            Security = null;
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.InitializeMe();
        }

        [Initializer]
        protected override void SetStyles()
        {
            ChartType = SeriesChartType.Column;
            ChartArea = "secondary";
        }
        [Initializer]
        protected override void BuildSeries()
        {
            Points.Clear();
            List<Position> positions;
            if (Security != null)
                positions = Simulation.PortfolioManager.Portfolio.GetPositions(Security);
            else
                positions = Simulation.PortfolioManager.Portfolio.GetPositions(Simulation.SimulationTimeSpan.end);

            DateTime currentDate = Simulation.SimulationTimeSpan.start;
            DateTime endDate = Simulation.SimulationTimeSpan.end;

            while (currentDate <= endDate)
            {

                if (!Calendar.IsTradingDay(currentDate))
                {
                    if (Points.Count > 0)
                    {
                        var clonePoint = Points.Last().Clone();
                        clonePoint.XValue = currentDate.ToOADate();
                        Points.Add(clonePoint);
                    }
                }
                else
                {
                    var positionSum = (from position in positions
                                       where position.IsOpen(currentDate)
                                       select position.GrossPositionValue(currentDate, TimeOfDay.MarketEndOfDay)).Sum();

                    var newPoint = Points.AddAndReturn(new DataPoint(this)
                    {
                        XValue = currentDate.ToOADate(),
                        YValues = new[] { positionSum.ToDouble() },
                        IsValueShownAsLabel = false
                    });
                    newPoint.Color = newPoint.YValues[0] > 0 ? Color.FromArgb(64, 0, 255, 0) : Color.FromArgb(64, 255, 0, 0);
                }

                currentDate = currentDate.AddDays(1);
            }
        }

        public override void RefreshSeries()
        {
            throw new NotImplementedException();
        }
    }
    public class FinanceTradeSeries : FinanceSeries
    {
        public Security Security { get; }
        private Simulation Simulation { get; }

        public FinanceTradeSeries(Security security, Simulation simulation)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.InitializeMe();
        }

        [Initializer]
        protected override void SetStyles()
        {
            ChartType = SeriesChartType.Point;
            ChartArea = "primary";
        }
        [Initializer]
        protected override void BuildSeries()
        {
            Points.Clear();

            BuildTradeSeries();
            BuildStopSeries();
        }
        private void BuildTradeSeries()
        {
            var trades = (from pos in Simulation.PortfolioManager.Portfolio.GetPositions(Security) select pos.ExecutedTrades)
                        .ToList().SelectMany(x => x).ToList();

            foreach (Trade trade in trades)
            {
                var pt = new DataPoint(this)
                {
                    XValue = trade.TradeDate.ToOADate(),
                    IsValueShownAsLabel = false,
                    Tag = trade,
                    Color = Color.Transparent,
                    MarkerStyle = MarkerStyle.Diamond,
                    MarkerSize = 10,
                    MarkerBorderWidth = 2,
                    MarkerBorderColor = Color.White
                };

                //
                // Set Y value based on buy or sell
                //
                var bar = Security.GetPriceBar(trade.TradeDate, PriceBarSize.Daily);
                pt.YValues = new[] { trade.ExecutedPrice.ToDouble() };

                Points.Add(pt);
            }
        }
        private void BuildStopSeries()
        {
            var trades = Simulation.PortfolioManager.TradeManager.GetAllStoplosses(Security);

            foreach (Trade trade in trades)
            {
                var pt = new DataPoint(this)
                {
                    XValue = trade.TradeDate.ToOADate(),
                    YValues = new[] { trade.ExpectedExecutionPrice.ToDouble() },
                    IsValueShownAsLabel = false,
                    Tag = trade,
                    Color = Color.Yellow,
                    MarkerStyle = MarkerStyle.Square,
                    MarkerSize = 5,
                    MarkerBorderColor = Color.DarkSlateGray
                };

                Points.Add(pt);
            }
        }

        public override void RefreshSeries()
        {
            throw new NotImplementedException();
        }
    }

    public class FinanceAccountingSeries : FinanceSeries
    {
        public Simulation Simulation { get; }
        public Portfolio Portfolio => Simulation.PortfolioManager.Portfolio;

        public FinanceAccountingSeries(Simulation simulation)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.InitializeMe();
        }

        [Initializer]
        protected override void SetStyles()
        {
            ChartType = SeriesChartType.Line;
            ChartArea = "primary";
        }
        [Initializer]
        protected override void BuildSeries()
        {
            Points.Clear();

            DateTime currentDate = Simulation.SimulationTimeSpan.start;
            while (currentDate <= Simulation.SimulationTimeSpan.end)
            {
                var pt = new DataPoint(this)
                {
                    XValue = currentDate.ToOADate(),
                    YValues = new double[] { Portfolio.GetByAccountingSeriesValue(AccountingSeriesValue.EquityWithLoanValue, currentDate).ToDouble() },
                    IsValueShownAsLabel = false
                };
                Points.Add(pt);
                currentDate = Calendar.NextTradingDay(currentDate);
            }
        }

        public override void RefreshSeries()
        {
            throw new NotImplementedException();
        }
    }

    #endregion
    #region Tiny Charts

    public abstract class TinyChartPanel : Panel
    {
        public abstract TinyChartBase PrimaryChart { get; set; }
        protected abstract void LoadPrimaryChart(TinyChartBase chart);

        [Initializer]
        private void InitializeLayout()
        {
            Dock = DockStyle.Fill;
        }
    }
    public abstract class TinyResultsChartPanel : Panel
    {
        public abstract Chart PrimaryChart { get; set; }
        protected abstract void LoadPrimaryChart(Chart chart);

        [Initializer]
        private void InitializeLayout()
        {
            Dock = DockStyle.Fill;
        }
    }

    public class TinySecurityChartPanel : TinyChartPanel
    {
        public override TinyChartBase PrimaryChart { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected override void LoadPrimaryChart(TinyChartBase chart)
        {
            throw new NotImplementedException();
        }
    }
    public class TinySimResultsChartPanel : TinyChartPanel
    {
        public override TinyChartBase PrimaryChart { get; set; }
        public Simulation Simulation;

        public TinySimResultsChartPanel()
        {
            this.InitializeMe();
            LoadPrimaryChart(new TinyFinanceSimResultsChart());
        }
        public void LoadSimulation(Simulation simulation)
        {
            if (simulation == this.Simulation)
                return;

            this.Simulation = simulation;
            (PrimaryChart as TinyFinanceSimResultsChart).LoadSimulation(simulation);
        }

        protected override void LoadPrimaryChart(TinyChartBase chart)
        {
            PrimaryChart = chart;
            PrimaryChart.Dock = DockStyle.Fill;
            PrimaryChart.Visible = true;
            this.Controls.Add(PrimaryChart);
        }
    }
    public class TinySectorResultsChartPanel : TinyResultsChartPanel
    {
        public override Chart PrimaryChart { get; set; }
        public Simulation Simulation;

        public TinySectorResultsChartPanel()
        {
            this.InitializeMe();
            LoadPrimaryChart(new TinySectorResultsChart());
        }
        public void LoadSimulation(Simulation simulation)
        {
            if (simulation == this.Simulation)
                return;

            this.Simulation = simulation;
            (PrimaryChart as TinySectorResultsChart).LoadSimulation(simulation);
        }

        protected override void LoadPrimaryChart(Chart chart)
        {
            PrimaryChart = chart;
            PrimaryChart.Dock = DockStyle.Fill;
            PrimaryChart.Visible = true;
            this.Controls.Add(PrimaryChart);
        }
    }

    public abstract class TinyChartBase : Chart
    {
        public FinanceChartView CurrentView { get; protected set; }

        protected FinanceSeries PrimarySeries { get; set; }
        protected TinyFinanceChartArea PrimaryChartArea { get; set; }

        protected bool NoData => (PrimarySeries == null);

        public (DateTime min, DateTime max) PrimarySeriesDateRange
        {
            get
            {
                if (PrimarySeries == null)
                    return (DateTime.MinValue, DateTime.MaxValue);
                return (PrimarySeries.MinXValue, PrimarySeries.MaxXValue);
            }
        }

        [Initializer]
        protected abstract void InitializeChartAreas();

        public void SetView(DateTime minView, DateTime maxView)
        {
            if (CurrentView == null)
                CurrentView = new FinanceChartView(minView, maxView);
            else
            {
                CurrentView.MinDate = minView;
                CurrentView.MaxDate = maxView;
            }

            RedrawAxes();
            Invalidate();
        }

        protected void ReloadChart()
        {
            if (NoData)
            {
                Invalidate();
                return;
            }

            if (PrimarySeries is FinanceAccountingSeries acctSer)
                CurrentView = new FinanceChartView(acctSer.MinXValue, acctSer.MaxXValue);
            else
                CurrentView = FinanceChartView.Default();

            RedrawAxes();
            Invalidate();
        }
        protected void RedrawAxes()
        {
            SetXAxis();
            SetPrimaryChartAreaY1Axis();
        }
        protected void SetXAxis()
        {
            PrimaryChartArea.AxisX.Minimum = CurrentView.MinDate.ToOADate();
            PrimaryChartArea.AxisX.Maximum = CurrentView.MaxDate.ToOADate();
        }
        protected virtual void SetPrimaryChartAreaY1Axis()
        {
            var minAct = Math.Max(PrimarySeries.MinYValue(CurrentView.MinDate, CurrentView.MaxDate).ToDouble(), 0);
            var maxAct = PrimarySeries.MaxYValue(CurrentView.MinDate, CurrentView.MaxDate).ToDouble();

            var minAdj = RoundDownWholeNumberLargestPlace(minAct.ToDecimal()).ToDouble();
            var maxAdj = RoundUpWholeNumberLargestPlace(maxAct.ToDecimal()).ToDouble();

            if ((maxAdj / maxAct) > 1.25)
                maxAdj = RoundUpWholeNumber2ndLargestPlace(maxAct.ToDecimal()).ToDouble();

            if ((minAdj / minAct) < .75)
                minAdj = RoundDownWholeNumber2ndLargestPlace(minAct.ToDecimal()).ToDouble();

            PrimaryChartArea.AxisY.Maximum = maxAdj;
            PrimaryChartArea.AxisY.Minimum = minAdj;
            PrimaryChartArea.AxisY.Interval = (maxAdj - minAdj) / 10;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PaintNoData(e);
        }
        protected void PaintNoData(PaintEventArgs e)
        {
            if (!NoData)
                return;

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, 35, 35, 35)))
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.DrawString($"No Data", Helpers.SystemFont(16), brush, this.Width / 3, this.Height / 5 * 2, new StringFormat());
                return;
            }
        }

    }
    public class TinyFinanceSimResultsChart : TinyChartBase
    {
        public Simulation Simulation { get; private set; }

        public TinyFinanceSimResultsChart()
        {
            this.InitializeMe();
        }

        protected override void InitializeChartAreas()
        {
            ChartAreas.Clear();

            //
            // Primary Chart
            //
            PrimaryChartArea = new TinyFinanceChartArea("primary");

            PrimaryChartArea.AxisX.LabelStyle.Enabled = false;
            PrimaryChartArea.AxisX.IsMarginVisible = false;
            PrimaryChartArea.AxisX.Title = "";

            PrimaryChartArea.AxisX.MajorTickMark.Enabled = false;

            PrimaryChartArea.AxisY.LabelStyle.Enabled = false;
            PrimaryChartArea.AxisY.IsMarginVisible = false;
            PrimaryChartArea.AxisY.MajorTickMark.Enabled = false;

            ChartAreas.Add(PrimaryChartArea);
        }
        public void LoadSimulation(Simulation simulation)
        {
            this.Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.Series.Clear();

            if (Simulation == null)
            {
                PrimarySeries = null;
                ReloadChart();
                return;
            }

            PrimarySeries = new FinanceAccountingSeries(Simulation);

            this.Series.Add(PrimarySeries);
            ReloadChart();
        }
    }

    public class TinyFinanceChartArea : ChartArea
    {
        public TinyFinanceChartArea(string name) : base(name)
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyles()
        {
            this.BackColor = Color.Black;

            // Axis X Style

            this.AxisX.LabelStyle.Enabled = false;
            this.AxisX.IsMarginVisible = false;
            this.AxisX.Title = "";
            this.AxisX.MajorTickMark.Enabled = false;
            this.AxisX.IntervalType = DateTimeIntervalType.Weeks;
            this.AxisX.IsStartedFromZero = false;
            this.AxisX.Interval = 1;
            this.AxisX.IntervalOffset = 1;

            // Axis Y Style
            this.AxisY.LabelStyle.Enabled = false;
            this.AxisY.IsMarginVisible = false;
            this.AxisY.MajorTickMark.Enabled = false;
            this.AxisY.MajorGrid.Enabled = false;
            this.AxisY.IntervalType = DateTimeIntervalType.Number;
            this.AxisY.IsStartedFromZero = false;

        }

        #region Striplines

        [Initializer]
        private void DrawStriplines()
        {
            this.AxisX.StripLines.Clear();

            DrawMonthStartStriplines();
            DrawYearStartStriplines();
        }
        private void DrawWeekendStriplines()
        {
            StripLine slWeekend = new StripLine()
            {
                IntervalOffset = -1.5,
                IntervalOffsetType = DateTimeIntervalType.Days,
                Interval = 1,
                IntervalType = DateTimeIntervalType.Weeks,
                StripWidth = 2,
                StripWidthType = DateTimeIntervalType.Days,
                BackColor = Color.FromArgb(5, 5, 5),
                BorderColor = Color.FromArgb(10, 10, 10)
            };
            this.AxisX.StripLines.Add(slWeekend);
        }
        private void DrawMonthStartStriplines()
        {
            StripLine slMonth = new StripLine()
            {
                IntervalOffset = -.5,
                IntervalOffsetType = DateTimeIntervalType.Days,
                Interval = 1,
                IntervalType = DateTimeIntervalType.Months,
                StripWidth = .10,
                StripWidthType = DateTimeIntervalType.Days,
                BackColor = Color.FromArgb(32, 32, 0),
                BorderColor = Color.FromArgb(32, 32, 0)
            };
            this.AxisX.StripLines.Add(slMonth);
        }
        private void DrawYearStartStriplines()
        {
            StripLine slYear = new StripLine()
            {
                IntervalOffset = -.5,
                IntervalOffsetType = DateTimeIntervalType.Days,
                Interval = 1,
                IntervalType = DateTimeIntervalType.Years,
                StripWidth = .10,
                StripWidthType = DateTimeIntervalType.Days,
                BackColor = Color.FromArgb(128, 0, 0),
                BorderColor = Color.FromArgb(128, 0, 0)
            };
            this.AxisX.StripLines.Add(slYear);
        }
        private void DrawHolidayStriplines()
        {
            foreach (DateTime holiday in Calendar.AllHolidays(DateTime.Today.AddYears(-10), DateTime.Today))
            {
                StripLine slHoliday = new StripLine()
                {
                    IntervalOffset = holiday.ToOADate() - 0.5,
                    IntervalType = DateTimeIntervalType.Auto,
                    StripWidth = 1,
                    StripWidthType = DateTimeIntervalType.Days,
                    BackColor = Color.FromArgb(25, 0, 25),
                    BorderColor = Color.FromArgb(10, 10, 10),
                };
                this.AxisX.StripLines.Add(slHoliday);
            }

        }

        #endregion
    }

    public class TinySectorResultsChart : Chart
    {

        public Simulation Simulation { get; protected set; }
        protected bool NoData => Simulation == null;

        public TinySectorResultsChart()
        {
            this.InitializeMe();
        }

        public void LoadSimulation(Simulation simulation)
        {
            this.Simulation = simulation;

            this.Series.Clear();
            this.Series.Add(new SectorResultSeries(this.Simulation));

            SetYAxis();
            SetXAxis();
            Invalidate();
        }

        private void SetYAxis()
        {
            int min = (this.Series[0] as SectorResultSeries).MinYValue - 2;
            int max = (this.Series[0] as SectorResultSeries).MaxYValue + 2;

            this.ChartAreas["primary"].AxisY.Maximum = max;
            this.ChartAreas["primary"].AxisY.Minimum = min;

            this.ChartAreas["primary"].AxisY.MajorGrid.Interval = Math.Max(Math.Abs(min), max);
            this.ChartAreas["primary"].AxisY.MajorGrid.IntervalOffset = max > Math.Abs(min) ? -(max + min) : 0;

        }
        private void SetXAxis()
        {
            this.ChartAreas["primary"].AxisX.Maximum = (this.Series[0].Points.Count() / 2);
            this.ChartAreas["primary"].AxisX.Minimum = 0;
        }

        [Initializer]
        protected void InitializeChart()
        {
            this.ChartAreas.Clear();
            this.ChartAreas.Add(new TinyBarResultsChartArea("primary"));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PaintNoData(e);
        }
        protected void PaintNoData(PaintEventArgs e)
        {
            if (!NoData)
                return;

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, 35, 35, 35)))
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.DrawString($"No Data", Helpers.SystemFont(12), brush, this.Width / 3, this.Height / 5 * 2, new StringFormat());
                return;
            }
        }
    }

    public class TinyBarResultsChartArea : ChartArea
    {
        public TinyBarResultsChartArea(string name) : base(name)
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyles()
        {
            this.BackColor = Color.Black;

            // Axis X Style

            this.AxisX.IsMarginVisible = false;
            this.AxisX.Title = "";

            this.AxisX.MajorTickMark.Enabled = false;
            this.AxisX.MajorGrid.Enabled = true;
            this.AxisX.MajorGrid.LineColor = Color.DimGray;
            this.AxisX.MajorGrid.Interval = 1;
            this.AxisX.MajorGrid.IntervalOffset = 0;

            this.AxisX.IntervalType = DateTimeIntervalType.Number;
            this.AxisX.Interval = .5;
            this.AxisX.IntervalOffset = 0;
            this.AxisX.Minimum = 0;

            // Axis Y Style
            this.AxisY.LabelStyle.Enabled = false;
            this.AxisY.IsMarginVisible = false;
            this.AxisY.MajorTickMark.Enabled = false;

            this.AxisY.MajorGrid.Enabled = true;
            this.AxisY.MajorGrid.LineColor = Color.White;
            this.AxisY.MajorGrid.Interval = 0;
            this.AxisY.MajorGrid.IntervalType = DateTimeIntervalType.Number;

            this.AxisY.IntervalType = DateTimeIntervalType.Number;
            this.AxisY.IsStartedFromZero = true;
        }
    }

    public class SectorResultSeries : Series
    {
        public int MaxYValue
        {
            get
            {
                return this.Points.Count > 0 ? (int)(from pt in this.Points select pt.YValues.Max()).Max() : 0;
            }
        }
        public int MinYValue
        {
            get
            {
                return this.Points.Count > 0 ? (int)(from pt in this.Points select pt.YValues.Min()).Min() : 0;
            }
        }

        public Simulation Simulation { get; }
        private List<SectorPerformanceResults> Results => Simulation.Results.SectorResults();

        public SectorResultSeries(Simulation simulation)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.InitializeMe();
        }

        [Initializer]
        protected void SetStyles()
        {
            ChartArea = "primary";
            this.ChartType = SeriesChartType.Column;
        }
        [Initializer]
        protected void BuildSeries()
        {
            this.Points.Clear();
            int index = 1;

            foreach (var result in Results)
            {
                var pt1 = new DataPoint()
                {
                    XValue = index - .5,
                    YValues = new[] { result.WinningPositions.ToDouble() },
                    Color = Color.Green,
                    AxisLabel = result.Sector.ToShorthand(),
                    Label = (result.WinningPositions == 0 ? "" : result.WinningPositions.ToString()),
                    LabelForeColor = Color.White
                };
                Points.Add(pt1);

                var pt2 = new DataPoint()
                {
                    XValue = index - .5,
                    YValues = new[] { -result.LosingPositions.ToDouble() },
                    Color = Color.Red,
                    AxisLabel = result.Sector.ToShorthand(),
                    Label = (result.LosingPositions == 0 ? "" : result.LosingPositions.ToString()),
                    LabelForeColor = Color.White
                };
                Points.Add(pt2);

                index += 1;
            }
        }
    }


    #endregion
    #region Trend Monitor Charts

    public class TinySectorTrendChart : Chart
    {
        public TrendIndex TrendIndex { get; protected set; }
        public DateTime IndexDay { get; protected set; }

        protected bool NoData => TrendIndex == null;

        public TinySectorTrendChart()
        {
            this.InitializeMe();
        }

        public void LoadTrendIndex(TrendIndex trendIndex, DateTime indexDay)
        {
            this.TrendIndex = trendIndex;
            this.IndexDay = indexDay;

            this.Series.Clear();
            this.Series.Add(new SectorTrendSeries(TrendIndex, IndexDay));

            Invalidate();
        }

        public void LoadIndexDay(DateTime indexDay)
        {
            if (TrendIndex == null)
                return;

            this.IndexDay = indexDay;

            this.Series.Clear();
            this.Series.Add(new SectorTrendSeries(TrendIndex, IndexDay));

            Invalidate();
        }

        private void SetYAxis()
        {
            this.ChartAreas["primary"].AxisY.Maximum = 1.5;
            this.ChartAreas["primary"].AxisY.Minimum = 0;
        }
        private void SetXAxis()
        {
            this.ChartAreas["primary"].AxisX.Maximum = 4;
            this.ChartAreas["primary"].AxisX.Minimum = 0;
        }

        [Initializer]
        protected void InitializeChart()
        {
            this.ChartAreas.Clear();
            this.ChartAreas.Add(new TinySectorTrendChartArea("primary"));

            SetYAxis();
            SetXAxis();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PaintNoData(e);
        }
        protected void PaintNoData(PaintEventArgs e)
        {
            if (!NoData)
                return;

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, 35, 35, 35)))
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.DrawString($"No Data", Helpers.SystemFont(12), brush, this.Width / 3, this.Height / 5 * 2, new StringFormat());
                return;
            }
        }
    }

    public class TinySectorTrendChartArea : ChartArea
    {
        public TinySectorTrendChartArea(string name) : base(name)
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyles()
        {
            this.BackColor = Color.Black;

            // Axis X Style

            this.AxisX.IsMarginVisible = false;
            this.AxisX.Title = "";

            this.AxisX.MajorTickMark.Enabled = false;
            this.AxisX.MajorGrid.Enabled = false;
            this.AxisX.LabelStyle.Enabled = false;

            this.AxisX.IntervalType = DateTimeIntervalType.Number;
            this.AxisX.Interval = 1;
            this.AxisX.IntervalOffset = 0;

            // Axis Y Style
            this.AxisY.LabelStyle.Enabled = false;
            this.AxisY.IsMarginVisible = false;
            this.AxisY.MajorTickMark.Enabled = false;

            this.AxisY.MajorGrid.Enabled = false;

            this.AxisY.IntervalType = DateTimeIntervalType.Number;
            this.AxisY.IsStartedFromZero = true;

            this.AxisY.StripLines.Add(new StripLine()
            {
                Interval = 2,
                StripWidth = 1,
                BackColor = Color.FromArgb(32, 128, 128, 128)
            });
            this.AxisY.StripLines.Add(new StripLine()
            {
                Interval = 1,
                StripWidth = 0.01,
                BackColor = Color.DarkSlateGray
            });
        }
    }

    public class SectorTrendSeries : Series
    {

        public TrendIndex TrendIndex { get; }
        public DateTime IndexDay { get; }

        public SectorTrendSeries(TrendIndex trendIndex, DateTime indexDay)
        {
            TrendIndex = trendIndex ?? throw new ArgumentNullException(nameof(trendIndex));
            IndexDay = indexDay;
            this.InitializeMe();
        }

        [Initializer]
        protected void SetStyles()
        {
            ChartArea = "primary";
            this.ChartType = SeriesChartType.Column;
        }
        [Initializer]
        protected void BuildSeries()
        {
            this.Points.Clear();

            var trendBreakdown = TrendIndex.GetIndexDay(IndexDay, false).GetTrendSummary();

            var bullishPt = new DataPoint()
            {
                XValue = 1,
                YValues = new[] { trendBreakdown.bullish.ToDouble() },
                Color = Color.FromArgb(196, 0, 255, 0),
                Label = (trendBreakdown.bullish).ToString("0%"),
                LabelForeColor = Color.White
            };
            Points.Add(bullishPt);

            var sidewaysPt = new DataPoint()
            {
                XValue = 2,
                YValues = new[] { trendBreakdown.sideways.ToDouble() },
                Color = Color.FromArgb(196, 255, 255, 0),
                Label = (trendBreakdown.sideways).ToString("0%"),
                LabelForeColor = Color.White
            };
            Points.Add(sidewaysPt);

            var bearishPt = new DataPoint()
            {
                XValue = 3,
                YValues = new[] { trendBreakdown.bearish.ToDouble() },
                Color = Color.FromArgb(196, 255, 0, 0),
                Label = (trendBreakdown.bearish).ToString("0%"),
                LabelForeColor = Color.White
            };
            Points.Add(bearishPt);
            Console.WriteLine();
        }
    }

    #endregion
}




