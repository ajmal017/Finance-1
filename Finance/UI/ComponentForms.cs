using Finance.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms.DataVisualization.Charting;
using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;
using static Finance.Helpers;

namespace Finance
{
    #region System Clock

    //
    // Simple UI Components enabling display of a clock panel with user-selected Time Zones
    //

    public class SystemClock : Form, IPersistLayout
    {
        private static SystemClock _Instance { get; set; }
        public static SystemClock Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new SystemClock();
                return _Instance;
            }
        }

        public Size _defaultSize => new Size(0, 0);
        public bool Sizeable => false;

        Timer tmrClockTimer;
        List<TimeZoneInfo> timeZones;
        Panel innerPanel;
        MenuStrip menuStrip;
        ToolStripMenuItem menuAddZone;

        public SystemClock()
        {
            Name = "Clock";

            this.InitializeMe();
            tmrClockTimer?.Start();

            this.Load += (s, e) => SetDisplay();
            this.Shown += (s, e) => LoadLayout();
            this.ResizeEnd += (s, e) => SaveLayout();
        }

        [Initializer]
        private void SetMenuStrip()
        {
            menuStrip = new MenuStrip();
            menuAddZone = new ToolStripMenuItem();

            menuStrip.Name = "menu";
            menuAddZone.Text = "Add";
            menuAddZone.Click += (s, e) =>
            {
                (new TimeZoneSelector(this)).Show();
            };

            menuStrip.Items.Add(menuAddZone);
            Controls.Add(menuStrip);
        }
        [Initializer]
        private void SetStyle()
        {
            //
            // Form
            //
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Text = "World Clock";

            //
            // Inner Panel
            //
            innerPanel = new Panel();
            Controls.Add(innerPanel);
            innerPanel.DockTo(menuStrip, ControlEdge.Bottom, 0);
        }
        [Initializer]
        private void SetSavedTimeZones()
        {
            var str = Settings.Instance.WorldClockZones;
            foreach (string tzId in str.Split(','))
            {
                AddTimeZone(tzId);
            }
        }
        [Initializer]
        private void SetTimerActions()
        {
            tmrClockTimer = new Timer();
            tmrClockTimer.Interval = 1000 - DateTime.Now.Millisecond;
            tmrClockTimer.Tick += (s, e) => { SetTimes(); };
        }

        private void SetDisplay()
        {
            SuspendLayout();
            innerPanel.Controls.Clear();
            foreach (TimeZoneInfo zone in timeZones)
            {
                var ctrl = new TimePanel(zone, (zone.Id == TimeZoneInfo.Local.Id ? true : false));
                ctrl.Delete += (s, e) =>
                {
                    if (s is TimePanel t)
                        RemoveTimeZone(t.timeZone);
                };
                innerPanel.Controls.Add(ctrl);

                for (int i = 1; i < innerPanel.Controls.Count; i++)
                    innerPanel.Controls[i].DockTo(innerPanel.Controls[i - 1], ControlEdge.Bottom, 0);

                innerPanel.Height = innerPanel.Controls.Count * TimePanel._panelSize.Height;
                innerPanel.Width = TimePanel._panelSize.Width;
            }

            Height = innerPanel.Height + menuStrip.Height + (Height - ClientRectangle.Height);
            Width = innerPanel.Width + (Width - ClientRectangle.Width);

            ResumeLayout();
        }
        private void SetTimes()
        {
            tmrClockTimer.Interval = 1000;
            if (Visible)
                Invoke(new Action(() =>
                {
                    foreach (Control ctrl in innerPanel.Controls)
                    {
                        if (ctrl is TimePanel t)
                            t.UpdateDisplay(DateTime.Now);
                    }
                }));
        }
        public void AddTimeZone(TimeZoneInfo timeZone)
        {
            if (timeZones == null)
                timeZones = new List<TimeZoneInfo>();

            if (timeZones.Contains(timeZone))
                return;

            timeZones.Add(timeZone);
            timeZones.Sort((x, y) => x.BaseUtcOffset.CompareTo(y.BaseUtcOffset));

            SaveTimeZoneSettings();
            SetDisplay();
        }
        public void AddTimeZone(string timeZoneId)
        {
            var tz = TimeZoneInfo.GetSystemTimeZones().First(x => x.Id == timeZoneId);
            AddTimeZone(tz);
        }
        public void RemoveTimeZone(TimeZoneInfo timeZone)
        {
            timeZones.Remove(timeZone);

            SaveTimeZoneSettings();
            SetDisplay();
        }
        public void RemoveTimeZone(string timeZoneId)
        {
            var tz = TimeZoneInfo.GetSystemTimeZones().First(x => x.Id == timeZoneId);
            RemoveTimeZone(tz);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            base.OnPaint(e);
        }
        private void SaveTimeZoneSettings()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (TimeZoneInfo tzi in timeZones)
                sb.AppendFormat("{0},", tzi.Id);

            Settings.Instance.WorldClockZones = sb.ToString().TrimEnd(',');
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
    public class TimePanel : Panel
    {
        public event EventHandler Delete;
        private void OnDelete() => Delete?.Invoke(this, null);

        public TimeZoneInfo timeZone { get; }
        public bool IsLocal { get; }
        Label lblZone;
        Label lblDate;
        Label lblClock;
        Label lblSeconds;
        Label lblDelete;

        public readonly static Size _panelSize = new Size(350, 125);

        public TimePanel(TimeZoneInfo timeZone, bool isLocal = false)
        {
            this.timeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
            IsLocal = isLocal;
            this.InitializeMe();

            UpdateDisplay(DateTime.Now);
        }

        [Initializer]
        private void SetStyle()
        {
            lblZone = new Label();
            lblDate = new Label();
            lblClock = new Label();
            lblSeconds = new Label();

            //
            // this Panel
            //
            BackColor = Color.Black;
            Size = _panelSize;

            //
            // lblZone
            //
            lblZone.Font = Helpers.SystemFont(12, FontStyle.Bold);
            lblZone.BackColor = IsLocal ? Color.DarkGreen : Color.DarkGoldenrod;
            lblZone.ForeColor = IsLocal ? Color.LightGray : Color.Black;
            lblZone.TextAlign = ContentAlignment.MiddleCenter;
            lblZone.Size = new Size(_panelSize.Width, 25);
            TimeSpan offset = timeZone.GetUtcOffset(DateTime.Now);
            lblZone.Text = $"{timeZone.StandardName} (UTC{(offset < TimeSpan.Zero ? "-" : "+")}{offset:hh\\:mm})";

            //
            // lblDate
            //
            lblDate.Font = Helpers.SystemFont(10);
            lblDate.ForeColor = Color.White;
            lblDate.TextAlign = ContentAlignment.MiddleCenter;
            lblDate.Size = new Size(_panelSize.Width, 15);

            //
            // lblClock
            //
            lblClock.Font = Helpers.SystemFont(48);
            lblClock.ForeColor = IsLocal ? Color.Green : Color.DarkGoldenrod;
            lblClock.TextAlign = ContentAlignment.MiddleCenter;
            lblClock.Size = new Size(_panelSize.Width, 60);

            //
            // lblSeconds
            //
            lblSeconds.Font = Helpers.SystemFont(12);
            lblSeconds.ForeColor = IsLocal ? Color.Green : Color.DarkGoldenrod;
            lblSeconds.TextAlign = ContentAlignment.MiddleCenter;
            lblSeconds.Size = new Size(50, 20);

            //
            // Add and Arrange
            //
            Controls.Add(lblZone);
            Controls.Add(lblDate);
            Controls.Add(lblClock);
            Controls.Add(lblSeconds);

            lblZone.Location = new Point(0, 0);
            lblDate.DockTo(lblZone, ControlEdge.Bottom, 0);
            lblClock.DockTo(lblDate, ControlEdge.Bottom, 0);
            lblSeconds.Location = new Point(260, 83);
            lblSeconds.BringToFront();
        }
        [Initializer]
        private void SetDelete()
        {
            if (IsLocal) return;

            lblDelete = new Label();
            lblDelete.Size = new Size(15, 15);
            lblDelete.Text = "X";
            lblDelete.Font = Helpers.SystemFont(8);
            lblDelete.BackColor = lblZone.BackColor;
            lblDelete.ForeColor = Color.Black;

            lblDelete.Click += (s, e) => OnDelete();

            Controls.Add(lblDelete);
            lblDelete.Location = new Point(this.Width - 15, 0);
            lblDelete.BringToFront();
        }

        public void UpdateDisplay(DateTime time)
        {
            lblDate.Text = TimeZoneInfo.ConvertTime(time, timeZone).ToString("dddd, dd MMMM yyyy (MM/dd/yy)");
            lblClock.Text = TimeZoneInfo.ConvertTime(time, timeZone).ToString(@"HH\:mm");
            lblSeconds.Text = DateTime.Now.ToString(@".ss");

            Refresh();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            base.OnPaint(e);
        }
    }
    public class TimeZoneSelector : Form
    {
        Size _formSize = new Size(300, 375);
        Size _selectorSize = new Size(275, 275);
        Size _buttonSize = new Size(125, 40);

        ListBox listSelector;
        Button btnAccept;
        Button btnClose;

        SystemClock clock;

        public TimeZoneSelector(SystemClock clock)
        {
            this.clock = clock;
            this.InitializeMe();
            Show();
        }
        [Initializer]
        private void SetStyle()
        {
            Size = _formSize;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
        }
        [Initializer]
        private void SetControls()
        {
            listSelector = new ListBox();
            btnAccept = new Button();
            btnClose = new Button();

            //
            // Selector
            //
            listSelector.Size = _selectorSize;
            listSelector.DataSource = (TimeZoneInfo.GetSystemTimeZones());
            listSelector.DisplayMember = "StandardName";
            listSelector.ValueMember = "Id";

            //
            // Accept
            //
            btnAccept.Size = _buttonSize;
            btnAccept.Text = "Add";
            btnAccept.Click += (s, e) =>
            {
                if (listSelector.SelectedIndex != -1)
                    this.clock?.AddTimeZone(listSelector.SelectedValue as string);
            };

            //
            // Closer
            //
            btnClose.Size = _buttonSize;
            btnClose.Text = "Close";
            btnClose.Click += (s, e) =>
            {
                Close();
            };

            //
            // Add and arrange
            //
            listSelector.Location = new Point(5, 10);

            Controls.Add(listSelector);
            Controls.Add(btnAccept);
            Controls.Add(btnClose);

            btnAccept.DockTo(listSelector, ControlEdge.Bottom, 5);
            btnClose.DockTo(btnAccept, ControlEdge.Right, 25);

        }

    }

    #endregion
    #region Data Scroller Form

    public class DataScrollerForm : Form
    {
        public Size _defaultSize => new Size(500, 25);
        public int FontSize { get; }
        Timer tmrScroll;

        Queue<Label> ScrollQueue = new Queue<Label>();

        public DataScrollerForm(int FontSize = 14)
        {
            this.FontSize = FontSize;
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyle()
        {
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.Black;
            DoubleBuffered = true;
            var formHandle = new FormHandle(this);
        }

        [Initializer]
        private void InitializeScroll()
        {
            tmrScroll = new Timer()
            {
                Interval = 5
            };
            tmrScroll.Tick += (s, e) =>
            {
                int last = 0;
                foreach (Control ctrl in this.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.Location = new Point(lbl.Location.X - 1, lbl.Location.Y);
                        if (lbl.Right <= 0)
                        {
                            lbl.Name = "Remove";
                            AddText(lbl.Text);
                        }
                        last = lbl.Right;
                    }
                }

                this.Controls.RemoveByKey("Remove");

                if (last < this.Width - 20 && ScrollQueue.Count > 0)
                {
                    this.Controls.Add(ScrollQueue.Dequeue());
                }
            };
            tmrScroll.Start();
        }
        public void AddText(string text)
        {
            ScrollQueue.Enqueue(new Label()
            {
                Text = text,
                Location = new Point(this.Width, 2),
                ForeColor = Color.White,
                Font = SystemFont(FontSize, FontStyle.Bold),
                AutoSize = true
            });
        }

    }

    #endregion
    #region Form Handle

    /*
     *  Provides a small area for moving and closing a borderless form
     */

    public class FormHandle : Form
    {
        private new Form ParentForm { get; }

        public FormHandle(Form parentForm)
        {
            ParentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            this.InitializeMe();
            this.Show();
        }

        [Initializer]
        private void InitializeStyle()
        {
            Size = new Size(100, ParentForm.Height);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Location = new Point(ParentForm.Location.X, ParentForm.Location.Y - 25);
        }
        [Initializer]
        private void InitializeFormControl()
        {
            this.Move += FormHandle_Move;
            this.FormClosed += FormHandle_FormClosed;
            this.GotFocus += FormHandle_GotFocus;
            ParentForm.SizeChanged += FormHandle_SizeChanged;
            ParentForm.LocationChanged += FormHandle_LocationChanged;
            ParentForm.FormClosed += ParentForm_FormClosed;
        }

        private void FormHandle_GotFocus(object sender, EventArgs e)
        {
            ParentForm.BringToFront();
        }
        private void FormHandle_LocationChanged(object sender, EventArgs e)
        {
            Location = new Point(ParentForm.Location.X, ParentForm.Location.Y - 25);
        }
        private void FormHandle_SizeChanged(object sender, EventArgs e)
        {
            Size = new Size(100, ParentForm.Height);
        }
        private void ParentForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this?.Close();
        }
        private void FormHandle_FormClosed(object sender, FormClosedEventArgs e)
        {
            ParentForm?.Close();
        }
        private void FormHandle_Move(object sender, EventArgs e)
        {
            if (ParentForm != null && !ParentForm.IsDisposed)
                ParentForm.Location = new Point(this.Location.X, this.Location.Y + 25);
        }
    }

    #endregion
    #region System Mode Indicator

    /// <summary>
    /// Borderless 'strip' attached to bottom of a screen which displays current system status as exists in global settings
    /// </summary>
    public class SystemModeIndicatorForm : Form
    {
        Label lblText;
        string strPrefix = "TRADING SYSTEM MODE:";
        int _formHeight = 35;
        Screen screen { get; }

        public SystemModeIndicatorForm(Screen screen)
        {
            this.screen = screen;
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyles()
        {
            FormBorderStyle = FormBorderStyle.None;
            Height = _formHeight;
            Width = Screen.FromControl(this).WorkingArea.Width;
            Location = new Point(screen.WorkingArea.Left, screen.WorkingArea.Bottom - _formHeight);
            BackColor = Settings.Instance.ApplicationMode == ApplicationMode.Production ? Color.DarkGreen : Color.DarkRed;
            ShowInTaskbar = false;
        }

        [Initializer]
        private void InitializeDataBinding()
        {
            lblText = new Label()
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.Black,
                Font = SystemFont(16, FontStyle.Bold),
                Text = $"{strPrefix} {Settings.Instance.ApplicationMode.Description()}",
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblText);

            //lblText.DataBindings.Add(new Binding("Text", Settings.Instance, "ApplicationModeString"));

            Settings.Instance.PropertyChanged += (s, e) =>
            {
                BackColor = Settings.Instance.ApplicationMode == ApplicationMode.Production ? Color.DarkGreen : Color.DarkRed;
                lblText.Text = $"{strPrefix} {Settings.Instance.ApplicationMode.Description()}";
            };
        }

    }

    #endregion
    #region IEX Message Status Display

    //public class IexMessageStatusForm : Form, IPersistLayout
    //{
    //    private static IexMessageStatusForm _Instance { get; set; }
    //    public static IexMessageStatusForm Instance
    //    {
    //        get
    //        {
    //            if (_Instance == null)
    //                _Instance = new IexMessageStatusForm();
    //            return _Instance;
    //        }
    //    }

    //    public bool Sizeable => false;

    //    Size _defaultSize = new Size(350, 100);

    //    Label lblMessagesUsed;
    //    Timer tmrUpdate;

    //    private IexMessageStatusForm()
    //    {
    //        Name = "IexMessages";
    //        this.InitializeMe();
    //        UpdateLabel();

    //        this.Shown += (s, e) => LoadLayout();
    //        this.ResizeEnd += (s, e) => SaveLayout();
    //    }

    //    [Initializer]
    //    private void InitializeStyles()
    //    {
    //        this.Size = _defaultSize;
    //        this.Text = "IEX Message Limit";
    //        this.FormBorderStyle = FormBorderStyle.FixedDialog;

    //        lblMessagesUsed = new Label()
    //        {
    //            Dock = DockStyle.Fill,
    //            Font = SystemFont(10, FontStyle.Bold),
    //            TextAlign = ContentAlignment.MiddleCenter,
    //            BackColor = Color.Black,
    //            ForeColor = Color.Goldenrod
    //        };
    //        this.Controls.Add(lblMessagesUsed);
    //    }

    //    [Initializer]
    //    private void InitializeUpdateHandler()
    //    {
    //        Settings.Instance.PropertyChanged += (s, e) =>
    //        {
    //            if (!this.Visible)
    //                return;

    //            //if (e.PropertyName == "IexMessageCount")
    //            //{
    //            //    Invoke(new Action(() => UpdateLabel()));
    //            //}

    //            if (e.PropertyName == "DataProvider")
    //            {
    //                ShowHide();
    //            }
    //            if (e.PropertyName == "IexCloudMode")
    //            {
    //                ShowHide();
    //            }
    //        };
    //    }

    //    [Initializer]
    //    private void InitializeUpdateTimer()
    //    {
    //        tmrUpdate = new Timer()
    //        {
    //            Interval = 2000
    //        };
    //        tmrUpdate.Tick += (s, e) => UpdateLabel();
    //    }

    //    public void ShowHide()
    //    {
    //        if (Settings.Instance.RefDataProvider == DataProviderType.IEXCloud
    //            && Settings.Instance.IexCloudMode == IexCloudMode.Production)
    //        {
    //            this.Show();
    //            tmrUpdate.Start();
    //        }
    //        else
    //        {
    //            this.Hide();
    //            tmrUpdate.Stop();
    //        }
    //    }

    //    private void UpdateLabel()
    //    {
    //        if (InvokeRequired)
    //        {
    //            Invoke(new Action(() => UpdateLabel()));
    //            return;
    //        }

    //        double percentUsed = 100 * (Settings.Instance.IexMessageCount.ToDouble() / Settings.Instance.IexMessageCountLimit.ToDouble());
    //        string displayTxt = string.Format($@"Messages used: {Settings.Instance.IexMessageCount:###,###,##0} / {Settings.Instance.IexMessageCountLimit:###,###,###} ({percentUsed:0.00}%)");
    //        lblMessagesUsed.Text = displayTxt;
    //    }

    //    public void SaveLayout()
    //    {
    //        Settings.Instance.SaveFormLayout(this);
    //    }
    //    public void LoadLayout()
    //    {
    //        Settings.Instance.LoadFormLayout(this);
    //    }
    //}

    #endregion
};