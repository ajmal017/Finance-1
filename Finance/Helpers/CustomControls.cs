using Finance.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Finance
{
    #region ControlManager

    //
    // Defines a generic class to implement a control issuer
    //

    public abstract class ControlManager
    {
        protected List<Control> issuedControls = new List<Control>();
        protected string ParentName { get; set; }

        protected ControlManager(string parentName)
        {
            ParentName = parentName ?? throw new ArgumentNullException(nameof(parentName));
        }

        public Control IssueControl()
        {
            return issuedControls.AddAndReturn(_IssueControl());
        }
        protected abstract Control _IssueControl();

    }

    #endregion
    #region Status Indicator Manager and Controls

    //
    // Control Manager instantiated within a class will issue out custom controls
    // which cn be updated locally to display the object's status on external forms
    //

    public class StatusLabelControlManager : ControlManager
    {
        private Tuple<string, Color> LastStatus = null;

        public StatusLabelControlManager(string parentName) : base(parentName)
        {
        }

        protected override Control _IssueControl()
        {
            var ret = issuedControls.AddAndReturn(new StatusIndicatorLabel(ParentName, issuedControls.Count));
            if (LastStatus != null)
                SetStatus(LastStatus.Item1, LastStatus.Item2);
            return ret;
        }

        public void SetStatus(string text, Color color)
        {
            foreach (StatusIndicatorLabel ctrl in issuedControls)
            {
                ctrl.SetStatus(text, color);
            }

            LastStatus = new Tuple<string, Color>(text, color);
        }
    }
    public class StatusIndicatorLabel : Label
    {
        public string DisplayName { get; set; }

        public StatusIndicatorLabel(string name, int instance)
        {
            Name = name + instance.ToString();
            DisplayName = Text = name;
            TextAlign = ContentAlignment.MiddleCenter;
            BorderStyle = BorderStyle.FixedSingle;
            BackColor = Color.LightBlue;
        }
        public void SetStatus(string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    SetStatus(text, color);
                }));
                return;
            }

            Text = $"{DisplayName}: {text}";
            BackColor = color;
            Refresh();
        }
    }

    #endregion
    #region Parameter Display Panel

    //
    // A Panel control which displays values from an object marked with ParameterAttribute
    // Provides interactive control for runtime-adjustment of parameter values
    //

    public class ParameterDisplayUpdatePanel : Panel
    {
        Label lblValueName = new Label();
        TextBox txtValue = new TextBox();
        ToolTip tipValueDescription = new ToolTip();
        Label lblMinMaxValues = new Label();

        int _Height = 30;
        Color backColor = Color.LightGray;

        public ParameterDisplayUpdatePanel(PropertyInfo property, object target)
        {
            if (!Attribute.IsDefined(property, typeof(ParameterAttribute)))
                throw new InvalidCastException();

            Height = _Height;
            BorderStyle = BorderStyle.FixedSingle;
            BackColor = backColor;

            AddValueName(property, target);
            AddValueTextBox(property, target);
            AddValueDescription(property, target);
            AddMinMaxValue(property, target);

            Arrange();
        }

        private void AddValueName(PropertyInfo property, object target)
        {
            var attr = property.GetCustomAttribute(typeof(ParameterAttribute)) as ParameterAttribute;

            lblValueName.Width = 200;
            lblValueName.Height = _Height;
            lblValueName.Text = attr.ValueName;
            lblValueName.TextAlign = ContentAlignment.MiddleLeft;
            lblValueName.Font = Helpers.SystemFont(8);

            Controls.Add(lblValueName);
        }
        private void AddValueTextBox(PropertyInfo property, object target)
        {
            var attr = property.GetCustomAttribute(typeof(ParameterAttribute)) as ParameterAttribute;

            txtValue.Width = 75;
            txtValue.Text = Convert.ToString(property.GetValue(target));
            txtValue.TextAlign = HorizontalAlignment.Center;
            txtValue.BorderStyle = BorderStyle.None;
            txtValue.Font = Helpers.SystemFont(12);

            string lastGoodValue = "";

            // Select all when entering the text box
            txtValue.Enter += (s, e) =>
            {
                lastGoodValue = txtValue.Text;
                txtValue.SelectAll();
            };

            // Update target when enter is pressed
            txtValue.KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.Return)
                    _UpdateControl();
            };

            // Update target when tab is pressed
            txtValue.PreviewKeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Tab)
                    _UpdateControl();
            };

            // Local function updates control
            void _UpdateControl()
            {
                if (txtValue.Text == lastGoodValue)
                    return;

                // Update
                if (Helpers.ModifyParameterValue(property, target, txtValue.Text))
                {
                    // Flash control to give feedback that the value was updated
                    BackColor = Color.LightGreen;
                    new Task(() =>
                    {
                        Thread.Sleep(2000);
                        Invoke(new Action(() =>
                        {
                            BackColor = backColor;
                        }));
                    }).Start();
                    lastGoodValue = txtValue.Text;
                    //Focus();
                }
                else
                {
                    // Flash control to give feedback that the value was NOT updated
                    BackColor = Color.PaleVioletRed;
                    new Task(() =>
                    {
                        Thread.Sleep(2000);
                        Invoke(new Action(() =>
                        {
                            BackColor = backColor;
                        }));
                    }).Start();
                    txtValue.Text = lastGoodValue;
                    txtValue.SelectAll();
                }
            }

            Controls.Add(txtValue);
        }
        private void AddValueDescription(PropertyInfo property, object target)
        {
            var attr = property.GetCustomAttribute(typeof(ParameterAttribute)) as ParameterAttribute;

            tipValueDescription.ToolTipIcon = ToolTipIcon.None;
            tipValueDescription.AutomaticDelay = 0;
            tipValueDescription.IsBalloon = false;
            tipValueDescription.ShowAlways = true;
            tipValueDescription.SetToolTip(lblValueName, attr.ValueDescription);
        }
        private void AddMinMaxValue(PropertyInfo property, object target)
        {
            var attr = property.GetCustomAttribute(typeof(ParameterAttribute)) as ParameterAttribute;

            lblMinMaxValues.Width = 100;
            lblMinMaxValues.Height = _Height;
            lblMinMaxValues.Text = attr.ToStringMinMaxValues();
            lblMinMaxValues.TextAlign = ContentAlignment.MiddleLeft;
            lblMinMaxValues.Font = Helpers.SystemFont(8);

            Controls.Add(lblMinMaxValues);
        }

        private void Arrange()
        {
            int xOffset = 0;
            foreach (Control control in Controls)
            {
                control.Location = new Point(xOffset, (Height - control.Height) / 2 - 1);
                xOffset += control.Width;
            }
            Width = xOffset;
        }
    }

    #endregion
    #region Strategy Display and Selection

    //
    // Provides a Panel which displays information on a Strategy as stored in the StrategyManager
    // list of available strategies.  Provides selection capabilities to select active strategy.
    //

    public class StrategyDisplayUpdatePanel : Panel
    {
        Label lblStrategyName = new Label();
        Label lblStrategyDesc = new Label();
        CheckBox chkActive = new CheckBox();

        int _Height = 60;
        int _Width = 375;

        public StrategyDisplayUpdatePanel(TradeStrategyBase strategy, StrategyManager manager)
        {
            Height = _Height;
            Width = _Width;
            BorderStyle = BorderStyle.FixedSingle;

            AddCheckBox(strategy, manager);
            AddStrategyName(strategy, manager);
            AddStrategyDesc(strategy, manager);

            foreach (Control control in Controls)
            {
                control.Click += (s, e) => chkActive.Checked = true;
            }

            Arrange();
        }

        private void AddCheckBox(TradeStrategyBase strategy, StrategyManager manager)
        {
            chkActive.Name = "check";
            chkActive.Size = new Size(20, 20);

            if (strategy == manager.ActiveTradeStrategy)
            {
                chkActive.Checked = true;
                chkActive.Enabled = false;
                BackColor = Color.LightGreen;
            }
            else
            {
                chkActive.Checked = false;
                chkActive.Enabled = true;
                BackColor = SystemColors.Control;
            }

            // Set strategy when the user selects
            chkActive.CheckedChanged += (s, e) =>
            {
                if (chkActive.Checked)
                    manager.SetStrategy(strategy);
            };

            // Update the active strategy when the checkmacrk is changed
            manager.StrategyChanged += (s, e) =>
            {
                if (strategy == manager.ActiveTradeStrategy)
                {
                    BackColor = Color.LightGreen;
                    chkActive.Enabled = false;
                }
                else
                {
                    BackColor = SystemColors.Control;
                    chkActive.Checked = false;
                    chkActive.Enabled = true;
                }
            };

            Controls.Add(chkActive);
        }
        private void AddStrategyName(TradeStrategyBase strategy, StrategyManager manager)
        {
            lblStrategyName.Name = "name";
            lblStrategyName.Width = _Width;
            lblStrategyName.Font = Helpers.SystemFont(10);
            lblStrategyName.Text = strategy.Name;

            Controls.Add(lblStrategyName);
        }
        private void AddStrategyDesc(TradeStrategyBase strategy, StrategyManager manager)
        {
            lblStrategyDesc.Name = "desc";
            lblStrategyDesc.Width = _Width;
            lblStrategyDesc.Font = Helpers.SystemFont(8);
            lblStrategyDesc.Text = strategy.Description;

            Controls.Add(lblStrategyDesc);
        }

        private void Arrange()
        {
            Controls["check"].Location = new Point(4, 2);
            int x1 = Controls["check"].Width;

            Controls["name"].Location = new Point(4 + x1, 2);

            Controls["desc"].Location = new Point(4, 2 + x1);
        }
    }

    #endregion
    #region Security List Display/Selector

    public class SecurityListControlManager : ControlManager
    {
        private DataManager dataManagerRef { get; }
        public SecurityListControlManager(DataManager dataManagerRef, string parentName) : base(parentName)
        {
            this.dataManagerRef = dataManagerRef ?? throw new ArgumentNullException(nameof(dataManagerRef));
        }
        protected override Control _IssueControl()
        {
            var ret = issuedControls.AddAndReturn(new SecurityListBox(ParentName, issuedControls.Count, dataManagerRef));

            return ret;
        }
        public void UpdateLists()
        {
            foreach (SecurityListBox control in issuedControls)
            {
                control.UpdateList();
            }
        }
    }
    public class SecurityListBox : ListBox
    {
        private DataManager dataManager { get; }
        public string DisplayName { get; set; }
        public Security SelectedSecurity
        {
            get
            {
                if (SelectedIndex == -1)
                    return null;
                return dataManager.GetSecurity(SelectedValue as string);
            }
        }

        public SecurityListBox(string name, int instance, DataManager dataManager)
        {
            Name = name + instance.ToString();
            DisplayName = Text = name;
            BorderStyle = BorderStyle.FixedSingle;
            SelectionMode = SelectionMode.One;
            Font = Helpers.SystemFont(12);
            Size = new Size(175, 250);
            this.dataManager = dataManager;
            DataSource = dataManager.GetAllTickers();
            DoubleBuffered = true;
        }
        public void UpdateList()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { UpdateList(); }));
            }
            else
            {
                string selectedTicker = SelectedValue as string;
                DataSource = null;
                DataSource = dataManager.GetAllTickers();
                int i = (DataSource as List<string>).IndexOf(selectedTicker);
                SelectedIndex = i;
                Refresh();
            }
        }

    }

    #endregion
    #region Security Info Panel

    public class SecurityInfoPanel : Panel
    {
        public Security security { get; private set; }
        private DataManager dataManager { get; } = null;

        GroupBox grpContents;
        Button btnUpdateSecurity;

        Size _pnlSize = new Size(350, 250);

        public SecurityInfoPanel(Security security, DataManager dataManager = null)
        {
            this.security = security;
            this.dataManager = dataManager;

            Size = _pnlSize;

            //
            // Group Box to hold all controls
            //
            grpContents = new GroupBox()
            {
                Text = "Security Data",
                Size = new Size(_pnlSize.Width - 5, _pnlSize.Height - 5),
                Location = new Point(2, 0)
            };

            //
            // Button to request datamanager update security
            //
            btnUpdateSecurity = new Button()
            {
                Name = "btnUpdateSecurity",
                Text = "Update Security",
                Size = new Size(175, 25),
                BackColor = Color.Orange,
                Dock = DockStyle.Bottom
            };
            btnUpdateSecurity.Click += (s, e) =>
            {
                if (this.security == null || dataManager == null)
                    return;

                dataManager.UpdateSecurity(this.security, DateTime.Today);
                grpContents.Controls.Remove(btnUpdateSecurity);

                Label lblUpdating = new Label()
                {
                    Text = "Updating...",
                    Width = grpContents.Width,
                    Dock = DockStyle.Bottom,
                    Font = Helpers.SystemFont(12, FontStyle.Bold)
                };
                grpContents.Controls.Add(lblUpdating);
            };

            //
            // Link datamanager callback to refresh
            //
            if (this.dataManager != null)
            {
                this.dataManager.SecurityDataResponse += (s, e) =>
                {
                    Redraw();
                };
            }

            Controls.Add(grpContents);

            Redraw();
        }
        public void Load(Security security)
        {
            this.security = security;
            Redraw();
        }
        private void Redraw()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { Redraw(); }));
                return;
            }

            grpContents.Controls.Clear();
            if (security == null)
                return;

            //
            // Get a list of all UI Output methods in Security (marked with Attribute)
            //
            var methods = (from method in security.GetType().GetMethods()
                           where Attribute.IsDefined(method, typeof(UiDisplayTextAttribute))
                           select method).
                          OrderBy(x => ((UiDisplayTextAttribute)x.GetCustomAttribute(typeof(UiDisplayTextAttribute))).Order);
            //
            // Add a label for each UI output method
            //
            foreach (var field in methods)
            {
                grpContents.Controls.Add(new Label()
                {
                    Text = field.Invoke(security, null) as string,
                    Width = (int)(grpContents.Width * .90),
                    Font = Helpers.SystemFont(8),
                    AutoEllipsis = true
                });
            }

            //
            // Arrange labels
            //
            grpContents.Controls[0].Location = new Point(5, 20);
            for (int i = 1; i < grpContents.Controls.Count; i++)
            {
                grpContents.Controls[i].DockTo(grpContents.Controls[i - 1], DockSide.Bottom);
            }

            //
            // Add update button
            //
            if (dataManager != null && !security.DataUpToDate)
            {
                ShowUpdateButton();
            }

            Refresh();
        }
        private void ShowUpdateButton()
        {
            grpContents.Controls.Add(btnUpdateSecurity);
        }
    }

    #endregion
    #region Finance Chart (Base Class)

    public abstract class FinanceChart : Chart
    {
        protected ChartArea chartArea;

        protected static Size _chrtSize = new Size(1500, 1000);
        protected static TimeSpan _defaultView = new TimeSpan(120, 0, 0, 0);
        protected static TimeSpan _zoomStep = new TimeSpan(2, 0, 0, 0);

        protected Point? cursorLocation = null;

        protected Tuple<DateTime, DateTime> currentView;
        protected Tuple<DateTime, double> currentCursorPoint = null;

        protected bool Ready { get; set; } = false;
        protected bool NoData { get; set; } = true;

        [Initializer]
        private void SetCursorLocation()
        {
            MouseMove += (s, e) =>
            {
                cursorLocation = e.Location;
                Invalidate();
            };
        }
        [Initializer]
        private void SetDefaultStyles()
        {
            //
            // Chart
            //
            Size = _chrtSize;
            BackColor = SystemColors.Control;
            DoubleBuffered = true;

            //
            // Chart Area
            //
            chartArea.BackColor = Color.Black;
            chartArea.InnerPlotPosition.Auto = false;
            chartArea.InnerPlotPosition.Width = 95;
            chartArea.InnerPlotPosition.Height = 90;
            chartArea.InnerPlotPosition.X = 5;
            chartArea.InnerPlotPosition.Y = 0;

            // Axis X Style
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Weeks;
            chartArea.AxisX.IsStartedFromZero = false;
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.IntervalOffset = 1;
            chartArea.AxisX.Title = "Date";
            chartArea.AxisX.LabelStyle = new LabelStyle() { Format = "MM/dd/yy" };

            chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(25, 25, 25);
            chartArea.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chartArea.AxisX.MajorGrid.LineWidth = 1;
            chartArea.AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Days;
            chartArea.AxisX.MajorGrid.Interval = 1;

            // Axis Y Style
            chartArea.AxisY.IntervalType = DateTimeIntervalType.Number;
            chartArea.AxisY.IsStartedFromZero = false;
            chartArea.AxisY.LabelStyle.Format = "$0.00";
            chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(25, 25, 25);
            chartArea.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chartArea.AxisY.MajorGrid.LineWidth = 1;
            chartArea.AxisX.MajorGrid.IntervalOffset = -0.5;
        }
        [Initializer]
        private void SetMouseScroll()
        {
            MouseWheel += ZoomTimeframeOnWheel;
        }
        [Initializer]
        private void SetMouseDrag()
        {
            MouseMove += MoveTimeframeOnDrag;
        }

        protected abstract void Redraw();
        public abstract void SetView(DateTime min, DateTime max);

        private void ZoomTimeframeOnWheel(object sender, MouseEventArgs e)
        {
            SetZoomStep();

            if (e.Delta < 0)
                SetView(currentView.Item1.Add(-_zoomStep), currentView.Item2);
            else
                SetView(currentView.Item1.Add(+_zoomStep), currentView.Item2);

            AdjustZoomedView();
            Redraw();
        }

        private int lastPositionX { get; set; }
        private void MoveTimeframeOnDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Drag the chart
                int deltaMove = (lastPositionX - e.X);
                SetDragView(deltaMove);
            }

            lastPositionX = e.X;
        }
        private void SetDragView(int deltaX)
        {
            double scaleFactor = 1.04;
            var percentMove = scaleFactor * (deltaX / ((chartArea.InnerPlotPosition.Width / 100d) * Width));
            var daysDrag = (currentView.Span().TotalDays * percentMove);

            SetView(currentView.Item1.AddDays(daysDrag), currentView.Item2.AddDays(daysDrag));
        }

        private void SetZoomStep()
        {
            if (currentView.Span().TotalDays < 90)
                _zoomStep = new TimeSpan(5, 0, 0, 0);
            else if (currentView.Span().TotalDays < 180)
                _zoomStep = new TimeSpan(7, 0, 0, 0);
            else
                _zoomStep = new TimeSpan(30, 0, 0, 0);
        }
        private void AdjustZoomedView()
        {
            //
            // Change some global styles based on zoom level
            //

            // Remove X gridlines on views greater than 360 days
            chartArea.AxisX.MajorGrid.Enabled = !(currentView.Span().TotalDays > 360);


        }

        protected void DrawWeekendStriplines()
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
            chartArea.AxisX.StripLines.Add(slWeekend);
        }
        protected void DrawMonthStartStriplines()
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
            chartArea.AxisX.StripLines.Add(slMonth);
        }
        protected void DrawYearStartStriplines()
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
            chartArea.AxisX.StripLines.Add(slYear);
        }
        protected void DrawHolidayStriplines()
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
                chartArea.AxisX.StripLines.Add(slHoliday);
            }

        }

        protected void SetCursorChartPoint(PaintEventArgs e)
        {
            if (cursorLocation.Value.X.IsBetween(0, Width, false) && cursorLocation.Value.Y.IsBetween(0, Height, false))
            {
                currentCursorPoint = new Tuple<DateTime, double>(
                    DateTime.FromOADate(chartArea.AxisX.PixelPositionToValue(cursorLocation.Value.X)).AddHours(12).Date,
                    chartArea.AxisY.PixelPositionToValue(cursorLocation.Value.Y));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (NoData) PaintNoData(e);

            if (!Ready || cursorLocation == null)
                return;

            SetCursorChartPoint(e);
        }
        protected void PaintNoData(PaintEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(35, 35, 35)))
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.DrawString($"No Data", Helpers.SystemFont(100), brush, Width / 3, Height / 3, new StringFormat());
                return;
            }
        }
        protected void PaintHorizontalCursorLine(PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.DimGray, 1))
            {
                // Draw Horizontal Line
                Point pt1 = new Point(
                    Convert.ToInt32(chartArea.AxisX.ValueToPixelPosition(currentView.Item1.ToOADate())),
                    cursorLocation.Value.Y);

                Point pt2 = new Point(
                    Convert.ToInt32(chartArea.AxisX.ValueToPixelPosition(currentView.Item2.ToOADate())),
                    cursorLocation.Value.Y);

                e.Graphics.DrawLine(pen, pt1, pt2);
            }
        }
        protected void PaintVerticalCursorLine(PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.DimGray, 1))
            {
                // Draw Vertical Line
                Point pt1 = new Point(cursorLocation.Value.X,
                    Convert.ToInt32(chartArea.AxisY.ValueToPixelPosition(chartArea.AxisY.Maximum)));

                Point pt2 = new Point(cursorLocation.Value.X,
                    Convert.ToInt32(chartArea.AxisY.ValueToPixelPosition(chartArea.AxisY.Minimum)));

                e.Graphics.DrawLine(pen, pt1, pt2);
            }
        }

    }

    #endregion
    #region Single Security Chart

    public class SingleSecurityChart : FinanceChart
    {
        SecuritySeries securitySeries;
        Security security;
        PriceBarTooltip barTooltip;

        static decimal _bufferYAxis = 0.05m;

        private BarSize currentBarSize { get; set; }

        public SingleSecurityChart(Security security) : base()
        {
            chartArea = new ChartArea("default");

            this.InitializeMe();

            ChartAreas.Clear();
            ChartAreas.Add(chartArea);

            Load(security);
        }

        [Initializer]
        private void SetStyles()
        {
            //
            // Default values are contained in base class
            //
            chartArea.AxisY.Title = "Share Price";

            DrawWeekendStriplines();
            DrawHolidayStriplines();
            DrawMonthStartStriplines();
            DrawYearStartStriplines();

            MouseMove += DrawSelectionHighlightStripline;
        }
        [Initializer]
        private void SetTooltip()
        {
            barTooltip = new PriceBarTooltip
            {
                Visible = false
            };
            Controls.Add(barTooltip);
            MouseMove += ShowPricebarTooltip;
        }

        protected override void Redraw()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Redraw()));
                return;
            }

            // Load Series into Chart Area
            Series.Clear();
            securitySeries.ChartArea = "default";
            Series.Add(securitySeries);

            // Set X
            SetXAxis(currentView.Item1, currentView.Item2);

            // Set Y
            SetYAxis(securitySeries.MinY(currentView.Item1, currentView.Item2),
                    securitySeries.MaxY(currentView.Item1, currentView.Item2));

            // Refresh View
            Update();

            Ready = true;
        }
        private void SetXAxis(DateTime min, DateTime max)
        {
            chartArea.AxisX.Minimum = min.ToOADate();
            chartArea.AxisX.Maximum = max.ToOADate();

            switch (currentBarSize)
            {
                case BarSize.Daily:
                    {
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Weeks;
                        chartArea.AxisX.IntervalOffset = 1;
                        chartArea.AxisX.LabelStyle = new LabelStyle() { Format = "MM/dd/yy" };
                    }
                    break;
                case BarSize.Weekly:
                    {
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Weeks;
                        chartArea.AxisX.IntervalOffset = 1;
                        chartArea.AxisX.LabelStyle = new LabelStyle() { Format = "MM/dd/yy" };
                    }
                    break;
                case BarSize.Monthly:
                    {
                        chartArea.AxisX.IntervalType = DateTimeIntervalType.Months;
                        chartArea.AxisX.IntervalOffset = 0;
                        chartArea.AxisX.LabelStyle = new LabelStyle() { Format = "MMM yy" };
                    }
                    break;
                default:
                    break;
            }
        }
        private void SetYAxis(decimal min, decimal max)
        {
            double minY = (min * (1 - _bufferYAxis)).ToDouble();
            double maxY = (max * (1 + _bufferYAxis)).ToDouble();

            minY = Math.Floor(minY);
            maxY = Math.Ceiling(maxY);

            chartArea.AxisY.Minimum = minY;
            chartArea.AxisY.Maximum = maxY;

            // Display interval lines rounded to nearest whole value depending on share price
            double IntervalSpan = (chartArea.AxisY.Maximum - chartArea.AxisY.Minimum);
            chartArea.AxisY.Interval = Math.Floor(IntervalSpan / 5);
        }

        private void SetSeriesZoomLevel()
        {
            if (currentView.Span().TotalDays < 360)
            {
                if (currentBarSize != BarSize.Daily)
                    currentBarSize = BarSize.Daily;
            }
            else if (currentView.Span().TotalDays < 720)
            {
                if (currentBarSize != BarSize.Weekly)
                    currentBarSize = BarSize.Weekly;
            }
            else if (currentView.Span().TotalDays >= 720)
            {
                if (currentBarSize != BarSize.Monthly)
                    currentBarSize = BarSize.Monthly;
            }

            if (securitySeries.BarSize != currentBarSize)
            {
                securitySeries.SelectSeries(currentBarSize);
                SetZoomLevelOptions();
            }
        }
        private void SetZoomLevelOptions()
        {
            //
            // Adjust Stripline Options
            //
            chartArea.AxisX.StripLines.Clear();
            switch (currentBarSize)
            {
                case BarSize.Daily:
                case BarSize.Weekly:
                    DrawWeekendStriplines();
                    DrawHolidayStriplines();
                    DrawMonthStartStriplines();
                    DrawYearStartStriplines();
                    break;
                case BarSize.Monthly:
                    DrawYearStartStriplines();
                    break;
                default:
                    break;
            }
        }
        private void ShowPricebarTooltip(object sender, EventArgs e)
        {
            if (HitTest(cursorLocation.Value.X, cursorLocation.Value.Y).ChartElementType == ChartElementType.DataPoint)
            {
                PriceBar bar = (HitTest(cursorLocation.Value.X, cursorLocation.Value.Y).Object as DataPoint).Tag as PriceBar;

                if (barTooltip.Visible && bar == barTooltip.PriceBar)
                    return;
                else
                {
                    barTooltip.Location = cursorLocation.Value - barTooltip.Size;
                    barTooltip.Show(bar);
                }
            }
            else
            {
                barTooltip.Hide();
            }
        }

        public void Load(Security security)
        {
            if (security == null) return;

            Ready = false;
            this.security = security;
            securitySeries = security.ToChartSeries();

            if (!securitySeries.HasPoints)
            {
                securitySeries = SecuritySeries.Default();
                NoData = true;
            }
            else
                NoData = false;

            // Set current view based on points available
            if (securitySeries.MinX > securitySeries.MaxX.Subtract(_defaultView))
                currentView = new Tuple<DateTime, DateTime>(securitySeries.MinX, securitySeries.MaxX);
            else
                currentView = new Tuple<DateTime, DateTime>(securitySeries.MaxX.Subtract(_defaultView), securitySeries.MaxX);

            if (securitySeries.Points.Count > 0)
                Redraw();
        }
        public override void SetView(DateTime min, DateTime max)
        {
            if (!min.IsBetween(securitySeries.MinX, securitySeries.MaxX, false))
                return;

            currentView = new Tuple<DateTime, DateTime>(min, max);
            SetSeriesZoomLevel();
            Redraw();
        }


        #region Chart Effects

        //
        // Striplines highlighting the day under the cursor
        //
        private List<StripLine> selectionHighlightLines = new List<StripLine>();
        private void DrawSelectionHighlightStripline(object sender, MouseEventArgs e)
        {
            //
            // Add a stripline highlighting the selected time we are hovering over
            //
            if (!Ready || currentCursorPoint == null) return;

            if (selectionHighlightLines.Count > 0)
                selectionHighlightLines.ForEach(x => chartArea.AxisX.StripLines.Remove(x));
            selectionHighlightLines.Clear();

            StripLine slHighlight = new StripLine()
            {
                IntervalType = DateTimeIntervalType.Auto,
                BackColor = Color.FromArgb(0, 25, 0),
                BorderColor = Color.FromArgb(10, 10, 10)
            };

            switch (currentBarSize)
            {
                case BarSize.Daily:
                    {
                        slHighlight.IntervalOffset = currentCursorPoint.Item1.ToOADate() - 0.5;
                        slHighlight.StripWidthType = DateTimeIntervalType.Days;
                        slHighlight.StripWidth = 1;
                    }
                    break;
                case BarSize.Weekly:
                    {
                        slHighlight.IntervalOffset = currentCursorPoint.Item1.ToOADate() - currentCursorPoint.Item1.DayOfWeek.ToInt();
                        slHighlight.StripWidthType = DateTimeIntervalType.Days;
                        slHighlight.StripWidth = 5;
                    }
                    break;
                case BarSize.Monthly:
                    {
                        // TODO: Fix this
                        slHighlight.IntervalOffset = new DateTime(currentCursorPoint.Item1.Year, currentCursorPoint.Item1.Month, 15).ToOADate();
                        slHighlight.StripWidthType = DateTimeIntervalType.Months;
                        slHighlight.StripWidth = 1;
                    }
                    break;
                default:
                    break;
            }

            selectionHighlightLines.Add(slHighlight);
            selectionHighlightLines.ForEach(x => chartArea.AxisX.StripLines.Add(x));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!Ready || cursorLocation == null)
                return;

            //
            // Draw Axis Lines
            //
            PaintHorizontalCursorLine(e);

            //
            // Draw Axis labels               
            //
            PaintAxisXLabel(e);
            PaintAxisYLabel(e);
        }
        protected void PaintAxisXLabel(PaintEventArgs e)
        {
            Point pt1 = new Point(
                cursorLocation.Value.X,
                Convert.ToInt32(chartArea.AxisY.ValueToPixelPosition(chartArea.AxisY.Minimum)));

            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                switch (currentBarSize)
                {
                    case BarSize.Daily:
                        g.DrawString($"{currentCursorPoint.Item1:ddd MM/dd/yy}", Helpers.SystemFont(12), brush, cursorLocation.Value.X, pt1.Y - 20);
                        break;
                    case BarSize.Weekly:
                        DateTime displayDate = currentCursorPoint.Item1.AddDays(-currentCursorPoint.Item1.DayOfWeek.ToInt());
                        g.DrawString($"{displayDate:dd MMM}-{displayDate.AddDays(4):dd MMM yy}", Helpers.SystemFont(12), brush, cursorLocation.Value.X, pt1.Y - 20);
                        break;
                    case BarSize.Monthly:
                        g.DrawString($"{currentCursorPoint.Item1:MMM yy}", Helpers.SystemFont(12), brush, cursorLocation.Value.X, pt1.Y - 20);
                        break;
                    default:
                        break;
                }
            }
        }
        protected void PaintAxisYLabel(PaintEventArgs e)
        {
            Point pt1 = new Point(
                Convert.ToInt32(chartArea.AxisX.ValueToPixelPosition(currentView.Item1.ToOADate())),
                cursorLocation.Value.Y);

            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.DrawString($"{currentCursorPoint.Item2:$0.00}", Helpers.SystemFont(12), brush, pt1.X, cursorLocation.Value.Y);
            }
        }

        #endregion
    }
    public class SecuritySeries : Series
    {
        public DateTime MinX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Min()); }
        public DateTime MaxX { get => DateTime.FromOADate((from pt in Points select pt.XValue).Max()); }
        public decimal MinY(DateTime start, DateTime end)
        {
            return (from pt in Points
                    where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
                    where pt.YValues.Count() > 0
                    select pt.YValues.Min()).Min().ToDecimal();
        }
        public decimal MaxY(DateTime start, DateTime end)
        {
            return (from pt in Points
                    where DateTime.FromOADate(pt.XValue).IsBetween(start, end)
                    where pt.YValues.Count() > 0
                    select pt.YValues.Max()).Max().ToDecimal();
        }
        public PriceBar FromXValue(DateTime X)
        {
            return priceBars.FirstOrDefault(x => x.BarDateTime == X);
        }
        public bool HasPoints
        {
            get
            {
                return (Points.Count > 0);
            }
        }

        public BarSize BarSize { get; private set; } = BarSize.Daily;

        public Security Security { get; }
        private List<PriceBar> priceBars { get; set; }
        private Dictionary<BarSize, List<DataPoint>> seriesViews { get; }

        public SecuritySeries(Security security)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));

            seriesViews = new Dictionary<BarSize, List<DataPoint>>();

            this.InitializeMe();
        }
        private SecuritySeries() { }
        public static SecuritySeries Default()
        {
            SecuritySeries ret = new SecuritySeries();
            ret.SetStyles();

            ret.Points.AddXY(DateTime.Today.AddDays(-30).ToOADate(), 0);
            ret.Points.AddXY(DateTime.Today.ToOADate(), 1);

            return ret;
        }

        [Initializer]
        private void SetStyles()
        {
            ChartType = SeriesChartType.Candlestick;

            this["PriceUpColor"] = "Green";
            this["PriceDownColor"] = "Red";
        }
        [Initializer]
        private void BuildSeries()
        {
            priceBars = Security.GetPriceBars();
            priceBars.Sort((x, y) => x.BarDateTime.CompareTo(y.BarDateTime));

            BuildSeries_Daily();
            BuildSeries_Weekly();
            BuildSeries_Monthly();

            Points.Clear();
            seriesViews[BarSize].ForEach(pt => Points.Add(pt));
        }
        private void BuildSeries_Daily()
        {
            var pts = new List<DataPoint>();
            foreach (PriceBar bar in priceBars)
            {
                var pt = new DataPoint(this)
                {
                    XValue = bar.BarDateTime.ToOADate(),
                    YValues = bar.AsChartingValue(),
                    IsValueShownAsLabel = false,
                    Tag = bar,
                    Color = (bar.Change >= 0 ? Color.Green : Color.Red)
                };
                pts.Add(pt);
            }
            seriesViews.Add(BarSize.Daily, pts);
        }
        private void BuildSeries_Weekly()
        {
            var pts = new List<DataPoint>();
            foreach (PriceBar bar in priceBars.ToWeekly())
            {
                var pt = new DataPoint(this)
                {
                    XValue = bar.BarDateTime.ToOADate(),
                    YValues = bar.AsChartingValue(),
                    IsValueShownAsLabel = false,
                    Tag = bar,
                    Color = (bar.Change >= 0 ? Color.Green : Color.Red)
                };
                pts.Add(pt);
            }
            seriesViews.Add(BarSize.Weekly, pts);
        }
        private void BuildSeries_Monthly()
        {
            var pts = new List<DataPoint>();
            foreach (PriceBar bar in priceBars.ToMonthly())
            {
                var pt = new DataPoint(this)
                {
                    XValue = bar.BarDateTime.ToOADate(),
                    YValues = bar.AsChartingValue(),
                    IsValueShownAsLabel = false,
                    Tag = bar,
                    Color = (bar.Change >= 0 ? Color.Green : Color.Red)
                };
                pts.Add(pt);
            }
            seriesViews.Add(BarSize.Monthly, pts);
        }

        public void SelectSeries(BarSize barSize)
        {
            if (barSize == BarSize)
                return;

            BarSize = barSize;
            Points.Clear();
            seriesViews[barSize].ForEach(pt => Points.Add(pt));
        }
    }

    #endregion
    #region Multi Security Chart

    public partial class MultiSecurityChart : Chart
    {
        ChartArea chartArea;
        SecuritySeries securitySeries;
        List<Security> securities;

        static decimal _bufferYAxis = 0.10m;

        public MultiSecurityChart(List<Security> securities) : base()
        {
            this.securities = securities;
            chartArea = new ChartArea();

            this.InitializeMe();

            ChartAreas.Clear();
            ChartAreas.Add(chartArea);

            if (this.securities.Count > 0)
                Load();
        }

        [Initializer]
        private void SetStyles()
        {
            chartArea.BackColor = Color.FromArgb(255, 0, 0, 8);

            // Axis X Style
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Days;
            chartArea.AxisX.IsStartedFromZero = false;
            chartArea.AxisX.Interval = 7;
            chartArea.AxisX.Title = "Date";

            // Axis Y Style
            chartArea.AxisY.IntervalType = DateTimeIntervalType.Number;
            chartArea.AxisY.IsStartedFromZero = false;
            chartArea.AxisY.LabelStyle.Format = "$0.00";
            chartArea.AxisY.Title = "Share Price";

        }

        private void SetXAxis(DateTime min, DateTime max)
        {
            chartArea.AxisX.Minimum = min.ToOADate();
            chartArea.AxisX.Maximum = max.ToOADate();
        }
        private void SetYAxis(decimal min, decimal max)
        {
            chartArea.AxisY.Minimum = (min * (1 - _bufferYAxis)).ToDouble();
            chartArea.AxisY.Maximum = (max * (1 + _bufferYAxis)).ToDouble();

            // Display interval lines rounded to nearest whole value depending on share price
            double IntervalSpan = (chartArea.AxisY.Maximum - chartArea.AxisY.Minimum);
            chartArea.AxisY.Interval = Math.Floor(IntervalSpan / 5);
            chartArea.AxisY.MajorGrid.LineColor = Color.DarkGoldenrod;
        }

        public void Load()
        {
            if (securities.Count == 0)
                return;

            //securitySeries = this.security.ToChartSeries();

            if (securitySeries.Points.Count > 0)
                Redraw();
        }
        private void Redraw()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Redraw()));
                return;
            }

            // Load Series into Chart Area
            Series.Clear();
            securitySeries.ChartArea = "default";
            Series.Add(securitySeries);

            // Set X
            SetXAxis(securitySeries.MinX, securitySeries.MaxX);

            // Set Y
            //SetYAxis(securitySeries.MinY, securitySeries.MaxY);

            // Refresh View
            Update();
        }
    }

    #endregion
    #region Other Charting Elements

    public class PriceBarTooltip : Panel
    {
        Size _pnlSize = new Size(100, 100);
        Label lblDisplayTxt = new Label();
        public PriceBar PriceBar { get; private set; }

        public PriceBarTooltip()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void SetStyle()
        {
            BackColor = Color.FromArgb(10, 10, 10);
            BorderStyle = BorderStyle.FixedSingle;
            Size = _pnlSize;
            DoubleBuffered = true;

            lblDisplayTxt.Font = Helpers.SystemFont(8);
            lblDisplayTxt.ForeColor = Color.White;
            lblDisplayTxt.Size = _pnlSize;
            Controls.Add(lblDisplayTxt);
        }

        public void Show(PriceBar priceBar)
        {
            PriceBar = priceBar;

            string displayTxt = string.Format($"{priceBar.BarDateTime.ToShortDateString()}{Environment.NewLine}" +
                $"OPEN:  {priceBar.Open}{Environment.NewLine}" +
                $"HIGH:  {priceBar.High}{Environment.NewLine}" +
                $"LOW:   {priceBar.Low}{Environment.NewLine}" +
                $"CLOSE: {priceBar.Close}{Environment.NewLine}" +
                $"CHG:   {priceBar.Change}{Environment.NewLine}" +
                $"RNG:   {priceBar.Range}");

            lblDisplayTxt.Text = displayTxt;

            Show();
        }

    }

    #endregion
}
