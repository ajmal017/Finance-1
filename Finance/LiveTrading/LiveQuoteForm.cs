using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Finance;
using Finance.Data;
using Finance.LiveTrading;
using static Finance.Helpers;

namespace Finance.LiveTrading
{
    public class LiveQuoteForm : Form, IPersistLayout
    {
        private static LiveQuoteForm _Instance { get; set; }
        public static LiveQuoteForm Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new LiveQuoteForm();
                }
                return _Instance;
            }
        }

        #region Events

        public event SelectedSecurityChangedEventHandler ActiveSecurityChanged;
        private void OnActiveSecurityChanged()
        {
            ActiveSecurityChanged?.Invoke(this, new SelectedSecurityEventArgs(this.ActiveSecurity));
        }

        #endregion

        public Security ActiveSecurity { get; protected set; }
        public bool Sizeable => false;

        private LiveIntradayTickChartPanel liveIntradayTickChartPanel1;
        private LiveQuotePanel liveQuotePanel1;

        private LiveQuoteForm()
        {
            this.Shown += (s, e) => LoadLayout();
            this.ResizeEnd += (s, e) => SaveLayout();

            InitializeComponent();
            this.InitializeMe();

            this.FormClosing += (s, e) =>
            {
                this.Hide();
                e.Cancel = true;
            };
        }

        private void InitializeComponent()
        {
            this.liveQuotePanel1 = new Finance.LiveQuotePanel();
            this.liveIntradayTickChartPanel1 = new Finance.LiveIntradayTickChartPanel();
            this.SuspendLayout();
            // 
            // liveQuotePanel1
            // 
            this.liveQuotePanel1.BackColor = System.Drawing.Color.Black;
            this.liveQuotePanel1.Location = new System.Drawing.Point(13, 13);
            this.liveQuotePanel1.MaximumSize = new System.Drawing.Size(400, 250);
            this.liveQuotePanel1.MinimumSize = new System.Drawing.Size(400, 250);
            this.liveQuotePanel1.Name = "liveQuotePanel1";
            this.liveQuotePanel1.Size = new System.Drawing.Size(400, 250);
            this.liveQuotePanel1.TabIndex = 0;
            // 
            // liveIntradayTickChartPanel1
            // 
            this.liveIntradayTickChartPanel1.Location = new System.Drawing.Point(426, 14);
            this.liveIntradayTickChartPanel1.MaximumSize = new System.Drawing.Size(500, 250);
            this.liveIntradayTickChartPanel1.MinimumSize = new System.Drawing.Size(500, 250);
            this.liveIntradayTickChartPanel1.Name = "liveIntradayTickChartPanel1";
            this.liveIntradayTickChartPanel1.Size = new System.Drawing.Size(500, 250);
            this.liveIntradayTickChartPanel1.TabIndex = 1;
            // 
            // TradeEntryForm
            // 
            this.ClientSize = new System.Drawing.Size(938, 276);
            this.Controls.Add(this.liveIntradayTickChartPanel1);
            this.Controls.Add(this.liveQuotePanel1);
            this.Name = "TradeEntryForm";
            this.ResumeLayout(false);

        }

        [Initializer]
        private void InitializeHandlers()
        {

        }

        public void SetActiveSecurity(Security security)
        {
            if (!LiveDataProvider.Instance.Connected)
                return;

            if (ActiveSecurity != security)
            {
                ActiveSecurity = security;
                OnActiveSecurityChanged();

                liveQuotePanel1.LoadSecurity(this.ActiveSecurity);
                liveIntradayTickChartPanel1.LoadSecurity(this.ActiveSecurity);
                LiveDataProvider.Instance.RequestStreamingQuotes(this.ActiveSecurity);
            }
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
