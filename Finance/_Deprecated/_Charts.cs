using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization;
using System.Windows.Forms.DataVisualization.Charting;

namespace Finance
{
    #region Finance Chart (Base Class)

    /*
     *  Custom Chart object which implements features which should be common to all chart 
     *  controls used in the library.
     */

    //public abstract class FinanceChart : Chart
    //{

    //    #region Events

    //    public event EventHandler ViewChanged;
    //    private void OnViewChanged()
    //    {
    //        ViewChanged?.Invoke(this, null);
    //    }

    //    #endregion

    //    protected ChartArea chartArea;

    //    //protected static Size _chrtSize = new Size(1500, 1000);
    //    protected static TimeSpan _defaultView = new TimeSpan(365, 0, 0, 0);
    //    protected static TimeSpan _zoomStep = new TimeSpan(2, 0, 0, 0);

    //    protected Point? cursorLocation = null;

    //    private Tuple<DateTime, DateTime> _CurrentView;
    //    public Tuple<DateTime, DateTime> CurrentView
    //    {
    //        get => _CurrentView;
    //        set
    //        {
    //            _CurrentView = value;
    //            OnViewChanged();
    //        }
    //    }

    //    protected Tuple<DateTime, double> currentCursorPoint = null;

    //    protected bool Ready { get; set; } = false;
    //    protected bool NoData { get; set; } = true;

    //    [Initializer]
    //    private void SetCursorLocation()
    //    {
    //        MouseMove += (s, e) =>
    //        {
    //            cursorLocation = e.Location;
    //            Invalidate();
    //        };
    //    }
    //    [Initializer]
    //    private void SetDefaultStyles()
    //    {
    //        //
    //        // Chart
    //        //
    //        BackColor = SystemColors.Control;
    //        DoubleBuffered = true;

    //        //
    //        // Chart Area
    //        //
    //        chartArea.BackColor = Color.Black;
    //        chartArea.InnerPlotPosition.Auto = false;
    //        chartArea.InnerPlotPosition.Width = 95;
    //        chartArea.InnerPlotPosition.Height = 90;
    //        chartArea.InnerPlotPosition.X = 5;
    //        chartArea.InnerPlotPosition.Y = 0;

    //        // Axis X Style
    //        chartArea.AxisX.IntervalType = DateTimeIntervalType.Weeks;
    //        chartArea.AxisX.IsStartedFromZero = false;
    //        chartArea.AxisX.Interval = 1;
    //        chartArea.AxisX.IntervalOffset = 1;
    //        chartArea.AxisX.Title = "Date";
    //        chartArea.AxisX.LabelStyle = new LabelStyle() { Format = "MM/dd/yy" };

    //        chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(25, 25, 25);
    //        chartArea.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
    //        chartArea.AxisX.MajorGrid.LineWidth = 1;
    //        chartArea.AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Days;
    //        chartArea.AxisX.MajorGrid.Interval = 1;
    //        chartArea.AxisX.MajorGrid.IntervalOffset = -0.5;

    //        // Axis Y Style
    //        chartArea.AxisY.IntervalType = DateTimeIntervalType.Number;
    //        chartArea.AxisY.IsStartedFromZero = false;
    //        chartArea.AxisY.LabelStyle.Format = "$0.00";
    //        chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(25, 25, 25);
    //        chartArea.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
    //        chartArea.AxisY.MajorGrid.LineWidth = 1;

    //        // Axis Y2 Style
    //        chartArea.AxisY2.Enabled = AxisEnabled.False;
    //        chartArea.AxisY2.IntervalType = DateTimeIntervalType.Number;
    //        chartArea.AxisY2.IsStartedFromZero = false;
    //        chartArea.AxisY2.LabelStyle = new LabelStyle() { Format = "###", Enabled = true, ForeColor = Color.White, Interval = 1 };
    //        chartArea.AxisY2.Title = "Position";
    //    }
    //    [Initializer]
    //    private void SetMouseScroll()
    //    {
    //        MouseWheel += ZoomTimeframeOnWheel;
    //    }
    //    [Initializer]
    //    private void SetMouseDrag()
    //    {
    //        MouseMove += MoveTimeframeOnDrag;
    //    }

    //    protected abstract void Redraw();
    //    public abstract void SetView(DateTime min, DateTime max);

    //    private void ZoomTimeframeOnWheel(object sender, MouseEventArgs e)
    //    {
    //        SetZoomStep();

    //        if (e.Delta < 0)
    //            SetView(CurrentView.Item1.Add(-_zoomStep), CurrentView.Item2);
    //        else
    //            SetView(CurrentView.Item1.Add(+_zoomStep), CurrentView.Item2);

    //        AdjustZoomedView();
    //        Redraw();
    //    }

    //    private int lastPositionX { get; set; }
    //    private void MoveTimeframeOnDrag(object sender, MouseEventArgs e)
    //    {
    //        if (e.Button == MouseButtons.Left)
    //        {
    //            // Drag the chart
    //            int deltaMove = (lastPositionX - e.X);
    //            SetDragView(deltaMove);
    //        }

    //        lastPositionX = e.X;
    //    }
    //    private void SetDragView(int deltaX)
    //    {
    //        double scaleFactor = 1.04;
    //        var percentMove = scaleFactor * (deltaX / ((chartArea.InnerPlotPosition.Width / 100d) * Width));
    //        var daysDrag = (CurrentView.Span().TotalDays * percentMove);

    //        SetView(CurrentView.Item1.AddDays(daysDrag), CurrentView.Item2.AddDays(daysDrag));
    //    }

    //    private void SetZoomStep()
    //    {
    //        if (CurrentView.Span().TotalDays < 90)
    //            _zoomStep = new TimeSpan(5, 0, 0, 0);
    //        else if (CurrentView.Span().TotalDays < 180)
    //            _zoomStep = new TimeSpan(7, 0, 0, 0);
    //        else
    //            _zoomStep = new TimeSpan(30, 0, 0, 0);
    //    }
    //    private void AdjustZoomedView()
    //    {
    //        //
    //        // Change some global styles based on zoom level
    //        //

    //        // Remove X gridlines on views greater than 360 days
    //        chartArea.AxisX.MajorGrid.Enabled = !(CurrentView.Span().TotalDays > 365);
    //    }

    //    protected void DrawWeekendStriplines()
    //    {
    //        StripLine slWeekend = new StripLine()
    //        {
    //            IntervalOffset = -1.5,
    //            IntervalOffsetType = DateTimeIntervalType.Days,
    //            Interval = 1,
    //            IntervalType = DateTimeIntervalType.Weeks,
    //            StripWidth = 2,
    //            StripWidthType = DateTimeIntervalType.Days,
    //            BackColor = Color.FromArgb(5, 5, 5),
    //            BorderColor = Color.FromArgb(10, 10, 10)
    //        };
    //        chartArea.AxisX.StripLines.Add(slWeekend);
    //    }
    //    protected void DrawMonthStartStriplines()
    //    {
    //        StripLine slMonth = new StripLine()
    //        {
    //            IntervalOffset = -.5,
    //            IntervalOffsetType = DateTimeIntervalType.Days,
    //            Interval = 1,
    //            IntervalType = DateTimeIntervalType.Months,
    //            StripWidth = .10,
    //            StripWidthType = DateTimeIntervalType.Days,
    //            BackColor = Color.FromArgb(32, 32, 0),
    //            BorderColor = Color.FromArgb(32, 32, 0)
    //        };
    //        chartArea.AxisX.StripLines.Add(slMonth);
    //    }
    //    protected void DrawYearStartStriplines()
    //    {
    //        StripLine slYear = new StripLine()
    //        {
    //            IntervalOffset = -.5,
    //            IntervalOffsetType = DateTimeIntervalType.Days,
    //            Interval = 1,
    //            IntervalType = DateTimeIntervalType.Years,
    //            StripWidth = .10,
    //            StripWidthType = DateTimeIntervalType.Days,
    //            BackColor = Color.FromArgb(128, 0, 0),
    //            BorderColor = Color.FromArgb(128, 0, 0)
    //        };
    //        chartArea.AxisX.StripLines.Add(slYear);
    //    }
    //    protected void DrawHolidayStriplines()
    //    {
    //        foreach (DateTime holiday in Calendar.AllHolidays(DateTime.Today.AddYears(-10), DateTime.Today))
    //        {
    //            StripLine slHoliday = new StripLine()
    //            {
    //                IntervalOffset = holiday.ToOADate() - 0.5,
    //                IntervalType = DateTimeIntervalType.Auto,
    //                StripWidth = 1,
    //                StripWidthType = DateTimeIntervalType.Days,
    //                BackColor = Color.FromArgb(25, 0, 25),
    //                BorderColor = Color.FromArgb(10, 10, 10),
    //            };
    //            chartArea.AxisX.StripLines.Add(slHoliday);
    //        }

    //    }

    //    protected void SetCursorChartPoint(PaintEventArgs e)
    //    {
    //        if (cursorLocation.Value.X.IsBetween(0, Width, false) && cursorLocation.Value.Y.IsBetween(0, Height, false))
    //        {
    //            currentCursorPoint = new Tuple<DateTime, double>(
    //                DateTime.FromOADate(chartArea.AxisX.PixelPositionToValue(cursorLocation.Value.X)).AddHours(12).Date,
    //                chartArea.AxisY.PixelPositionToValue(cursorLocation.Value.Y));
    //        }
    //    }

    //    protected override void OnPaint(PaintEventArgs e)
    //    {
    //        base.OnPaint(e);

    //        if (NoData) PaintNoData(e);

    //        if (!Ready || cursorLocation == null)
    //            return;

    //        SetCursorChartPoint(e);
    //    }
    //    protected void PaintNoData(PaintEventArgs e)
    //    {
    //        using (SolidBrush brush = new SolidBrush(Color.FromArgb(35, 35, 35)))
    //        {
    //            Graphics g = e.Graphics;
    //            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
    //            g.DrawString($"No Data", Helpers.SystemFont(100), brush, Width / 3, Height / 3, new StringFormat());
    //            return;
    //        }
    //    }
    //    protected void PaintHorizontalCursorLine(PaintEventArgs e)
    //    {
    //        using (Pen pen = new Pen(Color.DimGray, 1))
    //        {
    //            // Draw Horizontal Line
    //            Point pt1 = new Point(
    //                Convert.ToInt32(chartArea.AxisX.ValueToPixelPosition(CurrentView.Item1.ToOADate())),
    //                cursorLocation.Value.Y);

    //            Point pt2 = new Point(
    //                Convert.ToInt32(chartArea.AxisX.ValueToPixelPosition(CurrentView.Item2.ToOADate())),
    //                cursorLocation.Value.Y);

    //            e.Graphics.DrawLine(pen, pt1, pt2);
    //        }
    //    }
    //    protected void PaintVerticalCursorLine(PaintEventArgs e)
    //    {
    //        using (Pen pen = new Pen(Color.DimGray, 1))
    //        {
    //            // Draw Vertical Line
    //            Point pt1 = new Point(cursorLocation.Value.X,
    //                Convert.ToInt32(chartArea.AxisY.ValueToPixelPosition(chartArea.AxisY.Maximum)));

    //            Point pt2 = new Point(cursorLocation.Value.X,
    //                Convert.ToInt32(chartArea.AxisY.ValueToPixelPosition(chartArea.AxisY.Minimum)));

    //            e.Graphics.DrawLine(pen, pt1, pt2);
    //        }
    //    }

    //}

    #endregion
    #region Single Security Chart

    /*
     *  Implementation of FinanceChart which displays a single Security as a candlestick chart (using SecuritSeries)
     */

    //public class SingleSecurityChart : FinanceChart
    //{
    //    SecuritySeries securitySeries;
    //    SignalSeries signalSeries;
    //    PositionSeries positionSeries;
    //    TradeSeries tradeSeries;

    //    public Security security { get; private set; }
    //    PriceBarInfoPanel barTooltip;

    //    #region Display Options

    //    // As user zooms in and out, adjust data bars to daily/weekly/monthly; false = all daily bars
    //    public bool ZoomDataBars { get; set; } = true;
    //    public bool ToolTips { get; set; } = true;

    //    #endregion

    //    static decimal _bufferYAxis = 0.05m;

    //    private BarSize currentBarSize { get; set; }

    //    public SingleSecurityChart()
    //    {
    //        SetStyles();
    //        SetTooltip();
    //        SetBlankState();
    //    }

    //    [Initializer]
    //    private void SetStyles()
    //    {
    //        //
    //        // Default values are contained in base class
    //        //
    //        chartArea = new ChartArea("default");
    //        ChartAreas.Clear();
    //        ChartAreas.Add(chartArea);
    //        chartArea.AxisY.Title = "Share Price";

    //        DrawWeekendStriplines();
    //        DrawHolidayStriplines();
    //        DrawMonthStartStriplines();
    //        DrawYearStartStriplines();

    //        MouseMove += DrawSelectionHighlightStripline;
    //    }
    //    [Initializer]
    //    private void SetTooltip()
    //    {
    //        barTooltip = new PriceBarInfoPanel
    //        {
    //            Visible = false
    //        };
    //        Controls.Add(barTooltip);
    //    }
    //    [Initializer]
    //    private void SetBlankState()
    //    {
    //        securitySeries = SecuritySeries.Default();
    //        NoData = true;
    //        CurrentView = new Tuple<DateTime, DateTime>(securitySeries.MinX, securitySeries.MaxX);
    //        Redraw();
    //    }

    //    protected override void Redraw()
    //    {
    //        if (InvokeRequired)
    //        {
    //            Invoke(new Action(() => Redraw()));
    //            return;
    //        }

    //        // Load Series into Chart Area
    //        Series.Clear();
    //        securitySeries.ChartArea = "default";
    //        Series.Add(securitySeries);

    //        // Set X
    //        SetXAxis(CurrentView.Item1, CurrentView.Item2);

    //        // Set Y
    //        SetYAxis(securitySeries.MinY(CurrentView.Item1, CurrentView.Item2),
    //                securitySeries.MaxY(CurrentView.Item1, CurrentView.Item2));

    //        // Set Secondary Y
    //        SetSecondaryYAxis();

    //        // Load Signals if present
    //        if (signalSeries != null)
    //        {
    //            signalSeries.ChartArea = "default";
    //            Series.Add(signalSeries);
    //        }

    //        // Load Positions if present
    //        if (positionSeries != null)
    //        {
    //            positionSeries.ChartArea = "default";
    //            positionSeries.YAxisType = AxisType.Secondary;
    //            Series.Add(positionSeries);
    //        }

    //        // Load Trade and Stop series
    //        if (tradeSeries != null)
    //        {
    //            tradeSeries.ChartArea = "default";
    //            Series.Add(tradeSeries);
    //        }

    //        // Refresh View
    //        Update();

    //        Ready = true;
    //    }

    //    private void SetXAxis(DateTime min, DateTime max)
    //    {
    //        chartArea.AxisX.Minimum = min.ToOADate();
    //        chartArea.AxisX.Maximum = max.ToOADate();

    //        switch (currentBarSize)
    //        {
    //            case BarSize.Daily:
    //                {
    //                    chartArea.AxisX.IntervalType = DateTimeIntervalType.Weeks;
    //                    chartArea.AxisX.IntervalOffset = 1;
    //                    chartArea.AxisX.LabelStyle = new LabelStyle() { Format = "MM/dd/yy" };
    //                }
    //                break;
    //            case BarSize.Weekly:
    //                {
    //                    chartArea.AxisX.IntervalType = DateTimeIntervalType.Weeks;
    //                    chartArea.AxisX.IntervalOffset = 1;
    //                    chartArea.AxisX.LabelStyle = new LabelStyle() { Format = "MM/dd/yy" };
    //                }
    //                break;
    //            case BarSize.Monthly:
    //                {
    //                    chartArea.AxisX.IntervalType = DateTimeIntervalType.Months;
    //                    chartArea.AxisX.IntervalOffset = 0;
    //                    chartArea.AxisX.LabelStyle = new LabelStyle() { Format = "MMM yy" };
    //                }
    //                break;
    //            default:
    //                break;
    //        }
    //    }
    //    private void SetYAxis(decimal min, decimal max)
    //    {
    //        double minY = (min * (1 - _bufferYAxis)).ToDouble();
    //        double maxY = (max * (1 + _bufferYAxis)).ToDouble();

    //        minY = Math.Floor(minY);
    //        maxY = Math.Ceiling(maxY);

    //        chartArea.AxisY.Minimum = minY;
    //        chartArea.AxisY.Maximum = maxY;

    //        // Display interval lines rounded to nearest whole value depending on share price
    //        double IntervalSpan = (chartArea.AxisY.Maximum - chartArea.AxisY.Minimum);
    //        chartArea.AxisY.Interval = Math.Floor(IntervalSpan / 5);
    //    }
    //    private void SetSecondaryYAxis()
    //    {
    //        if (positionSeries == null)
    //        {
    //            chartArea.AxisY2.Enabled = AxisEnabled.False;
    //            return;
    //        }

    //        chartArea.AxisY2.Enabled = AxisEnabled.True;

    //        double max = positionSeries.MaxY(CurrentView.Item1, CurrentView.Item2).ToDouble();
    //        double min = Math.Abs(positionSeries.MinY(CurrentView.Item1, CurrentView.Item2).ToDouble());

    //        max = Math.Max(min, max);

    //        //
    //        // The secondary Y Axis is centered at zero with an equal range positive and negative to allow for long/short positions
    //        //            
    //        double maxY = Math.Ceiling(max * 1.5);

    //        chartArea.AxisY2.Maximum = maxY;
    //        chartArea.AxisY2.Minimum = -maxY;

    //        chartArea.AxisY2.Interval = Math.Floor(maxY / 5);
    //        chartArea.AxisY2.Crossing = 0;
    //        chartArea.AxisY2.MajorGrid.Enabled = true;
    //        chartArea.AxisY2.MajorGrid.LineColor = Color.FromArgb(64, 128, 128, 128);
    //        chartArea.AxisY2.MajorGrid.Interval = maxY;

    //    }
    //    private void SetSeriesZoomLevel()
    //    {
    //        if (CurrentView.Span().TotalDays < 360 || ZoomDataBars == false)
    //        {
    //            if (currentBarSize != BarSize.Daily)
    //                currentBarSize = BarSize.Daily;
    //        }
    //        else if (CurrentView.Span().TotalDays < 720)
    //        {
    //            if (currentBarSize != BarSize.Weekly)
    //                currentBarSize = BarSize.Weekly;
    //        }
    //        else if (CurrentView.Span().TotalDays >= 720)
    //        {
    //            if (currentBarSize != BarSize.Monthly)
    //                currentBarSize = BarSize.Monthly;
    //        }

    //        if (securitySeries.BarSize != currentBarSize)
    //        {
    //            securitySeries.SelectSeries(currentBarSize);
    //            SetZoomLevelOptions();
    //        }
    //    }
    //    private void SetZoomLevelOptions()
    //    {
    //        //
    //        // Adjust Stripline Options
    //        //
    //        chartArea.AxisX.StripLines.Clear();
    //        switch (currentBarSize)
    //        {
    //            case BarSize.Daily:
    //            case BarSize.Weekly:
    //                DrawWeekendStriplines();
    //                DrawHolidayStriplines();
    //                DrawMonthStartStriplines();
    //                DrawYearStartStriplines();
    //                break;
    //            case BarSize.Monthly:
    //                DrawYearStartStriplines();
    //                break;
    //            default:
    //                break;
    //        }
    //    }

    //    public void Load(Security security)
    //    {
    //        if (security == null) return;

    //        signalSeries = null;
    //        positionSeries = null;

    //        Ready = false;
    //        this.security = security;
    //        securitySeries = security.ToChartSeries();

    //        if (!securitySeries.HasPoints)
    //        {
    //            securitySeries = SecuritySeries.Default();
    //            NoData = true;
    //        }
    //        else
    //            NoData = false;

    //        // Set current view based on points available
    //        if (securitySeries.MinX > securitySeries.MaxX.Subtract(_defaultView))
    //            CurrentView = new Tuple<DateTime, DateTime>(securitySeries.MinX, securitySeries.MaxX);
    //        else
    //            CurrentView = new Tuple<DateTime, DateTime>(securitySeries.MaxX.Subtract(_defaultView), securitySeries.MaxX);

    //        if (securitySeries.Points.Count > 0)
    //            Redraw();
    //    }
    //    public void LoadSignals(Simulation simulation)
    //    {
    //        if (security == null) return;

    //        signalSeries = new SignalSeries(simulation, this.security);
    //        Redraw();
    //    }
    //    public void LoadPositions(Simulation simulation)
    //    {
    //        if (security == null) return;

    //        positionSeries = new PositionSeries(simulation, this.security);
    //        Redraw();
    //    }
    //    public void LoadTrades(Simulation simulation)
    //    {
    //        if (security == null) return;

    //        tradeSeries = new TradeSeries(simulation, this.security);
    //        Redraw();
    //    }
    //    public override void SetView(DateTime min, DateTime max)
    //    {
    //        if (securitySeries == null)
    //            return;

    //        //if (!min.IsBetween(securitySeries.MinX, securitySeries.MaxX, false))
    //        //    return;

    //        CurrentView = new Tuple<DateTime, DateTime>(min, max);
    //        SetSeriesZoomLevel();
    //        Redraw();
    //    }

    //    #region Chart Visual Effects

    //    //
    //    // Striplines highlighting the day under the cursor
    //    //
    //    private List<StripLine> selectionHighlightLines = new List<StripLine>();
    //    private void DrawSelectionHighlightStripline(object sender, MouseEventArgs e)
    //    {
    //        //
    //        // Add a stripline highlighting the selected time we are hovering over
    //        //
    //        if (!Ready || currentCursorPoint == null) return;

    //        if (selectionHighlightLines.Count > 0)
    //            selectionHighlightLines.ForEach(x => chartArea.AxisX.StripLines.Remove(x));
    //        selectionHighlightLines.Clear();

    //        StripLine slHighlight = new StripLine()
    //        {
    //            IntervalType = DateTimeIntervalType.Auto,
    //            BackColor = Color.FromArgb(0, 25, 0),
    //            BorderColor = Color.FromArgb(10, 10, 10)
    //        };

    //        switch (currentBarSize)
    //        {
    //            case BarSize.Daily:
    //                {
    //                    slHighlight.IntervalOffset = currentCursorPoint.Item1.ToOADate() - 0.5;
    //                    slHighlight.StripWidthType = DateTimeIntervalType.Days;
    //                    slHighlight.StripWidth = 1;
    //                }
    //                break;
    //            case BarSize.Weekly:
    //                {
    //                    slHighlight.IntervalOffset = currentCursorPoint.Item1.ToOADate() - currentCursorPoint.Item1.DayOfWeek.ToInt();
    //                    slHighlight.StripWidthType = DateTimeIntervalType.Days;
    //                    slHighlight.StripWidth = 5;
    //                }
    //                break;
    //            case BarSize.Monthly:
    //                {
    //                    // TODO: Fix this
    //                    slHighlight.IntervalOffset = new DateTime(currentCursorPoint.Item1.Year, currentCursorPoint.Item1.Month, 15).ToOADate();
    //                    slHighlight.StripWidthType = DateTimeIntervalType.Months;
    //                    slHighlight.StripWidth = 1;
    //                }
    //                break;
    //            default:
    //                break;
    //        }

    //        selectionHighlightLines.Add(slHighlight);
    //        selectionHighlightLines.ForEach(x => chartArea.AxisX.StripLines.Add(x));
    //    }

    //    protected override void OnPaint(PaintEventArgs e)
    //    {
    //        base.OnPaint(e);

    //        if (!Ready || cursorLocation == null)
    //            return;

    //        //
    //        // Draw Axis Lines
    //        //
    //        PaintHorizontalCursorLine(e);

    //        //
    //        // Draw Axis labels               
    //        //
    //        PaintAxisXLabel(e);
    //        PaintAxisYLabel(e);

    //        //
    //        // Adjust Position Size Widths
    //        //
    //        if (positionSeries != null && positionSeries["PixelPointWidth"] != (chartArea.ChartPointPixelWidth()).ToString())
    //            positionSeries["PixelPointWidth"] = (chartArea.ChartPointPixelWidth()).ToString();
    //    }
    //    protected void PaintAxisXLabel(PaintEventArgs e)
    //    {
    //        if (currentCursorPoint == null)
    //            return;

    //        Point pt1 = new Point(
    //            cursorLocation.Value.X,
    //            Convert.ToInt32(chartArea.AxisY.ValueToPixelPosition(chartArea.AxisY.Minimum)));

    //        using (SolidBrush brush = new SolidBrush(Color.White))
    //        {
    //            Graphics g = e.Graphics;
    //            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

    //            switch (currentBarSize)
    //            {
    //                case BarSize.Daily:
    //                    g.DrawString($"{currentCursorPoint.Item1:ddd MM/dd/yy}", Helpers.SystemFont(12), brush, cursorLocation.Value.X, pt1.Y - 20);
    //                    break;
    //                case BarSize.Weekly:
    //                    DateTime displayDate = currentCursorPoint.Item1.AddDays(-currentCursorPoint.Item1.DayOfWeek.ToInt());
    //                    g.DrawString($"{displayDate:dd MMM}-{displayDate.AddDays(4):dd MMM yy}", Helpers.SystemFont(12), brush, cursorLocation.Value.X, pt1.Y - 20);
    //                    break;
    //                case BarSize.Monthly:
    //                    g.DrawString($"{currentCursorPoint.Item1:MMM yy}", Helpers.SystemFont(12), brush, cursorLocation.Value.X, pt1.Y - 20);
    //                    break;
    //                default:
    //                    break;
    //            }
    //        }
    //    }
    //    protected void PaintAxisYLabel(PaintEventArgs e)
    //    {
    //        if (currentCursorPoint == null)
    //            return;

    //        Point pt1 = new Point(
    //            Convert.ToInt32(chartArea.AxisX.ValueToPixelPosition(CurrentView.Item1.ToOADate())),
    //            cursorLocation.Value.Y);

    //        using (SolidBrush brush = new SolidBrush(Color.White))
    //        {
    //            Graphics g = e.Graphics;
    //            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
    //            g.DrawString($"{currentCursorPoint.Item2:$0.00}", Helpers.SystemFont(12), brush, pt1.X, cursorLocation.Value.Y);
    //        }
    //    }

    //    #endregion
    //}

    //public class SecuritySeries : Series
    //{
    //    public DateTime MinX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Min()); }
    //    public DateTime MaxX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Max()); }
    //    public decimal MinY(DateTime start, DateTime end)
    //    {
    //        if (Points.Count == 0)
    //            return 0;

    //        try
    //        {
    //            return (from pt in Points
    //                    where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
    //                    where pt.YValues.Count() > 0
    //                    select pt.YValues.Min()).Min().ToDecimal();
    //        }
    //        catch (Exception)
    //        {
    //            return 0;
    //        }

    //    }
    //    public decimal MaxY(DateTime start, DateTime end)
    //    {
    //        if (Points.Count == 0)
    //            return 0;

    //        try
    //        {
    //            return (from pt in Points
    //                    where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
    //                    where pt.YValues.Count() > 0
    //                    select pt.YValues.Max()).Max().ToDecimal();
    //        }
    //        catch (Exception)
    //        {
    //            return 0;
    //        }
    //    }

    //    public PriceBar FromXValue(DateTime X)
    //    {
    //        return priceBars.FirstOrDefault(x => x.BarDateTime == X);
    //    }
    //    public bool HasPoints
    //    {
    //        get
    //        {
    //            return (Points.Count > 0);
    //        }
    //    }

    //    public BarSize BarSize { get; private set; } = BarSize.Daily;

    //    public Security Security { get; }
    //    private List<PriceBar> priceBars { get; set; }
    //    private Dictionary<BarSize, List<DataPoint>> seriesViews { get; }

    //    public SecuritySeries(Security security)
    //    {
    //        Security = security ?? throw new ArgumentNullException(nameof(security));

    //        seriesViews = new Dictionary<BarSize, List<DataPoint>>();

    //        this.InitializeMe();
    //    }
    //    private SecuritySeries() { }
    //    public static SecuritySeries Default()
    //    {
    //        SecuritySeries ret = new SecuritySeries();
    //        ret.SetStyles();

    //        ret.Points.AddXY(DateTime.Today.AddDays(-30).ToOADate(), 0);
    //        ret.Points.AddXY(DateTime.Today.ToOADate(), 1);

    //        return ret;
    //    }

    //    [Initializer]
    //    private void SetStyles()
    //    {
    //        ChartType = SeriesChartType.Candlestick;

    //        this["PriceUpColor"] = "Green";
    //        this["PriceDownColor"] = "Red";
    //    }
    //    [Initializer]
    //    private void BuildSeries()
    //    {
    //        priceBars = Security.GetPriceBars();
    //        priceBars.Sort((x, y) => x.BarDateTime.CompareTo(y.BarDateTime));

    //        BuildSeries_Daily();
    //        BuildSeries_Weekly();
    //        BuildSeries_Monthly();

    //        Points.Clear();
    //        seriesViews[BarSize].ForEach(pt => Points.Add(pt));
    //    }
    //    private void BuildSeries_Daily()
    //    {
    //        var pts = new List<DataPoint>();
    //        foreach (PriceBar bar in priceBars)
    //        {
    //            var pt = new DataPoint(this)
    //            {
    //                XValue = bar.BarDateTime.ToOADate(),
    //                YValues = bar.AsChartingValue(),
    //                IsValueShownAsLabel = false,
    //                Tag = bar,
    //                Color = (bar.Change >= 0 ? Color.Green : Color.Red)
    //            };
    //            pts.Add(pt);
    //        }
    //        seriesViews.Add(BarSize.Daily, pts);
    //    }
    //    private void BuildSeries_Weekly()
    //    {
    //        var pts = new List<DataPoint>();
    //        foreach (PriceBar bar in priceBars.ToWeekly())
    //        {
    //            var pt = new DataPoint(this)
    //            {
    //                XValue = bar.BarDateTime.ToOADate(),
    //                YValues = bar.AsChartingValue(),
    //                IsValueShownAsLabel = false,
    //                Tag = bar,
    //                Color = (bar.Change >= 0 ? Color.Green : Color.Red)
    //            };
    //            pts.Add(pt);
    //        }
    //        seriesViews.Add(BarSize.Weekly, pts);
    //    }
    //    private void BuildSeries_Monthly()
    //    {
    //        var pts = new List<DataPoint>();
    //        foreach (PriceBar bar in priceBars.ToMonthly())
    //        {
    //            var pt = new DataPoint(this)
    //            {
    //                XValue = bar.BarDateTime.ToOADate(),
    //                YValues = bar.AsChartingValue(),
    //                IsValueShownAsLabel = false,
    //                Tag = bar,
    //                Color = (bar.Change >= 0 ? Color.Green : Color.Red)
    //            };
    //            pts.Add(pt);
    //        }
    //        seriesViews.Add(BarSize.Monthly, pts);
    //    }

    //    public void SelectSeries(BarSize barSize)
    //    {
    //        if (barSize == BarSize)
    //            return;

    //        BarSize = barSize;
    //        Points.Clear();
    //        seriesViews[barSize].ForEach(pt => Points.Add(pt));
    //    }
    //}
    //public class SignalSeries : Series
    //{
    //    public DateTime MinX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Min()); }
    //    public DateTime MaxX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Max()); }
    //    public decimal MinY(DateTime start, DateTime end)
    //    {
    //        return (from pt in Points
    //                where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
    //                where pt.YValues.Count() > 0
    //                select pt.YValues.Min()).Min().ToDecimal();
    //    }
    //    public decimal MaxY(DateTime start, DateTime end)
    //    {
    //        return (from pt in Points
    //                where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
    //                where pt.YValues.Count() > 0
    //                select pt.YValues.Max()).Max().ToDecimal();
    //    }
    //    public Signal FromXValue(DateTime X)
    //    {
    //        return signals.FirstOrDefault(x => x.SignalDate == X);
    //    }
    //    public bool HasPoints
    //    {
    //        get
    //        {
    //            return (Points.Count > 0);
    //        }
    //    }

    //    public Security Security { get; }
    //    public Simulation Simulation { get; }

    //    private List<Signal> signals { get; set; }

    //    public SignalSeries(Simulation simulation, Security security)
    //    {
    //        Security = security ?? throw new ArgumentNullException(nameof(security));
    //        Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));

    //        this.InitializeMe();
    //    }
    //    private SignalSeries() { }
    //    public static SignalSeries Default()
    //    {
    //        SignalSeries ret = new SignalSeries();
    //        ret.SetStyles();

    //        ret.Points.AddXY(DateTime.Today.AddDays(-30).ToOADate(), 0);
    //        ret.Points.AddXY(DateTime.Today.ToOADate(), 1);

    //        return ret;
    //    }

    //    [Initializer]
    //    private void SetStyles()
    //    {
    //        ChartType = SeriesChartType.Point;
    //    }
    //    [Initializer]
    //    private void BuildSeries()
    //    {
    //        Points.Clear();
    //        signals = Simulation.StrategyManager.GetSignalHistory(Security);

    //        foreach (Signal signal in signals)
    //        {
    //            if (signal.SignalAction == TradeActionBuySell.None)
    //                continue;

    //            var pt = new DataPoint(this)
    //            {
    //                XValue = signal.SignalDate.ToOADate(),
    //                IsValueShownAsLabel = false,
    //                Tag = signal,
    //                Color = (signal.SignalAction == TradeActionBuySell.Buy ? Color.LightGreen : Color.PaleVioletRed),
    //                MarkerStyle = MarkerStyle.Circle,
    //                MarkerSize = 7,
    //                MarkerBorderColor = Color.DarkSlateGray,

    //            };

    //            //
    //            // Set Y value based on buy or sell
    //            //
    //            var bar = Security.GetPriceBar(signal.SignalDate);
    //            switch (signal.SignalAction)
    //            {
    //                case TradeActionBuySell.Buy:
    //                    pt.YValues = new[] { bar.Close.ToDouble() * 1.02 };
    //                    break;
    //                case TradeActionBuySell.Sell:
    //                    pt.YValues = new[] { bar.Close.ToDouble() * 0.98 };
    //                    break;
    //            }

    //            Points.Add(pt);
    //        }
    //    }
    //}
    //public class PositionSeries : Series
    //{
    //    public DateTime MinX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Min()); }
    //    public DateTime MaxX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Max()); }
    //    public decimal MinY(DateTime start, DateTime end)
    //    {
    //        var ret = (from pt in Points
    //                   where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
    //                   where pt.YValues.Count() > 0
    //                   select pt.YValues.Min());

    //        if (ret.Count() > 0)
    //            return ret.Min().ToDecimal();
    //        else
    //            return 1;
    //    }
    //    public decimal MaxY(DateTime start, DateTime end)
    //    {
    //        var ret = (from pt in Points
    //                   where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
    //                   where pt.YValues.Count() > 0
    //                   select pt.YValues.Max());

    //        if (ret.Count() > 0)
    //            return ret.Max().ToDecimal();
    //        else
    //            return 1;
    //    }
    //    public Position FromXValue(DateTime X)
    //    {
    //        return positions.FirstOrDefault(x => x.ExecutedTrades.Where(y => y.TradeDate == X).Count() > 0);
    //    }
    //    public bool HasPoints
    //    {
    //        get
    //        {
    //            return (Points.Count > 0);
    //        }
    //    }

    //    public Security Security { get; }
    //    public Simulation Simulation { get; }

    //    private List<Position> positions { get; set; }

    //    public PositionSeries(Simulation simulation, Security security)
    //    {
    //        Security = security ?? throw new ArgumentNullException(nameof(security));
    //        Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));

    //        this.InitializeMe();
    //    }
    //    private PositionSeries() { }
    //    public static PositionSeries Default()
    //    {
    //        PositionSeries ret = new PositionSeries();
    //        ret.SetStyles();

    //        ret.Points.AddXY(DateTime.Today.AddDays(-30).ToOADate(), 0);
    //        ret.Points.AddXY(DateTime.Today.ToOADate(), 1);

    //        return ret;
    //    }

    //    [Initializer]
    //    private void SetStyles()
    //    {
    //        ChartType = SeriesChartType.Column;
    //        //MarkerBorderWidth = 1;
    //    }
    //    [Initializer]
    //    private void BuildSeries()
    //    {
    //        Points.Clear();
    //        positions = Simulation.PortfolioManager.Portfolio.GetPositions(Security);

    //        foreach (Position position in positions)
    //        {
    //            DateTime startDate = position.ExecutedTrades.Min(x => x.TradeDate);

    //            DateTime endDate;
    //            if (position.IsOpen(Simulation.SimulationTimeSpan.Item2))
    //                endDate = Simulation.SimulationTimeSpan.Item2;
    //            else
    //                endDate = position.ExecutedTrades.Max(x => x.TradeDate);

    //            while (startDate <= endDate)
    //            {
    //                if (Calendar.IsTradingDay(startDate))
    //                {
    //                    var pt = new DataPoint(this)
    //                    {
    //                        XValue = startDate.ToOADate(),
    //                        YValues = new[] { Convert.ToDouble(position.Size(startDate)) },
    //                        IsValueShownAsLabel = false,
    //                        Tag = position
    //                    };
    //                    pt.Color = pt.YValues[0] > 0 ? Color.FromArgb(64, 0, 255, 0) : Color.FromArgb(64, 255, 0, 0);
    //                    Points.Add(pt);
    //                }
    //                else
    //                {
    //                    var pt = Points.Last().Clone();
    //                    pt.XValue = startDate.ToOADate();
    //                    pt.Color = pt.YValues[0] > 0 ? Color.FromArgb(64, 0, 255, 0) : Color.FromArgb(64, 255, 0, 0);
    //                    Points.Add(pt);
    //                }

    //                startDate = startDate.AddDays(1);
    //            }
    //        }
    //    }
    //}

    //public class TradeSeries : Series
    //{
    //    public DateTime MinX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Min()); }
    //    public DateTime MaxX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Max()); }
    //    public decimal MinY(DateTime start, DateTime end)
    //    {
    //        return (from pt in Points
    //                where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
    //                where pt.YValues.Count() > 0
    //                select pt.YValues.Min()).Min().ToDecimal();
    //    }
    //    public decimal MaxY(DateTime start, DateTime end)
    //    {
    //        return (from pt in Points
    //                where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
    //                where pt.YValues.Count() > 0
    //                select pt.YValues.Max()).Max().ToDecimal();
    //    }
    //    public Trade FromXValue(DateTime X)
    //    {
    //        return trades.FirstOrDefault(x => x.TradeDate == X);
    //    }
    //    public bool HasPoints
    //    {
    //        get
    //        {
    //            return (Points.Count > 0);
    //        }
    //    }

    //    public Security Security { get; }
    //    public Simulation Simulation { get; }

    //    private List<Trade> trades { get; set; }
    //    private List<Trade> stops { get; set; }

    //    public TradeSeries(Simulation simulation, Security security)
    //    {
    //        Security = security ?? throw new ArgumentNullException(nameof(security));
    //        Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));

    //        this.InitializeMe();
    //    }
    //    private TradeSeries() { }
    //    public static TradeSeries Default()
    //    {
    //        TradeSeries ret = new TradeSeries();
    //        ret.SetStyles();

    //        ret.Points.AddXY(DateTime.Today.AddDays(-30).ToOADate(), 0);
    //        ret.Points.AddXY(DateTime.Today.ToOADate(), 1);

    //        return ret;
    //    }

    //    [Initializer]
    //    private void SetStyles()
    //    {
    //        ChartType = SeriesChartType.Point;
    //    }
    //    [Initializer]
    //    private void BuildTradeSeries()
    //    {
    //        Points.Clear();

    //        var trades = (from pos in Simulation.PortfolioManager.Portfolio.GetPositions(Security) select pos.ExecutedTrades)
    //                    .ToList().SelectMany(x => x).ToList();

    //        foreach (Trade trade in trades)
    //        {
    //            var pt = new DataPoint(this)
    //            {
    //                XValue = trade.TradeDate.ToOADate(),
    //                IsValueShownAsLabel = false,
    //                Tag = trade,
    //                Color = Color.Transparent,
    //                MarkerStyle = MarkerStyle.Diamond,
    //                MarkerSize = 10,
    //                MarkerBorderWidth = 2,
    //                MarkerBorderColor = Color.White
    //            };

    //            //
    //            // Set Y value based on buy or sell
    //            //
    //            var bar = Security.GetPriceBar(trade.TradeDate);
    //            switch (trade.TradeActionBuySell)
    //            {
    //                case TradeActionBuySell.Buy:
    //                    pt.YValues = new[] { bar.Close.ToDouble() };
    //                    break;
    //                case TradeActionBuySell.Sell:
    //                    pt.YValues = new[] { bar.Close.ToDouble() };
    //                    break;
    //            }
    //            Points.Add(pt);
    //        }
    //    }
    //    [Initializer]
    //    private void BuildStopSeries()
    //    {
    //        var trades = Simulation.PortfolioManager.TradeManager.GetAllStoplosses(Security);

    //        foreach (Trade trade in trades)
    //        {
    //            var pt = new DataPoint(this)
    //            {
    //                XValue = trade.TradeDate.ToOADate(),
    //                YValues = new[] { trade.ExpectedExecutionPrice.ToDouble() },
    //                IsValueShownAsLabel = false,
    //                Tag = trade,
    //                Color = Color.Yellow,
    //                MarkerStyle = MarkerStyle.Square,
    //                MarkerSize = 5,
    //                MarkerBorderColor = Color.DarkSlateGray
    //            };

    //            Points.Add(pt);
    //        }
    //    }
    //}

    #endregion
    #region Account Chart

    /*
     * Chart which displays selected portfolio values (equity over time, positions, etc)
     */

    //public class AccountChart : FinanceChart
    //{
    //    public Simulation Simulation { get; private set; }
    //    public AccountingSeries accountingSeries { get; private set; }

    //    static decimal _bufferYAxis = 0.05m;

    //    public AccountingSeriesValue AccountingSeriesValue { get; private set; }

    //    public AccountChart(Simulation simulation) : base()
    //    {
    //        chartArea = new ChartArea("default");

    //        this.InitializeMe();

    //        ChartAreas.Clear();
    //        ChartAreas.Add(chartArea);

    //        Load(simulation);
    //    }

    //    [Initializer]
    //    private void SetStyles()
    //    {
    //        //
    //        // Default values are contained in base class
    //        //
    //        chartArea.AxisY.Title = "Balance";

    //        chartArea.AxisX.IntervalType = DateTimeIntervalType.Weeks;
    //        chartArea.AxisX.IntervalOffset = 1;
    //        chartArea.AxisX.LabelStyle = new LabelStyle() { Format = "MM/dd/yy" };

    //        DrawWeekendStriplines();
    //        DrawHolidayStriplines();
    //        DrawMonthStartStriplines();
    //        DrawYearStartStriplines();

    //        MouseMove += DrawSelectionHighlightStripline;
    //    }

    //    protected override void Redraw()
    //    {
    //        if (InvokeRequired)
    //        {
    //            Invoke(new Action(() => Redraw()));
    //            return;
    //        }

    //        // Load Series into Chart Area
    //        Series.Clear();
    //        accountingSeries.ChartArea = "default";
    //        Series.Add(accountingSeries);

    //        // Set X
    //        SetXAxis(CurrentView.Item1, CurrentView.Item2);

    //        // Set Y
    //        SetYAxis(accountingSeries.MinY(CurrentView.Item1, CurrentView.Item2),
    //                accountingSeries.MaxY(CurrentView.Item1, CurrentView.Item2));

    //        // Refresh View
    //        Update();

    //        Ready = true;
    //    }

    //    private void SetXAxis(DateTime min, DateTime max)
    //    {
    //        chartArea.AxisX.Minimum = min.ToOADate();
    //        chartArea.AxisX.Maximum = max.ToOADate();
    //    }
    //    private void SetYAxis(decimal min, decimal max)
    //    {
    //        double minY = (min * (1 - _bufferYAxis)).ToDouble();
    //        double maxY = (max * (1 + _bufferYAxis)).ToDouble();

    //        minY = Math.Floor(minY);
    //        maxY = Math.Ceiling(maxY);

    //        chartArea.AxisY.Minimum = minY;
    //        chartArea.AxisY.Maximum = maxY;

    //        // Display interval lines rounded to nearest whole value depending on share price
    //        double IntervalSpan = (chartArea.AxisY.Maximum - chartArea.AxisY.Minimum);
    //        chartArea.AxisY.Interval = Math.Max(Math.Floor(IntervalSpan / 5), 1);


    //    }

    //    public void Load(Simulation simulation)
    //    {
    //        if (simulation == null) return;

    //        Ready = false;
    //        this.Simulation = simulation;
    //        accountingSeries = simulation.ToChartSeries();

    //        if (!accountingSeries.HasPoints)
    //        {
    //            accountingSeries = AccountingSeries.Default();
    //            NoData = true;
    //        }
    //        else
    //            NoData = false;

    //        CurrentView = new Tuple<DateTime, DateTime>(accountingSeries.MinX, accountingSeries.MaxX);

    //        if (accountingSeries.Points.Count > 0)
    //            Redraw();
    //    }
    //    public override void SetView(DateTime min, DateTime max)
    //    {
    //        if (!min.IsBetween(accountingSeries.MinX, accountingSeries.MaxX, false))
    //            return;

    //        CurrentView = new Tuple<DateTime, DateTime>(min, max);

    //        Redraw();
    //    }
    //    public void SetSeries(AccountingSeriesValue value)
    //    {
    //        if (AccountingSeriesValue == value)
    //            return;

    //        AccountingSeriesValue = value;
    //        accountingSeries.SelectSeries(value);
    //        Redraw();
    //    }

    //    #region Chart Visual Effects

    //    //
    //    // Striplines highlighting the day under the cursor
    //    //
    //    private List<StripLine> selectionHighlightLines = new List<StripLine>();
    //    private void DrawSelectionHighlightStripline(object sender, MouseEventArgs e)
    //    {
    //        //
    //        // Add a stripline highlighting the selected time we are hovering over
    //        //
    //        if (!Ready || currentCursorPoint == null) return;

    //        if (selectionHighlightLines.Count > 0)
    //            selectionHighlightLines.ForEach(x => chartArea.AxisX.StripLines.Remove(x));
    //        selectionHighlightLines.Clear();

    //        StripLine slHighlight = new StripLine()
    //        {
    //            IntervalType = DateTimeIntervalType.Auto,
    //            BackColor = Color.FromArgb(0, 25, 0),
    //            BorderColor = Color.FromArgb(10, 10, 10)
    //        };

    //        slHighlight.IntervalOffset = currentCursorPoint.Item1.ToOADate() - 0.5;
    //        slHighlight.StripWidthType = DateTimeIntervalType.Days;
    //        slHighlight.StripWidth = 1;

    //        selectionHighlightLines.Add(slHighlight);
    //        selectionHighlightLines.ForEach(x => chartArea.AxisX.StripLines.Add(x));
    //    }

    //    protected override void OnPaint(PaintEventArgs e)
    //    {
    //        base.OnPaint(e);

    //        if (!Ready || cursorLocation == null)
    //            return;

    //        //
    //        // Draw Axis Lines
    //        //
    //        PaintHorizontalCursorLine(e);

    //        //
    //        // Draw Axis labels               
    //        //
    //        PaintAxisXLabel(e);
    //        PaintAxisYLabel(e);
    //    }
    //    protected void PaintAxisXLabel(PaintEventArgs e)
    //    {
    //        if (currentCursorPoint == null)
    //            return;

    //        Point pt1 = new Point(
    //            cursorLocation.Value.X,
    //            Convert.ToInt32(chartArea.AxisY.ValueToPixelPosition(chartArea.AxisY.Minimum)));

    //        using (SolidBrush brush = new SolidBrush(Color.White))
    //        {
    //            Graphics g = e.Graphics;
    //            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
    //            g.DrawString($"{currentCursorPoint.Item1:ddd MM/dd/yy}", Helpers.SystemFont(12), brush, cursorLocation.Value.X, pt1.Y - 20);

    //        }
    //    }
    //    protected void PaintAxisYLabel(PaintEventArgs e)
    //    {
    //        if (currentCursorPoint == null)
    //            return;

    //        Point pt1 = new Point(
    //            Convert.ToInt32(chartArea.AxisX.ValueToPixelPosition(CurrentView.Item1.ToOADate())),
    //            cursorLocation.Value.Y);

    //        using (SolidBrush brush = new SolidBrush(Color.White))
    //        {
    //            Graphics g = e.Graphics;
    //            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
    //            g.DrawString($"{currentCursorPoint.Item2:$0.00}", Helpers.SystemFont(12), brush, pt1.X, cursorLocation.Value.Y);
    //        }
    //    }

    //    #endregion
    //}

    //public class AccountingSeries : Series
    //{
    //    public DateTime MinX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Min()); }
    //    public DateTime MaxX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Max()); }
    //    public decimal MinY(DateTime start, DateTime end)
    //    {
    //        return (from pt in Points
    //                where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
    //                where pt.YValues.Count() > 0
    //                select pt.YValues.Min()).Min().ToDecimal();
    //    }
    //    public decimal MaxY(DateTime start, DateTime end)
    //    {
    //        return (from pt in Points
    //                where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
    //                where pt.YValues.Count() > 0
    //                select pt.YValues.Max()).Max().ToDecimal();
    //    }

    //    public bool HasPoints
    //    {
    //        get
    //        {
    //            return (Points.Count > 0);
    //        }
    //    }

    //    public Simulation Simulation { get; }
    //    public Portfolio Portfolio { get; }
    //    public AccountingSeriesValue AccountingSeriesValue { get; private set; }

    //    public AccountingSeries(Simulation simulation)
    //    {
    //        Simulation = simulation ?? throw new Exception(nameof(Simulation));
    //        Portfolio = Simulation.PortfolioManager.Portfolio;

    //        this.InitializeMe();
    //    }
    //    private AccountingSeries() { }
    //    public static AccountingSeries Default()
    //    {
    //        AccountingSeries ret = new AccountingSeries();
    //        ret.SetStyles();

    //        ret.Points.AddXY(DateTime.Today.AddDays(-30).ToOADate(), 0);
    //        ret.Points.AddXY(DateTime.Today.ToOADate(), 1);

    //        return ret;
    //    }

    //    [Initializer]
    //    private void SetStyles()
    //    {
    //        ChartType = SeriesChartType.Line;
    //    }
    //    [Initializer]
    //    private void BuildSeries()
    //    {
    //        Points.Clear();
    //        //
    //        // Iterate through the simulation time span and create a datapoint for each value
    //        //
    //        DateTime currentDate = Simulation.SimulationTimeSpan.Item1;
    //        while (currentDate <= Simulation.SimulationTimeSpan.Item2)
    //        {
    //            var pt = new DataPoint(this)
    //            {
    //                XValue = currentDate.ToOADate(),
    //                YValues = new double[] { Portfolio.GetByAccountingSeriesValue(this.AccountingSeriesValue, currentDate).ToDouble() },
    //                IsValueShownAsLabel = false
    //            };
    //            Points.Add(pt);
    //            currentDate = Calendar.NextTradingDay(currentDate);
    //        }
    //    }

    //    public void SelectSeries(AccountingSeriesValue value)
    //    {
    //        if (value == this.AccountingSeriesValue)
    //            return;

    //        this.AccountingSeriesValue = value;
    //        BuildSeries();
    //    }

    //}

    #endregion
}
