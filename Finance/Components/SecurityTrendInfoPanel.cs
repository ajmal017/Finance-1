using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using static Finance.Helpers;

namespace Finance
{
    public class SecurityTrendInfoPanel : FlowLayoutPanel
    {
        Size _defaultSize = new Size(200, 20);

        public Security Security { get; private set; }
        public PriceBarSize PriceBarSize { get; private set; }

        private DateTime Start { get; set; }
        private DateTime End { get; set; }

        public SecurityTrendInfoPanel()
        {
            this.InitializeMe();
        }
        public SecurityTrendInfoPanel(Security security, PriceBarSize priceBarSize, DateTime? start = null, DateTime? end = null)
        {
            this.InitializeMe();
            LoadSecurity(security, priceBarSize, start, end);
        }

        [Initializer]
        private void InitializeStyles()
        {
            Size = _defaultSize;
            FlowDirection = FlowDirection.TopDown;
            Margin = new Padding(0);
            Padding = new Padding(0);
        }
        public void LoadSecurity(Security security, PriceBarSize priceBarSize, DateTime? start = null, DateTime? end = null)
        {
            if (security == null || security.DailyPriceBarData.Count == 0)
                return;

            this.Security = security;
            this.PriceBarSize = priceBarSize;
            this.Start = start ?? Security.GetFirstBar(PriceBarSize).BarDateTime;
            this.End = end ?? Security.GetLastBar(PriceBarSize).BarDateTime;

            UpdateInfo();
        }
        private void UpdateInfo()
        {
            SuspendLayout();
            this.Controls.Clear();
            var trendInfo = Security.GetNetChangeByTrendType(PriceBarSize, 6, Start, End);
            foreach (var item in trendInfo)
            {
                this.Controls.Add(new TrendInfoPanel(item));
            }
            ResizeControls();
            ResumeLayout();
        }
        private void ResizeControls()
        {
            this.Height = 0;
            foreach (Control ctrl in this.Controls)
            {
                ctrl.Width = this.Width;
                this.Height += ctrl.Height;
            }
        }

        private class TrendInfoPanel : Panel
        {
            Size _defaultSize = new Size(100, 20);
            private Analysis.NetChangeByTrendType TrendInfo;

            public TrendInfoPanel(Analysis.NetChangeByTrendType trendInfo)
            {
                TrendInfo = trendInfo ?? throw new ArgumentNullException(nameof(trendInfo));
                this.InitializeMe();
                ResizeControls();
            }

            private void ResizeControls()
            {
                this.Controls["title"].Size = new Size((this.Width / 3) * 2, this.Height);
                this.Controls["values"].Size = new Size((this.Width / 3), this.Height);
                this.Controls["values"].DockTo(this.Controls["title"], ControlEdge.Right, 0);
                Refresh();
            }

            [Initializer]
            private void InitializeStyles()
            {
                this.Size = _defaultSize;
                this.Padding = new Padding(0);
                this.Margin = new Padding(0);
                this.BackColor = Color.Black;
                this.SizeChanged += (s, e) => ResizeControls();
            }

            [Initializer]
            private void InitializeDisplay()
            {
                Label title = new Label()
                {
                    Name = "title",
                    Text = TrendInfo.TrendType.Description(),
                    Size = new Size(this.Width / 3, this.Height),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(0, 0),
                    Font = SystemFont(8, FontStyle.Bold),
                    BackColor = Settings.Instance.GetColorByTrend(TrendInfo.TrendType)
                };
                switch (TrendInfo.TrendType)
                {
                    case TrendQualification.NotSet:
                        title.ForeColor = Color.Black;
                        break;
                    case TrendQualification.AmbivalentSideways:
                        title.ForeColor = Color.White;
                        break;
                    case TrendQualification.SuspectSideways:
                        title.ForeColor = Color.White;
                        break;
                    case TrendQualification.ConfirmedSideways:
                    case TrendQualification.SuspectBullish:
                    case TrendQualification.ConfirmedBullish:
                    case TrendQualification.SuspectBearish:
                    case TrendQualification.ConfirmedBearish:
                        title.ForeColor = Color.White;
                        break;
                }

                Label values = new Label()
                {
                    Name = "values",
                    Text = $"{TrendInfo.AverageChange:0.00%} ({TrendInfo.NumberOccurrance}x)",
                    Size = new Size((this.Width / 3) * 2, this.Height),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.White,
                    Font = SystemFont(8)
                };

                this.Controls.AddRange(new[] { title, values });
                values.DockTo(title, ControlEdge.Right, 0);

                Refresh();
            }

        }
    }
}
